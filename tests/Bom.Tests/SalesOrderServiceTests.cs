using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Bom.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bom.Tests;

/// <summary>
/// Uji SalesOrderService dengan DbContext InMemory (UoM 1..5 disemai lewat EnsureCreated).
/// Dataset: FG_P (finished, base pcs) dengan PackLevel "Pack" (isi 12); RM_A (raw) untuk uji
/// penolakan produk yang tidak bisa dijual.
/// </summary>
public class SalesOrderServiceTests
{
    private const int RM_A = 10;
    private const int FG_P = 20;
    private const int PackLevelPack = 1;
    private const int UomPcs = 5;

    private static BomDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseInMemoryDatabase($"so-tests-{Guid.NewGuid()}")
            .Options;

        var ctx = new BomDbContext(options);
        ctx.Database.EnsureCreated(); // menyemai Uom 1..5

        ctx.Items.AddRange(
            new Item { Id = RM_A, Code = "RM-A", Name = "Raw A", Type = ItemType.RawMaterial, BaseUomId = UomPcs, StandardMaterialCost = 100m },
            new Item { Id = FG_P, Code = "FG-P", Name = "Finished P", Type = ItemType.FinishedGood, BaseUomId = UomPcs });

        ctx.PackLevels.Add(new PackLevel { Id = PackLevelPack, FinishedItemId = FG_P, Name = "Pack", QtyPerBase = 12m, Sequence = 1 });

        // Saldo awal FG_P: 100 pcs @ 10.000 (lewat ledger Adjustment).
        ctx.SaveChanges();
        return ctx;
    }

    private static async Task SeedStockAsync(BomDbContext ctx, int itemId, decimal qty, decimal cost)
    {
        var ledger = new InventoryLedgerService(ctx);
        await ledger.ApplyMovementAsync(itemId, StockMovementType.Adjustment, qty, cost,
            DateOnly.FromDateTime(DateTime.Today), null, null, "saldo awal");
        await ctx.SaveChangesAsync();
    }

    private static SalesOrderService NewService(BomDbContext ctx) => new(ctx, new InventoryLedgerService(ctx));

    private static UpsertSalesOrderDto NewSoDto(decimal qty = 10m, decimal price = 15000m, int? packLevelId = null) => new(
        "SO-0001", null, "Budi", DateOnly.FromDateTime(DateTime.Today), 0.11m, null,
        new List<UpsertSalesOrderLineDto> { new(FG_P, packLevelId, qty, price) });

    [Fact]
    public async Task Create_MenyimpanHeaderDanBaris_StatusDraft()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);

        var so = await service.CreateAsync(NewSoDto());

        Assert.Equal(SalesOrderStatus.Draft, so.Status);
        Assert.Equal("Budi", so.CustomerName);
        Assert.Single(so.Lines);
        Assert.Equal(150000m, so.Lines[0].LineTotal);
    }

    [Fact]
    public async Task Create_ProdukBukanSellable_MelemparBadRequest()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var dto = NewSoDto() with { Lines = new List<UpsertSalesOrderLineDto> { new(RM_A, null, 1m, 100m) } };

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.CreateAsync(dto));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Create_PackLevelBukanMilikProduk_MelemparBadRequest()
    {
        using var ctx = NewContext();
        // Produk lain tanpa PackLevel ini.
        ctx.Items.Add(new Item { Id = 21, Code = "FG-Q", Name = "Finished Q", Type = ItemType.FinishedGood, BaseUomId = UomPcs });
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        var dto = NewSoDto() with { Lines = new List<UpsertSalesOrderLineDto> { new(21, PackLevelPack, 1m, 100m) } };

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.CreateAsync(dto));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Complete_TanpaPackLevel_MengurangiStokSatuanDasar()
    {
        using var ctx = NewContext();
        await SeedStockAsync(ctx, FG_P, 100m, 10000m);
        var service = NewService(ctx);

        var so = await service.CreateAsync(NewSoDto(qty: 10m));
        var completed = await service.CompleteAsync(so.Id);

        Assert.Equal(SalesOrderStatus.Completed, completed.Status);

        var balance = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == FG_P);
        Assert.Equal(90m, balance.QtyOnHand);
        Assert.Equal(10000m, balance.AverageCost); // keluaran tidak mengubah rata-rata

        var movement = await ctx.StockMovements.FirstAsync(m => m.Type == StockMovementType.Sale);
        Assert.Equal(10m, movement.Qty);
        Assert.Equal("SalesOrder", movement.RefType);
    }

    [Fact]
    public async Task Complete_DenganPackLevel_MengurangiStokSesuaiQtyPerBase()
    {
        using var ctx = NewContext();
        await SeedStockAsync(ctx, FG_P, 100m, 10000m);
        var service = NewService(ctx);

        // Jual 2 Pack (isi 12) -> harusnya kurangi stok 24 pcs.
        var so = await service.CreateAsync(NewSoDto(qty: 2m, price: 150000m, packLevelId: PackLevelPack));
        await service.CompleteAsync(so.Id);

        var balance = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == FG_P);
        Assert.Equal(76m, balance.QtyOnHand); // 100 - (2*12)

        var movement = await ctx.StockMovements.FirstAsync(m => m.Type == StockMovementType.Sale);
        Assert.Equal(24m, movement.Qty);
    }

    [Fact]
    public async Task Complete_StokTidakCukup_TetapBerhasilJadiNegatif()
    {
        using var ctx = NewContext();
        await SeedStockAsync(ctx, FG_P, 5m, 10000m);
        var service = NewService(ctx);

        var so = await service.CreateAsync(NewSoDto(qty: 10m));
        await service.CompleteAsync(so.Id);

        var balance = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == FG_P);
        Assert.Equal(-5m, balance.QtyOnHand);
    }

    [Fact]
    public async Task Cancel_DariDraft_Berhasil()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var so = await service.CreateAsync(NewSoDto());

        var result = await service.CancelAsync(so.Id);

        Assert.Equal(SalesOrderStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task Cancel_SetelahCompleted_MelemparConflict()
    {
        using var ctx = NewContext();
        await SeedStockAsync(ctx, FG_P, 100m, 10000m);
        var service = NewService(ctx);
        var so = await service.CreateAsync(NewSoDto());
        await service.CompleteAsync(so.Id);

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.CancelAsync(so.Id));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Update_SetelahCompleted_MelemparConflict()
    {
        using var ctx = NewContext();
        await SeedStockAsync(ctx, FG_P, 100m, 10000m);
        var service = NewService(ctx);
        var so = await service.CreateAsync(NewSoDto());
        await service.CompleteAsync(so.Id);

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.UpdateAsync(so.Id, NewSoDto()));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task GetReceipt_SebelumCompleted_MelemparConflict()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var so = await service.CreateAsync(NewSoDto());

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.GetReceiptAsync(so.Id));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task GetReceipt_SetelahCompleted_HitungSubtotalDanPajakBenar()
    {
        using var ctx = NewContext();
        await SeedStockAsync(ctx, FG_P, 100m, 10000m);
        var service = NewService(ctx);
        var so = await service.CreateAsync(NewSoDto(qty: 10m, price: 15000m)); // subtotal 150.000, PPN 11%
        await service.CompleteAsync(so.Id);

        var receipt = await service.GetReceiptAsync(so.Id);

        Assert.Equal(150000m, receipt.Subtotal);
        Assert.Equal(16500m, receipt.TaxAmount);
        Assert.Equal(166500m, receipt.GrandTotal);
    }
}
