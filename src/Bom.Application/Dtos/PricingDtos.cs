using Bom.Domain.Enums;

namespace Bom.Application.Dtos;

/// <summary>Override margin untuk satu kanal pada simulasi.</summary>
public record ChannelMarginOverrideDto(int SalesChannelId, decimal MarginPercent);

/// <summary>Diskon untuk satu jenjang kemasan pada simulasi (0..1).</summary>
public record PackDiscountDto(int PackLevelId, decimal DiscountPercent);

/// <summary>
/// Permintaan simulasi harga. Nilai bertanda opsional (nullable) akan jatuh ke default dari
/// <see cref="Bom.Domain.Entities.PricingSetting"/> bila tidak diisi.
/// </summary>
public record SimulatePricingRequest(
    int ProductItemId,
    PricingMethod PricingMethod,
    CostBasis CostBasis,
    TaxMode TaxMode,
    decimal? TargetPercent = null,
    decimal? ManualBasePrice = null,
    decimal? FloorCost = null,
    decimal? MinMarginPercent = null,
    decimal? TaxPercent = null,
    RoundingRule? RoundingRule = null,
    decimal? FixedCostPerPeriod = null,
    IReadOnlyList<ChannelMarginOverrideDto>? ChannelMargins = null,
    IReadOnlyList<PackDiscountDto>? PackDiscounts = null);

/// <summary>Ringkasan satu jenjang kemasan pada hasil simulasi.</summary>
public record PricingPackLevelDto(int Id, string Name, decimal QtyPerBase);

/// <summary>
/// Satu sel matriks harga: (jenjang × kemasan). Untuk jenjang selain HET, <see cref="NetPrice"/>
/// bersifat tanpa pajak dan <see cref="TaxAmount"/> bersifat informasi; untuk HET,
/// <see cref="GrossPrice"/> adalah harga konsumen termasuk PPN.
/// </summary>
public record PricingCellDto(
    string TierName,
    int? SalesChannelId,
    int PackLevelId,
    string PackLevelName,
    decimal QtyPerBase,
    decimal NetPrice,
    decimal TaxAmount,
    decimal GrossPrice);

/// <summary>
/// Hasil simulasi harga sebuah produk. <see cref="BasePrice"/>, <see cref="MarginPct"/>,
/// <see cref="ContributionMargin"/>, dan <see cref="BreakEvenQty"/> dinyatakan per satu satuan
/// dasar (botol). <see cref="Cells"/> adalah matriks harga jenjang × kemasan.
/// </summary>
public record PricingResultDto(
    int ProductItemId,
    decimal Cost,
    CostBasis CostBasis,
    decimal BasePrice,
    decimal MarginPct,
    decimal ContributionMargin,
    int? BreakEvenQty,
    bool FloorViolation,
    IReadOnlyList<string> Tiers,
    IReadOnlyList<PricingPackLevelDto> PackLevels,
    IReadOnlyList<PricingCellDto> Cells);
