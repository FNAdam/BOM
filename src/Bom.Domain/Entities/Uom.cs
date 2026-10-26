using Bom.Domain.Enums;

namespace Bom.Domain.Entities;

/// <summary>
/// Satuan ukur (unit of measure), mis. kg, g, L, ml, pcs.
/// </summary>
public class Uom
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public UomType Type { get; set; }

    /// <summary>Pengali menuju satuan dasar dimensi (mis. g -> kg = 0.001).</summary>
    public decimal FactorToBase { get; set; } = 1m;

    /// <summary>Menandai satuan dasar untuk dimensi terkait.</summary>
    public bool IsBase { get; set; }
}
