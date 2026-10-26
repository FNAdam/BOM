using Bom.Application.Dtos;

namespace Bom.Application.Services;

/// <summary>
/// Kontrak layanan costing standar (cost roll-up). Implementasi menghitung biaya
/// secara rekursif menembus BOM Active dan melempar <see cref="BomServiceException"/>
/// (404 item tidak ada, 409 siklus) untuk error domain.
/// </summary>
public interface ICostingService
{
    /// <summary>Hitung rincian biaya standar sebuah item (tanpa menyimpan).</summary>
    Task<CostBreakdownDto> GetCostBreakdownAsync(int itemId, CancellationToken ct = default);

    /// <summary>Hitung biaya lalu simpan sebagai <see cref="Bom.Domain.Entities.CostSnapshot"/>.</summary>
    Task<CostBreakdownDto> SaveSnapshotAsync(int itemId, CancellationToken ct = default);

    /// <summary>Daftar snapshot tersimpan; opsional difilter per item, terbaru lebih dulu.</summary>
    Task<IReadOnlyList<CostSnapshotDto>> ListSnapshotsAsync(int? itemId, CancellationToken ct = default);

    /// <summary>
    /// Biaya standar per satu base UoM item (dibeli = harga supplier preferred efektif / fallback
    /// StandardMaterialCost; ber-BOM = rollup dinyatakan per base UoM). Dipakai analisis variance
    /// produksi. Melempar 404 bila item tidak ada, 409 bila terdeteksi siklus BOM.
    /// </summary>
    Task<decimal> GetUnitCostPerBaseUomAsync(int itemId, CancellationToken ct = default);
}
