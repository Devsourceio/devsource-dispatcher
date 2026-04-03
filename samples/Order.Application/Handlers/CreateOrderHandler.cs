using DevSource.Dispatcher.Commands;
using Order.Application.Commands;
using Order.Domain;

namespace Order.Application.Handlers;

public sealed class CreateOrderHandler(IOrderRepository repository) : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repository = repository;

    public async ValueTask<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var items = command.Items.Select(static item => new OrderItem(item.ProductName, item.Quantity, item.UnitPrice));
        var order = Domain.Order.Create(command.CustomerName, items);

        await _repository.AddAsync(order, cancellationToken).ConfigureAwait(false);

        return order.Id;
    }
}
