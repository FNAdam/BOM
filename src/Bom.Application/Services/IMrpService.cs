using Bom.Application.Dtos;

namespace Bom.Application.Services;

/// <summary>
/// Kontrak MRP sederhana: explosion + netting rekursif atas BOM Active, menghasilkan
/// rencana produksi (Make) dan saran pembelian (Buy). Melempar
/// <see cref="BomServiceException"/> (400 validasi, 404 item tidak ada, 409 siklus).
/// </summary>
public interface IMrpService
{
    /// <summary>Jalankan MRP untuk sekumpulan demand produk.</summary>
    Task<MrpResultDto> ExplodeAsync(MrpRequestDto request, CancellationToken ct = default);
}
