namespace Bom.Domain.Entities;

/// <summary>
/// Konversi eksplisit antar satuan ukur (from -> to) dengan faktor pengali.
/// </summary>
public class UomConversion
{
    public int Id { get; set; }

    public int FromUomId { get; set; }

    public int ToUomId { get; set; }

    /// <summary>Nilai FromUom dikalikan Factor untuk memperoleh nilai ToUom.</summary>
    public decimal Factor { get; set; }

    // Navigation
    public Uom? FromUom { get; set; }
    public Uom? ToUom { get; set; }
}
