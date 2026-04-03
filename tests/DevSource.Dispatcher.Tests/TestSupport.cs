using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;

namespace DevSource.Dispatcher.Tests;

internal sealed record TestCommand(TrackingState TrackingState) : ICommand;

internal sealed record TestCommandWithResponse(string Value, TrackingState TrackingState) : ICommand<string>;

internal sealed record TestQuery(int Value, TrackingState TrackingState) : IQuery<int>;

internal sealed record TestNotification(TrackingState TrackingState) : INotification;

internal sealed class TrackingState
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries;

    public void Add(string value) => _entries.Add(value);
}

internal sealed class TestCommandHandler : ICommandHandler<TestCommand>
{
    public ValueTask HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
    {
        command.TrackingState.Add("handler:command");
        return ValueTask.CompletedTask;
    }
}

internal sealed class TestCommandWithResponseHandler : ICommandHandler<TestCommandWithResponse, string>
{
    public ValueTask<string> HandleAsync(TestCommandWithResponse command, CancellationToken cancellationToken = default)
    {
        command.TrackingState.Add("handler:command-response");
        return ValueTask.FromResult($"handled:{command.Value}");
    }
}

internal sealed class TestQueryHandler : IQueryHandler<TestQuery, int>
{
    public ValueTask<int> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
    {
        query.TrackingState.Add("handler:query");
        return ValueTask.FromResult(query.Value * 2);
    }
}

internal sealed class TestNotificationHandler(string name) : INotificationHandler<TestNotification>
{
    public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken = default)
    {
        notification.TrackingState.Add($"notification:{name}");
        return ValueTask.CompletedTask;
    }
}

internal sealed class OrderedCommandBehavior(int order, string name) : IPipelineBehavior<TestCommand>, IOrderedPipelineBehavior
{
    public int Order { get; } = order;

    public async ValueTask HandleAsync(TestCommand request, RequestHandlerDelegate next, CancellationToken cancellationToken)
    {
        request.TrackingState.Add($"before:{name}");
        await next().ConfigureAwait(false);
        request.TrackingState.Add($"after:{name}");
    }
}

internal sealed class OrderedResponseBehavior(int order, string name) : IPipelineBehavior<TestCommandWithResponse, string>, IOrderedPipelineBehavior
{
    public int Order { get; } = order;

    public async ValueTask<string> HandleAsync(TestCommandWithResponse request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        request.TrackingState.Add($"before:{name}");
        var response = await next().ConfigureAwait(false);
        request.TrackingState.Add($"after:{name}");
        return response;
    }
}

internal sealed class OrderedQueryBehavior(int order, string name) : IPipelineBehavior<TestQuery, int>, IOrderedPipelineBehavior
{
    public int Order { get; } = order;

    public async ValueTask<int> HandleAsync(TestQuery request, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
    {
        request.TrackingState.Add($"before:{name}");
        var response = await next().ConfigureAwait(false);
        request.TrackingState.Add($"after:{name}");
        return response;
    }
}

internal sealed class AlphaCommandBehavior : IPipelineBehavior<TestCommand>
{
    public async ValueTask HandleAsync(TestCommand request, RequestHandlerDelegate next, CancellationToken cancellationToken)
    {
        request.TrackingState.Add("alpha");
        await next().ConfigureAwait(false);
    }
}

internal sealed class ZetaCommandBehavior : IPipelineBehavior<TestCommand>
{
    public async ValueTask HandleAsync(TestCommand request, RequestHandlerDelegate next, CancellationToken cancellationToken)
    {
        request.TrackingState.Add("zeta");
        await next().ConfigureAwait(false);
    }
}

internal sealed class StubRequestHandlerResolver : IRequestHandlerResolver
{
    private readonly Dictionary<Type, object> _handlers = new();
    private readonly Dictionary<Type, object> _behaviors = new();
    private readonly Dictionary<Type, object> _notificationHandlers = new();

    public void RegisterCommandHandler<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand
        => _handlers[typeof(ICommandHandler<TCommand>)] = handler;

    public void RegisterCommandHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> handler) where TCommand : ICommand<TResponse>
        => _handlers[typeof(ICommandHandler<TCommand, TResponse>)] = handler;

