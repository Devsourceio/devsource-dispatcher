using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Represents a mediator that orchestrates the sending of commands, querying of results, and publishing of notifications.
/// Provides methods to dispatch commands, queries, and notifications through appropriate dispatchers.
/// </summary>
public class Mediator(
    ICommandDispatcher commandDispatcher,
    IQueryDispatcher queryDispatcher,
    INotificationDispatcher notificationDispatcher) : IMediator
{
    private readonly ICommandDispatcher _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
    private readonly IQueryDispatcher _queryDispatcher = queryDispatcher ?? throw new ArgumentNullException(nameof(queryDispatcher));
    private readonly INotificationDispatcher _notificationDispatcher = notificationDispatcher ?? throw new ArgumentNullException(nameof(notificationDispatcher));

    /// <inheritdoc />
    public ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        => _commandDispatcher.DispatchAsync(command, cancellationToken);

    /// <inheritdoc />
    public ValueTask<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
        => _commandDispatcher.DispatchAsync<TCommand, TResponse>(command, cancellationToken);

    /// <inheritdoc />
    public ValueTask<TResponse> QueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
        => _queryDispatcher.DispatchAsync<TQuery, TResponse>(query, cancellationToken);

    /// <inheritdoc />
    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => _notificationDispatcher.PublishAsync(notification, cancellationToken);

    /// <inheritdoc />
    public ValueTask<TResponse> PublishAsync<TNotification, TResponse>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification<TResponse>
        => _notificationDispatcher.PublishAsync<TNotification, TResponse>(notification, cancellationToken);
}
