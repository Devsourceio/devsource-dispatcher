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
    public async ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        => await _commandDispatcher.DispatchAsync(command, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
        => await _commandDispatcher.DispatchAsync<TCommand, TResponse>(command, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask<TResponse> QueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
        => await _queryDispatcher.DispatchAsync<TQuery, TResponse>(query, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => await _notificationDispatcher.PublishAsync(notification, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask<TResponse> PublishAsync<TNotification, TResponse>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification<TResponse>
        => await _notificationDispatcher.PublishAsync<TNotification, TResponse>(notification, cancellationToken).ConfigureAwait(false);
}
