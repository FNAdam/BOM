using Bom.Api.Common;
using Bom.Application.Dtos;
using Bom.Domain.Entities;
using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bom.Api.Controllers;

[ApiController]
[Route("api/item-categories")]
public class ItemCategoriesController : ControllerBase
{
    private readonly BomDbContext _db;

    public ItemCategoriesController(BomDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemCategoryDto>>> GetAll()
    {
        var categories = await _db.ItemCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ItemCategoryDto(c.Id, c.Name, c.Description))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemCategoryDto>> GetById(int id)
    {
        var category = await _db.ItemCategories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ItemCategoryDto(c.Id, c.Name, c.Description))
            .FirstOrDefaultAsync();

        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<ItemCategoryDto>> Create(UpsertItemCategoryDto dto)
    {
        var entity = new ItemCategory { Name = dto.Name, Description = dto.Description };
        _db.ItemCategories.Add(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Kategori dengan Name '{dto.Name}' sudah ada." });
        }

        var result = new ItemCategoryDto(entity.Id, entity.Name, entity.Description);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpsertItemCategoryDto dto)
    {
        var entity = await _db.ItemCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null) return NotFound();

        entity.Name = dto.Name;
        entity.Description = dto.Description;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Kategori dengan Name '{dto.Name}' sudah ada." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.ItemCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null) return NotFound();

        _db.ItemCategories.Remove(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsForeignKeyViolation(ex))
        {
            return Conflict(new { message = "Kategori tidak dapat dihapus karena masih dipakai oleh item." });
        }

        return NoContent();
    }
}
