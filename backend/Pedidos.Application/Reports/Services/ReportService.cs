using Pedidos.Application.Reports.Dtos;
using Pedidos.Application.Reports.Interfaces;

namespace Pedidos.Application.Reports.Services;

/// <summary>
/// Implementação dos casos de uso de relatórios. Orquestra o repositório de
/// faturamento e agrega os totais do período — trafegando somente DTOs.
/// </summary>
public class ReportService : IReportService
{
    private readonly IRevenueRepository _revenueRepository;

    public ReportService(IRevenueRepository revenueRepository)
    {
        _revenueRepository = revenueRepository;
    }

    /// <inheritdoc />
    public async Task<RevenueReportDto> GetRevenueByPeriodAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (to < from)
            throw new ArgumentException("A data final não pode ser anterior à data inicial.", nameof(to));

        var days = await _revenueRepository.GetRevenueByDayAsync(from, to, cancellationToken);
        var byPayment = await _revenueRepository.GetRevenueByPaymentMethodAsync(from, to, cancellationToken);

        return new RevenueReportDto
        {
            From = from,
            To = to,
            Days = days,
            ByPaymentMethod = byPayment,
            TotalRevenue = days.Sum(d => d.Revenue),
            TotalOrders = days.Sum(d => d.OrderCount)
        };
    }

    /// <inheritdoc />
    public Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        => _revenueRepository.GetDashboardAsync(cancellationToken);
}
