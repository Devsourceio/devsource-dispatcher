using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Queries;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class DispatcherBehaviorRegistrationTests
{
    [Fact]
    public void AddDispatcherBehavior_WithNullServices_ShouldThrow()
    {
        var action = () => DispatcherServiceCollectionExtensions.AddDispatcherBehavior<ClosedBehavior>(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void AddDispatcherBehavior_WithNullType_ShouldThrow()
    {
        var action = () => new ServiceCollection().AddDispatcherBehavior(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData(typeof(IPipelineBehavior<TestCommand>))]
    [InlineData(typeof(AbstractBehavior))]
    [InlineData(typeof(int))]
    public void AddDispatcherBehavior_WithNonConcreteClass_ShouldThrow(Type type)
    {
        var action = () => new ServiceCollection().AddDispatcherBehavior(type);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Contains("concrete class", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(MismatchedOpenBehavior<>))]
    public void AddDispatcherBehavior_WithoutRegistrableContract_ShouldThrow(Type type)
    {
        var action = () => new ServiceCollection().AddDispatcherBehavior(type);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Contains("registrable pipeline behavior contract", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddDispatcherBehavior_ClosedBehavior_ShouldWrapCommandAndQuery()
    {
        var log = new BehaviorLog();
        var provider = BuildProvider(log, static services => services.AddDispatcherBehavior<ClosedBehavior>());
        var mediator = provider.GetRequiredService<IMediator>();
        var tracking = new TrackingState();

        await mediator.SendAsync(new TestCommand(tracking), TestContext.Current.CancellationToken);
        var queryResponse = await mediator.QueryAsync<TestQuery, int>(new TestQuery(2, tracking), TestContext.Current.CancellationToken);

        Assert.Equal(4, queryResponse);
        Assert.Equal(["closed:command", "closed:query"], log.Entries);
        Assert.Equal(["handler:command", "handler:query"], tracking.Entries);
    }

    [Fact]
    public async Task AddDispatcherBehavior_OpenGenericBehavior_ShouldWrapEveryResponseRequest()
    {
        var log = new BehaviorLog();
        var provider = BuildProvider(log, static services => services.AddDispatcherBehavior(typeof(RecordingBehavior<,>)));
        var mediator = provider.GetRequiredService<IMediator>();
        var tracking = new TrackingState();

        var commandResponse = await mediator.SendAsync<TestCommandWithResponse, string>(new TestCommandWithResponse("v", tracking), TestContext.Current.CancellationToken);
        var queryResponse = await mediator.QueryAsync<TestQuery, int>(new TestQuery(3, tracking), TestContext.Current.CancellationToken);

        Assert.Equal("handled:v", commandResponse);
        Assert.Equal(6, queryResponse);
        Assert.Equal(["TestCommandWithResponse", "TestQuery"], log.Entries);
    }

    [Fact]
    public async Task AddDispatcherBehavior_OpenGenericVoidBehavior_ShouldWrapCommands()
    {
        var log = new BehaviorLog();
        var provider = BuildProvider(log, static services => services.AddDispatcherBehavior(typeof(OpenCommandBehavior<>)));

        await provider.GetRequiredService<IMediator>().SendAsync(new TestCommand(new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal(["open:TestCommand"], log.Entries);
    }

    [Fact]
    public async Task AddDispatcherBehavior_OpenGenericWithUnsatisfiedConstraint_ShouldSkipThoseRequests()
    {
        var log = new BehaviorLog();
        var provider = BuildProvider(log, static services => services.AddDispatcherBehavior(typeof(CommandOnlyBehavior<,>)));
        var mediator = provider.GetRequiredService<IMediator>();
        var tracking = new TrackingState();

        await mediator.SendAsync<TestCommandWithResponse, string>(new TestCommandWithResponse("v", tracking), TestContext.Current.CancellationToken);
        await mediator.QueryAsync<TestQuery, int>(new TestQuery(1, tracking), TestContext.Current.CancellationToken);

        Assert.Equal(["command-only:TestCommandWithResponse"], log.Entries);
    }

    [Fact]
    public async Task AddDispatcherBehavior_BehaviorWithBothContracts_ShouldRegisterOnlyMatchingArity()
    {
        var log = new BehaviorLog();
        var provider = BuildProvider(log, static services => services.AddDispatcherBehavior(typeof(BothContractsBehavior<,>)));

        await provider.GetRequiredService<IMediator>().QueryAsync<TestQuery, int>(new TestQuery(1, new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal(["both:TestQuery"], log.Entries);
    }

    [Fact]
    public async Task AddDispatcherBehavior_RegisteredTwice_ShouldRunOnce()
    {
        var log = new BehaviorLog();
        var provider = BuildProvider(log, static services =>
        {
            services.AddDispatcherBehavior<ClosedBehavior>();
            services.AddDispatcherBehavior(typeof(ClosedBehavior));
        });

        await provider.GetRequiredService<IMediator>().SendAsync(new TestCommand(new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal(["closed:command"], log.Entries);
    }

    [Fact]
    public async Task AddDispatcherBehavior_WithOrderedBehaviors_ShouldRunInOrder()
    {
        var log = new BehaviorLog();
        var provider = BuildProvider(log, static services =>
        {
            services.AddDispatcherBehavior<LateBehavior>();
            services.AddDispatcherBehavior<ClosedBehavior>();
        });

        await provider.GetRequiredService<IMediator>().SendAsync(new TestCommand(new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal(["late:command", "closed:command"], log.Entries);
    }

    private static ServiceProvider BuildProvider(BehaviorLog log, Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddTransient<ICommandHandler<TestCommand>, TestCommandHandler>();
        services.AddTransient<ICommandHandler<TestCommandWithResponse, string>, TestCommandWithResponseHandler>();
        services.AddTransient<IQueryHandler<TestQuery, int>, TestQueryHandler>();
        services.AddDispatcher();
        configure(services);
        return services.BuildServiceProvider();
    }

    internal sealed class BehaviorLog
    {
        private readonly List<string> _entries = [];

        public IReadOnlyList<string> Entries => _entries;

        public void Add(string entry) => _entries.Add(entry);
    }

    internal sealed class ClosedBehavior(BehaviorLog log) : IPipelineBehavior<TestCommand>, IPipelineBehavior<TestQuery, int>, IOrderedPipelineBehavior
    {
        public int Order => 10;

        public ValueTask HandleAsync(TestCommand request, RequestHandlerDelegate next, CancellationToken cancellationToken)
        {
            log.Add("closed:command");
            return next();
        }

        public ValueTask<int> HandleAsync(TestQuery request, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
        {
            log.Add("closed:query");
            return next();
        }
    }

    internal sealed class LateBehavior(BehaviorLog log) : IPipelineBehavior<TestCommand>, IOrderedPipelineBehavior
    {
        public int Order => 1;

        public ValueTask HandleAsync(TestCommand request, RequestHandlerDelegate next, CancellationToken cancellationToken)
        {
            log.Add("late:command");
            return next();
        }
    }

    internal sealed class RecordingBehavior<TRequest, TResponse>(BehaviorLog log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            log.Add(typeof(TRequest).Name);
            return next();
        }
    }

    internal sealed class OpenCommandBehavior<TRequest>(BehaviorLog log) : IPipelineBehavior<TRequest>
        where TRequest : notnull
    {
        public ValueTask HandleAsync(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
        {
            log.Add($"open:{typeof(TRequest).Name}");
            return next();
        }
    }

    internal sealed class CommandOnlyBehavior<TRequest, TResponse>(BehaviorLog log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICommand<TResponse>
    {
        public ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            log.Add($"command-only:{typeof(TRequest).Name}");
            return next();
        }
    }

    internal sealed class BothContractsBehavior<TRequest, TResponse>(BehaviorLog log) : IPipelineBehavior<TRequest>, IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public ValueTask HandleAsync(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
            => next();

        public ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            log.Add($"both:{typeof(TRequest).Name}");
            return next();
        }
    }

    internal sealed class MismatchedOpenBehavior<TResponse> : IPipelineBehavior<TestCommand, TResponse>
    {
        public ValueTask<TResponse> HandleAsync(TestCommand request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            => next();
    }

    internal abstract class AbstractBehavior : IPipelineBehavior<TestCommand>
    {
        public abstract ValueTask HandleAsync(TestCommand request, RequestHandlerDelegate next, CancellationToken cancellationToken);
    }
}
