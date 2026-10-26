using Bom.Application.Dtos;
using Bom.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bom.Api.Controllers;

/// <summary>
/// Endpoint costing standar (cost roll-up). Menghitung rincian biaya per 1 satuan
/// keluaran, menyimpan snapshot, dan membaca riwayat snapshot.
/// </summary>
[ApiController]
public class CostingController : ControllerBase
{
    private readonly ICostingService _service;

    public CostingController(ICostingService service) => _service = service;

    /// <summary>Hitung rincian biaya sebuah item tanpa menyimpan.</summary>
    [HttpGet("api/items/{itemId:int}/cost-breakdown")]
    public async Task<ActionResult<CostBreakdownDto>> GetBreakdown(int itemId)
    {
        try
        {
            var breakdown = await _service.GetCostBreakdownAsync(itemId);
            return Ok(breakdown);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    /// <summary>Hitung biaya lalu simpan sebagai snapshot.</summary>
    [HttpPost("api/costing/rollup/{itemId:int}")]
    public async Task<ActionResult<CostBreakdownDto>> Rollup(int itemId)
    {
        try
        {
            var breakdown = await _service.SaveSnapshotAsync(itemId);
            return Ok(breakdown);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    /// <summary>Daftar snapshot tersimpan; opsional difilter per item.</summary>
    [HttpGet("api/costing/snapshots")]
    public async Task<ActionResult<IEnumerable<CostSnapshotDto>>> GetSnapshots([FromQuery] int? itemId)
    {
        var snapshots = await _service.ListSnapshotsAsync(itemId);
        return Ok(snapshots);
    }

    private ObjectResult FromException(BomServiceException ex) =>
        StatusCode(ex.StatusCode, new { message = ex.Message });
}
