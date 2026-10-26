using Bom.Api.Common;
using Bom.Application.Dtos;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bom.Api.Controllers;

/// <summary>Jenjang kemasan (Botol / Pack / Karton) per produk jadi. CRUD bersarang di bawah item.</summary>
[ApiController]
[Route("api/items/{itemId:int}/pack-levels")]
public class PackLevelsController : ControllerBase
{
    private readonly BomDbContext _db;

    public PackLevelsController(BomDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PackLevelDto>>> GetAll(int itemId)
    {
        var levels = await _db.PackLevels.AsNoTracking()
            .Where(p => p.FinishedItemId == itemId)
            .OrderBy(p => p.Sequence)
            .ThenBy(p => p.Id)
            .Select(ToDto)
            .ToListAsync();

        return Ok(levels);
    }

    [HttpPost]
    public async Task<ActionResult<PackLevelDto>> Create(int itemId, UpsertPackLevelDto dto)
    {
        var productError = await ValidateProductAsync(itemId);
        if (productError is not null) return productError;

        var entity = new PackLevel
        {
            FinishedItemId = itemId,
            Name = dto.Name.Trim(),
            QtyPerBase = dto.QtyPerBase,
            Barcode = dto.Barcode,
            Sequence = dto.Sequence,
            IsDefaultSales = dto.IsDefaultSales
        };

        _db.PackLevels.Add(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Kemasan '{dto.Name}' sudah ada untuk produk ini." });
        }

        var created = await _db.PackLevels.AsNoTracking()
            .Where(p => p.Id == entity.Id).Select(ToDto).FirstAsync();
        return CreatedAtAction(nameof(GetAll), new { itemId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int itemId, int id, UpsertPackLevelDto dto)
    {
        var entity = await _db.PackLevels.FirstOrDefaultAsync(p => p.Id == id && p.FinishedItemId == itemId);
        if (entity is null) return NotFound();

        entity.Name = dto.Name.Trim();
        entity.QtyPerBase = dto.QtyPerBase;
        entity.Barcode = dto.Barcode;
        entity.Sequence = dto.Sequence;
        entity.IsDefaultSales = dto.IsDefaultSales;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Kemasan '{dto.Name}' sudah ada untuk produk ini." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int itemId, int id)
    {
        var entity = await _db.PackLevels.FirstOrDefaultAsync(p => p.Id == id && p.FinishedItemId == itemId);
        if (entity is null) return NotFound();

        _db.PackLevels.Remove(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsForeignKeyViolation(ex))
        {
            return Conflict(new { message = "Kemasan tidak dapat dihapus karena masih dipakai oleh price list." });
        }

        return NoContent();
    }

    /// <summary>Pastikan produk ada dan bertipe SemiFinished / FinishedGood.</summary>
    private async Task<ActionResult?> ValidateProductAsync(int itemId)
    {
        var item = await _db.Items.AsNoTracking()
            .Where(i => i.Id == itemId)
            .Select(i => new { i.Type })
            .FirstOrDefaultAsync();

        if (item is null)
            return NotFound(new { message = $"Item {itemId} tidak ditemukan." });
        if (item.Type is not (ItemType.SemiFinished or ItemType.FinishedGood))
            return BadRequest(new { message = "Jenjang kemasan hanya untuk produk SemiFinished atau FinishedGood." });

        return null;
    }

    private static readonly System.Linq.Expressions.Expression<Func<PackLevel, PackLevelDto>> ToDto = p =>
        new PackLevelDto(p.Id, p.FinishedItemId, p.Name, p.QtyPerBase, p.Barcode, p.Sequence, p.IsDefaultSales);
}
