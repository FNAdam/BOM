using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Bom.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using BomEntity = Bom.Domain.Entities.Bom;

namespace Bom.Tests;

/// <summary>
/// Uji ProductionService dengan DbContext InMemory (UoM 1..5 disemai lewat EnsureCreated).
///
/// Dataset:
///   RM_A  (10) raw, base kg, std cost 100.
///   SF_B  (11) semi-finished, base L (punya BOM sendiri: RM_C) -> untuk uji explode SATU level.
///   RM_C  (12) raw, base L.
///   FG_P  (20) finished, base pcs, BOM 2 komponen (RM_A + SF_B), output 10.
///   FG_Q  (21) finished, base pcs, BOM 1 komponen (RM_A 10 kg) + labor 50/unit, output 10
///              -> mereproduksi contoh COGS spec (material 1000 + conv 500 = 1500, /10 = 150).
/// </summary>
public class ProductionServiceTests
{
    private const int RM_A = 10;
    private const int SF_B = 11;
    private const int RM_C = 12;
    private const int FG_P = 20;
    private const int FG_Q = 21;

    private const int BomSfB = 90;
    private const int BomFgP = 100;
    private const int BomFgQ = 110;

    private const int UomKg = 1;
    private const int UomL = 3;
    private const int UomPcs = 5;

    private static BomDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseInMemoryDatabase($"production-tests-{Guid.NewGuid()}")
            .Options;

        var ctx = new BomDbContext(options);
        ctx.Database.EnsureCreated(); // menyemai Uom 1..5

        ctx.Items.AddRange(
            new Item { Id = RM_A, Code = "RM-A", Name = "Raw A", Type = ItemType.RawMaterial, BaseUomId = UomKg, StandardMaterialCost = 100m },
            new Item { Id = SF_B, Code = "SF-B", Name = "Semi B", Type = ItemType.SemiFinished, BaseUomId = UomL, StandardMaterialCost = 0m },
            new Item { Id = RM_C, Code = "RM-C", Name = "Raw C", Type = ItemType.RawMaterial, BaseUomId = UomL, StandardMaterialCost = 5000m },
            new Item { Id = FG_P, Code = "FG-P", Name = "Finished P", Type = ItemType.FinishedGood, BaseUomId = UomPcs },
            new Item { Id = FG_Q, Code = "FG-Q", Name = "Finished Q", Type = ItemType.FinishedGood, BaseUomId = UomPcs });

        // BOM SF_B: output 1 L, 1 komponen RM_C (agar SF_B "ber-BOM").
        ctx.Boms.Add(new BomEntity
        {
            Id = BomSfB,
            ProductItemId = SF_B,
            Version = 1,
            Status = BomStatus.Active,
            OutputQuantity = 1m,
            OutputUomId = UomL,
            YieldPercent = 1m,
            Components = { new BomComponent { ComponentItemId = RM_C, Quantity = 1m, UomId = UomL, Sequence = 1 } }
        });

        // BOM FG_P: output 10, komponen RM_A (10 kg) + SF_B (5 L).
        ctx.Boms.Add(new BomEntity
        {
            Id = BomFgP,
            ProductItemId = FG_P,
            Version = 1,
            Status = BomStatus.Active,
            OutputQuantity = 10m,
            OutputUomId = UomPcs,
            YieldPercent = 1m,
            Components =
            {
                new BomComponent { ComponentItemId = RM_A, Quantity = 10m, UomId = UomKg, Sequence = 1 },
                new BomComponent { ComponentItemId = SF_B, Quantity = 5m, UomId = UomL, Sequence = 2 }
            }
        });

        // BOM FG_Q: output 10, komponen RM_A (10 kg), labor 50/unit.
        ctx.Boms.Add(new BomEntity
        {
            Id = BomFgQ,
            ProductItemId = FG_Q,
            Version = 1,
            Status = BomStatus.Active,
            OutputQuantity = 10m,
            OutputUomId = UomPcs,
            YieldPercent = 1m,
            Components = { new BomComponent { ComponentItemId = RM_A, Quantity = 10m, UomId = UomKg, Sequence = 1 } },
            Operations = { new BomOperation { Sequence = 1, Name = "Assembly", LaborCostPerOutputUnit = 50m, OverheadRate = 0m } }
        });

