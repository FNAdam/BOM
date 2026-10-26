using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Bom.Api.Controllers;

/// <summary>
/// Perintah produksi &amp; COGS aktual (Fase 5). Membuat order dari BOM Active, mencatat konsumsi,
/// menutup order (posting ledger rata-rata bergerak) dan mengembalikan laporan COGS + variance.
/// </summary>
[ApiController]
[Route("api/production-orders")]
public class ProductionOrdersController : ControllerBase
{
    private readonly IProductionService _service;

    public ProductionOrdersController(IProductionService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductionOrderListDto>>> GetAll(
        [FromQuery] ProductionStatus? status,
        [FromQuery] int? productItemId)
    {
        var orders = await _service.ListAsync(status, productItemId);
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductionOrderDto>> GetById(int id)
    {
        var order = await _service.GetByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<ProductionOrderDto>> Create(UpsertProductionOrderDto dto)
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
    public async Task<IActionResult> Update(int id, UpsertProductionOrderDto dto)
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

    [HttpPost("{id:int}/consumption")]
    public async Task<ActionResult<ProductionOrderDto>> RecordConsumption(int id, RecordConsumptionDto dto)
    {
        try
        {
            var order = await _service.RecordConsumptionAsync(id, dto);
            return Ok(order);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<CogsReportDto>> Close(int id, CloseProductionDto dto)
    {
        try
        {
            var report = await _service.CloseAsync(id, dto);
            return Ok(report);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    [HttpGet("{id:int}/cogs")]
    public async Task<ActionResult<CogsReportDto>> GetCogs(int id)
    {
        try
        {
            var report = await _service.GetCogsAsync(id);
            return Ok(report);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    private ObjectResult FromException(BomServiceException ex) =>
        StatusCode(ex.StatusCode, new { message = ex.Message });
}
