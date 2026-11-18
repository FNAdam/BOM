using Bom.Domain.Enums;

namespace Bom.Application.Dtos;

/// <summary>Pesanan pembelian lengkap: header + baris item.</summary>
public record PurchaseOrderDto(
    int Id,
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    string OrderNo,
    PurchaseOrderStatus Status,
    DateOnly OrderDate,
    string? Notes,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

/// <summary>Satu baris pesanan pembelian.</summary>
public record PurchaseOrderLineDto(
    int Id,
    int ItemId,
    string ItemCode,
    string ItemName,
    int UomId,
    string UomCode,
    decimal QtyOrdered,
    decimal UnitPrice,
    decimal QtyReceived);

/// <summary>Ringkasan pesanan pembelian untuk tampilan daftar.</summary>
public record PurchaseOrderListDto(
    int Id,
    string OrderNo,
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    PurchaseOrderStatus Status,
    DateOnly OrderDate,
    int LineCount,
    decimal TotalValue);

/// <summary>Payload create/update pesanan pembelian (hanya boleh saat status Draft).</summary>
public record UpsertPurchaseOrderDto(
    int SupplierId,
    string OrderNo,
    DateOnly OrderDate,
    string? Notes,
    List<UpsertPurchaseOrderLineDto> Lines);

/// <summary>Satu baris payload create/update.</summary>
public record UpsertPurchaseOrderLineDto(
    int ItemId,
    int UomId,
    decimal QtyOrdered,
    decimal UnitPrice);

/// <summary>Payload catat penerimaan barang (bisa parsial, bisa beberapa baris sekaligus).</summary>
public record ReceivePurchaseOrderDto(List<ReceivePurchaseOrderLineDto> Lines);

/// <summary>Satu baris penerimaan: qty yang diterima SEKARANG (bukan akumulasi).</summary>
public record ReceivePurchaseOrderLineDto(int LineId, decimal QtyReceiving);
