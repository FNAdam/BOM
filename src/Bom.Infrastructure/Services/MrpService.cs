using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BomEntity = Bom.Domain.Entities.Bom;

namespace Bom.Infrastructure.Services;

/// <summary>
/// MRP sederhana (Material Requirements Planning) di atas <see cref="BomDbContext"/>.
///
/// <para>Explosion + netting rekursif. Semua kuantitas diagregasi per item dalam <b>base UoM</b> item.
/// Untuk tiap demand top-level, <see cref="RequireAsync"/> menelusuri BOM Active menembus komponennya:</para>
/// <list type="bullet">
///   <item>gross[item] += qtyNeeded.</item>
///   <item>Demand top-level TIDAK di-net (target produksi eksplisit); item level bawah di-net terhadap
///   on-hand tersisa: use = min(onHand, qty), remaining = qty - use.</item>
///   <item>Bila remaining &gt; 0 dan item punya BOM Active, tiap komponen dikonversi ke base UoM lalu
///   di-scale: childNeed = remaining * compBaseQty / (OutputQuantity * YieldPercent), rekursi.</item>
/// </list>
///
/// <para>Klasifikasi: <b>Make</b> (punya BOM Active) = planned production; <b>Buy</b> (tanpa BOM Active)
/// = saran PO memakai harga supplier preferred efektif (fallback StandardMaterialCost), dengan
/// pembulatan MOQ dan lead time.</para>
///
/// <para>Anti-siklus: path set per cabang + batas kedalaman <see cref="MaxDepth"/> (409).</para>
/// </summary>
public class MrpService : IMrpService
{
    private const int MaxDepth = 20;

    private readonly BomDbContext _db;

    public MrpService(BomDbContext db) => _db = db;

