using Bom.Application.Dtos;
using FluentValidation;

namespace Bom.Application.Validators;

/// <summary>
/// Validasi payload create/update pesanan pembelian: supplier &amp; nomor PO wajib, minimal satu
/// baris, serta validasi tiap baris.
/// </summary>
public class UpsertPurchaseOrderValidator : AbstractValidator<UpsertPurchaseOrderDto>
{
    public UpsertPurchaseOrderValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Supplier wajib dipilih.");
        RuleFor(x => x.OrderNo)
            .NotEmpty().WithMessage("Nomor PO wajib diisi.")
            .MaximumLength(40);

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Pesanan pembelian harus memiliki minimal satu baris.");

        RuleForEach(x => x.Lines).SetValidator(new UpsertPurchaseOrderLineValidator());
    }
}

/// <summary>Validasi baris pesanan pembelian: item &amp; satuan wajib, qty &gt; 0, harga &gt;= 0.</summary>
public class UpsertPurchaseOrderLineValidator : AbstractValidator<UpsertPurchaseOrderLineDto>
{
    public UpsertPurchaseOrderLineValidator()
    {
        RuleFor(x => x.ItemId).GreaterThan(0).WithMessage("Item wajib dipilih.");
        RuleFor(x => x.UomId).GreaterThan(0).WithMessage("Satuan wajib dipilih.");
        RuleFor(x => x.QtyOrdered).GreaterThan(0).WithMessage("Qty dipesan harus lebih besar dari 0.");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Harga tidak boleh negatif.");
    }
}

/// <summary>Validasi penerimaan barang: minimal satu baris, qty diterima &gt; 0.</summary>
public class ReceivePurchaseOrderValidator : AbstractValidator<ReceivePurchaseOrderDto>
{
    public ReceivePurchaseOrderValidator()
    {
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Isi minimal satu baris yang diterima.");

        RuleForEach(x => x.Lines).SetValidator(new ReceivePurchaseOrderLineValidator());
    }
}

public class ReceivePurchaseOrderLineValidator : AbstractValidator<ReceivePurchaseOrderLineDto>
{
    public ReceivePurchaseOrderLineValidator()
    {
        RuleFor(x => x.LineId).GreaterThan(0);
        RuleFor(x => x.QtyReceiving).GreaterThan(0).WithMessage("Qty diterima harus lebih besar dari 0.");
    }
}
