using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bom.Infrastructure.Services;

/// <summary>
/// Penetapan harga jual (Fase 6) di atas <see cref="BomDbContext"/> + <see cref="ICostingService"/>.
///
/// <para><b>Biaya</b>: CostBasis Standard = TotalCost cost roll-up standar; LastCOGS = UnitCost
/// pergerakan <see cref="StockMovementType.ProductionReceipt"/> terbaru produk (fallback ke biaya
/// standar bila belum pernah produksi).</para>
///
/// <para><b>Harga dasar (ex-factory, per 1 satuan dasar/botol)</b>: Markup = cost*(1+target);
/// Margin = cost/(1-target); Manual = ManualBasePrice. Dibulatkan per jenjang (default: rupiah utuh).</para>
///
/// <para><b>Jenjang</b>: mulai Ex-factory = base; tiap kanal (urut Sequence):
/// running = round(running*(1+margin)); <b>HET</b> = round(lastChannelNet*(1+PPN)) (termasuk PPN).
/// Untuk tiap jenjang × kemasan: harga = jenjangNetPerBase * QtyPerBase * (1 - packDiscount).</para>
///
/// <para><b>Metrik per botol</b>: contribution = base - cost; marginPct = (base-cost)/base;
/// breakEvenQty = ceil(fixedCost/contribution). <b>Floor guard</b>: base &lt; FloorCost atau
/// marginPct &lt; MinMarginPercent menandai FloorViolation (hasil tetap dikembalikan).</para>
/// </summary>
public class PricingService : IPricingService
{
    private readonly BomDbContext _db;
    private readonly ICostingService _costing;

    public PricingService(BomDbContext db, ICostingService costing)
    {
        _db = db;
        _costing = costing;
    }

    // ------------------------------------------------------------- Simulate

    public async Task<PricingResultDto> SimulateAsync(SimulatePricingRequest req, CancellationToken ct = default)
    {
        var comp = await ComputeAsync(req, ct);
        return BuildResult(comp);
    }

    // ---------------------------------------------------------- Price list CRUD

    public async Task<IReadOnlyList<PriceListListItemDto>> ListPriceListsAsync(
        int? productItemId, PriceListStatus? status, CancellationToken ct = default)
    {
        var query = _db.PriceLists.AsNoTracking();

        if (productItemId is int pid)
            query = query.Where(p => p.ProductItemId == pid);
        if (status is PriceListStatus st)
            query = query.Where(p => p.Status == st);

        return await query
            .OrderBy(p => p.ProductItem!.Code)
            .ThenByDescending(p => p.Id)
            .Select(p => new PriceListListItemDto(
                p.Id,
                p.ProductItemId,
                p.ProductItem!.Code,
                p.ProductItem!.Name,
                p.PricingMethod,
                p.CostBasis,
                p.TargetPercent,
                p.Status,
                p.EffectiveFrom,
                p.EffectiveTo,
                p.Lines.Count))
            .ToListAsync(ct);
    }

    public async Task<PriceListDto?> GetPriceListAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.PriceLists.AsNoTracking()
            .Include(p => p.ProductItem)
            .Include(p => p.Lines).ThenInclude(l => l.SalesChannel)
            .Include(p => p.Lines).ThenInclude(l => l.PackLevel)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<PriceListDto> CreatePriceListAsync(SavePriceListRequest req, CancellationToken ct = default)
    {
        var comp = await ComputeAsync(req.Simulation, ct);
        RequireRealPackLevels(comp);

        var entity = new PriceList
        {
            ProductItemId = req.Simulation.ProductItemId,
            PricingMethod = req.Simulation.PricingMethod,
            CostBasis = req.Simulation.CostBasis,
            TargetPercent = comp.Target,
            ManualBasePrice = req.Simulation.ManualBasePrice,
            FloorCost = req.Simulation.FloorCost,
            TaxMode = req.Simulation.TaxMode,
            TaxPercent = comp.TaxPercent,
            EffectiveFrom = req.EffectiveFrom,
            EffectiveTo = req.EffectiveTo,
            Status = PriceListStatus.Draft,
            Notes = req.Notes,
            Lines = BuildLines(comp)
        };

        _db.PriceLists.Add(entity);
        await _db.SaveChangesAsync(ct);

        return (await GetPriceListAsync(entity.Id, ct))!;
    }

