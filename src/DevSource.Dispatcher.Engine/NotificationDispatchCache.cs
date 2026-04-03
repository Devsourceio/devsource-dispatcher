using DevSource.Dispatcher.Notifications;

namespace DevSource.Dispatcher.Engine;

internal static class NotificationDispatchCache<TNotification>
    where TNotification : INotification
{
    public static readonly Func<IRequestHandlerResolver, TNotification, CancellationToken, ValueTask> ExecuteAsync = ExecuteCoreAsync;

    private static async ValueTask ExecuteCoreAsync(IRequestHandlerResolver handlerResolver, TNotification notification, CancellationToken cancellationToken)
    {
        var handlers = handlerResolver.GetNotificationHandlers<TNotification>();

        foreach (var handler in handlers)
            await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);
    }
}
