using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Infrastructure.Seed;

namespace Pedidos.Api.Controllers;

/// <summary>
/// Endpoint utilitário para gerar VOLUME de dados sob demanda (teste de performance).
/// Em produção ficaria protegido/atrás de feature flag; aqui é aberto de propósito
/// para facilitar a avaliação.
/// </summary>
[ApiController]
[Route("api/seed")]
[Produces("application/json")]
[AllowAnonymous]
public class SeedController : ControllerBase
{
    private readonly DataSeeder _seeder;

    public SeedController(DataSeeder seeder)
    {
        _seeder = seeder;
    }

    /// <summary>Gera N pedidos adicionais (com itens) usando COPY binário.</summary>
    /// <param name="count">Quantidade de pedidos a gerar (1..1.000.000).</param>
    [HttpPost("orders")]
    [ProducesResponseType(typeof(SeedResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SeedResult>> GenerateOrders(
        [FromQuery] int count = 50_000,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 1_000_000);
        var result = await _seeder.GenerateForPerformanceAsync(count, cancellationToken);
        return Ok(result);
    }

}
