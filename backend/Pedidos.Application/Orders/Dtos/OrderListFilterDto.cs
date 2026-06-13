using Pedidos.Domain.Entities;

namespace Pedidos.Application.Orders.Dtos;

/// <summary>Filtros e paginação para a listagem de pedidos.</summary>
public class OrderListFilterDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    /// <summary>Filtra por status (opcional).</summary>
    public OrderStatus? Status { get; init; }

    /// <summary>Busca por nome do cliente (opcional, case-insensitive).</summary>
    public string? Search { get; init; }

    /// <summary>Filtra pedidos criados a partir desta data (opcional).</summary>
    public DateOnly? From { get; init; }

    /// <summary>Filtra pedidos criados até esta data, inclusive (opcional).</summary>
    public DateOnly? To { get; init; }
}
