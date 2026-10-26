using Bom.Domain.Enums;
using BomEntity = Bom.Domain.Entities.Bom;

namespace Bom.Domain.Entities;

/// <summary>
/// Perintah produksi (production order) untuk membuat satu batch produk dari sebuah BOM.
/// Kuantitas dinyatakan dalam satuan keluaran BOM (OutputUom); untuk data contoh/produk F&amp;B
/// OutputUom = base UoM produk sehingga <see cref="ActualQty"/> langsung dipakai saat penerimaan
/// produk jadi ke stok.
/// </summary>
public class ProductionOrder
{
    public int Id { get; set; }

    /// <summary>BOM (resep) yang dipakai; harus Active saat order dibuat.</summary>
    public int BomId { get; set; }

    /// <summary>Item produk keluaran (disalin dari <see cref="Bom.ProductItemId"/>).</summary>
    public int ProductItemId { get; set; }

    /// <summary>Nomor batch produksi.</summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>Rencana keluaran dalam OutputUom BOM.</summary>
    public decimal PlannedQty { get; set; }

    /// <summary>Keluaran aktual, diisi saat order ditutup.</summary>
    public decimal? ActualQty { get; set; }

    public ProductionStatus Status { get; set; } = ProductionStatus.Open;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public BomEntity? Bom { get; set; }
    public Item? ProductItem { get; set; }
    public ICollection<ProductionConsumption> Consumptions { get; set; } = new List<ProductionConsumption>();
}
