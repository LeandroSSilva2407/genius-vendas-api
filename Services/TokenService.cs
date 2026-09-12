using System.Security.Cryptography;
using GeniusVendas.Api.Data;
using GeniusVendas.Api.Models;
using Npgsql;

namespace GeniusVendas.Api.Services;

public sealed class TokenService
{
    private readonly DatabaseConnectionFactory _factory;
    private readonly IConfiguration _configuration;
    public TokenService(DatabaseConnectionFactory factory, IConfiguration configuration)
    {
        _factory = factory; _configuration = configuration;
    }

    public async Task<SessionInfo> CreateAsync(AuthenticatedUser user, CancellationToken ct)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expires = DateTime.UtcNow.AddHours(_configuration.GetValue("Api:TokenHours", 12));
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"INSERT INTO app_session(token,user_id,expires_at_utc) VALUES(@t,@u,@e)", conn);
        cmd.Parameters.AddWithValue("t", token);
        cmd.Parameters.AddWithValue("u", user.UserId);
        cmd.Parameters.AddWithValue("e", expires);
        await cmd.ExecuteNonQueryAsync(ct);
        return new SessionInfo(token, user.UserId, user.CompanyId, user.SellerId, user.SellerCode, user.SellerName, expires);
    }

    public async Task<SessionInfo?> ValidateAsync(string token, CancellationToken ct)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
SELECT s.token,s.expires_at_utc,u.id,u.company_id,u.seller_id,se.gdoor_code,se.name
FROM app_session s
JOIN app_user u ON u.id=s.user_id AND u.active=true
JOIN seller se ON se.id=u.seller_id AND se.active=true
WHERE s.token=@t AND s.expires_at_utc > (now() at time zone 'utc')", conn);
        cmd.Parameters.AddWithValue("t", token);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return new SessionInfo(r.GetString(0), r.GetInt64(2), r.GetInt64(3), r.GetInt64(4), r.GetString(5), r.GetString(6), r.GetDateTime(1));
    }
}
