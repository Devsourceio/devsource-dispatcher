namespace DevSource.Dispatcher.Behaviors;

/// <summary>
/// Runs every registered <see cref="IRequestValidator{TRequest}"/> before requests that return a response, and throws
/// <see cref="RequestValidationException"/> with all failures when any validator reports one.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="validators">The validators registered for the request type; may be empty.</param>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IRequestValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>, Engine.IOrderedPipelineBehavior
    where TRequest : notnull
{
    /// <inheritdoc />
    public int Order => -1000;

    /// <inheritdoc />
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        await RequestValidator.ValidateAsync(validators, request, cancellationToken).ConfigureAwait(false);
        return await next().ConfigureAwait(false);
    }
}

/// <summary>
/// Runs every registered <see cref="IRequestValidator{TRequest}"/> before commands that have no response, and throws
/// <see cref="RequestValidationException"/> with all failures when any validator reports one.
/// </summary>
/// <typeparam name="TRequest">The command type.</typeparam>
/// <param name="validators">The validators registered for the command type; may be empty.</param>
public sealed class ValidationCommandBehavior<TRequest>(IEnumerable<IRequestValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest>, Engine.IOrderedPipelineBehavior
    where TRequest : notnull
{
    /// <inheritdoc />
    public int Order => -1000;

    /// <inheritdoc />
    public async ValueTask HandleAsync(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
    {
        await RequestValidator.ValidateAsync(validators, request, cancellationToken).ConfigureAwait(false);
        await next().ConfigureAwait(false);
    }
}
