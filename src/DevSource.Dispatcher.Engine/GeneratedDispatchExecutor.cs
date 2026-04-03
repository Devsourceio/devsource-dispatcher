using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Exposes reusable runtime execution paths for source-generated dispatchers.
/// </summary>
public static class GeneratedDispatchExecutor
{
    /// <summary>
    /// Executes a command without a response.
    /// </summary>
    public static ValueTask ExecuteCommandAsync<TCommand>(IRequestHandlerResolver handlerResolver, TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
        => CommandDispatchCache<TCommand>.ExecuteAsync(handlerResolver, command, cancellationToken);

    /// <summary>
    /// Executes a command with a response.
    /// </summary>
    public static ValueTask<TResponse> ExecuteCommandAsync<TCommand, TResponse>(IRequestHandlerResolver handlerResolver, TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
        => CommandDispatchCache<TCommand, TResponse>.ExecuteAsync(handlerResolver, command, cancellationToken);

    /// <summary>
    /// Executes a query.
    /// </summary>
    public static ValueTask<TResponse> ExecuteQueryAsync<TQuery, TResponse>(IRequestHandlerResolver handlerResolver, TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
        => QueryDispatchCache<TQuery, TResponse>.ExecuteAsync(handlerResolver, query, cancellationToken);

    /// <summary>
    /// Publishes a notification.
    /// </summary>
    public static ValueTask PublishNotificationAsync<TNotification>(IRequestHandlerResolver handlerResolver, TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => NotificationDispatchCache<TNotification>.ExecuteAsync(handlerResolver, notification, cancellationToken);
}
