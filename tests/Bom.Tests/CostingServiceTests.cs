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
/// Uji CostingService dengan DbContext InMemory. Data sampel mereproduksi contoh rencana
/// (Base Kopi dan Kopi Susu Botol) dengan toleransi +/- 1%.
///
/// UoM yang disemai (EnsureCreated -> HasData): kg=1, g=2, L=3, ml=4, pcs=5.
/// </summary>
public class CostingServiceTests
{
    // --- Item ids ---
    private const int KopiBubuk = 10;
    private const int Air = 11;
    private const int Gula = 12;
    private const int Susu = 13;
    private const int GulaCair = 14;
    private const int BotolPet = 15;
    private const int Cap = 16;
    private const int Label = 17;
    private const int BaseKopi = 20;   // semi-finished, base uom L
    private const int KopiSusu = 30;   // finished, base uom pcs (botol)

    // --- Uom ids (dari seed) ---
    private const int UomKg = 1;
    private const int UomG = 2;
    private const int UomL = 3;
    private const int UomPcs = 5;

    private static BomDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseInMemoryDatabase($"costing-tests-{Guid.NewGuid()}")
            .Options;

        var ctx = new BomDbContext(options);
        ctx.Database.EnsureCreated(); // menyemai Uom 1..5

        ctx.Suppliers.Add(new Supplier { Id = 1, Code = "SUP-1", Name = "Supplier Utama" });

        // Bahan baku & kemasan (item "dibeli").
        ctx.Items.AddRange(
            new Item { Id = KopiBubuk, Code = "RM-KOPI", Name = "Kopi Bubuk", Type = ItemType.RawMaterial, BaseUomId = UomKg, StandardMaterialCost = 0m },
            new Item { Id = Air, Code = "RM-AIR", Name = "Air", Type = ItemType.RawMaterial, BaseUomId = UomL, StandardMaterialCost = 50m },
            new Item { Id = Gula, Code = "RM-GULA", Name = "Gula", Type = ItemType.RawMaterial, BaseUomId = UomKg, StandardMaterialCost = 14000m },
            new Item { Id = Susu, Code = "RM-SUSU", Name = "Susu", Type = ItemType.RawMaterial, BaseUomId = UomL, StandardMaterialCost = 15000m },
            new Item { Id = GulaCair, Code = "RM-GULACAIR", Name = "Gula Cair", Type = ItemType.RawMaterial, BaseUomId = UomL, StandardMaterialCost = 10000m },
            new Item { Id = BotolPet, Code = "PK-BOTOL", Name = "Botol PET 250ml", Type = ItemType.Packaging, BaseUomId = UomPcs, StandardMaterialCost = 800m },
            new Item { Id = Cap, Code = "PK-CAP", Name = "Cap", Type = ItemType.Packaging, BaseUomId = UomPcs, StandardMaterialCost = 150m },
            new Item { Id = Label, Code = "PK-LABEL", Name = "Label", Type = ItemType.Packaging, BaseUomId = UomPcs, StandardMaterialCost = 100m },
            new Item { Id = BaseKopi, Code = "SF-BASEKOPI", Name = "Base Kopi", Type = ItemType.SemiFinished, BaseUomId = UomL },
            new Item { Id = KopiSusu, Code = "FG-KOPISUSU", Name = "Kopi Susu Botol 250ml", Type = ItemType.FinishedGood, BaseUomId = UomPcs });

        // Harga preferred kopi bubuk: 120 / g -> per kg = 120 / 0.001 = 120.000 (menguji preferred + konversi UoM).
        ctx.ItemSupplierPrices.Add(new ItemSupplierPrice
        {
            Id = 1,
            ItemId = KopiBubuk,
            SupplierId = 1,
            UomId = UomG,
            Price = 120m,
            IsPreferred = true
        });

