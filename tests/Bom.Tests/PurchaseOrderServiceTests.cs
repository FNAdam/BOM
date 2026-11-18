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
/// Uji PurchaseOrderService dengan DbContext InMemory (UoM 1..5 disemai lewat EnsureCreated).
/// Dataset: Supplier SUP-1; Item RM_A (raw, base kg).
/// </summary>
public class PurchaseOrderServiceTests
{
    private const int RM_A = 10;
    private const int RM_B = 11;
    private const int SupplierId = 1;
    private const int UomKg = 1;
    private const int UomG = 2;

    private static BomDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseInMemoryDatabase($"po-tests-{Guid.NewGuid()}")
            .Options;

        var ctx = new BomDbContext(options);
        ctx.Database.EnsureCreated(); // menyemai Uom 1..5

        ctx.Suppliers.Add(new Supplier { Id = SupplierId, Code = "SUP-1", Name = "Supplier Utama" });
        ctx.Items.AddRange(
            new Item { Id = RM_A, Code = "RM-A", Name = "Raw A", Type = ItemType.RawMaterial, BaseUomId = UomKg, StandardMaterialCost = 100m },
            new Item { Id = RM_B, Code = "RM-B", Name = "Raw B", Type = ItemType.RawMaterial, BaseUomId = UomKg, StandardMaterialCost = 200m });

