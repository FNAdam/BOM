using System.ComponentModel.DataAnnotations;
using Bom.Domain.Enums;

namespace Bom.Application.Dtos;

/// <summary>Representasi baca lengkap untuk sebuah BOM beserta komponen & operasinya.</summary>
public record BomDto(
    int Id,
    int ProductItemId,
    string ProductCode,
    string ProductName,
    int Version,
    BomStatus Status,
    decimal OutputQuantity,
    int OutputUomId,
    string OutputUomCode,
    decimal YieldPercent,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Notes,
    IReadOnlyList<BomComponentDto> Components,
    IReadOnlyList<BomOperationDto> Operations);

/// <summary>Baris komponen dalam BOM (untuk baca).</summary>
public record BomComponentDto(
    int Id,
    int ComponentItemId,
    string ComponentCode,
    string ComponentName,
    decimal Quantity,
    int UomId,
    string UomCode,
    decimal ScrapPercent,
    int Sequence,
    bool IsOptional,
    bool HasBom);

/// <summary>Langkah operasi/biaya konversi dalam BOM (untuk baca).</summary>
public record BomOperationDto(
    int Id,
    int Sequence,
    string Name,
    decimal LaborCostPerOutputUnit,
    decimal OverheadRate);

/// <summary>Ringkasan BOM untuk tampilan daftar.</summary>
public record BomListItemDto(
    int Id,
    int ProductItemId,
    string ProductCode,
    string ProductName,
    int Version,
    BomStatus Status,
    decimal OutputQuantity,
    int OutputUomId,
    string OutputUomCode,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    int ComponentCount);

/// <summary>Payload create/update untuk BOM (nested components & operations).</summary>
public record UpsertBomDto(
    [Range(1, int.MaxValue, ErrorMessage = "ProductItemId wajib dipilih.")] int ProductItemId,
    [Range(0, double.MaxValue)] decimal OutputQuantity,
    [Range(1, int.MaxValue, ErrorMessage = "OutputUomId wajib dipilih.")] int OutputUomId,
    [Range(0, double.MaxValue)] decimal YieldPercent,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    [StringLength(500)] string? Notes,
    List<UpsertBomComponentDto> Components,
    List<UpsertBomOperationDto> Operations);

/// <summary>Payload baris komponen (nested dalam UpsertBomDto).</summary>
public record UpsertBomComponentDto(
    [Range(1, int.MaxValue, ErrorMessage = "ComponentItemId wajib dipilih.")] int ComponentItemId,
    [Range(0, double.MaxValue)] decimal Quantity,
    [Range(1, int.MaxValue, ErrorMessage = "UomId wajib dipilih.")] int UomId,
    [Range(0, double.MaxValue)] decimal ScrapPercent,
    int Sequence,
    bool IsOptional,
    [StringLength(300)] string? Notes);

/// <summary>Payload baris operasi (nested dalam UpsertBomDto).</summary>
public record UpsertBomOperationDto(
    int Sequence,
    [Required, StringLength(100)] string Name,
    [Range(0, double.MaxValue)] decimal LaborCostPerOutputUnit,
    [Range(0, double.MaxValue)] decimal OverheadRate);

/// <summary>Node pohon hasil explode BOM multi-level.</summary>
public record BomExplosionNodeDto(
    int ItemId,
    string ItemCode,
    string ItemName,
    ItemType ItemType,
    decimal Quantity,
    string UomCode,
    decimal ScrapPercent,
    decimal EffectiveQuantity,
    int Level,
    bool HasBom,
    IReadOnlyList<BomExplosionNodeDto> Children);
