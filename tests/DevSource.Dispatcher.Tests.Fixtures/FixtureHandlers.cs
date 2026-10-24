using DevSource.Dispatcher;
using DevSource.Dispatcher.Commands;

// Marked by hand: this assembly stands in for a dispatcher assembly that discovery must pick up from a referencing assembly.
[assembly: DispatcherAssembly]

namespace DevSource.Dispatcher.Tests.Fixtures;

public sealed record FixtureCommand : ICommand;

public sealed class FixtureCommandHandler : ICommandHandler<FixtureCommand>
{
    public ValueTask HandleAsync(FixtureCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
