using GeniusVendas.Api.Data; using GeniusVendas.Api.Models; using Npgsql;
namespace GeniusVendas.Api.Repositories;
public sealed class CustomerRepository
{
 private readonly DatabaseConnectionFactory _factory; public CustomerRepository(DatabaseConnectionFactory factory)=>_factory=factory;
 public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(long companyId,CancellationToken ct){var list=new List<CustomerDto>();await using var c=_factory.Create();await c.OpenAsync(ct);await using var cmd=new NpgsqlCommand(@"SELECT id,gdoor_code,name,document,active,updated_at_utc FROM customer WHERE company_id=@c AND active=true ORDER BY name",c);cmd.Parameters.AddWithValue("c",companyId);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))list.Add(new CustomerDto(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetBoolean(4),r.GetDateTime(5)));return list;}
 public async Task<long?> GetIdByCodeAsync(long companyId,string code,CancellationToken ct){await using var c=_factory.Create();await c.OpenAsync(ct);await using var cmd=new NpgsqlCommand("SELECT id FROM customer WHERE company_id=@c AND gdoor_code=@g AND active=true",c);cmd.Parameters.AddWithValue("c",companyId);cmd.Parameters.AddWithValue("g",code);var o=await cmd.ExecuteScalarAsync(ct);return o is null?null:Convert.ToInt64(o);}
 public async Task<IReadOnlyList<ClienteVendaDto>> ListarParaVendaAsync(
    int empresaId,
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
            name,
            document,
            updated_at_utc
        FROM customer
        WHERE company_id = @empresaId
          AND active = TRUE
          AND (
                @pesquisa IS NULL
                OR @pesquisa = ''
                OR gdoor_code ILIKE @filtro
                OR name ILIKE @filtro
                OR document ILIKE @filtro
              )
        ORDER BY name
        LIMIT @limite
        OFFSET @deslocamento;
        """;

    await using var comando = new NpgsqlCommand(sql, conexao);

    comando.Parameters.AddWithValue("@empresaId", empresaId);
    comando.Parameters.AddWithValue("@pesquisa", (object?)pesquisa ?? DBNull.Value);
    comando.Parameters.AddWithValue("@filtro", $"%{pesquisa ?? string.Empty}%");
    comando.Parameters.AddWithValue("@limite", quantidadePorPagina);
    comando.Parameters.AddWithValue("@deslocamento", deslocamento);

    var clientes = new List<ClienteVendaDto>();

    await using var leitor =
        await comando.ExecuteReaderAsync(cancellationToken);

    while (await leitor.ReadAsync(cancellationToken))
    {
        clientes.Add(new ClienteVendaDto
        {
            Id = leitor.GetInt32(leitor.GetOrdinal("id")),

            Codigo =
                leitor.GetString(leitor.GetOrdinal("gdoor_code")),

            Nome =
                leitor.GetString(leitor.GetOrdinal("name")),

            Documento =
                leitor.IsDBNull(leitor.GetOrdinal("document"))
                    ? null
                    : leitor.GetString(leitor.GetOrdinal("document")),

            AtualizadoEmUtc =
                leitor.IsDBNull(leitor.GetOrdinal("updated_at_utc"))
                    ? null
                    : leitor.GetDateTime(
                        leitor.GetOrdinal("updated_at_utc"))
        });
    }

    return clientes;
}
public async Task<ClienteVendaDto?> ObterParaVendaPorCodigoAsync(
    int empresaId,
    string codigo,
    CancellationToken cancellationToken)
{
    await using var conexao = _factory.Create();
    await conexao.OpenAsync(cancellationToken);

    const string sql = """
        SELECT
            id,
            gdoor_code,
            name,
            document,
            updated_at_utc
        FROM customer
        WHERE company_id = @empresaId
          AND gdoor_code = @codigo
          AND active = TRUE
        LIMIT 1;
        """;

    await using var comando = new NpgsqlCommand(sql, conexao);

    comando.Parameters.AddWithValue("@empresaId", empresaId);
    comando.Parameters.AddWithValue("@codigo", codigo);

    await using var leitor =
        await comando.ExecuteReaderAsync(cancellationToken);

    if (!await leitor.ReadAsync(cancellationToken))
        return null;

    return new ClienteVendaDto
    {
        Id = leitor.GetInt32(leitor.GetOrdinal("id")),
        Codigo = leitor.GetString(leitor.GetOrdinal("gdoor_code")),
        Nome = leitor.GetString(leitor.GetOrdinal("name")),

        Documento =
            leitor.IsDBNull(leitor.GetOrdinal("document"))
                ? null
                : leitor.GetString(leitor.GetOrdinal("document")),

        AtualizadoEmUtc =
            leitor.IsDBNull(leitor.GetOrdinal("updated_at_utc"))
                ? null
                : leitor.GetDateTime(
                    leitor.GetOrdinal("updated_at_utc"))
    };
}
}
