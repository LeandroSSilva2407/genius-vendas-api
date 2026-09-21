using GeniusVendas.Api.Models;
using GeniusVendas.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GeniusVendas.Api.Controllers
{
    [ApiController]
    [Route("api/clientes")]
    public sealed class ClientesController : ControllerBase
    {
        private readonly CustomerRepository _clienteRepository;

        public ClientesController(CustomerRepository clienteRepository)
        {
            _clienteRepository = clienteRepository;
        }

        /// <summary>
        /// Lista os clientes ativos pertencentes à empresa do vendedor.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ClienteVendaDto>>> ListarAsync(
            [FromQuery] string? pesquisa,
            [FromQuery] int pagina = 1,
            [FromQuery] int quantidade = 50,
            CancellationToken cancellationToken = default)
        {
            var sessao = HttpContext.Items["Session"] as SessionInfo;

            if (sessao is null)
                return Unauthorized();

            var clientes =
                await _clienteRepository.ListarParaVendaAsync(
                    sessao.CompanyId,
                    pesquisa,
                    pagina,
                    quantidade,
                    cancellationToken);

            return Ok(clientes);
        }

        /// <summary>
        /// Obtém um cliente pelo código utilizado no GDOOR.
        /// </summary>
        [HttpGet("{codigo}")]
        public async Task<ActionResult<ClienteVendaDto>> ObterPorCodigoAsync(
            string codigo,
            CancellationToken cancellationToken)
        {
            var sessao = HttpContext.Items["Session"] as SessionInfo;

            if (sessao is null)
                return Unauthorized();

            var cliente =
                await _clienteRepository.ObterParaVendaPorCodigoAsync(
                    sessao.CompanyId,
                    codigo,
                    cancellationToken);

            if (cliente is null)
                return NotFound();

            return Ok(cliente);
        }
    }
}
