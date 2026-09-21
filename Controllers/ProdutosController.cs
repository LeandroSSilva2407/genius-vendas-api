using GeniusVendas.Api.Models;
using GeniusVendas.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GeniusVendas.Api.Controllers
{
    [ApiController]
    [Route("api/produtos")]
    public sealed class ProdutosController : ControllerBase
    {
        private readonly ProductRepository _produtoRepository;

        public ProdutosController(ProductRepository produtoRepository)
        {
            _produtoRepository = produtoRepository;
        }

        /// <summary>
        /// Retorna os produtos ativos da empresa do vendedor autenticado.
        /// Permite pesquisa por código, código de barras ou descrição.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProdutoVendaDto>>> ListarAsync(
            [FromQuery] string? pesquisa,
            [FromQuery] int pagina = 1,
            [FromQuery] int quantidade = 50,
            CancellationToken cancellationToken = default)
        {
            var sessao = HttpContext.Items["Session"] as SessionInfo;

            if (sessao is null)
                return Unauthorized();

            var produtos = await _produtoRepository.ListarParaVendaAsync(
                sessao.CompanyId,
                pesquisa,
                pagina,
                quantidade,
                cancellationToken);

            return Ok(produtos);
        }

        /// <summary>
        /// Retorna um produto pelo código utilizado no GDOOR.
        /// </summary>
        [HttpGet("{codigo}")]
        public async Task<ActionResult<ProdutoVendaDto>> ObterPorCodigoAsync(
            string codigo,
            CancellationToken cancellationToken)
        {
            var sessao = HttpContext.Items["Session"] as SessionInfo;

            if (sessao is null)
                return Unauthorized();

            var produto =
                await _produtoRepository.ObterParaVendaPorCodigoAsync(
                    sessao.CompanyId,
                    codigo,
                    cancellationToken);

            if (produto is null)
                return NotFound();

            return Ok(produto);
        }
    }
}
