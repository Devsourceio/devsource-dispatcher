namespace Order.Application.Commands;

public sealed record CreateOrderItemCommand(string ProductName, int Quantity, decimal UnitPrice);
