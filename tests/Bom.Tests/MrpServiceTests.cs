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
/// Uji MrpService dengan DbContext InMemory. Mereproduksi struktur Kopi Susu (Fase 3) dan
/// menyemai sebagian on-hand (Base Kopi 5 L, Botol PET 30 pcs) untuk menguji netting rekursif,
/// klasifikasi Buy/Make, dan pembulatan MOQ.
///
/// UoM yang disemai (EnsureCreated -> HasData): kg=1, g=2, L=3, ml=4, pcs=5.
/// </summary>
public class MrpServiceTests
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
            .UseInMemoryDatabase($"mrp-tests-{Guid.NewGuid()}")
            .Options;

        var ctx = new BomDbContext(options);
        ctx.Database.EnsureCreated(); // menyemai Uom 1..5

        ctx.Suppliers.Add(new Supplier { Id = 1, Code = "SUP-1", Name = "Supplier Utama", LeadTimeDays = 7 });

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

        // Preferred price kopi bubuk: 120 / g -> 120.000 / kg (menguji preferred + konversi UoM).
        ctx.ItemSupplierPrices.Add(new ItemSupplierPrice
        {
            Id = 1,
            ItemId = KopiBubuk,
            SupplierId = 1,
            UomId = UomG,
            Price = 120m,
            IsPreferred = true
        });

        // Preferred price Botol PET: 800 / pcs, MOQ 50 pcs (menguji pembulatan MOQ pada saran PO).
        ctx.ItemSupplierPrices.Add(new ItemSupplierPrice
        {
            Id = 2,
            ItemId = BotolPet,
            SupplierId = 1,
            UomId = UomPcs,
            Price = 800m,
            MinOrderQty = 50m,
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

        // BOM Kopi Susu: output 100 botol, yield 98%.
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
            }
        });

        // On-hand parsial (netting): Base Kopi 5 L, Botol PET 30 pcs.
        ctx.InventoryBalances.AddRange(
            new InventoryBalance { Id = 1, ItemId = BaseKopi, QtyOnHand = 5m },
            new InventoryBalance { Id = 2, ItemId = BotolPet, QtyOnHand = 30m });

        ctx.SaveChanges();
        return ctx;
    }

    private static MrpRequestDto Demand(int productItemId, decimal quantity)
        => new(new[] { new MrpDemandInputDto(productItemId, quantity) });

    private static void AssertClose(decimal expected, decimal actual, double tolerance = 0.01)
    {
        var allowed = Math.Abs((double)expected) * tolerance;
        var diff = Math.Abs((double)(actual - expected));
        Assert.True(diff <= allowed, $"Diharapkan ~{expected}, diperoleh {actual} (selisih {diff}, toleransi {allowed}).");
    }

    // --------------------------------------------------------- Top demand tidak di-net

    [Fact]
    public async Task Explode_TopDemand_IsMakeAndNotNetted()
    {
        using var ctx = NewContext();
        var service = new MrpService(ctx);

        var result = await service.ExplodeAsync(Demand(KopiSusu, 100m));

        var kopiSusu = Assert.Single(result.Lines, l => l.ItemId == KopiSusu);
        Assert.Equal("Make", kopiSusu.Source);
        Assert.Equal(100m, kopiSusu.GrossQty);
        Assert.Equal(0m, kopiSusu.OnHandQty); // demand top-level tidak di-net
        Assert.Equal(100m, kopiSusu.NetQty);
        Assert.Equal("pcs", kopiSusu.UomCode);
        Assert.Null(kopiSusu.SuggestedOrderQty); // Make tidak punya saran PO

        // Demand di-echo dengan kode/nama/uom.
        var demand = Assert.Single(result.Demands);
        Assert.Equal(KopiSusu, demand.ProductItemId);
        Assert.Equal("pcs", demand.UomCode);
        Assert.Equal(100m, demand.Quantity);
    }

    // --------------------------------------------------------- Netting rekursif: net = gross - onHand

    [Fact]
    public async Task Explode_LowerLevel_NetsAgainstOnHand()
    {
        using var ctx = NewContext();
        var service = new MrpService(ctx);

        var result = await service.ExplodeAsync(Demand(KopiSusu, 100m));

        // Base Kopi gross = 100 * (15 * 1.01) / (100 * 0.98) = 15.4592 L; on-hand 5 -> net 10.4592.
        var baseKopi = Assert.Single(result.Lines, l => l.ItemId == BaseKopi);
        Assert.Equal("Make", baseKopi.Source);
        AssertClose(15.45918m, baseKopi.GrossQty);
        Assert.Equal(5m, baseKopi.OnHandQty);
        AssertClose(10.45918m, baseKopi.NetQty);
        Assert.Equal(baseKopi.GrossQty - baseKopi.OnHandQty, baseKopi.NetQty);

        // Botol PET gross = 100 * (100 * 1.02) / 98 = 104.0816 pcs; on-hand 30 -> net 74.0816.
        var botol = Assert.Single(result.Lines, l => l.ItemId == BotolPet);
        Assert.Equal("Buy", botol.Source);
        AssertClose(104.08163m, botol.GrossQty);
        Assert.Equal(30m, botol.OnHandQty);
        AssertClose(74.08163m, botol.NetQty);
        Assert.Equal(botol.GrossQty - botol.OnHandQty, botol.NetQty);
    }

    // --------------------------------------------------------- Base Kopi netto ter-explode ke raw-nya

    [Fact]
    public async Task Explode_NettedSemiFinished_ExplodesRemainderToRawMaterials()
    {
        using var ctx = NewContext();
        var service = new MrpService(ctx);

        var result = await service.ExplodeAsync(Demand(KopiSusu, 100m));

        // Base Kopi net = 10.4592 L. BOM output 1 L, yield 1 -> raw = net * qty komponen.
        var netBaseKopi = Assert.Single(result.Lines, l => l.ItemId == BaseKopi).NetQty;

        // Kopi Bubuk: 0.05 kg/L * netBaseKopi (tidak ada on-hand).
        var kopi = Assert.Single(result.Lines, l => l.ItemId == KopiBubuk);
        Assert.Equal("Buy", kopi.Source);
        AssertClose(netBaseKopi * 0.05m, kopi.NetQty);
        AssertClose(120000m, kopi.UnitCost!.Value); // preferred g -> kg

        // Air: 0.95 L/L, fallback StandardMaterialCost 50/L.
        var air = Assert.Single(result.Lines, l => l.ItemId == Air);
        AssertClose(netBaseKopi * 0.95m, air.NetQty);
        AssertClose(50m, air.UnitCost!.Value);

        // Gula: 0.03 kg/L.
        var gula = Assert.Single(result.Lines, l => l.ItemId == Gula);
        AssertClose(netBaseKopi * 0.03m, gula.NetQty);
    }

    // --------------------------------------------------------- MOQ + lead time pada saran PO

    [Fact]
    public async Task Explode_BuyItem_RoundsSuggestedOrderQtyToMoq()
    {
        using var ctx = NewContext();
        var service = new MrpService(ctx);

        var result = await service.ExplodeAsync(Demand(KopiSusu, 100m));

        var botol = Assert.Single(result.Lines, l => l.ItemId == BotolPet);
        // net 74.0816, MOQ 50 -> Ceil(74.08 / 50) * 50 = 100 pcs.
        Assert.Equal(100m, botol.SuggestedOrderQty);
        Assert.Equal(50m, botol.MinOrderQty);
        Assert.Equal(800m, botol.UnitCost);
        Assert.Equal(80000m, botol.ExtendedCost); // 100 * 800
        Assert.Equal(1, botol.SupplierId);
        Assert.Equal("Supplier Utama", botol.SupplierName);
        Assert.Equal(7, botol.LeadTimeDays);

        // Item tanpa MOQ: suggested = net (Cap std cost 150, tidak ada preferred).
        var cap = Assert.Single(result.Lines, l => l.ItemId == Cap);
        Assert.Equal("Buy", cap.Source);
        Assert.Null(cap.MinOrderQty);
        Assert.Equal(cap.NetQty, cap.SuggestedOrderQty);
        AssertClose(150m, cap.UnitCost!.Value);
    }

    // --------------------------------------------------------- Klasifikasi Make/Buy & total biaya

    [Fact]
    public async Task Explode_ClassifiesMakeAndBuy_AndTotalsPurchaseCost()
    {
        using var ctx = NewContext();
        var service = new MrpService(ctx);

        var result = await service.ExplodeAsync(Demand(KopiSusu, 100m));

        // Make = item ber-BOM Active (Kopi Susu, Base Kopi); Buy = 8 sisanya.
        var make = result.Lines.Where(l => l.Source == "Make").Select(l => l.ItemId).ToHashSet();
        Assert.Equal(new HashSet<int> { KopiSusu, BaseKopi }, make);

        var buy = result.Lines.Where(l => l.Source == "Buy").Select(l => l.ItemId).ToHashSet();
        Assert.Equal(new HashSet<int> { KopiBubuk, Air, Gula, Susu, GulaCair, BotolPet, Cap, Label }, buy);

        // Make diurutkan sebelum Buy.
        var firstBuyIndex = result.Lines.ToList().FindIndex(l => l.Source == "Buy");
        var lastMakeIndex = result.Lines.ToList().FindLastIndex(l => l.Source == "Make");
        Assert.True(lastMakeIndex < firstBuyIndex);

        // Total biaya pembelian = Σ ExtendedCost baris Buy, dan bernilai wajar (~316.624).
        var expectedTotal = result.Lines.Where(l => l.Source == "Buy").Sum(l => l.ExtendedCost ?? 0m);
        Assert.Equal(expectedTotal, result.TotalPurchaseCost);
        AssertClose(316624m, result.TotalPurchaseCost);
    }

    // --------------------------------------------------------- Multi-demand agregasi

    [Fact]
    public async Task Explode_MultipleDemands_AggregatesRequirements()
    {
        using var ctx = NewContext();
        var service = new MrpService(ctx);

        var request = new MrpRequestDto(new[]
        {
            new MrpDemandInputDto(KopiSusu, 100m),
            new MrpDemandInputDto(BaseKopi, 3m) // permintaan tambahan Base Kopi langsung
        });

        var result = await service.ExplodeAsync(request);

        Assert.Equal(2, result.Demands.Count);

        // Base Kopi gross = 15.4592 (dari Kopi Susu) + 3 (demand langsung) = 18.4592.
        var baseKopi = Assert.Single(result.Lines, l => l.ItemId == BaseKopi);
        AssertClose(18.45918m, baseKopi.GrossQty);
        // Demand top-level Base Kopi tidak di-net; hanya cabang dari Kopi Susu yang di-net (on-hand 5).
        Assert.Equal(5m, baseKopi.OnHandQty);
        AssertClose(13.45918m, baseKopi.NetQty);
    }

    // --------------------------------------------------------- Validasi

    [Fact]
    public async Task Explode_EmptyDemands_Throws400()
    {
        using var ctx = NewContext();
        var service = new MrpService(ctx);

        var ex = await Assert.ThrowsAsync<BomServiceException>(
            () => service.ExplodeAsync(new MrpRequestDto(Array.Empty<MrpDemandInputDto>())));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Explode_NonProductDemand_Throws400()
    {
        using var ctx = NewContext();
        var service = new MrpService(ctx);

        // Gula bertipe RawMaterial -> bukan produk BOM.
        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.ExplodeAsync(Demand(Gula, 10m)));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Explode_UnknownItem_Throws404()
    {
        using var ctx = NewContext();
        var service = new MrpService(ctx);

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.ExplodeAsync(Demand(9999, 10m)));
        Assert.Equal(404, ex.StatusCode);
    }

    // --------------------------------------------------------- Anti-siklus

    [Fact]
    public async Task Explode_CyclicBoms_Throws409()
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

        var service = new MrpService(ctx);
        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.ExplodeAsync(Demand(40, 10m)));
        Assert.Equal(409, ex.StatusCode);
    }
}
