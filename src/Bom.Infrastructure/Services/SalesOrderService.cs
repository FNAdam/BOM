using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bom.Infrastructure.Services;

/// <inheritdoc cref="ISalesOrderService"/>
public class SalesOrderService : ISalesOrderService
{
    private static readonly HashSet<ItemType> SellableTypes = [ItemType.SemiFinished, ItemType.FinishedGood];

    private readonly BomDbContext _db;
    private readonly IInventoryLedgerService _ledger;

    public SalesOrderService(BomDbContext db, IInventoryLedgerService ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    // ---------------------------------------------------------------- Reads

    public async Task<IReadOnlyList<SalesOrderListDto>> ListAsync(
        SalesOrderStatus? status, int? salesChannelId, CancellationToken ct = default)
    {
        var query = _db.SalesOrders.AsNoTracking();

        if (status is SalesOrderStatus st)
            query = query.Where(o => o.Status == st);
        if (salesChannelId is int chId)
            query = query.Where(o => o.SalesChannelId == chId);

        return await query
            .OrderByDescending(o => o.Id)
            .Select(o => new SalesOrderListDto(
                o.Id,
                o.OrderNo,
                o.CustomerName,
                o.SalesChannelId,
                o.SalesChannel != null ? o.SalesChannel.Name : null,
                o.Status,
                o.OrderDate,
                o.Lines.Count,
                o.Lines.Sum(l => l.Qty * l.UnitPrice) * (1 + o.TaxPercent)))
            .ToListAsync(ct);
    }

    public async Task<SalesOrderDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await LoadFullAsync(id, ct);
        return order is null ? null : MapToDto(order);
    }

    public async Task<SalesReceiptDto> GetReceiptAsync(int id, CancellationToken ct = default)
    {
        var order = await LoadFullAsync(id, ct);
        if (order is null)
            throw BomServiceException.NotFound($"Pesanan penjualan {id} tidak ditemukan.");
        if (order.Status != SalesOrderStatus.Completed)
            throw BomServiceException.Conflict("Struk hanya tersedia untuk pesanan yang sudah Completed.");

        var lines = MapLines(order.Lines);
        var subtotal = lines.Sum(l => l.LineTotal);
        var taxAmount = subtotal * order.TaxPercent;

        return new SalesReceiptDto(
            order.Id, order.OrderNo, order.CustomerName, order.SalesChannel?.Name, order.OrderDate,
            subtotal, order.TaxPercent, taxAmount, subtotal + taxAmount, lines);
    }

    // -------------------------------------------------------------- Commands

