namespace Bom.Domain.Enums;

/// <summary>
/// Jenis pergerakan stok pada buku besar persediaan (<see cref="Entities.StockMovement"/>).
///
/// Konvensi: <see cref="Entities.StockMovement.Qty"/> selalu disimpan sebagai <b>magnitudo positif</b>;
/// arah (masuk/keluar) ditentukan oleh tipe ini:
/// masuk = <see cref="Receipt"/>, <see cref="ProductionReceipt"/>;
/// keluar = <see cref="Issue"/>, <see cref="ProductionIssue"/>;
/// <see cref="Adjustment"/> = penyesuaian (delta, boleh menaikkan/menurunkan).
/// </summary>
public enum StockMovementType
{
    /// <summary>Penerimaan pembelian (stok masuk).</summary>
    Receipt,

    /// <summary>Pengeluaran umum non-produksi (stok keluar).</summary>
    Issue,

    /// <summary>Pengeluaran komponen ke produksi saat order ditutup (stok keluar).</summary>
    ProductionIssue,

    /// <summary>Penerimaan produk jadi hasil produksi (stok masuk).</summary>
    ProductionReceipt,

    /// <summary>Penyesuaian saldo (stock opname / koreksi).</summary>
    Adjustment
}
