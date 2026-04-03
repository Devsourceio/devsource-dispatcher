namespace DevSource.Dispatcher;

/// <summary>
/// Defines a behavior that wraps the execution of a command without a response.
/// </summary>
/// <typeparam name="TRequest">The request type flowing through the pipeline.</typeparam>
public interface IPipelineBehavior<in TRequest> where TRequest : notnull
{
    /// <summary>
    /// Executes logic before or after the next step in the pipeline.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The next step in the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel execution.</param>
    ValueTask HandleAsync(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken);
}
