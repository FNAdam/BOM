using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BomEntity = Bom.Domain.Entities.Bom;

namespace Bom.Infrastructure.Services;

/// <summary>
/// Implementasi aturan bisnis BOM di atas <see cref="BomDbContext"/>.
/// Melempar <see cref="BomServiceException"/> (400/404/409) untuk kesalahan domain.
/// </summary>
public class BomService : IBomService
{
    private const int MaxExplosionDepth = 20;

    private readonly BomDbContext _db;

    public BomService(BomDbContext db) => _db = db;

    // ---------------------------------------------------------------- Reads

    public async Task<IReadOnlyList<BomListItemDto>> ListAsync(int? productItemId, BomStatus? status, CancellationToken ct = default)
    {
        var query = _db.Boms.AsNoTracking();

        if (productItemId is int pid)
            query = query.Where(b => b.ProductItemId == pid);
        if (status is BomStatus st)
            query = query.Where(b => b.Status == st);

        return await query
            .OrderBy(b => b.ProductItem!.Code)
            .ThenBy(b => b.Version)
            .Select(b => new BomListItemDto(
                b.Id,
                b.ProductItemId,
                b.ProductItem!.Code,
                b.ProductItem!.Name,
                b.Version,
                b.Status,
                b.OutputQuantity,
                b.OutputUomId,
                b.OutputUom!.Code,
                b.EffectiveFrom,
                b.EffectiveTo,
                b.Components.Count))
            .ToListAsync(ct);
    }

    public async Task<BomDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var bom = await LoadFullAsync(id, ct);
        if (bom is null) return null;

