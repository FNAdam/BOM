using Bom.Api.Common;
using Bom.Application.Dtos;
using Bom.Domain.Entities;
using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bom.Api.Controllers;

[ApiController]
[Route("api/item-supplier-prices")]
public class ItemSupplierPricesController : ControllerBase
{
    private readonly BomDbContext _db;

    public ItemSupplierPricesController(BomDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemSupplierPriceDto>>> GetAll()
    {
        var prices = await _db.ItemSupplierPrices
            .AsNoTracking()
            .OrderBy(p => p.ItemId)
            .ThenBy(p => p.SupplierId)
            .Select(ToDto)
            .ToListAsync();

        return Ok(prices);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemSupplierPriceDto>> GetById(int id)
    {
        var price = await _db.ItemSupplierPrices
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync();

        return price is null ? NotFound() : Ok(price);
    }

    [HttpPost]
    public async Task<ActionResult<ItemSupplierPriceDto>> Create(UpsertItemSupplierPriceDto dto)
    {
        var refError = await ValidateReferencesAsync(dto);
        if (refError is not null) return refError;

        var entity = new ItemSupplierPrice
        {
            ItemId = dto.ItemId,
            SupplierId = dto.SupplierId,
            UomId = dto.UomId,
            Price = dto.Price,
            Currency = dto.Currency,
            MinOrderQty = dto.MinOrderQty,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            IsPreferred = dto.IsPreferred
        };

        _db.ItemSupplierPrices.Add(entity);
        await _db.SaveChangesAsync();

        var created = await _db.ItemSupplierPrices.AsNoTracking()
            .Where(p => p.Id == entity.Id).Select(ToDto).FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpsertItemSupplierPriceDto dto)
    {
        var entity = await _db.ItemSupplierPrices.FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null) return NotFound();

        var refError = await ValidateReferencesAsync(dto);
        if (refError is not null) return refError;

        entity.ItemId = dto.ItemId;
        entity.SupplierId = dto.SupplierId;
        entity.UomId = dto.UomId;
        entity.Price = dto.Price;
        entity.Currency = dto.Currency;
        entity.MinOrderQty = dto.MinOrderQty;
        entity.EffectiveFrom = dto.EffectiveFrom;
        entity.EffectiveTo = dto.EffectiveTo;
        entity.IsPreferred = dto.IsPreferred;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.ItemSupplierPrices.FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null) return NotFound();

        _db.ItemSupplierPrices.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Pastikan Item, Supplier, dan UOM yang dirujuk memang ada. Balas 400 bila tidak.</summary>
    private async Task<ActionResult?> ValidateReferencesAsync(UpsertItemSupplierPriceDto dto)
    {
        if (!await _db.Items.AnyAsync(i => i.Id == dto.ItemId))
            return BadRequest(new { message = $"ItemId {dto.ItemId} tidak ditemukan." });

        if (!await _db.Suppliers.AnyAsync(s => s.Id == dto.SupplierId))
            return BadRequest(new { message = $"SupplierId {dto.SupplierId} tidak ditemukan." });

        if (!await _db.Uoms.AnyAsync(u => u.Id == dto.UomId))
            return BadRequest(new { message = $"UomId {dto.UomId} tidak ditemukan." });

        return null;
    }

    // Projeksi entity -> DTO (dijalankan di database, tanpa N+1).
    private static readonly System.Linq.Expressions.Expression<Func<ItemSupplierPrice, ItemSupplierPriceDto>> ToDto = p => new ItemSupplierPriceDto(
        p.Id,
        p.ItemId,
        p.Item!.Name,
        p.SupplierId,
        p.Supplier!.Name,
        p.UomId,
        p.Uom!.Code,
        p.Price,
        p.Currency,
        p.MinOrderQty,
        p.EffectiveFrom,
        p.EffectiveTo,
        p.IsPreferred);
}
