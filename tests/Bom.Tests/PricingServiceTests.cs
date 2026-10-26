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
/// Uji PricingService dengan DbContext InMemory. SalesChannels (Distributor 12% / Retail 20%) dan
/// PricingSetting (target 45% / PPN 11%) disemai lewat HasData saat EnsureCreated.
///
/// Reproduksi contoh roadmap (toleransi ±1 karena pembulatan per jenjang):
///   cost 4022, Margin 45% -> Ex-factory 7313; Distributor +12% -> 8191; Retail +20% -> 9829;
///   +PPN 11% -> HET 10910; ContributionMargin 3291; break-even (fixed 30.000.000) -> 9116.
///   PackLevel Botol×1 / Pack×12 / Karton×24 -> Ex-factory Pack 87.756, Karton 175.512.
/// </summary>
public class PricingServiceTests
{
    private const int FG_MILK = 20;   // cost 4022, punya PackLevel Botol/Pack/Karton
    private const int FG_MARKUP = 21;  // cost 1000, untuk uji Markup vs Margin
    private const int FG_COGS = 22;    // cost 4022 + ProductionReceipt terakhir @5000
    private const int UomPcs = 5;

    private static BomDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseInMemoryDatabase($"pricing-tests-{Guid.NewGuid()}")
            .Options;

        var ctx = new BomDbContext(options);
        ctx.Database.EnsureCreated(); // menyemai Uom 1..5, SalesChannels, PricingSetting

        ctx.Items.AddRange(
            new Item { Id = FG_MILK, Code = "FG-MILK", Name = "Susu", Type = ItemType.FinishedGood, BaseUomId = UomPcs, StandardMaterialCost = 4022m },
            new Item { Id = FG_MARKUP, Code = "FG-MK", Name = "Markup", Type = ItemType.FinishedGood, BaseUomId = UomPcs, StandardMaterialCost = 1000m },
            new Item { Id = FG_COGS, Code = "FG-COGS", Name = "Cogs", Type = ItemType.FinishedGood, BaseUomId = UomPcs, StandardMaterialCost = 4022m });

        ctx.PackLevels.AddRange(
            new PackLevel { FinishedItemId = FG_MILK, Name = "Botol", QtyPerBase = 1m, Sequence = 1, IsDefaultSales = true },
            new PackLevel { FinishedItemId = FG_MILK, Name = "Pack", QtyPerBase = 12m, Sequence = 2 },
            new PackLevel { FinishedItemId = FG_MILK, Name = "Karton", QtyPerBase = 24m, Sequence = 3 });

        // COGS terakhir untuk FG_COGS = UnitCost ProductionReceipt terbaru (5000), berbeda dari standar 4022.
        ctx.StockMovements.AddRange(
            new StockMovement { ItemId = FG_COGS, Type = StockMovementType.ProductionReceipt, Qty = 5m, UnitCost = 4500m, MovementDate = new DateOnly(2026, 1, 1) },
            new StockMovement { ItemId = FG_COGS, Type = StockMovementType.ProductionReceipt, Qty = 5m, UnitCost = 5000m, MovementDate = new DateOnly(2026, 2, 1) });

