namespace Bom.Domain.Enums;

/// <summary>
/// Status siklus hidup sebuah BOM (resep).
/// Draft = masih bisa diedit; Active = versi berlaku (maks. 1 per produk);
/// Obsolete = versi lama yang sudah digantikan.
/// </summary>
public enum BomStatus
{
    Draft,
    Active,
    Obsolete
}
