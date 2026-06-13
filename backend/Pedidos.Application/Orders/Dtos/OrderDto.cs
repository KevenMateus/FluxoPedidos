namespace Pedidos.Application.Orders.Dtos;

/// <summary>Pedido completo com itens, total e linha do tempo, como exposto pela API.</summary>
public class OrderDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentMethodLabel { get; init; } = string.Empty;
    public string? Notes { get; init; }

    /// <summary>Total do pedido (soma dos totais de linha).</summary>
    public decimal Total { get; init; }

    public IReadOnlyList<OrderItemDto> Items { get; init; } = Array.Empty<OrderItemDto>();

    /// <summary>Histórico de eventos (timeline), em ordem cronológica.</summary>
    public IReadOnlyList<OrderEventDto> Events { get; init; } = Array.Empty<OrderEventDto>();

    /// <summary>Próximos status válidos a partir do status atual (para a UI montar as ações).</summary>
    public IReadOnlyList<string> AllowedNextStatuses { get; init; } = Array.Empty<string>();
}
