// Label tampilan untuk nilai enum (Bahasa Indonesia, lebih familiar buat pemilik usaha).
// PENTING: ini cuma untuk TAMPILAN. Value yang dikirim ke API / dipakai untuk perbandingan kode
// (mis. status === 'Draft') TETAP pakai value asli (Inggris) - jangan pernah kirim hasil label()
// balik ke server.

function makeLabeler<T extends string>(map: Record<T, string>) {
  return (value: T): string => map[value] ?? value;
}

export const itemTypeLabel = makeLabeler({
  RawMaterial: 'Bahan Baku',
  Packaging: 'Kemasan',
  SemiFinished: 'Setengah Jadi',
  FinishedGood: 'Produk Jadi',
});

export const uomTypeLabel = makeLabeler({
  Weight: 'Berat',
  Volume: 'Volume',
  Count: 'Satuan Hitung',
});

export const costingMethodLabel = makeLabeler({
  Standard: 'Biaya Standar',
  MovingAverage: 'Rata-rata Bergerak',
});

export const bomStatusLabel = makeLabeler({
  Draft: 'Draf',
  Active: 'Aktif',
  Obsolete: 'Tidak Dipakai',
});

export const productionStatusLabel = makeLabeler({
  Open: 'Berjalan',
  Closed: 'Selesai',
  Cancelled: 'Dibatalkan',
});

export const stockMovementTypeLabel = makeLabeler({
  Receipt: 'Pembelian Masuk',
  Issue: 'Keluar Manual',
  ProductionIssue: 'Dipakai Produksi',
  ProductionReceipt: 'Hasil Produksi',
  Adjustment: 'Penyesuaian',
});

export const pricingMethodLabel = makeLabeler({
  Margin: 'Margin (dari harga jual)',
  Markup: 'Markup (dari biaya)',
  Manual: 'Harga Manual',
});

export const costBasisLabel = makeLabeler({
  Standard: 'Biaya Standar',
  LastCOGS: 'COGS Terakhir',
});

export const roundingRuleLabel = makeLabeler({
  None: 'Tanpa Pembulatan',
  Nearest100: 'Ke Rp 100 Terdekat',
  Nearest500: 'Ke Rp 500 Terdekat',
  Psych990: 'Harga Ganjil (...990)',
});

export const priceListStatusLabel = bomStatusLabel;
