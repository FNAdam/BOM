using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bom.Infrastructure.Services;

/// <summary>
/// Laporan dashboard (Fase 7) di atas <see cref="BomDbContext"/>, <see cref="ICostingService"/>,
/// dan <see cref="IPricingService"/>.
///
/// <para>Himpunan produk = gabungan produk dengan PriceList Active dan produk SemiFinished/FinishedGood
/// ber-BOM Active. Untuk tiap produk: <b>standardCost</b> dari cost roll-up standar;
/// <b>hargaExFactory</b> dari baris Ex-factory price list Active (per satuan dasar) bila ada, jika
/// tidak dari simulasi default (metode Margin, basis Standard). <b>contributionMargin</b> = harga − cost;
/// <b>marginPct</b> = (harga − cost) / harga; <b>belowMinMargin</b> bila marginPct &lt; MinMarginPercent.</para>
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly BomDbContext _db;
    private readonly ICostingService _costing;
    private readonly IPricingService _pricing;

    private const string ExFactoryTier = "Ex-factory";

    public DashboardService(BomDbContext db, ICostingService costing, IPricingService pricing)
    {
        _db = db;
        _costing = costing;
        _pricing = pricing;
    }

    public async Task<DashboardMarginDto> GetMarginReportAsync(CancellationToken ct = default)
    {
        var setting = await _db.PricingSettings.AsNoTracking().FirstOrDefaultAsync(ct) ?? new PricingSetting();
        var minMargin = setting.MinMarginPercent;

        // Price list Active (beserta baris Ex-factory + jenjang kemasan) per produk.
        var activeLists = await _db.PriceLists.AsNoTracking()
            .Where(p => p.Status == PriceListStatus.Active)
            .Include(p => p.Lines).ThenInclude(l => l.PackLevel)
            .ToListAsync(ct);
        var listByProduct = activeLists
            .GroupBy(p => p.ProductItemId)
            .ToDictionary(g => g.Key, g => g.First());

        // Produk ber-BOM Active.
        var bomProductIds = await _db.Boms.AsNoTracking()
            .Where(b => b.Status == BomStatus.Active)
            .Select(b => b.ProductItemId)
            .Distinct()
            .ToListAsync(ct);

        var productIds = new HashSet<int>(listByProduct.Keys);
        foreach (var id in bomProductIds)
            productIds.Add(id);

        // Metadata produk (hanya SemiFinished/FinishedGood yang bisa diberi harga).
        var products = await _db.Items.AsNoTracking()
            .Where(i => productIds.Contains(i.Id)
                        && (i.Type == ItemType.SemiFinished || i.Type == ItemType.FinishedGood))
            .Select(i => new ProductRow(i.Id, i.Code, i.Name, i.Type))
            .ToListAsync(ct);

        var items = new List<DashboardMarginItemDto>(products.Count);
        foreach (var product in products)
        {
            decimal standardCost;
            try
            {
                standardCost = (await _costing.GetCostBreakdownAsync(product.Id, ct)).TotalCost;
            }
            catch (BomServiceException)
            {
                // Lewati produk yang tidak dapat dihitung (mis. siklus BOM) agar laporan tetap tampil.
                continue;
            }

            var hasActiveList = listByProduct.TryGetValue(product.Id, out var priceList);
            var exFactory = hasActiveList ? ExFactoryFromPriceList(priceList!) : 0m;
            if (exFactory <= 0m)
            {
                try
                {
                    exFactory = await SimulateDefaultBaseAsync(product.Id, ct);
                }
                catch (BomServiceException)
                {
                    // Mis. DefaultTargetMarginPercent >= 1: lewati produk agar laporan tetap tampil.
                    continue;
                }
            }

            var contribution = exFactory - standardCost;
            var marginPct = exFactory == 0m ? 0m : contribution / exFactory;

            items.Add(new DashboardMarginItemDto(
                product.Id,
                product.Code,
                product.Name,
                product.Type,
                standardCost,
                exFactory,
                contribution,
                marginPct,
                marginPct < minMargin,
                hasActiveList));
        }

        // Terburuk lebih dulu (margin menaik), tie-break kode produk.
        var ordered = items
            .OrderBy(i => i.MarginPct)
            .ThenBy(i => i.ProductCode)
            .ToList();

        var summary = new DashboardMarginSummaryDto(
            ordered.Count,
            ordered.Count == 0 ? 0m : ordered.Average(i => i.MarginPct),
            ordered.Count(i => i.BelowMinMargin));

        return new DashboardMarginDto(minMargin, ordered, summary);
    }

    /// <summary>
    /// Harga ex-factory per 1 satuan dasar dari baris Ex-factory price list Active.
    /// Dipilih baris pada jenjang kemasan terkecil (mendekati satuan dasar) lalu dibagi QtyPerBase.
    /// </summary>
    private static decimal ExFactoryFromPriceList(PriceList list)
    {
        var line = list.Lines
            .Where(l => string.Equals(l.TierName, ExFactoryTier, StringComparison.OrdinalIgnoreCase))
            .OrderBy(l => l.PackLevel != null ? l.PackLevel.QtyPerBase : 1m)
            .FirstOrDefault();

        if (line is null)
            return 0m;

        var qtyPerBase = line.PackLevel?.QtyPerBase ?? 1m;
        return qtyPerBase <= 0m ? line.NetPrice : line.NetPrice / qtyPerBase;
    }

    /// <summary>Harga dasar ex-factory dari simulasi default (metode Margin, basis Standard).</summary>
    private async Task<decimal> SimulateDefaultBaseAsync(int productItemId, CancellationToken ct)
    {
        var req = new SimulatePricingRequest(
            productItemId,
            PricingMethod.Margin,
            CostBasis.Standard,
            TaxMode.Exclusive);

        var result = await _pricing.SimulateAsync(req, ct);
        return result.BasePrice;
    }

    private sealed record ProductRow(int Id, string Code, string Name, ItemType Type);
}