    public async Task<PriceListDto> UpdatePriceListAsync(int id, SavePriceListRequest req, CancellationToken ct = default)
    {
        var entity = await _db.PriceLists
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (entity is null)
            throw BomServiceException.NotFound($"Price list {id} tidak ditemukan.");
        if (entity.Status != PriceListStatus.Draft)
            throw BomServiceException.Conflict("Hanya price list berstatus Draft yang dapat diubah.");

        var comp = await ComputeAsync(req.Simulation, ct);
        RequireRealPackLevels(comp);

        entity.ProductItemId = req.Simulation.ProductItemId;
        entity.PricingMethod = req.Simulation.PricingMethod;
        entity.CostBasis = req.Simulation.CostBasis;
        entity.TargetPercent = comp.Target;
        entity.ManualBasePrice = req.Simulation.ManualBasePrice;
        entity.FloorCost = req.Simulation.FloorCost;
        entity.TaxMode = req.Simulation.TaxMode;
        entity.TaxPercent = comp.TaxPercent;
        entity.EffectiveFrom = req.EffectiveFrom;
        entity.EffectiveTo = req.EffectiveTo;
        entity.Notes = req.Notes;

        _db.PriceListLines.RemoveRange(entity.Lines);
        entity.Lines = BuildLines(comp);

        await _db.SaveChangesAsync(ct);
        return (await GetPriceListAsync(entity.Id, ct))!;
    }

    public async Task DeletePriceListAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.PriceLists.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
            throw BomServiceException.NotFound($"Price list {id} tidak ditemukan.");
        if (entity.Status == PriceListStatus.Active)
            throw BomServiceException.Conflict("Price list Active tidak dapat dihapus. Non-aktifkan atau ganti versi lebih dulu.");

        _db.PriceLists.Remove(entity); // cascade menghapus baris
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PriceListDto> ActivatePriceListAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.PriceLists.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
            throw BomServiceException.NotFound($"Price list {id} tidak ditemukan.");
        if (entity.Status == PriceListStatus.Active)
            return (await GetPriceListAsync(id, ct))!;

        // Obsolete-kan Active lama LEBIH DULU (save terpisah) agar tidak melanggar
        // partial unique index "satu Active per produk" saat transisi.
        var previouslyActive = await _db.PriceLists
            .Where(p => p.ProductItemId == entity.ProductItemId && p.Status == PriceListStatus.Active && p.Id != entity.Id)
            .ToListAsync(ct);

        if (previouslyActive.Count > 0)
        {
            foreach (var old in previouslyActive)
                old.Status = PriceListStatus.Obsolete;
            await _db.SaveChangesAsync(ct);
        }

        entity.Status = PriceListStatus.Active;
        entity.EffectiveFrom ??= DateOnly.FromDateTime(DateTime.Today);
        await _db.SaveChangesAsync(ct);

