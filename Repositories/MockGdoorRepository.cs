using GeniusVendas.Api.Models;

namespace GeniusVendas.Api.Repositories;

public sealed class MockGdoorRepository : IGdoorRepository
{
    private readonly List<ProductDto> products =
    [
        new("000001", "7890000000011", "ARROZ TIPO 1 5KG", 28.90m, 25.90m, 10m, 82m),
        new("000002", "7890000000028", "CAFE 250G", 9.80m, 8.90m, 10m, 45m),
        new("000003", "7890000000035", "ACUCAR 1KG", 5.50m, 4.95m, 12m, 120m),
        new("000004", "7890000000042", "FEIJAO 1KG", 8.75m, 8.10m, 10m, 63m)
    ];

    private readonly List<CustomerDto> customers =
    [
        new("000000", "CONSUMIDOR", ""),
        new("000059", "SUPERMERCADO ABC", "12345678000199"),
        new("000060", "MERCADINHO CENTRAL", "98765432000155")
    ];

    public Task<(string SellerCode, string SellerName)?> AuthenticateAsync(
        string username, string password, CancellationToken ct)
    {
        if (username.Equals("antonio", StringComparison.OrdinalIgnoreCase)
            && password == "1234")
            return Task.FromResult<(string, string)?>(("ANTONIO", "Antonio"));

        return Task.FromResult<(string, string)?>(null);
    }

    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ProductDto>>(products);

    public Task<ProductDto?> GetProductAsync(string code, CancellationToken ct) =>
        Task.FromResult(products.FirstOrDefault(x => x.Code == code));

    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CustomerDto>>(customers);

    public Task<string> CreateOrderAsync(
        CreateOrderRequest request,
        IReadOnlyList<OrderCalculatedItem> calculatedItems,
        CancellationToken ct)
    {
        return Task.FromResult(DateTime.Now.ToString("HHmmss"));
    }
}
