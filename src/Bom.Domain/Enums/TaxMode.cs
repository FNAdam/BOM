namespace Bom.Domain.Enums;

/// <summary>
/// Perlakuan PPN terhadap harga jenjang.
/// Exclusive = harga jenjang bersifat tanpa pajak (net), PPN ditambahkan di atasnya;
/// Inclusive = harga jenjang sudah termasuk pajak (gross).
/// </summary>
public enum TaxMode
{
    Exclusive,
    Inclusive
}
