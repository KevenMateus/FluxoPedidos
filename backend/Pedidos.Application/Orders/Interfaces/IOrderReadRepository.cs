using Pedidos.Application.Common;
using Pedidos.Application.Orders.Dtos;

namespace Pedidos.Application.Orders.Interfaces;

/// <summary>
/// Lado de LEITURA do agregado Pedido. Implementado com Dapper, pois são consultas
/// pesadas: listagem paginada e filtrada com agregação de totais sobre milhares de
/// pedidos. SQL afiado evita o overhead do tracking do EF.
/// </summary>
public interface IOrderReadRepository
{
    /// <summary>Lista pedidos paginados e filtrados, já com total e contagem de itens calculados no banco.</summary>
    Task<PagedResult<OrderListItemDto>> GetPagedAsync(OrderListFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>Retorna um pedido completo (itens + timeline) ou <c>null</c> se não existir.</summary>
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
