namespace Bom.Domain.Entities;

/// <summary>
/// Saldo persediaan (stok) per item. Satu baris per item (unik pada <see cref="ItemId"/>).
///
/// Fase 4 (Procurement/MRP): <see cref="QtyOnHand"/> di-set manual untuk netting MRP.
/// Fase 5 (Produksi &amp; COGS) akan mengelola StockMovement + moving-average yang otomatis
/// meng-update <see cref="QtyOnHand"/> dan <see cref="AverageCost"/>.
/// </summary>
public class InventoryBalance
{
    public int Id { get; set; }

    /// <summary>Item yang saldonya dicatat (wajib, unik).</summary>
    public int ItemId { get; set; }

    /// <summary>Kuantitas di tangan dalam base UoM item. Default 0.</summary>
    public decimal QtyOnHand { get; set; }

    /// <summary>Biaya rata-rata bergerak per base UoM (diisi Fase 5). Default 0.</summary>
    public decimal AverageCost { get; set; }

    // Navigation
    public Item? Item { get; set; }
}
