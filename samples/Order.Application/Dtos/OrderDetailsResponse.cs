namespace Order.Application.Dtos;

public sealed record OrderDetailsResponse(
    Guid Id,
    string CustomerName,
    DateTimeOffset CreatedAtUtc,
    decimal TotalAmount,
    IReadOnlyList<OrderItemResponse> Items);
