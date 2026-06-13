using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Common;
using Pedidos.Application.Orders.Dtos;
using Pedidos.Application.Orders.Interfaces;
using Pedidos.Domain.Entities;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Repositories;

/// <summary>
/// Escrita de pedidos via EF Core. Justificativa do uso de EF aqui: criar e mudar
/// o estado de um pedido é transacional, valida FKs e invariantes do domínio e se
/// beneficia do rastreamento de mudanças e do INSERT/UPDATE em lote do SaveChanges.
/// </summary>
public class OrderWriteRepository : IOrderWriteRepository
{
    private readonly AppDbContext _db;

    public OrderWriteRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == dto.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"Cliente {dto.CustomerId} não encontrado.");

        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"Produto(s) não encontrado(s): {string.Join(", ", missing)}");

        var items = dto.Items.Select(i =>
        {
            var product = products[i.ProductId];
            return new OrderItem(product.Id, product.Name, i.Quantity, product.UnitPrice);
        });

        var order = Order.Create(dto.CustomerId, DateTime.UtcNow, items, dto.Status, dto.PaymentMethod, dto.Notes);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return Map(order, customer.Name);
    }

    /// <inheritdoc />
    public async Task<OrderDto> ChangeStatusAsync(Guid orderId, OrderStatus newStatus, string? note, CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Events)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundException($"Pedido {orderId} não encontrado.");

        order.ChangeStatus(newStatus, DateTime.UtcNow, note);
        await _db.SaveChangesAsync(cancellationToken);

        var customerName = await _db.Customers
            .Where(c => c.Id == order.CustomerId)
            .Select(c => c.Name)
            .FirstAsync(cancellationToken);

        return Map(order, customerName);
    }

    /// <inheritdoc />
    public async Task<bool> AppendEnrichmentAsync(Guid orderId, string description, CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders
            .Include(o => o.Events)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
            return false;

        order.AddEnrichment(description, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static OrderDto Map(Order order, string customerName) => new()
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        CustomerName = customerName,
        CreatedAt = order.CreatedAt,
        Status = order.Status.ToString(),
        StatusLabel = Order.StatusLabel(order.Status),
        PaymentMethod = order.PaymentMethod.ToString(),
        PaymentMethodLabel = Order.PaymentLabel(order.PaymentMethod),
        Notes = order.Notes,
        Total = order.Total,
        Items = order.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal
        }).ToList(),
        Events = order.Events.OrderBy(e => e.OccurredAt).Select(e => new OrderEventDto
        {
            Type = e.Type.ToString(),
            Description = e.Description,
            Source = e.Source,
            OccurredAt = e.OccurredAt
        }).ToList(),
        AllowedNextStatuses = OrderStatusRules.NextOf(order.Status).Select(s => s.ToString()).ToList()
    };
}
