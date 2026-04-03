namespace DevSource.Dispatcher.Queries;

/// <summary>
/// Dispatches a query to exactly one query handler.
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>
    /// Executes the query through the configured pipeline and handler.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query being dispatched.</typeparam>
    /// <typeparam name="TResponse">The type of the response produced by the query.</typeparam>
    /// <param name="query">The query instance to dispatch.</param>
    /// <param name="cancellationToken">The token used to cancel execution.</param>
    ValueTask<TResponse> DispatchAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>;
}
