using Microsoft.Extensions.DependencyInjection;
using Order.Domain;
using Order.Infrastructure.Persistence;

namespace Order.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        return services;
    }
}
