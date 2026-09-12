using GeniusVendas.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GeniusVendas.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromServices] IGdoorRepository repo,
        CancellationToken ct) =>
        Ok(await repo.GetProductsAsync(ct));
}
