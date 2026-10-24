using DevSource.Dispatcher.Engine;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public abstract class AbstractGeneratedDispatcherBase : IGeneratedDispatcher
{
    ValueTask<bool> IGeneratedCommandDispatcher.TryDispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        => ValueTask.FromResult(false);

    ValueTask<DispatchResult<TResponse>> IGeneratedCommandDispatcher.TryDispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
        => ValueTask.FromResult(DispatchResult<TResponse>.NotHandled());

    ValueTask<DispatchResult<TResponse>> IGeneratedQueryDispatcher.TryDispatchAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
        => ValueTask.FromResult(DispatchResult<TResponse>.NotHandled());

    ValueTask<bool> IGeneratedNotificationDispatcher.TryPublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        => ValueTask.FromResult(false);
}

public sealed class DispatcherGeneratedRegistrationTests
{
    [Fact]
    public void Register_WithNullServices_ShouldThrow()
    {
        var action = () => DispatcherGeneratedRegistration.Register(null!, typeof(SampleGeneratedDispatcher));

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Register_WithNullType_ShouldThrow()
    {
        var action = () => DispatcherGeneratedRegistration.Register(new ServiceCollection(), null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(AbstractGeneratedDispatcherBase))]
    public void Register_WithInvalidType_ShouldThrow(Type type)
    {
        var action = () => DispatcherGeneratedRegistration.Register(new ServiceCollection(), type);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Contains(type.FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Register_WithServiceProviderConstructor_ShouldCreateInstance()
    {
        var provider = BuildProvider<ServiceProviderConstructorDispatcher>();

        var dispatcher = Assert.IsType<ServiceProviderConstructorDispatcher>(provider.GetRequiredService<IGeneratedDispatcher>());
        Assert.NotNull(dispatcher.ServiceProvider);
    }

    [Fact]
    public void Register_WithParameterlessConstructor_ShouldCreateInstance()
    {
        var provider = BuildProvider<ParameterlessDispatcher>();

        Assert.IsType<ParameterlessDispatcher>(provider.GetRequiredService<IGeneratedDispatcher>());
    }

    [Fact]
    public void Register_WithoutSupportedConstructor_ShouldThrowWhenResolved()
    {
        var provider = BuildProvider<UnsupportedConstructorDispatcher>();

        var action = () => provider.GetRequiredService<IGeneratedDispatcher>();

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains(typeof(UnsupportedConstructorDispatcher).FullName!, exception.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider BuildProvider<TDispatcher>()
        where TDispatcher : class, IGeneratedDispatcher
    {
        var services = new ServiceCollection();
        DispatcherGeneratedRegistration.Register(services, typeof(TDispatcher));
        return services.BuildServiceProvider();
    }


    public sealed class ServiceProviderConstructorDispatcher(IServiceProvider serviceProvider) : AbstractGeneratedDispatcherBase
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;
    }

    public sealed class ParameterlessDispatcher : AbstractGeneratedDispatcherBase;

    public sealed class UnsupportedConstructorDispatcher(string unsupported) : AbstractGeneratedDispatcherBase
    {
        public string Unsupported { get; } = unsupported;
    }
}
