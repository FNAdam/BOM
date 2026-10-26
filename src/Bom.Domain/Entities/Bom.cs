using Bom.Domain.Enums;

namespace Bom.Domain.Entities;

/// <summary>
/// Header BOM (resep) untuk satu produk (semi-finished / finished good).
/// Sebuah produk dapat memiliki beberapa versi; hanya satu yang boleh berstatus Active.
/// </summary>
public class Bom
{
    public int Id { get; set; }

    /// <summary>Item produk keluaran BOM ini (wajib SemiFinished / FinishedGood).</summary>
    public int ProductItemId { get; set; }

    /// <summary>Nomor versi resep untuk produk terkait (unik per produk).</summary>
    public int Version { get; set; } = 1;

    public BomStatus Status { get; set; } = BomStatus.Draft;

    /// <summary>Jumlah keluaran per satu batch resep (basis skala explode).</summary>
    public decimal OutputQuantity { get; set; } = 1m;

    /// <summary>Satuan ukur keluaran.</summary>
    public int OutputUomId { get; set; }

    /// <summary>Persentase yield efektif dalam rentang (0,1]. Default 1 = 100%.</summary>
    public decimal YieldPercent { get; set; } = 1m;

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public Item? ProductItem { get; set; }
    public Uom? OutputUom { get; set; }
    public ICollection<BomComponent> Components { get; set; } = new List<BomComponent>();
    public ICollection<BomOperation> Operations { get; set; } = new List<BomOperation>();
}
