using Bom.Application.Dtos;
using Bom.Application.Validators;
using Bom.Domain.Enums;
using Xunit;

namespace Bom.Tests;

/// <summary>
/// Uji unit validator kunci (tanpa host). Memastikan aturan bisnis ditegakkan FluentValidation
/// sebelum menyentuh controller/service.
/// </summary>
public class ValidatorTests
{
    private static UpsertBomDto MakeBom(decimal yield, params UpsertBomComponentDto[] components) =>
        new(
            ProductItemId: 1,
            OutputQuantity: 100m,
            OutputUomId: 5,
            YieldPercent: yield,
            EffectiveFrom: null,
            EffectiveTo: null,
            Notes: null,
            Components: components.ToList(),
            Operations: new List<UpsertBomOperationDto>());

    private static UpsertBomComponentDto MakeComponent() =>
        new(ComponentItemId: 2, Quantity: 1m, UomId: 5, ScrapPercent: 0m, Sequence: 1, IsOptional: false, Notes: null);

    // ----------------------------------------------------------------- BOM yield

    [Theory]
    [InlineData(1.5)]
    [InlineData(0)]
    [InlineData(-0.1)]
    public void BomValidator_YieldOutsideZeroToOne_IsInvalid(double yield)
    {
        var result = new UpsertBomValidator().Validate(MakeBom((decimal)yield, MakeComponent()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertBomDto.YieldPercent));
    }

    [Fact]
    public void BomValidator_ValidYieldWithComponent_IsValid()
    {
        var result = new UpsertBomValidator().Validate(MakeBom(0.98m, MakeComponent()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void BomValidator_NoComponents_IsInvalid()
    {
        var result = new UpsertBomValidator().Validate(MakeBom(1m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertBomDto.Components));
    }

    // ----------------------------------------------------------------- MRP demands

    [Fact]
    public void MrpValidator_EmptyDemands_IsInvalid()
    {
        var result = new MrpRequestValidator().Validate(new MrpRequestDto(new List<MrpDemandInputDto>()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void MrpValidator_WithValidDemand_IsValid()
    {
        var result = new MrpRequestValidator().Validate(
            new MrpRequestDto(new List<MrpDemandInputDto> { new(ProductItemId: 1, Quantity: 10m) }));

        Assert.True(result.IsValid);
    }

    // ----------------------------------------------------------------- Pricing target

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    public void PricingValidator_MarginTargetAtOrAboveOne_IsInvalid(double target)
    {
        var req = new SimulatePricingRequest(
            ProductItemId: 1,
            PricingMethod: PricingMethod.Margin,
            CostBasis: CostBasis.Standard,
            TaxMode: TaxMode.Exclusive,
            TargetPercent: (decimal)target);

        var result = new SimulatePricingRequestValidator().Validate(req);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SimulatePricingRequest.TargetPercent));
    }

    [Fact]
    public void PricingValidator_MarginTargetBelowOne_IsValid()
    {
        var req = new SimulatePricingRequest(
            ProductItemId: 1,
            PricingMethod: PricingMethod.Margin,
            CostBasis: CostBasis.Standard,
            TaxMode: TaxMode.Exclusive,
            TargetPercent: 0.45m);

        var result = new SimulatePricingRequestValidator().Validate(req);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void PricingValidator_MarkupTargetAboveOne_IsValid()
    {
        // Untuk metode Markup, target >= 1 (markup 150%) tetap sah.
        var req = new SimulatePricingRequest(
            ProductItemId: 1,
            PricingMethod: PricingMethod.Markup,
            CostBasis: CostBasis.Standard,
            TaxMode: TaxMode.Exclusive,
            TargetPercent: 1.5m);

        var result = new SimulatePricingRequestValidator().Validate(req);

        Assert.True(result.IsValid);
    }
}
