using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Bom.Api.Controllers;

[ApiController]
[Route("api/boms")]
public class BomsController : ControllerBase
{
    private readonly IBomService _service;

    public BomsController(IBomService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BomListItemDto>>> GetAll(
        [FromQuery] int? productItemId,
        [FromQuery] BomStatus? status)
    {
        var boms = await _service.ListAsync(productItemId, status);
        return Ok(boms);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BomDto>> GetById(int id)
    {
        var bom = await _service.GetByIdAsync(id);
        return bom is null ? NotFound() : Ok(bom);
    }

    [HttpPost]
    public async Task<ActionResult<BomDto>> Create(UpsertBomDto dto)
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
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpsertBomDto dto)
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

    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<BomDto>> Activate(int id)
    {
        try
        {
            var bom = await _service.ActivateAsync(id);
            return Ok(bom);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpPost("{id:int}/new-version")]
    public async Task<ActionResult<BomDto>> NewVersion(int id)
    {
        try
        {
            var created = await _service.CreateNewVersionAsync(id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpGet("{id:int}/explode")]
    public async Task<ActionResult<BomExplosionNodeDto>> Explode(int id, [FromQuery] decimal? qty)
    {
        try
        {
            var tree = await _service.ExplodeAsync(id, qty);
            return Ok(tree);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    private ObjectResult FromException(BomServiceException ex) =>
        StatusCode(ex.StatusCode, new { message = ex.Message });
}
