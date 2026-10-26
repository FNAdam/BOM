using System.ComponentModel.DataAnnotations;
using Bom.Domain.Enums;

namespace Bom.Application.Dtos;

/// <summary>Perintah produksi lengkap: header + baris konsumsi komponen.</summary>
public record ProductionOrderDto(
    int Id,
    int BomId,
    int BomVersion,
    int ProductItemId,
    string ProductCode,
    string ProductName,
    string OutputUomCode,
    string BatchNo,
    decimal PlannedQty,
    decimal? ActualQty,
    ProductionStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Notes,
    IReadOnlyList<ProductionConsumptionDto> Consumptions);

/// <summary>Baris konsumsi komponen (planned + actual, dalam base UoM komponen).</summary>
public record ProductionConsumptionDto(
    int Id,
    int ComponentItemId,
    string ComponentCode,
    string ComponentName,
    string UomCode,
    decimal PlannedQty,
    decimal ActualQty,
    decimal ActualUnitCost,
    decimal ActualCost);

/// <summary>Ringkasan perintah produksi untuk tampilan daftar.</summary>
public record ProductionOrderListDto(
    int Id,
    string BatchNo,
    int BomId,
    int ProductItemId,
    string ProductCode,
    string ProductName,
    string OutputUomCode,
    decimal PlannedQty,
    decimal? ActualQty,
    ProductionStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate);

/// <summary>Payload create/update perintah produksi.</summary>
public record UpsertProductionOrderDto(
    [Range(1, int.MaxValue, ErrorMessage = "BomId wajib dipilih.")] int BomId,
    [Range(0.000001, double.MaxValue, ErrorMessage = "PlannedQty harus lebih besar dari 0.")] decimal PlannedQty,
    [Required, StringLength(40)] string BatchNo,
    DateOnly? StartDate,
    DateOnly? EndDate,
    [StringLength(500)] string? Notes);

/// <summary>Payload catat konsumsi aktual (per komponen) pada order Open.</summary>
public record RecordConsumptionDto(List<RecordConsumptionLineDto> Lines);

/// <summary>Satu baris konsumsi aktual.</summary>
public record RecordConsumptionLineDto(
    int ComponentItemId,
    [Range(0, double.MaxValue, ErrorMessage = "ActualQty tidak boleh negatif.")] decimal ActualQty);

/// <summary>Payload tutup order (keluaran aktual).</summary>
public record CloseProductionDto(
    [Range(0.000001, double.MaxValue, ErrorMessage = "ActualQty harus lebih besar dari 0.")] decimal ActualQty);

/// <summary>
/// Laporan COGS aktual sebuah order yang telah ditutup, beserta analisis variance
/// terhadap biaya standar (dari CostingService).
/// </summary>
public record CogsReportDto(
    int OrderId,
    decimal ActualQty,
    decimal MaterialCost,
    decimal ConversionCost,
    decimal CogsTotal,
    decimal CogsUnit,
    decimal StandardTotal,
    decimal TotalVariance,
    decimal MaterialVariance,
    decimal YieldVariance,
    IReadOnlyList<CogsVarianceLineDto> Lines);

/// <summary>
/// Variance per komponen. PriceVariance = (ActualUnitCost - StdUnitCost) * ActualQty;
/// UsageVariance = (ActualQty - StdQtyAllowed) * StdUnitCost. Positif = tidak menguntungkan
/// (biaya aktual di atas standar).
/// </summary>
public record CogsVarianceLineDto(
    int ComponentId,
    string Code,
    string Name,
    string UomCode,
    decimal ActualQty,
    decimal ActualUnitCost,
    decimal ActualCost,
    decimal StdQtyAllowed,
    decimal StdUnitCost,
    decimal PriceVariance,
    decimal UsageVariance);

/// <summary>Satu baris buku besar pergerakan stok.</summary>
public record StockMovementDto(
    int Id,
    int ItemId,
    string ItemCode,
    string ItemName,
    StockMovementType Type,
    decimal Qty,
    decimal UnitCost,
    DateOnly MovementDate,
    string? RefType,
    int? RefId,
    string? Notes);
