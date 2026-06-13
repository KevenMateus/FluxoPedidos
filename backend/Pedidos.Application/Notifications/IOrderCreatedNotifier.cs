using Pedidos.Application.Orders.Dtos;

namespace Pedidos.Application.Notifications;

/// <summary>
/// Porta de saída para notificar sistemas externos quando um pedido é criado.
/// A implementação concreta (HTTP para o microserviço Node) vive na Infrastructure,
/// respeitando a Inversão de Dependência: a Application não conhece o transporte.
/// </summary>
public interface IOrderCreatedNotifier
{
    /// <summary>Notifica que um pedido foi criado. Não deve derrubar o fluxo principal em caso de falha.</summary>
    Task NotifyAsync(OrderDto order, CancellationToken cancellationToken = default);
}
