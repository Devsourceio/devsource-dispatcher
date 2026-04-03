using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;

namespace DevSource.Dispatcher.Benchmarks;

internal sealed class BenchmarkGeneratedDispatcher(IRequestHandlerResolver handlerResolver) : IGeneratedDispatcher
{
    private readonly IRequestHandlerResolver _handlerResolver = handlerResolver;

    ValueTask<bool> IGeneratedCommandDispatcher.TryDispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        => ValueTask.FromResult(false);

    async ValueTask<DispatchResult<TResponse>> IGeneratedCommandDispatcher.TryDispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
    {
        if (command is DevSourceCommand typedCommand)
        {
            var response = await GeneratedDispatchExecutor.ExecuteCommandAsync<DevSourceCommand, int>(_handlerResolver, typedCommand, cancellationToken).ConfigureAwait(false);
            return DispatchResult<TResponse>.Handled((TResponse)(object)response);
        }

        return DispatchResult<TResponse>.NotHandled();
    }

    async ValueTask<DispatchResult<TResponse>> IGeneratedQueryDispatcher.TryDispatchAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
    {
        if (query is DevSourceQuery typedQuery)
        {
            var response = await GeneratedDispatchExecutor.ExecuteQueryAsync<DevSourceQuery, int>(_handlerResolver, typedQuery, cancellationToken).ConfigureAwait(false);
            return DispatchResult<TResponse>.Handled((TResponse)(object)response);
        }

        return DispatchResult<TResponse>.NotHandled();
    }

    ValueTask<bool> IGeneratedNotificationDispatcher.TryPublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        => ValueTask.FromResult(false);
}
