using GeniusVendas.Api.Models; using GeniusVendas.Api.Repositories; using Microsoft.AspNetCore.Mvc;
namespace GeniusVendas.Api.Controllers;
[ApiController][Route("api/orders")]
public sealed class OrdersController:ControllerBase
{
 [HttpPost] public async Task<IActionResult> Create(CreateOrderRequest req,[FromServices]CustomerRepository customers,[FromServices]ProductRepository products,[FromServices]OrderRepository orders,CancellationToken ct)
 {
 var s=(SessionInfo)HttpContext.Items["Session"]!;
 var customerId=await customers.GetIdByCodeAsync(s.CompanyId,req.CustomerCode,ct);
 if(customerId is null)return BadRequest(new{message="Cliente não encontrado."});
 if(req.Items is null||req.Items.Count==0)return BadRequest(new{message="Pedido sem itens."});
 var calc=new List<OrderItemResponse>();foreach(var item in req.Items)
 {if(item.Quantity<=0)
 return BadRequest(new{message=$"Quantidade inválida: {item.ProductCode}"});
 var p=await products.GetByCodeAsync(s.CompanyId,item.ProductCode,ct);
 if(p is null)return BadRequest(new{message=$"Produto não encontrado: {item.ProductCode}"});
 if(p.Stock<item.Quantity)
 return BadRequest(new{message=$"Estoque insuficiente para {p.Description}. Disponível: {p.Stock}."});
 var wholesale=p.WholesalePrice>0&&p.WholesaleMinQty>0&&item.Quantity>=p.WholesaleMinQty;
 var unit=wholesale?p.WholesalePrice:p.RetailPrice;calc.Add(new OrderItemResponse(p.GdoorCode,p.Description,item.Quantity,unit,wholesale,unit*item.Quantity));}
 return Ok(await orders.CreateAsync(
    s,
    customerId.Value,
    calc,
    req.Payments,
    ct));}
 [HttpGet("mine")] public async Task<IActionResult> Mine([FromServices]OrderRepository orders,CancellationToken ct){var s=(SessionInfo)HttpContext.Items["Session"]!;return Ok(await orders.GetBySellerAsync(s,ct));}
 /// <summary>
/// Recoloca um pedido com erro na fila de transmissão para o GDOOR.
/// </summary>
[HttpPost("{id:int}/retry")]
public async Task<IActionResult> ReenviarAsync(
    int id,
    [FromServices] OrderRepository pedidos,
    CancellationToken cancellationToken)
{
    var sessao = HttpContext.Items["Session"] as SessionInfo;

    if (sessao is null)
        return Unauthorized();

    var reenviado = await pedidos.ReenviarAsync(
        id,
        sessao.CompanyId,
        sessao.SellerId,
        cancellationToken);

    if (!reenviado)
    {
        return BadRequest(new
        {
            mensagem =
                "O pedido não foi encontrado ou não está disponível para reenvio."
        });
    }

    return Ok(new
    {
        pedidoId = id,
        mensagem = "Pedido colocado novamente na fila de transmissão."
    });
}
}
