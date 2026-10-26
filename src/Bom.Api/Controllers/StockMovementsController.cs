using Bom.Application.Dtos;
using Bom.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bom.Api.Controllers;

/// <summary>
/// Buku besar pergerakan stok (inventory ledger) hasil produksi/penerimaan/pengeluaran.
/// Read-only; ditulis oleh <see cref="IProductionService"/> saat order ditutup.
/// </summary>
[ApiController]
[Route("api/stock-movements")]
public class StockMovementsController : ControllerBase
{
    private readonly IProductionService _service;

    public StockMovementsController(IProductionService service) => _service = service;

    /// <summary>Daftar pergerakan stok; opsional difilter per item, terbaru lebih dulu.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StockMovementDto>>> GetAll([FromQuery] int? itemId)
    {
        var movements = await _service.ListStockMovementsAsync(itemId);
        return Ok(movements);
    }
}
