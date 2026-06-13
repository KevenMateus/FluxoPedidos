using System.Text;
using Dapper;
using Pedidos.Application.Common;
using Pedidos.Application.Orders.Dtos;
using Pedidos.Application.Orders.Interfaces;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Dapper;

/// <summary>
/// Leitura de pedidos via Dapper. São consultas pesadas (paginação + filtros +
/// agregação de totais sobre milhares de pedidos/itens). Optei por Dapper aqui para
/// ter controle fino do SQL: pagino os pedidos pelo índice de created_at ANTES de
/// agregar os itens (LATERAL JOIN), evitando agregar a tabela inteira a cada página.
/// </summary>
public class OrderReadRepository : IOrderReadRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrderReadRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private sealed record ListRow(Guid Id, Guid CustomerId, string CustomerName, DateTime CreatedAt, int Status, int PaymentMethod, int ItemCount, decimal Total);
    private sealed record HeaderRow(Guid Id, Guid CustomerId, string CustomerName, DateTime CreatedAt, int Status, int PaymentMethod, string? Notes);
    private sealed record ItemRow(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
    private sealed record EventRow(int Type, string Description, string Source, DateTime OccurredAt);

    /// <inheritdoc />
    public async Task<PagedResult<OrderListItemDto>> GetPagedAsync(OrderListFilterDto filter, CancellationToken cancellationToken = default)
    {
        var offset = (filter.Page - 1) * filter.PageSize;

        var conds = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", offset);

        var needsSearchJoin = !string.IsNullOrWhiteSpace(filter.Search);

        if (filter.Status.HasValue)
        {
            conds.Add("o.status = @Status");
            parameters.Add("Status", (int)filter.Status.Value);
        }
        if (needsSearchJoin)
        {
            conds.Add("cf.name ILIKE @Search");
            parameters.Add("Search", $"%{filter.Search!.Trim()}%");
        }
        if (filter.From.HasValue)
        {
            conds.Add("o.created_at >= @FromUtc");
            parameters.Add("FromUtc", DateTime.SpecifyKind(filter.From.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));
        }
        if (filter.To.HasValue)
        {
            conds.Add("o.created_at < @ToExclusiveUtc");
            parameters.Add("ToExclusiveUtc", DateTime.SpecifyKind(filter.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));
        }

        var where = conds.Count > 0 ? "WHERE " + string.Join(" AND ", conds) : string.Empty;

        var sql = new StringBuilder();
        sql.Append("SELECT count(*) FROM orders o ");
        if (needsSearchJoin) sql.Append("JOIN customers cf ON cf.id = o.customer_id ");
        sql.Append(where).AppendLine(";");
        sql.AppendLine();
        sql.AppendLine("SELECT o.id             AS Id,");
        sql.AppendLine("       o.customer_id    AS CustomerId,");
        sql.AppendLine("       c.name           AS CustomerName,");
        sql.AppendLine("       o.created_at     AS CreatedAt,");
        sql.AppendLine("       o.status         AS Status,");
        sql.AppendLine("       o.payment_method AS PaymentMethod,");
        sql.AppendLine("       COALESCE(agg.item_count, 0)::int AS ItemCount,");
        sql.AppendLine("       COALESCE(agg.total, 0)           AS Total");
        sql.AppendLine("FROM (");
        sql.AppendLine("    SELECT o.id, o.customer_id, o.created_at, o.status, o.payment_method");
        sql.AppendLine("    FROM orders o");
        if (needsSearchJoin) sql.AppendLine("    JOIN customers cf ON cf.id = o.customer_id");
        if (where.Length > 0) sql.Append("    ").AppendLine(where);
        sql.AppendLine("    ORDER BY o.created_at DESC, o.id");
        sql.AppendLine("    LIMIT @PageSize OFFSET @Offset");
        sql.AppendLine(") o");
        sql.AppendLine("JOIN customers c ON c.id = o.customer_id");
        sql.AppendLine("LEFT JOIN LATERAL (");
        sql.AppendLine("    SELECT COUNT(*) AS item_count, SUM(i.quantity * i.unit_price) AS total");
        sql.AppendLine("    FROM order_items i");
        sql.AppendLine("    WHERE i.order_id = o.id");
        sql.AppendLine(") agg ON TRUE");
        sql.AppendLine("ORDER BY o.created_at DESC, o.id;");

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql.ToString(), parameters, cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);
        var totalCount = await multi.ReadSingleAsync<long>();
        var rows = (await multi.ReadAsync<ListRow>()).ToList();

        var items = rows.Select(r =>
        {
            var status = (OrderStatus)r.Status;
            var payment = (PaymentMethod)r.PaymentMethod;
            return new OrderListItemDto
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                CustomerName = r.CustomerName,
                CreatedAt = r.CreatedAt,
                Status = status.ToString(),
                StatusLabel = Order.StatusLabel(status),
                PaymentMethod = payment.ToString(),
                PaymentMethodLabel = Order.PaymentLabel(payment),
                ItemCount = r.ItemCount,
                Total = r.Total
            };
        }).ToList();

        return new PagedResult<OrderListItemDto>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc />
    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = new StringBuilder();
        sql.AppendLine("SELECT o.id             AS Id,");
        sql.AppendLine("       o.customer_id    AS CustomerId,");
        sql.AppendLine("       c.name           AS CustomerName,");
        sql.AppendLine("       o.created_at     AS CreatedAt,");
        sql.AppendLine("       o.status         AS Status,");
        sql.AppendLine("       o.payment_method AS PaymentMethod,");
        sql.AppendLine("       o.notes          AS Notes");
        sql.AppendLine("FROM orders o");
        sql.AppendLine("JOIN customers c ON c.id = o.customer_id");
        sql.AppendLine("WHERE o.id = @Id;");
        sql.AppendLine();
        sql.AppendLine("SELECT i.product_id              AS ProductId,");
        sql.AppendLine("       i.product_name            AS ProductName,");
        sql.AppendLine("       i.quantity                AS Quantity,");
        sql.AppendLine("       i.unit_price              AS UnitPrice,");
        sql.AppendLine("       i.quantity * i.unit_price AS LineTotal");
        sql.AppendLine("FROM order_items i");
        sql.AppendLine("WHERE i.order_id = @Id;");
        sql.AppendLine();
        sql.AppendLine("SELECT e.type        AS Type,");
        sql.AppendLine("       e.description  AS Description,");
        sql.AppendLine("       e.source       AS Source,");
        sql.AppendLine("       e.occurred_at  AS OccurredAt");
        sql.AppendLine("FROM order_events e");
        sql.AppendLine("WHERE e.order_id = @Id");
        sql.AppendLine("ORDER BY e.occurred_at, e.id;");

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql.ToString(), new { Id = id }, cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);
        var header = await multi.ReadSingleOrDefaultAsync<HeaderRow>();
        if (header is null)
            return null;

        var items = (await multi.ReadAsync<ItemRow>()).Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal
        }).ToList();

        var events = (await multi.ReadAsync<EventRow>()).Select(e => new OrderEventDto
        {
            Type = ((OrderEventType)e.Type).ToString(),
            Description = e.Description,
            Source = e.Source,
            OccurredAt = e.OccurredAt
        }).ToList();

        var status = (OrderStatus)header.Status;
        var payment = (PaymentMethod)header.PaymentMethod;

        return new OrderDto
        {
            Id = header.Id,
            CustomerId = header.CustomerId,
            CustomerName = header.CustomerName,
            CreatedAt = header.CreatedAt,
            Status = status.ToString(),
            StatusLabel = Order.StatusLabel(status),
            PaymentMethod = payment.ToString(),
            PaymentMethodLabel = Order.PaymentLabel(payment),
            Notes = header.Notes,
            Total = items.Sum(i => i.LineTotal),
            Items = items,
            Events = events,
            AllowedNextStatuses = OrderStatusRules.NextOf(status).Select(s => s.ToString()).ToList()
        };
    }
}
