using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Queries;
using MediatR;
using Wolverine;

namespace DevSource.Dispatcher.Benchmarks;

public sealed class DevSourceCommandHandler : ICommandHandler<DevSourceCommand, int>
{
    public ValueTask<int> HandleAsync(DevSourceCommand command, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(command.Value + 1);
}

public sealed class DevSourceQueryHandler : IQueryHandler<DevSourceQuery, int>
{
    public ValueTask<int> HandleAsync(DevSourceQuery query, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(query.Value + 1);
}

public sealed class MediatRCommandHandler : IRequestHandler<MediatRCommand, int>
{
    public Task<int> Handle(MediatRCommand request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value + 1);
}

public sealed class MediatRQueryHandler : IRequestHandler<MediatRQuery, int>
{
    public Task<int> Handle(MediatRQuery request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value + 1);
}

public sealed class WolverineCommandHandler : IWolverineHandler
{
    public static int Handle(WolverineCommand command)
        => command.Value + 1;
}

public sealed class WolverineQueryHandler : IWolverineHandler
{
    public static int Handle(WolverineQuery query)
        => query.Value + 1;
}

public static class WolverineBenchmarkProbe
{
    private static TaskCompletionSource<int> _commandCompletion = NewCompletionSource();
    private static TaskCompletionSource<int> _queryCompletion = NewCompletionSource();

    public static Task<int> CommandTask => _commandCompletion.Task;

    public static Task<int> QueryTask => _queryCompletion.Task;

    public static void ResetCommand()
        => _commandCompletion = NewCompletionSource();

    public static void ResetQuery()
        => _queryCompletion = NewCompletionSource();

    public static void CompleteCommand(int value)
        => _commandCompletion.TrySetResult(value);

    public static void CompleteQuery(int value)
        => _queryCompletion.TrySetResult(value);

    private static TaskCompletionSource<int> NewCompletionSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class WolverineCommandProbeHandler : IWolverineHandler
{
    public static void Handle(WolverineCommand command)
        => WolverineBenchmarkProbe.CompleteCommand(command.Value + 1);
}

public sealed class WolverineQueryProbeHandler : IWolverineHandler
{
    public static void Handle(WolverineQuery query)
        => WolverineBenchmarkProbe.CompleteQuery(query.Value + 1);
}
