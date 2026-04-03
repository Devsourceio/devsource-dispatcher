namespace DevSource.Dispatcher.Engine;

internal static class PipelineBehaviorOrderer
{
    public static IPipelineBehavior<TRequest>[] Order<TRequest>(IEnumerable<IPipelineBehavior<TRequest>> behaviors)
        where TRequest : notnull
        => behaviors
            .OrderBy(GetOrder)
            .ThenBy(static behavior => behavior.GetType().FullName, StringComparer.Ordinal)
            .ToArray();

    public static IPipelineBehavior<TRequest, TResponse>[] Order<TRequest, TResponse>(IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
        where TRequest : notnull
        => behaviors
            .OrderBy(GetOrder)
            .ThenBy(static behavior => behavior.GetType().FullName, StringComparer.Ordinal)
            .ToArray();

    private static int GetOrder(object behavior)
        => behavior is IOrderedPipelineBehavior orderedBehavior
            ? orderedBehavior.Order
            : int.MaxValue;
}
