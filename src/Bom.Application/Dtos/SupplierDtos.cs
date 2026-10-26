using System.ComponentModel.DataAnnotations;

namespace Bom.Application.Dtos;

/// <summary>Representasi baca untuk Supplier.</summary>
public record SupplierDto(
    int Id,
    string Code,
    string Name,
    int? LeadTimeDays,
    bool IsActive);

/// <summary>Payload create/update untuk Supplier.</summary>
public record UpsertSupplierDto(
    [Required, StringLength(20)] string Code,
    [Required, StringLength(150)] string Name,
    [Range(0, int.MaxValue)] int? LeadTimeDays,
    bool IsActive = true);
