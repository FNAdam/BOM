namespace Bom.Domain.Enums;

/// <summary>
/// Status siklus hidup pesanan penjualan. Draft = bebas diedit/dihapus. Completed = sudah
/// diposting ke stok (StockMovementType.Sale) dan struk bisa dicetak - baris &amp; harga tidak
/// bisa diubah lagi. Cancelled = dibatalkan (hanya dari Draft, belum menyentuh stok).
/// </summary>
public enum SalesOrderStatus
{
    Draft,
    Completed,
    Cancelled
}
