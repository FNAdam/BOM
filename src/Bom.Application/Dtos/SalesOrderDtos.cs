using Bom.Domain.Enums;

namespace Bom.Application.Dtos;

/// <summary>Pesanan penjualan lengkap: header + baris produk.</summary>
public record SalesOrderDto(
    int Id,
    string OrderNo,
    int? SalesChannelId,
    string? SalesChannelName,
    string? CustomerName,
    SalesOrderStatus Status,
    DateOnly OrderDate,
    decimal TaxPercent,
    string? Notes,
    IReadOnlyList<SalesOrderLineDto> Lines);

/// <summary>Satu baris pesanan penjualan.</summary>
public record SalesOrderLineDto(
    int Id,
    int ProductItemId,
    string ProductCode,
    string ProductName,
    int? PackLevelId,
    string? PackLevelName,
    string BaseUomCode,
    decimal Qty,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>Ringkasan pesanan penjualan untuk tampilan daftar.</summary>
public record SalesOrderListDto(
    int Id,
    string OrderNo,
    string? CustomerName,
    int? SalesChannelId,
    string? SalesChannelName,
    SalesOrderStatus Status,
    DateOnly OrderDate,
    int LineCount,
    decimal GrandTotal);

/// <summary>Payload create/update pesanan penjualan (hanya boleh saat status Draft).</summary>
public record UpsertSalesOrderDto(
    string OrderNo,
    int? SalesChannelId,
    string? CustomerName,
    DateOnly OrderDate,
    decimal TaxPercent,
    string? Notes,
    List<UpsertSalesOrderLineDto> Lines);

/// <summary>Satu baris payload create/update.</summary>
public record UpsertSalesOrderLineDto(
    int ProductItemId,
    int? PackLevelId,
    decimal Qty,
    decimal UnitPrice);

/// <summary>Data siap cetak struk - hanya tersedia untuk pesanan berstatus Completed.</summary>
public record SalesReceiptDto(
    int Id,
    string OrderNo,
    string? CustomerName,
    string? SalesChannelName,
    DateOnly OrderDate,
    decimal Subtotal,
    decimal TaxPercent,
    decimal TaxAmount,
    decimal GrandTotal,
    IReadOnlyList<SalesOrderLineDto> Lines);
