using GeniusVendas.Api.Models; using GeniusVendas.Api.Repositories; using Microsoft.AspNetCore.Mvc;
namespace GeniusVendas.Api.Controllers;
[ApiController][Route("api/products")]
public sealed class ProductsController:ControllerBase
{
 [HttpGet] public async Task<IActionResult> Get([FromServices]ProductRepository repo,CancellationToken ct){var s=(SessionInfo)HttpContext.Items["Session"]!;return Ok(await repo.GetAllAsync(s.CompanyId,ct));}
}
