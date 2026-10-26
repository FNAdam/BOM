namespace Bom.Domain.Entities;

/// <summary>
/// Satu baris konsumsi komponen pada sebuah <see cref="ProductionOrder"/>.
/// Kuantitas (planned &amp; actual) dinyatakan dalam <b>base UoM</b> komponen.
/// Biaya aktual (<see cref="ActualUnitCost"/> / <see cref="ActualCost"/>) diisi saat order ditutup
/// dari biaya rata-rata bergerak komponen saat itu.
/// </summary>
public class ProductionConsumption
{
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }

    /// <summary>Item komponen yang dikonsumsi.</summary>
    public int ComponentItemId { get; set; }

    /// <summary>Rencana konsumsi dalam base UoM komponen (dari explode BOM satu level).</summary>
    public decimal PlannedQty { get; set; }

    /// <summary>Konsumsi aktual dalam base UoM komponen (dapat diedit saat Open). Default 0.</summary>
    public decimal ActualQty { get; set; }

    /// <summary>Base UoM komponen.</summary>
    public int UomId { get; set; }

    /// <summary>Biaya rata-rata bergerak per base UoM komponen saat close. Default 0.</summary>
    public decimal ActualUnitCost { get; set; }

    /// <summary>ActualQty * ActualUnitCost. Default 0.</summary>
    public decimal ActualCost { get; set; }

    // Navigation
    public ProductionOrder? ProductionOrder { get; set; }
    public Item? ComponentItem { get; set; }
    public Uom? Uom { get; set; }
}
