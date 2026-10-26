using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BomEntity = Bom.Domain.Entities.Bom;

namespace Bom.Infrastructure.Services;

/// <summary>
/// Costing standar (cost roll-up) di atas <see cref="BomDbContext"/>.
///
/// Formula per 1 satuan keluaran (OutputUom) produk:
///   material = Σ komponen [ Convert(qty*(1+scrap), compUom, compBaseUom, density) * unitCostPerBaseUom(comp) ]
///             / OutputQuantity / YieldPercent
///   labor    = Σ BomOperation.LaborCostPerOutputUnit
///   overhead = Σ BomOperation.OverheadRate
///   total    = material + labor + overhead
///
/// unitCostPerBaseUom(item): item tanpa BOM Active dinilai sebagai "dibeli" (harga supplier
/// preferred yang efektif, dikonversi ke per base UoM, fallback ke StandardMaterialCost);
/// item ber-BOM dinilai dari BOM lalu dinyatakan per base UoM: total / Convert(1, outputUom, baseUom).
///
/// Rekursi dijaga dengan path set (deteksi siklus -&gt; 409) + batas kedalaman, dan
/// dipercepat dengan memoization per item dalam satu perhitungan.
/// </summary>
public class CostingService : ICostingService
{
    private const int MaxDepth = 20;

    private readonly BomDbContext _db;

    public CostingService(BomDbContext db) => _db = db;

    // ------------------------------------------------------------- Public API

    public async Task<CostBreakdownDto> GetCostBreakdownAsync(int itemId, CancellationToken ct = default)
    {
        var (breakdown, _, _) = await ComputeAsync(itemId, ct);
        return breakdown;
    }

    public async Task<CostBreakdownDto> SaveSnapshotAsync(int itemId, CancellationToken ct = default)
    {
        var (breakdown, activeBom, _) = await ComputeAsync(itemId, ct);

        var snapshot = new CostSnapshot
        {
            ItemId = itemId,
            AsOfDate = DateOnly.FromDateTime(DateTime.Today),
            MaterialCost = breakdown.MaterialCost,
            LaborCost = breakdown.LaborCost,
            OverheadCost = breakdown.OverheadCost,
            TotalCost = breakdown.TotalCost,
            OutputUomId = activeBom?.OutputUomId ?? await BaseUomIdAsync(itemId, ct),
            SourceBomId = activeBom?.Id,
            BomVersion = activeBom?.Version
        };

        _db.CostSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);

