using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Generated;
using Microsoft.Extensions.DependencyInjection;

namespace Order.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderApplication(this IServiceCollection services)
    {
        return services.AddGeneratedDispatcher();
    }
}