    public async Task<MrpResultDto> ExplodeAsync(MrpRequestDto request, CancellationToken ct = default)
    {
        if (request?.Demands is null || request.Demands.Count == 0)
            throw BomServiceException.BadRequest("Minimal satu baris demand diperlukan.");

        var ctx = new MrpContext(DateOnly.FromDateTime(DateTime.Today));

        // Muat semua saldo on-hand sekali (hindari N+1); base UoM item.
        ctx.OnHandRemaining = await _db.InventoryBalances.AsNoTracking()
            .ToDictionaryAsync(b => b.ItemId, b => b.QtyOnHand, ct);

        var demandLines = new List<MrpDemandLineDto>();

        foreach (var demand in request.Demands)
        {
            if (demand.Quantity <= 0)
                throw BomServiceException.BadRequest("Quantity demand harus lebih besar dari 0.");

            var product = await _db.Items.AsNoTracking()
                .Include(i => i.BaseUom)
                .FirstOrDefaultAsync(i => i.Id == demand.ProductItemId, ct);

            if (product is null)
                throw BomServiceException.NotFound($"Item {demand.ProductItemId} tidak ditemukan.");
            if (product.Type is not (ItemType.SemiFinished or ItemType.FinishedGood))
                throw BomServiceException.BadRequest(
                    $"Produk demand '{product.Code}' harus bertipe SemiFinished atau FinishedGood.");

            demandLines.Add(new MrpDemandLineDto(
                product.Id, product.Code, product.Name, demand.Quantity, product.BaseUom!.Code));

            await RequireAsync(product, demand.Quantity, isTopDemand: true, ctx, new HashSet<int>(), depth: 0, ct);
        }

        // Item mana yang punya BOM Active -> klasifikasi Make; sisanya Buy.
        var itemIds = ctx.Gross.Keys.ToList();
        var makeIds = (await _db.Boms.AsNoTracking()
            .Where(b => b.Status == BomStatus.Active && itemIds.Contains(b.ProductItemId))
            .Select(b => b.ProductItemId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        var lines = new List<MrpLineDto>();
        foreach (var itemId in itemIds)
        {
            var item = ctx.Items[itemId];
            var gross = ctx.Gross[itemId];
            var onHand = ctx.OnHandApplied.GetValueOrDefault(itemId);
            var net = ctx.Net.GetValueOrDefault(itemId);

            if (makeIds.Contains(itemId))
            {
                lines.Add(new MrpLineDto(
                    itemId, item.Code, item.Name, item.Type, "Make",
                    gross, onHand, net, item.BaseUom!.Code,
                    SupplierId: null, SupplierName: null, UnitCost: null, MinOrderQty: null,
                    SuggestedOrderQty: null, LeadTimeDays: null, ExtendedCost: null));
            }
            else
            {
                lines.Add(await ResolveBuyLineAsync(item, gross, onHand, net, ctx, ct));
            }
        }

        // Make dulu (per level, lalu kode), lalu Buy (per kode).
        var ordered = lines
            .OrderBy(l => l.Source == "Make" ? 0 : 1)
            .ThenBy(l => ctx.Level.GetValueOrDefault(l.ItemId))
            .ThenBy(l => l.Code, StringComparer.Ordinal)
            .ToList();

        var totalPurchaseCost = ordered
            .Where(l => l.Source == "Buy")
            .Sum(l => l.ExtendedCost ?? 0m);

        return new MrpResultDto(demandLines, ordered, totalPurchaseCost);
    }

    // ----------------------------------------------------------- Explosion

    private async Task RequireAsync(
        Item item, decimal qtyNeeded, bool isTopDemand,
        MrpContext ctx, HashSet<int> path, int depth, CancellationToken ct)
    {
        if (depth >= MaxDepth)
            throw BomServiceException.Conflict(
                $"Kedalaman MRP melebihi batas {MaxDepth}; kemungkinan struktur BOM bermasalah pada item '{item.Code}'.");
        if (!path.Add(item.Id))
            throw BomServiceException.Conflict($"Terdeteksi siklus BOM pada item '{item.Code}'.");

        ctx.Items[item.Id] = item;
        ctx.Level[item.Id] = ctx.Level.TryGetValue(item.Id, out var lv) ? Math.Min(lv, depth) : depth;
        ctx.Gross[item.Id] = ctx.Gross.GetValueOrDefault(item.Id) + qtyNeeded;

        // Netting: demand top-level tidak di-net; item level bawah di-net terhadap on-hand tersisa.
        decimal remaining;
        if (isTopDemand)
        {
            remaining = qtyNeeded;
        }
        else
        {
            var onHand = ctx.OnHandRemaining.GetValueOrDefault(item.Id);
            var use = Math.Min(onHand, qtyNeeded);
            if (use > 0)
            {
                ctx.OnHandRemaining[item.Id] = onHand - use;
                ctx.OnHandApplied[item.Id] = ctx.OnHandApplied.GetValueOrDefault(item.Id) + use;
            }
            remaining = qtyNeeded - use;
        }
        ctx.Net[item.Id] = ctx.Net.GetValueOrDefault(item.Id) + remaining;

        if (remaining > 0)
        {
            var bom = await GetActiveBomAsync(item.Id, ctx, ct);
            if (bom is not null)
            {
                var divisor = bom.OutputQuantity * bom.YieldPercent;
                foreach (var comp in bom.Components.OrderBy(c => c.Sequence))
                {
                    var compItem = comp.ComponentItem!;
                    var effectiveQty = comp.Quantity * (1 + comp.ScrapPercent);
                    var compBaseQty = UomConverter.Convert(effectiveQty, comp.Uom!, compItem.BaseUom!, compItem.Density);
                    var childNeed = divisor == 0 ? 0m : remaining * compBaseQty / divisor;
                    if (childNeed > 0)
                        await RequireAsync(compItem, childNeed, isTopDemand: false, ctx, path, depth + 1, ct);
                }
            }
        }

        path.Remove(item.Id);
    }

    // ------------------------------------------------------------ Buy line

    /// <summary>Bangun baris Buy: harga supplier preferred efektif (fallback StandardMaterialCost),
    /// konversi ke per base UoM, pembulatan MOQ, dan biaya extended.</summary>
    private async Task<MrpLineDto> ResolveBuyLineAsync(
        Item item, decimal gross, decimal onHand, decimal net, MrpContext ctx, CancellationToken ct)
    {
        var preferred = await _db.ItemSupplierPrices.AsNoTracking()
            .Include(p => p.Uom)
            .Include(p => p.Supplier)
            .Where(p => p.ItemId == item.Id && p.IsPreferred)
            .OrderBy(p => p.Id)
            .ToListAsync(ct);

        var price = preferred.FirstOrDefault(p =>
            (p.EffectiveFrom is null || p.EffectiveFrom <= ctx.Today) &&
            (p.EffectiveTo is null || p.EffectiveTo >= ctx.Today));

        decimal unitCostPerBase;
        decimal moqBase = 0m;
        int? supplierId = null;
        string? supplierName = null;
        int? leadTimeDays = null;

        if (price is not null)
        {
            // Convert(1, priceUom, baseUom) = jumlah base UoM dalam 1 satuan harga.
            var baseUnitsPerPriceUnit = UomConverter.Convert(1m, price.Uom!, item.BaseUom!, item.Density);
            unitCostPerBase = baseUnitsPerPriceUnit != 0 ? price.Price / baseUnitsPerPriceUnit : item.StandardMaterialCost;
            moqBase = price.MinOrderQty > 0
                ? UomConverter.Convert(price.MinOrderQty, price.Uom!, item.BaseUom!, item.Density)
                : 0m;
            supplierId = price.SupplierId;
            supplierName = price.Supplier!.Name;
            leadTimeDays = price.Supplier!.LeadTimeDays;
        }
        else
        {
            unitCostPerBase = item.StandardMaterialCost;
        }

        var suggestedOrderQty = moqBase > 0 ? Math.Ceiling(net / moqBase) * moqBase : net;
        var extendedCost = suggestedOrderQty * unitCostPerBase;

        return new MrpLineDto(
            item.Id, item.Code, item.Name, item.Type, "Buy",
            gross, onHand, net, item.BaseUom!.Code,
            supplierId, supplierName, unitCostPerBase,
            moqBase > 0 ? moqBase : null,
            suggestedOrderQty, leadTimeDays, extendedCost);
    }

    // ------------------------------------------------------------- Loading

    private async Task<BomEntity?> GetActiveBomAsync(int productItemId, MrpContext ctx, CancellationToken ct)
    {
        if (ctx.ActiveBomCache.TryGetValue(productItemId, out var cached))
            return cached;

        var bom = await _db.Boms.AsNoTracking()
            .Include(b => b.Components).ThenInclude(c => c.ComponentItem).ThenInclude(i => i!.BaseUom)
            .Include(b => b.Components).ThenInclude(c => c.Uom)
            .FirstOrDefaultAsync(b => b.ProductItemId == productItemId && b.Status == BomStatus.Active, ct);

        ctx.ActiveBomCache[productItemId] = bom;
        return bom;
    }

    // -------------------------------------------------------------- Context

    /// <summary>State satu kali perhitungan MRP: agregasi per item, on-hand tersisa/terpakai, dan cache muat.</summary>
    private sealed class MrpContext
    {
        public MrpContext(DateOnly today) => Today = today;

        public DateOnly Today { get; }

        /// <summary>On-hand tersisa yang belum dialokasikan (menurun saat netting). Base UoM.</summary>
        public Dictionary<int, decimal> OnHandRemaining { get; set; } = new();

        /// <summary>On-hand yang telah dialokasikan ke netting (= gross - net). Base UoM.</summary>
        public Dictionary<int, decimal> OnHandApplied { get; } = new();

        public Dictionary<int, decimal> Gross { get; } = new();
        public Dictionary<int, decimal> Net { get; } = new();
        public Dictionary<int, Item> Items { get; } = new();
        public Dictionary<int, int> Level { get; } = new();
        public Dictionary<int, BomEntity?> ActiveBomCache { get; } = new();
    }
}
