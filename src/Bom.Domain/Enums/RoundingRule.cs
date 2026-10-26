namespace Bom.Domain.Enums;

/// <summary>
/// Aturan pembulatan harga per jenjang.
/// None = bulatkan ke rupiah utuh; Nearest100 / Nearest500 = ke kelipatan terdekat;
/// Psych990 = pembulatan psikologis berakhiran 990.
/// </summary>
public enum RoundingRule
{
    None,
    Nearest100,
    Nearest500,
    Psych990
}
