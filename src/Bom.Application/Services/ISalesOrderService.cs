using Bom.Application.Dtos;
using Bom.Domain.Enums;

namespace Bom.Application.Services;

/// <summary>
/// Pesanan penjualan ke customer (teks bebas, tanpa master data): Draft (bebas diedit) →
/// Completed (immutable, diposting ke stok lewat <see cref="IInventoryLedgerService"/> sebagai
/// StockMovementType.Sale), atau Cancelled dari Draft.
/// </summary>
public interface ISalesOrderService
{
    Task<IReadOnlyList<SalesOrderListDto>> ListAsync(
        SalesOrderStatus? status, int? salesChannelId, CancellationToken ct = default);

    Task<SalesOrderDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<SalesOrderDto> CreateAsync(UpsertSalesOrderDto dto, CancellationToken ct = default);

    /// <summary>Hanya boleh saat status Draft - seluruh baris diganti.</summary>
    Task UpdateAsync(int id, UpsertSalesOrderDto dto, CancellationToken ct = default);

    /// <summary>Hanya boleh saat status Draft.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Draft → Completed: posting StockMovement.Sale per baris (stok keluar pada rata-rata
    /// bergerak saat ini) lalu mengunci pesanan. Stok boleh jadi negatif (tidak diblokir).
    /// </summary>
    Task<SalesOrderDto> CompleteAsync(int id, CancellationToken ct = default);

    /// <summary>Batalkan pesanan (hanya dari Draft - belum menyentuh stok).</summary>
    Task<SalesOrderDto> CancelAsync(int id, CancellationToken ct = default);

    /// <summary>Data siap cetak struk. Hanya tersedia untuk pesanan Completed.</summary>
    Task<SalesReceiptDto> GetReceiptAsync(int id, CancellationToken ct = default);
}
