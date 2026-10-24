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

    /// <summary>
    /// Executes a command without a response on behalf of a generated dispatcher and reports it as handled.
    /// </summary>
    public static ValueTask<bool> ExecuteCommandHandledAsync<TCommand>(IRequestHandlerResolver handlerResolver, TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
        => ToHandled(CommandDispatchCache<TCommand>.ExecuteAsync(handlerResolver, command, cancellationToken));

    /// <summary>
    /// Executes a command with a response on behalf of a generated dispatcher and reports it as handled.
    /// </summary>
    /// <typeparam name="TCommand">The concrete command type known to the generated dispatcher.</typeparam>
    /// <typeparam name="TCommandResponse">The response type declared by the command.</typeparam>
    /// <typeparam name="TResponse">The response type requested by the caller of the generated dispatcher.</typeparam>
    public static ValueTask<DispatchResult<TResponse>> ExecuteCommandHandledAsync<TCommand, TCommandResponse, TResponse>(IRequestHandlerResolver handlerResolver, TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TCommandResponse>
        => ToHandled<TCommandResponse, TResponse>(CommandDispatchCache<TCommand, TCommandResponse>.ExecuteAsync(handlerResolver, command, cancellationToken));

    /// <summary>
    /// Executes a query on behalf of a generated dispatcher and reports it as handled.
    /// </summary>
    /// <typeparam name="TQuery">The concrete query type known to the generated dispatcher.</typeparam>
    /// <typeparam name="TQueryResponse">The response type declared by the query.</typeparam>
    /// <typeparam name="TResponse">The response type requested by the caller of the generated dispatcher.</typeparam>
    public static ValueTask<DispatchResult<TResponse>> ExecuteQueryHandledAsync<TQuery, TQueryResponse, TResponse>(IRequestHandlerResolver handlerResolver, TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TQueryResponse>
        => ToHandled<TQueryResponse, TResponse>(QueryDispatchCache<TQuery, TQueryResponse>.ExecuteAsync(handlerResolver, query, cancellationToken));

    /// <summary>
    /// Publishes a notification on behalf of a generated dispatcher and reports it as handled.
    /// </summary>
    public static ValueTask<bool> PublishNotificationHandledAsync<TNotification>(IRequestHandlerResolver handlerResolver, TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => ToHandled(NotificationDispatchCache<TNotification>.ExecuteAsync(handlerResolver, notification, cancellationToken));

    // The helpers below keep the synchronous path free of async state machines: only a pending task pays for one.
    private static ValueTask<bool> ToHandled(ValueTask task)
    {
        if (!task.IsCompletedSuccessfully)
            return AwaitHandledAsync(task);

        task.GetAwaiter().GetResult();
        return new ValueTask<bool>(true);
    }

    private static async ValueTask<bool> AwaitHandledAsync(ValueTask task)
    {
        await task.ConfigureAwait(false);
        return true;
    }

    private static ValueTask<DispatchResult<TResponse>> ToHandled<TSource, TResponse>(ValueTask<TSource> task)
    {
        if (!task.IsCompletedSuccessfully)
            return AwaitHandledAsync<TSource, TResponse>(task);

        return new ValueTask<DispatchResult<TResponse>>(DispatchResult<TResponse>.Handled(Cast<TSource, TResponse>(task.Result)));
    }

    private static async ValueTask<DispatchResult<TResponse>> AwaitHandledAsync<TSource, TResponse>(ValueTask<TSource> task)
        => DispatchResult<TResponse>.Handled(Cast<TSource, TResponse>(await task.ConfigureAwait(false)));

    // TSource and TResponse are the same type at runtime; the JIT removes the box/unbox pair for value types.
    private static TResponse Cast<TSource, TResponse>(TSource response)
        => (TResponse)(object)response!;
}
