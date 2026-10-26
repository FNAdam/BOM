using System.ComponentModel.DataAnnotations;
using Bom.Domain.Enums;

namespace Bom.Application.Dtos;

/// <summary>Satu baris permintaan produksi (demand) untuk MRP.</summary>
public record MrpDemandInputDto(
    [Range(1, int.MaxValue, ErrorMessage = "ProductItemId wajib dipilih.")] int ProductItemId,
    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity harus lebih besar dari 0.")] decimal Quantity);

/// <summary>Payload MRP explosion: daftar demand produk (qty dalam base UoM produk).</summary>
public record MrpRequestDto(
    [Required, MinLength(1, ErrorMessage = "Minimal satu baris demand diperlukan.")]
    IReadOnlyList<MrpDemandInputDto> Demands);

/// <summary>Demand yang diselesaikan (dengan kode/nama produk dan satuan) untuk ditampilkan kembali.</summary>
public record MrpDemandLineDto(
    int ProductItemId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    string UomCode);

/// <summary>
/// Satu baris hasil MRP untuk sebuah item, teragregasi dalam base UoM item.
///
/// <para><b>Make</b>: item punya BOM Active dan <see cref="NetQty"/> &gt; 0 (planned production).
/// Field pembelian (Supplier*/UnitCost/*OrderQty/*Cost/LeadTimeDays) bernilai null.</para>
/// <para><b>Buy</b>: item tanpa BOM Active (saran PO). Field pembelian diisi dari supplier preferred
/// efektif (fallback StandardMaterialCost bila tak ada harga).</para>
/// </summary>
public record MrpLineDto(
    int ItemId,
    string Code,
    string Name,
    ItemType ItemType,
    string Source, // "Buy" | "Make"
    decimal GrossQty,
    decimal OnHandQty,
    decimal NetQty,
    string UomCode,
    int? SupplierId,
    string? SupplierName,
    decimal? UnitCost,
    decimal? MinOrderQty,
    decimal? SuggestedOrderQty,
    int? LeadTimeDays,
    decimal? ExtendedCost);

/// <summary>Hasil MRP: demand yang diproses, baris kebutuhan (Make/Buy), dan total biaya pembelian.</summary>
public record MrpResultDto(
    IReadOnlyList<MrpDemandLineDto> Demands,
    IReadOnlyList<MrpLineDto> Lines,
    decimal TotalPurchaseCost);
