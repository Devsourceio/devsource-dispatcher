using DevSource.Dispatcher.Queries;

namespace DevSource.Dispatcher.Engine;

internal static class QueryDispatchCache<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public static readonly Func<IRequestHandlerResolver, TQuery, CancellationToken, ValueTask<TResponse>> ExecuteAsync = ExecuteCoreAsync;

    private static ValueTask<TResponse> ExecuteCoreAsync(IRequestHandlerResolver handlerResolver, TQuery query, CancellationToken cancellationToken)
    {
        var handler = handlerResolver.GetRequiredQueryHandler<TQuery, TResponse>();
        var behaviors = PipelineBehaviorOrderer.Order(handlerResolver.GetBehaviors<TQuery, TResponse>());

        return InvokeAsync(behaviors, handler, query, cancellationToken, 0);
    }

    private static ValueTask<TResponse> InvokeAsync(
        IReadOnlyList<IPipelineBehavior<TQuery, TResponse>> behaviors,
        IQueryHandler<TQuery, TResponse> handler,
        TQuery query,
        CancellationToken cancellationToken,
        int index)
    {
        if (index >= behaviors.Count)
            return handler.HandleAsync(query, cancellationToken);

        return behaviors[index].HandleAsync(
            query,
            () => InvokeAsync(behaviors, handler, query, cancellationToken, index + 1),
            cancellationToken);
    }
}
