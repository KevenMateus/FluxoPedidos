using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Reports.Dtos;
using Pedidos.Application.Reports.Interfaces;

namespace Pedidos.Api.Controllers;

/// <summary>Endpoints de relatórios.</summary>
[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>Faturamento agregado por dia e por forma de pagamento no intervalo (inclusivo).</summary>
    /// <param name="from">Data inicial (yyyy-MM-dd).</param>
    /// <param name="to">Data final (yyyy-MM-dd).</param>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(RevenueReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RevenueReportDto>> Revenue([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken = default)
        => Ok(await _reportService.GetRevenueByPeriodAsync(from, to, cancellationToken));

    /// <summary>Exporta o relatório de faturamento do período em CSV (gerado no servidor).</summary>
    [HttpGet("revenue/csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> RevenueCsv([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken = default)
    {
        var report = await _reportService.GetRevenueByPeriodAsync(from, to, cancellationToken);
        var pt = new CultureInfo("pt-BR");

        var sb = new StringBuilder();
        sb.AppendLine("Data;Pedidos;Faturamento");
        foreach (var d in report.Days)
            sb.AppendLine($"{d.Date:dd/MM/yyyy};{d.OrderCount};{d.Revenue.ToString("F2", pt)}");
        sb.AppendLine();
        sb.AppendLine($"Total;{report.TotalOrders};{report.TotalRevenue.ToString("F2", pt)}");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"faturamento_{from:yyyyMMdd}_{to:yyyyMMdd}.csv";
        return File(bytes, "text/csv", fileName);
    }

    /// <summary>Indicadores agregados para o dashboard.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> Dashboard(CancellationToken cancellationToken = default)
        => Ok(await _reportService.GetDashboardAsync(cancellationToken));
}
