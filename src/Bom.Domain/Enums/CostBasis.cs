namespace Bom.Domain.Enums;

/// <summary>
/// Dasar biaya yang dipakai saat menetapkan harga.
/// Standard = biaya standar per unit (cost roll-up dari CostingService);
/// LastCOGS = COGS/unit produksi terakhir (UnitCost pergerakan ProductionReceipt terbaru).
/// </summary>
public enum CostBasis
{
    Standard,
    LastCOGS
}
