using Bom.Application.Dtos;
using Bom.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bom.Api.Controllers;

/// <summary>
/// Endpoint MRP sederhana: explosion + netting rekursif atas BOM Active,
/// menghasilkan rencana produksi (Make) dan saran pembelian (Buy).
/// </summary>
[ApiController]
public class MrpController : ControllerBase
{
    private readonly IMrpService _service;

    public MrpController(IMrpService service) => _service = service;

    /// <summary>Jalankan MRP untuk sekumpulan demand produk (SemiFinished / FinishedGood).</summary>
    [HttpPost("api/mrp/explode")]
    public async Task<ActionResult<MrpResultDto>> Explode(MrpRequestDto request)
    {
        try
        {
            var result = await _service.ExplodeAsync(request);
            return Ok(result);
        }
        catch (BomServiceException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }
}
