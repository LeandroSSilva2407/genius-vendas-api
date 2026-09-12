using GeniusVendas.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GeniusVendas.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromServices] IGdoorRepository repo,
        CancellationToken ct) =>
        Ok(await repo.GetCustomersAsync(ct));
}
