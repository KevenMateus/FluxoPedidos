using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Orders.Dtos;
using Pedidos.Application.Orders.Interfaces;

namespace Pedidos.Api.Controllers;

/// <summary>
/// Endpoints internos chamados pelo microserviço (não pelo navegador). Protegidos
/// por um token de serviço compartilhado (header X-Service-Token) em vez de JWT —
/// é comunicação máquina-a-máquina.
/// </summary>
[ApiController]
[Route("api/internal")]
[Produces("application/json")]
[AllowAnonymous]
public class InternalController : ControllerBase
{
    private const string ServiceTokenHeader = "X-Service-Token";

    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;

    public InternalController(IOrderService orderService, IConfiguration configuration)
    {
        _orderService = orderService;
        _configuration = configuration;
    }

    /// <summary>Anexa um evento de enriquecimento à timeline de um pedido (uso do microserviço).</summary>
    [HttpPost("orders/{id:guid}/enrichment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AppendEnrichment(Guid id, [FromBody] AppendEnrichmentDto dto, CancellationToken cancellationToken = default)
    {
        var expected = _configuration["Services:Token"];
        if (string.IsNullOrEmpty(expected) ||
            !Request.Headers.TryGetValue(ServiceTokenHeader, out var provided) ||
            provided != expected)
        {
            return Unauthorized();
        }

        var ok = await _orderService.AppendEnrichmentAsync(id, dto.Description, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
