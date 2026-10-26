using Bom.Application.Dtos;
using Bom.Domain.Entities;
using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bom.Api.Controllers;

/// <summary>Setelan penetapan harga global (singleton, Id = 1). GET + PUT (upsert).</summary>
[ApiController]
[Route("api/pricing-settings")]
public class PricingSettingsController : ControllerBase
{
    private const int SingletonId = 1;

    private readonly BomDbContext _db;

    public PricingSettingsController(BomDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PricingSettingDto>> Get()
    {
        var setting = await _db.PricingSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == SingletonId) ?? new PricingSetting();

        return Ok(ToDto(setting));
    }

    [HttpPut]
    public async Task<ActionResult<PricingSettingDto>> Update(UpsertPricingSettingDto dto)
    {
        var setting = await _db.PricingSettings.FirstOrDefaultAsync(s => s.Id == SingletonId);
        if (setting is null)
        {
            setting = new PricingSetting { Id = SingletonId };
            _db.PricingSettings.Add(setting);
        }

        setting.DefaultTargetMarginPercent = dto.DefaultTargetMarginPercent;
        setting.DefaultTaxPercent = dto.DefaultTaxPercent;
        setting.MinMarginPercent = dto.MinMarginPercent;
        setting.RoundingRule = dto.RoundingRule;

        await _db.SaveChangesAsync();

        return Ok(ToDto(setting));
    }

    private static PricingSettingDto ToDto(PricingSetting s) => new(
        s.DefaultTargetMarginPercent,
        s.DefaultTaxPercent,
        s.MinMarginPercent,
        s.RoundingRule);
}
