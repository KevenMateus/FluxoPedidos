using Pedidos.Application.Orders.Dtos;
using Pedidos.Domain.Entities;

namespace Pedidos.Application.Orders.Interfaces;

/// <summary>
/// Lado de ESCRITA do agregado Pedido. Implementado com EF Core, pois a criação e
/// as mudanças de estado envolvem transação, rastreamento de mudanças e validação
/// de invariantes do domínio — cenário em que o EF é mais produtivo e seguro.
/// </summary>
public interface IOrderWriteRepository
{
    /// <summary>
    /// Persiste um novo pedido. Resolve os preços dos produtos no servidor, monta o
    /// agregado de domínio (com status/pagamento/observação e eventos iniciais) e
    /// grava tudo em uma única transação.
    /// </summary>
    Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);

    /// <summary>Altera o status do pedido (validando a transição) e registra os eventos.</summary>
    Task<OrderDto> ChangeStatusAsync(Guid orderId, OrderStatus newStatus, string? note, CancellationToken cancellationToken = default);

    /// <summary>Anexa um evento de enriquecimento à timeline (uso do microserviço).</summary>
    Task<bool> AppendEnrichmentAsync(Guid orderId, string description, CancellationToken cancellationToken = default);
}
