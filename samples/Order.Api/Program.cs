using DevSource.Dispatcher;
using DevSource.Dispatcher.Behaviors;
using DevSource.Dispatcher.Engine;
using Order.Application.Commands;
using Order.Application.Dtos;
using Order.Application.Queries;
using Order.Application.Validators;
using Order.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrderInfrastructure();
builder.Services.AddDispatcherDiscovery();
builder.Services.AddDispatcherLogging();
builder.Services.AddDispatcherValidation();
builder.Services.AddTransient<IRequestValidator<CreateOrderCommand>, CreateOrderCommandValidator>();

var app = builder.Build();

app.MapPost("/orders", async (CreateOrderRequest request, IMediator mediator, CancellationToken cancellationToken) =>
{
    Guid orderId;
    try
    {
        orderId = await mediator.SendAsync<CreateOrderCommand, Guid>(
            new CreateOrderCommand(
                request.CustomerName,
                request.Items.Select(static item => new CreateOrderItemCommand(item.ProductName, item.Quantity, item.UnitPrice)).ToArray()),
            cancellationToken).ConfigureAwait(false);
    }
    catch (RequestValidationException exception)
    {
        return Results.ValidationProblem(exception.Errors
            .GroupBy(static error => error.PropertyName)
            .ToDictionary(static group => group.Key, static group => group.Select(static error => error.Message).ToArray()));
    }

    var order = await mediator.QueryAsync<GetOrderByIdQuery, OrderDetailsResponse?>(new GetOrderByIdQuery(orderId), cancellationToken).ConfigureAwait(false);
    return order is null ? Results.NotFound() : Results.Created($"/orders/{orderId}", order);
});

app.MapGet("/orders/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
{
    var order = await mediator.QueryAsync<GetOrderByIdQuery, OrderDetailsResponse?>(new GetOrderByIdQuery(id), cancellationToken).ConfigureAwait(false);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.Run();

public sealed record CreateOrderItemRequest(string ProductName, int Quantity, decimal UnitPrice);

public sealed record CreateOrderRequest(string CustomerName, IReadOnlyList<CreateOrderItemRequest> Items);
