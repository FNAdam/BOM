namespace Bom.Domain.Entities;

/// <summary>
/// Kategori item, mis. Bahan Baku, Kemasan, Produk Jadi.
/// </summary>
public class ItemCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Navigation
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
