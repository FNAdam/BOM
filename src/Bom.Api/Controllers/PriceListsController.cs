using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Bom.Api.Controllers;

/// <summary>
/// Daftar harga (price list) tersimpan: CRUD + aktivasi (satu Active per produk).
/// </summary>
[ApiController]
[Route("api/price-lists")]
public class PriceListsController : ControllerBase
{
    private readonly IPricingService _service;

    public PriceListsController(IPricingService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PriceListListItemDto>>> GetAll(
        [FromQuery] int? productItemId,
        [FromQuery] PriceListStatus? status)
    {
        var lists = await _service.ListPriceListsAsync(productItemId, status);
        return Ok(lists);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PriceListDto>> GetById(int id)
    {
        var list = await _service.GetPriceListAsync(id);
        return list is null ? NotFound() : Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<PriceListDto>> Create(SavePriceListRequest request)
    {
        try
        {
            var created = await _service.CreatePriceListAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PriceListDto>> Update(int id, SavePriceListRequest request)
    {
        try
        {
            var updated = await _service.UpdatePriceListAsync(id, request);
            return Ok(updated);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeletePriceListAsync(id);
            return NoContent();
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<PriceListDto>> Activate(int id)
    {
        try
        {
            var activated = await _service.ActivatePriceListAsync(id);
            return Ok(activated);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    private ObjectResult FromException(BomServiceException ex) =>
        StatusCode(ex.StatusCode, new { message = ex.Message });
}
