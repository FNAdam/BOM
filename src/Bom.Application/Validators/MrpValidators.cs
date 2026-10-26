using Bom.Application.Dtos;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>Validasi permintaan MRP: minimal satu baris demand, dan tiap demand valid.</summary>
public class MrpRequestValidator : AbstractValidator<MrpRequestDto>
{
    public MrpRequestValidator()
    {
        RuleFor(x => x.Demands)
            .NotEmpty().WithMessage("Minimal satu baris demand diperlukan.");

        RuleForEach(x => x.Demands).SetValidator(new MrpDemandInputValidator());
    }
}

/// <summary>Validasi satu baris demand MRP: produk wajib dipilih, Quantity &gt; 0.</summary>
public class MrpDemandInputValidator : AbstractValidator<MrpDemandInputDto>
{
    public MrpDemandInputValidator()
    {
        RuleFor(x => x.ProductItemId).GreaterThan(0).WithMessage("ProductItemId wajib dipilih.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity harus lebih besar dari 0.");
    }
}
