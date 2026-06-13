namespace Pedidos.Application.Orders.Dtos;

/// <summary>Item de um pedido, como exposto pela API.</summary>
public class OrderItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}
