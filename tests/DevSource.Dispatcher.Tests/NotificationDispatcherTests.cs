using DevSource.Dispatcher.Engine;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class NotificationDispatcherTests
{
    [Fact]
    public void Constructor_WithNullResolver_ShouldThrow()
    {
        // Arrange
        IRequestHandlerResolver? resolver = null;

        // Act
        var action = () => new DevSource.Dispatcher.Engine.NotificationDispatcher(resolver!);

        // Asserts
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrow()
    {
        // Arrange
        IServiceProvider? serviceProvider = null;

        // Act
        var action = () => new DevSource.Dispatcher.Engine.NotificationDispatcher(serviceProvider!);

        // Asserts
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task PublishAsync_ShouldUseGeneratedDispatcherWhenHandled()
    {
        // Arrange
        var resolver = new Mock<IRequestHandlerResolver>(MockBehavior.Strict);
        var generatedDispatcher = new TestGeneratedDispatcher { HandleNotification = true };
        var dispatcher = new DevSource.Dispatcher.Engine.NotificationDispatcher(resolver.Object, generatedDispatcher);

        // Act
        await dispatcher.PublishAsync(new TestNotification(new TrackingState()), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(1, generatedDispatcher.NotificationCalls);
        resolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishAsync_ShouldFallbackToRuntimeHandlers()
    {
        // Arrange
        var tracking = new TrackingState();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterNotificationHandlers(new TestNotificationHandler("one"), new TestNotificationHandler("two"));
        var dispatcher = new DevSource.Dispatcher.Engine.NotificationDispatcher(resolver, new TestGeneratedDispatcher());

        // Act
        await dispatcher.PublishAsync(new TestNotification(tracking), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(["notification:one", "notification:two"], tracking.Entries);
    }

    [Fact]
    public async Task PublishAsync_WhenNotificationIsNull_ShouldThrow()
    {
        // Arrange
        var dispatcher = new DevSource.Dispatcher.Engine.NotificationDispatcher(new StubRequestHandlerResolver());

        // Act
        Task ActAsync() => dispatcher.PublishAsync<TestNotification>(null!).AsTask();

        // Asserts
        await Assert.ThrowsAsync<ArgumentNullException>(ActAsync);
    }

    [Fact]
    public async Task ServiceProviderConstructor_ShouldPublishThroughRuntimeHandlers()
    {
        // Arrange
        var tracking = new TrackingState();
        var services = new ServiceCollection();
        services.AddTransient<DevSource.Dispatcher.Notifications.INotificationHandler<TestNotification>>(_ => new TestNotificationHandler("runtime"));
        var dispatcher = new DevSource.Dispatcher.Engine.NotificationDispatcher(services.BuildServiceProvider());

        // Act
        await dispatcher.PublishAsync(new TestNotification(tracking), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(["notification:runtime"], tracking.Entries);
    }

    [Fact]
    public async Task PublishAsyncWithResponse_ShouldInvokeAllHandlersAndReturnLastResult()
    {
        // Arrange
        var tracking = new TrackingState();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterNotificationHandlers(
            new TestNotificationWithResponseHandler("one"),
            new TestNotificationWithResponseHandler("two"));
        var dispatcher = new DevSource.Dispatcher.Engine.NotificationDispatcher(resolver);

        // Act
        var result = await dispatcher.PublishAsync<TestNotificationWithResponse, string>(
            new TestNotificationWithResponse(tracking),
            TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal("handled:two", result);
        Assert.Equal(["notification-response:one", "notification-response:two"], tracking.Entries);
    }

    [Fact]
    public async Task PublishAsyncWithResponse_WhenNotificationIsNull_ShouldThrow()
    {
        // Arrange
        var dispatcher = new DevSource.Dispatcher.Engine.NotificationDispatcher(new StubRequestHandlerResolver());

        // Act
        Task ActAsync() => dispatcher.PublishAsync<TestNotificationWithResponse, string>(null!).AsTask();

        // Asserts
        await Assert.ThrowsAsync<ArgumentNullException>(ActAsync);
    }

    [Fact]
    public async Task UnifiedGeneratedDispatcherConstructor_ShouldUseGeneratedDispatcher()
    {
        // Arrange
        var generatedDispatcher = new TestGeneratedDispatcher { HandleNotification = true };
        var dispatcher = new DevSource.Dispatcher.Engine.NotificationDispatcher(new StubRequestHandlerResolver(), (DevSource.Dispatcher.Engine.IGeneratedDispatcher)generatedDispatcher);

        // Act
        await dispatcher.PublishAsync(new TestNotification(new TrackingState()), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(1, generatedDispatcher.NotificationCalls);
    }

    [Fact]
    public async Task ServiceProviderAndUnifiedGeneratedDispatcherConstructor_ShouldUseGeneratedDispatcher()
    {
        // Arrange
        var generatedDispatcher = new TestGeneratedDispatcher { HandleNotification = true };
        var dispatcher = new DevSource.Dispatcher.Engine.NotificationDispatcher(new ServiceCollection().BuildServiceProvider(), (DevSource.Dispatcher.Engine.IGeneratedDispatcher)generatedDispatcher);

        // Act
        await dispatcher.PublishAsync(new TestNotification(new TrackingState()), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(1, generatedDispatcher.NotificationCalls);
    }
}
