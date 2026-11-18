using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bom.Infrastructure.Services;

/// <inheritdoc cref="IInventoryLedgerService"/>
public class InventoryLedgerService : IInventoryLedgerService
{
    private readonly BomDbContext _db;

    public InventoryLedgerService(BomDbContext db) => _db = db;

    public async Task<decimal> ApplyMovementAsync(
        int itemId, StockMovementType type, decimal qty, decimal unitCost,
        DateOnly date, string? refType, int? refId, string? notes, CancellationToken ct = default)
    {
        var balance = await _db.InventoryBalances.FirstOrDefaultAsync(b => b.ItemId == itemId, ct);
        if (balance is null)
        {
            balance = new InventoryBalance { ItemId = itemId, QtyOnHand = 0m, AverageCost = 0m };
            _db.InventoryBalances.Add(balance);
        }

        var recordedUnitCost = unitCost;
        switch (type)
        {
            case StockMovementType.Receipt:
            case StockMovementType.ProductionReceipt:
            {
                var newQty = balance.QtyOnHand + qty;
                // Saldo <= 0 (mis. habis terjual lebih dulu / backflush negatif) bikin rumus
                // rata-rata tertimbang biasa salah (komponen "qty negatif * avg lama" meracuni
                // hasil) - kalau saldo sebelumnya bukan stok riil positif, mulai lagi dari harga
                // terima ini saja.
                if (balance.QtyOnHand <= 0)
                    balance.AverageCost = unitCost;
                else if (newQty > 0)
                    balance.AverageCost = (balance.QtyOnHand * balance.AverageCost + qty * unitCost) / newQty;
                balance.QtyOnHand = newQty;
                break;
            }
            case StockMovementType.Issue:
            case StockMovementType.ProductionIssue:
            case StockMovementType.Sale:
            {
                recordedUnitCost = balance.AverageCost; // keluaran memakai rata-rata saat ini
                balance.QtyOnHand -= qty;               // rata-rata tetap
                break;
            }
            case StockMovementType.Adjustment:
            {
                var newQty = balance.QtyOnHand + qty; // qty boleh negatif (delta)
                if (qty > 0 && balance.QtyOnHand <= 0)
                    balance.AverageCost = unitCost;
                else if (qty > 0 && newQty > 0)
                    balance.AverageCost = (balance.QtyOnHand * balance.AverageCost + qty * unitCost) / newQty;
                else
                    recordedUnitCost = balance.AverageCost;
                balance.QtyOnHand = newQty;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Jenis pergerakan stok tidak dikenali.");
        }

        _db.StockMovements.Add(new StockMovement
        {
            ItemId = itemId,
            Type = type,
            Qty = qty,
            UnitCost = recordedUnitCost,
            MovementDate = date,
            RefType = refType,
            RefId = refId,
            Notes = notes
        });

        return recordedUnitCost;
    }
}
