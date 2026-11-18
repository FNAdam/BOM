using System.Text.RegularExpressions;
using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bom.Infrastructure.Services;

/// <inheritdoc cref="IPurchaseOrderService"/>
public class PurchaseOrderService : IPurchaseOrderService
{
    private const string OrderNoPrefix = "PO";

    private readonly BomDbContext _db;
    private readonly IInventoryLedgerService _ledger;

    public PurchaseOrderService(BomDbContext db, IInventoryLedgerService ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    /// <summary>
    /// Sama seperti saran kode Item (lihat ItemsController.GetNextCode): cuma nomor yang persis
    /// cocok pola "PO-0000" yang dihitung; nomor lama/manual di luar pola diabaikan. Tidak dikunci
    /// lintas-request - cuma saran awal, keunikan tetap divalidasi saat Create (409 bila tabrakan).
    /// </summary>
    public async Task<string> GetNextOrderNoAsync(CancellationToken ct = default)
    {
        var candidates = await _db.PurchaseOrders
            .Where(o => o.OrderNo.StartsWith(OrderNoPrefix + "-"))
            .Select(o => o.OrderNo)
            .ToListAsync(ct);

        var pattern = new Regex($"^{Regex.Escape(OrderNoPrefix)}-(\\d+)$");
        var maxNumber = 0;
        foreach (var no in candidates)
        {
            var match = pattern.Match(no);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var n) && n > maxNumber)
                maxNumber = n;
        }

