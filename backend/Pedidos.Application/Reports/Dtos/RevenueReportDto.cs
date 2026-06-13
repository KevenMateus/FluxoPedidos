namespace Pedidos.Application.Reports.Dtos;

/// <summary>Relatório de faturamento por período, dia a dia.</summary>
public class RevenueReportDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }

    /// <summary>Soma do faturamento de todos os dias do período.</summary>
    public decimal TotalRevenue { get; init; }

    /// <summary>Total de pedidos no período.</summary>
    public int TotalOrders { get; init; }

    /// <summary>Detalhamento dia a dia (apenas dias com faturamento).</summary>
    public IReadOnlyList<DailyRevenueDto> Days { get; init; } = Array.Empty<DailyRevenueDto>();

    /// <summary>Quebra do faturamento por forma de pagamento no período.</summary>
    public IReadOnlyList<PaymentRevenueDto> ByPaymentMethod { get; init; } = Array.Empty<PaymentRevenueDto>();
}

/// <summary>Faturamento agregado por forma de pagamento.</summary>
public class PaymentRevenueDto
{
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentMethodLabel { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public decimal Revenue { get; init; }
}
