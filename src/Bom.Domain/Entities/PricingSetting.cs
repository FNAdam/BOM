using Bom.Domain.Enums;

namespace Bom.Domain.Entities;

/// <summary>
/// Setelan penetapan harga global (singleton, Id = 1). Menyediakan default target margin, PPN,
/// margin minimum (untuk floor guard), dan aturan pembulatan yang dipakai bila permintaan
/// simulasi tidak menyertakan nilai override.
/// </summary>
public class PricingSetting
{
    public int Id { get; set; } = 1;

    /// <summary>Target margin default dalam rentang [0,1) (mis. 0,45 = 45%).</summary>
    public decimal DefaultTargetMarginPercent { get; set; } = 0.45m;

    /// <summary>Tarif PPN default dalam rentang [0,1) (mis. 0,11 = 11%).</summary>
    public decimal DefaultTaxPercent { get; set; } = 0.11m;

    /// <summary>Margin minimum yang diperbolehkan; di bawah ini ditandai FloorViolation.</summary>
    public decimal MinMarginPercent { get; set; }

    public RoundingRule RoundingRule { get; set; } = RoundingRule.None;
}
