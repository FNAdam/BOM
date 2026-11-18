namespace Bom.Domain.Enums;

/// <summary>
/// Status siklus hidup sebuah purchase order (pesanan pembelian ke supplier).
/// Draft = masih bisa diedit/dihapus bebas. Sent = sudah "dikirim" ke supplier, siap menerima
/// barang. PartiallyReceived = sebagian baris sudah diterima, belum semua. Received = seluruh
/// baris sudah diterima penuh (qty diterima = qty dipesan di semua baris). Cancelled = dibatalkan
/// (boleh dari Draft/Sent/PartiallyReceived - sisa yang belum diterima dianggap tidak akan datang).
/// </summary>
public enum PurchaseOrderStatus
{
    Draft,
    Sent,
    PartiallyReceived,
    Received,
    Cancelled
}
