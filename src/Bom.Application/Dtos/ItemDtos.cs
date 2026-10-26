using System.ComponentModel.DataAnnotations;
using Bom.Domain.Enums;

namespace Bom.Application.Dtos;

/// <summary>Representasi baca untuk Item (termasuk data turunan navigasi).</summary>
public record ItemDto(
    int Id,
    string Code,
    string Name,
    ItemType Type,
    int BaseUomId,
    string BaseUomCode,
    int? CategoryId,
    string? CategoryName,
    decimal StandardMaterialCost,
    CostingMethod CostingMethod,
    decimal? Density,
    bool IsActive);

/// <summary>Payload create/update untuk Item.</summary>
public record UpsertItemDto(
    [Required, StringLength(30)] string Code,
    [Required, StringLength(150)] string Name,
    ItemType Type,
    [Range(1, int.MaxValue, ErrorMessage = "BaseUomId wajib dipilih.")] int BaseUomId,
    int? CategoryId,
    // [Range(0, double.MaxValue)] 
    [Range(0, 100000000)]
    decimal StandardMaterialCost,
    CostingMethod CostingMethod,
    decimal? Density,
    bool IsActive = true);

/// <summary>Kode berikutnya yang disarankan untuk sebuah tipe item (mis. "RW-0001").</summary>
public record NextItemCodeDto(string Code);
