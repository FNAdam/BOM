namespace Bom.Domain.Entities;

/// <summary>
/// Foto (snapshot) biaya standar sebuah item pada tanggal tertentu, hasil cost roll-up.
/// Menyimpan rincian material / labor / overhead / total per satu satuan keluaran (OutputUom).
/// </summary>
public class CostSnapshot
{
    public int Id { get; set; }

    /// <summary>Item yang dihitung biayanya.</summary>
    public int ItemId { get; set; }

    /// <summary>Tanggal berlaku snapshot (biasanya hari perhitungan).</summary>
    public DateOnly AsOfDate { get; set; }

    public decimal MaterialCost { get; set; }

    public decimal LaborCost { get; set; }

    public decimal OverheadCost { get; set; }

    public decimal TotalCost { get; set; }

    /// <summary>Satuan acuan hasil (per 1 OutputUom untuk produk ber-BOM, atau base UoM untuk item beli).</summary>
    public int OutputUomId { get; set; }

    /// <summary>BOM sumber perhitungan (null untuk item yang dibeli / tanpa BOM Active).</summary>
    public int? SourceBomId { get; set; }

    /// <summary>Versi BOM sumber saat snapshot dibuat (null untuk item beli).</summary>
    public int? BomVersion { get; set; }

    // Navigation
    public Item? Item { get; set; }
    public Uom? OutputUom { get; set; }
    public Bom? SourceBom { get; set; }
}