        ctx.SaveChanges();
        return ctx;
    }

    private static ProductionService NewService(BomDbContext ctx) => new(ctx, new CostingService(ctx));

    private static async Task SeedBalanceAsync(BomDbContext ctx, int itemId, decimal qty, decimal avg)
    {
        ctx.InventoryBalances.Add(new InventoryBalance { ItemId = itemId, QtyOnHand = qty, AverageCost = avg });
        await ctx.SaveChangesAsync();
    }

    // ------------------------------------------------------- Create (one level)

    [Fact]
    public async Task Create_GeneratesConsumptions_OneLevelOnly()
    {
        using var ctx = NewContext();
        var svc = NewService(ctx);

        var order = await svc.CreateAsync(new UpsertProductionOrderDto(
            BomFgP, PlannedQty: 10m, BatchNo: "B-001", StartDate: null, EndDate: null, Notes: null));

        Assert.Equal(ProductionStatus.Open, order.Status);
        Assert.Equal(FG_P, order.ProductItemId);

        // Hanya komponen langsung: RM_A dan SF_B (RM_C dari sub-BOM SF_B TIDAK ikut).
        Assert.Equal(2, order.Consumptions.Count);
        Assert.DoesNotContain(order.Consumptions, c => c.ComponentItemId == RM_C);

        var rmA = Assert.Single(order.Consumptions, c => c.ComponentItemId == RM_A);
        Assert.Equal(10m, rmA.PlannedQty);   // 10 kg * 10 / 10
        Assert.Equal(10m, rmA.ActualQty);    // actual awal = planned
        Assert.Equal("kg", rmA.UomCode);

        var sfB = Assert.Single(order.Consumptions, c => c.ComponentItemId == SF_B);
        Assert.Equal(5m, sfB.PlannedQty);    // 5 L * 10 / 10
        Assert.Equal("L", sfB.UomCode);
    }

    [Fact]
    public async Task Create_FromNonActiveBom_Throws400()
    {
        using var ctx = NewContext();
        // Jadikan BOM FG_Q non-Active.
        var bom = await ctx.Boms.FirstAsync(b => b.Id == BomFgQ);
        bom.Status = BomStatus.Draft;
        await ctx.SaveChangesAsync();

        var svc = NewService(ctx);
        var ex = await Assert.ThrowsAsync<BomServiceException>(() => svc.CreateAsync(
            new UpsertProductionOrderDto(BomFgQ, 10m, "B-X", null, null, null)));
        Assert.Equal(400, ex.StatusCode);
    }

    // ------------------------------------------------------------ Moving average

    [Fact]
    public async Task Close_IssuesComponentAtCurrentAverage_AndReducesOnHand()
    {
        using var ctx = NewContext();
        await SeedBalanceAsync(ctx, RM_A, qty: 1000m, avg: 100m);
        var svc = NewService(ctx);

        var order = await svc.CreateAsync(new UpsertProductionOrderDto(BomFgQ, 10m, "B-Q1", null, null, null));
        await svc.CloseAsync(order.Id, new CloseProductionDto(ActualQty: 10m));

        // Komponen ter-issue pada avg 100; on-hand 1000 -> 990.
        var rmBal = await ctx.InventoryBalances.AsNoTracking().FirstAsync(b => b.ItemId == RM_A);
        Assert.Equal(990m, rmBal.QtyOnHand);
        Assert.Equal(100m, rmBal.AverageCost); // OUT tidak mengubah avg

        var issue = await ctx.StockMovements.AsNoTracking()
            .SingleAsync(m => m.ItemId == RM_A && m.Type == StockMovementType.ProductionIssue);
        Assert.Equal(10m, issue.Qty);
        Assert.Equal(100m, issue.UnitCost);
    }

    [Fact]
    public async Task Close_TwoProductionReceipts_WeightMovingAverage()
    {
        using var ctx = NewContext();
        await SeedBalanceAsync(ctx, RM_A, qty: 1000m, avg: 100m);
        var svc = NewService(ctx);

        // Order 1: RM_A avg 100 -> COGS_unit 150. Produk terima 10 @150.
        var o1 = await svc.CreateAsync(new UpsertProductionOrderDto(BomFgQ, 10m, "B-Q1", null, null, null));
        await svc.CloseAsync(o1.Id, new CloseProductionDto(10m));

        var afterFirst = await ctx.InventoryBalances.AsNoTracking().FirstAsync(b => b.ItemId == FG_Q);
        Assert.Equal(10m, afterFirst.QtyOnHand);
        Assert.Equal(150m, afterFirst.AverageCost);

        // Simulasikan penerimaan RM_A baru pada harga lebih tinggi: avg naik ke 200.
        var rmBal = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == RM_A);
        rmBal.AverageCost = 200m;
        await ctx.SaveChangesAsync();

        // Order 2: RM_A avg 200 -> COGS_unit 250. Produk terima 10 @250.
        var o2 = await svc.CreateAsync(new UpsertProductionOrderDto(BomFgQ, 10m, "B-Q2", null, null, null));
        await svc.CloseAsync(o2.Id, new CloseProductionDto(10m));

        // Moving average produk: (10*150 + 10*250) / 20 = 200; on-hand 20.
        var afterSecond = await ctx.InventoryBalances.AsNoTracking().FirstAsync(b => b.ItemId == FG_Q);
        Assert.Equal(20m, afterSecond.QtyOnHand);
        Assert.Equal(200m, afterSecond.AverageCost);
    }

    // --------------------------------------------------------------- Close/COGS

    [Fact]
    public async Task Close_ComputesCogs_MatchesHandCalc()
    {
        using var ctx = NewContext();
        await SeedBalanceAsync(ctx, RM_A, qty: 1000m, avg: 100m);
        var svc = NewService(ctx);

        var order = await svc.CreateAsync(new UpsertProductionOrderDto(BomFgQ, 10m, "B-Q1", null, null, null));
        var report = await svc.CloseAsync(order.Id, new CloseProductionDto(ActualQty: 10m));

        // 1 komponen 10 unit @100 + labor 50/unit, output 10.
        Assert.Equal(1000m, report.MaterialCost);
        Assert.Equal(500m, report.ConversionCost);
        Assert.Equal(1500m, report.CogsTotal);
        Assert.Equal(150m, report.CogsUnit);

        // Order tertutup, ActualQty terisi.
        var reloaded = await svc.GetByIdAsync(order.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(ProductionStatus.Closed, reloaded!.Status);
        Assert.Equal(10m, reloaded.ActualQty);

        // Produk jadi diterima pada COGS_unit.
        var receipt = await ctx.StockMovements.AsNoTracking()
            .SingleAsync(m => m.ItemId == FG_Q && m.Type == StockMovementType.ProductionReceipt);
        Assert.Equal(10m, receipt.Qty);
        Assert.Equal(150m, receipt.UnitCost);

        var fgBal = await ctx.InventoryBalances.AsNoTracking().FirstAsync(b => b.ItemId == FG_Q);
        Assert.Equal(10m, fgBal.QtyOnHand);
        Assert.Equal(150m, fgBal.AverageCost);
    }

    [Fact]
    public async Task Close_NonOpenOrder_Throws409()
    {
        using var ctx = NewContext();
        await SeedBalanceAsync(ctx, RM_A, qty: 1000m, avg: 100m);
        var svc = NewService(ctx);

        var order = await svc.CreateAsync(new UpsertProductionOrderDto(BomFgQ, 10m, "B-Q1", null, null, null));
        await svc.CloseAsync(order.Id, new CloseProductionDto(10m));

        var ex = await Assert.ThrowsAsync<BomServiceException>(
            () => svc.CloseAsync(order.Id, new CloseProductionDto(10m)));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Close_ZeroActualQty_Throws400()
    {
        using var ctx = NewContext();
        await SeedBalanceAsync(ctx, RM_A, qty: 1000m, avg: 100m);
        var svc = NewService(ctx);

        var order = await svc.CreateAsync(new UpsertProductionOrderDto(BomFgQ, 10m, "B-Q1", null, null, null));
        var ex = await Assert.ThrowsAsync<BomServiceException>(
            () => svc.CloseAsync(order.Id, new CloseProductionDto(0m)));
        Assert.Equal(400, ex.StatusCode);
    }

    // ----------------------------------------------------------------- Variance

    [Fact]
    public async Task Close_Variance_PriceUsageAndYield_HaveCorrectSignAndValue()
    {
        using var ctx = NewContext();
        // RM_A aktual mahal (avg 120 > std 100) dan dipakai berlebih (12 > allowed).
        await SeedBalanceAsync(ctx, RM_A, qty: 1000m, avg: 120m);
        var svc = NewService(ctx);

        var order = await svc.CreateAsync(new UpsertProductionOrderDto(BomFgQ, PlannedQty: 10m, "B-QV", null, null, null));

        // Catat konsumsi aktual RM_A = 12 (planned 10).
        await svc.RecordConsumptionAsync(order.Id, new RecordConsumptionDto(
            new List<RecordConsumptionLineDto> { new(RM_A, ActualQty: 12m) }));

        // Tutup dengan keluaran aktual 8 (< planned 10) -> ada yield variance.
        var report = await svc.CloseAsync(order.Id, new CloseProductionDto(ActualQty: 8m));

        var line = Assert.Single(report.Lines);
        Assert.Equal(RM_A, line.ComponentId);
        Assert.Equal(120m, line.ActualUnitCost);
        Assert.Equal(100m, line.StdUnitCost);
        Assert.Equal(12m, line.ActualQty);
        Assert.Equal(8m, line.StdQtyAllowed);           // (10/10) * 8

        // PriceVariance = (120 - 100) * 12 = 240 (unfavorable, +).
        Assert.Equal(240m, line.PriceVariance);
        // UsageVariance = (12 - 8) * 100 = 400 (unfavorable, +).
        Assert.Equal(400m, line.UsageVariance);

        // materialCost = 12 * 120 = 1440; standardMaterial = 8 * 100 = 800.
        Assert.Equal(1440m, report.MaterialCost);
        Assert.Equal(640m, report.MaterialVariance);    // 240 + 400

        // standardTotalPerUnit = 150; yieldVariance = (10*1 - 8) * 150 = 300.
        Assert.Equal(1200m, report.StandardTotal);      // 150 * 8
        Assert.Equal(300m, report.YieldVariance);
    }

    [Fact]
    public async Task RecordConsumption_UnknownComponent_Throws400()
    {
        using var ctx = NewContext();
        var svc = NewService(ctx);

        var order = await svc.CreateAsync(new UpsertProductionOrderDto(BomFgQ, 10m, "B-Q1", null, null, null));
        var ex = await Assert.ThrowsAsync<BomServiceException>(() => svc.RecordConsumptionAsync(
            order.Id, new RecordConsumptionDto(new List<RecordConsumptionLineDto> { new(RM_C, 1m) })));
        Assert.Equal(400, ex.StatusCode);
    }

    // -------------------------------------------------------------- Ledger read

    [Fact]
    public async Task ListStockMovements_FiltersByItem()
    {
        using var ctx = NewContext();
        await SeedBalanceAsync(ctx, RM_A, qty: 1000m, avg: 100m);
        var svc = NewService(ctx);

        var order = await svc.CreateAsync(new UpsertProductionOrderDto(BomFgQ, 10m, "B-Q1", null, null, null));
        await svc.CloseAsync(order.Id, new CloseProductionDto(10m));

        var rmMoves = await svc.ListStockMovementsAsync(RM_A);
        Assert.All(rmMoves, m => Assert.Equal(RM_A, m.ItemId));
        Assert.Contains(rmMoves, m => m.Type == StockMovementType.ProductionIssue);

        var fgMoves = await svc.ListStockMovementsAsync(FG_Q);
        Assert.Contains(fgMoves, m => m.Type == StockMovementType.ProductionReceipt);
    }
}
