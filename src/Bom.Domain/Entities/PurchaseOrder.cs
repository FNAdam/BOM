using Bom.Domain.Enums;

namespace Bom.Domain.Entities;

/// <summary>
/// Pesanan pembelian (purchase order) ke satu supplier. Penerimaan barang per baris memposting
/// <see cref="StockMovement"/> (Type=Receipt) lewat buku besar stok (lihat Application.IInventoryLedgerService),
/// yang juga meng-update <see cref="InventoryBalance"/> (rata-rata bergerak).
/// </summary>
public class PurchaseOrder
{
    public int Id { get; set; }

    public int SupplierId { get; set; }

    /// <summary>Nomor PO, diisi manual (bebas format, mis. "PO-2026-11-001").</summary>
    public string OrderNo { get; set; } = string.Empty;

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public DateOnly OrderDate { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public Supplier? Supplier { get; set; }
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
