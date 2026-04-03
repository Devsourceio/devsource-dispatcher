using DevSource.Dispatcher;
using DevSource.Dispatcher.Engine;
using Microsoft.Extensions.Logging;

namespace Order.Application.Behaviors;

public sealed class CorrelationLoggingBehavior<TRequest, TResponse>(ILogger<CorrelationLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>, IOrderedPipelineBehavior
    where TRequest : notnull
{
    private readonly ILogger<CorrelationLoggingBehavior<TRequest, TResponse>> _logger = logger;

    public int Order => 100;

    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        var response = await next().ConfigureAwait(false);
        _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
        return response;
    }
}
