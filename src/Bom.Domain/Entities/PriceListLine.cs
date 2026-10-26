namespace Bom.Domain.Entities;

/// <summary>
/// Satu sel matriks harga: kombinasi jenjang (Ex-factory / kanal / HET) dan jenjang kemasan
/// (PackLevel). Menyimpan harga net, PPN, dan gross hasil simulasi beserta kontribusi marginnya.
/// </summary>
public class PriceListLine
{
    public int Id { get; set; }

    public int PriceListId { get; set; }

    /// <summary>Kanal penjualan; null untuk baris Ex-factory dan HET.</summary>
    public int? SalesChannelId { get; set; }

    public int PackLevelId { get; set; }

    /// <summary>Nama jenjang (mis. "Ex-factory", "Distributor", "Retail", "HET"); ≤ 40.</summary>
    public string TierName { get; set; } = string.Empty;

    /// <summary>Margin/PPN yang diterapkan untuk mencapai jenjang ini.</summary>
    public decimal MarginPercent { get; set; }

    /// <summary>Harga tanpa pajak (net) per kemasan.</summary>
    public decimal NetPrice { get; set; }

    /// <summary>Nilai PPN per kemasan.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Harga termasuk pajak (gross) per kemasan.</summary>
    public decimal GrossPrice { get; set; }

    /// <summary>Kontribusi margin per kemasan (NetPrice - biaya untuk kemasan tsb).</summary>
    public decimal ContributionMargin { get; set; }

    // Navigation
    public PriceList? PriceList { get; set; }
    public SalesChannel? SalesChannel { get; set; }
    public PackLevel? PackLevel { get; set; }
}
