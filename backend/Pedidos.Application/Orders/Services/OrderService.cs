using Microsoft.Extensions.Logging;
using Pedidos.Application.Common;
using Pedidos.Application.Notifications;
using Pedidos.Application.Orders.Dtos;
using Pedidos.Application.Orders.Interfaces;

namespace Pedidos.Application.Orders.Services;

/// <summary>
/// Implementação dos casos de uso de Pedido. Depende apenas de abstrações
/// (repositórios do próprio slice e a porta de notificação), nunca de
/// implementações concretas nem de outro repositório que não o seu.
/// </summary>
public class OrderService : IOrderService
{
    private const int MaxPageSize = 100;

    private readonly IOrderReadRepository _readRepository;
    private readonly IOrderWriteRepository _writeRepository;
    private readonly IOrderCreatedNotifier _notifier;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderReadRepository readRepository,
        IOrderWriteRepository writeRepository,
        IOrderCreatedNotifier notifier,
        ILogger<OrderService> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _notifier = notifier;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<PagedResult<OrderListItemDto>> ListAsync(OrderListFilterDto filter, CancellationToken cancellationToken = default)
    {
        var normalized = new OrderListFilterDto
        {
            Page = filter.Page < 1 ? 1 : filter.Page,
            PageSize = filter.PageSize switch { < 1 => 20, > MaxPageSize => MaxPageSize, _ => filter.PageSize },
            Status = filter.Status,
            Search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim(),
            From = filter.From,
            To = filter.To
        };

        return _readRepository.GetPagedAsync(normalized, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _readRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Items is null || dto.Items.Count == 0)
            throw new NotFoundException("O pedido deve conter ao menos um item.");

        var created = await _writeRepository.CreateAsync(dto, cancellationToken);

        try
        {
            await _notifier.NotifyAsync(created, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao notificar criação do pedido {OrderId}. Pedido foi salvo mesmo assim.", created.Id);
        }

        return created;
    }

    /// <inheritdoc />
    public async Task<OrderDto> ChangeStatusAsync(Guid orderId, ChangeStatusDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return await _writeRepository.ChangeStatusAsync(orderId, dto.Status, dto.Note, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AppendEnrichmentAsync(Guid orderId, string description, CancellationToken cancellationToken = default)
        => _writeRepository.AppendEnrichmentAsync(orderId, description, cancellationToken);
}
