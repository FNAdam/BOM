// Helper konversi persen (string, mis. "45") <-> desimal (0.45) dipakai backend.
// UI selalu menampilkan/menerima persen - lebih mudah dipahami daripada angka 0-1 mentah.
//
// PENTING: string ini dipasang ke <input type="number">, yang menurut spesifikasi HTML HARUS
// pakai titik sebagai pemisah desimal (bukan koma ala format Indonesia) - makanya pakai
// Number.prototype.toString() biasa di sini, BUKAN toLocaleString('id-ID').

export function decToPctStr(value: number): string {
  // Bulatkan dulu supaya tidak muncul noise floating-point (mis. 0.98*100 = 98.00000000000001).
  const rounded = Math.round(value * 100 * 10000) / 10000;
  return rounded.toString();
}

export function pctStrToDec(value: string): number {
  const n = Number(value);
  return Number.isFinite(n) ? n / 100 : 0;
}
