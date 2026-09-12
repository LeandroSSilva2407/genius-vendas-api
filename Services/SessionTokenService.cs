using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace GeniusVendas.Api.Services;

public sealed class SessionTokenService
{
    private readonly ConcurrentDictionary<string, DateTime> tokens = new();

    public string Create()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        tokens[token] = DateTime.UtcNow.AddHours(12);
        return token;
    }

    public bool IsValid(string token)
    {
        if (!tokens.TryGetValue(token, out var expiration)) return false;
        if (expiration > DateTime.UtcNow) return true;
        tokens.TryRemove(token, out _);
        return false;
    }
}
