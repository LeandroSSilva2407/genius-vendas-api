using GeniusVendas.Api.Models;

namespace GeniusVendas.Api.Services;

public sealed class BearerTokenMiddleware
{
    private readonly RequestDelegate _next;
    public BearerTokenMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, TokenService tokens)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health") || path.StartsWithSegments("/api/auth") || path.StartsWithSegments("/api/sync"))
        {
            await _next(context); return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401; return;
        }

        var session = await tokens.ValidateAsync(header[7..].Trim(), context.RequestAborted);
        if (session is null) { context.Response.StatusCode = 401; return; }

        context.Items["Session"] = session;
        await _next(context);
    }
}
