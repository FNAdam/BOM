using Bom.Application.Dtos;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>Validasi payload create/update kategori item: nama wajib.</summary>
public class UpsertItemCategoryValidator : AbstractValidator<UpsertItemCategoryDto>
{
    public UpsertItemCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name wajib diisi.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(300);
    }
}
