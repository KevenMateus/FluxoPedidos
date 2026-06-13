namespace Pedidos.Application.Orders.Dtos;

/// <summary>
/// Versão "enxuta" do pedido para a listagem paginada. Evita trazer todos os
/// itens de milhares de pedidos de uma vez — traz apenas o total e a contagem.
/// </summary>
public class OrderListItemDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentMethodLabel { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public decimal Total { get; init; }
}
