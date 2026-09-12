namespace GeniusVendas.Api.Models;

public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResponse(string Token, string SellerCode, string SellerName);

public sealed record ProductDto(
    string Code,
    string Barcode,
    string Description,
    decimal RetailPrice,
    decimal WholesalePrice,
    decimal WholesaleMinQty,
    decimal Stock);

public sealed record CustomerDto(string Code, string Name, string Document);

public sealed record CreateOrderItemRequest(string ProductCode, decimal Quantity);

public sealed record CreateOrderRequest(
    string CustomerCode,
    string SellerCode,
    IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record OrderCalculatedItem(
    string ProductCode,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    bool Wholesale,
    decimal Total);

public sealed record CreateOrderResponse(
    string GdoorOrderNumber,
    decimal Total,
    IReadOnlyList<OrderCalculatedItem> Items);
