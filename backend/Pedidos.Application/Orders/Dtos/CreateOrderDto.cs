using System.ComponentModel.DataAnnotations;
using Pedidos.Domain.Entities;

namespace Pedidos.Application.Orders.Dtos;

/// <summary>Payload para criação de um pedido.</summary>
public class CreateOrderDto
{
    /// <summary>Cliente que está realizando o pedido.</summary>
    [Required]
    public Guid CustomerId { get; init; }

    /// <summary>Status inicial (apenas Pending ou Paid). Padrão: Pending.</summary>
    public OrderStatus Status { get; init; } = OrderStatus.Pending;

    /// <summary>Forma de pagamento. Padrão: Pix.</summary>
    public PaymentMethod PaymentMethod { get; init; } = PaymentMethod.Pix;

    /// <summary>Observação opcional sobre o pedido.</summary>
    [MaxLength(1000)]
    public string? Notes { get; init; }

    /// <summary>Itens do pedido (ao menos um).</summary>
    [Required]
    [MinLength(1, ErrorMessage = "O pedido deve conter ao menos um item.")]
    public List<CreateOrderItemDto> Items { get; init; } = new();
}

/// <summary>
/// Item informado na criação. O preço unitário NÃO é informado pelo cliente —
/// é resolvido no servidor a partir do produto, evitando adulteração de preço.
/// </summary>
public class CreateOrderItemDto
{
    [Required]
    public Guid ProductId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public int Quantity { get; init; }
}

/// <summary>Payload para alterar o status de um pedido.</summary>
public class ChangeStatusDto
{
    [Required]
    public OrderStatus Status { get; init; }

    /// <summary>Observação opcional registrada junto à mudança de status.</summary>
    [MaxLength(1000)]
    public string? Note { get; init; }
}

/// <summary>Payload enviado pelo microserviço para anexar um evento de enriquecimento.</summary>
public class AppendEnrichmentDto
{
    [Required]
    [MaxLength(500)]
    public string Description { get; init; } = string.Empty;
}
