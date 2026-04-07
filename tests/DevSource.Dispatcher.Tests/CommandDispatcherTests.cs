using Bogus;
using DevSource.Dispatcher.Engine;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class CommandDispatcherTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void Constructor_WithNullResolver_ShouldThrow()
    {
        // Arrange
        IRequestHandlerResolver? resolver = null;

        // Act
        var action = () => new DevSource.Dispatcher.Engine.CommandDispatcher(resolver!);

        // Asserts
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrow()
    {
        // Arrange
        IServiceProvider? serviceProvider = null;

        // Act
        var action = () => new DevSource.Dispatcher.Engine.CommandDispatcher(serviceProvider!);

        // Asserts
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task DispatchAsync_WithoutResponse_ShouldUseGeneratedDispatcherWhenHandled()
    {
        // Arrange
        var resolver = new Mock<IRequestHandlerResolver>(MockBehavior.Strict);
        var generatedDispatcher = new TestGeneratedDispatcher { HandleCommandWithoutResponse = true };
        var dispatcher = new DevSource.Dispatcher.Engine.CommandDispatcher(resolver.Object, generatedDispatcher);

        // Act
        await dispatcher.DispatchAsync(new TestCommand(new TrackingState()), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(1, generatedDispatcher.CommandWithoutResponseCalls);
        resolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DispatchAsync_WithoutResponse_ShouldFallbackToRuntimePipeline()
    {
        // Arrange
        var resolver = new StubRequestHandlerResolver();
        var tracking = new TrackingState();
        var generatedDispatcher = new TestGeneratedDispatcher();
        resolver.RegisterCommandHandler(new TestCommandHandler());
        resolver.RegisterCommandBehaviors(new OrderedCommandBehavior(2, "second"), new OrderedCommandBehavior(1, "first"));
        var dispatcher = new DevSource.Dispatcher.Engine.CommandDispatcher(resolver, generatedDispatcher);

        // Act
        await dispatcher.DispatchAsync(new TestCommand(tracking), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(1, generatedDispatcher.CommandWithoutResponseCalls);
        Assert.Equal(["before:first", "before:second", "handler:command", "after:second", "after:first"], tracking.Entries);
    }

    [Fact]
    public async Task DispatchAsync_WithResponse_ShouldUseGeneratedDispatcherWhenHandled()
    {
        // Arrange
        var resolver = new Mock<IRequestHandlerResolver>(MockBehavior.Strict);
        var generatedDispatcher = new TestGeneratedDispatcher { HandleCommandWithResponse = true };
        var dispatcher = new DevSource.Dispatcher.Engine.CommandDispatcher(resolver.Object, generatedDispatcher);

        // Act
        var response = await dispatcher.DispatchAsync<TestCommandWithResponse, string>(new TestCommandWithResponse(_faker.Random.Word(), new TrackingState()), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal("generated-response", response);
        Assert.Equal(1, generatedDispatcher.CommandWithResponseCalls);
        resolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DispatchAsync_WithResponse_ShouldFallbackToRuntimePipeline()
    {
        // Arrange
        var value = _faker.Random.Word();
        var tracking = new TrackingState();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterCommandHandler(new TestCommandWithResponseHandler());
        resolver.RegisterBehaviors(new OrderedResponseBehavior(2, "second"), new OrderedResponseBehavior(1, "first"));
        var dispatcher = new DevSource.Dispatcher.Engine.CommandDispatcher(resolver, new TestGeneratedDispatcher());

        // Act
        var response = await dispatcher.DispatchAsync<TestCommandWithResponse, string>(new TestCommandWithResponse(value, tracking), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal($"handled:{value}", response);
        Assert.Equal(["before:first", "before:second", "handler:command-response", "after:second", "after:first"], tracking.Entries);
    }

    [Fact]
    public async Task DispatchAsync_WhenCommandIsNull_ShouldThrow()
    {
        // Arrange
        var dispatcher = new DevSource.Dispatcher.Engine.CommandDispatcher(new StubRequestHandlerResolver());

        // Act
        Task ActAsync() => dispatcher.DispatchAsync<TestCommand>(null!).AsTask();

        // Asserts
        await Assert.ThrowsAsync<ArgumentNullException>(ActAsync);
    }

    [Fact]
    public async Task DispatchAsync_WithResponse_WhenCommandIsNull_ShouldThrow()
    {
        // Arrange
        var dispatcher = new DevSource.Dispatcher.Engine.CommandDispatcher(new StubRequestHandlerResolver());

        // Act
        Task ActAsync() => dispatcher.DispatchAsync<TestCommandWithResponse, string>(null!).AsTask();

        // Asserts
        await Assert.ThrowsAsync<ArgumentNullException>(ActAsync);
    }

    [Fact]
    public async Task ServiceProviderConstructor_ShouldDispatchThroughRuntimePipeline()
    {
        // Arrange
        var tracking = new TrackingState();
        var services = new ServiceCollection();
        services.AddTransient<DevSource.Dispatcher.Commands.ICommandHandler<TestCommand>, TestCommandHandler>();
        services.AddTransient<DevSource.Dispatcher.IPipelineBehavior<TestCommand>>(_ => new OrderedCommandBehavior(1, "runtime"));
        var dispatcher = new DevSource.Dispatcher.Engine.CommandDispatcher(services.BuildServiceProvider());

        // Act
        await dispatcher.DispatchAsync(new TestCommand(tracking), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(["before:runtime", "handler:command", "after:runtime"], tracking.Entries);
    }

    [Fact]
    public async Task UnifiedGeneratedDispatcherConstructor_ShouldUseGeneratedDispatcher()
    {
        // Arrange
        var generatedDispatcher = new TestGeneratedDispatcher { HandleCommandWithoutResponse = true };
        var dispatcher = new DevSource.Dispatcher.Engine.CommandDispatcher(new StubRequestHandlerResolver(), (DevSource.Dispatcher.Engine.IGeneratedDispatcher)generatedDispatcher);

        // Act
        await dispatcher.DispatchAsync(new TestCommand(new TrackingState()), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(1, generatedDispatcher.CommandWithoutResponseCalls);
    }

    [Fact]
    public async Task ServiceProviderAndUnifiedGeneratedDispatcherConstructor_ShouldUseGeneratedDispatcher()
    {
        // Arrange
        var generatedDispatcher = new TestGeneratedDispatcher { HandleCommandWithoutResponse = true };
        var dispatcher = new DevSource.Dispatcher.Engine.CommandDispatcher(new ServiceCollection().BuildServiceProvider(), (DevSource.Dispatcher.Engine.IGeneratedDispatcher)generatedDispatcher);

        // Act
        await dispatcher.DispatchAsync(new TestCommand(new TrackingState()), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(1, generatedDispatcher.CommandWithoutResponseCalls);
    }
}
