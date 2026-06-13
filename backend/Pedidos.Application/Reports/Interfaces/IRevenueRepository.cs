using Pedidos.Application.Reports.Dtos;

namespace Pedidos.Application.Reports.Interfaces;

/// <summary>
/// Leitura analítica de faturamento/indicadores. Implementado com Dapper: são
/// agregações (GROUP BY) sobre todo o histórico de pedidos — consultas pesadas que
/// se beneficiam de SQL explícito e do mapeamento direto e sem tracking do Dapper.
/// </summary>
public interface IRevenueRepository
{
    /// <summary>Faturamento por dia dentro do intervalo [from, to] (inclusivo).</summary>
    Task<IReadOnlyList<DailyRevenueDto>> GetRevenueByDayAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Faturamento por forma de pagamento dentro do intervalo [from, to].</summary>
    Task<IReadOnlyList<PaymentRevenueDto>> GetRevenueByPaymentMethodAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Indicadores agregados sobre todo o histórico (para o dashboard).</summary>
    Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
