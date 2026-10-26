using Bom.Api.Common;
using Bom.Application.Dtos;
using Bom.Domain.Entities;
using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bom.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly BomDbContext _db;

    public SuppliersController(BomDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetAll()
    {
        var suppliers = await _db.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Code)
            .Select(s => new SupplierDto(s.Id, s.Code, s.Name, s.LeadTimeDays, s.IsActive))
            .ToListAsync();

        return Ok(suppliers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierDto>> GetById(int id)
    {
        var supplier = await _db.Suppliers
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SupplierDto(s.Id, s.Code, s.Name, s.LeadTimeDays, s.IsActive))
            .FirstOrDefaultAsync();

        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create(UpsertSupplierDto dto)
    {
        var entity = new Supplier
        {
            Code = dto.Code,
            Name = dto.Name,
            LeadTimeDays = dto.LeadTimeDays,
            IsActive = dto.IsActive
        };
        _db.Suppliers.Add(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Supplier dengan Code '{dto.Code}' sudah ada." });
        }

        var result = new SupplierDto(entity.Id, entity.Code, entity.Name, entity.LeadTimeDays, entity.IsActive);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpsertSupplierDto dto)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        if (entity is null) return NotFound();

        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.LeadTimeDays = dto.LeadTimeDays;
        entity.IsActive = dto.IsActive;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Supplier dengan Code '{dto.Code}' sudah ada." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        if (entity is null) return NotFound();

        _db.Suppliers.Remove(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsForeignKeyViolation(ex))
        {
            return Conflict(new { message = "Supplier tidak dapat dihapus karena masih dipakai oleh daftar harga." });
        }

        return NoContent();
    }
}
