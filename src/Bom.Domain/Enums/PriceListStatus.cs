namespace Bom.Domain.Enums;

/// <summary>
/// Status siklus hidup sebuah daftar harga (price list).
/// Draft = masih bisa diedit; Active = versi berlaku (maks. 1 per produk);
/// Obsolete = versi lama yang sudah digantikan.
/// </summary>
public enum PriceListStatus
{
    Draft,
    Active,
    Obsolete
}
