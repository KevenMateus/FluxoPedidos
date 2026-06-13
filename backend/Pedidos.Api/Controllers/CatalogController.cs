using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Catalog.Dtos;
using Pedidos.Application.Catalog.Interfaces;

namespace Pedidos.Api.Controllers;

/// <summary>Dados de apoio (clientes e produtos) para preencher a tela de criação de pedidos.</summary>
[ApiController]
[Route("api/catalog")]
[Produces("application/json")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>Lista clientes (para o seletor de cliente).</summary>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerLookupDto>>> Customers([FromQuery] int take = 100, CancellationToken cancellationToken = default)
        => Ok(await _catalogService.GetCustomersAsync(take, cancellationToken));

    /// <summary>Lista produtos (para o seletor de itens).</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductLookupDto>>> Products([FromQuery] int take = 100, CancellationToken cancellationToken = default)
        => Ok(await _catalogService.GetProductsAsync(take, cancellationToken));
}
