namespace Bom.Domain.Enums;

/// <summary>
/// Status siklus hidup sebuah perintah produksi (production order).
/// Open = masih bisa mencatat konsumsi/diedit; Closed = sudah ditutup &amp; COGS terhitung
/// (ledger terposting); Cancelled = dibatalkan tanpa posting.
/// </summary>
public enum ProductionStatus
{
    Open,
    Closed,
    Cancelled
}
