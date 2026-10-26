using Bom.Domain.Enums;

namespace Bom.Domain.Entities;

/// <summary>
/// Satu baris buku besar pergerakan stok (inventory ledger). Setiap perubahan
/// <see cref="InventoryBalance"/> menulis tepat satu baris di sini.
///
/// Konvensi tanda: <see cref="Qty"/> selalu <b>magnitudo positif</b>; arah masuk/keluar
/// ditentukan oleh <see cref="Type"/> (lihat <see cref="StockMovementType"/>).
/// <see cref="UnitCost"/> = biaya per base UoM pada saat pergerakan (untuk keluaran = rata-rata
/// bergerak saat itu; untuk masukan = biaya perolehan / COGS unit).
/// </summary>
public class StockMovement
{
    public int Id { get; set; }

    public int ItemId { get; set; }

    public StockMovementType Type { get; set; }

    /// <summary>Magnitudo kuantitas (selalu &gt;= 0) dalam base UoM item.</summary>
    public decimal Qty { get; set; }

    /// <summary>Biaya per base UoM saat pergerakan.</summary>
    public decimal UnitCost { get; set; }

    public DateOnly MovementDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Tipe dokumen sumber (mis. "ProductionOrder"); opsional.</summary>
    public string? RefType { get; set; }

    /// <summary>Id dokumen sumber (mis. ProductionOrder.Id); opsional.</summary>
    public int? RefId { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public Item? Item { get; set; }
}
