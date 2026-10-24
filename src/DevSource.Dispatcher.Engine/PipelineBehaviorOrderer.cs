namespace DevSource.Dispatcher.Engine;

internal static class PipelineBehaviorOrderer
{
    public static IReadOnlyList<IPipelineBehavior<TRequest>> Order<TRequest>(IEnumerable<IPipelineBehavior<TRequest>> behaviors)
        where TRequest : notnull
        => OrderCore(behaviors);

    public static IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> Order<TRequest, TResponse>(IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
        where TRequest : notnull
        => OrderCore(behaviors);

    // The source is enumerated once (a resolver may hand back a lazy iterator); zero or one behavior needs no sort.
    private static IReadOnlyList<T> OrderCore<T>(IEnumerable<T> behaviors)
        where T : notnull
    {
        var list = behaviors as IReadOnlyList<T> ?? behaviors.ToArray();
        if (list.Count < 2)
            return list;

        return list
            .OrderBy(static behavior => GetOrder(behavior))
            .ThenBy(static behavior => behavior.GetType().FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static int GetOrder(object behavior)
        => behavior is IOrderedPipelineBehavior orderedBehavior
            ? orderedBehavior.Order
            : int.MaxValue;
}
