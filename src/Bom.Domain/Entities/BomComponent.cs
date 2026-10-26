namespace Bom.Domain.Entities;

/// <summary>
/// Satu baris komponen (bahan) yang dipakai sebuah BOM.
/// </summary>
public class BomComponent
{
    public int Id { get; set; }

    public int BomId { get; set; }

    /// <summary>Item komponen yang dikonsumsi.</summary>
    public int ComponentItemId { get; set; }

    /// <summary>Jumlah komponen per satu batch keluaran BOM induk.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Satuan ukur komponen (harus sedimensi dengan base UoM item komponen).</summary>
    public int UomId { get; set; }

    /// <summary>Persentase susut/scrap (>= 0). Kuantitas efektif = Quantity * (1 + ScrapPercent).</summary>
    public decimal ScrapPercent { get; set; }

    public int Sequence { get; set; }

    public bool IsOptional { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public Bom? Bom { get; set; }
    public Item? ComponentItem { get; set; }
    public Uom? Uom { get; set; }
}
