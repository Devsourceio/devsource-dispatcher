using DevSource.Dispatcher.Notifications;
using Microsoft.Extensions.Logging;
using Order.Application.Notifications;

namespace Order.Application.Handlers;

public sealed class OrderCreatedHandler(ILogger<OrderCreatedHandler> logger) : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<OrderCreatedHandler> _logger = logger;

    public ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Order {OrderId} was created at {CreatedAtUtc}", notification.OrderId, notification.CreatedAtUtc);
        return ValueTask.CompletedTask;
    }
}
