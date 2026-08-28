using DevSource.Dispatcher.Notifications;

namespace DevSource.Dispatcher.Engine;

internal static class NotificationDispatchCache<TNotification, TResult>
    where TNotification : INotification<TResult>
{
    public static readonly Func<IRequestHandlerResolver, TNotification, CancellationToken, ValueTask<TResult>> ExecuteAsync = ExecuteCoreAsync;

    private static async ValueTask<TResult> ExecuteCoreAsync(
        IRequestHandlerResolver handlerResolver,
        TNotification notification,
        CancellationToken cancellationToken)
    {
        var result = default(TResult)!;

        foreach (var handler in handlerResolver.GetNotificationHandlers<TNotification, TResult>())
            result = await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);

        return result;
    }
}
