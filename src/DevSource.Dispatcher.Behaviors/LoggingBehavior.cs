using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DevSource.Dispatcher.Behaviors;

/// <summary>
/// Logs the start, duration, and failure of every request that returns a response (queries and commands with a response).
/// </summary>
/// <remarks>
/// Only the request type name is logged, never the request itself, because requests can carry personal data.
/// Exceptions are logged and rethrown. The behavior runs before every other ordered behavior.
/// </remarks>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="logger">The logger used to write entries.</param>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>, Engine.IOrderedPipelineBehavior
    where TRequest : notnull
{
    /// <inheritdoc />
    public int Order => int.MinValue;

    /// <inheritdoc />
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var started = Stopwatch.GetTimestamp();
        RequestLog.Handling(logger, requestType);

        try
        {
            var response = await next().ConfigureAwait(false);
            RequestLog.Handled(logger, requestType, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return response;
        }
        catch (Exception exception)
        {
            RequestLog.Failed(logger, exception, requestType, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Logs the start, duration, and failure of every command that has no response.
/// </summary>
/// <remarks>See <see cref="LoggingBehavior{TRequest, TResponse}"/> for what is and is not logged.</remarks>
/// <typeparam name="TRequest">The command type.</typeparam>
/// <param name="logger">The logger used to write entries.</param>
public sealed class LoggingCommandBehavior<TRequest>(ILogger<LoggingCommandBehavior<TRequest>> logger)
    : IPipelineBehavior<TRequest>, Engine.IOrderedPipelineBehavior
    where TRequest : notnull
{
    /// <inheritdoc />
    public int Order => int.MinValue;

    /// <inheritdoc />
    public async ValueTask HandleAsync(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var started = Stopwatch.GetTimestamp();
        RequestLog.Handling(logger, requestType);

        try
        {
            await next().ConfigureAwait(false);
            RequestLog.Handled(logger, requestType, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        catch (Exception exception)
        {
            RequestLog.Failed(logger, exception, requestType, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw;
        }
    }
}
