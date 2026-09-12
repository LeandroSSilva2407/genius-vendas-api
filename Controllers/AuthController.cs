using GeniusVendas.Api.Models; using GeniusVendas.Api.Repositories; using GeniusVendas.Api.Services; using Microsoft.AspNetCore.Mvc;
namespace GeniusVendas.Api.Controllers;
[ApiController][Route("api/auth")]
public sealed class AuthController:ControllerBase
{
 [HttpPost("login")] public async Task<IActionResult> Login(LoginRequest req,[FromServices]UserRepository users,[FromServices]TokenService tokens,CancellationToken ct){var u=await users.AuthenticateAsync(req.Username,req.Password,ct);if(u is null)return Unauthorized(new{message="Usuário ou senha inválidos."});var s=await tokens.CreateAsync(u,ct);return Ok(new LoginResponse(s.Token,s.ExpiresAtUtc,s.UserId,s.CompanyId,s.SellerCode,s.SellerName));}
}
