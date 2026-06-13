namespace Pedidos.Domain.Entities;

/// <summary>
/// Situação de um pedido ao longo do seu ciclo de vida.
/// </summary>
public enum OrderStatus
{
    /// <summary>Criado, aguardando pagamento.</summary>
    Pending = 0,

    /// <summary>Pago.</summary>
    Paid = 1,

    /// <summary>Enviado ao cliente.</summary>
    Shipped = 2,

    /// <summary>Entregue.</summary>
    Delivered = 3,

    /// <summary>Cancelado.</summary>
    Cancelled = 4
}

/// <summary>
/// Regras de transição de status. Concentra o fluxo válido do pedido num único
/// lugar (não se pula de Cancelado para Pago, por exemplo).
/// </summary>
public static class OrderStatusRules
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.Paid] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(),
    };

    /// <summary>Indica se a transição de <paramref name="from"/> para <paramref name="to"/> é permitida.</summary>
    public static bool CanTransition(OrderStatus from, OrderStatus to)
        => from != to && Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    /// <summary>Próximos status válidos a partir de um status atual.</summary>
    public static IReadOnlyList<OrderStatus> NextOf(OrderStatus status)
        => Allowed.TryGetValue(status, out var targets) ? targets : Array.Empty<OrderStatus>();
}
