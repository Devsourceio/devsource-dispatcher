using DevSource.Dispatcher.Engine;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class PipelineBehaviorOrdererTests
{
    private static readonly TrackingState Tracking = new();

    [Fact]
    public void Order_WithoutBehaviors_ShouldReturnEmpty()
    {
        Assert.Empty(PipelineBehaviorOrderer.Order<TestCommand>([]));
        Assert.Empty(PipelineBehaviorOrderer.Order<TestCommand>(Lazy()));
    }

    [Fact]
    public void Order_WithSingleBehavior_ShouldReturnItWithoutSorting()
    {
        var behavior = new OrderedCommandBehavior(5, "only");

        var ordered = PipelineBehaviorOrderer.Order<TestCommand>(Lazy(behavior));

        Assert.Same(behavior, Assert.Single(ordered));
    }

    [Fact]
    public void Order_WithListSource_ShouldNotCopyWhenFewerThanTwo()
    {
        IPipelineBehavior<TestCommand>[] source = [new OrderedCommandBehavior(1, "one")];

        var ordered = PipelineBehaviorOrderer.Order<TestCommand>(source);

        Assert.Same(source, ordered);
    }

    [Fact]
    public async Task Order_WithLazySourceAndManyBehaviors_ShouldSortByOrderThenTypeName()
    {
        var ordered = PipelineBehaviorOrderer.Order<TestCommand>(Lazy(
            new ZetaCommandBehavior(),
            new OrderedCommandBehavior(2, "two"),
            new AlphaCommandBehavior(),
            new OrderedCommandBehavior(1, "one")));

        Assert.Equal(4, ordered.Count);
        foreach (var behavior in ordered)
            await behavior.HandleAsync(new TestCommand(Tracking), static () => ValueTask.CompletedTask, CancellationToken.None);

        Assert.Equal(["before:one", "after:one", "before:two", "after:two", "alpha", "zeta"], Tracking.Entries);
    }

    [Fact]
    public void OrderWithResponse_ShouldHandleEmptySingleAndMany()
    {
        Assert.Empty(PipelineBehaviorOrderer.Order<TestQuery, int>(Lazy2()));
        Assert.Single(PipelineBehaviorOrderer.Order<TestQuery, int>(Lazy2(new OrderedQueryBehavior(1, "one"))));

        var ordered = PipelineBehaviorOrderer.Order<TestQuery, int>(Lazy2(new OrderedQueryBehavior(2, "two"), new OrderedQueryBehavior(1, "one")));

        Assert.Equal([1, 2], ordered.Cast<IOrderedPipelineBehavior>().Select(static behavior => behavior.Order));
    }

    private static IEnumerable<IPipelineBehavior<TestCommand>> Lazy(params IPipelineBehavior<TestCommand>[] behaviors)
    {
        foreach (var behavior in behaviors)
            yield return behavior;
    }

    private static IEnumerable<IPipelineBehavior<TestQuery, int>> Lazy2(params IPipelineBehavior<TestQuery, int>[] behaviors)
    {
        foreach (var behavior in behaviors)
            yield return behavior;
    }
}
