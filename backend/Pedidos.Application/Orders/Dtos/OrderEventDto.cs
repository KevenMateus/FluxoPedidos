namespace Pedidos.Application.Orders.Dtos;

/// <summary>Evento da linha do tempo de um pedido, como exposto pela API.</summary>
public class OrderEventDto
{
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}
