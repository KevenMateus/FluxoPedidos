using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Common;
using Pedidos.Application.Orders.Dtos;
using Pedidos.Application.Orders.Interfaces;
using Pedidos.Domain.Entities;

namespace Pedidos.Api.Controllers;

/// <summary>Endpoints de pedidos.</summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Lista pedidos de forma paginada e filtrada, já com o total de cada pedido.</summary>
    /// <param name="page">Página (base 1).</param>
    /// <param name="pageSize">Tamanho da página (máx. 100).</param>
    /// <param name="status">Filtra por status (opcional).</param>
    /// <param name="search">Busca por nome do cliente (opcional).</param>
    /// <param name="from">Pedidos criados a partir desta data (opcional).</param>
    /// <param name="to">Pedidos criados até esta data, inclusive (opcional).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderListItemDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] string? search = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var filter = new OrderListFilterDto { Page = page, PageSize = pageSize, Status = status, Search = search, From = from, To = to };
        return Ok(await _orderService.ListAsync(filter, cancellationToken));
    }

    /// <summary>Obtém um pedido completo (itens + timeline) por id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>Cria um novo pedido com itens, status inicial, forma de pagamento e observação.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        var created = await _orderService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Altera o status de um pedido (respeitando as transições válidas).</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> ChangeStatus(Guid id, [FromBody] ChangeStatusDto dto, CancellationToken cancellationToken = default)
        => Ok(await _orderService.ChangeStatusAsync(id, dto, cancellationToken));
}
