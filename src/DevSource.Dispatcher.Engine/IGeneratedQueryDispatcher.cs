using DevSource.Dispatcher.Queries;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Represents a generated query dispatch path preferred over runtime fallback.
/// </summary>
public interface IGeneratedQueryDispatcher
{
    /// <summary>
    /// Attempts to dispatch a query using generated code.
    /// </summary>
    ValueTask<DispatchResult<TResponse>> TryDispatchAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>;
}
