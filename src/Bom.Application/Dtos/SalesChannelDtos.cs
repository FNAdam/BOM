using System.ComponentModel.DataAnnotations;

namespace Bom.Application.Dtos;

/// <summary>Kanal penjualan (jenjang harga di atas harga dasar).</summary>
public record SalesChannelDto(
    int Id,
    string Code,
    string Name,
    decimal DefaultMarginPercent,
    int Sequence,
    bool IsActive);

/// <summary>Payload create/update kanal penjualan.</summary>
public record UpsertSalesChannelDto(
    [Required, StringLength(20)] string Code,
    [Required, StringLength(60)] string Name,
    [Range(0, 100, ErrorMessage = "DefaultMarginPercent tidak boleh negatif.")] decimal DefaultMarginPercent,
    int Sequence,
    bool IsActive);
