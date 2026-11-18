namespace Bom.Domain.Entities;

/// <summary>
/// Satu baris item pada <see cref="PurchaseOrder"/>. <see cref="QtyReceived"/> terakumulasi tiap
/// kali barang diterima (boleh bertahap/parsial) dan tidak boleh melebihi <see cref="QtyOrdered"/>.
/// </summary>
public class PurchaseOrderLine
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }

    public int ItemId { get; set; }

    public int UomId { get; set; }

    public decimal QtyOrdered { get; set; }

    /// <summary>Harga sepakat per satuan (UomId), dipakai sebagai UnitCost saat penerimaan diposting.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Akumulasi qty yang sudah diterima. Default 0.</summary>
    public decimal QtyReceived { get; set; }

    // Navigation
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Item? Item { get; set; }
    public Uom? Uom { get; set; }
}
