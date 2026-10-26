using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BomEntity = Bom.Domain.Entities.Bom;

namespace Bom.Infrastructure.Services;

/// <summary>
/// Produksi &amp; COGS aktual di atas <see cref="BomDbContext"/>.
///
/// <para><b>Buku besar stok + rata-rata bergerak</b> (<see cref="ApplyMovementAsync"/>): tiap
/// perubahan saldo menulis satu <see cref="StockMovement"/> (Qty = magnitudo positif; arah dari
/// Type) dan meng-upsert <see cref="InventoryBalance"/>.
/// Masuk (Receipt/ProductionReceipt): newQty = onHand + qty; newAvg = (onHand*avg + qty*unitCost)/newQty.
/// Keluar (Issue/ProductionIssue): unitCost = rata-rata saat ini; onHand -= qty; rata-rata tetap.
/// Saldo negatif diizinkan (backflush) dan tidak memblok penutupan order.</para>
///
/// <para><b>Alur close → COGS</b>: untuk tiap konsumsi, ActualUnitCost = rata-rata komponen saat itu,
/// ActualCost = ActualQty * ActualUnitCost, lalu posting ProductionIssue. materialCost = Σ ActualCost;
/// conversionCost = (Σ Labor + Σ Overhead) * actualQty; COGS_total = material + conversion;
/// COGS_unit = COGS_total / actualQty. Produk jadi diterima (ProductionReceipt) pada COGS_unit.</para>
///
/// <para><b>Variance</b> memakai <see cref="ICostingService"/> (biaya standar):
/// PriceVariance = (ActualUnitCost − stdUnit) * ActualQty; UsageVariance = (ActualQty − stdQtyAllowed) * stdUnit.
/// Positif = tidak menguntungkan.</para>
///
/// <para>Konsumsi <b>tidak</b> memposting ledger saat direkam; semua posting dilakukan sekali saat
/// close agar sederhana &amp; idempotent.</para>
/// </summary>
public class ProductionService : IProductionService
{
    private readonly BomDbContext _db;
    private readonly ICostingService _costing;

    public ProductionService(BomDbContext db, ICostingService costing)
    {
        _db = db;
        _costing = costing;
    }

    // ---------------------------------------------------------------- Reads

    public async Task<IReadOnlyList<ProductionOrderListDto>> ListAsync(
        ProductionStatus? status, int? productItemId, CancellationToken ct = default)
    {
        var query = _db.ProductionOrders.AsNoTracking();

        if (status is ProductionStatus st)
            query = query.Where(o => o.Status == st);
        if (productItemId is int pid)
            query = query.Where(o => o.ProductItemId == pid);

        return await query
            .OrderByDescending(o => o.Id)
            .Select(o => new ProductionOrderListDto(
                o.Id,
                o.BatchNo,
                o.BomId,
                o.ProductItemId,
                o.ProductItem!.Code,
                o.ProductItem!.Name,
                o.Bom!.OutputUom!.Code,
                o.PlannedQty,
                o.ActualQty,
                o.Status,
                o.StartDate,
                o.EndDate))
            .ToListAsync(ct);
    }