    public void RegisterQueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> handler) where TQuery : IQuery<TResponse>
        => _handlers[typeof(IQueryHandler<TQuery, TResponse>)] = handler;

    public void RegisterCommandBehaviors<TCommand>(params IPipelineBehavior<TCommand>[] behaviors) where TCommand : ICommand
        => _behaviors[typeof(IPipelineBehavior<TCommand>)] = behaviors;

    public void RegisterBehaviors<TRequest, TResponse>(params IPipelineBehavior<TRequest, TResponse>[] behaviors) where TRequest : notnull
        => _behaviors[typeof(IPipelineBehavior<TRequest, TResponse>)] = behaviors;

    public void RegisterNotificationHandlers<TNotification>(params INotificationHandler<TNotification>[] handlers) where TNotification : INotification
        => _notificationHandlers[typeof(INotificationHandler<TNotification>)] = handlers;

    public ICommandHandler<TCommand> GetRequiredCommandHandler<TCommand>() where TCommand : ICommand
        => (ICommandHandler<TCommand>)_handlers[typeof(ICommandHandler<TCommand>)];

    public ICommandHandler<TCommand, TResponse> GetRequiredCommandHandler<TCommand, TResponse>() where TCommand : ICommand<TResponse>
        => (ICommandHandler<TCommand, TResponse>)_handlers[typeof(ICommandHandler<TCommand, TResponse>)];

    public IQueryHandler<TQuery, TResponse> GetRequiredQueryHandler<TQuery, TResponse>() where TQuery : IQuery<TResponse>
        => (IQueryHandler<TQuery, TResponse>)_handlers[typeof(IQueryHandler<TQuery, TResponse>)];

    public IEnumerable<IPipelineBehavior<TCommand>> GetCommandBehaviors<TCommand>() where TCommand : ICommand
        => _behaviors.TryGetValue(typeof(IPipelineBehavior<TCommand>), out var value)
            ? (IEnumerable<IPipelineBehavior<TCommand>>)value
            : [];

    public IEnumerable<IPipelineBehavior<TRequest, TResponse>> GetBehaviors<TRequest, TResponse>() where TRequest : notnull
        => _behaviors.TryGetValue(typeof(IPipelineBehavior<TRequest, TResponse>), out var value)
            ? (IEnumerable<IPipelineBehavior<TRequest, TResponse>>)value
            : [];

    public IEnumerable<INotificationHandler<TNotification>> GetNotificationHandlers<TNotification>() where TNotification : INotification
        => _notificationHandlers.TryGetValue(typeof(INotificationHandler<TNotification>), out var value)
            ? (IEnumerable<INotificationHandler<TNotification>>)value
            : [];
}

internal sealed class TestGeneratedDispatcher : IGeneratedDispatcher
{
    public bool HandleCommandWithoutResponse { get; set; }

    public bool HandleCommandWithResponse { get; set; }

    public bool HandleQuery { get; set; }

    public bool HandleNotification { get; set; }

    public int CommandWithoutResponseCalls { get; private set; }

    public int CommandWithResponseCalls { get; private set; }

    public int QueryCalls { get; private set; }

    public int NotificationCalls { get; private set; }

    ValueTask<bool> IGeneratedCommandDispatcher.TryDispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
    {
        CommandWithoutResponseCalls++;
        return ValueTask.FromResult(HandleCommandWithoutResponse);
    }

    ValueTask<DispatchResult<TResponse>> IGeneratedCommandDispatcher.TryDispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
    {
        CommandWithResponseCalls++;
        if (HandleCommandWithResponse)
            return ValueTask.FromResult(DispatchResult<TResponse>.Handled((TResponse)(object)"generated-response"));

        return ValueTask.FromResult(DispatchResult<TResponse>.NotHandled());
    }

    ValueTask<DispatchResult<TResponse>> IGeneratedQueryDispatcher.TryDispatchAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
    {
        QueryCalls++;
        if (HandleQuery)
            return ValueTask.FromResult(DispatchResult<TResponse>.Handled((TResponse)(object)123));

        return ValueTask.FromResult(DispatchResult<TResponse>.NotHandled());
    }

    ValueTask<bool> IGeneratedNotificationDispatcher.TryPublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
    {
        NotificationCalls++;
        return ValueTask.FromResult(HandleNotification);
    }
}

internal sealed class SampleGeneratedDispatcher : IGeneratedDispatcher
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
