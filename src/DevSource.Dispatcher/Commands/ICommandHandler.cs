namespace DevSource.Dispatcher.Commands;

/// <summary>
/// Handles a single command type that does not return a response.
/// </summary>
/// <typeparam name="TCommand">The command type handled by this contract.</typeparam>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="command">The command instance to execute.</param>
    /// <param name="cancellationToken">The token used to cancel execution.</param>
    ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
