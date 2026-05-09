namespace Order.Domain;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Order(Guid id, string customerName, DateTimeOffset createdAtUtc, IEnumerable<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.", nameof(customerName));

        ArgumentNullException.ThrowIfNull(items);

        Id = id;
        CustomerName = customerName;
        CreatedAtUtc = createdAtUtc;
        _items.AddRange(items);

        if (_items.Count == 0)
            throw new ArgumentException("An order must contain at least one item.", nameof(items));
    }

    public Guid Id { get; }

    public string CustomerName { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<OrderItem> Items => _items;

    public decimal TotalAmount => _items.Sum(static item => item.TotalAmount);
}
