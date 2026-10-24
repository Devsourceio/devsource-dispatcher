using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Queries;
using DevSource.Dispatcher.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class HandlerServiceRegisterTests
{
    private static readonly System.Reflection.Assembly TestAssembly = typeof(HandlerServiceRegisterTests).Assembly;

    [Fact]
    public void Register_WithNullServices_ShouldThrow()
    {
        var action = () => HandlerServiceRegister.Register(null!, TestAssembly);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Register_WithNullAssemblies_ShouldThrow()
    {
        var action = () => HandlerServiceRegister.Register(new ServiceCollection(), null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Register_WithNullAssemblyEntry_ShouldThrow()
    {
        var action = () => HandlerServiceRegister.Register(new ServiceCollection(), [null!]);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Register_WithoutAssemblies_ShouldThrow()
    {
        var action = () => HandlerServiceRegister.Register(new ServiceCollection());

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Register_ShouldCountRegisteredHandlers()
    {
        var services = new ServiceCollection();

        var summary = HandlerServiceRegister.Register(services, TestAssembly);

        Assert.Equal(2, summary.RegisteredCommandHandlerCount);
        Assert.Equal(1, summary.RegisteredQueryHandlerCount);
        Assert.True(summary.RegisteredNotificationHandlerCount >= 1);
    }

    [Fact]
    public void Register_Twice_ShouldNotDuplicateOrRecount()
    {
        var services = new ServiceCollection();
        HandlerServiceRegister.Register(services, TestAssembly);
        var descriptorCount = services.Count;

        var summary = HandlerServiceRegister.Register(services, TestAssembly);

        Assert.Equal(descriptorCount, services.Count);
        Assert.Equal(0, summary.RegisteredCommandHandlerCount);
        Assert.Equal(0, summary.RegisteredQueryHandlerCount);
        Assert.Equal(0, summary.RegisteredNotificationHandlerCount);
    }

    [Fact]
    public void Register_WhenFactoryHandlerAlreadyRegistered_ShouldThrowNamingFactoryRegistration()
    {
        var services = new ServiceCollection();
        services.AddTransient<ICommandHandler<TestCommand>>(_ => new TestCommandHandler());

        var action = () => HandlerServiceRegister.Register(services, TestAssembly);

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("factory/instance registration", exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(TestCommandHandler).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Register_WhenSameImplementationAlreadyRegistered_ShouldSkipIt()
    {
        var services = new ServiceCollection();
        services.AddTransient<ICommandHandler<TestCommand>, TestCommandHandler>();

        var summary = HandlerServiceRegister.Register(services, TestAssembly);

        Assert.Equal(1, summary.RegisteredCommandHandlerCount);
        Assert.Single(services, static descriptor => descriptor.ServiceType == typeof(ICommandHandler<TestCommand>));
    }

    [Fact]
    public void Register_WithUnrelatedAndPreRegisteredHandlerServices_ShouldIgnoreThemWhenMapping()
    {
        var services = new ServiceCollection();
        services.AddSingleton("text");
        services.AddSingleton<IComparable<int>>(1);
        services.AddTransient<ICommandHandler<TestCommandWithResponse, string>, TestCommandWithResponseHandler>();
        services.AddTransient<IQueryHandler<TestQuery, int>, TestQueryHandler>();

        var summary = HandlerServiceRegister.Register(services, TestAssembly);

        Assert.Equal(1, summary.RegisteredCommandHandlerCount);
        Assert.Equal(0, summary.RegisteredQueryHandlerCount);
    }

    [Fact]
    public void Register_WhenDifferentImplementationAlreadyRegistered_ShouldThrowNamingBothTypes()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(ICommandHandler<FixtureCommand>), typeof(object));

        var action = () => HandlerServiceRegister.Register(services, typeof(FixtureCommand).Assembly);

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains($"Existing: {typeof(object).FullName}", exception.Message, StringComparison.Ordinal);
        Assert.Contains($"New: {typeof(FixtureCommandHandler).FullName}", exception.Message, StringComparison.Ordinal);
    }
}
