using GeniusVendas.Api.Data; using GeniusVendas.Api.Models; using Npgsql;
namespace GeniusVendas.Api.Repositories;

public sealed class OrderRepository
{
 private readonly DatabaseConnectionFactory _factory; public OrderRepository(DatabaseConnectionFactory factory)=>_factory=factory;
 public async Task<CreateOrderResponse> CreateAsync(
    SessionInfo session,
    long customerId,
    IReadOnlyList<OrderItemResponse> items,
    IReadOnlyList<OrderPaymentRequest> payments,
    CancellationToken ct)
{
    if (items is null || items.Count == 0)
        throw new InvalidOperationException(
            "O pedido deve possuir pelo menos um item.");

    if (payments is null || payments.Count == 0)
        throw new InvalidOperationException(
            "O pedido deve possuir pelo menos uma forma de pagamento.");

    var total = items.Sum(x => x.Total);

    var paymentTotal = payments.Sum(x => x.Value);

    if (Math.Abs(total - paymentTotal) > 0.01m)
    {
        throw new InvalidOperationException(
            $"A soma dos pagamentos ({paymentTotal:N2}) " +
            $"difere do total do pedido ({total:N2}).");
    }

    foreach (var payment in payments)
    {
        if (string.IsNullOrWhiteSpace(payment.Species))
        {
            throw new InvalidOperationException(
                "Existe pagamento sem espécie informada.");
        }

        if (payment.Value <= 0)
        {
            throw new InvalidOperationException(
                $"Valor inválido para a forma de pagamento " +
                $"'{payment.Species}'.");
        }
    }

    await using var c = _factory.Create();
    await c.OpenAsync(ct);

    await using var tx =
        await c.BeginTransactionAsync(ct);

    try
    {
        var external = Guid.NewGuid();
        long orderId;

        // -------------------------------------------------
        // CABEÇALHO
        // -------------------------------------------------

        await using (var cmd = new NpgsqlCommand(
            """
            INSERT INTO sales_order
            (
                company_id,
                external_id,
                customer_id,
                seller_id,
                order_date_utc,
                total,
                status
            )
            VALUES
            (
                @c,
                @e,
                @cu,
                @s,
                @d,
                @t,
                'PENDING_GDOOR'
            )
            RETURNING id
            """,
            c,
            tx))
        {
            cmd.Parameters.AddWithValue(
                "c",
                session.CompanyId);

            cmd.Parameters.AddWithValue(
                "e",
                external);

            cmd.Parameters.AddWithValue(
                "cu",
                customerId);

            cmd.Parameters.AddWithValue(
                "s",
                session.SellerId);

            cmd.Parameters.AddWithValue(
                "d",
                DateTime.UtcNow);

            cmd.Parameters.AddWithValue(
                "t",
                total);

            orderId =
                Convert.ToInt64(
                    await cmd.ExecuteScalarAsync(ct));
        }

        // -------------------------------------------------
        // ITENS
        // -------------------------------------------------

        foreach (var x in items)
        {
            long productId;

            await using (var pcmd = new NpgsqlCommand(
                """
                SELECT id
                FROM product
                WHERE company_id = @c
                  AND gdoor_code = @g
                """,
                c,
                tx))
            {
                pcmd.Parameters.AddWithValue(
                    "c",
                    session.CompanyId);

                pcmd.Parameters.AddWithValue(
                    "g",
                    x.ProductCode);

                var result =
                    await pcmd.ExecuteScalarAsync(ct);

                if (result is null ||
                    result == DBNull.Value)
                {
                    throw new InvalidOperationException(
                        $"Produto '{x.ProductCode}' " +
                        $"não encontrado.");
                }

                productId =
                    Convert.ToInt64(result);
            }

            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO sales_order_item
                (
                    order_id,
                    product_id,
                    product_code,
                    description,
                    quantity,
                    unit_price,
                    wholesale,
                    total
                )
                VALUES
                (
                    @o,
                    @p,
                    @g,
                    @d,
                    @q,
                    @u,
                    @w,
                    @t
                )
                """,
                c,
                tx);

            cmd.Parameters.AddWithValue(
                "o",
                orderId);

            cmd.Parameters.AddWithValue(
                "p",
                productId);

            cmd.Parameters.AddWithValue(
                "g",
                x.ProductCode);

            cmd.Parameters.AddWithValue(
                "d",
                x.Description);

            cmd.Parameters.AddWithValue(
                "q",
                x.Quantity);

            cmd.Parameters.AddWithValue(
                "u",
                x.UnitPrice);

            cmd.Parameters.AddWithValue(
                "w",
                x.Wholesale);

            cmd.Parameters.AddWithValue(
                "t",
                x.Total);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        // -------------------------------------------------
        // PAGAMENTOS
        // -------------------------------------------------

        foreach (var payment in payments)
        {
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO sales_order_payment
                (
                    order_id,
                    species,
                    value
                )
                VALUES
                (
                    @o,
                    @s,
                    @v
                )
                """,
                c,
                tx);

            cmd.Parameters.AddWithValue(
                "o",
                orderId);

            cmd.Parameters.AddWithValue(
                "s",
                payment.Species.Trim());

            cmd.Parameters.AddWithValue(
                "v",
                payment.Value);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);

        return new CreateOrderResponse(
            orderId,
            external.ToString(),
            "PENDING_GDOOR",
            total,
            items);
    }
    catch
    {
        await tx.RollbackAsync(ct);
        throw;
    }
}
 public async Task<IReadOnlyList<PendingOrderDto>> GetPendingAsync(
    long companyId,
    CancellationToken ct)
{
    var dict =
        new Dictionary<
            long,
            (
                string ext,
                string customer,
                string seller,
                DateTime date,
                decimal total,
                List<OrderItemResponse> items,
                List<OrderPaymentResponse> payments
            )>();

    await using var c = _factory.Create();
    await c.OpenAsync(ct);

    const string ordersSql = """
        SELECT
            o.id,
            o.external_id::text,
            cu.gdoor_code,
            se.gdoor_code,
            o.order_date_utc,
            o.total,
            i.product_code,
            i.description,
            i.quantity,
            i.unit_price,
            i.wholesale,
            i.total
        FROM sales_order o

        JOIN customer cu
          ON cu.id = o.customer_id

        JOIN seller se
          ON se.id = o.seller_id

        JOIN sales_order_item i
          ON i.order_id = o.id

        WHERE o.company_id = @c
          AND o.status = 'PENDING_GDOOR'
          AND o.gdoor_order_number IS NULL

        ORDER BY
            o.id,
            i.id
        """;

    await using (var cmd =
        new NpgsqlCommand(ordersSql, c))
    {
        cmd.Parameters.AddWithValue(
            "c",
            companyId);

        await using var r =
            await cmd.ExecuteReaderAsync(ct);

        while (await r.ReadAsync(ct))
        {
            var id = r.GetInt64(0);

            if (!dict.ContainsKey(id))
            {
                dict[id] =
                (
                    r.GetString(1),
                    r.GetString(2),
                    r.GetString(3),
                    r.GetDateTime(4),
                    r.GetDecimal(5),
                    new List<OrderItemResponse>(),
                    new List<OrderPaymentResponse>()
                );
            }

            dict[id].items.Add(
                new OrderItemResponse(
                    r.GetString(6),
                    r.GetString(7),
                    r.GetDecimal(8),
                    r.GetDecimal(9),
                    r.GetBoolean(10),
                    r.GetDecimal(11)));
        }
    }

    const string paymentsSql = """
        SELECT
            p.order_id,
            p.species,
            p.value
        FROM sales_order_payment p

        JOIN sales_order o
          ON o.id = p.order_id

        WHERE o.company_id = @c
          AND o.status = 'PENDING_GDOOR'
          AND o.gdoor_order_number IS NULL

        ORDER BY
            p.order_id,
            p.id
        """;

    await using (var cmd =
        new NpgsqlCommand(paymentsSql, c))
    {
        cmd.Parameters.AddWithValue(
            "c",
            companyId);

        await using var r =
            await cmd.ExecuteReaderAsync(ct);

        while (await r.ReadAsync(ct))
        {
            var orderId =
                r.GetInt64(0);

            if (!dict.TryGetValue(
                    orderId,
                    out var order))
            {
                continue;
            }

            order.payments.Add(
                new OrderPaymentResponse(
                    r.GetString(1),
                    r.GetDecimal(2)));
        }
    }

    return dict
        .Select(x =>
            new PendingOrderDto(
                x.Key,
                x.Value.ext,
                x.Value.customer,
                x.Value.seller,
                x.Value.date,
                x.Value.total,
                x.Value.items,
                x.Value.payments))
        .ToList();
}
 public async Task CompleteAsync(long companyId,long orderId,string gdoorNumber,CancellationToken ct){await using var c=_factory.Create();await c.OpenAsync(ct);await using var cmd=new NpgsqlCommand("UPDATE sales_order SET status='TRANSMITTED_GDOOR',gdoor_order_number=@g,sent_to_gdoor_at_utc=(now() at time zone 'utc'),error_message=NULL WHERE id=@i AND company_id=@c",c);cmd.Parameters.AddWithValue("g",gdoorNumber);cmd.Parameters.AddWithValue("i",orderId);cmd.Parameters.AddWithValue("c",companyId);await cmd.ExecuteNonQueryAsync(ct);}
 public async Task FailAsync(long companyId,long orderId,string error,CancellationToken ct){await using var c=_factory.Create();await c.OpenAsync(ct);await using var cmd=new NpgsqlCommand("UPDATE sales_order SET status='ERROR_GDOOR',error_message=@e WHERE id=@i AND company_id=@c",c);cmd.Parameters.AddWithValue("e",error);cmd.Parameters.AddWithValue("i",orderId);cmd.Parameters.AddWithValue("c",companyId);await cmd.ExecuteNonQueryAsync(ct);}
 public async Task<IReadOnlyList<OrderStatusDto>> GetBySellerAsync(SessionInfo s,CancellationToken ct){var list=new List<OrderStatusDto>();await using var c=_factory.Create();await c.OpenAsync(ct);await using var cmd=new NpgsqlCommand(@"SELECT id,external_id::text,status,gdoor_order_number,error_message,total,order_date_utc,sent_to_gdoor_at_utc FROM sales_order WHERE company_id=@c AND seller_id=@s ORDER BY id DESC LIMIT 100",c);cmd.Parameters.AddWithValue("c",s.CompanyId);cmd.Parameters.AddWithValue("s",s.SellerId);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))list.Add(new OrderStatusDto(r.GetInt64(0),r.GetString(1),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),r.IsDBNull(4)?null:r.GetString(4),r.GetDecimal(5),r.GetDateTime(6),r.IsDBNull(7)?null:r.GetDateTime(7)));return list;}
 public async Task<bool> ReenviarAsync(
    int pedidoId,
    int empresaId,
    int vendedorId,
    CancellationToken cancellationToken)
{
    await using var conexao = _factory.Create();
    await conexao.OpenAsync(cancellationToken);

    const string sql = """
        UPDATE sales_order
        SET status = 'PENDING_GDOOR'
        WHERE id = @pedidoId
          AND company_id = @empresaId
          AND seller_id = @vendedorId
          AND status = 'ERROR_GDOOR';
        """;

    await using var comando = new NpgsqlCommand(sql, conexao);

    comando.Parameters.AddWithValue("@pedidoId", pedidoId);
    comando.Parameters.AddWithValue("@empresaId", empresaId);
    comando.Parameters.AddWithValue("@vendedorId", vendedorId);

    var registros =
        await comando.ExecuteNonQueryAsync(cancellationToken);

    return registros > 0;
}
}
