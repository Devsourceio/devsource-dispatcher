namespace Order.Application.Dtos;

public sealed record OrderItemResponse(string ProductName, int Quantity, decimal UnitPrice, decimal TotalAmount);