        ctx.SaveChanges();
        return ctx;
    }

    private static PurchaseOrderService NewService(BomDbContext ctx) => new(ctx, new InventoryLedgerService(ctx));

    private static UpsertPurchaseOrderDto NewPoDto(decimal qty = 100m, decimal price = 10m) => new(
        SupplierId, "PO-0001", DateOnly.FromDateTime(DateTime.Today), null,
        new List<UpsertPurchaseOrderLineDto> { new(RM_A, UomKg, qty, price) });

    [Fact]
    public async Task Create_MenyimpanHeaderDanBaris_StatusDraft()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);

        var po = await service.CreateAsync(NewPoDto());

        Assert.Equal(PurchaseOrderStatus.Draft, po.Status);
        Assert.Single(po.Lines);
        Assert.Equal(100m, po.Lines[0].QtyOrdered);
        Assert.Equal(0m, po.Lines[0].QtyReceived);
    }

    [Fact]
    public async Task Create_SupplierTidakAda_MelemparNotFound()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var dto = NewPoDto() with { SupplierId = 999 };

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.CreateAsync(dto));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task Update_SetelahDikirim_MelemparConflict()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var po = await service.CreateAsync(NewPoDto());
        await service.SendAsync(po.Id);

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.UpdateAsync(po.Id, NewPoDto()));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Receive_PenuhSatuKali_StatusReceivedDanStokBertambah()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var po = await service.CreateAsync(NewPoDto(qty: 100m, price: 10m));
        await service.SendAsync(po.Id);

        var result = await service.ReceiveAsync(po.Id, new ReceivePurchaseOrderDto(
            new List<ReceivePurchaseOrderLineDto> { new(po.Lines[0].Id, 100m) }));

        Assert.Equal(PurchaseOrderStatus.Received, result.Status);
        Assert.Equal(100m, result.Lines[0].QtyReceived);

        var balance = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == RM_A);
        Assert.Equal(100m, balance.QtyOnHand);
        Assert.Equal(10m, balance.AverageCost);

        var movement = await ctx.StockMovements.FirstAsync(m => m.ItemId == RM_A);
        Assert.Equal(StockMovementType.Receipt, movement.Type);
        Assert.Equal("PurchaseOrder", movement.RefType);
        Assert.Equal(po.Id, movement.RefId);
    }

    [Fact]
    public async Task Receive_Bertahap_StatusPartiallyReceivedLaluReceived_RataRataBenar()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var po = await service.CreateAsync(NewPoDto(qty: 100m, price: 10m));
        await service.SendAsync(po.Id);

        var afterFirst = await service.ReceiveAsync(po.Id, new ReceivePurchaseOrderDto(
            new List<ReceivePurchaseOrderLineDto> { new(po.Lines[0].Id, 40m) }));
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, afterFirst.Status);
        Assert.Equal(40m, afterFirst.Lines[0].QtyReceived);

        var afterSecond = await service.ReceiveAsync(po.Id, new ReceivePurchaseOrderDto(
            new List<ReceivePurchaseOrderLineDto> { new(po.Lines[0].Id, 60m) }));
        Assert.Equal(PurchaseOrderStatus.Received, afterSecond.Status);
        Assert.Equal(100m, afterSecond.Lines[0].QtyReceived);

        var balance = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == RM_A);
        Assert.Equal(100m, balance.QtyOnHand);
        Assert.Equal(10m, balance.AverageCost); // harga sama di kedua penerimaan -> rata-rata tetap 10
    }

    [Fact]
    public async Task Receive_HargaBeda_RataRataBergerakTerhitungBenar()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        // Saldo awal 10kg @ 100 (lewat Adjustment manual di ledger).
        var ledger = new InventoryLedgerService(ctx);
        await ledger.ApplyMovementAsync(RM_A, StockMovementType.Adjustment, 10m, 100m,
            DateOnly.FromDateTime(DateTime.Today), null, null, "saldo awal");
        await ctx.SaveChangesAsync();

        var po = await service.CreateAsync(NewPoDto(qty: 10m, price: 200m));
        await service.SendAsync(po.Id);
        await service.ReceiveAsync(po.Id, new ReceivePurchaseOrderDto(
            new List<ReceivePurchaseOrderLineDto> { new(po.Lines[0].Id, 10m) }));

        // (10*100 + 10*200) / 20 = 150.
        var balance = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == RM_A);
        Assert.Equal(20m, balance.QtyOnHand);
        Assert.Equal(150m, balance.AverageCost);
    }

    [Fact]
    public async Task Receive_BarisDalamGram_ItemBerbasisKg_DikonversiKeKgDenganBenar()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);

        // Pesan 1000 g RM_A (item berbasis kg) @ 120/gram.
        var dto = new UpsertPurchaseOrderDto(
            SupplierId, "PO-G-1", DateOnly.FromDateTime(DateTime.Today), null,
            new List<UpsertPurchaseOrderLineDto> { new(RM_A, UomG, 1000m, 120m) });
        var po = await service.CreateAsync(dto);
        await service.SendAsync(po.Id);

        var result = await service.ReceiveAsync(po.Id, new ReceivePurchaseOrderDto(
            new List<ReceivePurchaseOrderLineDto> { new(po.Lines[0].Id, 1000m) }));

        Assert.Equal(PurchaseOrderStatus.Received, result.Status);

        // 1000 g = 1 kg; biaya total 1000*120 = 120.000, dibagi 1 kg = 120.000/kg.
        var balance = await ctx.InventoryBalances.FirstAsync(b => b.ItemId == RM_A);
        Assert.Equal(1m, balance.QtyOnHand);
        Assert.Equal(120000m, balance.AverageCost);

        var movement = await ctx.StockMovements.FirstAsync(m => m.ItemId == RM_A);
        Assert.Equal(1m, movement.Qty); // dicatat dalam base UoM (kg), bukan 1000 (gram)
    }

    [Fact]
    public async Task Receive_MelebihiSisa_MelemparBadRequest()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var po = await service.CreateAsync(NewPoDto(qty: 100m));
        await service.SendAsync(po.Id);

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.ReceiveAsync(po.Id,
            new ReceivePurchaseOrderDto(new List<ReceivePurchaseOrderLineDto> { new(po.Lines[0].Id, 150m) })));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Receive_SaatMasihDraft_MelemparConflict()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var po = await service.CreateAsync(NewPoDto());

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.ReceiveAsync(po.Id,
            new ReceivePurchaseOrderDto(new List<ReceivePurchaseOrderLineDto> { new(po.Lines[0].Id, 10m) })));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Cancel_DariSent_BerhasilJadiCancelled()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var po = await service.CreateAsync(NewPoDto());
        await service.SendAsync(po.Id);

        var result = await service.CancelAsync(po.Id);

        Assert.Equal(PurchaseOrderStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task Cancel_SetelahReceived_MelemparConflict()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var po = await service.CreateAsync(NewPoDto(qty: 10m));
        await service.SendAsync(po.Id);
        await service.ReceiveAsync(po.Id, new ReceivePurchaseOrderDto(
            new List<ReceivePurchaseOrderLineDto> { new(po.Lines[0].Id, 10m) }));

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.CancelAsync(po.Id));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Delete_SetelahDikirim_MelemparConflict()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var po = await service.CreateAsync(NewPoDto());
        await service.SendAsync(po.Id);

        var ex = await Assert.ThrowsAsync<BomServiceException>(() => service.DeleteAsync(po.Id));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task GetNextOrderNo_KosongDulu_LaluBertambah()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);

        Assert.Equal("PO-0001", await service.GetNextOrderNoAsync());

        await service.CreateAsync(NewPoDto() with { OrderNo = "PO-0001" });
        Assert.Equal("PO-0002", await service.GetNextOrderNoAsync());

        // Nomor manual di luar pola "PO-0000" diabaikan dari perhitungan.
        await service.CreateAsync(NewPoDto() with { OrderNo = "PESANAN-KHUSUS" });
        Assert.Equal("PO-0002", await service.GetNextOrderNoAsync());
    }

    [Fact]
    public async Task List_FilterByStatusDanSupplier_Bekerja()
    {
        using var ctx = NewContext();
        var service = NewService(ctx);
        var po1 = await service.CreateAsync(NewPoDto());
        await service.SendAsync(po1.Id);
        await service.CreateAsync(NewPoDto() with { OrderNo = "PO-0002" });

        var sentOnly = await service.ListAsync(PurchaseOrderStatus.Sent, null);
        Assert.Single(sentOnly);

        var bySupplier = await service.ListAsync(null, SupplierId);
        Assert.Equal(2, bySupplier.Count);
    }
}
