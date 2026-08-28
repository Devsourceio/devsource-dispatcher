namespace DevSource.Dispatcher.Notifications;

/// <summary>
/// Defines a handler for processing notifications with a specific return type.
/// Handlers implement this interface to provide logic for processing instances
/// of a notification that produces a corresponding response.
/// </summary>
/// <typeparam name="TNotification">The type of the notification to handle, constrained to implement <see cref="INotification{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by handling the notification.</typeparam>
public interface INotificationHandler<in TNotification, TResponse> where TNotification : INotification<TResponse>
{
    /// <summary>
    /// Processes a notification and produces a corresponding response.
    /// </summary>
    /// <param name="notification">The notification instance to be processed.</param>
    /// <param name="cancellationToken">The cancellation token that may be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response produced by handling the notification.</returns>
    ValueTask<TResponse> HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}