        return $"{OrderNoPrefix}-{(maxNumber + 1).ToString().PadLeft(4, '0')}";
    }

    // ---------------------------------------------------------------- Reads

    public async Task<IReadOnlyList<PurchaseOrderListDto>> ListAsync(
        PurchaseOrderStatus? status, int? supplierId, CancellationToken ct = default)
    {
        var query = _db.PurchaseOrders.AsNoTracking();

        if (status is PurchaseOrderStatus st)
            query = query.Where(o => o.Status == st);
        if (supplierId is int sid)
            query = query.Where(o => o.SupplierId == sid);

        return await query
            .OrderByDescending(o => o.Id)
            .Select(o => new PurchaseOrderListDto(
                o.Id,
                o.OrderNo,
                o.SupplierId,
                o.Supplier!.Code,
                o.Supplier!.Name,
                o.Status,
                o.OrderDate,
                o.Lines.Count,
                o.Lines.Sum(l => l.QtyOrdered * l.UnitPrice)))
            .ToListAsync(ct);
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await LoadFullAsync(id, ct);
        return order is null ? null : MapToDto(order);
    }

    // -------------------------------------------------------------- Commands

    public async Task<PurchaseOrderDto> CreateAsync(UpsertPurchaseOrderDto dto, CancellationToken ct = default)
    {
        await ValidateSupplierAndLinesAsync(dto, ct);

        var order = new PurchaseOrder
        {
            SupplierId = dto.SupplierId,
            OrderNo = dto.OrderNo.Trim(),
            Status = PurchaseOrderStatus.Draft,
            OrderDate = dto.OrderDate,
            Notes = dto.Notes,
            Lines = dto.Lines.Select(ToLineEntity).ToList()
        };

        _db.PurchaseOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(order.Id, ct))!;
    }

    public async Task UpdateAsync(int id, UpsertPurchaseOrderDto dto, CancellationToken ct = default)
    {
        await ValidateSupplierAndLinesAsync(dto, ct);

        var order = await _db.PurchaseOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null)
            throw BomServiceException.NotFound($"Pesanan pembelian {id} tidak ditemukan.");
        if (order.Status != PurchaseOrderStatus.Draft)
            throw BomServiceException.Conflict("Hanya pesanan pembelian berstatus Draft yang dapat diubah.");

        order.SupplierId = dto.SupplierId;
        order.OrderNo = dto.OrderNo.Trim();
        order.OrderDate = dto.OrderDate;
        order.Notes = dto.Notes;

        _db.PurchaseOrderLines.RemoveRange(order.Lines);
        order.Lines = dto.Lines.Select(ToLineEntity).ToList();

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
            throw BomServiceException.NotFound($"Pesanan pembelian {id} tidak ditemukan.");
        if (order.Status != PurchaseOrderStatus.Draft)
            throw BomServiceException.Conflict("Hanya pesanan pembelian berstatus Draft yang dapat dihapus.");

        _db.PurchaseOrders.Remove(order); // cascade menghapus baris
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PurchaseOrderDto> SendAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
            throw BomServiceException.NotFound($"Pesanan pembelian {id} tidak ditemukan.");
        if (order.Status != PurchaseOrderStatus.Draft)
            throw BomServiceException.Conflict("Hanya pesanan pembelian berstatus Draft yang dapat dikirim.");

        order.Status = PurchaseOrderStatus.Sent;
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(id, ct))!;
    }

    public async Task<PurchaseOrderDto> ReceiveAsync(int id, ReceivePurchaseOrderDto dto, CancellationToken ct = default)
    {
        var order = await _db.PurchaseOrders
            .Include(o => o.Lines).ThenInclude(l => l.Item).ThenInclude(i => i!.BaseUom)
            .Include(o => o.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null)
            throw BomServiceException.NotFound($"Pesanan pembelian {id} tidak ditemukan.");
        if (order.Status != PurchaseOrderStatus.Sent && order.Status != PurchaseOrderStatus.PartiallyReceived)
            throw BomServiceException.Conflict("Penerimaan barang hanya untuk PO berstatus Sent atau PartiallyReceived.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var note = $"PO {order.OrderNo}";

        foreach (var receiveLine in dto.Lines)
        {
            var line = order.Lines.FirstOrDefault(l => l.Id == receiveLine.LineId);
            if (line is null)
                throw BomServiceException.BadRequest($"Baris {receiveLine.LineId} bukan bagian dari PO ini.");

            var remaining = line.QtyOrdered - line.QtyReceived;
            if (receiveLine.QtyReceiving > remaining)
                throw BomServiceException.BadRequest(
                    $"Qty diterima untuk item {line.ItemId} ({receiveLine.QtyReceiving}) melebihi sisa yang dipesan ({remaining}).");

            // InventoryBalance/StockMovement selalu dalam base UoM item - baris PO boleh pakai UoM
            // lain (mis. beli dalam gram untuk item berbasis kg), jadi dikonversi dulu di sini.
            var item = line.Item!;
            decimal qtyBase;
            decimal unitCostBase;
            try
            {
                qtyBase = UomConverter.Convert(receiveLine.QtyReceiving, line.Uom!, item.BaseUom!, item.Density);
                var totalCost = line.UnitPrice * receiveLine.QtyReceiving;
                unitCostBase = qtyBase == 0 ? 0m : totalCost / qtyBase;
            }
            catch (InvalidOperationException ex)
            {
                throw BomServiceException.BadRequest(
                    $"Satuan baris item {item.Code} tidak bisa dikonversi ke satuan dasarnya: {ex.Message}");
            }

            await _ledger.ApplyMovementAsync(
                item.Id, StockMovementType.Receipt, qtyBase, unitCostBase,
                today, "PurchaseOrder", order.Id, note, ct);

            line.QtyReceived += receiveLine.QtyReceiving;
        }

        order.Status = order.Lines.All(l => l.QtyReceived >= l.QtyOrdered)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;

        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(id, ct))!;
    }

    public async Task<PurchaseOrderDto> CancelAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
            throw BomServiceException.NotFound($"Pesanan pembelian {id} tidak ditemukan.");
        if (order.Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
            throw BomServiceException.Conflict("PO yang sudah Received/Cancelled tidak dapat dibatalkan lagi.");

        order.Status = PurchaseOrderStatus.Cancelled;
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(id, ct))!;
    }

    // --------------------------------------------------------------- Helpers

    private async Task ValidateSupplierAndLinesAsync(UpsertPurchaseOrderDto dto, CancellationToken ct)
    {
        var supplierExists = await _db.Suppliers.AnyAsync(s => s.Id == dto.SupplierId, ct);
        if (!supplierExists)
            throw BomServiceException.NotFound($"Supplier {dto.SupplierId} tidak ditemukan.");

        var itemIds = dto.Lines.Select(l => l.ItemId).Distinct().ToList();
        var uomIds = dto.Lines.Select(l => l.UomId).Distinct().ToList();

        var items = await _db.Items.AsNoTracking()
            .Include(i => i.BaseUom)
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);
        if (items.Count != itemIds.Count)
            throw BomServiceException.BadRequest("Satu atau lebih Item pada baris PO tidak ditemukan.");

        var uoms = await _db.Uoms.AsNoTracking()
            .Where(u => uomIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);
        if (uoms.Count != uomIds.Count)
            throw BomServiceException.BadRequest("Satu atau lebih Satuan pada baris PO tidak ditemukan.");

        // Konversi dicek di awal (bukan nunggu gagal saat Terima Barang) supaya PO yang sudah
        // terlanjur Dikirim tidak macet karena baris yang satuannya tidak bisa dikonversi.
        foreach (var line in dto.Lines)
        {
            var item = items[line.ItemId];
            var uom = uoms[line.UomId];
            try
            {
                UomConverter.Convert(1m, uom, item.BaseUom!, item.Density);
            }
            catch (InvalidOperationException ex)
            {
                throw BomServiceException.BadRequest(
                    $"Satuan '{uom.Code}' pada baris item {item.Code} tidak bisa dikonversi ke satuan dasarnya: {ex.Message}");
            }
        }
    }

    private static PurchaseOrderLine ToLineEntity(UpsertPurchaseOrderLineDto l) => new()
    {
        ItemId = l.ItemId,
        UomId = l.UomId,
        QtyOrdered = l.QtyOrdered,
        UnitPrice = l.UnitPrice,
        QtyReceived = 0m
    };

    private Task<PurchaseOrder?> LoadFullAsync(int id, CancellationToken ct)
        => _db.PurchaseOrders.AsNoTracking()
            .Include(o => o.Supplier)
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .Include(o => o.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    private static PurchaseOrderDto MapToDto(PurchaseOrder order) => new(
        order.Id,
        order.SupplierId,
        order.Supplier!.Code,
        order.Supplier!.Name,
        order.OrderNo,
        order.Status,
        order.OrderDate,
        order.Notes,
        order.Lines
            .OrderBy(l => l.Id)
            .Select(l => new PurchaseOrderLineDto(
                l.Id,
                l.ItemId,
                l.Item!.Code,
                l.Item!.Name,
                l.UomId,
                l.Uom!.Code,
                l.QtyOrdered,
                l.UnitPrice,
                l.QtyReceived))
            .ToList());
}
