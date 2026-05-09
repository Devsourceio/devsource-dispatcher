using DevSource.Dispatcher.Queries;
using Order.Application.Dtos;
using Order.Application.Queries;
using Order.Domain;

namespace Order.Application.Handlers;

public sealed class GetOrderByIdQueryHandler(IOrderRepository orderRepository) : IQueryHandler<GetOrderByIdQuery, OrderDetailsResponse?>
{
    private readonly IOrderRepository _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));

    public async ValueTask<OrderDetailsResponse?> HandleAsync(GetOrderByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken).ConfigureAwait(false);
        return order is null
            ? null
            : new OrderDetailsResponse(
                order.Id,
                order.CustomerName,
                order.CreatedAtUtc,
                order.TotalAmount,
                order.Items.Select(static item => new OrderItemResponse(item.ProductName, item.Quantity, item.UnitPrice, item.TotalAmount)).ToArray());
    }
}
