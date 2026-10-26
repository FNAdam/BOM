using Bom.Domain.Enums;

namespace Bom.Domain.Entities;

/// <summary>
/// Item katalog: bahan baku, kemasan, semi-finished, atau produk jadi.
/// </summary>
public class Item
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ItemType Type { get; set; }

    /// <summary>Satuan dasar untuk item ini (wajib).</summary>
    public int BaseUomId { get; set; }

    public int? CategoryId { get; set; }

    public decimal StandardMaterialCost { get; set; }

    public CostingMethod CostingMethod { get; set; } = CostingMethod.Standard;

    /// <summary>Densitas (mis. kg/L) untuk konversi Weight &lt;-&gt; Volume; opsional.</summary>
    public decimal? Density { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public Uom? BaseUom { get; set; }
    public ItemCategory? Category { get; set; }
    public ICollection<ItemSupplierPrice> SupplierPrices { get; set; } = new List<ItemSupplierPrice>();
}
