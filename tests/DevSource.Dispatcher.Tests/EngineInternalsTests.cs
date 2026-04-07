using DevSource.Dispatcher.Engine;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class EngineInternalsTests
{
    [Fact]
    public async Task CommandDispatchCache_ShouldExecuteHandlerAndOrderedBehaviors()
    {
        // Arrange
        var tracking = new TrackingState();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterCommandHandler(new TestCommandHandler());
        resolver.RegisterCommandBehaviors(new OrderedCommandBehavior(2, "two"), new OrderedCommandBehavior(1, "one"));

        // Act
        await CommandDispatchCache<TestCommand>.ExecuteAsync(resolver, new TestCommand(tracking), CancellationToken.None);

        // Asserts
        Assert.Equal(["before:one", "before:two", "handler:command", "after:two", "after:one"], tracking.Entries);
    }

    [Fact]
    public async Task CommandDispatchCacheWithResponse_ShouldExecuteHandlerAndOrderedBehaviors()
    {
        // Arrange
        var tracking = new TrackingState();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterCommandHandler(new TestCommandWithResponseHandler());
        resolver.RegisterBehaviors(new OrderedResponseBehavior(2, "two"), new OrderedResponseBehavior(1, "one"));

        // Act
        var response = await CommandDispatchCache<TestCommandWithResponse, string>.ExecuteAsync(resolver, new TestCommandWithResponse("value", tracking), CancellationToken.None);

        // Asserts
        Assert.Equal("handled:value", response);
        Assert.Equal(["before:one", "before:two", "handler:command-response", "after:two", "after:one"], tracking.Entries);
    }

    [Fact]
    public async Task QueryDispatchCache_ShouldExecuteHandlerAndOrderedBehaviors()
    {
        // Arrange
        var tracking = new TrackingState();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterQueryHandler(new TestQueryHandler());
        resolver.RegisterBehaviors(new OrderedQueryBehavior(2, "two"), new OrderedQueryBehavior(1, "one"));

        // Act
        var response = await QueryDispatchCache<TestQuery, int>.ExecuteAsync(resolver, new TestQuery(10, tracking), CancellationToken.None);

        // Asserts
        Assert.Equal(20, response);
        Assert.Equal(["before:one", "before:two", "handler:query", "after:two", "after:one"], tracking.Entries);
    }

    [Fact]
    public async Task NotificationDispatchCache_ShouldExecuteAllHandlers()
    {
        // Arrange
        var tracking = new TrackingState();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterNotificationHandlers(new TestNotificationHandler("one"), new TestNotificationHandler("two"));

        // Act
        await NotificationDispatchCache<TestNotification>.ExecuteAsync(resolver, new TestNotification(tracking), CancellationToken.None);

        // Asserts
        Assert.Equal(["notification:one", "notification:two"], tracking.Entries);
    }

    [Fact]
    public void PipelineBehaviorOrderer_ShouldUseOrderThenTypeName()
    {
        // Arrange
        var ordered = PipelineBehaviorOrderer.Order<TestCommand>([new ZetaCommandBehavior(), new AlphaCommandBehavior(), new OrderedCommandBehavior(1, "ordered")]);

        // Act
        var orderedTypes = ordered.Select(static x => x.GetType().Name).ToArray();

        // Asserts
        Assert.Equal([nameof(OrderedCommandBehavior), nameof(AlphaCommandBehavior), nameof(ZetaCommandBehavior)], orderedTypes);
    }

    [Fact]
    public async Task GeneratedDispatchExecutor_ShouldRouteToCaches()
    {
        // Arrange
        var tracking = new TrackingState();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterCommandHandler(new TestCommandHandler());
        resolver.RegisterCommandHandler(new TestCommandWithResponseHandler());
        resolver.RegisterQueryHandler(new TestQueryHandler());
        resolver.RegisterNotificationHandlers(new TestNotificationHandler("one"));

        // Act
        await GeneratedDispatchExecutor.ExecuteCommandAsync(resolver, new TestCommand(tracking), TestContext.Current.CancellationToken);
        var commandResponse = await GeneratedDispatchExecutor.ExecuteCommandAsync<TestCommandWithResponse, string>(resolver, new TestCommandWithResponse("x", tracking), TestContext.Current.CancellationToken);
        var queryResponse = await GeneratedDispatchExecutor.ExecuteQueryAsync<TestQuery, int>(resolver, new TestQuery(4, tracking), TestContext.Current.CancellationToken);
        await GeneratedDispatchExecutor.PublishNotificationAsync(resolver, new TestNotification(tracking), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal("handled:x", commandResponse);
        Assert.Equal(8, queryResponse);
        Assert.Equal(["handler:command", "handler:command-response", "handler:query", "notification:one"], tracking.Entries);
    }

    [Fact]
    public void DispatchResultFactories_ShouldReturnExpectedValues()
    {
        // Arrange
        const string value = "ok";

        // Act
        var handled = DispatchResult<string>.Handled(value);
        var notHandled = DispatchResult<string>.NotHandled();

        // Asserts
        Assert.True(handled.WasHandled);
        Assert.Equal(value, handled.Response);
        Assert.False(notHandled.WasHandled);
        Assert.Null(notHandled.Response);
    }
}
