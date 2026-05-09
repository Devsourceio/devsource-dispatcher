using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace DevSource.Dispatcher.Engine;

internal static class DispatcherGeneratedRegistration
{
    public static void Register(IServiceCollection services, Type generatedDispatcherType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(generatedDispatcherType);

        if (!typeof(IGeneratedDispatcher).IsAssignableFrom(generatedDispatcherType) || generatedDispatcherType.IsAbstract)
            throw new ArgumentException($"Type {generatedDispatcherType.FullName} must implement {typeof(IGeneratedDispatcher).FullName}.", nameof(generatedDispatcherType));

        services.AddDispatcher();
        services.TryAddTransient(generatedDispatcherType, serviceProvider => CreateGeneratedDispatcher(serviceProvider, generatedDispatcherType));
        services.TryAddTransient(typeof(IGeneratedDispatcher), serviceProvider =>
            (IGeneratedDispatcher)serviceProvider.GetRequiredService(generatedDispatcherType));
        services.TryAddTransient(typeof(IGeneratedCommandDispatcher), serviceProvider =>
            (IGeneratedCommandDispatcher)serviceProvider.GetRequiredService(generatedDispatcherType));
        services.TryAddTransient(typeof(IGeneratedQueryDispatcher), serviceProvider =>
            (IGeneratedQueryDispatcher)serviceProvider.GetRequiredService(generatedDispatcherType));
        services.TryAddTransient(typeof(IGeneratedNotificationDispatcher), serviceProvider =>
            (IGeneratedNotificationDispatcher)serviceProvider.GetRequiredService(generatedDispatcherType));
        services.Replace(ServiceDescriptor.Transient<ICommandDispatcher>(static serviceProvider => new CommandDispatcher(
            serviceProvider.GetRequiredService<IRequestHandlerResolver>(),
            serviceProvider.GetRequiredService<IGeneratedDispatcher>())));
        services.Replace(ServiceDescriptor.Transient<IQueryDispatcher>(static serviceProvider => new QueryDispatcher(
            serviceProvider.GetRequiredService<IRequestHandlerResolver>(),
            serviceProvider.GetRequiredService<IGeneratedDispatcher>())));
        services.Replace(ServiceDescriptor.Transient<INotificationDispatcher>(static serviceProvider => new NotificationDispatcher(
            serviceProvider.GetRequiredService<IRequestHandlerResolver>(),
            serviceProvider.GetRequiredService<IGeneratedDispatcher>())));
    }

    private static object CreateGeneratedDispatcher(IServiceProvider serviceProvider, Type generatedDispatcherType)
    {
        var resolverConstructor = generatedDispatcherType.GetConstructor([typeof(IRequestHandlerResolver)]);
        if (resolverConstructor is not null)
            return resolverConstructor.Invoke([serviceProvider.GetRequiredService<IRequestHandlerResolver>()]);

        var serviceProviderConstructor = generatedDispatcherType.GetConstructor([typeof(IServiceProvider)]);
        if (serviceProviderConstructor is not null)
            return serviceProviderConstructor.Invoke([serviceProvider]);

        var parameterlessConstructor = generatedDispatcherType.GetConstructor(Type.EmptyTypes);
        if (parameterlessConstructor is not null)
            return parameterlessConstructor.Invoke([]);

        throw new InvalidOperationException($"Type {generatedDispatcherType.FullName} must expose a public constructor accepting {typeof(IRequestHandlerResolver).FullName}, {typeof(IServiceProvider).FullName}, or no arguments.");
    }
}
