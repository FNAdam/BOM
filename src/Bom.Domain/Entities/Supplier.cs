namespace Bom.Domain.Entities;

/// <summary>
/// Pemasok (supplier) bahan baku / kemasan.
/// </summary>
public class Supplier
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int? LeadTimeDays { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<ItemSupplierPrice> Prices { get; set; } = new List<ItemSupplierPrice>();
}
