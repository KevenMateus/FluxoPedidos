using FluentAssertions;
using Moq;
using Pedidos.Application.Catalog.Dtos;
using Pedidos.Application.Catalog.Interfaces;
using Pedidos.Application.Catalog.Services;
using Xunit;

namespace Pedidos.Tests.Catalog;

public class CatalogServiceTests
{
    private readonly Mock<ICatalogReadRepository> _repo = new();

    private CatalogService CreateSut() => new(_repo.Object);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1000, 500)]
    [InlineData(100, 100)]
    public async Task GetCustomersAsync_Limita_Take(int input, int expected)
    {
        _repo.Setup(r => r.GetCustomersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CustomerLookupDto>());

        await CreateSut().GetCustomersAsync(input);

        _repo.Verify(r => r.GetCustomersAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductsAsync_Delega_AoRepositorio()
    {
        var products = new List<ProductLookupDto> { new() { Id = Guid.NewGuid(), Name = "Café" } };
        _repo.Setup(r => r.GetProductsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var result = await CreateSut().GetProductsAsync(50);

        result.Should().BeEquivalentTo(products);
        _repo.Verify(r => r.GetProductsAsync(50, It.IsAny<CancellationToken>()), Times.Once);
    }
}
