using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Catalog.Dtos;
using Pedidos.Application.Catalog.Interfaces;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Repositories;

/// <summary>
/// Leitura de catálogo via EF Core + LINQ. Consultas diretas e simples: projeção
/// para DTO com AsNoTracking. É o cenário em que o EF brilha — sem SQL manual.
/// </summary>
public class CatalogReadRepository : ICatalogReadRepository
{
    private readonly AppDbContext _db;

    public CatalogReadRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CustomerLookupDto>> GetCustomersAsync(int take, CancellationToken cancellationToken = default)
    {
        return await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Take(take)
            .Select(c => new CustomerLookupDto { Id = c.Id, Name = c.Name, Email = c.Email })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductLookupDto>> GetProductsAsync(int take, CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Take(take)
            .Select(p => new ProductLookupDto { Id = p.Id, Name = p.Name, Sku = p.Sku, UnitPrice = p.UnitPrice })
            .ToListAsync(cancellationToken);
    }
}
