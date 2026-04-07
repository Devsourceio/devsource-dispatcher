using DevSource.Dispatcher.Commands;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Provides a caching mechanism for command dispatching operations to optimize the invocation
/// of command handlers and associated pipeline behaviors during execution. This class is
/// designed for internal use by the command dispatcher infrastructure.
/// </summary>
/// <typeparam name="TCommand">
/// The type of the command being dispatched. Must implement the <see cref="ICommand"/> interface.
/// </typeparam>
internal static class CommandDispatchCache<TCommand>
    where TCommand : ICommand
{
    public static readonly Func<IRequestHandlerResolver, TCommand, CancellationToken, ValueTask> ExecuteAsync = ExecuteCoreAsync;

    /// <summary>
    /// Executes the core logic for dispatching a command, invoking the corresponding handler and
    /// associated pipeline behaviors in the correct order to ensure proper execution of the command.
    /// </summary>
    /// <param name="handlerResolver">
    /// An instance of <see cref="IRequestHandlerResolver"/> responsible for resolving the
    /// required command handler and pipeline behaviors for the command being dispatched.
    /// </param>
    /// <param name="command">
    /// The command instance to be handled. Must implement the <see cref="ICommand"/> interface.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete. Allows the
    /// operation to be canceled if the token is signaled.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation of invoking pipeline
    /// behaviors and the command handler for the given command.
    /// </returns>
    private static ValueTask ExecuteCoreAsync(IRequestHandlerResolver handlerResolver, TCommand command,
        CancellationToken cancellationToken)
    {
        var handler = handlerResolver.GetRequiredCommandHandler<TCommand>();
        var behaviors = PipelineBehaviorOrderer.Order(handlerResolver.GetCommandBehaviors<TCommand>());

        return InvokeAsync(behaviors, handler, command,  0, cancellationToken);
    }

    /// <summary>
    /// Invokes the pipeline behaviors and command handler in the correct sequence for the specified
    /// command, ensuring proper execution and processing of the command through the defined pipeline.
    /// </summary>
    /// <param name="behaviors">
    /// The collection of pipeline behaviors to execute. Each behavior is responsible for processing
    /// the command and invoking the next step in the pipeline.
    /// </param>
    /// <param name="handler">
    /// The command handler responsible for handling the execution of the command if all
    /// pipeline behaviors have been processed.
    /// </param>
    /// <param name="command">
    /// The command instance to be processed. The command must implement the <see cref="ICommand"/> interface.
    /// </param>
    /// <param name="index">
    /// The index of the current pipeline behavior to execute. Used to determine the next behavior
    /// in the sequence during recursive invocation.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete. Allows the
    /// operation to be canceled if the token is signaled.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that represents the asynchronous operation of executing the pipeline
    /// behaviors and command handler for the provided command.
    /// </returns>
    private static ValueTask InvokeAsync(
        IReadOnlyList<IPipelineBehavior<TCommand>> behaviors,
        ICommandHandler<TCommand> handler,
        TCommand command,
        int index,
        CancellationToken cancellationToken)
    {
        if (index >= behaviors.Count)
            return handler.HandleAsync(command, cancellationToken);

        return behaviors[index].HandleAsync(
            command,
            () => InvokeAsync(behaviors, handler, command,  index + 1, cancellationToken),
            cancellationToken);
    }
}

/// <summary>
/// Provides a caching mechanism for the execution of command handlers to optimize
/// performance and reduce overhead during the dispatching process. This static class
/// encapsulates precompiled operation logic for handling commands and their responses.
/// </summary>
/// <typeparam name="TCommand">
/// The type of the command being dispatched. Must implement the <see cref="ICommand{TResponse}"/> interface.
/// </typeparam>
/// <typeparam name="TResponse">
/// The type of the response expected after the execution of the command.
/// </typeparam>
internal static class CommandDispatchCache<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    public static readonly Func<IRequestHandlerResolver, TCommand, CancellationToken, ValueTask<TResponse>> ExecuteAsync = ExecuteCoreAsync;

    /// <summary>
    /// Executes the core logic for dispatching a command by resolving the corresponding handler
    /// and processing the associated pipeline behaviors in the correct order. This method ensures
    /// the seamless execution of the command while adhering to the execution flow defined by its
    /// associated behaviors.
    /// </summary>
    /// <param name="handlerResolver">
    /// An instance of <see cref="IRequestHandlerResolver"/> responsible for resolving the required
    /// command handler and pipeline behaviors for the command being dispatched.
    /// </param>
    /// <param name="command">
    /// The instance of the command to be dispatched. Must be of type <typeparamref name="TCommand"/>
    /// and implement the <see cref="ICommand{TResponse}"/> interface.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. Enables the operation to be canceled to
    /// prevent unnecessary computation or resource usage.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation of processing
    /// the command through its associated behaviors and invoking its handler to produce a response
    /// of type <typeparamref name="TResponse"/>.
    /// </returns>
    private static ValueTask<TResponse> ExecuteCoreAsync(IRequestHandlerResolver handlerResolver, TCommand command,
        CancellationToken cancellationToken)
    {
        var handler = handlerResolver.GetRequiredCommandHandler<TCommand, TResponse>();
        var behaviors = PipelineBehaviorOrderer.Order(handlerResolver.GetBehaviors<TCommand, TResponse>());

        return InvokeAsync(behaviors, handler, command,  0, cancellationToken);
    }

    /// <summary>
    /// Coordinates the invocation of a list of pipeline behaviors and the corresponding command handler for processing a command.
    /// Executes each behavior in sequence, passing control to the next behavior or the command handler upon successful completion.
    /// </summary>
    /// <param name="behaviors">
    /// A read-only list of pipeline behaviors implementing <see cref="IPipelineBehavior{TCommand, TResponse}"/>
    /// to be executed in the specified order.
    /// </param>
    /// <param name="handler">
    /// The command handler implementing <see cref="ICommandHandler{TCommand, TResponse}"/>
    /// responsible for executing the core command logic once all behaviors are processed.
    /// </param>
    /// <param name="command">
    /// The command instance to be processed. Must implement the <see cref="ICommand{TResponse}"/> interface.
    /// </param>
    /// <param name="index">
    /// The current index of the pipeline behavior being executed, used as a recursion entry point for processing subsequent behaviors.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe for cancellation requests while the operation is in progress. It ensures proper resource cleanup and interruption.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResponse}"/> representing the asynchronous operation of executing the specified pipeline behaviors
    /// and command handler in sequence.
    /// </returns>
    private static ValueTask<TResponse> InvokeAsync(
        IReadOnlyList<IPipelineBehavior<TCommand, TResponse>> behaviors,
        ICommandHandler<TCommand, TResponse> handler,
        TCommand command,
        int index,
        CancellationToken cancellationToken)
    {
        if (index >= behaviors.Count)
            return handler.HandleAsync(command, cancellationToken);

        return behaviors[index].HandleAsync(
            command,
            () => InvokeAsync(behaviors, handler, command,  index + 1, cancellationToken),
            cancellationToken);
    }
}
