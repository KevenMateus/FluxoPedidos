using FluentAssertions;
using Pedidos.Domain.Entities;
using Xunit;

namespace Pedidos.Tests.Orders;

/// <summary>Testes das invariantes e do fluxo de status no agregado de domínio.</summary>
public class OrderDomainTests
{
    private static Order NewOrder(OrderStatus status = OrderStatus.Pending)
    {
        var item = new OrderItem(Guid.NewGuid(), "Café", 2, 10m);
        return Order.Create(Guid.NewGuid(), DateTime.UtcNow, new[] { item }, status, PaymentMethod.Pix, null);
    }

    [Fact]
    public void Create_GeraEventoDeCriacao_E_CalculaTotal()
    {
        var order = NewOrder();
        order.Total.Should().Be(20m);
        order.Events.Should().ContainSingle(e => e.Type == OrderEventType.Created);
    }

    [Fact]
    public void Create_Pago_GeraEventoDePagamento()
    {
        var order = NewOrder(OrderStatus.Paid);
        order.Events.Should().Contain(e => e.Type == OrderEventType.PaymentReceived);
    }

    [Fact]
    public void Create_ComStatusInvalido_Lanca()
    {
        var item = new OrderItem(Guid.NewGuid(), "Café", 1, 10m);
        var act = () => Order.Create(Guid.NewGuid(), DateTime.UtcNow, new[] { item }, OrderStatus.Delivered, PaymentMethod.Pix, null);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ChangeStatus_TransicaoValida_RegistraEvento()
    {
        var order = NewOrder();
        order.ChangeStatus(OrderStatus.Paid, DateTime.UtcNow);
        order.Status.Should().Be(OrderStatus.Paid);
        order.Events.Should().Contain(e => e.Type == OrderEventType.StatusChanged);
        order.Events.Should().Contain(e => e.Type == OrderEventType.PaymentReceived);
    }

    [Fact]
    public void ChangeStatus_TransicaoInvalida_Lanca()
    {
        var order = NewOrder();
        var act = () => order.ChangeStatus(OrderStatus.Delivered, DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped, false)]
    public void CanTransition_SegueOFluxo(OrderStatus from, OrderStatus to, bool expected)
        => OrderStatusRules.CanTransition(from, to).Should().Be(expected);
}
