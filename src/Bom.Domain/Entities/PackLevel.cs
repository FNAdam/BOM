namespace Bom.Domain.Entities;

/// <summary>
/// Jenjang kemasan sebuah produk jadi (mis. Botol, Pack, Karton). <see cref="QtyPerBase"/> = jumlah
/// satuan dasar (base UoM produk) per satu kemasan ini, dipakai untuk menskalakan harga jenjang
/// dari harga per satuan dasar menjadi harga per kemasan.
/// </summary>
public class PackLevel
{
    public int Id { get; set; }

    /// <summary>Produk jadi pemilik kemasan ini.</summary>
    public int FinishedItemId { get; set; }

    /// <summary>Nama kemasan (≤ 40, mis. "Botol", "Pack", "Karton"); unik per produk.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Jumlah satuan dasar per kemasan (mis. Pack = 12, Karton = 24).</summary>
    public decimal QtyPerBase { get; set; } = 1m;

    /// <summary>Barcode kemasan (opsional, ≤ 40).</summary>
    public string? Barcode { get; set; }

    /// <summary>Urutan tampil (kecil lebih dulu).</summary>
    public int Sequence { get; set; }

    /// <summary>Menandai kemasan default untuk penjualan.</summary>
    public bool IsDefaultSales { get; set; }

    // Navigation
    public Item? FinishedItem { get; set; }
}
