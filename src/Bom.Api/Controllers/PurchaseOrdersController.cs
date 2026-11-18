using Bom.Api.Common;
using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bom.Api.Controllers;

/// <summary>
/// Pesanan pembelian ke supplier: Draft → Sent → (Partially)Received, atau Cancelled.
/// Penerimaan barang memposting ke buku besar stok (StockMovement.Receipt + InventoryBalance).
/// </summary>
[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _service;

    public PurchaseOrdersController(IPurchaseOrderService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseOrderListDto>>> GetAll(
        [FromQuery] PurchaseOrderStatus? status,
        [FromQuery] int? supplierId)
    {
        var orders = await _service.ListAsync(status, supplierId);
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(int id)
    {
        var order = await _service.GetByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create(UpsertPurchaseOrderDto dto)
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
            return Conflict(new { message = $"Nomor PO '{dto.OrderNo}' sudah dipakai." });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpsertPurchaseOrderDto dto)
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
            return Conflict(new { message = $"Nomor PO '{dto.OrderNo}' sudah dipakai." });
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

    [HttpPost("{id:int}/send")]
    public async Task<ActionResult<PurchaseOrderDto>> Send(int id)
    {
        try
        {
            return Ok(await _service.SendAsync(id));
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpPost("{id:int}/receive")]
    public async Task<ActionResult<PurchaseOrderDto>> Receive(int id, ReceivePurchaseOrderDto dto)
    {
        try
        {
            return Ok(await _service.ReceiveAsync(id, dto));
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<PurchaseOrderDto>> Cancel(int id)
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
