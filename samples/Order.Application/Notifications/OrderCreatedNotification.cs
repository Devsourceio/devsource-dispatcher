using DevSource.Dispatcher.Notifications;

namespace Order.Application.Notifications;

public sealed record OrderCreatedNotification(Guid OrderId, DateTimeOffset CreatedAtUtc) : INotification;
