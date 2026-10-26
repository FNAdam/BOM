namespace Bom.Domain.Entities;

/// <summary>
/// Langkah operasi/konversi pada sebuah BOM beserta biaya tenaga kerja dan overhead.
/// Dipakai penuh untuk perhitungan biaya di Fase 3.
/// </summary>
public class BomOperation
{
    public int Id { get; set; }

    public int BomId { get; set; }

    public int Sequence { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Biaya tenaga kerja per satu satuan keluaran BOM.</summary>
    public decimal LaborCostPerOutputUnit { get; set; }

    /// <summary>Tarif overhead per satu satuan keluaran BOM.</summary>
    public decimal OverheadRate { get; set; }

    // Navigation
    public Bom? Bom { get; set; }
}
