using System.Text.RegularExpressions;
using Bom.Api.Common;
using Bom.Application.Dtos;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bom.Api.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly BomDbContext _db;

    public ItemsController(BomDbContext db) => _db = db;

    /// <summary>Prefix kode per tipe item, dipakai oleh auto-generate kode (format "PREFIX-0000").</summary>
    private static readonly IReadOnlyDictionary<ItemType, string> CodePrefixes = new Dictionary<ItemType, string>
    {
        [ItemType.RawMaterial] = "RW",
        [ItemType.Packaging] = "PK",
        [ItemType.SemiFinished] = "SF",
        [ItemType.FinishedGood] = "FG",
    };

    /// <summary>
    /// Saran kode berikutnya untuk tipe item tertentu, format "{PREFIX}-0000" (nomor urut 4 digit,
    /// bertambah otomatis bila sudah ada &gt; 9999). Hanya kode yang persis cocok pola prefix ini yang
    /// dihitung — kode lama/manual yang tidak mengikuti pola diabaikan dari perhitungan nomor urut.
    /// Catatan: tidak dikunci lintas-request; nilai ini hanya SARAN awal untuk form — kode final tetap
    /// divalidasi unik saat Create/Update (409 bila tabrakan).
    /// </summary>
    [HttpGet("next-code")]
    public async Task<ActionResult<NextItemCodeDto>> GetNextCode([FromQuery] ItemType type)
    {
        if (!CodePrefixes.TryGetValue(type, out var prefix))
            return BadRequest(new { message = $"Tipe item '{type}' tidak dikenali." });

        var candidates = await _db.Items
            .Where(i => i.Code.StartsWith(prefix + "-"))
            .Select(i => i.Code)
            .ToListAsync();

        var pattern = new Regex($"^{Regex.Escape(prefix)}-(\\d+)$");
        var maxNumber = 0;
        foreach (var code in candidates)
        {
            var match = pattern.Match(code);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var n) && n > maxNumber)
                maxNumber = n;
        }

        var nextCode = $"{prefix}-{(maxNumber + 1).ToString().PadLeft(4, '0')}";
        return Ok(new NextItemCodeDto(nextCode));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetAll()
    {
        var items = await _db.Items
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .Select(ToDto)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemDto>> GetById(int id)
    {
        var item = await _db.Items
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync();

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create(UpsertItemDto dto)
    {
        var refError = await ValidateReferencesAsync(dto);
        if (refError is not null) return refError;

        var entity = new Item
        {
            Code = dto.Code,
            Name = dto.Name,
            Type = dto.Type,
            BaseUomId = dto.BaseUomId,
            CategoryId = dto.CategoryId,
            StandardMaterialCost = dto.StandardMaterialCost,
            CostingMethod = dto.CostingMethod,
            Density = dto.Density,
            IsActive = dto.IsActive
        };

        _db.Items.Add(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Item dengan Code '{dto.Code}' sudah ada." });
        }

        var created = await _db.Items.AsNoTracking().Where(i => i.Id == entity.Id).Select(ToDto).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpsertItemDto dto)
    {
        var entity = await _db.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (entity is null) return NotFound();

        var refError = await ValidateReferencesAsync(dto);
        if (refError is not null) return refError;

        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.Type = dto.Type;
        entity.BaseUomId = dto.BaseUomId;
        entity.CategoryId = dto.CategoryId;
        entity.StandardMaterialCost = dto.StandardMaterialCost;
        entity.CostingMethod = dto.CostingMethod;
        entity.Density = dto.Density;
        entity.IsActive = dto.IsActive;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Item dengan Code '{dto.Code}' sudah ada." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (entity is null) return NotFound();

        _db.Items.Remove(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsForeignKeyViolation(ex))
        {
            return Conflict(new { message = "Item tidak dapat dihapus karena masih direferensikan oleh data lain." });
        }

        return NoContent();
    }

    /// <summary>Validasi keberadaan FK (BaseUom wajib, Category opsional). Balas 400 bila tidak valid.</summary>
    private async Task<ActionResult?> ValidateReferencesAsync(UpsertItemDto dto)
    {
        if (!await _db.Uoms.AnyAsync(u => u.Id == dto.BaseUomId))
            return BadRequest(new { message = $"BaseUomId {dto.BaseUomId} tidak ditemukan." });

        if (dto.CategoryId is int categoryId && !await _db.ItemCategories.AnyAsync(c => c.Id == categoryId))
            return BadRequest(new { message = $"CategoryId {categoryId} tidak ditemukan." });

        return null;
    }

    // Projeksi entity -> DTO (dijalankan di sisi database, tanpa N+1).
    private static readonly System.Linq.Expressions.Expression<Func<Item, ItemDto>> ToDto = i => new ItemDto(
        i.Id,
        i.Code,
        i.Name,
        i.Type,
        i.BaseUomId,
        i.BaseUom!.Code,
        i.CategoryId,
        i.Category != null ? i.Category.Name : null,
        i.StandardMaterialCost,
        i.CostingMethod,
        i.Density,
        i.IsActive);
}
