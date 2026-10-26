using Bom.Application.Dtos;
using Bom.Domain.Enums;

namespace Bom.Application.Services;

/// <summary>
/// Kontrak layanan produksi &amp; COGS aktual. Implementasi (Infrastructure) mengelola perintah
/// produksi, buku besar stok (StockMovement) dengan biaya rata-rata bergerak, serta perhitungan
/// COGS dan variance saat order ditutup. Melempar <see cref="BomServiceException"/> (400/404/409)
/// untuk error domain.
/// </summary>
public interface IProductionService
{
    Task<IReadOnlyList<ProductionOrderListDto>> ListAsync(
        ProductionStatus? status, int? productItemId, CancellationToken ct = default);

    Task<ProductionOrderDto?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Buat order Open dari BOM Active; men-generate baris konsumsi dari komponen langsung.</summary>
    Task<ProductionOrderDto> CreateAsync(UpsertProductionOrderDto dto, CancellationToken ct = default);

    /// <summary>Edit header order Open. Bila BomId / PlannedQty berubah, konsumsi di-generate ulang.</summary>
    Task UpdateAsync(int id, UpsertProductionOrderDto dto, CancellationToken ct = default);

    /// <summary>Hapus order (hanya Open).</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Catat konsumsi aktual per komponen (order Open); belum memposting ledger.</summary>
    Task<ProductionOrderDto> RecordConsumptionAsync(int id, RecordConsumptionDto dto, CancellationToken ct = default);

    /// <summary>
    /// Tutup order (Open -&gt; Closed): posting ProductionIssue tiap komponen pada biaya rata-rata
    /// bergerak saat ini, hitung COGS, posting ProductionReceipt produk jadi pada COGS/unit,
    /// dan kembalikan laporan COGS + variance.
    /// </summary>
    Task<CogsReportDto> CloseAsync(int id, CloseProductionDto dto, CancellationToken ct = default);

    /// <summary>Laporan COGS + variance untuk order yang sudah Closed.</summary>
    Task<CogsReportDto> GetCogsAsync(int id, CancellationToken ct = default);

    /// <summary>Daftar buku besar pergerakan stok; opsional difilter per item, terbaru lebih dulu.</summary>
    Task<IReadOnlyList<StockMovementDto>> ListStockMovementsAsync(int? itemId, CancellationToken ct = default);
}
