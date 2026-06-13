using Pedidos.Application.Catalog.Dtos;

namespace Pedidos.Application.Catalog.Interfaces;

/// <summary>
/// Leituras simples de catálogo (clientes/produtos para seletores). São consultas
/// diretas, sem agregação — por isso implementadas com EF Core + LINQ (projeção
/// AsNoTracking), onde o EF é mais produtivo e legível que SQL manual.
/// </summary>
public interface ICatalogReadRepository
{
    Task<IReadOnlyList<CustomerLookupDto>> GetCustomersAsync(int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductLookupDto>> GetProductsAsync(int take, CancellationToken cancellationToken = default);
}
