using System.ComponentModel.DataAnnotations;

namespace Bom.Application.Dtos;

/// <summary>Representasi baca untuk harga item-supplier (termasuk nama/kode turunan).</summary>
public record ItemSupplierPriceDto(
    int Id,
    int ItemId,
    string ItemName,
    int SupplierId,
    string SupplierName,
    int UomId,
    string UomCode,
    decimal Price,
    string Currency,
    decimal MinOrderQty,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsPreferred);

/// <summary>Payload create/update untuk harga item-supplier.</summary>
public record UpsertItemSupplierPriceDto(
    [Range(1, int.MaxValue, ErrorMessage = "ItemId wajib dipilih.")] int ItemId,
    [Range(1, int.MaxValue, ErrorMessage = "SupplierId wajib dipilih.")] int SupplierId,
    [Range(1, int.MaxValue, ErrorMessage = "UomId wajib dipilih.")] int UomId,
    [Range(0, double.MaxValue)] decimal Price,
    [Required, StringLength(3)] string Currency = "IDR",
    [Range(0, double.MaxValue)] decimal MinOrderQty = 0m,
    DateOnly? EffectiveFrom = null,
    DateOnly? EffectiveTo = null,
    bool IsPreferred = false);
