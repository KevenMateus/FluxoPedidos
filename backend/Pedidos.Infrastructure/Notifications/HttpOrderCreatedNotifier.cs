using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Pedidos.Application.Notifications;
using Pedidos.Application.Orders.Dtos;

namespace Pedidos.Infrastructure.Notifications;

/// <summary>
/// Implementação da porta de notificação via HTTP. Faz um POST do pedido recém
/// criado para o microserviço Node. Usa <see cref="IHttpClientFactory"/> (cliente
/// nomeado "notifications") com timeout curto — a notificação é best-effort.
/// </summary>
public class HttpOrderCreatedNotifier : IOrderCreatedNotifier
{
    public const string HttpClientName = "notifications";

    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpOrderCreatedNotifier> _logger;

    public HttpOrderCreatedNotifier(HttpClient httpClient, ILogger<HttpOrderCreatedNotifier> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/events/order-created", order, cancellationToken);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Pedido {OrderId} notificado ao microserviço de notificações.", order.Id);
    }
}
