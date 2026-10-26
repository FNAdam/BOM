using Bom.Application.Dtos;

namespace Bom.Application.Services;

/// <summary>
/// Kontrak layanan laporan dashboard. Menyusun margin per produk dari biaya standar
/// (<see cref="ICostingService"/>) dan harga ex-factory (price list Active atau simulasi default).
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Hitung laporan margin untuk produk ber-harga (PriceList Active) atau ber-BOM Active,
    /// termasuk ringkasan agregat. Baris diurutkan margin menaik (terburuk lebih dulu).
    /// </summary>
    Task<DashboardMarginDto> GetMarginReportAsync(CancellationToken ct = default);
}
