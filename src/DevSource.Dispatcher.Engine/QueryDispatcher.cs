using DevSource.Dispatcher;
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
    public async ValueTask<TResponse> DispatchAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery<TResponse>
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        if (_generatedDispatcher is not null)
        {
            var generatedResult = await _generatedDispatcher.TryDispatchAsync<TQuery, TResponse>(query, cancellationToken).ConfigureAwait(false);
            if (generatedResult.WasHandled)
                return generatedResult.Response;
        }

        return await QueryDispatchCache<TQuery, TResponse>.ExecuteAsync(_handlerResolver, query, cancellationToken).ConfigureAwait(false);
    }
}
