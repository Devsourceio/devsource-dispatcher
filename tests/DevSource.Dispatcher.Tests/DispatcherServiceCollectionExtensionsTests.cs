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
        services.AddSingleton<string>("auto");
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

    [Fact]
    public async Task AddDispatcherFromAssemblyContaining_ShouldRegisterHandlersAutomatically()
    {
        // Arrange
        var tracking = new TrackingState();
        var services = new ServiceCollection();
        services.AddSingleton<string>("auto");
        services.AddSingleton("auto");

        // Act
        services.AddDispatcherFromAssemblyContaining<TestCommandHandler>();
        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<DevSource.Dispatcher.Commands.ICommandDispatcher>()
            .DispatchAsync(new TestCommand(tracking), TestContext.Current.CancellationToken);
        var commandResponse = await provider.GetRequiredService<DevSource.Dispatcher.Commands.ICommandDispatcher>()
            .DispatchAsync<TestCommandWithResponse, string>(new TestCommandWithResponse("value", tracking), TestContext.Current.CancellationToken);
        var queryResponse = await provider.GetRequiredService<DevSource.Dispatcher.Queries.IQueryDispatcher>()
            .DispatchAsync<TestQuery, int>(new TestQuery(2, tracking), TestContext.Current.CancellationToken);
        await provider.GetRequiredService<DevSource.Dispatcher.Notifications.INotificationDispatcher>()
            .PublishAsync(new TestNotification(tracking), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal("handled:value", commandResponse);
        Assert.Equal(4, queryResponse);
        Assert.Collection(
            tracking.Entries,
            entry => Assert.Equal("handler:command", entry),
            entry => Assert.Equal("handler:command-response", entry),
            entry => Assert.Equal("handler:query", entry),
            entry => Assert.StartsWith("notification:", entry));
    }

    [Fact]
    public async Task AddDispatcherFromAssemblyContaining_WithGeneratedDispatcher_ShouldRegisterHandlersAutomatically()
    {
        // Arrange
        var tracking = new TrackingState();
        var services = new ServiceCollection();
        services.AddSingleton<string>("auto");

        // Act
        services.AddDispatcherFromAssemblyContaining<TestCommandHandler, SampleGeneratedDispatcher>();
        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<DevSource.Dispatcher.Commands.ICommandDispatcher>()
            .DispatchAsync(new TestCommand(tracking), TestContext.Current.CancellationToken);
        var queryResponse = await provider.GetRequiredService<DevSource.Dispatcher.Queries.IQueryDispatcher>()
            .DispatchAsync<TestQuery, int>(new TestQuery(2, tracking), TestContext.Current.CancellationToken);
        await provider.GetRequiredService<DevSource.Dispatcher.Notifications.INotificationDispatcher>()
            .PublishAsync(new TestNotification(tracking), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal(4, queryResponse);
        Assert.Collection(
            tracking.Entries,
            entry => Assert.Equal("handler:command", entry),
            entry => Assert.Equal("handler:query", entry),
            entry => Assert.StartsWith("notification:", entry));
        Assert.IsType<SampleGeneratedDispatcher>(provider.GetRequiredService<DevSource.Dispatcher.Engine.IGeneratedDispatcher>());
    }

    [Fact]
    public async Task AddDispatcherDiscovery_ShouldRegisterHandlersAutomatically()
    {
        // Arrange
        var tracking = new TrackingState();
        var services = new ServiceCollection();
        services.AddSingleton<string>("auto");

        // Act
        services.AddDispatcherDiscovery();
        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<DevSource.Dispatcher.Commands.ICommandDispatcher>()
            .DispatchAsync(new TestCommand(tracking), TestContext.Current.CancellationToken);
        var commandResponse = await provider.GetRequiredService<DevSource.Dispatcher.Commands.ICommandDispatcher>()
            .DispatchAsync<TestCommandWithResponse, string>(new TestCommandWithResponse("value", tracking), TestContext.Current.CancellationToken);
        var queryResponse = await provider.GetRequiredService<DevSource.Dispatcher.Queries.IQueryDispatcher>()
            .DispatchAsync<TestQuery, int>(new TestQuery(2, tracking), TestContext.Current.CancellationToken);
        await provider.GetRequiredService<DevSource.Dispatcher.Notifications.INotificationDispatcher>()
            .PublishAsync(new TestNotification(tracking), TestContext.Current.CancellationToken);

        // Asserts
        Assert.Equal("handled:value", commandResponse);
        Assert.Equal(4, queryResponse);
        Assert.Collection(
            tracking.Entries,
            entry => Assert.Equal("handler:command", entry),
            entry => Assert.Equal("handler:command-response", entry),
            entry => Assert.Equal("handler:query", entry),
            entry => Assert.StartsWith("notification:", entry));
    }

    [Fact]
    public void AddDispatcherDiscovery_ShouldWireGeneratedDispatcherWhenAvailable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<string>("auto");

        // Act
        services.AddDispatcherDiscovery();
        var provider = services.BuildServiceProvider();

        // Asserts
        var generatedDispatcher = provider.GetRequiredService<DevSource.Dispatcher.Engine.IGeneratedDispatcher>();
        Assert.Equal("DevSource.Dispatcher.Generated.GeneratedDispatcher", generatedDispatcher.GetType().FullName);
    }

    [Fact]
    public void AddDispatcherDiscovery_ShouldEmitReport()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<string>("auto");
        DispatcherDiscoveryReport? report = null;

        // Act
        services.AddDispatcherDiscovery(discoveryReport => report = discoveryReport);

        // Asserts
        Assert.NotNull(report);
        Assert.Equal(typeof(DispatcherServiceCollectionExtensionsTests).Assembly.GetName().Name, report!.RootAssemblyName);
        Assert.Contains(typeof(DispatcherServiceCollectionExtensionsTests).Assembly.GetName().Name, report.DiscoveredAssemblies);
        Assert.Equal("DevSource.Dispatcher.Generated.GeneratedDispatcher", report.GeneratedDispatcherTypeName);
        Assert.True(report.RegisteredCommandHandlerCount >= 2);
        Assert.True(report.RegisteredQueryHandlerCount >= 1);
        Assert.True(report.RegisteredNotificationHandlerCount >= 1);
    }

    [Fact]
    public void Register_ShouldThrowWhenSingleHandlerAlreadyRegisteredTwice()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<DevSource.Dispatcher.Commands.ICommandHandler<TestCommand>, TestCommandHandler>();
        services.AddTransient<DevSource.Dispatcher.Commands.ICommandHandler<TestCommand>>(_ => new TestCommandHandler());

        // Act
        var action = () => HandlerServiceRegistrar.Register(services, typeof(TestCommandHandler).Assembly);

        // Asserts
        var exception = Assert.Throws<InvalidOperationException>(action);
        var serviceTypeName = typeof(DevSource.Dispatcher.Commands.ICommandHandler<TestCommand>).FullName;
        Assert.NotNull(serviceTypeName);
        Assert.Contains(serviceTypeName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FindGeneratedDispatcherType_ShouldThrowWhenMultipleGeneratedDispatchersAreDiscovered()
    {
        // Arrange
        var assembly = typeof(DispatcherServiceCollectionExtensionsTests).Assembly;
        var assemblies = new[] { assembly, assembly };

        // Act
        var action = () => DispatcherDiscovery.FindGeneratedDispatcherType(assemblies);

        // Asserts
        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("Multiple generated dispatchers", exception.Message, StringComparison.Ordinal);
    }
}
