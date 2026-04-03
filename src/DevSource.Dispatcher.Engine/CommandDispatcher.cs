using DevSource.Dispatcher.Commands;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Dispatches commands using a generated path when available and falls back to runtime resolution.
/// </summary>
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IGeneratedCommandDispatcher? _generatedDispatcher;
    private readonly IRequestHandlerResolver _handlerResolver;

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an explicit runtime resolver.
    /// </summary>
    public CommandDispatcher(IRequestHandlerResolver handlerResolver, IGeneratedCommandDispatcher? generatedDispatcher = null)
    {
        _handlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));
        _generatedDispatcher = generatedDispatcher;
    }

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an <see cref="IServiceProvider"/>.
    /// </summary>
    public CommandDispatcher(IServiceProvider serviceProvider, IGeneratedCommandDispatcher? generatedDispatcher = null)
        : this(new ServiceProviderRequestHandlerResolver(serviceProvider), generatedDispatcher)
    {
    }

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an explicit runtime resolver and a unified generated dispatcher.
    /// </summary>
    public CommandDispatcher(IRequestHandlerResolver handlerResolver, IGeneratedDispatcher generatedDispatcher)
        : this(handlerResolver, (IGeneratedCommandDispatcher)generatedDispatcher)
    {
    }

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an <see cref="IServiceProvider"/> and a unified generated dispatcher.
    /// </summary>
    public CommandDispatcher(IServiceProvider serviceProvider, IGeneratedDispatcher generatedDispatcher)
        : this(serviceProvider, (IGeneratedCommandDispatcher)generatedDispatcher)
    {
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (_generatedDispatcher is not null)
        {
            var wasHandled = await _generatedDispatcher.TryDispatchAsync(command, cancellationToken).ConfigureAwait(false);
            if (wasHandled)
                return;
        }

        await CommandDispatchCache<TCommand>.ExecuteAsync(_handlerResolver, command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command,
        CancellationToken cancellationToken = default) where TCommand : ICommand<TResponse>
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (_generatedDispatcher is not null)
        {
            var generatedResult = await _generatedDispatcher.TryDispatchAsync<TCommand, TResponse>(command, cancellationToken).ConfigureAwait(false);
            if (generatedResult.WasHandled)
                return generatedResult.Response;
        }

        return await CommandDispatchCache<TCommand, TResponse>.ExecuteAsync(_handlerResolver, command, cancellationToken).ConfigureAwait(false);
    }
}
