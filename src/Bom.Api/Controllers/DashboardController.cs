using Bom.Application.Dtos;
using Bom.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bom.Api.Controllers;

/// <summary>
/// Endpoint laporan dashboard (Fase 7). Menyajikan ringkasan margin per produk
/// untuk mendeteksi produk dengan margin di bawah ambang minimum.
/// </summary>
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    /// <summary>Laporan margin per produk beserta ringkasan agregat.</summary>
    [HttpGet("api/dashboard/margin")]
    public async Task<ActionResult<DashboardMarginDto>> GetMargin(CancellationToken ct)
    {
        var report = await _service.GetMarginReportAsync(ct);
        return Ok(report);
    }
}