        var hasBomSet = await ActiveBomProductIdsAsync(bom.Components.Select(c => c.ComponentItemId), ct);
        return MapToDto(bom, hasBomSet);
    }

    // -------------------------------------------------------------- Commands

    public async Task<BomDto> CreateAsync(UpsertBomDto dto, CancellationToken ct = default)
    {
        await ValidateAsync(dto, ct);

        var nextVersion = await NextVersionAsync(dto.ProductItemId, ct);

        var entity = new BomEntity
        {
            ProductItemId = dto.ProductItemId,
            Version = nextVersion,
            Status = BomStatus.Draft,
            OutputQuantity = dto.OutputQuantity,
            OutputUomId = dto.OutputUomId,
            YieldPercent = dto.YieldPercent,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            Notes = dto.Notes,
            Components = dto.Components.Select(MapComponent).ToList(),
            Operations = dto.Operations.Select(MapOperation).ToList()
        };

        _db.Boms.Add(entity);
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.Id, ct))!;
    }

    public async Task UpdateAsync(int id, UpsertBomDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Boms
            .Include(b => b.Components)
            .Include(b => b.Operations)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (entity is null)
            throw BomServiceException.NotFound($"BOM {id} tidak ditemukan.");
        if (entity.Status != BomStatus.Draft)
            throw BomServiceException.Conflict("Hanya BOM berstatus Draft yang dapat diubah.");

        await ValidateAsync(dto, ct);

        entity.ProductItemId = dto.ProductItemId;
        entity.OutputQuantity = dto.OutputQuantity;
        entity.OutputUomId = dto.OutputUomId;
        entity.YieldPercent = dto.YieldPercent;
        entity.EffectiveFrom = dto.EffectiveFrom;
        entity.EffectiveTo = dto.EffectiveTo;
        entity.Notes = dto.Notes;

        _db.BomComponents.RemoveRange(entity.Components);
        _db.BomOperations.RemoveRange(entity.Operations);
        entity.Components = dto.Components.Select(MapComponent).ToList();
        entity.Operations = dto.Operations.Select(MapOperation).ToList();

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Boms.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (entity is null)
            throw BomServiceException.NotFound($"BOM {id} tidak ditemukan.");
        if (entity.Status == BomStatus.Active)
            throw BomServiceException.Conflict("BOM Active tidak dapat dihapus. Buat versi baru atau non-aktifkan lebih dulu.");

        _db.Boms.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<BomDto> ActivateAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Boms
            .Include(b => b.Components)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (entity is null)
            throw BomServiceException.NotFound($"BOM {id} tidak ditemukan.");
        if (entity.Status != BomStatus.Draft)
            throw BomServiceException.Conflict("Hanya BOM berstatus Draft yang dapat diaktifkan.");

        // Validasi ulang aturan bisnis atas kondisi terkini.
        await ValidateAsync(
            entity.ProductItemId, entity.OutputUomId, entity.OutputQuantity, entity.YieldPercent,
            entity.Components.Select(c => (c.ComponentItemId, c.UomId, c.Quantity, c.ScrapPercent)).ToList(), ct);

        // Cek siklus melalui BOM Active dari tiap komponen.
        var trigger = await FindCycleTriggerAsync(entity.ProductItemId, entity.Components.Select(c => c.ComponentItemId), ct);
        if (trigger is int triggerItemId)
        {
            var code = await _db.Items.AsNoTracking()
                .Where(i => i.Id == triggerItemId)
                .Select(i => i.Code)
                .FirstOrDefaultAsync(ct) ?? triggerItemId.ToString();
            throw BomServiceException.Conflict($"BOM tidak dapat diaktifkan: terdeteksi siklus melalui item '{code}'.");
        }

        // Obsolete-kan Active lama LEBIH DULU (save terpisah) agar tidak melanggar
        // partial unique index "satu Active per produk" saat transisi.
        var previouslyActive = await _db.Boms
            .Where(b => b.ProductItemId == entity.ProductItemId && b.Status == BomStatus.Active && b.Id != entity.Id)
            .ToListAsync(ct);

        if (previouslyActive.Count > 0)
        {
            foreach (var old in previouslyActive)
                old.Status = BomStatus.Obsolete;
            await _db.SaveChangesAsync(ct);
        }

        entity.Status = BomStatus.Active;
        entity.EffectiveFrom ??= DateOnly.FromDateTime(DateTime.Today);
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.Id, ct))!;
    }

    public async Task<BomDto> CreateNewVersionAsync(int id, CancellationToken ct = default)
    {
        var source = await _db.Boms
            .AsNoTracking()
            .Include(b => b.Components)
            .Include(b => b.Operations)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (source is null)
            throw BomServiceException.NotFound($"BOM {id} tidak ditemukan.");

        var clone = new BomEntity
        {
            ProductItemId = source.ProductItemId,
            Version = await NextVersionAsync(source.ProductItemId, ct),
            Status = BomStatus.Draft,
            OutputQuantity = source.OutputQuantity,
            OutputUomId = source.OutputUomId,
            YieldPercent = source.YieldPercent,
            EffectiveFrom = null,
            EffectiveTo = null,
            Notes = source.Notes,
            Components = source.Components
                .Select(c => new BomComponent
                {
                    ComponentItemId = c.ComponentItemId,
                    Quantity = c.Quantity,
                    UomId = c.UomId,
                    ScrapPercent = c.ScrapPercent,
                    Sequence = c.Sequence,
                    IsOptional = c.IsOptional,
                    Notes = c.Notes
                })
                .ToList(),
            Operations = source.Operations
                .Select(o => new BomOperation
                {
                    Sequence = o.Sequence,
                    Name = o.Name,
                    LaborCostPerOutputUnit = o.LaborCostPerOutputUnit,
                    OverheadRate = o.OverheadRate
                })
                .ToList()
        };

        _db.Boms.Add(clone);
        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(clone.Id, ct))!;
    }

    // --------------------------------------------------------------- Explode

    public async Task<BomExplosionNodeDto> ExplodeAsync(int id, decimal? qty, CancellationToken ct = default)
    {
        var root = await _db.Boms
            .AsNoTracking()
            .Include(b => b.ProductItem)
            .Include(b => b.OutputUom)
            .Include(b => b.Components).ThenInclude(c => c.ComponentItem).ThenInclude(i => i!.BaseUom)
            .Include(b => b.Components).ThenInclude(c => c.Uom)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (root is null)
            throw BomServiceException.NotFound($"BOM {id} tidak ditemukan.");

        var targetQty = qty ?? root.OutputQuantity;
        if (targetQty <= 0)
            throw BomServiceException.BadRequest("qty harus lebih besar dari 0.");

        var activeBomCache = new Dictionary<int, BomEntity?>();
        var path = new HashSet<int> { root.ProductItemId };

        var scale = root.OutputQuantity == 0 ? 0 : targetQty / root.OutputQuantity;

        var children = new List<BomExplosionNodeDto>();
        foreach (var component in root.Components.OrderBy(c => c.Sequence))
            children.Add(await ExplodeComponentAsync(component, scale, 1, path, activeBomCache, ct));

        return new BomExplosionNodeDto(
            root.ProductItemId,
            root.ProductItem!.Code,
            root.ProductItem!.Name,
            root.ProductItem!.Type,
            targetQty,
            root.OutputUom!.Code,
            0m,
            targetQty,
            0,
            true,
            children);
    }

    private async Task<BomExplosionNodeDto> ExplodeComponentAsync(
        BomComponent component,
        decimal parentScale,
        int level,
        HashSet<int> path,
        Dictionary<int, BomEntity?> activeBomCache,
        CancellationToken ct)
    {
        var item = component.ComponentItem!;
        var scaledQuantity = component.Quantity * parentScale;
        var effectiveQuantity = component.Quantity * (1 + component.ScrapPercent) * parentScale;

        var activeBom = await GetActiveBomAsync(item.Id, activeBomCache, ct);
        var hasBom = activeBom is not null;

        var children = new List<BomExplosionNodeDto>();
        if (hasBom && level < MaxExplosionDepth && !path.Contains(item.Id))
        {
            path.Add(item.Id);
            var childScale = activeBom!.OutputQuantity == 0 ? 0 : effectiveQuantity / activeBom.OutputQuantity;
            foreach (var child in activeBom.Components.OrderBy(c => c.Sequence))
                children.Add(await ExplodeComponentAsync(child, childScale, level + 1, path, activeBomCache, ct));
            path.Remove(item.Id);
        }

        return new BomExplosionNodeDto(
            item.Id,
            item.Code,
            item.Name,
            item.Type,
            scaledQuantity,
            component.Uom!.Code,
            component.ScrapPercent,
            effectiveQuantity,
            level,
            hasBom,
            children);
    }

    private async Task<BomEntity?> GetActiveBomAsync(int productItemId, Dictionary<int, BomEntity?> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(productItemId, out var cached))
            return cached;

        var bom = await _db.Boms
            .AsNoTracking()
            .Include(b => b.Components).ThenInclude(c => c.ComponentItem).ThenInclude(i => i!.BaseUom)
            .Include(b => b.Components).ThenInclude(c => c.Uom)
            .FirstOrDefaultAsync(b => b.ProductItemId == productItemId && b.Status == BomStatus.Active, ct);

        cache[productItemId] = bom;
        return bom;
    }

    // ------------------------------------------------------- Cycle detection

    /// <summary>
    /// True bila mengaktifkan BOM produk <paramref name="productItemId"/> dengan komponen
    /// <paramref name="componentItemIds"/> akan membentuk siklus (langsung atau tak langsung,
    /// menelusuri BOM Active dari tiap komponen). Komponen == produk juga dianggap siklus.
    /// </summary>
    public bool WouldCreateCycle(int productItemId, IEnumerable<int> componentItemIds)
        => FindCycleTriggerAsync(productItemId, componentItemIds, CancellationToken.None).GetAwaiter().GetResult() is not null;

    /// <summary>
    /// DFS: untuk tiap komponen langsung, telusuri BOM Active-nya secara rekursif; jika suatu
    /// jalur kembali ke <paramref name="productItemId"/>, kembalikan id komponen langsung pemicunya.
    /// </summary>
    private async Task<int?> FindCycleTriggerAsync(int productItemId, IEnumerable<int> componentItemIds, CancellationToken ct)
    {
        foreach (var componentItemId in componentItemIds.Distinct())
        {
            if (await ReachesAsync(componentItemId, productItemId, new HashSet<int>(), ct))
                return componentItemId;
        }
        return null;
    }

    private async Task<bool> ReachesAsync(int itemId, int target, HashSet<int> visited, CancellationToken ct)
    {
        if (itemId == target) return true;
        if (!visited.Add(itemId)) return false;

        var active = await _db.Boms
            .AsNoTracking()
            .Include(b => b.Components)
            .FirstOrDefaultAsync(b => b.ProductItemId == itemId && b.Status == BomStatus.Active, ct);

        if (active is null) return false;

        foreach (var component in active.Components)
        {
            if (await ReachesAsync(component.ComponentItemId, target, visited, ct))
                return true;
        }
        return false;
    }

    // ------------------------------------------------------------ Validation

    private Task ValidateAsync(UpsertBomDto dto, CancellationToken ct)
        => ValidateAsync(
            dto.ProductItemId, dto.OutputUomId, dto.OutputQuantity, dto.YieldPercent,
            dto.Components.Select(c => (c.ComponentItemId, c.UomId, c.Quantity, c.ScrapPercent)).ToList(), ct);

    private async Task ValidateAsync(
        int productItemId,
        int outputUomId,
        decimal outputQuantity,
        decimal yieldPercent,
        IReadOnlyList<(int ComponentItemId, int UomId, decimal Quantity, decimal ScrapPercent)> components,
        CancellationToken ct)
    {
        var product = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == productItemId, ct);
        if (product is null)
            throw BomServiceException.BadRequest($"ProductItemId {productItemId} tidak ditemukan.");
        if (product.Type is not (ItemType.SemiFinished or ItemType.FinishedGood))
            throw BomServiceException.BadRequest("Produk BOM harus bertipe SemiFinished atau FinishedGood.");

        if (!await _db.Uoms.AnyAsync(u => u.Id == outputUomId, ct))
            throw BomServiceException.BadRequest($"OutputUomId {outputUomId} tidak ditemukan.");

        if (outputQuantity <= 0)
            throw BomServiceException.BadRequest("OutputQuantity harus lebih besar dari 0.");

        if (yieldPercent <= 0 || yieldPercent > 1)
            throw BomServiceException.BadRequest("YieldPercent harus berada dalam rentang (0,1].");

        var componentItemIds = components.Select(c => c.ComponentItemId).Distinct().ToList();
        var uomIds = components.Select(c => c.UomId).Distinct().ToList();

        var items = await _db.Items.AsNoTracking()
            .Include(i => i.BaseUom)
            .Where(i => componentItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);
        var uoms = await _db.Uoms.AsNoTracking()
            .Where(u => uomIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        foreach (var c in components)
        {
            if (c.ComponentItemId == productItemId)
                throw BomServiceException.BadRequest("Komponen tidak boleh sama dengan produk BOM (siklus).");

            if (!items.TryGetValue(c.ComponentItemId, out var item))
                throw BomServiceException.BadRequest($"ComponentItemId {c.ComponentItemId} tidak ditemukan.");
            if (!uoms.TryGetValue(c.UomId, out var uom))
                throw BomServiceException.BadRequest($"UomId {c.UomId} tidak ditemukan.");

            if (c.Quantity <= 0)
                throw BomServiceException.BadRequest($"Quantity komponen '{item.Code}' harus lebih besar dari 0.");
            if (c.ScrapPercent < 0)
                throw BomServiceException.BadRequest($"ScrapPercent komponen '{item.Code}' tidak boleh negatif.");

            if (item.BaseUom!.Type != uom.Type)
                throw BomServiceException.BadRequest(
                    $"UoM '{uom.Code}' tidak sedimensi dengan base UoM item '{item.Code}' (konversi lintas-dimensi ditangani Fase 3).");
        }
    }

    // --------------------------------------------------------------- Helpers

    private async Task<int> NextVersionAsync(int productItemId, CancellationToken ct)
    {
        var max = await _db.Boms
            .Where(b => b.ProductItemId == productItemId)
            .Select(b => (int?)b.Version)
            .MaxAsync(ct);
        return (max ?? 0) + 1;
    }

    private Task<BomEntity?> LoadFullAsync(int id, CancellationToken ct)
        => _db.Boms
            .AsNoTracking()
            .Include(b => b.ProductItem)
            .Include(b => b.OutputUom)
            .Include(b => b.Components).ThenInclude(c => c.ComponentItem)
            .Include(b => b.Components).ThenInclude(c => c.Uom)
            .Include(b => b.Operations)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    /// <summary>Kumpulan productItemId yang memiliki BOM Active (untuk menandai HasBom).</summary>
    private async Task<HashSet<int>> ActiveBomProductIdsAsync(IEnumerable<int> itemIds, CancellationToken ct)
    {
        var ids = itemIds.Distinct().ToList();
        if (ids.Count == 0) return new HashSet<int>();

        var found = await _db.Boms.AsNoTracking()
            .Where(b => b.Status == BomStatus.Active && ids.Contains(b.ProductItemId))
            .Select(b => b.ProductItemId)
            .Distinct()
            .ToListAsync(ct);
        return found.ToHashSet();
    }

    private static BomComponent MapComponent(UpsertBomComponentDto dto) => new()
    {
        ComponentItemId = dto.ComponentItemId,
        Quantity = dto.Quantity,
        UomId = dto.UomId,
        ScrapPercent = dto.ScrapPercent,
        Sequence = dto.Sequence,
        IsOptional = dto.IsOptional,
        Notes = dto.Notes
    };

    private static BomOperation MapOperation(UpsertBomOperationDto dto) => new()
    {
        Sequence = dto.Sequence,
        Name = dto.Name,
        LaborCostPerOutputUnit = dto.LaborCostPerOutputUnit,
        OverheadRate = dto.OverheadRate
    };

    private static BomDto MapToDto(BomEntity bom, HashSet<int> hasBomSet) => new(
        bom.Id,
        bom.ProductItemId,
        bom.ProductItem!.Code,
        bom.ProductItem!.Name,
        bom.Version,
        bom.Status,
        bom.OutputQuantity,
        bom.OutputUomId,
        bom.OutputUom!.Code,
        bom.YieldPercent,
        bom.EffectiveFrom,
        bom.EffectiveTo,
        bom.Notes,
        bom.Components
            .OrderBy(c => c.Sequence)
            .Select(c => new BomComponentDto(
                c.Id,
                c.ComponentItemId,
                c.ComponentItem!.Code,
                c.ComponentItem!.Name,
                c.Quantity,
                c.UomId,
                c.Uom!.Code,
                c.ScrapPercent,
                c.Sequence,
                c.IsOptional,
                hasBomSet.Contains(c.ComponentItemId)))
            .ToList(),
        bom.Operations
            .OrderBy(o => o.Sequence)
            .Select(o => new BomOperationDto(
                o.Id,
                o.Sequence,
                o.Name,
                o.LaborCostPerOutputUnit,
                o.OverheadRate))
            .ToList());
}
