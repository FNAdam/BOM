using Bom.Application.Dtos;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>
/// Validasi payload create/update pesanan penjualan: minimal satu baris, PPN di rentang [0,1),
/// serta validasi tiap baris. Validasi produk harus bisa dijual (SemiFinished/FinishedGood) dan
/// kepemilikan PackLevel dilakukan di SalesOrderService (butuh akses DB).
/// </summary>
public class UpsertSalesOrderValidator : AbstractValidator<UpsertSalesOrderDto>
{
    public UpsertSalesOrderValidator()
    {
        RuleFor(x => x.OrderNo)
            .NotEmpty().WithMessage("Nomor pesanan/struk wajib diisi.")
            .MaximumLength(40);

        RuleFor(x => x.TaxPercent)
            .GreaterThanOrEqualTo(0).WithMessage("PPN tidak boleh negatif.")
            .LessThan(1).WithMessage("PPN harus di bawah 100% (isi desimal, mis. 0.11 untuk 11%).");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Pesanan penjualan harus memiliki minimal satu baris produk.");

        RuleForEach(x => x.Lines).SetValidator(new UpsertSalesOrderLineValidator());
    }
}

/// <summary>Validasi baris pesanan penjualan: produk wajib, qty &gt; 0, harga &gt;= 0.</summary>
public class UpsertSalesOrderLineValidator : AbstractValidator<UpsertSalesOrderLineDto>
{
    public UpsertSalesOrderLineValidator()
    {
        RuleFor(x => x.ProductItemId).GreaterThan(0).WithMessage("Produk wajib dipilih.");
        RuleFor(x => x.Qty).GreaterThan(0).WithMessage("Qty harus lebih besar dari 0.");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Harga tidak boleh negatif.");
    }
}
