namespace Bom.Domain.Entities;

/// <summary>
/// Harga beli item dari supplier tertentu pada satuan tertentu, dengan masa berlaku.
/// </summary>
public class ItemSupplierPrice
{
    public int Id { get; set; }

    public int ItemId { get; set; }

    public int SupplierId { get; set; }

    public int UomId { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "IDR";

    public decimal MinOrderQty { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    /// <summary>Menandai harga/supplier yang diutamakan untuk item ini.</summary>
    public bool IsPreferred { get; set; }

    // Navigation
    public Item? Item { get; set; }
    public Supplier? Supplier { get; set; }
    public Uom? Uom { get; set; }
}
