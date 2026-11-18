using Bom.Domain.Enums;

namespace Bom.Domain.Entities;

/// <summary>
/// Pesanan penjualan produk jadi/setengah jadi ke customer. Tidak ada master data Customer -
/// nama pembeli cuma teks bebas per transaksi. Begitu Completed, baris diposting ke
/// StockMovement (Type=Sale) lewat Application.IInventoryLedgerService, dan struk bisa dicetak
/// dari data pesanan ini (bukan entity terpisah).
/// </summary>
public class SalesOrder
{
    public int Id { get; set; }

    /// <summary>Nomor pesanan/struk, diisi manual (bebas format, mis. "INV-2026-11-001").</summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>Kanal penjualan (opsional - kosong berarti ex-factory/langsung).</summary>
    public int? SalesChannelId { get; set; }

    /// <summary>Nama pembeli, teks bebas (opsional).</summary>
    public string? CustomerName { get; set; }

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    public DateOnly OrderDate { get; set; }

    /// <summary>PPN dalam rentang [0,1) (mis. 0,11 = 11%). Ditambahkan di atas subtotal (exclusive).</summary>
    public decimal TaxPercent { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public SalesChannel? SalesChannel { get; set; }
    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
}
