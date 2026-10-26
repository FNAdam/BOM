using Bom.Application.Dtos;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>
/// Validasi payload create/update BOM: produk &amp; satuan keluaran wajib, OutputQuantity &gt; 0,
/// YieldPercent di rentang (0,1], minimal satu komponen, serta validasi tiap komponen/operasi.
/// </summary>
public class UpsertBomValidator : AbstractValidator<UpsertBomDto>
{
    public UpsertBomValidator()
    {
        RuleFor(x => x.ProductItemId).GreaterThan(0).WithMessage("ProductItemId wajib dipilih.");
        RuleFor(x => x.OutputUomId).GreaterThan(0).WithMessage("OutputUomId wajib dipilih.");

        RuleFor(x => x.OutputQuantity)
            .GreaterThan(0).WithMessage("OutputQuantity harus lebih besar dari 0.");

        RuleFor(x => x.YieldPercent)
            .GreaterThan(0).WithMessage("YieldPercent harus lebih besar dari 0.")
            .LessThanOrEqualTo(1).WithMessage("YieldPercent tidak boleh lebih dari 1 (100%).");

        RuleFor(x => x.Components)
            .NotEmpty().WithMessage("BOM harus memiliki minimal satu komponen.");

        RuleForEach(x => x.Components).SetValidator(new UpsertBomComponentValidator());
        RuleForEach(x => x.Operations).SetValidator(new UpsertBomOperationValidator());
    }
}

/// <summary>Validasi baris komponen BOM: item &amp; satuan wajib, Quantity &gt; 0, Scrap &gt;= 0.</summary>
public class UpsertBomComponentValidator : AbstractValidator<UpsertBomComponentDto>
{
    public UpsertBomComponentValidator()
    {
        RuleFor(x => x.ComponentItemId).GreaterThan(0).WithMessage("ComponentItemId wajib dipilih.");
        RuleFor(x => x.UomId).GreaterThan(0).WithMessage("UomId wajib dipilih.");

        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity komponen harus lebih besar dari 0.");
        RuleFor(x => x.ScrapPercent).GreaterThanOrEqualTo(0).WithMessage("ScrapPercent tidak boleh negatif.");
    }
}

/// <summary>Validasi baris operasi BOM: nama wajib, biaya labor &amp; overhead tidak negatif.</summary>
public class UpsertBomOperationValidator : AbstractValidator<UpsertBomOperationDto>
{
    public UpsertBomOperationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama operasi wajib diisi.")
            .MaximumLength(100);

        RuleFor(x => x.LaborCostPerOutputUnit)
            .GreaterThanOrEqualTo(0).WithMessage("LaborCostPerOutputUnit tidak boleh negatif.");
        RuleFor(x => x.OverheadRate)
            .GreaterThanOrEqualTo(0).WithMessage("OverheadRate tidak boleh negatif.");
    }
}