        // BOM Base Kopi: output 1 L, yield 100%.
        ctx.Boms.Add(new BomEntity
        {
            Id = 100,
            ProductItemId = BaseKopi,
            Version = 1,
            Status = BomStatus.Active,
            OutputQuantity = 1m,
            OutputUomId = UomL,
            YieldPercent = 1m,
            Components =
            {
                new BomComponent { ComponentItemId = KopiBubuk, Quantity = 0.05m, UomId = UomKg, ScrapPercent = 0m, Sequence = 1 },
                new BomComponent { ComponentItemId = Air, Quantity = 0.95m, UomId = UomL, ScrapPercent = 0m, Sequence = 2 },
                new BomComponent { ComponentItemId = Gula, Quantity = 0.03m, UomId = UomKg, ScrapPercent = 0m, Sequence = 3 }
            }
        });

        // BOM Kopi Susu: output 100 botol, yield 98%, labor+overhead 500/botol.
        ctx.Boms.Add(new BomEntity
        {
            Id = 200,
            ProductItemId = KopiSusu,
            Version = 3,
            Status = BomStatus.Active,
            OutputQuantity = 100m,
            OutputUomId = UomPcs,
            YieldPercent = 0.98m,
            Components =
            {
                new BomComponent { ComponentItemId = BaseKopi, Quantity = 15m, UomId = UomL, ScrapPercent = 0.01m, Sequence = 1 },
                new BomComponent { ComponentItemId = Susu, Quantity = 8m, UomId = UomL, ScrapPercent = 0.005m, Sequence = 2 },
                new BomComponent { ComponentItemId = GulaCair, Quantity = 2m, UomId = UomL, ScrapPercent = 0m, Sequence = 3 },
                new BomComponent { ComponentItemId = BotolPet, Quantity = 100m, UomId = UomPcs, ScrapPercent = 0.02m, Sequence = 4 },
                new BomComponent { ComponentItemId = Cap, Quantity = 100m, UomId = UomPcs, ScrapPercent = 0m, Sequence = 5 },
                new BomComponent { ComponentItemId = Label, Quantity = 100m, UomId = UomPcs, ScrapPercent = 0m, Sequence = 6 }
            },
            Operations =
            {
                new BomOperation { Sequence = 1, Name = "Mixing & Filling", LaborCostPerOutputUnit = 300m, OverheadRate = 200m }
            }
        });

