namespace Order.Domain;

public sealed class OrderItem
{
    public OrderItem(string productName, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        if (unitPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must be greater than zero.");

        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public string ProductName { get; }

    public int Quantity { get; }

    public decimal UnitPrice { get; }

    public decimal TotalAmount => Quantity * UnitPrice;
}
