using DevSource.Dispatcher.Engine;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class AsyncGeneratedDispatchTests
{
    [Fact]
    public async Task Command_GeneratedCompletesAsynchronouslyAndHandles_ShouldNotRunRuntimePipeline()
    {
        var generated = new TestGeneratedDispatcher { HandleCommandWithoutResponse = true, RunAsynchronously = true };
        var dispatcher = new CommandDispatcher(new StubRequestHandlerResolver(), generated);

        await dispatcher.DispatchAsync(new TestCommand(new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal(1, generated.CommandWithoutResponseCalls);
    }

    [Fact]
    public async Task Command_GeneratedCompletesAsynchronouslyWithoutHandling_ShouldFallBackToRuntimePipeline()
    {
        var tracking = new TrackingState();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterCommandHandler(new TestCommandHandler());
        var dispatcher = new CommandDispatcher(resolver, new TestGeneratedDispatcher { RunAsynchronously = true });

        await dispatcher.DispatchAsync(new TestCommand(tracking), TestContext.Current.CancellationToken);

        Assert.Equal(["handler:command"], tracking.Entries);
    }

    [Fact]
    public async Task CommandWithResponse_GeneratedCompletesAsynchronouslyAndHandles_ShouldReturnGeneratedResponse()
    {
        var dispatcher = new CommandDispatcher(new StubRequestHandlerResolver(), new TestGeneratedDispatcher { HandleCommandWithResponse = true, RunAsynchronously = true });

        var response = await dispatcher.DispatchAsync<TestCommandWithResponse, string>(new TestCommandWithResponse("v", new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal("generated-response", response);
    }

    [Fact]
    public async Task CommandWithResponse_GeneratedCompletesAsynchronouslyWithoutHandling_ShouldFallBackToRuntimePipeline()
    {
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterCommandHandler(new TestCommandWithResponseHandler());
        var dispatcher = new CommandDispatcher(resolver, new TestGeneratedDispatcher { RunAsynchronously = true });

        var response = await dispatcher.DispatchAsync<TestCommandWithResponse, string>(new TestCommandWithResponse("v", new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal("handled:v", response);
    }

    [Fact]
    public async Task Query_GeneratedCompletesAsynchronouslyAndHandles_ShouldReturnGeneratedResponse()
    {
        var dispatcher = new QueryDispatcher(new StubRequestHandlerResolver(), new TestGeneratedDispatcher { HandleQuery = true, RunAsynchronously = true });

        var response = await dispatcher.DispatchAsync<TestQuery, int>(new TestQuery(2, new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal(123, response);
    }

    [Fact]
    public async Task Query_GeneratedCompletesAsynchronouslyWithoutHandling_ShouldFallBackToRuntimePipeline()
    {
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterQueryHandler(new TestQueryHandler());
        var dispatcher = new QueryDispatcher(resolver, new TestGeneratedDispatcher { RunAsynchronously = true });

        var response = await dispatcher.DispatchAsync<TestQuery, int>(new TestQuery(2, new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal(4, response);
    }

    [Fact]
    public async Task Query_GeneratedHandlesSynchronouslyWithoutHandling_ShouldFallBackToRuntimePipeline()
    {
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterQueryHandler(new TestQueryHandler());
        var dispatcher = new QueryDispatcher(resolver, new TestGeneratedDispatcher());

        var response = await dispatcher.DispatchAsync<TestQuery, int>(new TestQuery(5, new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal(10, response);
    }

    [Fact]
    public async Task Notification_GeneratedCompletesAsynchronouslyAndHandles_ShouldNotRunRuntimePipeline()
    {
        var generated = new TestGeneratedDispatcher { HandleNotification = true, RunAsynchronously = true };
        var dispatcher = new NotificationDispatcher(new StubRequestHandlerResolver(), generated);

        await dispatcher.PublishAsync(new TestNotification(new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal(1, generated.NotificationCalls);
    }

    [Fact]
    public async Task Notification_GeneratedCompletesAsynchronouslyWithoutHandling_ShouldFallBackToRuntimePipeline()
    {
        var tracking = new TrackingState();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterNotificationHandlers(new TestNotificationHandler("one"));
        var dispatcher = new NotificationDispatcher(resolver, new TestGeneratedDispatcher { RunAsynchronously = true });

        await dispatcher.PublishAsync(new TestNotification(tracking), TestContext.Current.CancellationToken);

        Assert.Equal(["notification:one"], tracking.Entries);
    }

    [Fact]
    public void DispatchAsync_WithMissingHandler_ShouldThrowSynchronously()
    {
        var dispatcher = new CommandDispatcher(new ServiceProviderRequestHandlerResolver(new ServiceCollection().BuildServiceProvider()));

        Assert.Throws<InvalidOperationException>(() => dispatcher.DispatchAsync(new TestCommand(new TrackingState()), CancellationToken.None));
    }

    [Fact]
    public async Task Command_GeneratedFaults_ShouldSurfaceFaultWithoutRunningRuntimePipeline()
    {
        var dispatcher = new CommandDispatcher(new StubRequestHandlerResolver(), new TestGeneratedDispatcher { Fault = true });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dispatcher.DispatchAsync(new TestCommand(new TrackingState()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CommandWithResponse_GeneratedFaults_ShouldSurfaceFaultWithoutRunningRuntimePipeline()
    {
        var dispatcher = new CommandDispatcher(new StubRequestHandlerResolver(), new TestGeneratedDispatcher { Fault = true });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dispatcher.DispatchAsync<TestCommandWithResponse, string>(new TestCommandWithResponse("v", new TrackingState()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Query_GeneratedFaults_ShouldSurfaceFaultWithoutRunningRuntimePipeline()
    {
        var dispatcher = new QueryDispatcher(new StubRequestHandlerResolver(), new TestGeneratedDispatcher { Fault = true });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dispatcher.DispatchAsync<TestQuery, int>(new TestQuery(1, new TrackingState()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Notification_GeneratedFaults_ShouldSurfaceFaultWithoutRunningRuntimePipeline()
    {
        var dispatcher = new NotificationDispatcher(new StubRequestHandlerResolver(), new TestGeneratedDispatcher { Fault = true });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dispatcher.PublishAsync(new TestNotification(new TrackingState()), TestContext.Current.CancellationToken));
    }
}
