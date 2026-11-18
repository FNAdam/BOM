using Bom.Application.Dtos;
using Bom.Domain.Enums;

namespace Bom.Application.Services;

/// <summary>
/// Pesanan pembelian ke supplier: Draft (bebas diedit) → Sent → PartiallyReceived/Received,
/// atau Cancelled dari Draft/Sent/PartiallyReceived. Penerimaan barang memposting
/// <see cref="IInventoryLedgerService"/> (StockMovement.Receipt + update InventoryBalance).
/// </summary>
public interface IPurchaseOrderService
{
    Task<IReadOnlyList<PurchaseOrderListDto>> ListAsync(
        PurchaseOrderStatus? status, int? supplierId, CancellationToken ct = default);

    Task<PurchaseOrderDto?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Saran nomor PO berikutnya (format "PO-0000") - hanya saran, tetap divalidasi unik saat Create.</summary>
    Task<string> GetNextOrderNoAsync(CancellationToken ct = default);

    Task<PurchaseOrderDto> CreateAsync(UpsertPurchaseOrderDto dto, CancellationToken ct = default);

    /// <summary>Hanya boleh saat status Draft - seluruh baris diganti.</summary>
    Task UpdateAsync(int id, UpsertPurchaseOrderDto dto, CancellationToken ct = default);

    /// <summary>Hanya boleh saat status Draft.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Draft → Sent, menandai PO siap menerima barang.</summary>
    Task<PurchaseOrderDto> SendAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Catat barang yang diterima (boleh parsial/bertahap). Tiap baris memposting satu
    /// StockMovement.Receipt. Status otomatis jadi PartiallyReceived atau Received.
    /// </summary>
    Task<PurchaseOrderDto> ReceiveAsync(int id, ReceivePurchaseOrderDto dto, CancellationToken ct = default);

    /// <summary>Batalkan sisa yang belum diterima (dari Draft/Sent/PartiallyReceived).</summary>
    Task<PurchaseOrderDto> CancelAsync(int id, CancellationToken ct = default);
}
