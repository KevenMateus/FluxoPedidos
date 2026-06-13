namespace Pedidos.Application.Reports.Dtos;

/// <summary>Indicadores agregados para o dashboard (sobre todo o histórico).</summary>
public class DashboardSummaryDto
{
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public decimal AverageTicket { get; init; }
    public IReadOnlyList<StatusCountDto> ByStatus { get; init; } = Array.Empty<StatusCountDto>();
}

/// <summary>Contagem de pedidos por status.</summary>
public class StatusCountDto
{
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public int Count { get; init; }
}
