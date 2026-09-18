namespace GeniusVendas.Api.Models;

public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, long UserId, long CompanyId, string SellerCode, string SellerName);

public sealed record ProductDto(
    long Id,
    string GdoorCode,
    string Barcode,
    string Description,
    decimal RetailPrice,
    decimal WholesalePrice,
    decimal WholesaleMinQty,
    decimal Stock,
    bool Active,
    DateTime UpdatedAtUtc);

public sealed record CustomerDto(
    long Id,
    string GdoorCode,
    string Name,
    string Document,
    bool Active,
    DateTime UpdatedAtUtc);

public sealed record OrderItemRequest(string ProductCode, decimal Quantity);

public sealed record CreateOrderRequest(
    string CustomerCode,
    IReadOnlyList<OrderItemRequest> Items,
    IReadOnlyList<OrderPaymentRequest> Payments);

public sealed record OrderPaymentRequest(
    string Species,
    decimal Value);

public sealed record OrderItemResponse(
    string ProductCode,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    bool Wholesale,
    decimal Total);

public sealed record CreateOrderResponse(
    long OrderId,
    string ExternalId,
    string Status,
    decimal Total,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderStatusDto(
    long OrderId,
    string ExternalId,
    string Status,
    string? GdoorOrderNumber,
    string? ErrorMessage,
    decimal Total,
    DateTime OrderDateUtc,
    DateTime? SentToGdoorAtUtc);

public sealed record SyncProductRequest(
    string GdoorCode,
    string Barcode,
    string Description,
    decimal RetailPrice,
    decimal WholesalePrice,
    decimal WholesaleMinQty,
    decimal Stock,
    bool Active,
    DateTime? UpdatedAtUtc);

public sealed record SyncCustomerRequest(
    string GdoorCode,
    string Name,
    string Document,
    bool Active,
    DateTime? UpdatedAtUtc);

public sealed record SyncSellerRequest(
    string GdoorCode,
    string Name,
    bool Active);

public sealed record SyncBatchRequest<T>(
    IReadOnlyList<T> Items);

public sealed record SyncResult(
    int Received,
    int Upserted);

public sealed record PendingOrderDto(
    long OrderId,
    string ExternalId,
    string CustomerCode,
    string SellerCode,
    DateTime OrderDateUtc,
    decimal Total,
    IReadOnlyList<OrderItemResponse> Items,
    IReadOnlyList<OrderPaymentResponse> Payments);

public sealed record CompleteOrderRequest(
    string GdoorOrderNumber);

public sealed record FailOrderRequest(
    string ErrorMessage);

public sealed record OrderPaymentResponse(
    string Species,
    decimal Value);
