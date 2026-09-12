using GeniusVendas.Api.Models; using GeniusVendas.Api.Repositories; using Microsoft.AspNetCore.Mvc;
namespace GeniusVendas.Api.Controllers;
[ApiController][Route("api/customers")]
public sealed class CustomersController:ControllerBase
{
 [HttpGet] public async Task<IActionResult> Get([FromServices]CustomerRepository repo,CancellationToken ct){var s=(SessionInfo)HttpContext.Items["Session"]!;return Ok(await repo.GetAllAsync(s.CompanyId,ct));}
}
