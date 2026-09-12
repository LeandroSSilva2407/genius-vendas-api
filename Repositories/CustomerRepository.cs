using GeniusVendas.Api.Data; using GeniusVendas.Api.Models; using Npgsql;
namespace GeniusVendas.Api.Repositories;
public sealed class CustomerRepository
{
 private readonly DatabaseConnectionFactory _factory; public CustomerRepository(DatabaseConnectionFactory factory)=>_factory=factory;
 public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(long companyId,CancellationToken ct){var list=new List<CustomerDto>();await using var c=_factory.Create();await c.OpenAsync(ct);await using var cmd=new NpgsqlCommand(@"SELECT id,gdoor_code,name,document,active,updated_at_utc FROM customer WHERE company_id=@c AND active=true ORDER BY name",c);cmd.Parameters.AddWithValue("c",companyId);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))list.Add(new CustomerDto(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetBoolean(4),r.GetDateTime(5)));return list;}
 public async Task<long?> GetIdByCodeAsync(long companyId,string code,CancellationToken ct){await using var c=_factory.Create();await c.OpenAsync(ct);await using var cmd=new NpgsqlCommand("SELECT id FROM customer WHERE company_id=@c AND gdoor_code=@g AND active=true",c);cmd.Parameters.AddWithValue("c",companyId);cmd.Parameters.AddWithValue("g",code);var o=await cmd.ExecuteScalarAsync(ct);return o is null?null:Convert.ToInt64(o);}
}
