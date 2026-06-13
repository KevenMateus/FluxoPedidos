namespace Pedidos.Application.Reports.Dtos;

/// <summary>Indicadores agregados para o dashboard.</summary>
public class DashboardSummaryDto
{
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public decimal AverageTicket { get; init; }

    /// <summary>Faturamento dos últimos 30 dias (para sparkline).</summary>
    public decimal RevenueThirtyDays { get; init; }

    /// <summary>Faturamento dos 30 dias anteriores (para calcular tendência).</summary>
    public decimal RevenuePreviousThirtyDays { get; init; }

    /// <summary>Pedidos dos últimos 30 dias.</summary>
    public int OrdersThirtyDays { get; init; }

    public IReadOnlyList<StatusCountDto> ByStatus { get; init; } = Array.Empty<StatusCountDto>();
    public IReadOnlyList<DailyRevenueDto> SparklineDays { get; init; } = Array.Empty<DailyRevenueDto>();
    public IReadOnlyList<PaymentRevenueDto> ByPaymentMethod { get; init; } = Array.Empty<PaymentRevenueDto>();
}

/// <summary>Contagem de pedidos por status.</summary>
public class StatusCountDto
{
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public int Count { get; init; }
}
