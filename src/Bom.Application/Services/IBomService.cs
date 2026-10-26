using Bom.Application.Dtos;
using Bom.Domain.Enums;

namespace Bom.Application.Services;

/// <summary>
/// Kontrak layanan BOM. Implementasi (di layer Infrastructure) menegakkan seluruh
/// aturan bisnis dan melempar <see cref="BomServiceException"/> untuk error domain.
/// </summary>
public interface IBomService
{
    Task<IReadOnlyList<BomListItemDto>> ListAsync(int? productItemId, BomStatus? status, CancellationToken ct = default);

    Task<BomDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<BomDto> CreateAsync(UpsertBomDto dto, CancellationToken ct = default);

    Task UpdateAsync(int id, UpsertBomDto dto, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Aktifkan BOM Draft: cek siklus, obsolete-kan Active lama, set Active + EffectiveFrom.</summary>
    Task<BomDto> ActivateAsync(int id, CancellationToken ct = default);

    /// <summary>Klon header+komponen+operasi menjadi Draft versi baru (max versi produk + 1).</summary>
    Task<BomDto> CreateNewVersionAsync(int id, CancellationToken ct = default);

    /// <summary>Explode multi-level; qty null memakai OutputQuantity BOM.</summary>
    Task<BomExplosionNodeDto> ExplodeAsync(int id, decimal? qty, CancellationToken ct = default);
}
