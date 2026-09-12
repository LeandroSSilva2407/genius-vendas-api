using GeniusVendas.Api.Models; using GeniusVendas.Api.Repositories; using Microsoft.AspNetCore.Mvc;
namespace GeniusVendas.Api.Controllers;
[ApiController][Route("api/sync")]
public sealed class SyncController:ControllerBase
{
 private async Task<long?> Company(SyncRepository repo,CancellationToken ct){var key=Request.Headers["X-Integration-Key"].ToString();if(string.IsNullOrWhiteSpace(key))return null;return await repo.ResolveCompanyAsync(key,ct);}
 [HttpPost("products")] public async Task<IActionResult> Products(SyncBatchRequest<SyncProductRequest> req,[FromServices]SyncRepository repo,CancellationToken ct){var c=await Company(repo,ct);if(c is null)return Unauthorized();var n=await repo.UpsertProductsAsync(c.Value,req.Items,ct);return Ok(new SyncResult(req.Items.Count,n));}
 [HttpPost("customers")] public async Task<IActionResult> Customers(SyncBatchRequest<SyncCustomerRequest> req,[FromServices]SyncRepository repo,CancellationToken ct){var c=await Company(repo,ct);if(c is null)return Unauthorized();var n=await repo.UpsertCustomersAsync(c.Value,req.Items,ct);return Ok(new SyncResult(req.Items.Count,n));}
 [HttpGet("orders/pending")] public async Task<IActionResult> Pending([FromServices]SyncRepository sync,[FromServices]OrderRepository orders,CancellationToken ct){var c=await Company(sync,ct);if(c is null)return Unauthorized();return Ok(await orders.GetPendingAsync(c.Value,ct));}
 [HttpPost("orders/{id:long}/complete")] public async Task<IActionResult> Complete(long id,CompleteOrderRequest req,[FromServices]SyncRepository sync,[FromServices]OrderRepository orders,CancellationToken ct){var c=await Company(sync,ct);if(c is null)return Unauthorized();await orders.CompleteAsync(c.Value,id,req.GdoorOrderNumber,ct);return Ok();}
 [HttpPost("orders/{id:long}/fail")] public async Task<IActionResult> Fail(long id,FailOrderRequest req,[FromServices]SyncRepository sync,[FromServices]OrderRepository orders,CancellationToken ct){var c=await Company(sync,ct);if(c is null)return Unauthorized();await orders.FailAsync(c.Value,id,req.ErrorMessage,ct);return Ok();}
}
