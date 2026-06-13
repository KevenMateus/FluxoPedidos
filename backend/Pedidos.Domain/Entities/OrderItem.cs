using Pedidos.Domain.Common;

namespace Pedidos.Domain.Entities;

/// <summary>
/// Item de um pedido. O preço unitário é "fotografado" no momento da venda
/// (snapshot), de modo que alterações futuras no preço do produto não afetam
/// pedidos já realizados.
/// </summary>
public class OrderItem : Entity
{
    /// <summary>Pedido ao qual o item pertence.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>Produto referenciado.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Nome do produto no momento da venda (snapshot).</summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>Quantidade adquirida.</summary>
    public int Quantity { get; private set; }

    /// <summary>Preço unitário no momento da venda (snapshot).</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Total da linha (quantidade × preço unitário).</summary>
    public decimal LineTotal => Quantity * UnitPrice;

    private OrderItem() { }

    public OrderItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("A quantidade deve ser maior que zero.", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("O preço unitário não pode ser negativo.", nameof(unitPrice));

        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    /// <summary>Vincula o item à sua raiz de agregado. Uso interno do domínio.</summary>
    internal void AttachToOrder(Guid orderId) => OrderId = orderId;
}
