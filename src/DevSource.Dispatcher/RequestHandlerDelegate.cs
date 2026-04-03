namespace DevSource.Dispatcher;

/// <summary>
/// Represents the continuation for the next step of a pipeline without a response.
/// </summary>
public delegate ValueTask RequestHandlerDelegate();