        return breakdown;
    }

    public async Task<IReadOnlyList<CostSnapshotDto>> ListSnapshotsAsync(int? itemId, CancellationToken ct = default)
    {
        var query = _db.CostSnapshots.AsNoTracking();
        if (itemId is int id)
            query = query.Where(s => s.ItemId == id);

        return await query
            .OrderByDescending(s => s.AsOfDate)
            .ThenByDescending(s => s.Id)
            .Select(s => new CostSnapshotDto(
                s.Id,
                s.ItemId,
                s.Item!.Code,
                s.Item!.Name,
                s.AsOfDate,
                s.MaterialCost,
                s.LaborCost,
                s.OverheadCost,
                s.TotalCost,
                s.OutputUomId,
                s.OutputUom!.Code,
                s.SourceBomId,
                s.BomVersion))
            .ToListAsync(ct);
    }

    public async Task<decimal> GetUnitCostPerBaseUomAsync(int itemId, CancellationToken ct = default)
    {
        var item = await _db.Items.AsNoTracking()
            .Include(i => i.BaseUom)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item is null)
            throw BomServiceException.NotFound($"Item {itemId} tidak ditemukan.");

        var ctx = new CostContext(DateOnly.FromDateTime(DateTime.Today));
        return await UnitCostPerBaseUomAsync(item, ctx, depth: 0, ct);
    }

    // ------------------------------------------------------------ Computation

    /// <summary>Muat item + BOM Active-nya (bila ada), lalu hitung pohon biaya.</summary>
    private async Task<(CostBreakdownDto Breakdown, BomEntity? ActiveBom, Item Item)> ComputeAsync(int itemId, CancellationToken ct)
    {
        var item = await _db.Items.AsNoTracking()
            .Include(i => i.BaseUom)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item is null)
            throw BomServiceException.NotFound($"Item {itemId} tidak ditemukan.");

        var ctx = new CostContext(DateOnly.FromDateTime(DateTime.Today));
        var activeBom = await GetActiveBomAsync(itemId, ctx, ct);

        CostBreakdownDto breakdown;
        if (activeBom is null)
        {
            // Item dibeli: rincian tanpa komponen, biaya = per base UoM.
            var perBase = await PurchasedUnitCostAsync(item, ctx, ct);
            breakdown = new CostBreakdownDto(
                item.Id, item.Code, item.Name, item.BaseUom!.Code,
                perBase, 0m, 0m, perBase, IsPurchased: true,
                Components: Array.Empty<CostLineDto>());
        }
        else
        {
            breakdown = await BuildBreakdownAsync(item, activeBom, ctx, depth: 0, ct);
        }

        return (breakdown, activeBom, item);
    }

    /// <summary>Pohon biaya untuk item ber-BOM, per 1 satuan keluaran BOM.</summary>
    private async Task<CostBreakdownDto> BuildBreakdownAsync(Item item, BomEntity bom, CostContext ctx, int depth, CancellationToken ct)
    {
        if (ctx.BreakdownMemo.TryGetValue(item.Id, out var cached))
            return cached;

        if (depth >= MaxDepth)
            throw BomServiceException.Conflict($"Kedalaman BOM melebihi batas {MaxDepth}; kemungkinan struktur bermasalah pada item '{item.Code}'.");

        if (!ctx.Path.Add(item.Id))
            throw BomServiceException.Conflict($"Terdeteksi siklus BOM pada item '{item.Code}'.");

        var lines = new List<CostLineDto>();
        var material = 0m;
        var divisor = bom.OutputQuantity * bom.YieldPercent;

        foreach (var comp in bom.Components.OrderBy(c => c.Sequence))
        {
            var compItem = comp.ComponentItem!;
            var unitCost = await UnitCostPerBaseUomAsync(compItem, ctx, depth + 1, ct);

            var effectiveQty = comp.Quantity * (1 + comp.ScrapPercent);
            var convertedToBase = UomConverter.Convert(effectiveQty, comp.Uom!, compItem.BaseUom!, compItem.Density);
            var extended = divisor == 0 ? 0m : convertedToBase * unitCost / divisor;
            material += extended;

            var childBom = await GetActiveBomAsync(compItem.Id, ctx, ct);
            IReadOnlyList<CostLineDto>? children = null;
            if (childBom is not null)
                children = (await BuildBreakdownAsync(compItem, childBom, ctx, depth + 1, ct)).Components;

            lines.Add(new CostLineDto(
                compItem.Id,
                compItem.Code,
                compItem.Name,
                bom.OutputQuantity == 0 ? 0m : comp.Quantity / bom.OutputQuantity,
                comp.Uom!.Code,
                comp.ScrapPercent,
                unitCost,
                extended,
                childBom is not null,
                children));
        }

        var labor = bom.Operations.Sum(o => o.LaborCostPerOutputUnit);
        var overhead = bom.Operations.Sum(o => o.OverheadRate);
        var total = material + labor + overhead;

        ctx.Path.Remove(item.Id);

        var dto = new CostBreakdownDto(
            item.Id, item.Code, item.Name, bom.OutputUom!.Code,
            material, labor, overhead, total, IsPurchased: false, lines);

        ctx.BreakdownMemo[item.Id] = dto;
        return dto;
    }

    /// <summary>Biaya per satu base UoM item (dibeli atau dari BOM). Dimemoisasi.</summary>
    private async Task<decimal> UnitCostPerBaseUomAsync(Item item, CostContext ctx, int depth, CancellationToken ct)
    {
        if (ctx.UnitCostMemo.TryGetValue(item.Id, out var cached))
            return cached;

        var activeBom = await GetActiveBomAsync(item.Id, ctx, ct);

        decimal perBase;
        if (activeBom is null)
        {
            perBase = await PurchasedUnitCostAsync(item, ctx, ct);
        }
        else
        {
            var breakdown = await BuildBreakdownAsync(item, activeBom, ctx, depth, ct);
            // Nyatakan biaya per-output menjadi per base UoM item.
            var outputPerBase = UomConverter.Convert(1m, activeBom.OutputUom!, item.BaseUom!, item.Density);
            perBase = outputPerBase == 0 ? 0m : breakdown.TotalCost / outputPerBase;
        }

        ctx.UnitCostMemo[item.Id] = perBase;
        return perBase;
    }

    /// <summary>
    /// Biaya per base UoM untuk item yang dibeli: harga supplier preferred yang efektif
    /// (dikonversi dari satuan harga ke base UoM), fallback ke StandardMaterialCost.
    /// </summary>
    private async Task<decimal> PurchasedUnitCostAsync(Item item, CostContext ctx, CancellationToken ct)
    {
        var preferred = await _db.ItemSupplierPrices.AsNoTracking()
            .Include(p => p.Uom)
            .Where(p => p.ItemId == item.Id && p.IsPreferred)
            .OrderBy(p => p.Id)
            .ToListAsync(ct);

        var price = preferred.FirstOrDefault(p =>
            (p.EffectiveFrom is null || p.EffectiveFrom <= ctx.Today) &&
            (p.EffectiveTo is null || p.EffectiveTo >= ctx.Today));

        if (price is not null)
        {
            // Convert(1, priceUom, baseUom) = jumlah base UoM dalam 1 satuan harga.
            var baseUnitsPerPriceUnit = UomConverter.Convert(1m, price.Uom!, item.BaseUom!, item.Density);
            if (baseUnitsPerPriceUnit != 0)
                return price.Price / baseUnitsPerPriceUnit;
        }

        return item.StandardMaterialCost;
    }

    // --------------------------------------------------------------- Loading

    private async Task<BomEntity?> GetActiveBomAsync(int productItemId, CostContext ctx, CancellationToken ct)
    {
        if (ctx.ActiveBomCache.TryGetValue(productItemId, out var cached))
            return cached;

        var bom = await _db.Boms.AsNoTracking()
            .Include(b => b.OutputUom)
            .Include(b => b.Operations)
            .Include(b => b.Components).ThenInclude(c => c.ComponentItem).ThenInclude(i => i!.BaseUom)
            .Include(b => b.Components).ThenInclude(c => c.Uom)
            .FirstOrDefaultAsync(b => b.ProductItemId == productItemId && b.Status == BomStatus.Active, ct);

        ctx.ActiveBomCache[productItemId] = bom;
        return bom;
    }

    private async Task<int> BaseUomIdAsync(int itemId, CancellationToken ct)
        => await _db.Items.AsNoTracking()
            .Where(i => i.Id == itemId)
            .Select(i => i.BaseUomId)
            .FirstAsync(ct);

    // -------------------------------------------------------------- Context

    /// <summary>State satu kali perhitungan: tanggal acuan, cache muat, memoization, dan path (deteksi siklus).</summary>
    private sealed class CostContext
    {
        public CostContext(DateOnly today) => Today = today;

        public DateOnly Today { get; }
        public Dictionary<int, BomEntity?> ActiveBomCache { get; } = new();
        public Dictionary<int, decimal> UnitCostMemo { get; } = new();
        public Dictionary<int, CostBreakdownDto> BreakdownMemo { get; } = new();
        public HashSet<int> Path { get; } = new();
    }
}
