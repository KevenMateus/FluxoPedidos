using System.Text;
using Dapper;
using Pedidos.Application.Reports.Dtos;
using Pedidos.Application.Reports.Interfaces;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Dapper;

/// <summary>
/// Faturamento e indicadores via Dapper. São agregações (GROUP BY) que cruzam
/// orders × order_items sobre o histórico, filtrando pelo índice de created_at.
/// SQL explícito + mapeamento sem tracking é o caminho mais eficiente.
/// </summary>
public class RevenueRepository : IRevenueRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RevenueRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private sealed record RevenueRow(DateTime Day, int OrderCount, decimal Revenue);
    private sealed record PaymentRow(int PaymentMethod, int OrderCount, decimal Revenue);
    private sealed record TotalsRow(decimal TotalRevenue, int TotalOrders);
    private sealed record StatusRow(int Status, int Count);

    private static (DateTime from, DateTime toExclusive) ToUtcRange(DateOnly from, DateOnly to)
        => (DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
            DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));

    /// <inheritdoc />
    public async Task<IReadOnlyList<DailyRevenueDto>> GetRevenueByDayAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toExclusiveUtc) = ToUtcRange(from, to);

        var sql = new StringBuilder();
        sql.AppendLine("SELECT (o.created_at AT TIME ZONE 'UTC')::date AS Day,");
        sql.AppendLine("       COUNT(DISTINCT o.id)::int               AS OrderCount,");
        sql.AppendLine("       COALESCE(SUM(i.quantity * i.unit_price), 0) AS Revenue");
        sql.AppendLine("FROM orders o");
        sql.AppendLine("JOIN order_items i ON i.order_id = o.id");
        sql.AppendLine("WHERE o.created_at >= @FromUtc AND o.created_at < @ToExclusiveUtc");
        sql.AppendLine("GROUP BY (o.created_at AT TIME ZONE 'UTC')::date");
        sql.AppendLine("ORDER BY Day;");

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql.ToString(), new { FromUtc = fromUtc, ToExclusiveUtc = toExclusiveUtc }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RevenueRow>(command);

        return rows.Select(r => new DailyRevenueDto
        {
            Date = DateOnly.FromDateTime(r.Day),
            OrderCount = r.OrderCount,
            Revenue = r.Revenue
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentRevenueDto>> GetRevenueByPaymentMethodAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toExclusiveUtc) = ToUtcRange(from, to);

        var sql = new StringBuilder();
        sql.AppendLine("SELECT o.payment_method               AS PaymentMethod,");
        sql.AppendLine("       COUNT(DISTINCT o.id)::int       AS OrderCount,");
        sql.AppendLine("       COALESCE(SUM(i.quantity * i.unit_price), 0) AS Revenue");
        sql.AppendLine("FROM orders o");
        sql.AppendLine("JOIN order_items i ON i.order_id = o.id");
        sql.AppendLine("WHERE o.created_at >= @FromUtc AND o.created_at < @ToExclusiveUtc");
        sql.AppendLine("GROUP BY o.payment_method");
        sql.AppendLine("ORDER BY Revenue DESC;");

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql.ToString(), new { FromUtc = fromUtc, ToExclusiveUtc = toExclusiveUtc }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<PaymentRow>(command);

        return rows.Select(r =>
        {
            var method = (PaymentMethod)r.PaymentMethod;
            return new PaymentRevenueDto
            {
                PaymentMethod = method.ToString(),
                PaymentMethodLabel = Order.PaymentLabel(method),
                OrderCount = r.OrderCount,
                Revenue = r.Revenue
            };
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var sql = new StringBuilder();
        sql.AppendLine("SELECT COALESCE(SUM(i.quantity * i.unit_price), 0) AS TotalRevenue,");
        sql.AppendLine("       COUNT(DISTINCT o.id)::int                   AS TotalOrders");
        sql.AppendLine("FROM orders o");
        sql.AppendLine("JOIN order_items i ON i.order_id = o.id;");
        sql.AppendLine();
        sql.AppendLine("SELECT status AS Status, COUNT(*)::int AS Count");
        sql.AppendLine("FROM orders");
        sql.AppendLine("GROUP BY status");
        sql.AppendLine("ORDER BY status;");

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql.ToString(), cancellationToken: cancellationToken);
        using var multi = await connection.QueryMultipleAsync(command);

        var totals = await multi.ReadSingleAsync<TotalsRow>();
        var statusRows = (await multi.ReadAsync<StatusRow>()).ToList();

        return new DashboardSummaryDto
        {
            TotalRevenue = totals.TotalRevenue,
            TotalOrders = totals.TotalOrders,
            AverageTicket = totals.TotalOrders > 0 ? Math.Round(totals.TotalRevenue / totals.TotalOrders, 2) : 0,
            ByStatus = statusRows.Select(s =>
            {
                var status = (OrderStatus)s.Status;
                return new StatusCountDto { Status = status.ToString(), StatusLabel = Order.StatusLabel(status), Count = s.Count };
            }).ToList()
        };
    }
}
