using GeniusVendas.Api.Models;
using GeniusVendas.Api.Repositories;
using GeniusVendas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeniusVendas.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        [FromServices] IGdoorRepository repo,
        [FromServices] SessionTokenService tokens,
        CancellationToken ct)
    {
        var seller = await repo.AuthenticateAsync(
            request.Username, request.Password, ct);

        if (seller is null)
            return Unauthorized(new { message = "Usuário ou senha inválidos." });

        return Ok(new LoginResponse(
            tokens.Create(),
            seller.Value.SellerCode,
            seller.Value.SellerName));
    }
}
