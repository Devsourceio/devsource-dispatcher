using DevSource.Dispatcher.Engine;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class DispatcherServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDispatcher_WithNullServices_ShouldThrow()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var action = () => DevSource.Dispatcher.Engine.DispatcherServiceCollectionExtensions.AddDispatcher(services!);

        // Asserts
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void AddDispatcher_ShouldRegisterRuntimeServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<DevSource.Dispatcher.Commands.ICommandHandler<TestCommand>, TestCommandHandler>();
        services.AddTransient<DevSource.Dispatcher.Commands.ICommandHandler<TestCommandWithResponse, string>, TestCommandWithResponseHandler>();
        services.AddTransient<DevSource.Dispatcher.Queries.IQueryHandler<TestQuery, int>, TestQueryHandler>();
        services.AddTransient<DevSource.Dispatcher.Notifications.INotificationHandler<TestNotification>, TestNotificationHandler>(_ => new TestNotificationHandler("registered"));

        // Act
        services.AddDispatcher();
        var provider = services.BuildServiceProvider();

        // Asserts
        Assert.IsType<DevSource.Dispatcher.Engine.ServiceProviderRequestHandlerResolver>(provider.GetRequiredService<DevSource.Dispatcher.Engine.IRequestHandlerResolver>());
        Assert.IsType<DevSource.Dispatcher.Engine.CommandDispatcher>(provider.GetRequiredService<DevSource.Dispatcher.Commands.ICommandDispatcher>());
        Assert.IsType<DevSource.Dispatcher.Engine.QueryDispatcher>(provider.GetRequiredService<DevSource.Dispatcher.Queries.IQueryDispatcher>());
        Assert.IsType<DevSource.Dispatcher.Engine.NotificationDispatcher>(provider.GetRequiredService<DevSource.Dispatcher.Notifications.INotificationDispatcher>());
        Assert.IsType<DevSource.Dispatcher.Engine.Mediator>(provider.GetRequiredService<DevSource.Dispatcher.IMediator>());
    }

    [Fact]
    public void AddDispatcher_WithGeneratedDispatcher_ShouldRegisterGeneratedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<DevSource.Dispatcher.Commands.ICommandHandler<TestCommandWithResponse, string>, TestCommandWithResponseHandler>();
        services.AddTransient<DevSource.Dispatcher.Queries.IQueryHandler<TestQuery, int>, TestQueryHandler>();

        // Act
        services.AddDispatcher<SampleGeneratedDispatcher>();
        var provider = services.BuildServiceProvider();

        // Asserts
        Assert.IsType<SampleGeneratedDispatcher>(provider.GetRequiredService<DevSource.Dispatcher.Engine.IGeneratedDispatcher>());
        Assert.IsType<SampleGeneratedDispatcher>(provider.GetRequiredService<DevSource.Dispatcher.Engine.IGeneratedCommandDispatcher>());
        Assert.IsType<SampleGeneratedDispatcher>(provider.GetRequiredService<DevSource.Dispatcher.Engine.IGeneratedQueryDispatcher>());
        Assert.IsType<SampleGeneratedDispatcher>(provider.GetRequiredService<DevSource.Dispatcher.Engine.IGeneratedNotificationDispatcher>());
    }

    [Fact]
    public async Task AddDispatcher_WithGeneratedDispatcher_ShouldResolveDispatchersThatExecute()
    {
        // Arrange
        var tracking = new TrackingState();
        var services = new ServiceCollection();
        services.AddTransient<DevSource.Dispatcher.Commands.ICommandHandler<TestCommand>, TestCommandHandler>();
        services.AddTransient<DevSource.Dispatcher.Queries.IQueryHandler<TestQuery, int>, TestQueryHandler>();
        services.AddTransient<DevSource.Dispatcher.Notifications.INotificationHandler<TestNotification>>(_ => new TestNotificationHandler("registered"));
        services.AddDispatcher<SampleGeneratedDispatcher>();
        var provider = services.BuildServiceProvider();

        // Act
        await provider.GetRequiredService<DevSource.Dispatcher.Commands.ICommandDispatcher>().DispatchAsync(new TestCommand(tracking), TestContext.Current.CancellationToken);
        var queryResponse = await provider.GetRequiredService<DevSource.Dispatcher.Queries.IQueryDispatcher>().DispatchAsync<TestQuery, int>(new TestQuery(2, tracking), TestContext.Current.CancellationToken);
        await provider.GetRequiredService<DevSource.Dispatcher.Notifications.INotificationDispatcher>().PublishAsync(new TestNotification(tracking), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(4, queryResponse);
        Assert.Equal(["handler:command", "handler:query", "notification:registered"], tracking.Entries);
    }
}
