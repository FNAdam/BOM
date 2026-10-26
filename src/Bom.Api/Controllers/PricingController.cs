using Bom.Application.Dtos;
using Bom.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bom.Api.Controllers;

/// <summary>
/// Simulasi harga jual (Fase 6): menghitung matriks harga jenjang × kemasan dari biaya
/// (standar / LastCOGS) tanpa menyimpan.
/// </summary>
[ApiController]
[Route("api/pricing")]
public class PricingController : ControllerBase
{
    private readonly IPricingService _service;

    public PricingController(IPricingService service) => _service = service;

    [HttpPost("simulate")]
    public async Task<ActionResult<PricingResultDto>> Simulate(SimulatePricingRequest request)
    {
        try
        {
            var result = await _service.SimulateAsync(request);
            return Ok(result);
        }
        catch (BomServiceException ex)
        {
            return FromException(ex);
        }
    }

    private ObjectResult FromException(BomServiceException ex) =>
        StatusCode(ex.StatusCode, new { message = ex.Message });
}
