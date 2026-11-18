namespace Bom.Domain.Entities;

/// <summary>
/// Satu baris produk pada <see cref="SalesOrder"/>. <see cref="Qty"/> &amp; <see cref="UnitPrice"/>
/// dalam satuan <see cref="PackLevel"/> yang dipilih (mis. per "Pack"); kalau <see cref="PackLevelId"/>
/// kosong, keduanya dalam satuan dasar produk. Konversi ke satuan dasar untuk posting stok cukup
/// qty * PackLevel.QtyPerBase (PackLevel selalu dalam dimensi satuan dasar produk, tidak perlu
/// UomConverter lintas dimensi seperti pada PurchaseOrderLine).
/// </summary>
public class SalesOrderLine
{
    public int Id { get; set; }

    public int SalesOrderId { get; set; }

    /// <summary>Produk yang dijual (harus SemiFinished/FinishedGood).</summary>
    public int ProductItemId { get; set; }

    /// <summary>Jenjang kemasan (opsional - kosong berarti satuan dasar produk).</summary>
    public int? PackLevelId { get; set; }

    public decimal Qty { get; set; }

    /// <summary>Harga per satuan (per PackLevel kalau diisi, per satuan dasar kalau tidak).</summary>
    public decimal UnitPrice { get; set; }

    // Navigation
    public SalesOrder? SalesOrder { get; set; }
    public Item? ProductItem { get; set; }
    public PackLevel? PackLevel { get; set; }
}
