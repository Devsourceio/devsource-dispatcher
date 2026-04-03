namespace Order.Domain;

public interface IOrderRepository
{
    ValueTask AddAsync(Order order, CancellationToken cancellationToken = default);

    ValueTask<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
