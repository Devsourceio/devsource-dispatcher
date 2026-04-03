using DevSource.Dispatcher.Commands;

namespace Order.Application.Commands;

public sealed record CreateOrderItemCommand(string ProductName, int Quantity, decimal UnitPrice);

public sealed record CreateOrderCommand(string CustomerName, IReadOnlyList<CreateOrderItemCommand> Items) : ICommand<Guid>;
