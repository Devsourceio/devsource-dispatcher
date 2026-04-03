using DevSource.Dispatcher.Notifications;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Represents a generated notification dispatch path preferred over runtime fallback.
/// </summary>
public interface IGeneratedNotificationDispatcher
{
    /// <summary>
    /// Attempts to publish a notification using generated code.
    /// </summary>
    ValueTask<bool> TryPublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
