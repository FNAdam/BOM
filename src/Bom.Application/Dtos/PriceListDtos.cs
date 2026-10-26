using Bom.Domain.Enums;

namespace Bom.Application.Dtos;

/// <summary>Satu baris daftar harga tersimpan (sel matriks).</summary>
public record PriceListLineDto(
    int Id,
    int? SalesChannelId,
    string? SalesChannelName,
    int PackLevelId,
    string PackLevelName,
    string TierName,
    decimal MarginPercent,
    decimal NetPrice,
    decimal TaxAmount,
    decimal GrossPrice,
    decimal ContributionMargin);

/// <summary>Daftar harga lengkap: parameter + baris hasil.</summary>
public record PriceListDto(
    int Id,
    int ProductItemId,
    string ProductCode,
    string ProductName,
    PricingMethod PricingMethod,
    CostBasis CostBasis,
    decimal TargetPercent,
    decimal? ManualBasePrice,
    decimal? FloorCost,
    TaxMode TaxMode,
    decimal TaxPercent,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    PriceListStatus Status,
    string? Notes,
    IReadOnlyList<PriceListLineDto> Lines);

/// <summary>Ringkasan daftar harga untuk tampilan daftar.</summary>
public record PriceListListItemDto(
    int Id,
    int ProductItemId,
    string ProductCode,
    string ProductName,
    PricingMethod PricingMethod,
    CostBasis CostBasis,
    decimal TargetPercent,
    PriceListStatus Status,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    int LineCount);

/// <summary>
/// Payload simpan daftar harga: parameter simulasi (untuk regenerasi baris) + metadata.
/// </summary>
public record SavePriceListRequest(
    SimulatePricingRequest Simulation,
    string? Notes = null,
    DateOnly? EffectiveFrom = null,
    DateOnly? EffectiveTo = null);
