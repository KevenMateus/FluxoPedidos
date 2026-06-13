using Pedidos.Application.Common;
using Pedidos.Application.Orders.Dtos;

namespace Pedidos.Application.Orders.Interfaces;

/// <summary>
/// Casos de uso do agregado Pedido. O serviço orquestra repositórios e portas,
/// trafegando exclusivamente DTOs (nunca entidades de domínio).
/// </summary>
public interface IOrderService
{
    /// <summary>Lista pedidos paginados e filtrados, com seus totais.</summary>
    Task<PagedResult<OrderListItemDto>> ListAsync(OrderListFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>Obtém um pedido completo por id.</summary>
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Cria um pedido e dispara a notificação de "pedido criado".</summary>
    Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);

    /// <summary>Altera o status de um pedido.</summary>
    Task<OrderDto> ChangeStatusAsync(Guid orderId, ChangeStatusDto dto, CancellationToken cancellationToken = default);

    /// <summary>Anexa um evento de enriquecimento (chamado pelo microserviço).</summary>
    Task<bool> AppendEnrichmentAsync(Guid orderId, string description, CancellationToken cancellationToken = default);
}
