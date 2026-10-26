using System.ComponentModel.DataAnnotations;

namespace Bom.Application.Dtos;

/// <summary>Representasi baca untuk kategori item.</summary>
public record ItemCategoryDto(
    int Id,
    string Name,
    string? Description);

/// <summary>Payload create/update untuk kategori item.</summary>
public record UpsertItemCategoryDto(
    [Required, StringLength(100)] string Name,
    [StringLength(300)] string? Description);
