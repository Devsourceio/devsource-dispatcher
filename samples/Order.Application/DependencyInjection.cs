using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Generated;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Behaviors;
using Order.Application.Commands;
using Order.Application.Handlers;
using Order.Application.Notifications;
using Order.Application.Queries;

namespace Order.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderApplication(this IServiceCollection services)
    {
        services.AddTransient<DevSource.Dispatcher.Commands.ICommandHandler<CreateOrderCommand, Guid>, CreateOrderHandler>();
        services.AddTransient<DevSource.Dispatcher.Queries.IQueryHandler<GetOrderByIdQuery, Dtos.OrderDto?>, GetOrderByIdHandler>();
        services.AddTransient<DevSource.Dispatcher.Notifications.INotificationHandler<OrderCreatedNotification>, OrderCreatedHandler>();
        services.AddTransient(typeof(DevSource.Dispatcher.IPipelineBehavior<,>), typeof(CorrelationLoggingBehavior<,>));
        services.AddDispatcher<GeneratedDispatcher>();
        return services;
    }
}
