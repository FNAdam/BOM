using Bom.Application.Dtos;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>Validasi payload create/update UOM: kode & nama wajib, FactorToBase &gt; 0.</summary>
public class UpsertUomValidator : AbstractValidator<UpsertUomDto>
{
    public UpsertUomValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code wajib diisi.")
            .MaximumLength(10);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name wajib diisi.")
            .MaximumLength(50);

        RuleFor(x => x.FactorToBase)
            .GreaterThan(0).WithMessage("FactorToBase harus lebih besar dari 0.");
    }
}
