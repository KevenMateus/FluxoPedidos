using Pedidos.Domain.Common;

namespace Pedidos.Domain.Entities;

/// <summary>Tipo de evento na linha do tempo de um pedido.</summary>
public enum OrderEventType
{
    /// <summary>Pedido criado.</summary>
    Created = 0,

    /// <summary>Mudança de status.</summary>
    StatusChanged = 1,

    /// <summary>Pagamento confirmado.</summary>
    PaymentReceived = 2,

    /// <summary>Observação registrada.</summary>
    NoteAdded = 3,

    /// <summary>Enriquecimento gerado pelo microserviço (faixa/risco etc.).</summary>
    Enriched = 4
}

/// <summary>
/// Evento da linha do tempo (histórico) de um pedido. Cada mudança relevante —
/// criação, pagamento, troca de status, observação, enriquecimento — vira um
/// evento imutável, o que dá o histórico auditável exibido ao expandir o pedido.
/// </summary>
public class OrderEvent : Entity
{
    /// <summary>Pedido ao qual o evento pertence.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>Tipo do evento.</summary>
    public OrderEventType Type { get; private set; }

    /// <summary>Descrição legível do evento.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Origem do evento: "system", "user" ou "service" (microserviço).</summary>
    public string Source { get; private set; } = "system";

    /// <summary>Momento em que o evento ocorreu (UTC).</summary>
    public DateTime OccurredAt { get; private set; }

    private OrderEvent() { }

    public OrderEvent(OrderEventType type, string description, string source, DateTime occurredAtUtc)
    {
        Type = type;
        Description = description;
        Source = source;
        OccurredAt = occurredAtUtc;
    }

    /// <summary>Vincula o evento à sua raiz de agregado. Uso interno do domínio.</summary>
    internal void AttachToOrder(Guid orderId) => OrderId = orderId;
}
