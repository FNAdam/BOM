using Bom.Domain.Enums;

namespace Bom.Application.Dtos;

/// <summary>
/// Satu baris laporan margin per produk. <see cref="ExFactoryPrice"/> dan
/// <see cref="StandardCost"/> dinyatakan per 1 satuan dasar (botol); margin dihitung terhadap harga jual.
/// </summary>
public record DashboardMarginItemDto(
    int ProductItemId,
    string ProductCode,
    string ProductName,
    ItemType ItemType,
    decimal StandardCost,
    decimal ExFactoryPrice,
    decimal ContributionMargin,
    decimal MarginPct,
    bool BelowMinMargin,
    bool HasActivePriceList);

/// <summary>Ringkasan agregat laporan margin.</summary>
public record DashboardMarginSummaryDto(
    int ProductCount,
    decimal AvgMarginPct,
    int BelowMinCount);

/// <summary>
/// Laporan margin dashboard: ambang margin minimum, baris per produk (diurutkan margin menaik /
/// terburuk dulu), dan ringkasan agregat.
/// </summary>
public record DashboardMarginDto(
    decimal MinMarginPercent,
    IReadOnlyList<DashboardMarginItemDto> Items,
    DashboardMarginSummaryDto Summary);
