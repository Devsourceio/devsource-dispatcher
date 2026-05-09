using DevSource.Dispatcher.Commands;
using Order.Application.Commands;
using Order.Domain;

namespace Order.Application.Handlers;

public sealed class CreateOrderCommandHandler(IOrderRepository orderRepository) : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));

    public async ValueTask<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = new global::Order.Domain.Order(
            Guid.NewGuid(),
            command.CustomerName,
            DateTimeOffset.UtcNow,
            command.Items.Select(static item => new global::Order.Domain.OrderItem(item.ProductName, item.Quantity, item.UnitPrice)));

        await _orderRepository.AddAsync(order, cancellationToken).ConfigureAwait(false);
        return order.Id;
    }
}
