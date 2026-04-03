namespace Order.Domain;

public sealed record OrderItem(string ProductName, int Quantity, decimal UnitPrice);
