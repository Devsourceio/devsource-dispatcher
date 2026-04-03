namespace DevSource.Dispatcher.Notifications;

/// <summary>
/// Defines a contract for handling a notification of type <typeparamref name="TNotification"/>.
/// </summary>
/// <typeparam name="TNotification">
/// The type of the notification to be handled, which must implement the <see cref="INotification"/> interface.
/// </typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>
    /// Handles the processing of a notification asynchronously.
    /// </summary>
    /// <param name="notification">The notification instance containing the information needed for processing.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to receive notice of cancellation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}