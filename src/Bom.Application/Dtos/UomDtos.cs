using System.ComponentModel.DataAnnotations;
using Bom.Domain.Enums;

namespace Bom.Application.Dtos;

/// <summary>Representasi baca untuk satuan ukur (UOM).</summary>
public record UomDto(
    int Id,
    string Code,
    string Name,
    UomType Type,
    decimal FactorToBase,
    bool IsBase);

/// <summary>Payload create/update untuk UOM.</summary>
public record UpsertUomDto(
    [Required, StringLength(10)] string Code,
    [Required, StringLength(50)] string Name,
    UomType Type,
    [Range(0.0000001, double.MaxValue, ErrorMessage = "FactorToBase harus lebih besar dari 0.")] decimal FactorToBase = 1m,
    bool IsBase = false);
