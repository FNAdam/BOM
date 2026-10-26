namespace Bom.Domain.Enums;

/// <summary>
/// Metode penetapan harga dasar (ex-factory) sebuah produk.
/// Margin = harga dari margin atas harga jual (cost / (1 - target));
/// Markup = harga dari markup atas biaya (cost * (1 + target));
/// Manual = harga dasar ditetapkan manual.
/// </summary>
public enum PricingMethod
{
    Margin,
    Markup,
    Manual
}
