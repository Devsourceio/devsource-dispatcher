using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;

namespace DevSource.Dispatcher;

/// <summary>
/// Provides a unified API over command, query, and notification dispatching.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a command to exactly one command handler.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command to send.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The token used to cancel execution.</param>
    ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand;

    /// <summary>
    /// Sends a command to exactly one command handler and returns its response.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command to send.</typeparam>
    /// <typeparam name="TResponse">The type of the response returned by the command.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The token used to cancel execution.</param>
    ValueTask<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>;

    /// <summary>
    /// Sends a query to exactly one query handler and returns its response.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query to send.</typeparam>
    /// <typeparam name="TResponse">The type of the response returned by the query.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token used to cancel execution.</param>
    ValueTask<TResponse> QueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>;

    /// <summary>
    /// Publishes a notification to all registered notification handlers.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification to publish.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">The token used to cancel execution.</param>
    ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
