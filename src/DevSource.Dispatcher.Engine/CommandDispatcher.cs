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
    public ValueTask DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (_generatedDispatcher is null)
            return CommandDispatchCache<TCommand>.ExecuteAsync(_handlerResolver, command, cancellationToken);

        var generated = _generatedDispatcher.TryDispatchAsync(command, cancellationToken);
        if (!generated.IsCompletedSuccessfully)
            return DispatchAfterGeneratedAsync(generated, command, cancellationToken);

        return generated.Result
            ? ValueTask.CompletedTask
            : CommandDispatchCache<TCommand>.ExecuteAsync(_handlerResolver, command, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command,
        CancellationToken cancellationToken = default) where TCommand : ICommand<TResponse>
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (_generatedDispatcher is null)
            return CommandDispatchCache<TCommand, TResponse>.ExecuteAsync(_handlerResolver, command, cancellationToken);

        var generated = _generatedDispatcher.TryDispatchAsync<TCommand, TResponse>(command, cancellationToken);
        if (!generated.IsCompletedSuccessfully)
            return DispatchAfterGeneratedAsync<TCommand, TResponse>(generated, command, cancellationToken);

        var result = generated.Result;
        return result.WasHandled
            ? ValueTask.FromResult(result.Response)
            : CommandDispatchCache<TCommand, TResponse>.ExecuteAsync(_handlerResolver, command, cancellationToken);
    }

    private async ValueTask DispatchAfterGeneratedAsync<TCommand>(ValueTask<bool> generated, TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        if (!await generated.ConfigureAwait(false))
            await CommandDispatchCache<TCommand>.ExecuteAsync(_handlerResolver, command, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<TResponse> DispatchAfterGeneratedAsync<TCommand, TResponse>(
        ValueTask<DispatchResult<TResponse>> generated,
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>
    {
        var result = await generated.ConfigureAwait(false);
        if (result.WasHandled)
            return result.Response;

        return await CommandDispatchCache<TCommand, TResponse>.ExecuteAsync(_handlerResolver, command, cancellationToken).ConfigureAwait(false);
    }
}
