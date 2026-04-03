namespace DevSource.Dispatcher;

/// <summary>
/// Represents the continuation for the next step of a pipeline with a response.
/// </summary>
/// <typeparam name="TResponse">The response type produced by the continuation.</typeparam>
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>();
