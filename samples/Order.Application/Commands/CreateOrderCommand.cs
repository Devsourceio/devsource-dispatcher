using DevSource.Dispatcher.Commands;

namespace Order.Application.Commands;

public sealed record CreateOrderCommand(string CustomerName, IReadOnlyList<CreateOrderItemCommand> Items) : ICommand<Guid>;
