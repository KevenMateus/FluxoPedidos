using Pedidos.Domain.Common;

namespace Pedidos.Domain.Entities;

/// <summary>
/// Produto que pode ser adicionado a um pedido.
/// </summary>
public class Product : Entity
{
    /// <summary>Nome do produto.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Código (SKU) do produto.</summary>
    public string Sku { get; private set; } = string.Empty;

    /// <summary>Preço unitário corrente do produto.</summary>
    public decimal UnitPrice { get; private set; }

    private Product() { }

    public Product(string name, string sku, decimal unitPrice)
    {
        Name = name;
        Sku = sku;
        UnitPrice = unitPrice;
    }
}
