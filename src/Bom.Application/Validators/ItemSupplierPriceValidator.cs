using Bom.Application.Dtos;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>
/// Validasi payload harga item-supplier: referensi wajib dipilih, harga &amp; MOQ tidak negatif,
/// Currency 3 huruf, dan periode efektif konsisten (EffectiveTo &gt;= EffectiveFrom bila keduanya diisi).
/// </summary>
public class UpsertItemSupplierPriceValidator : AbstractValidator<UpsertItemSupplierPriceDto>
{
    public UpsertItemSupplierPriceValidator()
    {
        RuleFor(x => x.ItemId).GreaterThan(0).WithMessage("ItemId wajib dipilih.");
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("SupplierId wajib dipilih.");
        RuleFor(x => x.UomId).GreaterThan(0).WithMessage("UomId wajib dipilih.");

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price tidak boleh negatif.");
        RuleFor(x => x.MinOrderQty).GreaterThanOrEqualTo(0).WithMessage("MinOrderQty tidak boleh negatif.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency wajib diisi.")
            .Length(3).WithMessage("Currency harus 3 huruf (mis. IDR).");

        RuleFor(x => x.EffectiveTo)
            .Must((dto, to) => dto.EffectiveFrom is null || to is null || to >= dto.EffectiveFrom)
            .WithMessage("EffectiveTo tidak boleh lebih awal dari EffectiveFrom.");
    }
}