        ctx.SaveChanges();
        return ctx;
    }

    private static void AssertClose(decimal expected, decimal actual, double tolerance = 0.01)
    {
        var allowed = Math.Abs((double)expected) * tolerance;
        var diff = Math.Abs((double)(actual - expected));
        Assert.True(diff <= allowed, $"Diharapkan ~{expected}, diperoleh {actual} (selisih {diff}, toleransi {allowed}).");
    }

    // --------------------------------------------------------- Base Kopi (semi-finished)

    [Fact]
    public async Task Breakdown_BaseKopi_IsAboutSixThousandFourHundred()
    {
        using var ctx = NewContext();
        var service = new CostingService(ctx);

        var bd = await service.GetCostBreakdownAsync(BaseKopi);

        Assert.False(bd.IsPurchased);
        Assert.Equal("L", bd.OutputUomCode);
        AssertClose(6467.5m, bd.MaterialCost);
        AssertClose(6467.5m, bd.TotalCost);

        // Preferred price (g -> kg) menghasilkan 120.000 / kg untuk kopi bubuk.
        var kopiLine = Assert.Single(bd.Components, c => c.ItemId == KopiBubuk);
        AssertClose(120000m, kopiLine.UnitCost);

        // Air memakai fallback StandardMaterialCost = 50 / L.
        var airLine = Assert.Single(bd.Components, c => c.ItemId == Air);
        AssertClose(50m, airLine.UnitCost);
    }

    // --------------------------------------------------------- Kopi Susu (finished good)

    [Fact]
    public async Task Breakdown_KopiSusu_IsAboutFourThousand()
    {
        using var ctx = NewContext();
        var service = new CostingService(ctx);

        var bd = await service.GetCostBreakdownAsync(KopiSusu);

        Assert.False(bd.IsPurchased);
        Assert.Equal("pcs", bd.OutputUomCode);
        Assert.Equal(6, bd.Components.Count);

        AssertClose(3522.27m, bd.MaterialCost);
        Assert.Equal(300m, bd.LaborCost);
        Assert.Equal(200m, bd.OverheadCost);
        AssertClose(4022m, bd.TotalCost);

        // Baris Base Kopi bersifat ber-BOM dan membawa anak (sub-komponennya).
        var baseKopiLine = Assert.Single(bd.Components, c => c.ItemId == BaseKopi);
        Assert.True(baseKopiLine.HasBom);
        Assert.NotNull(baseKopiLine.Children);
        Assert.Equal(3, baseKopiLine.Children!.Count);
        AssertClose(6467.5m, baseKopiLine.UnitCost);

        // Sum ExtendedCost komponen = MaterialCost (konsistensi pohon).
        var sumExtended = bd.Components.Sum(c => c.ExtendedCost);
        AssertClose(bd.MaterialCost, sumExtended);
    }

    // --------------------------------------------------------- Save snapshot

    [Fact]
    public async Task SaveSnapshot_PersistsTotalsAndSource()
    {
        using var ctx = NewContext();
        var service = new CostingService(ctx);

        var bd = await service.SaveSnapshotAsync(KopiSusu);
        AssertClose(4022m, bd.TotalCost);

        var snapshots = await service.ListSnapshotsAsync(KopiSusu);
        var snap = Assert.Single(snapshots);
        AssertClose(4022m, snap.TotalCost);
        AssertClose(3522.27m, snap.MaterialCost);
        Assert.Equal(200, snap.SourceBomId);
        Assert.Equal(3, snap.BomVersion);
        Assert.Equal(UomPcs, snap.OutputUomId);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), snap.AsOfDate);
    }

    // --------------------------------------------------------- Purchased item

    [Fact]
    public async Task Breakdown_PurchasedItem_UsesFallbackStandardCost()
    {
        using var ctx = NewContext();
        var service = new CostingService(ctx);

        var bd = await service.GetCostBreakdownAsync(Gula);

        Assert.True(bd.IsPurchased);
        Assert.Empty(bd.Components);
        Assert.Equal("kg", bd.OutputUomCode);
        Assert.Equal(14000m, bd.TotalCost);
        Assert.Equal(14000m, bd.MaterialCost);
    }

    // --------------------------------------------------------- Not found

    [Fact]
    public async Task Breakdown_UnknownItem_Throws404()
    {
        using var ctx = NewContext();
        var service = new CostingService(ctx);

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.GetCostBreakdownAsync(9999));
        Assert.Equal(404, ex.StatusCode);
    }

    // --------------------------------------------------------- Cycle -> 409

    [Fact]
    public async Task Breakdown_CyclicBoms_Throws409()
    {
        using var ctx = NewContext();

        // A (40) dan B (41) saling memakai lewat BOM Active -> siklus.
        ctx.Items.AddRange(
            new Item { Id = 40, Code = "SF-A", Name = "Semi A", Type = ItemType.SemiFinished, BaseUomId = UomKg },
            new Item { Id = 41, Code = "SF-B", Name = "Semi B", Type = ItemType.SemiFinished, BaseUomId = UomKg });

        ctx.Boms.Add(new BomEntity
        {
            Id = 400,
            ProductItemId = 40,
            Version = 1,
            Status = BomStatus.Active,
            OutputQuantity = 1m,
            OutputUomId = UomKg,
            YieldPercent = 1m,
            Components = { new BomComponent { ComponentItemId = 41, Quantity = 1m, UomId = UomKg, Sequence = 1 } }
        });
        ctx.Boms.Add(new BomEntity
        {
            Id = 401,
            ProductItemId = 41,
            Version = 1,
            Status = BomStatus.Active,
            OutputQuantity = 1m,
            OutputUomId = UomKg,
            YieldPercent = 1m,
            Components = { new BomComponent { ComponentItemId = 40, Quantity = 1m, UomId = UomKg, Sequence = 1 } }
        });
        ctx.SaveChanges();

        var service = new CostingService(ctx);
        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.GetCostBreakdownAsync(40));
        Assert.Equal(409, ex.StatusCode);
    }
}
