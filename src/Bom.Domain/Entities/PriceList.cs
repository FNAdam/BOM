using Bom.Domain.Enums;

namespace Bom.Domain.Entities;

/// <summary>
/// Daftar harga (price list) untuk satu produk jadi. Menyimpan parameter penetapan harga
/// (metode, dasar biaya, target, PPN, floor) beserta baris hasilnya (<see cref="Lines"/>).
/// Hanya satu price list yang boleh berstatus Active per produk (partial unique index).
/// </summary>
public class PriceList
{
    public int Id { get; set; }

    /// <summary>Produk (SemiFinished / FinishedGood) yang diberi harga.</summary>
    public int ProductItemId { get; set; }

    public PricingMethod PricingMethod { get; set; } = PricingMethod.Margin;

    public CostBasis CostBasis { get; set; } = CostBasis.Standard;

    /// <summary>Target margin/markup dalam rentang [0,1).</summary>
    public decimal TargetPercent { get; set; }

    /// <summary>Harga dasar manual (dipakai bila PricingMethod = Manual).</summary>
    public decimal? ManualBasePrice { get; set; }

    /// <summary>Batas bawah harga dasar (floor); di bawah ini ditandai FloorViolation.</summary>
    public decimal? FloorCost { get; set; }

    public TaxMode TaxMode { get; set; } = TaxMode.Exclusive;

    /// <summary>Tarif PPN dalam rentang [0,1).</summary>
    public decimal TaxPercent { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public PriceListStatus Status { get; set; } = PriceListStatus.Draft;

    public string? Notes { get; set; }

    // Navigation
    public Item? ProductItem { get; set; }
    public ICollection<PriceListLine> Lines { get; set; } = new List<PriceListLine>();
}
