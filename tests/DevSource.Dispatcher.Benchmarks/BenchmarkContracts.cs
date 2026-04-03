using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Queries;
using MediatR;

namespace DevSource.Dispatcher.Benchmarks;

public sealed record DevSourceCommand(int Value) : ICommand<int>;

public sealed record DevSourceQuery(int Value) : IQuery<int>;

public sealed record MediatRCommand(int Value) : IRequest<int>;

public sealed record MediatRQuery(int Value) : IRequest<int>;

public sealed record WolverineCommand(int Value);

public sealed record WolverineQuery(int Value);
