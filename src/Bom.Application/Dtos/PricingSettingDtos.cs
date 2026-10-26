using Bom.Domain.Enums;

namespace Bom.Application.Dtos;

/// <summary>Setelan penetapan harga global (singleton).</summary>
public record PricingSettingDto(
    decimal DefaultTargetMarginPercent,
    decimal DefaultTaxPercent,
    decimal MinMarginPercent,
    RoundingRule RoundingRule);

/// <summary>Payload update setelan penetapan harga (singleton).</summary>
public record UpsertPricingSettingDto(
    decimal DefaultTargetMarginPercent,
    decimal DefaultTaxPercent,
    decimal MinMarginPercent,
    RoundingRule RoundingRule);
