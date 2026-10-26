using Bom.Api.Common;
using Bom.Application.Dtos;
using Bom.Domain.Entities;
using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bom.Api.Controllers;

[ApiController]
[Route("api/uoms")]
public class UomsController : ControllerBase
{
    private readonly BomDbContext _db;

    public UomsController(BomDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UomDto>>> GetAll()
    {
        var uoms = await _db.Uoms
            .AsNoTracking()
            .OrderBy(u => u.Code)
            .Select(u => new UomDto(u.Id, u.Code, u.Name, u.Type, u.FactorToBase, u.IsBase))
            .ToListAsync();

        return Ok(uoms);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UomDto>> GetById(int id)
    {
        var uom = await _db.Uoms
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UomDto(u.Id, u.Code, u.Name, u.Type, u.FactorToBase, u.IsBase))
            .FirstOrDefaultAsync();

        return uom is null ? NotFound() : Ok(uom);
    }

    [HttpPost]
    public async Task<ActionResult<UomDto>> Create(UpsertUomDto dto)
    {
        var entity = new Uom
        {
            Code = dto.Code,
            Name = dto.Name,
            Type = dto.Type,
            FactorToBase = dto.FactorToBase,
            IsBase = dto.IsBase
        };
        _db.Uoms.Add(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"UOM dengan Code '{dto.Code}' sudah ada." });
        }

        var result = new UomDto(entity.Id, entity.Code, entity.Name, entity.Type, entity.FactorToBase, entity.IsBase);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpsertUomDto dto)
    {
        var entity = await _db.Uoms.FirstOrDefaultAsync(u => u.Id == id);
        if (entity is null) return NotFound();

        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.Type = dto.Type;
        entity.FactorToBase = dto.FactorToBase;
        entity.IsBase = dto.IsBase;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"UOM dengan Code '{dto.Code}' sudah ada." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Uoms.FirstOrDefaultAsync(u => u.Id == id);
        if (entity is null) return NotFound();

        _db.Uoms.Remove(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsForeignKeyViolation(ex))
        {
            return Conflict(new { message = "UOM tidak dapat dihapus karena masih dipakai oleh item/harga/konversi." });
        }

        return NoContent();
    }
}