    public async Task<SalesOrderDto> CreateAsync(UpsertSalesOrderDto dto, CancellationToken ct = default)
    {
        await ValidateLinesAsync(dto, ct);

        var order = new SalesOrder
        {
            SalesChannelId = dto.SalesChannelId,
            OrderNo = dto.OrderNo.Trim(),
            CustomerName = string.IsNullOrWhiteSpace(dto.CustomerName) ? null : dto.CustomerName.Trim(),
            Status = SalesOrderStatus.Draft,
            OrderDate = dto.OrderDate,
            TaxPercent = dto.TaxPercent,
            Notes = dto.Notes,
            Lines = dto.Lines.Select(ToLineEntity).ToList()
        };

        _db.SalesOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(order.Id, ct))!;
    }

    public async Task UpdateAsync(int id, UpsertSalesOrderDto dto, CancellationToken ct = default)
    {
        await ValidateLinesAsync(dto, ct);

        var order = await _db.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null)
            throw BomServiceException.NotFound($"Pesanan penjualan {id} tidak ditemukan.");
        if (order.Status != SalesOrderStatus.Draft)
            throw BomServiceException.Conflict("Hanya pesanan penjualan berstatus Draft yang dapat diubah.");

        order.OrderNo = dto.OrderNo.Trim();
        order.SalesChannelId = dto.SalesChannelId;
        order.CustomerName = string.IsNullOrWhiteSpace(dto.CustomerName) ? null : dto.CustomerName.Trim();
        order.OrderDate = dto.OrderDate;
        order.TaxPercent = dto.TaxPercent;
        order.Notes = dto.Notes;

        _db.SalesOrderLines.RemoveRange(order.Lines);
        order.Lines = dto.Lines.Select(ToLineEntity).ToList();

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
            throw BomServiceException.NotFound($"Pesanan penjualan {id} tidak ditemukan.");
        if (order.Status != SalesOrderStatus.Draft)
            throw BomServiceException.Conflict("Hanya pesanan penjualan berstatus Draft yang dapat dihapus.");

        _db.SalesOrders.Remove(order); // cascade menghapus baris
        await _db.SaveChangesAsync(ct);
    }

    public async Task<SalesOrderDto> CompleteAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.SalesOrders
            .Include(o => o.Lines).ThenInclude(l => l.PackLevel)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null)
            throw BomServiceException.NotFound($"Pesanan penjualan {id} tidak ditemukan.");
        if (order.Status != SalesOrderStatus.Draft)
            throw BomServiceException.Conflict("Hanya pesanan penjualan berstatus Draft yang dapat diselesaikan.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var note = $"SO {order.OrderNo}";

        foreach (var line in order.Lines)
        {
            // PackLevel.QtyPerBase selalu dalam satuan dasar produk yang sama (bukan lintas
            // dimensi) - beda dari PurchaseOrderLine yang butuh UomConverter.
            var qtyBase = line.PackLevelId is null ? line.Qty : line.Qty * line.PackLevel!.QtyPerBase;

            await _ledger.ApplyMovementAsync(
                line.ProductItemId, StockMovementType.Sale, qtyBase, unitCost: 0m,
                today, "SalesOrder", order.Id, note, ct);
        }

        order.Status = SalesOrderStatus.Completed;
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(id, ct))!;
    }

    public async Task<SalesOrderDto> CancelAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
            throw BomServiceException.NotFound($"Pesanan penjualan {id} tidak ditemukan.");
        if (order.Status != SalesOrderStatus.Draft)
            throw BomServiceException.Conflict("Hanya pesanan penjualan berstatus Draft yang dapat dibatalkan.");

        order.Status = SalesOrderStatus.Cancelled;
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(id, ct))!;
    }

    // --------------------------------------------------------------- Helpers

    private async Task ValidateLinesAsync(UpsertSalesOrderDto dto, CancellationToken ct)
    {
        if (dto.SalesChannelId is int chId)
        {
            var channelExists = await _db.SalesChannels.AnyAsync(c => c.Id == chId, ct);
            if (!channelExists)
                throw BomServiceException.NotFound($"Kanal penjualan {chId} tidak ditemukan.");
        }

        var productIds = dto.Lines.Select(l => l.ProductItemId).Distinct().ToList();
        var products = await _db.Items.AsNoTracking()
            .Where(i => productIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);
        if (products.Count != productIds.Count)
            throw BomServiceException.BadRequest("Satu atau lebih produk pada baris tidak ditemukan.");

        foreach (var product in products.Values)
        {
            if (!SellableTypes.Contains(product.Type))
                throw BomServiceException.BadRequest(
                    $"Item '{product.Code}' bukan produk yang bisa dijual (harus Setengah Jadi/Produk Jadi).");
        }

        var packLevelIds = dto.Lines.Where(l => l.PackLevelId.HasValue)
            .Select(l => l.PackLevelId!.Value).Distinct().ToList();
        if (packLevelIds.Count == 0) return;

        var packLevels = await _db.PackLevels.AsNoTracking()
            .Where(p => packLevelIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
        if (packLevels.Count != packLevelIds.Count)
            throw BomServiceException.BadRequest("Satu atau lebih kemasan pada baris tidak ditemukan.");

        foreach (var line in dto.Lines.Where(l => l.PackLevelId.HasValue))
        {
            var packLevel = packLevels[line.PackLevelId!.Value];
            if (packLevel.FinishedItemId != line.ProductItemId)
                throw BomServiceException.BadRequest(
                    $"Kemasan '{packLevel.Name}' bukan milik produk '{products[line.ProductItemId].Code}'.");
        }
    }

    private static SalesOrderLine ToLineEntity(UpsertSalesOrderLineDto l) => new()
    {
        ProductItemId = l.ProductItemId,
        PackLevelId = l.PackLevelId,
        Qty = l.Qty,
        UnitPrice = l.UnitPrice
    };

    private Task<SalesOrder?> LoadFullAsync(int id, CancellationToken ct)
        => _db.SalesOrders.AsNoTracking()
            .Include(o => o.SalesChannel)
            .Include(o => o.Lines).ThenInclude(l => l.ProductItem).ThenInclude(i => i!.BaseUom)
            .Include(o => o.Lines).ThenInclude(l => l.PackLevel)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    private static List<SalesOrderLineDto> MapLines(IEnumerable<SalesOrderLine> lines) => lines
        .OrderBy(l => l.Id)
        .Select(l => new SalesOrderLineDto(
            l.Id,
            l.ProductItemId,
            l.ProductItem!.Code,
            l.ProductItem!.Name,
            l.PackLevelId,
            l.PackLevel?.Name,
            l.ProductItem!.BaseUom!.Code,
            l.Qty,
            l.UnitPrice,
            l.Qty * l.UnitPrice))
        .ToList();

    private static SalesOrderDto MapToDto(SalesOrder order) => new(
        order.Id,
        order.OrderNo,
        order.SalesChannelId,
        order.SalesChannel?.Name,
        order.CustomerName,
        order.Status,
        order.OrderDate,
        order.TaxPercent,
        order.Notes,
        MapLines(order.Lines));
}
