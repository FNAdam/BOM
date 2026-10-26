using Bom.Application.Dtos;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>Validasi payload create/update perintah produksi: BOM wajib, PlannedQty &gt; 0, BatchNo wajib.</summary>
public class UpsertProductionOrderValidator : AbstractValidator<UpsertProductionOrderDto>
{
    public UpsertProductionOrderValidator()
    {
        RuleFor(x => x.BomId).GreaterThan(0).WithMessage("BomId wajib dipilih.");
        RuleFor(x => x.PlannedQty).GreaterThan(0).WithMessage("PlannedQty harus lebih besar dari 0.");
        RuleFor(x => x.BatchNo)
            .NotEmpty().WithMessage("BatchNo wajib diisi.")
            .MaximumLength(40);
    }
}

/// <summary>Validasi penutupan order: keluaran aktual &gt; 0.</summary>
public class CloseProductionValidator : AbstractValidator<CloseProductionDto>
{
    public CloseProductionValidator()
    {
        RuleFor(x => x.ActualQty).GreaterThan(0).WithMessage("ActualQty harus lebih besar dari 0.");
    }
}
