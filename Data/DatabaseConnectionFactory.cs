using Npgsql;

namespace GeniusVendas.Api.Data;

public sealed class DatabaseConnectionFactory
{
    private readonly IConfiguration _configuration;
    public DatabaseConnectionFactory(IConfiguration configuration) => _configuration = configuration;

    public NpgsqlConnection Create()
    {
        var value = Environment.GetEnvironmentVariable("DATABASE_URL")
                    ?? _configuration["Database:ConnectionString"];

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("PostgreSQL não configurado. Defina DATABASE_URL no Render ou Database:ConnectionString localmente.");

        return new NpgsqlConnection(Normalize(value));
    }

    private static string Normalize(string value)
    {
        value = value.Trim();
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return value;

        var uri = new Uri(value);
        var parts = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(parts[0]),
            Password = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "",
            Database = uri.AbsolutePath.Trim('/'),
            Pooling = true,
            Timeout = 15,
            CommandTimeout = 30
        };
        return builder.ConnectionString;
    }
}
