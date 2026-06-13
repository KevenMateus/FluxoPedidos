using Pedidos.Domain.Common;

namespace Pedidos.Domain.Entities;

/// <summary>
/// Raiz de agregado de Pedido. Concentra as regras de negócio: um pedido precisa
/// de ao menos um item, seu total é sempre derivado dos itens, as transições de
/// status seguem um fluxo válido, e toda mudança relevante registra um evento na
/// linha do tempo (histórico auditável).
/// </summary>
public class Order : Entity
{
    private readonly List<OrderItem> _items = new();
    private readonly List<OrderEvent> _events = new();

    /// <summary>Cliente dono do pedido.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>Data de criação do pedido (UTC). Base para o faturamento por período.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Situação atual do pedido.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>Forma de pagamento escolhida.</summary>
    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>Observação livre informada na criação (opcional).</summary>
    public string? Notes { get; private set; }

    /// <summary>Itens do pedido (somente leitura para o mundo externo).</summary>
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    /// <summary>Linha do tempo de eventos do pedido.</summary>
    public IReadOnlyCollection<OrderEvent> Events => _events.AsReadOnly();

    /// <summary>Total do pedido, sempre derivado da soma das linhas.</summary>
    public decimal Total => _items.Sum(i => i.LineTotal);

    private Order() { }

    private Order(Guid customerId, DateTime createdAtUtc, OrderStatus status, PaymentMethod paymentMethod, string? notes)
    {
        CustomerId = customerId;
        CreatedAt = createdAtUtc;
        Status = status;
        PaymentMethod = paymentMethod;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    /// <summary>
    /// Cria um novo pedido. O status inicial só pode ser Pendente ou Pago; os
    /// demais são alcançados por transição. Registra os eventos iniciais da timeline.
    /// </summary>
    public static Order Create(
        Guid customerId,
        DateTime createdAtUtc,
        IEnumerable<OrderItem> items,
        OrderStatus status,
        PaymentMethod paymentMethod,
        string? notes)
    {
        if (status is not (OrderStatus.Pending or OrderStatus.Paid))
            throw new InvalidOperationException("Um pedido só pode ser criado como Pendente ou Pago.");

        var order = new Order(customerId, createdAtUtc, status, paymentMethod, notes);
        foreach (var item in items)
            order.AddItem(item);

        if (order._items.Count == 0)
            throw new InvalidOperationException("Um pedido precisa de ao menos um item.");

        order.Raise(OrderEventType.Created, "Pedido criado.", "system", createdAtUtc);

        if (status == OrderStatus.Paid)
            order.Raise(OrderEventType.PaymentReceived, $"Pagamento confirmado ({PaymentLabel(paymentMethod)}).", "system", createdAtUtc);

        if (order.Notes is not null)
            order.Raise(OrderEventType.NoteAdded, $"Observação: {order.Notes}", "user", createdAtUtc);

        return order;
    }

    /// <summary>
    /// Altera o status seguindo as transições válidas e registra o(s) evento(s)
    /// correspondente(s) na timeline.
    /// </summary>
    public void ChangeStatus(OrderStatus newStatus, DateTime occurredAtUtc, string? note = null)
    {
        if (!OrderStatusRules.CanTransition(Status, newStatus))
            throw new InvalidOperationException($"Transição de status inválida: {StatusLabel(Status)} → {StatusLabel(newStatus)}.");

        var previous = Status;
        Status = newStatus;

        Raise(OrderEventType.StatusChanged, $"Status alterado de {StatusLabel(previous)} para {StatusLabel(newStatus)}.", "user", occurredAtUtc);

        if (newStatus == OrderStatus.Paid)
            Raise(OrderEventType.PaymentReceived, $"Pagamento confirmado ({PaymentLabel(PaymentMethod)}).", "system", occurredAtUtc);

        if (!string.IsNullOrWhiteSpace(note))
            Raise(OrderEventType.NoteAdded, $"Observação: {note.Trim()}", "user", occurredAtUtc);
    }

    /// <summary>Adiciona um evento de enriquecimento (gerado pelo microserviço).</summary>
    public void AddEnrichment(string description, DateTime occurredAtUtc)
        => Raise(OrderEventType.Enriched, description, "service", occurredAtUtc);

    /// <summary>Adiciona um item ao pedido, vinculando-o a esta raiz de agregado.</summary>
    public void AddItem(OrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.AttachToOrder(Id);
        _items.Add(item);
    }

    private void Raise(OrderEventType type, string description, string source, DateTime occurredAtUtc)
    {
        var evt = new OrderEvent(type, description, source, occurredAtUtc);
        evt.AttachToOrder(Id);
        _events.Add(evt);
    }

    /// <summary>Rótulo PT-BR do status.</summary>
    public static string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pendente",
        OrderStatus.Paid => "Pago",
        OrderStatus.Shipped => "Enviado",
        OrderStatus.Delivered => "Entregue",
        OrderStatus.Cancelled => "Cancelado",
        _ => status.ToString()
    };

    /// <summary>Rótulo PT-BR da forma de pagamento.</summary>
    public static string PaymentLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Pix => "PIX",
        PaymentMethod.Boleto => "Boleto",
        PaymentMethod.CreditCard => "Cartão de crédito",
        PaymentMethod.Cash => "Dinheiro",
        _ => method.ToString()
    };
}
