using GeniusVendas.Api.Data;
using GeniusVendas.Api.Models;
using Npgsql;

namespace GeniusVendas.Api.Repositories;

public sealed class SyncRepository
{
    private readonly DatabaseConnectionFactory _factory;

    public SyncRepository(DatabaseConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<long?> ResolveCompanyAsync(
        string key,
        CancellationToken ct)
    {
        await using var c = _factory.Create();
        await c.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            "SELECT id FROM company WHERE integration_key=@k AND active=true",
            c);

        cmd.Parameters.AddWithValue("k", key);

        var o = await cmd.ExecuteScalarAsync(ct);

        return o is null
            ? null
            : Convert.ToInt64(o);
    }

    public async Task<int> UpsertProductsAsync(
        long companyId,
        IReadOnlyList<SyncProductRequest> items,
        CancellationToken ct)
    {
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);

        var n = 0;

        foreach (var x in items)
        {
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO product(
                    company_id,
                    gdoor_code,
                    barcode,
                    description,
                    retail_price,
                    wholesale_price,
                    wholesale_min_qty,
                    stock,
                    active,
                    updated_at_utc
                )
                VALUES(
                    @c,@g,@b,@d,@r,@w,@m,@s,@a,@u
                )
                ON CONFLICT(company_id,gdoor_code)
                DO UPDATE SET
                    barcode=excluded.barcode,
                    description=excluded.description,
                    retail_price=excluded.retail_price,
                    wholesale_price=excluded.wholesale_price,
                    wholesale_min_qty=excluded.wholesale_min_qty,
                    stock=excluded.stock,
                    active=excluded.active,
                    updated_at_utc=excluded.updated_at_utc
                """,
                c,
                tx);

            cmd.Parameters.AddWithValue("c", companyId);
            cmd.Parameters.AddWithValue("g", x.GdoorCode);
            cmd.Parameters.AddWithValue("b", x.Barcode ?? "");
            cmd.Parameters.AddWithValue("d", x.Description);
            cmd.Parameters.AddWithValue("r", x.RetailPrice);
            cmd.Parameters.AddWithValue("w", x.WholesalePrice);
            cmd.Parameters.AddWithValue("m", x.WholesaleMinQty);
            cmd.Parameters.AddWithValue("s", x.Stock);
            cmd.Parameters.AddWithValue("a", x.Active);
            cmd.Parameters.AddWithValue("u", x.UpdatedAtUtc ?? DateTime.UtcNow);

            n += await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
        return n;
    }

    public async Task<int> UpsertCustomersAsync(
        long companyId,
        IReadOnlyList<SyncCustomerRequest> items,
        CancellationToken ct)
    {
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);

        var n = 0;

        foreach (var x in items)
        {
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO customer(
                    company_id,
                    gdoor_code,
                    name,
                    document,
                    active,
                    updated_at_utc
                )
                VALUES(
                    @c,@g,@n,@d,@a,@u
                )
                ON CONFLICT(company_id,gdoor_code)
                DO UPDATE SET
                    name=excluded.name,
                    document=excluded.document,
                    active=excluded.active,
                    updated_at_utc=excluded.updated_at_utc
                """,
                c,
                tx);

            cmd.Parameters.AddWithValue("c", companyId);
            cmd.Parameters.AddWithValue("g", x.GdoorCode);
            cmd.Parameters.AddWithValue("n", x.Name);
            cmd.Parameters.AddWithValue("d", x.Document ?? "");
            cmd.Parameters.AddWithValue("a", x.Active);
            cmd.Parameters.AddWithValue("u", x.UpdatedAtUtc ?? DateTime.UtcNow);

            n += await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
        return n;
    }

    public async Task<int> UpsertSellersAsync(
        long companyId,
        IReadOnlyList<SyncSellerRequest> items,
        CancellationToken ct)
    {
        await using var c = _factory.Create();
        await c.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);

        var n = 0;

        foreach (var x in items)
        {
            if (string.IsNullOrWhiteSpace(x.GdoorCode))
                continue;

            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO seller(
                    company_id,
                    gdoor_code,
                    name,
                    active
                )
                VALUES(
                    @c,@g,@n,@a
                )
                ON CONFLICT(company_id,gdoor_code)
                DO UPDATE SET
                    name=excluded.name,
                    active=excluded.active
                """,
                c,
                tx);

            cmd.Parameters.AddWithValue("c", companyId);
            cmd.Parameters.AddWithValue("g", x.GdoorCode.Trim());
            cmd.Parameters.AddWithValue("n", x.Name?.Trim() ?? "");
            cmd.Parameters.AddWithValue("a", x.Active);

            n += await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
        return n;
    }
}
