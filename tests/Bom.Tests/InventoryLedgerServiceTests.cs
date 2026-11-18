using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Bom.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bom.Tests;

/// <summary>
/// Uji InventoryLedgerService secara langsung (di luar Production/PurchaseOrder/SalesOrder),
/// fokus ke kasus tepi rata-rata bergerak.
/// </summary>
public class InventoryLedgerServiceTests
{
    private const int ItemA = 1;
    private const int UomPcs = 5;

    private static BomDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseInMemoryDatabase($"ledger-tests-{Guid.NewGuid()}")
            .Options;

        var ctx = new BomDbContext(options);
        ctx.Database.EnsureCreated(); // menyemai Uom 1..5
        ctx.Items.Add(new Item { Id = ItemA, Code = "A", Name = "Item A", Type = ItemType.FinishedGood, BaseUomId = UomPcs });
        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task Receipt_SetelahSaldoJadiNegatif_RataRataJadiHargaTerimaBaru_BukanTerinflasi()
    {
        using var ctx = NewContext();
        var ledger = new InventoryLedgerService(ctx);
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Jual 10 padahal stok masih 0 -> saldo jadi -10 (backflush negatif, diizinkan).
        await ledger.ApplyMovementAsync(ItemA, StockMovementType.Sale, 10m, 0m, today, null, null, null);
        await ctx.SaveChangesAsync();

        var afterSale = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == ItemA);
        Assert.Equal(-10m, afterSale.QtyOnHand);

        // Lalu terima 100 @ 10.000. Rumus rata-rata tertimbang naif akan hitung:
        // (-10*0 + 100*10000) / 90 = 11.111,1 (salah, lebih tinggi dari harga beli sungguhan).
        // Yang benar: karena saldo sebelumnya <= 0, rata-rata baru = harga terima ini saja.
        await ledger.ApplyMovementAsync(ItemA, StockMovementType.Receipt, 100m, 10000m, today, null, null, null);
        await ctx.SaveChangesAsync();

        var afterReceipt = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == ItemA);
        Assert.Equal(90m, afterReceipt.QtyOnHand);
        Assert.Equal(10000m, afterReceipt.AverageCost);
    }

    [Fact]
    public async Task Adjustment_SetelahSaldoNegatif_NaikkanSaldo_RataRataJadiHargaAdjustmentBaru()
    {
        using var ctx = NewContext();
        var ledger = new InventoryLedgerService(ctx);
        var today = DateOnly.FromDateTime(DateTime.Today);

        await ledger.ApplyMovementAsync(ItemA, StockMovementType.Sale, 5m, 0m, today, null, null, null);
        await ctx.SaveChangesAsync();

        await ledger.ApplyMovementAsync(ItemA, StockMovementType.Adjustment, 20m, 7000m, today, null, null, null);
        await ctx.SaveChangesAsync();

        var balance = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == ItemA);
        Assert.Equal(15m, balance.QtyOnHand);
        Assert.Equal(7000m, balance.AverageCost);
    }

    [Fact]
    public async Task Receipt_SaldoPositifNormal_TetapPakaiRataRataTertimbangBiasa()
    {
        using var ctx = NewContext();
        var ledger = new InventoryLedgerService(ctx);
        var today = DateOnly.FromDateTime(DateTime.Today);

        await ledger.ApplyMovementAsync(ItemA, StockMovementType.Receipt, 10m, 100m, today, null, null, null);
        await ctx.SaveChangesAsync();
        await ledger.ApplyMovementAsync(ItemA, StockMovementType.Receipt, 10m, 200m, today, null, null, null);
        await ctx.SaveChangesAsync();

        // (10*100 + 10*200) / 20 = 150.
        var balance = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == ItemA);
        Assert.Equal(20m, balance.QtyOnHand);
        Assert.Equal(150m, balance.AverageCost);
    }
}
