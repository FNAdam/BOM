using Bom.Application.Dtos;
using Bom.Domain.Entities;
using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bom.Api.Controllers;

/// <summary>
/// Saldo persediaan (stok) per item. Fase 4 hanya menyediakan set on-hand manual
/// untuk netting MRP; pergerakan stok otomatis (StockMovement) menyusul di Fase 5.
/// </summary>
[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly BomDbContext _db;

    public InventoryController(BomDbContext db) => _db = db;

    /// <summary>Daftar seluruh item beserta saldo on-hand-nya (0 bila belum ada saldo).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryBalanceDto>>> GetAll()
    {
        var rows = await (
            from i in _db.Items.AsNoTracking()
            join b in _db.InventoryBalances on i.Id equals b.ItemId into bal
            from b in bal.DefaultIfEmpty()
            orderby i.Code
            select new InventoryBalanceDto(
                i.Id,
                i.Code,
                i.Name,
                i.BaseUom!.Code,
                b != null ? b.QtyOnHand : 0m,
                b != null ? b.AverageCost : 0m))
            .ToListAsync();

        return Ok(rows);
    }

    /// <summary>Upsert saldo on-hand sebuah item (untuk seed stok saat uji MRP).</summary>
    [HttpPut("{itemId:int}")]
    public async Task<ActionResult<InventoryBalanceDto>> Upsert(int itemId, UpsertInventoryBalanceDto dto)
    {
        var item = await _db.Items.AsNoTracking()
            .Include(i => i.BaseUom)
            .FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null)
            return NotFound(new { message = $"Item {itemId} tidak ditemukan." });

        if (dto.QtyOnHand < 0)
            return BadRequest(new { message = "QtyOnHand tidak boleh negatif." });

        var balance = await _db.InventoryBalances.FirstOrDefaultAsync(b => b.ItemId == itemId);
        if (balance is null)
        {
            balance = new InventoryBalance { ItemId = itemId };
            _db.InventoryBalances.Add(balance);
        }

        balance.QtyOnHand = dto.QtyOnHand;
        if (dto.AverageCost is decimal averageCost)
            balance.AverageCost = averageCost;

        await _db.SaveChangesAsync();

        return Ok(new InventoryBalanceDto(
            item.Id, item.Code, item.Name, item.BaseUom!.Code, balance.QtyOnHand, balance.AverageCost));
    }
}
