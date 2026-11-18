using Bom.Api.Common;
using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bom.Api.Controllers;

/// <summary>
/// Pesanan penjualan ke customer: Draft → Completed (posting stok + struk bisa dicetak), atau
/// Cancelled dari Draft.
/// </summary>
[ApiController]
[Route("api/sales-orders")]
public class SalesOrdersController : ControllerBase
{
    private readonly ISalesOrderService _service;

    public SalesOrdersController(ISalesOrderService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalesOrderListDto>>> GetAll(
        [FromQuery] SalesOrderStatus? status,
        [FromQuery] int? salesChannelId)
    {
        var orders = await _service.ListAsync(status, salesChannelId);
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SalesOrderDto>> GetById(int id)
    {
        var order = await _service.GetByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("{id:int}/receipt")]
    public async Task<ActionResult<SalesReceiptDto>> GetReceipt(int id)
    {
        try
        {
            return Ok(await _service.GetReceiptAsync(id));
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<SalesOrderDto>> Create(UpsertSalesOrderDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Nomor pesanan '{dto.OrderNo}' sudah dipakai." });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpsertSalesOrderDto dto)
    {
        try
        {
            await _service.UpdateAsync(id, dto);
            return NoContent();
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueViolation(ex))
        {
            return Conflict(new { message = $"Nomor pesanan '{dto.OrderNo}' sudah dipakai." });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<SalesOrderDto>> Complete(int id)
    {
        try
        {
            return Ok(await _service.CompleteAsync(id));
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<SalesOrderDto>> Cancel(int id)
    {
        try
        {
            return Ok(await _service.CancelAsync(id));
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    private ObjectResult FromException(BomServiceException ex) =>
        StatusCode(ex.StatusCode, new { message = ex.Message });
}
