namespace Bom.Domain.Entities;

/// <summary>
/// Kanal penjualan (mis. Distributor, Retail) sebagai satu jenjang harga di atas harga dasar
/// (ex-factory). <see cref="DefaultMarginPercent"/> adalah margin default yang dikalikan pada harga
/// jenjang sebelumnya. Ex-factory sendiri bukan kanal (harga dasar); HET = jenjang terakhir + PPN.
/// </summary>
public class SalesChannel
{
    public int Id { get; set; }

    /// <summary>Kode unik kanal (≤ 20).</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Margin default kanal dalam rentang [0,1) (mis. 0,12 = 12%).</summary>
    public decimal DefaultMarginPercent { get; set; }

    /// <summary>Urutan jenjang (kecil lebih dulu).</summary>
    public int Sequence { get; set; }

    public bool IsActive { get; set; } = true;
}