        ctx.SaveChanges();
        return ctx;
    }

    private static PricingService NewService(BomDbContext ctx) => new(ctx, new CostingService(ctx));

    private static SimulatePricingRequest MilkRequest(
        CostBasis basis = CostBasis.Standard,
        PricingMethod method = PricingMethod.Margin,
        decimal? fixedCost = null,
        int productId = FG_MILK,
        decimal? floorCost = null) =>
        new(
            ProductItemId: productId,
            PricingMethod: method,
            CostBasis: basis,
            TaxMode: TaxMode.Exclusive,
            TargetPercent: 0.45m,
            TaxPercent: 0.11m,
            FloorCost: floorCost,
            FixedCostPerPeriod: fixedCost);

    private static PricingCellDto Cell(PricingResultDto r, string tier, string pack) =>
        Assert.Single(r.Cells, c => c.TierName == tier && c.PackLevelName == pack);

    private static void AssertClose(decimal expected, decimal actual) =>
        Assert.True(Math.Abs(expected - actual) <= 1m, $"Diharapkan ~{expected}, ditemukan {actual}.");

    // ---------------------------------------------------- Contoh roadmap: jenjang

    [Fact]
    public async Task Simulate_Margin45_ReproducesRoadmapTiers()
    {
        using var ctx = NewContext();
        var svc = NewService(ctx);

        var r = await svc.SimulateAsync(MilkRequest(fixedCost: 30_000_000m));

        Assert.Equal(4022m, r.Cost);
        AssertClose(7313m, r.BasePrice);                              // Ex-factory
        Assert.Equal(new[] { "Ex-factory", "Distributor", "Retail", "HET" }, r.Tiers);

        AssertClose(7313m, Cell(r, "Ex-factory", "Botol").NetPrice);
        AssertClose(8191m, Cell(r, "Distributor", "Botol").NetPrice);
        AssertClose(9829m, Cell(r, "Retail", "Botol").NetPrice);
        AssertClose(10910m, Cell(r, "HET", "Botol").GrossPrice);      // konsumen, termasuk PPN

        AssertClose(3291m, r.ContributionMargin);                    // 7313 - 4022
        Assert.Equal(9116, r.BreakEvenQty);                          // ceil(30.000.000 / 3291)
        Assert.False(r.FloorViolation);
    }

    [Fact]
    public async Task Simulate_PackLevels_ScaleByQtyPerBase()
    {
        using var ctx = NewContext();
        var svc = NewService(ctx);

        var r = await svc.SimulateAsync(MilkRequest());

        // Ex-factory net per botol 7313 -> Pack (×12) 87.756, Karton (×24) 175.512.
        AssertClose(87_756m, Cell(r, "Ex-factory", "Pack").NetPrice);
        AssertClose(175_512m, Cell(r, "Ex-factory", "Karton").NetPrice);
    }

    // ---------------------------------------------------- Markup vs Margin

    [Fact]
    public async Task Simulate_MarkupVsMargin_DifferentBase()
    {
        using var ctx = NewContext();
        var svc = NewService(ctx);

        var markup = await svc.SimulateAsync(MilkRequest(method: PricingMethod.Markup, productId: FG_MARKUP));
        var margin = await svc.SimulateAsync(MilkRequest(method: PricingMethod.Margin, productId: FG_MARKUP));

        // Target 0,45 atas cost 1000: Markup = 1000*(1+0,45) = 1450; Margin = 1000/(1-0,45) = 1818.
        Assert.Equal(1450m, markup.BasePrice);
        Assert.Equal(1818m, margin.BasePrice);
        Assert.NotEqual(markup.BasePrice, margin.BasePrice);
    }

    // ---------------------------------------------------- Floor guard

    [Fact]
    public async Task Simulate_BaseBelowFloor_FlagsViolation()
    {
        using var ctx = NewContext();
        var svc = NewService(ctx);

        var violated = await svc.SimulateAsync(MilkRequest(floorCost: 9000m)); // base 7313 < 9000
        Assert.True(violated.FloorViolation);

        var ok = await svc.SimulateAsync(MilkRequest(floorCost: 5000m));       // base 7313 >= 5000
        Assert.False(ok.FloorViolation);
    }

    // ---------------------------------------------------- CostBasis LastCOGS

    [Fact]
    public async Task Simulate_LastCogs_UsesLatestProductionReceiptUnitCost()
    {
        using var ctx = NewContext();
        var svc = NewService(ctx);

        var standard = await svc.SimulateAsync(MilkRequest(basis: CostBasis.Standard, productId: FG_COGS));
        var lastCogs = await svc.SimulateAsync(MilkRequest(basis: CostBasis.LastCOGS, productId: FG_COGS));

        Assert.Equal(4022m, standard.Cost);   // biaya standar
        Assert.Equal(5000m, lastCogs.Cost);   // UnitCost ProductionReceipt terbaru (bukan 4500)
        Assert.NotEqual(standard.Cost, lastCogs.Cost);
    }

    [Fact]
    public async Task Simulate_LastCogs_FallsBackToStandard_WhenNoProduction()
    {
        using var ctx = NewContext();
        var svc = NewService(ctx);

        // FG_MILK belum pernah produksi -> LastCOGS jatuh ke biaya standar 4022.
        var lastCogs = await svc.SimulateAsync(MilkRequest(basis: CostBasis.LastCOGS, productId: FG_MILK));
        Assert.Equal(4022m, lastCogs.Cost);
    }

    // ---------------------------------------------------- Save + activate

    [Fact]
    public async Task SaveAndActivate_MakesOnlyOneActivePerProduct()
    {
        using var ctx = NewContext();
        var svc = NewService(ctx);

        var save = new SavePriceListRequest(MilkRequest(), Notes: "v1");
        var first = await svc.CreatePriceListAsync(save);
        Assert.Equal(PriceListStatus.Draft, first.Status);
        Assert.NotEmpty(first.Lines);

        var activated = await svc.ActivatePriceListAsync(first.Id);
        Assert.Equal(PriceListStatus.Active, activated.Status);

        // Buat & aktifkan versi kedua -> yang pertama menjadi Obsolete.
        var second = await svc.CreatePriceListAsync(save);
        await svc.ActivatePriceListAsync(second.Id);

        var reloadFirst = await svc.GetPriceListAsync(first.Id);
        var reloadSecond = await svc.GetPriceListAsync(second.Id);
        Assert.Equal(PriceListStatus.Obsolete, reloadFirst!.Status);
        Assert.Equal(PriceListStatus.Active, reloadSecond!.Status);

        var actives = await svc.ListPriceListsAsync(FG_MILK, PriceListStatus.Active);
        Assert.Single(actives);
    }

    [Fact]
    public async Task Save_WithoutPackLevels_Throws400()
    {
        using var ctx = NewContext();
        var svc = NewService(ctx);

        // FG_MARKUP tidak punya PackLevel -> tidak dapat disimpan.
        var ex = await Assert.ThrowsAsync<BomServiceException>(() =>
            svc.CreatePriceListAsync(new SavePriceListRequest(MilkRequest(productId: FG_MARKUP))));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Simulate_UnknownProduct_Throws404()
    {
        using var ctx = NewContext();
        var svc = NewService(ctx);

        var ex = await Assert.ThrowsAsync<BomServiceException>(() =>
            svc.SimulateAsync(MilkRequest(productId: 999)));
        Assert.Equal(404, ex.StatusCode);
    }
}
