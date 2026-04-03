using DevSource.Dispatcher;
using Order.Application;
using Order.Application.Abstractions;
using Order.Application.Commands;
using Order.Application.Notifications;
using Order.Application.Queries;
using Order.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddOrderApplication();

var app = builder.Build();

app.MapPost("/orders", async (CreateOrderRequest request, IMediator mediator, IClock clock, CancellationToken cancellationToken) =>
{
    var orderId = await mediator.SendAsync<CreateOrderCommand, Guid>(
        new CreateOrderCommand(
            request.CustomerName,
            request.Items.Select(static item => new CreateOrderItemCommand(item.ProductName, item.Quantity, item.UnitPrice)).ToArray()),
        cancellationToken).ConfigureAwait(false);

    await mediator.PublishAsync(new OrderCreatedNotification(orderId, clock.UtcNow), cancellationToken).ConfigureAwait(false);

    return Results.Created($"/orders/{orderId}", new { OrderId = orderId });
});

app.MapGet("/orders/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
{
    var order = await mediator.QueryAsync<GetOrderByIdQuery, Order.Application.Dtos.OrderDto?>(new GetOrderByIdQuery(id), cancellationToken).ConfigureAwait(false);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.Run();

public sealed record CreateOrderItemRequest(string ProductName, int Quantity, decimal UnitPrice);

public sealed record CreateOrderRequest(string CustomerName, IReadOnlyList<CreateOrderItemRequest> Items);

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

internal sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order.Domain.Order> _orders = new();
    private readonly Lock _sync = new();

    public ValueTask AddAsync(Order.Domain.Order order, CancellationToken cancellationToken = default)
    {
        lock (_sync)
            _orders[order.Id] = order;

        return ValueTask.CompletedTask;
    }

    public ValueTask<Order.Domain.Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        Order.Domain.Order? order;

        lock (_sync)
        {
            _orders.TryGetValue(orderId, out var foundOrder);
            order = foundOrder;
        }

        return ValueTask.FromResult(order);
    }
}
