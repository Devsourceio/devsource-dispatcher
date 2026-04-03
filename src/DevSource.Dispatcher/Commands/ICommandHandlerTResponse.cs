namespace DevSource.Dispatcher.Commands;

/// <summary>
/// Handles a single command type that returns a response.
/// </summary>
/// <typeparam name="TCommand">The command type handled by this contract.</typeparam>
/// <typeparam name="TResponse">The response type produced by the command.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="command">The command instance to execute.</param>
    /// <param name="cancellationToken">The token used to cancel execution.</param>
    ValueTask<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
