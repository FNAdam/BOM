namespace Bom.Application.Services;

/// <summary>
/// Kesalahan aturan bisnis BOM yang membawa status HTTP yang tepat,
/// sehingga controller dapat memetakannya ke 400 / 404 / 409 dengan pesan jelas.
/// </summary>
public sealed class BomServiceException : Exception
{
    public int StatusCode { get; }

    private BomServiceException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public static BomServiceException BadRequest(string message) => new(400, message);
    public static BomServiceException NotFound(string message) => new(404, message);
    public static BomServiceException Conflict(string message) => new(409, message);
}
