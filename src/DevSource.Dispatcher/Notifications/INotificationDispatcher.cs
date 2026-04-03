namespace DevSource.Dispatcher.Notifications;

/// <summary>
/// Represents a dispatcher responsible for publishing notifications to their corresponding handlers.
/// Notifications are broadcasted to all registered handlers for the given notification type.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Publishes a notification to all registered handlers for the notification type.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification to publish. Must implement <see cref="INotification"/>.</typeparam>
    /// <param name="notification">The notification instance to publish.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation of publishing to all handlers.</returns>
    ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}