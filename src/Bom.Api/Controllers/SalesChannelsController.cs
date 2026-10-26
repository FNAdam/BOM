using Bom.Api.Common;
using Bom.Application.Dtos;
using Bom.Domain.Entities;
using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bom.Api.Controllers;

/// <summary>Kanal penjualan (jenjang harga di atas ex-factory). CRUD master data.</summary>
[ApiController]
[Route("api/sales-channels")]
public class SalesChannelsController : ControllerBase
{
    private readonly BomDbContext _db;

    public SalesChannelsController(BomDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalesChannelDto>>> GetAll()
    {
        var channels = await _db.SalesChannels
            .AsNoTracking()
            .OrderBy(c => c.Sequence)
            .ThenBy(c => c.Id)
            .Select(ToDto)
            .ToListAsync();

        return Ok(channels);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SalesChannelDto>> GetById(int id)
    {
        var channel = await _db.SalesChannels.AsNoTracking()
            .Where(c => c.Id == id).Select(ToDto).FirstOrDefaultAsync();

        return channel is null ? NotFound() : Ok(channel);
    }

    [HttpPost]
    public async Task<ActionResult<SalesChannelDto>> Create(UpsertSalesChannelDto dto)
    {
        var entity = new SalesChannel
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            DefaultMarginPercent = dto.DefaultMarginPercent,
            Sequence = dto.Sequence,
            IsActive = dto.IsActive
        };

        _db.SalesChannels.Add(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Kanal dengan Code '{dto.Code}' sudah ada." });
        }

        var created = await _db.SalesChannels.AsNoTracking()
            .Where(c => c.Id == entity.Id).Select(ToDto).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpsertSalesChannelDto dto)
    {
        var entity = await _db.SalesChannels.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null) return NotFound();

        entity.Code = dto.Code.Trim();
        entity.Name = dto.Name.Trim();
        entity.DefaultMarginPercent = dto.DefaultMarginPercent;
        entity.Sequence = dto.Sequence;
        entity.IsActive = dto.IsActive;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Kanal dengan Code '{dto.Code}' sudah ada." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.SalesChannels.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null) return NotFound();

        _db.SalesChannels.Remove(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsForeignKeyViolation(ex))
        {
            return Conflict(new { message = "Kanal tidak dapat dihapus karena masih dipakai oleh price list." });
        }

        return NoContent();
    }

    private static readonly System.Linq.Expressions.Expression<Func<SalesChannel, SalesChannelDto>> ToDto = c =>
        new SalesChannelDto(c.Id, c.Code, c.Name, c.DefaultMarginPercent, c.Sequence, c.IsActive);
}
