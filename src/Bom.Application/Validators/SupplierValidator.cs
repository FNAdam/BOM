using Bom.Application.Dtos;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>Validasi payload create/update Supplier: kode & nama wajib, lead time tidak negatif.</summary>
public class UpsertSupplierValidator : AbstractValidator<UpsertSupplierDto>
{
    public UpsertSupplierValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code wajib diisi.")
            .MaximumLength(20);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name wajib diisi.")
            .MaximumLength(150);

        RuleFor(x => x.LeadTimeDays)
            .GreaterThanOrEqualTo(0).When(x => x.LeadTimeDays.HasValue)
            .WithMessage("LeadTimeDays tidak boleh negatif.");
    }
}
