namespace DevSource.Dispatcher.Queries;

/// <summary>
/// Represents a query that reads application data and returns a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response returned by the query.</typeparam>
public interface IQuery<TResponse>;
