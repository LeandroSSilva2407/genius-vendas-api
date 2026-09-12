using GeniusVendas.Api.Models;
using GeniusVendas.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GeniusVendas.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateOrderRequest request,
        [FromServices] IGdoorRepository repo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerCode))
            return BadRequest(new { message = "Cliente obrigatório." });

        if (request.Items is null || request.Items.Count == 0)
            return BadRequest(new { message = "Pedido sem itens." });

        var calculated = new List<OrderCalculatedItem>();

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                return BadRequest(new { message = $"Quantidade inválida: {item.ProductCode}" });

            var product = await repo.GetProductAsync(item.ProductCode, ct);
            if (product is null)
                return BadRequest(new { message = $"Produto não encontrado: {item.ProductCode}" });

            if (product.Stock < item.Quantity)
                return BadRequest(new
                {
                    message = $"Estoque insuficiente para {product.Description}. " +
                              $"Solicitado {item.Quantity}; disponível {product.Stock}."
                });

            var wholesale =
                product.WholesalePrice > 0 &&
                product.WholesaleMinQty > 0 &&
                item.Quantity >= product.WholesaleMinQty;

            var unitPrice = wholesale
                ? product.WholesalePrice
                : product.RetailPrice;

            calculated.Add(new OrderCalculatedItem(
                product.Code,
                product.Description,
                item.Quantity,
                unitPrice,
                wholesale,
                unitPrice * item.Quantity));
        }

        var orderNumber = await repo.CreateOrderAsync(request, calculated, ct);
        return Ok(new CreateOrderResponse(
            orderNumber,
            calculated.Sum(x => x.Total),
            calculated));
    }
}
