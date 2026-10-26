namespace Bom.Application.Dtos;

/// <summary>
/// Rincian biaya standar sebuah item, dinyatakan per 1 satuan keluaran (OutputUom) produk.
/// Untuk item yang dibeli (tanpa BOM Active), <see cref="IsPurchased"/> = true dan
/// <see cref="Components"/> kosong.
/// </summary>
public record CostBreakdownDto(
    int ItemId,
    string ItemCode,
    string ItemName,
    string OutputUomCode,
    decimal MaterialCost,
    decimal LaborCost,
    decimal OverheadCost,
    decimal TotalCost,
    bool IsPurchased,
    IReadOnlyList<CostLineDto> Components);

/// <summary>
/// Satu baris komponen dalam pohon biaya. Nilai dinyatakan per 1 satuan keluaran produk induk.
/// </summary>
public record CostLineDto(
    int ItemId,
    string Code,
    string Name,
    // Kuantitas nominal komponen per 1 keluaran produk induk (belum termasuk scrap/yield).
    decimal QuantityPerOutput,
    string UomCode,
    decimal ScrapPercent,
    // Biaya per satu base UoM komponen.
    decimal UnitCost,
    // Biaya baris ini per 1 keluaran produk induk (sudah termasuk scrap dan yield).
    decimal ExtendedCost,
    bool HasBom,
    IReadOnlyList<CostLineDto>? Children);

/// <summary>Snapshot biaya tersimpan untuk sebuah item.</summary>
public record CostSnapshotDto(
    int Id,
    int ItemId,
    string ItemCode,
    string ItemName,
    DateOnly AsOfDate,
    decimal MaterialCost,
    decimal LaborCost,
    decimal OverheadCost,
    decimal TotalCost,
    int OutputUomId,
    string OutputUomCode,
    int? SourceBomId,
    int? BomVersion);
