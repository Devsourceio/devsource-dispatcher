using Order.Domain;

namespace Order.Infrastructure.Persistence;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Lock _sync = new();
    private readonly Dictionary<Guid, Order.Domain.Order> _orders = [];

    public ValueTask AddAsync(Order.Domain.Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        lock (_sync)
            _orders[order.Id] = order;

        return ValueTask.CompletedTask;
    }

    public ValueTask<Order.Domain.Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        Order.Domain.Order? order;

        lock (_sync)
            _orders.TryGetValue(orderId, out order);

        return ValueTask.FromResult(order);
    }
}
