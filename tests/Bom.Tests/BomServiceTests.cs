using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Bom.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using BomEntity = Bom.Domain.Entities.Bom;

namespace Bom.Tests;

/// <summary>
/// Uji unit BomService memakai DbContext InMemory: deteksi siklus, explode multi-level,
/// dan transisi status saat aktivasi.
/// </summary>
public class BomServiceTests
{
    private static BomDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseInMemoryDatabase($"bom-tests-{Guid.NewGuid()}")
            .Options;

        var ctx = new BomDbContext(options);
        // Menyemai master data (Uom 1..5, dsb.) sekaligus membuat store InMemory.
        ctx.Database.EnsureCreated();

        // Item uji: semua base UoM = kg (id 1) agar komponen sedimensi.
        ctx.Items.AddRange(
            new Item { Id = 1, Code = "FG-A", Name = "Produk A", Type = ItemType.FinishedGood, BaseUomId = 1 },
            new Item { Id = 2, Code = "SF-B", Name = "Semi B", Type = ItemType.SemiFinished, BaseUomId = 1 },
            new Item { Id = 3, Code = "RM-C", Name = "Bahan C", Type = ItemType.RawMaterial, BaseUomId = 1 });
        ctx.SaveChanges();
        return ctx;
    }

    // ------------------------------------------------------- (a) cycle detection

    [Fact]
    public void WouldCreateCycle_SelfReference_IsTrue()
    {
        using var ctx = NewContext();
        var service = new BomService(ctx);

        // Produk 1 dengan komponen 1 (dirinya sendiri) = siklus.
        Assert.True(service.WouldCreateCycle(productItemId: 1, componentItemIds: new[] { 1 }));
    }

    [Fact]
    public void WouldCreateCycle_Indirect_AToBToA_IsTrue()
    {
        using var ctx = NewContext();

        // BOM Active untuk produk B (id 2) yang memakai komponen A (id 1).
        ctx.Boms.Add(new BomEntity
        {
            Id = 100,
            ProductItemId = 2,
            Version = 1,
            Status = BomStatus.Active,
            OutputQuantity = 1m,
            OutputUomId = 1,
            YieldPercent = 1m,
            Components =
            {
                new BomComponent { ComponentItemId = 1, Quantity = 1m, UomId = 1, Sequence = 1 }
            }
        });
        ctx.SaveChanges();

        var service = new BomService(ctx);

        // Mengaktifkan BOM produk A (id 1) dengan komponen B (id 2) menutup siklus A->B->A.
        Assert.True(service.WouldCreateCycle(productItemId: 1, componentItemIds: new[] { 2 }));

        // Komponen tanpa BOM (id 3) tidak membentuk siklus.
        Assert.False(service.WouldCreateCycle(productItemId: 1, componentItemIds: new[] { 3 }));
    }

    // ------------------------------------------------------- (b) explode 2 level

    [Fact]
    public async Task ExplodeAsync_TwoLevels_ComputesEffectiveQuantity()
    {
        using var ctx = NewContext();

        // BOM_S (Active) untuk Semi B: output 1, komponen R (id 3) qty 3, tanpa scrap.
        ctx.Boms.Add(new BomEntity
        {
            Id = 200,
            ProductItemId = 2,
            Version = 1,
            Status = BomStatus.Active,
            OutputQuantity = 1m,
            OutputUomId = 1,
            YieldPercent = 1m,
            Components =
            {
                new BomComponent { ComponentItemId = 3, Quantity = 3m, UomId = 1, ScrapPercent = 0m, Sequence = 1 }
            }
        });

        // BOM_A (Draft) untuk Produk A: output 1, komponen S (id 2) qty 2, scrap 10%.
        ctx.Boms.Add(new BomEntity
        {
            Id = 201,
            ProductItemId = 1,
            Version = 1,
            Status = BomStatus.Draft,
            OutputQuantity = 1m,
            OutputUomId = 1,
            YieldPercent = 1m,
            Components =
            {
                new BomComponent { ComponentItemId = 2, Quantity = 2m, UomId = 1, ScrapPercent = 0.1m, Sequence = 1 }
            }
        });
        ctx.SaveChanges();

        var service = new BomService(ctx);
        var root = await service.ExplodeAsync(201, qty: 1m);

        Assert.Equal(0, root.Level);
        Assert.Equal(1m, root.Quantity);
        var semi = Assert.Single(root.Children);

        // Level 1: S = 2 * (1 + 0.1) = 2.2
        Assert.Equal(2, semi.ItemId);
        Assert.Equal(1, semi.Level);
        Assert.True(semi.HasBom);
        Assert.Equal(2.2m, semi.EffectiveQuantity);

        // Level 2: R = 3 * (childScale = 2.2 / 1) = 6.6
        var raw = Assert.Single(semi.Children);
        Assert.Equal(3, raw.ItemId);
        Assert.Equal(2, raw.Level);
        Assert.False(raw.HasBom);
        Assert.Equal(6.6m, raw.EffectiveQuantity);
    }

    // ------------------------------------------------------- (c) activate flow

    [Fact]
    public async Task ActivateAsync_MovesPreviousActiveToObsolete()
    {
        using var ctx = NewContext();

        // Versi 1 sudah Active, versi 2 masih Draft (produk A / id 1).
        ctx.Boms.Add(new BomEntity
        {
            Id = 300,
            ProductItemId = 1,
            Version = 1,
            Status = BomStatus.Active,
            OutputQuantity = 1m,
            OutputUomId = 1,
            YieldPercent = 1m,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            Components =
            {
                new BomComponent { ComponentItemId = 3, Quantity = 1m, UomId = 1, Sequence = 1 }
            }
        });
        ctx.Boms.Add(new BomEntity
        {
            Id = 301,
            ProductItemId = 1,
            Version = 2,
            Status = BomStatus.Draft,
            OutputQuantity = 1m,
            OutputUomId = 1,
            YieldPercent = 1m,
            Components =
            {
                new BomComponent { ComponentItemId = 3, Quantity = 2m, UomId = 1, Sequence = 1 }
            }
        });
        ctx.SaveChanges();

        var service = new BomService(ctx);
        var activated = await service.ActivateAsync(301);

        Assert.Equal(BomStatus.Active, activated.Status);
        Assert.NotNull(activated.EffectiveFrom);

        var oldVersion = await ctx.Boms.AsNoTracking().FirstAsync(b => b.Id == 300);
        var newVersion = await ctx.Boms.AsNoTracking().FirstAsync(b => b.Id == 301);
        Assert.Equal(BomStatus.Obsolete, oldVersion.Status);
        Assert.Equal(BomStatus.Active, newVersion.Status);
    }
}