        return (await GetPriceListAsync(entity.Id, ct))!;
    }

    // ------------------------------------------------------------ Computation

    private async Task<Computation> ComputeAsync(SimulatePricingRequest req, CancellationToken ct)
    {
        var product = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == req.ProductItemId, ct);
        if (product is null)
            throw BomServiceException.NotFound($"Item {req.ProductItemId} tidak ditemukan.");
        if (product.Type is not (ItemType.SemiFinished or ItemType.FinishedGood))
            throw BomServiceException.BadRequest("Penetapan harga hanya untuk produk SemiFinished atau FinishedGood.");

        var setting = await GetSettingAsync(ct);
        var target = req.TargetPercent ?? setting.DefaultTargetMarginPercent;
        var taxPercent = req.TaxPercent ?? setting.DefaultTaxPercent;
        var rounding = req.RoundingRule ?? setting.RoundingRule;
        var minMargin = req.MinMarginPercent ?? setting.MinMarginPercent;

        // Biaya dasar.
        var standardCost = (await _costing.GetCostBreakdownAsync(req.ProductItemId, ct)).TotalCost;
        var cost = standardCost;
        if (req.CostBasis == CostBasis.LastCOGS)
        {
            var lastCogs = await _db.StockMovements.AsNoTracking()
                .Where(m => m.ItemId == req.ProductItemId && m.Type == StockMovementType.ProductionReceipt)
                .OrderByDescending(m => m.MovementDate)
                .ThenByDescending(m => m.Id)
                .Select(m => (decimal?)m.UnitCost)
                .FirstOrDefaultAsync(ct);
            cost = lastCogs ?? standardCost;
        }

        // Harga dasar (ex-factory net per 1 satuan dasar), dibulatkan per jenjang.
        decimal baseNet = req.PricingMethod switch
        {
            PricingMethod.Markup => cost * (1 + target),
            PricingMethod.Margin => target >= 1
                ? throw BomServiceException.BadRequest("TargetPercent untuk metode Margin harus < 1.")
                : cost / (1 - target),
            PricingMethod.Manual => req.ManualBasePrice
                ?? throw BomServiceException.BadRequest("ManualBasePrice wajib diisi untuk metode Manual."),
            _ => throw BomServiceException.BadRequest("PricingMethod tidak dikenal.")
        };
        baseNet = Round(baseNet, rounding);

        // Jenjang kanal (urut Sequence), lalu HET (= jenjang terakhir + PPN).
        var channels = await _db.SalesChannels.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Sequence)
            .ThenBy(c => c.Id)
            .ToListAsync(ct);
        var overrides = (req.ChannelMargins ?? Array.Empty<ChannelMarginOverrideDto>())
            .ToDictionary(o => o.SalesChannelId, o => o.MarginPercent);

        var tiers = new List<Tier> { new("Ex-factory", null, target, baseNet, IsHet: false) };
        var running = baseNet;
        foreach (var ch in channels)
        {
            var margin = overrides.TryGetValue(ch.Id, out var ov) ? ov : ch.DefaultMarginPercent;
            running = Round(running * (1 + margin), rounding);
            tiers.Add(new Tier(ch.Name, ch.Id, margin, running, IsHet: false));
        }
        var het = Round(running * (1 + taxPercent), rounding);
        tiers.Add(new Tier("HET", null, taxPercent, het, IsHet: true));

        // Jenjang kemasan produk (fallback: satu kemasan dasar bila belum ada).
        var packRows = await _db.PackLevels.AsNoTracking()
            .Where(p => p.FinishedItemId == req.ProductItemId)
            .OrderBy(p => p.Sequence)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);
        var discounts = (req.PackDiscounts ?? Array.Empty<PackDiscountDto>())
            .ToDictionary(d => d.PackLevelId, d => d.DiscountPercent);

        var packs = new List<PackInfo>();
        if (packRows.Count == 0)
        {
            packs.Add(new PackInfo(0, "Base", 1m, 0m));
        }
        else
        {
            foreach (var p in packRows)
            {
                var disc = discounts.TryGetValue(p.Id, out var d) ? d : 0m;
                packs.Add(new PackInfo(p.Id, p.Name, p.QtyPerBase, disc));
            }
        }

        // Metrik per satuan dasar (botol).
        var contribution = baseNet - cost;
        var marginPct = baseNet == 0 ? 0m : (baseNet - cost) / baseNet;

        int? breakEven = null;
        if (req.FixedCostPerPeriod is decimal fixedCost && fixedCost > 0 && contribution > 0)
            breakEven = (int)Math.Ceiling(fixedCost / contribution);

        var floorViolation = (req.FloorCost is decimal floor && baseNet < floor) || marginPct < minMargin;

        return new Computation(
            product.Id, cost, req.CostBasis, baseNet, marginPct, contribution, breakEven, floorViolation,
            target, taxPercent, tiers, packs);
    }

    private async Task<PricingSetting> GetSettingAsync(CancellationToken ct)
        => await _db.PricingSettings.AsNoTracking().FirstOrDefaultAsync(ct) ?? new PricingSetting();

    // -------------------------------------------------------------- Building

    private static PricingResultDto BuildResult(Computation comp)
    {
        var cells = new List<PricingCellDto>();
        foreach (var pack in comp.Packs)
            foreach (var tier in comp.Tiers)
                cells.Add(BuildCell(tier, pack, comp.TaxPercent));

        return new PricingResultDto(
            comp.ProductItemId,
            comp.Cost,
            comp.CostBasis,
            comp.BaseNet,
            comp.MarginPct,
            comp.Contribution,
            comp.BreakEvenQty,
            comp.FloorViolation,
            comp.Tiers.Select(t => t.Name).ToList(),
            comp.Packs.Select(p => new PricingPackLevelDto(p.Id, p.Name, p.QtyPerBase)).ToList(),
            cells);
    }

    private static List<PriceListLine> BuildLines(Computation comp)
    {
        var lines = new List<PriceListLine>();
        foreach (var pack in comp.Packs)
        {
            var packBase = pack.QtyPerBase * (1 - pack.Discount);
            foreach (var tier in comp.Tiers)
            {
                var cell = BuildCell(tier, pack, comp.TaxPercent);
                var contribution = Math.Round(cell.NetPrice - comp.Cost * packBase, 4, MidpointRounding.AwayFromZero);
                lines.Add(new PriceListLine
                {
                    SalesChannelId = tier.SalesChannelId,
                    PackLevelId = pack.Id,
                    TierName = tier.Name,
                    MarginPercent = tier.MarginApplied,
                    NetPrice = cell.NetPrice,
                    TaxAmount = cell.TaxAmount,
                    GrossPrice = cell.GrossPrice,
                    ContributionMargin = contribution
                });
            }
        }
        return lines;
    }

    /// <summary>
    /// Sel harga (jenjang × kemasan). HET bersifat termasuk PPN (GrossPrice); jenjang lain tanpa PPN
    /// (NetPrice) dengan TaxAmount sebagai informasi.
    /// </summary>
    private static PricingCellDto BuildCell(Tier tier, PackInfo pack, decimal taxPercent)
    {
        var packBase = pack.QtyPerBase * (1 - pack.Discount);

        if (tier.IsHet)
        {
            var gross = Math.Round(tier.NetPerBase * packBase, 4, MidpointRounding.AwayFromZero);
            var net = 1 + taxPercent == 0
                ? gross
                : Math.Round(gross / (1 + taxPercent), 4, MidpointRounding.AwayFromZero);
            return new PricingCellDto(tier.Name, tier.SalesChannelId, pack.Id, pack.Name, pack.QtyPerBase,
                net, gross - net, gross);
        }

        var netPrice = Math.Round(tier.NetPerBase * packBase, 4, MidpointRounding.AwayFromZero);
        var tax = Math.Round(netPrice * taxPercent, 4, MidpointRounding.AwayFromZero);
        return new PricingCellDto(tier.Name, tier.SalesChannelId, pack.Id, pack.Name, pack.QtyPerBase,
            netPrice, tax, netPrice + tax);
    }

    private static void RequireRealPackLevels(Computation comp)
    {
        if (comp.Packs.Any(p => p.Id == 0))
            throw BomServiceException.BadRequest(
                "Produk belum memiliki jenjang kemasan (PackLevel). Tambahkan minimal satu sebelum menyimpan price list.");
    }

    private static PriceListDto MapToDto(PriceList p) => new(
        p.Id,
        p.ProductItemId,
        p.ProductItem!.Code,
        p.ProductItem!.Name,
        p.PricingMethod,
        p.CostBasis,
        p.TargetPercent,
        p.ManualBasePrice,
        p.FloorCost,
        p.TaxMode,
        p.TaxPercent,
        p.EffectiveFrom,
        p.EffectiveTo,
        p.Status,
        p.Notes,
        p.Lines
            .OrderBy(l => l.PackLevelId)
            .ThenBy(l => l.Id)
            .Select(l => new PriceListLineDto(
                l.Id,
                l.SalesChannelId,
                l.SalesChannel != null ? l.SalesChannel.Name : null,
                l.PackLevelId,
                l.PackLevel != null ? l.PackLevel.Name : string.Empty,
                l.TierName,
                l.MarginPercent,
                l.NetPrice,
                l.TaxAmount,
                l.GrossPrice,
                l.ContributionMargin))
            .ToList());

    // ------------------------------------------------------------- Rounding

    /// <summary>Pembulatan harga per jenjang. None = rupiah utuh (dipakai default & contoh).</summary>
    private static decimal Round(decimal value, RoundingRule rule) => rule switch
    {
        RoundingRule.Nearest100 => Math.Round(value / 100m, 0, MidpointRounding.AwayFromZero) * 100m,
        RoundingRule.Nearest500 => Math.Round(value / 500m, 0, MidpointRounding.AwayFromZero) * 500m,
        RoundingRule.Psych990 => Psych990(value),
        _ => Math.Round(value, 0, MidpointRounding.AwayFromZero)
    };

    private static decimal Psych990(decimal value)
    {
        var thousands = Math.Round(value / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;
        var candidate = thousands - 10m;
        return candidate < 0m ? 0m : candidate;
    }

    // --------------------------------------------------------------- Records

    private sealed record Tier(string Name, int? SalesChannelId, decimal MarginApplied, decimal NetPerBase, bool IsHet);

    private sealed record PackInfo(int Id, string Name, decimal QtyPerBase, decimal Discount);

    private sealed record Computation(
        int ProductItemId,
        decimal Cost,
        CostBasis CostBasis,
        decimal BaseNet,
        decimal MarginPct,
        decimal Contribution,
        int? BreakEvenQty,
        bool FloorViolation,
        decimal Target,
        decimal TaxPercent,
        IReadOnlyList<Tier> Tiers,
        IReadOnlyList<PackInfo> Packs);
}
