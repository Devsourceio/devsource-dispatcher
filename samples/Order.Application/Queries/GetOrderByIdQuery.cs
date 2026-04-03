using DevSource.Dispatcher.Queries;
using Order.Application.Dtos;

namespace Order.Application.Queries;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto?>;
