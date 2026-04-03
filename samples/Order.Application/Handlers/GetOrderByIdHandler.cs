using DevSource.Dispatcher.Queries;
using Order.Application.Dtos;
using Order.Application.Queries;
using Order.Domain;

namespace Order.Application.Handlers;

public sealed class GetOrderByIdHandler(IOrderRepository repository) : IQueryHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderRepository _repository = repository;

    public async ValueTask<OrderDto?> HandleAsync(GetOrderByIdQuery query, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(query.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
            return null;

        return new OrderDto(
            order.Id,
            order.CustomerName,
            order.Total,
            order.Items.Select(static item => new OrderItemDto(item.ProductName, item.Quantity, item.UnitPrice)).ToArray());
    }
}
