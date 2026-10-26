using System.ComponentModel.DataAnnotations;

namespace Bom.Application.Dtos;

/// <summary>Saldo persediaan sebuah item (QtyOnHand dalam base UoM item).</summary>
public record InventoryBalanceDto(
    int ItemId,
    string ItemCode,
    string ItemName,
    string UomCode,
    decimal QtyOnHand,
    decimal AverageCost);

/// <summary>Payload upsert saldo on-hand (untuk seed stok saat uji MRP).</summary>
public record UpsertInventoryBalanceDto(
    [Range(0, double.MaxValue, ErrorMessage = "QtyOnHand tidak boleh negatif.")] decimal QtyOnHand,
    decimal? AverageCost);
