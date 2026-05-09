using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace DevSource.Dispatcher.Engine;

internal static class HandlerServiceRegistrar
{
    public static HandlerRegistrationSummary Register(IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Length == 0)
            throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));

        var registeredSingleHandlers = BuildRegisteredSingleHandlerMap(services);
        var summary = new HandlerRegistrationSummary();

        foreach (var assembly in assemblies.Distinct())
        {
            ArgumentNullException.ThrowIfNull(assembly);

            foreach (var type in assembly.DefinedTypes)
            {
                if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
                    continue;

                RegisterHandlersForType(services, type.AsType(), registeredSingleHandlers, summary);
            }
        }

        return summary;
    }

    private static void RegisterHandlersForType(IServiceCollection services, Type implementationType, IDictionary<Type, Type?> registeredSingleHandlers, HandlerRegistrationSummary summary)
    {
        foreach (var serviceType in implementationType.GetInterfaces())
        {
            if (!serviceType.IsGenericType)
                continue;

            var genericTypeDefinition = serviceType.GetGenericTypeDefinition();

            if (genericTypeDefinition == typeof(ICommandHandler<>) ||
                genericTypeDefinition == typeof(ICommandHandler<,>) ||
                genericTypeDefinition == typeof(IQueryHandler<,>))
            {
                RegisterSingleHandler(services, serviceType, implementationType, registeredSingleHandlers, summary);
                continue;
            }

            if (genericTypeDefinition == typeof(INotificationHandler<>))
            {
                var descriptor = ServiceDescriptor.Transient(serviceType, implementationType);
                if (!services.Any(existing => existing.ServiceType == serviceType && existing.ImplementationType == implementationType))
                {
                    services.TryAddEnumerable(descriptor);
                    summary.RegisteredNotificationHandlerCount++;
                }
            }
        }
    }

    private static void RegisterSingleHandler(IServiceCollection services, Type serviceType, Type implementationType, IDictionary<Type, Type?> registeredSingleHandlers, HandlerRegistrationSummary summary)
    {
        if (registeredSingleHandlers.TryGetValue(serviceType, out var registeredImplementationType))
        {
            if (registeredImplementationType == implementationType)
                return;

            throw new InvalidOperationException($"Multiple handlers were registered for {serviceType.FullName}. Existing: {registeredImplementationType?.FullName ?? "factory/instance registration"}. New: {implementationType.FullName}.");
        }

        services.AddTransient(serviceType, implementationType);
        registeredSingleHandlers[serviceType] = implementationType;

        var genericTypeDefinition = serviceType.GetGenericTypeDefinition();
        if (genericTypeDefinition == typeof(IQueryHandler<,>))
            summary.RegisteredQueryHandlerCount++;
        else
            summary.RegisteredCommandHandlerCount++;
    }

    private static Dictionary<Type, Type?> BuildRegisteredSingleHandlerMap(IServiceCollection services)
    {
        var registeredSingleHandlers = new Dictionary<Type, Type?>();

        foreach (var descriptor in services)
        {
            if (!IsSingleHandlerContract(descriptor.ServiceType))
                continue;

            if (registeredSingleHandlers.TryGetValue(descriptor.ServiceType, out var existingImplementationType) &&
                existingImplementationType != descriptor.ImplementationType)
            {
                throw new InvalidOperationException($"Multiple handlers were registered for {descriptor.ServiceType.FullName}. Existing: {existingImplementationType?.FullName ?? "factory/instance registration"}. New: {descriptor.ImplementationType?.FullName ?? "factory/instance registration"}.");
            }

            registeredSingleHandlers[descriptor.ServiceType] = descriptor.ImplementationType;
        }

        return registeredSingleHandlers;
    }

    private static bool IsSingleHandlerContract(Type serviceType)
    {
        if (!serviceType.IsGenericType)
            return false;

        var genericTypeDefinition = serviceType.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(ICommandHandler<>) ||
               genericTypeDefinition == typeof(ICommandHandler<,>) ||
               genericTypeDefinition == typeof(IQueryHandler<,>);
    }
}
