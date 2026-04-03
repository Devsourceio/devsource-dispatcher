namespace DevSource.Dispatcher.Commands;

/// <summary>
/// Represents a command that changes the application state and returns a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response produced by the command.</typeparam>
public interface ICommand<out TResponse> : ICommand;
