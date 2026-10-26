using Bom.Application.Dtos;
using Bom.Domain.Enums;

namespace Bom.Application.Services;

/// <summary>
/// Kontrak layanan penetapan harga jual (Fase 6). Menyimulasikan matriks harga jenjang × kemasan
/// dari biaya (standar / LastCOGS), dan mengelola daftar harga (price list) tersimpan.
/// Melempar <see cref="BomServiceException"/> (400/404/409) untuk error domain.
/// </summary>
public interface IPricingService
{
    /// <summary>
    /// Simulasi harga tanpa menyimpan: hitung harga dasar, jenjang kanal, HET (termasuk PPN),
    /// matriks per kemasan, margin, kontribusi, break-even, dan penanda floor.
    /// </summary>
    Task<PricingResultDto> SimulateAsync(SimulatePricingRequest req, CancellationToken ct = default);

    /// <summary>Daftar price list; opsional difilter per produk dan/atau status.</summary>
    Task<IReadOnlyList<PriceListListItemDto>> ListPriceListsAsync(
        int? productItemId, PriceListStatus? status, CancellationToken ct = default);

    Task<PriceListDto?> GetPriceListAsync(int id, CancellationToken ct = default);

    /// <summary>Simpan price list baru (Draft) beserta baris hasil simulasi.</summary>
    Task<PriceListDto> CreatePriceListAsync(SavePriceListRequest req, CancellationToken ct = default);

    /// <summary>Perbarui price list Draft (regenerasi baris dari simulasi baru).</summary>
    Task<PriceListDto> UpdatePriceListAsync(int id, SavePriceListRequest req, CancellationToken ct = default);

    /// <summary>Hapus price list (hanya non-Active).</summary>
    Task DeletePriceListAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Aktifkan price list: set Active, dan Active lain untuk produk sama menjadi Obsolete
    /// (transisi aman terhadap partial unique index "satu Active per produk").
    /// </summary>
    Task<PriceListDto> ActivatePriceListAsync(int id, CancellationToken ct = default);
}
