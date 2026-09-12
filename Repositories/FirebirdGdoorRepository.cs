using System.Data;
using FirebirdSql.Data.FirebirdClient;
using GeniusVendas.Api.Models;
using GeniusVendas.Api.Options;
using Microsoft.Extensions.Options;

namespace GeniusVendas.Api.Repositories;

public sealed class FirebirdGdoorRepository : IGdoorRepository
{
    private readonly GdoorOptions options;

    public FirebirdGdoorRepository(IOptions<GdoorOptions> options)
    {
        this.options = options.Value;
    }

    private FbConnection CreateConnection() => new(options.ConnectionString);

    public async Task<(string SellerCode, string SellerName)?> AuthenticateAsync(
        string username, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Sql.Login))
            throw Missing("Gdoor:Sql:Login");

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new FbCommand(options.Sql.Login, conn);
        cmd.Parameters.Add("@USERNAME", FbDbType.VarChar).Value = username;
        cmd.Parameters.Add("@PASSWORD", FbDbType.VarChar).Value = password;

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return (
            r["SELLER_CODE"]?.ToString() ?? "",
            r["SELLER_NAME"]?.ToString() ?? ""
        );
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Sql.Products))
            throw Missing("Gdoor:Sql:Products");

        var list = new List<ProductDto>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new FbCommand(options.Sql.Products, conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);

        while (await r.ReadAsync(ct))
            list.Add(MapProduct(r));

        return list;
    }

    public async Task<ProductDto?> GetProductAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Sql.ProductByCode))
            throw Missing("Gdoor:Sql:ProductByCode");

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new FbCommand(options.Sql.ProductByCode, conn);
        cmd.Parameters.Add("@CODE", FbDbType.VarChar).Value = code;
        await using var r = await cmd.ExecuteReaderAsync(ct);

        return await r.ReadAsync(ct) ? MapProduct(r) : null;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Sql.Customers))
            throw Missing("Gdoor:Sql:Customers");

        var list = new List<CustomerDto>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new FbCommand(options.Sql.Customers, conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);

        while (await r.ReadAsync(ct))
        {
            list.Add(new CustomerDto(
                r["CODE"]?.ToString() ?? "",
                r["NAME"]?.ToString() ?? "",
                r["DOCUMENT"]?.ToString() ?? ""));
        }

        return list;
    }

    public async Task<string> CreateOrderAsync(
        CreateOrderRequest request,
        IReadOnlyList<OrderCalculatedItem> calculatedItems,
        CancellationToken ct)
    {
        await Task.CompletedTask;

        throw new NotImplementedException(
            "Conecte aqui a rotina já validada de criação do pedido GDOOR. " +
            "Veja README_GDOOR.md.");
    }

    private static ProductDto MapProduct(IDataRecord r) => new(
        r["CODE"]?.ToString() ?? "",
        r["BARCODE"]?.ToString() ?? "",
        r["DESCRIPTION"]?.ToString() ?? "",
        Convert.ToDecimal(r["RETAIL_PRICE"]),
        Convert.ToDecimal(r["WHOLESALE_PRICE"]),
        Convert.ToDecimal(r["WHOLESALE_MIN_QTY"]),
        Convert.ToDecimal(r["STOCK"]));

    private static InvalidOperationException Missing(string key) =>
        new($"SQL não configurado: {key}");
}
