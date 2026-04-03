namespace DevSource.Dispatcher.Commands;

/// <summary>
/// Represents a dispatcher for handling and executing commands.
/// </summary>
/// <remarks>
/// ICommandDispatcher provides methods for dispatching commands to their corresponding handlers.
/// It supports both void commands and commands that produce a result.
/// Commands must implement either <see cref="ICommand"/> or <see cref="ICommand{TResult}"/>.
/// </remarks>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatches a command to its corresponding handler for execution.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command to be dispatched. The command must implement <see cref="ICommand"/>.</typeparam>
    /// <param name="command">The command instance to dispatch.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Dispatches a command to its corresponding handler and executes it asynchronously.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command to be dispatched. The command must implement <see cref="ICommand"/>.</typeparam>
    /// <typeparam name="TResult">Represents the return of the operation</typeparam>
    /// <param name="command">The command instance to dispatch.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask<TResult> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;
}