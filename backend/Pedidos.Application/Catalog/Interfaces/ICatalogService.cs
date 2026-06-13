using Pedidos.Application.Catalog.Dtos;

namespace Pedidos.Application.Catalog.Interfaces;

/// <summary>Casos de uso de catálogo (dados de apoio para a criação de pedidos).</summary>
public interface ICatalogService
{
    Task<IReadOnlyList<CustomerLookupDto>> GetCustomersAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductLookupDto>> GetProductsAsync(int take = 100, CancellationToken cancellationToken = default);
}
