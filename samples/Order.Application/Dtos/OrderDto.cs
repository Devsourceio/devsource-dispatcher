namespace Order.Application.Dtos;

public sealed record OrderItemDto(string ProductName, int Quantity, decimal UnitPrice);

public sealed record OrderDto(Guid Id, string CustomerName, decimal Total, IReadOnlyList<OrderItemDto> Items);
