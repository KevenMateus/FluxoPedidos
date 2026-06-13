using FluentAssertions;
using Moq;
using Pedidos.Application.Reports.Dtos;
using Pedidos.Application.Reports.Interfaces;
using Pedidos.Application.Reports.Services;
using Xunit;

namespace Pedidos.Tests.Reports;

public class ReportServiceTests
{
    private readonly Mock<IRevenueRepository> _repo = new();

    private ReportService CreateSut() => new(_repo.Object);

    [Fact]
    public async Task GetRevenueByPeriodAsync_Agrega_Totais()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 1, 3);
        var days = new List<DailyRevenueDto>
        {
            new() { Date = from, OrderCount = 2, Revenue = 100m },
            new() { Date = to,   OrderCount = 3, Revenue = 250.50m }
        };
        _repo.Setup(r => r.GetRevenueByDayAsync(from, to, It.IsAny<CancellationToken>())).ReturnsAsync(days);
        _repo.Setup(r => r.GetRevenueByPaymentMethodAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PaymentRevenueDto>());

        var report = await CreateSut().GetRevenueByPeriodAsync(from, to);

        report.From.Should().Be(from);
        report.To.Should().Be(to);
        report.TotalRevenue.Should().Be(350.50m);
        report.TotalOrders.Should().Be(5);
        report.Days.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRevenueByPeriodAsync_Rejeita_IntervaloInvertido()
    {
        var from = new DateOnly(2026, 5, 10);
        var to = new DateOnly(2026, 5, 1);

        var act = async () => await CreateSut().GetRevenueByPeriodAsync(from, to);

        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.GetRevenueByDayAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRevenueByPeriodAsync_PeriodoVazio_RetornaZeros()
    {
        var from = new DateOnly(2026, 2, 1);
        var to = new DateOnly(2026, 2, 28);
        _repo.Setup(r => r.GetRevenueByDayAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyRevenueDto>());
        _repo.Setup(r => r.GetRevenueByPaymentMethodAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PaymentRevenueDto>());

        var report = await CreateSut().GetRevenueByPeriodAsync(from, to);

        report.TotalRevenue.Should().Be(0);
        report.TotalOrders.Should().Be(0);
        report.Days.Should().BeEmpty();
    }
}
