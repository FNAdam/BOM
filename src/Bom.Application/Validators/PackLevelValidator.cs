using Bom.Application.Dtos;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>Validasi payload jenjang kemasan: nama wajib, QtyPerBase &gt; 0.</summary>
public class UpsertPackLevelValidator : AbstractValidator<UpsertPackLevelDto>
{
    public UpsertPackLevelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name wajib diisi.")
            .MaximumLength(40);

        RuleFor(x => x.QtyPerBase)
            .GreaterThan(0).WithMessage("QtyPerBase harus lebih besar dari 0.");

        RuleFor(x => x.Barcode)
            .MaximumLength(40);
    }
}
