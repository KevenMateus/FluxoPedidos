using Pedidos.Application.Reports.Dtos;

namespace Pedidos.Application.Reports.Interfaces;

/// <summary>Casos de uso de relatórios.</summary>
public interface IReportService
{
    /// <summary>Monta o relatório de faturamento por período (dia a dia + por forma de pagamento).</summary>
    Task<RevenueReportDto> GetRevenueByPeriodAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Indicadores agregados para o dashboard.</summary>
    Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
