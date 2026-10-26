using Bom.Application.Dtos;
using Bom.Domain.Enums;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>
/// Validasi permintaan simulasi harga: produk wajib dipilih, untuk metode Margin TargetPercent &lt; 1
/// (margin 100% mustahil), dan PPN tidak negatif bila diisi.
/// </summary>
public class SimulatePricingRequestValidator : AbstractValidator<SimulatePricingRequest>
{
    public SimulatePricingRequestValidator()
    {
        RuleFor(x => x.ProductItemId).GreaterThan(0).WithMessage("ProductItemId wajib dipilih.");

        RuleFor(x => x.TargetPercent)
            .LessThan(1)
            .When(x => x.PricingMethod == PricingMethod.Margin && x.TargetPercent.HasValue)
            .WithMessage("TargetPercent untuk metode Margin harus < 1.");

        RuleFor(x => x.TaxPercent)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TaxPercent.HasValue)
            .WithMessage("TaxPercent (PPN) tidak boleh negatif.");
    }
}
