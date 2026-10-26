using Bom.Application.Dtos;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>
/// Validasi payload create/update Item. Melengkapi DataAnnotations yang sudah ada:
/// kode & nama wajib, satuan dasar harus dipilih, biaya material tidak negatif.
/// </summary>
public class UpsertItemValidator : AbstractValidator<UpsertItemDto>
{
    public UpsertItemValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code wajib diisi.")
            .MaximumLength(30);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name wajib diisi.")
            .MaximumLength(150);

        RuleFor(x => x.BaseUomId)
            .GreaterThan(0).WithMessage("BaseUomId wajib dipilih.");

        RuleFor(x => x.StandardMaterialCost)
            .GreaterThanOrEqualTo(0).WithMessage("StandardMaterialCost tidak boleh negatif.");

        RuleFor(x => x.Density)
            .GreaterThan(0).When(x => x.Density.HasValue)
            .WithMessage("Density harus lebih besar dari 0 bila diisi.");
    }
}
