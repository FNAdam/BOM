using Bom.Domain.Enums;

namespace Bom.Application.Services;

/// <summary>
/// Buku besar stok + rata-rata bergerak, dipakai bersama oleh <see cref="IProductionService"/> dan
/// <see cref="IPurchaseOrderService"/> (satu-satunya tempat <see cref="Domain.Entities.InventoryBalance"/>
/// dan <see cref="Domain.Entities.StockMovement"/> ditulis).
/// </summary>
public interface IInventoryLedgerService
{
    /// <summary>
    /// Terapkan satu pergerakan stok: upsert InventoryBalance (moving average) + tulis satu baris
    /// StockMovement. <paramref name="qty"/> = magnitudo positif (kecuali Adjustment yang boleh delta).
    /// Masuk (Receipt/ProductionReceipt): newQty = onHand + qty; newAvg = (onHand*avg + qty*unitCost)/newQty.
    /// Keluar (Issue/ProductionIssue): unitCost efektif = rata-rata saat ini; onHand -= qty; rata-rata tetap.
    /// Saldo negatif diizinkan.
    /// </summary>
    /// <returns>UnitCost efektif yang tercatat pada movement (untuk tipe keluaran, bukan parameter unitCost).</returns>
    Task<decimal> ApplyMovementAsync(
        int itemId, StockMovementType type, decimal qty, decimal unitCost,
        DateOnly date, string? refType, int? refId, string? notes, CancellationToken ct = default);
}
