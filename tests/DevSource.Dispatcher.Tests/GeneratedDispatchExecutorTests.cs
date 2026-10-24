using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Generated;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class GeneratedDispatchExecutorTests
{
    [Fact]
    public async Task ExecuteCommandHandledAsync_WhenHandlerCompletesSynchronously_ShouldReturnCompletedTrue()
    {
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterCommandHandler(new TestCommandHandler());
        var tracking = new TrackingState();

        var pending = GeneratedDispatchExecutor.ExecuteCommandHandledAsync(resolver, new TestCommand(tracking), TestContext.Current.CancellationToken);

        Assert.True(pending.IsCompletedSuccessfully);
        Assert.True(await pending);
        Assert.Equal(["handler:command"], tracking.Entries);
    }

    [Fact]
    public async Task ExecuteCommandHandledAsync_WhenHandlerCompletesAsynchronously_ShouldAwaitIt()
    {
        var gate = new TaskCompletionSource();
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterCommandHandler(new DelegateCommandHandler<TestCommand>(_ => GateAsync(gate)));

        var pending = GeneratedDispatchExecutor.ExecuteCommandHandledAsync(resolver, new TestCommand(new TrackingState()), TestContext.Current.CancellationToken);

        Assert.False(pending.IsCompletedSuccessfully);
        gate.SetResult();
        Assert.True(await pending);
    }

    [Fact]
    public async Task ExecuteCommandHandledAsync_WhenHandlerFaults_ShouldSurfaceTheException()
    {
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterCommandHandler(new DelegateCommandHandler<TestCommand>(static _ => new ValueTask(Task.FromException(new InvalidOperationException("handler-fault")))));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await GeneratedDispatchExecutor.ExecuteCommandHandledAsync(resolver, new TestCommand(new TrackingState()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExecuteCommandHandledAsync_WithResponse_ShouldReturnHandledResultSynchronouslyOrAsynchronously()
    {
        var gate = new TaskCompletionSource();
        var syncResolver = new StubRequestHandlerResolver();
        syncResolver.RegisterCommandHandler(new TestCommandWithResponseHandler());
        var asyncResolver = new StubRequestHandlerResolver();
        asyncResolver.RegisterCommandHandler(new DelegateCommandHandler<TestCommandWithResponse, string>(_ => GateAsync(gate, "async-value")));
        var command = new TestCommandWithResponse("v", new TrackingState());

        var sync = GeneratedDispatchExecutor.ExecuteCommandHandledAsync<TestCommandWithResponse, string, string>(syncResolver, command, TestContext.Current.CancellationToken);
        var pending = GeneratedDispatchExecutor.ExecuteCommandHandledAsync<TestCommandWithResponse, string, string>(asyncResolver, command, TestContext.Current.CancellationToken);

        Assert.True(sync.IsCompletedSuccessfully);
        Assert.Equal(DispatchResult<string>.Handled("handled:v"), await sync);
        Assert.False(pending.IsCompletedSuccessfully);
        gate.SetResult();
        Assert.Equal(DispatchResult<string>.Handled("async-value"), await pending);
    }

    [Fact]
    public async Task ExecuteQueryHandledAsync_ShouldReturnHandledResultSynchronouslyOrAsynchronously()
    {
        var gate = new TaskCompletionSource();
        var syncResolver = new StubRequestHandlerResolver();
        syncResolver.RegisterQueryHandler(new TestQueryHandler());
        var asyncResolver = new StubRequestHandlerResolver();
        asyncResolver.RegisterQueryHandler(new DelegateQueryHandler<TestQuery, int>(_ => GateAsync(gate, 99)));
        var query = new TestQuery(4, new TrackingState());

        var sync = GeneratedDispatchExecutor.ExecuteQueryHandledAsync<TestQuery, int, int>(syncResolver, query, TestContext.Current.CancellationToken);
        var pending = GeneratedDispatchExecutor.ExecuteQueryHandledAsync<TestQuery, int, int>(asyncResolver, query, TestContext.Current.CancellationToken);

        Assert.True(sync.IsCompletedSuccessfully);
        Assert.Equal(DispatchResult<int>.Handled(8), await sync);
        Assert.False(pending.IsCompletedSuccessfully);
        gate.SetResult();
        Assert.Equal(DispatchResult<int>.Handled(99), await pending);
    }

    [Fact]
    public async Task PublishNotificationHandledAsync_ShouldReturnTrueSynchronouslyOrAsynchronously()
    {
        var gate = new TaskCompletionSource();
        var syncResolver = new StubRequestHandlerResolver();
        syncResolver.RegisterNotificationHandlers(new TestNotificationHandler("one"));
        var asyncResolver = new StubRequestHandlerResolver();
        asyncResolver.RegisterNotificationHandlers(new DelegateNotificationHandler<TestNotification>(_ => GateAsync(gate)));
        var tracking = new TrackingState();

        var sync = GeneratedDispatchExecutor.PublishNotificationHandledAsync(syncResolver, new TestNotification(tracking), TestContext.Current.CancellationToken);
        var pending = GeneratedDispatchExecutor.PublishNotificationHandledAsync(asyncResolver, new TestNotification(tracking), TestContext.Current.CancellationToken);

        Assert.True(sync.IsCompletedSuccessfully);
        Assert.True(await sync);
        Assert.False(pending.IsCompletedSuccessfully);
        gate.SetResult();
        Assert.True(await pending);
        Assert.Equal(["notification:one"], tracking.Entries);
    }

    [Fact]
    public async Task GeneratedDispatcher_ShouldDispatchEveryKindWithoutAsyncStateMachines()
    {
        var resolver = new StubRequestHandlerResolver();
        resolver.RegisterCommandHandler(new TestCommandHandler());
        resolver.RegisterCommandHandler(new TestCommandWithResponseHandler());
        resolver.RegisterQueryHandler(new TestQueryHandler());
        resolver.RegisterNotificationHandlers(new TestNotificationHandler("one"));
        IGeneratedDispatcher generated = new GeneratedDispatcher(resolver);
        var tracking = new TrackingState();

        var command = generated.TryDispatchAsync(new TestCommand(tracking), TestContext.Current.CancellationToken);
        var commandWithResponse = generated.TryDispatchAsync<TestCommandWithResponse, string>(new TestCommandWithResponse("v", tracking), TestContext.Current.CancellationToken);
        var query = generated.TryDispatchAsync<TestQuery, int>(new TestQuery(2, tracking), TestContext.Current.CancellationToken);
        var notification = generated.TryPublishAsync(new TestNotification(tracking), TestContext.Current.CancellationToken);

        Assert.True(command.IsCompletedSuccessfully && commandWithResponse.IsCompletedSuccessfully && query.IsCompletedSuccessfully && notification.IsCompletedSuccessfully);
        Assert.True(await command);
        Assert.Equal(DispatchResult<string>.Handled("handled:v"), await commandWithResponse);
        Assert.Equal(DispatchResult<int>.Handled(4), await query);
        Assert.True(await notification);
    }

    private static async ValueTask GateAsync(TaskCompletionSource gate)
        => await gate.Task;

    private static async ValueTask<T> GateAsync<T>(TaskCompletionSource gate, T value)
    {
        await gate.Task;
        return value;
    }
}

// Generic handlers are ignored by discovery and by the source generator, so they can stub async completion safely.
internal sealed class DelegateCommandHandler<TCommand>(Func<TCommand, ValueTask> handle) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken = default) => handle(command);
}

internal sealed class DelegateCommandHandler<TCommand, TResponse>(Func<TCommand, ValueTask<TResponse>> handle) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public ValueTask<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default) => handle(command);
}

internal sealed class DelegateQueryHandler<TQuery, TResponse>(Func<TQuery, ValueTask<TResponse>> handle) : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public ValueTask<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default) => handle(query);
}

internal sealed class DelegateNotificationHandler<TNotification>(Func<TNotification, ValueTask> handle) : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default) => handle(notification);
}
