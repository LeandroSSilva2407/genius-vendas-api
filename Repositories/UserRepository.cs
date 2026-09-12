using GeniusVendas.Api.Data;
using GeniusVendas.Api.Models;
using GeniusVendas.Api.Services;
using Npgsql;

namespace GeniusVendas.Api.Repositories;

public sealed class UserRepository
{
    private readonly DatabaseConnectionFactory _factory; private readonly PasswordHasher _hasher;
    public UserRepository(DatabaseConnectionFactory factory, PasswordHasher hasher) { _factory=factory; _hasher=hasher; }

    public async Task<AuthenticatedUser?> AuthenticateAsync(string username, string password, CancellationToken ct)
    {
        await using var conn=_factory.Create(); await conn.OpenAsync(ct);
        await using var cmd=new NpgsqlCommand(@"SELECT u.id,u.company_id,u.seller_id,u.username,u.password_hash,se.gdoor_code,se.name FROM app_user u JOIN seller se ON se.id=u.seller_id WHERE lower(u.username)=lower(@u) AND u.active=true AND se.active=true",conn);
        cmd.Parameters.AddWithValue("u",username.Trim()); await using var r=await cmd.ExecuteReaderAsync(ct);
        if(!await r.ReadAsync(ct) || !_hasher.Verify(password,r.GetString(4))) return null;
        return new AuthenticatedUser(r.GetInt64(0),r.GetInt64(1),r.GetInt64(2),r.GetString(5),r.GetString(6),r.GetString(3));
    }
}
