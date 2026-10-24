using DevSource.Dispatcher.Queries;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Dispatches queries using a generated path when available and falls back to runtime resolution.
/// </summary>
public class QueryDispatcher : IQueryDispatcher
{
    private readonly IGeneratedQueryDispatcher? _generatedDispatcher;
    private readonly IRequestHandlerResolver _handlerResolver;

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an explicit runtime resolver.
    /// </summary>
    public QueryDispatcher(IRequestHandlerResolver handlerResolver, IGeneratedQueryDispatcher? generatedDispatcher = null)
    {
        _handlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));
        _generatedDispatcher = generatedDispatcher;
    }

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an <see cref="IServiceProvider"/>.
    /// </summary>
    public QueryDispatcher(IServiceProvider serviceProvider, IGeneratedQueryDispatcher? generatedDispatcher = null)
        : this(new ServiceProviderRequestHandlerResolver(serviceProvider), generatedDispatcher)
    {
    }

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an explicit runtime resolver and a unified generated dispatcher.
    /// </summary>
    public QueryDispatcher(IRequestHandlerResolver handlerResolver, IGeneratedDispatcher generatedDispatcher)
        : this(handlerResolver, (IGeneratedQueryDispatcher)generatedDispatcher)
    {
    }

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an <see cref="IServiceProvider"/> and a unified generated dispatcher.
    /// </summary>
    public QueryDispatcher(IServiceProvider serviceProvider, IGeneratedDispatcher generatedDispatcher)
        : this(serviceProvider, (IGeneratedQueryDispatcher)generatedDispatcher)
    {
    }

    /// <inheritdoc />
    public ValueTask<TResponse> DispatchAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery<TResponse>
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        if (_generatedDispatcher is null)
            return QueryDispatchCache<TQuery, TResponse>.ExecuteAsync(_handlerResolver, query, cancellationToken);

        var generated = _generatedDispatcher.TryDispatchAsync<TQuery, TResponse>(query, cancellationToken);
        if (!generated.IsCompletedSuccessfully)
            return DispatchAfterGeneratedAsync(generated, query, cancellationToken);

        var result = generated.Result;
        return result.WasHandled
            ? ValueTask.FromResult(result.Response)
            : QueryDispatchCache<TQuery, TResponse>.ExecuteAsync(_handlerResolver, query, cancellationToken);
    }

    private async ValueTask<TResponse> DispatchAfterGeneratedAsync<TQuery, TResponse>(
        ValueTask<DispatchResult<TResponse>> generated,
        TQuery query,
        CancellationToken cancellationToken)
        where TQuery : IQuery<TResponse>
    {
        var result = await generated.ConfigureAwait(false);
        if (result.WasHandled)
            return result.Response;

        return await QueryDispatchCache<TQuery, TResponse>.ExecuteAsync(_handlerResolver, query, cancellationToken).ConfigureAwait(false);
    }
}
