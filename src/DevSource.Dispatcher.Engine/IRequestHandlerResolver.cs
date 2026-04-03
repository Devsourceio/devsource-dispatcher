using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Resolves handlers and pipeline behaviors required by the runtime dispatcher.
/// </summary>
public interface IRequestHandlerResolver
{
    /// <summary>
    /// Resolves the handler for a command without a response.
    /// </summary>
    ICommandHandler<TCommand> GetRequiredCommandHandler<TCommand>()
        where TCommand : ICommand;

    /// <summary>
    /// Resolves the handler for a command with a response.
    /// </summary>
    ICommandHandler<TCommand, TResponse> GetRequiredCommandHandler<TCommand, TResponse>()
        where TCommand : ICommand<TResponse>;

    /// <summary>
    /// Resolves the handler for a query.
    /// </summary>
    IQueryHandler<TQuery, TResponse> GetRequiredQueryHandler<TQuery, TResponse>()
        where TQuery : IQuery<TResponse>;

    /// <summary>
    /// Resolves all pipeline behaviors for a command without a response.
    /// </summary>
    IEnumerable<IPipelineBehavior<TCommand>> GetCommandBehaviors<TCommand>()
        where TCommand : ICommand;

    /// <summary>
    /// Resolves all pipeline behaviors for a request with a response.
    /// </summary>
    IEnumerable<IPipelineBehavior<TRequest, TResponse>> GetBehaviors<TRequest, TResponse>()
        where TRequest : notnull;

    /// <summary>
    /// Resolves all notification handlers for a notification.
    /// </summary>
    IEnumerable<INotificationHandler<TNotification>> GetNotificationHandlers<TNotification>()
        where TNotification : INotification;
}
