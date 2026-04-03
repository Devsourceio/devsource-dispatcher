using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class ServiceProviderRequestHandlerResolverTests
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrow()
    {
        // Arrange
        IServiceProvider? serviceProvider = null;

        // Act
        var action = () => new DevSource.Dispatcher.Engine.ServiceProviderRequestHandlerResolver(serviceProvider!);

        // Asserts
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void GetRequiredHandlers_ShouldResolveRegisteredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<DevSource.Dispatcher.Commands.ICommandHandler<TestCommand>, TestCommandHandler>();
        services.AddTransient<DevSource.Dispatcher.Commands.ICommandHandler<TestCommandWithResponse, string>, TestCommandWithResponseHandler>();
        services.AddTransient<DevSource.Dispatcher.Queries.IQueryHandler<TestQuery, int>, TestQueryHandler>();
        services.AddTransient<DevSource.Dispatcher.IPipelineBehavior<TestCommand>, OrderedCommandBehavior>(_ => new OrderedCommandBehavior(1, "one"));
        services.AddTransient<DevSource.Dispatcher.IPipelineBehavior<TestQuery, int>, OrderedQueryBehavior>(_ => new OrderedQueryBehavior(1, "one"));
        services.AddTransient<DevSource.Dispatcher.Notifications.INotificationHandler<TestNotification>, TestNotificationHandler>(_ => new TestNotificationHandler("one"));
        var resolver = new DevSource.Dispatcher.Engine.ServiceProviderRequestHandlerResolver(services.BuildServiceProvider());

        // Act
        var commandHandler = resolver.GetRequiredCommandHandler<TestCommand>();
        var commandResponseHandler = resolver.GetRequiredCommandHandler<TestCommandWithResponse, string>();
        var queryHandler = resolver.GetRequiredQueryHandler<TestQuery, int>();
        var commandBehaviors = resolver.GetCommandBehaviors<TestCommand>();
        var queryBehaviors = resolver.GetBehaviors<TestQuery, int>();
        var notificationHandlers = resolver.GetNotificationHandlers<TestNotification>();

        // Asserts
        Assert.IsType<TestCommandHandler>(commandHandler);
        Assert.IsType<TestCommandWithResponseHandler>(commandResponseHandler);
        Assert.IsType<TestQueryHandler>(queryHandler);
        Assert.Single(commandBehaviors);
        Assert.Single(queryBehaviors);
        Assert.Single(notificationHandlers);
    }

    [Fact]
    public void GetRequiredHandlers_WhenHandlerMissing_ShouldThrow()
    {
        // Arrange
        var resolver = new DevSource.Dispatcher.Engine.ServiceProviderRequestHandlerResolver(new ServiceCollection().BuildServiceProvider());

        // Act
        var commandAction = () => resolver.GetRequiredCommandHandler<TestCommand>();
        var commandResponseAction = () => resolver.GetRequiredCommandHandler<TestCommandWithResponse, string>();
        var queryAction = () => resolver.GetRequiredQueryHandler<TestQuery, int>();

        // Asserts
        Assert.Throws<InvalidOperationException>(commandAction);
        Assert.Throws<InvalidOperationException>(commandResponseAction);
        Assert.Throws<InvalidOperationException>(queryAction);
    }
}
