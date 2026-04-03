using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Provides dependency injection registration helpers for the dispatcher engine.
/// </summary>
public static class DispatcherServiceCollectionExtensions
{
    /// <summary>
    /// Registers the dispatcher engine services without a generated dispatcher.
    /// </summary>
    public static IServiceCollection AddDispatcher(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<IRequestHandlerResolver, ServiceProviderRequestHandlerResolver>();
        services.TryAddTransient<ICommandDispatcher>(static serviceProvider => new CommandDispatcher(serviceProvider.GetRequiredService<IRequestHandlerResolver>()));
        services.TryAddTransient<IQueryDispatcher>(static serviceProvider => new QueryDispatcher(serviceProvider.GetRequiredService<IRequestHandlerResolver>()));
        services.TryAddTransient<INotificationDispatcher>(static serviceProvider => new NotificationDispatcher(serviceProvider.GetRequiredService<IRequestHandlerResolver>()));
        services.TryAddTransient<IMediator, Mediator>();

        return services;
    }

    /// <summary>
    /// Registers the dispatcher engine services and wires a generated dispatcher into the runtime fallback strategy.
    /// </summary>
    public static IServiceCollection AddDispatcher<TGeneratedDispatcher>(this IServiceCollection services)
        where TGeneratedDispatcher : class, IGeneratedDispatcher
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDispatcher();
        services.TryAddTransient<TGeneratedDispatcher>();
        services.TryAddTransient<IGeneratedDispatcher>(static serviceProvider => serviceProvider.GetRequiredService<TGeneratedDispatcher>());
        services.TryAddTransient<IGeneratedCommandDispatcher>(static serviceProvider => serviceProvider.GetRequiredService<TGeneratedDispatcher>());
        services.TryAddTransient<IGeneratedQueryDispatcher>(static serviceProvider => serviceProvider.GetRequiredService<TGeneratedDispatcher>());
        services.TryAddTransient<IGeneratedNotificationDispatcher>(static serviceProvider => serviceProvider.GetRequiredService<TGeneratedDispatcher>());
        services.Replace(ServiceDescriptor.Transient<ICommandDispatcher>(static serviceProvider => new CommandDispatcher(
            serviceProvider.GetRequiredService<IRequestHandlerResolver>(),
            serviceProvider.GetRequiredService<IGeneratedDispatcher>())));
        services.Replace(ServiceDescriptor.Transient<IQueryDispatcher>(static serviceProvider => new QueryDispatcher(
            serviceProvider.GetRequiredService<IRequestHandlerResolver>(),
            serviceProvider.GetRequiredService<IGeneratedDispatcher>())));
        services.Replace(ServiceDescriptor.Transient<INotificationDispatcher>(static serviceProvider => new NotificationDispatcher(
            serviceProvider.GetRequiredService<IRequestHandlerResolver>(),
            serviceProvider.GetRequiredService<IGeneratedDispatcher>())));

        return services;
    }
}
