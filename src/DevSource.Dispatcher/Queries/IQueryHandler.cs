namespace DevSource.Dispatcher.Queries;

/// <summary>
/// Handles a single query type and returns its response.
/// </summary>
/// <typeparam name="TQuery">The query type handled by this contract.</typeparam>
/// <typeparam name="TResponse">The response type returned by the query.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Executes the query.
    /// </summary>
    /// <param name="query">The query instance to execute.</param>
    /// <param name="cancellationToken">The token used to cancel execution.</param>
    ValueTask<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
