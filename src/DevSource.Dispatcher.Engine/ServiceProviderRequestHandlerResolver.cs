using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Resolves handlers and behaviors from an <see cref="IServiceProvider"/>.
/// </summary>
public sealed class ServiceProviderRequestHandlerResolver(IServiceProvider serviceProvider) : IRequestHandlerResolver
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public ICommandHandler<TCommand> GetRequiredCommandHandler<TCommand>()
        where TCommand : ICommand
        => _serviceProvider.GetService<ICommandHandler<TCommand>>()
            ?? throw new InvalidOperationException($"No handler for command of type {typeof(TCommand).Name} was found.");

    /// <inheritdoc />
    public ICommandHandler<TCommand, TResponse> GetRequiredCommandHandler<TCommand, TResponse>()
        where TCommand : ICommand<TResponse>
        => _serviceProvider.GetService<ICommandHandler<TCommand, TResponse>>()
            ?? throw new InvalidOperationException($"No handler for command of type {typeof(TCommand).Name} was found.");

    /// <inheritdoc />
    public IQueryHandler<TQuery, TResponse> GetRequiredQueryHandler<TQuery, TResponse>()
        where TQuery : IQuery<TResponse>
        => _serviceProvider.GetService<IQueryHandler<TQuery, TResponse>>()
            ?? throw new InvalidOperationException($"No handler for query of type {typeof(TQuery).Name} was found.");

    /// <inheritdoc />
    public IEnumerable<IPipelineBehavior<TCommand>> GetCommandBehaviors<TCommand>()
        where TCommand : ICommand
        => _serviceProvider.GetServices<IPipelineBehavior<TCommand>>();

    /// <inheritdoc />
    public IEnumerable<IPipelineBehavior<TRequest, TResponse>> GetBehaviors<TRequest, TResponse>()
        where TRequest : notnull
        => _serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();

    /// <inheritdoc />
    public IEnumerable<INotificationHandler<TNotification>> GetNotificationHandlers<TNotification>()
        where TNotification : INotification
        => _serviceProvider.GetServices<INotificationHandler<TNotification>>();
}
