using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pedidos.Application.Common;
using Pedidos.Application.Notifications;
using Pedidos.Application.Orders.Dtos;
using Pedidos.Application.Orders.Interfaces;
using Pedidos.Application.Orders.Services;
using Pedidos.Domain.Entities;
using Xunit;

namespace Pedidos.Tests.Orders;

public class OrderServiceTests
{
    private readonly Mock<IOrderReadRepository> _readRepo = new();
    private readonly Mock<IOrderWriteRepository> _writeRepo = new();
    private readonly Mock<IOrderCreatedNotifier> _notifier = new();

    private OrderService CreateSut() =>
        new(_readRepo.Object, _writeRepo.Object, _notifier.Object, NullLogger<OrderService>.Instance);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public async Task ListAsync_Normaliza_Pagina(int input, int expected)
    {
        _readRepo.Setup(r => r.GetPagedAsync(It.IsAny<OrderListFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<OrderListItemDto>());

        await CreateSut().ListAsync(new OrderListFilterDto { Page = input, PageSize = 20 });

        _readRepo.Verify(r => r.GetPagedAsync(It.Is<OrderListFilterDto>(f => f.Page == expected), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(500, 100)]
    [InlineData(50, 50)]
    public async Task ListAsync_Normaliza_TamanhoDaPagina(int input, int expected)
    {
        _readRepo.Setup(r => r.GetPagedAsync(It.IsAny<OrderListFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<OrderListItemDto>());

        await CreateSut().ListAsync(new OrderListFilterDto { Page = 1, PageSize = input });

        _readRepo.Verify(r => r.GetPagedAsync(It.Is<OrderListFilterDto>(f => f.PageSize == expected), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_Limpa_BuscaEmBranco()
    {
        _readRepo.Setup(r => r.GetPagedAsync(It.IsAny<OrderListFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<OrderListItemDto>());

        await CreateSut().ListAsync(new OrderListFilterDto { Page = 1, PageSize = 20, Search = "   " });

        _readRepo.Verify(r => r.GetPagedAsync(It.Is<OrderListFilterDto>(f => f.Search == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Delega_AoRepositorioDeLeitura()
    {
        var id = Guid.NewGuid();
        var expected = new OrderDto { Id = id };
        _readRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await CreateSut().GetByIdAsync(id);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task CreateAsync_Persiste_E_Notifica()
    {
        var dto = new CreateOrderDto
        {
            CustomerId = Guid.NewGuid(),
            Items = { new CreateOrderItemDto { ProductId = Guid.NewGuid(), Quantity = 2 } }
        };
        var created = new OrderDto { Id = Guid.NewGuid() };
        _writeRepo.Setup(r => r.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await CreateSut().CreateAsync(dto);

        result.Should().BeSameAs(created);
        _writeRepo.Verify(r => r.CreateAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.Verify(n => n.NotifyAsync(created, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NaoFalha_QuandoNotificacaoFalha()
    {
        var dto = new CreateOrderDto
        {
            CustomerId = Guid.NewGuid(),
            Items = { new CreateOrderItemDto { ProductId = Guid.NewGuid(), Quantity = 1 } }
        };
        var created = new OrderDto { Id = Guid.NewGuid() };
        _writeRepo.Setup(r => r.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(created);
        _notifier.Setup(n => n.NotifyAsync(It.IsAny<OrderDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("microserviço fora do ar"));

        var act = async () => await CreateSut().CreateAsync(dto);

        var result = await act.Should().NotThrowAsync();
        result.Subject.Should().BeSameAs(created);
    }

    [Fact]
    public async Task CreateAsync_Rejeita_PedidoSemItens()
    {
        var dto = new CreateOrderDto { CustomerId = Guid.NewGuid(), Items = new() };

        var act = async () => await CreateSut().CreateAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>();
        _writeRepo.Verify(r => r.CreateAsync(It.IsAny<CreateOrderDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_Delega_AoRepositorioDeEscrita()
    {
        var id = Guid.NewGuid();
        var dto = new ChangeStatusDto { Status = OrderStatus.Paid, Note = "ok" };
        var updated = new OrderDto { Id = id, Status = "Paid" };
        _writeRepo.Setup(r => r.ChangeStatusAsync(id, OrderStatus.Paid, "ok", It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        var result = await CreateSut().ChangeStatusAsync(id, dto);

        result.Should().BeSameAs(updated);
        _writeRepo.Verify(r => r.ChangeStatusAsync(id, OrderStatus.Paid, "ok", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AppendEnrichmentAsync_Delega_AoRepositorioDeEscrita()
    {
        var id = Guid.NewGuid();
        _writeRepo.Setup(r => r.AppendEnrichmentAsync(id, "faixa: alto", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateSut().AppendEnrichmentAsync(id, "faixa: alto");

        result.Should().BeTrue();
        _writeRepo.Verify(r => r.AppendEnrichmentAsync(id, "faixa: alto", It.IsAny<CancellationToken>()), Times.Once);
    }
}
