namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Represents the outcome of an optional generated dispatch path.
/// </summary>
/// <typeparam name="TResponse">The response type produced by the dispatch path.</typeparam>
public readonly record struct DispatchResult<TResponse>(bool WasHandled, TResponse Response)
{
    /// <summary>
    /// Creates a handled result.
    /// </summary>
    public static DispatchResult<TResponse> Handled(TResponse response) => new(true, response);

    /// <summary>
    /// Creates a not handled result.
    /// </summary>
    public static DispatchResult<TResponse> NotHandled() => new(false, default!);
}