    public async Task<ProductionOrderDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await LoadFullAsync(id, ct);
        return order is null ? null : MapToDto(order);
    }

    public async Task<IReadOnlyList<StockMovementDto>> ListStockMovementsAsync(int? itemId, CancellationToken ct = default)
    {
        var query = _db.StockMovements.AsNoTracking();
        if (itemId is int id)
            query = query.Where(m => m.ItemId == id);

        return await query
            .OrderByDescending(m => m.MovementDate)
            .ThenByDescending(m => m.Id)
            .Select(m => new StockMovementDto(
                m.Id,
                m.ItemId,
                m.Item!.Code,
                m.Item!.Name,
                m.Type,
                m.Qty,
                m.UnitCost,
                m.MovementDate,
                m.RefType,
                m.RefId,
                m.Notes))
            .ToListAsync(ct);
    }

    // -------------------------------------------------------------- Commands

    public async Task<ProductionOrderDto> CreateAsync(UpsertProductionOrderDto dto, CancellationToken ct = default)
    {
        ValidateHeader(dto);

        var bom = await LoadBomForExplodeAsync(dto.BomId, ct);
        if (bom is null)
            throw BomServiceException.NotFound($"BOM {dto.BomId} tidak ditemukan.");
        if (bom.Status != BomStatus.Active)
            throw BomServiceException.BadRequest("BOM harus berstatus Active untuk membuat perintah produksi.");

        var order = new ProductionOrder
        {
            BomId = bom.Id,
            ProductItemId = bom.ProductItemId,
            BatchNo = dto.BatchNo.Trim(),
            PlannedQty = dto.PlannedQty,
            Status = ProductionStatus.Open,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Notes = dto.Notes,
            Consumptions = GenerateConsumptions(bom, dto.PlannedQty)
        };

        _db.ProductionOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(order.Id, ct))!;
    }

    public async Task UpdateAsync(int id, UpsertProductionOrderDto dto, CancellationToken ct = default)
    {
        ValidateHeader(dto);

        var order = await _db.ProductionOrders
            .Include(o => o.Consumptions)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null)
            throw BomServiceException.NotFound($"Perintah produksi {id} tidak ditemukan.");
        if (order.Status != ProductionStatus.Open)
            throw BomServiceException.Conflict("Hanya perintah produksi Open yang dapat diubah.");

        // Regenerate baris konsumsi hanya bila BOM atau rencana kuantitas berubah.
        if (order.BomId != dto.BomId || order.PlannedQty != dto.PlannedQty)
        {
            var bom = await LoadBomForExplodeAsync(dto.BomId, ct);
            if (bom is null)
                throw BomServiceException.NotFound($"BOM {dto.BomId} tidak ditemukan.");
            if (bom.Status != BomStatus.Active)
                throw BomServiceException.BadRequest("BOM harus berstatus Active untuk membuat perintah produksi.");

            order.BomId = bom.Id;
            order.ProductItemId = bom.ProductItemId;
            _db.ProductionConsumptions.RemoveRange(order.Consumptions);
            order.Consumptions = GenerateConsumptions(bom, dto.PlannedQty);
        }

        order.BatchNo = dto.BatchNo.Trim();
        order.PlannedQty = dto.PlannedQty;
        order.StartDate = dto.StartDate;
        order.EndDate = dto.EndDate;
        order.Notes = dto.Notes;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.ProductionOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
            throw BomServiceException.NotFound($"Perintah produksi {id} tidak ditemukan.");
        if (order.Status != ProductionStatus.Open)
            throw BomServiceException.Conflict("Hanya perintah produksi Open yang dapat dihapus.");

        _db.ProductionOrders.Remove(order); // cascade menghapus konsumsi
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ProductionOrderDto> RecordConsumptionAsync(int id, RecordConsumptionDto dto, CancellationToken ct = default)
    {
        var order = await _db.ProductionOrders
            .Include(o => o.Consumptions)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null)
            throw BomServiceException.NotFound($"Perintah produksi {id} tidak ditemukan.");
        if (order.Status != ProductionStatus.Open)
            throw BomServiceException.Conflict("Konsumsi hanya dapat dicatat untuk order Open.");

        foreach (var line in dto.Lines ?? new List<RecordConsumptionLineDto>())
        {
            if (line.ActualQty < 0)
                throw BomServiceException.BadRequest("ActualQty tidak boleh negatif.");

            var consumption = order.Consumptions.FirstOrDefault(c => c.ComponentItemId == line.ComponentItemId);
            if (consumption is null)
                throw BomServiceException.BadRequest($"Komponen {line.ComponentItemId} bukan bagian dari order ini.");

            consumption.ActualQty = line.ActualQty;
        }

        await _db.SaveChangesAsync(ct);
        return (await GetByIdAsync(id, ct))!;
    }

    public async Task<CogsReportDto> CloseAsync(int id, CloseProductionDto dto, CancellationToken ct = default)
    {
        var order = await LoadForCloseAsync(id, ct);
        if (order is null)
            throw BomServiceException.NotFound($"Perintah produksi {id} tidak ditemukan.");
        if (order.Status != ProductionStatus.Open)
            throw BomServiceException.Conflict("Hanya perintah produksi Open yang dapat ditutup.");

        var actualQty = dto.ActualQty;
        if (actualQty <= 0)
            throw BomServiceException.BadRequest("ActualQty harus lebih besar dari 0.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var note = $"Batch {order.BatchNo}";

        // 1) Posting ProductionIssue tiap komponen pada rata-rata bergerak saat ini.
        foreach (var consumption in order.Consumptions)
        {
            if (consumption.ActualQty <= 0)
            {
                consumption.ActualUnitCost = 0m;
                consumption.ActualCost = 0m;
                continue;
            }

            var movement = await ApplyMovementAsync(
                consumption.ComponentItemId, StockMovementType.ProductionIssue,
                consumption.ActualQty, unitCost: 0m, today, "ProductionOrder", order.Id, note, ct);

            consumption.ActualUnitCost = movement.UnitCost;
            consumption.ActualCost = consumption.ActualQty * movement.UnitCost;
        }

        // 2) COGS.
        var materialCost = order.Consumptions.Sum(c => c.ActualCost);
        var conversionPerUnit = ConversionPerUnit(order.Bom!);
        var conversionCost = conversionPerUnit * actualQty;
        var cogsTotal = materialCost + conversionCost;
        var cogsUnit = cogsTotal / actualQty;

        // 3) Posting ProductionReceipt produk jadi pada COGS_unit.
        await ApplyMovementAsync(
            order.ProductItemId, StockMovementType.ProductionReceipt,
            actualQty, cogsUnit, today, "ProductionOrder", order.Id, note, ct);

        // 4) Tutup order.
        order.ActualQty = actualQty;
        order.EndDate = today;
        order.Status = ProductionStatus.Closed;

        await _db.SaveChangesAsync(ct);

        return await BuildCogsReportAsync(order, order.Bom!, ct);
    }

    public async Task<CogsReportDto> GetCogsAsync(int id, CancellationToken ct = default)
    {
        var order = await LoadForCloseAsync(id, ct);
        if (order is null)
            throw BomServiceException.NotFound($"Perintah produksi {id} tidak ditemukan.");
        if (order.Status != ProductionStatus.Closed)
            throw BomServiceException.Conflict("Laporan COGS hanya tersedia untuk order yang sudah ditutup.");

        return await BuildCogsReportAsync(order, order.Bom!, ct);
    }

    // -------------------------------------------------- Moving-average ledger

    /// <summary>
    /// Terapkan satu pergerakan stok ke <see cref="InventoryBalance"/> (upsert) dan tulis satu
    /// baris <see cref="StockMovement"/>. Mengembalikan movement (UnitCost efektif: untuk keluaran =
    /// rata-rata saat ini). <paramref name="qty"/> = magnitudo (kecuali Adjustment yang boleh delta).
    /// </summary>
    private async Task<StockMovement> ApplyMovementAsync(
        int itemId, StockMovementType type, decimal qty, decimal unitCost,
        DateOnly date, string? refType, int? refId, string? notes, CancellationToken ct)
    {
        var balance = await _db.InventoryBalances.FirstOrDefaultAsync(b => b.ItemId == itemId, ct);
        if (balance is null)
        {
            balance = new InventoryBalance { ItemId = itemId, QtyOnHand = 0m, AverageCost = 0m };
            _db.InventoryBalances.Add(balance);
        }

        var recordedUnitCost = unitCost;
        switch (type)
        {
            case StockMovementType.Receipt:
            case StockMovementType.ProductionReceipt:
            {
                var newQty = balance.QtyOnHand + qty;
                if (newQty > 0)
                    balance.AverageCost = (balance.QtyOnHand * balance.AverageCost + qty * unitCost) / newQty;
                balance.QtyOnHand = newQty;
                break;
            }
            case StockMovementType.Issue:
            case StockMovementType.ProductionIssue:
            {
                recordedUnitCost = balance.AverageCost; // keluaran memakai rata-rata saat ini
                balance.QtyOnHand -= qty;               // rata-rata tetap
                break;
            }
            case StockMovementType.Adjustment:
            {
                var newQty = balance.QtyOnHand + qty; // qty boleh negatif (delta)
                if (qty > 0 && newQty > 0)
                    balance.AverageCost = (balance.QtyOnHand * balance.AverageCost + qty * unitCost) / newQty;
                else
                    recordedUnitCost = balance.AverageCost;
                balance.QtyOnHand = newQty;
                break;
            }
        }

        var movement = new StockMovement
        {
            ItemId = itemId,
            Type = type,
            Qty = qty,
            UnitCost = recordedUnitCost,
            MovementDate = date,
            RefType = refType,
            RefId = refId,
            Notes = notes
        };
        _db.StockMovements.Add(movement);
        return movement;
    }

    // -------------------------------------------------------- COGS + variance

    private async Task<CogsReportDto> BuildCogsReportAsync(ProductionOrder order, BomEntity bom, CancellationToken ct)
    {
        var actualQty = order.ActualQty ?? 0m;

        // Biaya standar per 1 satuan keluaran produk (rollup BOM Active produk).
        var standardTotalPerUnit = (await _costing.GetCostBreakdownAsync(order.ProductItemId, ct)).TotalCost;
        var standardTotal = standardTotalPerUnit * actualQty;

        // Peta komponen BOM (untuk std qty allowed). GroupBy menghindari lempar bila komponen ganda.
        var bomByComp = bom.Components
            .GroupBy(c => c.ComponentItemId)
            .ToDictionary(g => g.Key, g => g.First());

        var lines = new List<CogsVarianceLineDto>();
        var standardMaterial = 0m;
        var materialCost = 0m;

        foreach (var consumption in order.Consumptions.OrderBy(c => c.Id))
        {
            var compItem = consumption.ComponentItem!;
            var stdUnit = await _costing.GetUnitCostPerBaseUomAsync(consumption.ComponentItemId, ct);

            var stdQtyAllowed = 0m;
            if (bomByComp.TryGetValue(consumption.ComponentItemId, out var comp))
            {
                var effectiveQty = comp.Quantity * (1 + comp.ScrapPercent);
                var baseQtyPerBatch = UomConverter.Convert(
                    effectiveQty, comp.Uom!, comp.ComponentItem!.BaseUom!, comp.ComponentItem!.Density);
                stdQtyAllowed = bom.OutputQuantity == 0
                    ? 0m
                    : baseQtyPerBatch / bom.OutputQuantity * actualQty;
            }

            var priceVariance = (consumption.ActualUnitCost - stdUnit) * consumption.ActualQty;
            var usageVariance = (consumption.ActualQty - stdQtyAllowed) * stdUnit;

            standardMaterial += stdQtyAllowed * stdUnit;
            materialCost += consumption.ActualCost;

            lines.Add(new CogsVarianceLineDto(
                compItem.Id,
                compItem.Code,
                compItem.Name,
                consumption.Uom!.Code,
                consumption.ActualQty,
                consumption.ActualUnitCost,
                consumption.ActualCost,
                stdQtyAllowed,
                stdUnit,
                priceVariance,
                usageVariance));
        }

        var materialVariance = materialCost - standardMaterial;
        var conversionCost = ConversionPerUnit(bom) * actualQty;
        var cogsTotal = materialCost + conversionCost;
        var cogsUnit = actualQty == 0 ? 0m : cogsTotal / actualQty;
        var totalVariance = cogsTotal - standardTotal;
        var yieldVariance = (order.PlannedQty * bom.YieldPercent - actualQty) * standardTotalPerUnit;

        return new CogsReportDto(
            order.Id,
            actualQty,
            materialCost,
            conversionCost,
            cogsTotal,
            cogsUnit,
            standardTotal,
            totalVariance,
            materialVariance,
            yieldVariance,
            lines);
    }

    // --------------------------------------------------------------- Helpers

    private static void ValidateHeader(UpsertProductionOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BatchNo))
            throw BomServiceException.BadRequest("BatchNo wajib diisi.");
        if (dto.PlannedQty <= 0)
            throw BomServiceException.BadRequest("PlannedQty harus lebih besar dari 0.");
    }

    private static decimal ConversionPerUnit(BomEntity bom)
        => bom.Operations.Sum(o => o.LaborCostPerOutputUnit) + bom.Operations.Sum(o => o.OverheadRate);

    /// <summary>Explode komponen langsung (satu level) BOM ke base UoM komponen, diskala plannedQty.</summary>
    private static List<ProductionConsumption> GenerateConsumptions(BomEntity bom, decimal plannedQty)
    {
        var list = new List<ProductionConsumption>();
        foreach (var comp in bom.Components.OrderBy(c => c.Sequence))
        {
            var compItem = comp.ComponentItem!;
            var effectiveQty = comp.Quantity * (1 + comp.ScrapPercent);
            var baseQtyPerBatch = UomConverter.Convert(effectiveQty, comp.Uom!, compItem.BaseUom!, compItem.Density);
            var planned = bom.OutputQuantity == 0 ? 0m : baseQtyPerBatch * plannedQty / bom.OutputQuantity;

            list.Add(new ProductionConsumption
            {
                ComponentItemId = compItem.Id,
                PlannedQty = planned,
                ActualQty = planned,
                UomId = compItem.BaseUomId,
                ActualUnitCost = 0m,
                ActualCost = 0m
            });
        }
        return list;
    }

    private Task<BomEntity?> LoadBomForExplodeAsync(int bomId, CancellationToken ct)
        => _db.Boms.AsNoTracking()
            .Include(b => b.Components).ThenInclude(c => c.ComponentItem).ThenInclude(i => i!.BaseUom)
            .Include(b => b.Components).ThenInclude(c => c.Uom)
            .FirstOrDefaultAsync(b => b.Id == bomId, ct);

    private Task<ProductionOrder?> LoadFullAsync(int id, CancellationToken ct)
        => _db.ProductionOrders.AsNoTracking()
            .Include(o => o.ProductItem)
            .Include(o => o.Bom).ThenInclude(b => b!.OutputUom)
            .Include(o => o.Consumptions).ThenInclude(c => c.ComponentItem)
            .Include(o => o.Consumptions).ThenInclude(c => c.Uom)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    /// <summary>Muat order + BOM (operasi, output uom, komponen) + konsumsi untuk close/COGS. Tracked.</summary>
    private Task<ProductionOrder?> LoadForCloseAsync(int id, CancellationToken ct)
        => _db.ProductionOrders
            .Include(o => o.Consumptions).ThenInclude(c => c.ComponentItem)
            .Include(o => o.Consumptions).ThenInclude(c => c.Uom)
            .Include(o => o.Bom).ThenInclude(b => b!.Operations)
            .Include(o => o.Bom).ThenInclude(b => b!.OutputUom)
            .Include(o => o.Bom).ThenInclude(b => b!.Components).ThenInclude(c => c.ComponentItem).ThenInclude(i => i!.BaseUom)
            .Include(o => o.Bom).ThenInclude(b => b!.Components).ThenInclude(c => c.Uom)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    private static ProductionOrderDto MapToDto(ProductionOrder order) => new(
        order.Id,
        order.BomId,
        order.Bom!.Version,
        order.ProductItemId,
        order.ProductItem!.Code,
        order.ProductItem!.Name,
        order.Bom!.OutputUom!.Code,
        order.BatchNo,
        order.PlannedQty,
        order.ActualQty,
        order.Status,
        order.StartDate,
        order.EndDate,
        order.Notes,
        order.Consumptions
            .OrderBy(c => c.Id)
            .Select(c => new ProductionConsumptionDto(
                c.Id,
                c.ComponentItemId,
                c.ComponentItem!.Code,
                c.ComponentItem!.Name,
                c.Uom!.Code,
                c.PlannedQty,
                c.ActualQty,
                c.ActualUnitCost,
                c.ActualCost))
            .ToList());
}
