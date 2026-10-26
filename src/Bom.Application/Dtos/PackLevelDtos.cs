using System.ComponentModel.DataAnnotations;

namespace Bom.Application.Dtos;

/// <summary>Jenjang kemasan sebuah produk (Botol / Pack / Karton).</summary>
public record PackLevelDto(
    int Id,
    int FinishedItemId,
    string Name,
    decimal QtyPerBase,
    string? Barcode,
    int Sequence,
    bool IsDefaultSales);

/// <summary>Payload create/update jenjang kemasan.</summary>
public record UpsertPackLevelDto(
    [Required, StringLength(40)] string Name,
    [Range(0.000001, double.MaxValue, ErrorMessage = "QtyPerBase harus lebih besar dari 0.")] decimal QtyPerBase,
    [StringLength(40)] string? Barcode,
    int Sequence,
    bool IsDefaultSales);
