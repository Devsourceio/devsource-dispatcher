using DevSource.Dispatcher.Commands;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Represents a generated command dispatch path preferred over runtime fallback.
/// </summary>
public interface IGeneratedCommandDispatcher
{
    /// <summary>
    /// Attempts to dispatch a command without a response using generated code.
    /// </summary>
    ValueTask<bool> TryDispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Attempts to dispatch a command with a response using generated code.
    /// </summary>
    ValueTask<DispatchResult<TResponse>> TryDispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>;
}
