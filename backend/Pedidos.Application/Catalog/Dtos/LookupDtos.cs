namespace Pedidos.Application.Catalog.Dtos;

/// <summary>Cliente em formato reduzido, para preencher seletores na UI.</summary>
public class CustomerLookupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

/// <summary>Produto em formato reduzido, para preencher seletores na UI.</summary>
public class ProductLookupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
}
