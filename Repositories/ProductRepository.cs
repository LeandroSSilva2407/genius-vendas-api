using GeniusVendas.Api.Data; using GeniusVendas.Api.Models; using Npgsql;
namespace GeniusVendas.Api.Repositories;
public sealed class ProductRepository
{
 private readonly DatabaseConnectionFactory _factory; public ProductRepository(DatabaseConnectionFactory factory)=>_factory=factory;
 public async Task<IReadOnlyList<ProductDto>> GetAllAsync(long companyId,CancellationToken ct){var list=new List<ProductDto>(); await using var c=_factory.Create(); await c.OpenAsync(ct); await using var cmd=new NpgsqlCommand(@"SELECT id,gdoor_code,barcode,description,retail_price,wholesale_price,wholesale_min_qty,stock,active,updated_at_utc FROM product WHERE company_id=@c AND active=true ORDER BY description",c);cmd.Parameters.AddWithValue("c",companyId);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))list.Add(Map(r));return list;}
 public async Task<ProductDto?> GetByCodeAsync(long companyId,string code,CancellationToken ct){await using var c=_factory.Create();await c.OpenAsync(ct);await using var cmd=new NpgsqlCommand(@"SELECT id,gdoor_code,barcode,description,retail_price,wholesale_price,wholesale_min_qty,stock,active,updated_at_utc FROM product WHERE company_id=@c AND gdoor_code=@g AND active=true",c);cmd.Parameters.AddWithValue("c",companyId);cmd.Parameters.AddWithValue("g",code);await using var r=await cmd.ExecuteReaderAsync(ct);return await r.ReadAsync(ct)?Map(r):null;}
 private static ProductDto Map(NpgsqlDataReader r)=>new(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetDecimal(4),r.GetDecimal(5),r.GetDecimal(6),r.GetDecimal(7),r.GetBoolean(8),r.GetDateTime(9));
}
