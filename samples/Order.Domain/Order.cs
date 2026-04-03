namespace Order.Domain;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order(Guid id, string customerName, IEnumerable<OrderItem> items)
    {
        Id = id;
        CustomerName = customerName;
        _items.AddRange(items);
    }

    public Guid Id { get; }

    public string CustomerName { get; }

    public IReadOnlyList<OrderItem> Items => _items;

    public decimal Total => _items.Sum(static item => item.Quantity * item.UnitPrice);

    public static Order Create(string customerName, IEnumerable<OrderItem> items)
        => new(Guid.NewGuid(), customerName, items);
}
