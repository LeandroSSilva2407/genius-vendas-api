using GeniusVendas.Api.Data; using GeniusVendas.Api.Models; using Npgsql;
namespace GeniusVendas.Api.Repositories;
public sealed class ProductRepository
{
 private readonly DatabaseConnectionFactory _factory; public ProductRepository(DatabaseConnectionFactory factory)=>_factory=factory;
 public async Task<IReadOnlyList<ProductDto>> GetAllAsync(long companyId,CancellationToken ct){var list=new List<ProductDto>(); await using var c=_factory.Create(); await c.OpenAsync(ct); await using var cmd=new NpgsqlCommand(@"SELECT id,gdoor_code,barcode,description,retail_price,wholesale_price,wholesale_min_qty,stock,active,updated_at_utc FROM product WHERE company_id=@c AND active=true ORDER BY description",c);cmd.Parameters.AddWithValue("c",companyId);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))list.Add(Map(r));return list;}
 public async Task<ProductDto?> GetByCodeAsync(long companyId,string code,CancellationToken ct){await using var c=_factory.Create();await c.OpenAsync(ct);await using var cmd=new NpgsqlCommand(@"SELECT id,gdoor_code,barcode,description,retail_price,wholesale_price,wholesale_min_qty,stock,active,updated_at_utc FROM product WHERE company_id=@c AND gdoor_code=@g AND active=true",c);cmd.Parameters.AddWithValue("c",companyId);cmd.Parameters.AddWithValue("g",code);await using var r=await cmd.ExecuteReaderAsync(ct);return await r.ReadAsync(ct)?Map(r):null;}
 private static ProductDto Map(NpgsqlDataReader r)=>new(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetDecimal(4),r.GetDecimal(5),r.GetDecimal(6),r.GetDecimal(7),r.GetBoolean(8),r.GetDateTime(9));
 public async Task<IReadOnlyList<ProdutoVendaDto>> ListarParaVendaAsync(
    long empresaId,
    string? pesquisa,
    int pagina,
    int quantidadePorPagina,
    CancellationToken cancellationToken)
{
    pagina = pagina <= 0 ? 1 : pagina;

    quantidadePorPagina = quantidadePorPagina <= 0
        ? 50
        : Math.Min(quantidadePorPagina, 100);

    var deslocamento = (pagina - 1) * quantidadePorPagina;

    await using var conexao = _factory.Create();
    await conexao.OpenAsync(cancellationToken);

    const string sql = """
        SELECT
            id,
            gdoor_code,
            barcode,
            description,
            retail_price,
            wholesale_price,
            wholesale_min_qty,
            stock,
            updated_at_utc
        FROM product
        WHERE company_id = @empresaId
          AND active = TRUE
          AND (
                @pesquisa IS NULL
                OR @pesquisa = ''
                OR gdoor_code ILIKE @filtro
                OR barcode ILIKE @filtro
                OR description ILIKE @filtro
              )
        ORDER BY description
        LIMIT @limite
        OFFSET @deslocamento;
        """;

    await using var comando = new NpgsqlCommand(sql, conexao);

    comando.Parameters.AddWithValue("@empresaId", empresaId);
    comando.Parameters.AddWithValue("@pesquisa", (object?)pesquisa ?? DBNull.Value);
    comando.Parameters.AddWithValue("@filtro", $"%{pesquisa ?? string.Empty}%");
    comando.Parameters.AddWithValue("@limite", quantidadePorPagina);
    comando.Parameters.AddWithValue("@deslocamento", deslocamento);

    var produtos = new List<ProdutoVendaDto>();

    await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

    while (await leitor.ReadAsync(cancellationToken))
    {
        produtos.Add(new ProdutoVendaDto
        {
            Id = leitor.GetInt32(leitor.GetOrdinal("id")),

            Codigo = leitor.GetString(
                leitor.GetOrdinal("gdoor_code")),

            CodigoBarras = leitor.IsDBNull(
                leitor.GetOrdinal("barcode"))
                    ? null
                    : leitor.GetString(leitor.GetOrdinal("barcode")),

            Descricao = leitor.GetString(
                leitor.GetOrdinal("description")),

            PrecoVarejo = leitor.GetDecimal(
                leitor.GetOrdinal("retail_price")),

            PrecoAtacado = leitor.IsDBNull(
                leitor.GetOrdinal("wholesale_price"))
                    ? 0
                    : leitor.GetDecimal(
                        leitor.GetOrdinal("wholesale_price")),

            QuantidadeMinimaAtacado = leitor.IsDBNull(
                leitor.GetOrdinal("wholesale_min_qty"))
                    ? 0
                    : leitor.GetDecimal(
                        leitor.GetOrdinal("wholesale_min_qty")),

            Estoque = leitor.IsDBNull(
                leitor.GetOrdinal("stock"))
                    ? 0
                    : leitor.GetDecimal(
                        leitor.GetOrdinal("stock")),

            AtualizadoEmUtc = leitor.IsDBNull(
                leitor.GetOrdinal("updated_at_utc"))
                    ? null
                    : leitor.GetDateTime(
                        leitor.GetOrdinal("updated_at_utc"))
        });
    }

    return produtos;
}

 public async Task<ProdutoVendaDto?> ObterParaVendaPorCodigoAsync(
    long empresaId,
    string codigo,
    CancellationToken cancellationToken)
{
    await using var conexao = _factory.Create();
    await conexao.OpenAsync(cancellationToken);

    const string sql = """
        SELECT
            id,
            gdoor_code,
            barcode,
            description,
            retail_price,
            wholesale_price,
            wholesale_min_qty,
            stock,
            updated_at_utc
        FROM product
        WHERE company_id = @empresaId
          AND gdoor_code = @codigo
          AND active = TRUE
        LIMIT 1;
        """;

    await using var comando = new NpgsqlCommand(sql, conexao);

    comando.Parameters.AddWithValue("@empresaId", empresaId);
    comando.Parameters.AddWithValue("@codigo", codigo);

    await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

    if (!await leitor.ReadAsync(cancellationToken))
        return null;

    return new ProdutoVendaDto
    {
        Id = leitor.GetInt32(leitor.GetOrdinal("id")),
        Codigo = leitor.GetString(leitor.GetOrdinal("gdoor_code")),

        CodigoBarras = leitor.IsDBNull(leitor.GetOrdinal("barcode"))
            ? null
            : leitor.GetString(leitor.GetOrdinal("barcode")),

        Descricao = leitor.GetString(leitor.GetOrdinal("description")),

        PrecoVarejo =
            leitor.GetDecimal(leitor.GetOrdinal("retail_price")),

        PrecoAtacado =
            leitor.IsDBNull(leitor.GetOrdinal("wholesale_price"))
                ? 0
                : leitor.GetDecimal(
                    leitor.GetOrdinal("wholesale_price")),

        QuantidadeMinimaAtacado =
            leitor.IsDBNull(leitor.GetOrdinal("wholesale_min_qty"))
                ? 0
                : leitor.GetDecimal(
                    leitor.GetOrdinal("wholesale_min_qty")),

        Estoque =
            leitor.IsDBNull(leitor.GetOrdinal("stock"))
                ? 0
                : leitor.GetDecimal(leitor.GetOrdinal("stock")),

        AtualizadoEmUtc =
            leitor.IsDBNull(leitor.GetOrdinal("updated_at_utc"))
                ? null
                : leitor.GetDateTime(
                    leitor.GetOrdinal("updated_at_utc"))
    };
}
}
