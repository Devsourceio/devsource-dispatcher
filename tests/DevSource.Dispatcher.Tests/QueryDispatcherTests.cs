using Bogus;
using DevSource.Dispatcher.Engine;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class QueryDispatcherTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void Constructor_WithNullResolver_ShouldThrow()
    {
        // Arrange
        IRequestHandlerResolver? resolver = null;

        // Act
        var action = () => new DevSource.Dispatcher.Engine.QueryDispatcher(resolver!);

        // Asserts
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrow()
    {
        // Arrange
        IServiceProvider? serviceProvider = null;

        // Act
        var action = () => new DevSource.Dispatcher.Engine.QueryDispatcher(serviceProvider!);

        // Asserts
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task DispatchAsync_ShouldUseGeneratedDispatcherWhenHandled()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var resolver = new Mock<IRequestHandlerResolver>(MockBehavior.Strict);
        var generatedDispatcher = new TestGeneratedDispatcher { HandleQuery = true };
        var dispatcher = new DevSource.Dispatcher.Engine.QueryDispatcher(resolver.Object, generatedDispatcher);

        // Act
        var response = await dispatcher.DispatchAsync<TestQuery, int>(new TestQuery(_faker.Random.Int(), new TrackingState()), cancellationToken);

        // Asserts
        Assert.Equal(123, response);
        Assert.Equal(1, generatedDispatcher.QueryCalls);
        resolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DispatchAsync_ShouldFallbackToRuntimePipeline()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tracking = new TrackingState();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterQueryHandler(new TestQueryHandler());
        resolver.RegisterBehaviors(new OrderedQueryBehavior(2, "second"), new OrderedQueryBehavior(1, "first"));
        var dispatcher = new DevSource.Dispatcher.Engine.QueryDispatcher(resolver, new TestGeneratedDispatcher());

        // Act
        var response = await dispatcher.DispatchAsync<TestQuery, int>(new TestQuery(21, tracking), cancellationToken);

        // Asserts
        Assert.Equal(42, response);
        Assert.Equal(["before:first", "before:second", "handler:query", "after:second", "after:first"], tracking.Entries);
    }

    [Fact]
    public async Task DispatchAsync_WhenQueryIsNull_ShouldThrow()
    {
        // Arrange
        var dispatcher = new DevSource.Dispatcher.Engine.QueryDispatcher(new StubRequestHandlerResolver());

        // Act
        Task ActAsync() => dispatcher.DispatchAsync<TestQuery, int>(null!).AsTask();

        // Asserts
        await Assert.ThrowsAsync<ArgumentNullException>(ActAsync);
    }

    [Fact]
    public async Task ServiceProviderConstructor_ShouldDispatchThroughRuntimePipeline()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tracking = new TrackingState();
        var services = new ServiceCollection();
        services.AddTransient<DevSource.Dispatcher.Queries.IQueryHandler<TestQuery, int>, TestQueryHandler>();
        services.AddTransient<DevSource.Dispatcher.IPipelineBehavior<TestQuery, int>>(_ => new OrderedQueryBehavior(1, "runtime"));
        var dispatcher = new DevSource.Dispatcher.Engine.QueryDispatcher(services.BuildServiceProvider());

        // Act
        var response = await dispatcher.DispatchAsync<TestQuery, int>(new TestQuery(7, tracking), cancellationToken);

        // Asserts
        Assert.Equal(14, response);
        Assert.Equal(["before:runtime", "handler:query", "after:runtime"], tracking.Entries);
    }

    [Fact]
    public async Task UnifiedGeneratedDispatcherConstructor_ShouldUseGeneratedDispatcher()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generatedDispatcher = new TestGeneratedDispatcher { HandleQuery = true };
        var dispatcher = new DevSource.Dispatcher.Engine.QueryDispatcher(new StubRequestHandlerResolver(), (DevSource.Dispatcher.Engine.IGeneratedDispatcher)generatedDispatcher);

        // Act
        var response = await dispatcher.DispatchAsync<TestQuery, int>(new TestQuery(_faker.Random.Int(), new TrackingState()), cancellationToken);

        // Asserts
        Assert.Equal(123, response);
        Assert.Equal(1, generatedDispatcher.QueryCalls);
    }

    [Fact]
    public async Task ServiceProviderAndUnifiedGeneratedDispatcherConstructor_ShouldUseGeneratedDispatcher()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var generatedDispatcher = new TestGeneratedDispatcher { HandleQuery = true };
        var dispatcher = new DevSource.Dispatcher.Engine.QueryDispatcher(new ServiceCollection().BuildServiceProvider(), (DevSource.Dispatcher.Engine.IGeneratedDispatcher)generatedDispatcher);

        // Act
        var response = await dispatcher.DispatchAsync<TestQuery, int>(new TestQuery(_faker.Random.Int(), new TrackingState()), cancellationToken);

        // Asserts
        Assert.Equal(123, response);
        Assert.Equal(1, generatedDispatcher.QueryCalls);
    }
}
