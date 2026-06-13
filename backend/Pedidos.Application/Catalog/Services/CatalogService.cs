using Pedidos.Application.Catalog.Dtos;
using Pedidos.Application.Catalog.Interfaces;

namespace Pedidos.Application.Catalog.Services;

/// <summary>Implementação dos casos de uso de catálogo. Apenas orquestra seu repositório.</summary>
public class CatalogService : ICatalogService
{
    private const int MaxTake = 500;
    private readonly ICatalogReadRepository _repository;

    public CatalogService(ICatalogReadRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CustomerLookupDto>> GetCustomersAsync(int take = 100, CancellationToken cancellationToken = default)
        => _repository.GetCustomersAsync(Math.Clamp(take, 1, MaxTake), cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ProductLookupDto>> GetProductsAsync(int take = 100, CancellationToken cancellationToken = default)
        => _repository.GetProductsAsync(Math.Clamp(take, 1, MaxTake), cancellationToken);
}
