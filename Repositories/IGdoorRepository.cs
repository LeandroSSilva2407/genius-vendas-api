using GeniusVendas.Api.Models;

namespace GeniusVendas.Api.Repositories;

public interface IGdoorRepository
{
    Task<(string SellerCode, string SellerName)?> AuthenticateAsync(
        string username, string password, CancellationToken ct);

    Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct);
    Task<ProductDto?> GetProductAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken ct);

    Task<string> CreateOrderAsync(
        CreateOrderRequest request,
        IReadOnlyList<OrderCalculatedItem> calculatedItems,
        CancellationToken ct);
}
