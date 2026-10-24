namespace DevSource.Dispatcher.Behaviors;

/// <summary>
/// Describes one validation failure.
/// </summary>
/// <param name="PropertyName">The name of the invalid property, or an empty string for a request-level failure.</param>
/// <param name="Message">The human-readable failure message.</param>
public sealed record ValidationError(string PropertyName, string Message);

/// <summary>
/// Validates requests of type <typeparamref name="TRequest"/> before they reach their handler.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
public interface IRequestValidator<in TRequest>
{
    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="cancellationToken">The token used to cancel validation.</param>
    /// <returns>The failures found, or an empty list when the request is valid.</returns>
    ValueTask<IReadOnlyList<ValidationError>> ValidateAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Thrown by the validation behaviors when at least one validator reports a failure.
/// </summary>
public sealed class RequestValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestValidationException"/> class.
    /// </summary>
    /// <param name="errors">Every failure reported by every validator.</param>
    public RequestValidationException(IReadOnlyList<ValidationError> errors)
        : base($"Request validation failed with {errors.Count} error(s).")
    {
        Errors = errors;
    }

    /// <summary>
    /// Gets every failure reported by every validator.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }
}

internal static class RequestValidator
{
    public static async ValueTask ValidateAsync<TRequest>(IEnumerable<IRequestValidator<TRequest>> validators, TRequest request, CancellationToken cancellationToken)
    {
        List<ValidationError>? errors = null;

        foreach (var validator in validators)
        {
            var found = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            if (found.Count > 0)
                (errors ??= []).AddRange(found);
        }

        if (errors is not null)
            throw new RequestValidationException(errors);
    }
}
