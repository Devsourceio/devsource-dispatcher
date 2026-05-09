using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Provides dependency injection registration helpers for the dispatcher engine.
/// </summary>
public static class DispatcherServiceCollectionExtensions
{
    /// <summary>
    /// Registers the dispatcher engine services, discovers application assemblies automatically,
    /// registers handlers found in them, and wires the generated dispatcher when available.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddDispatcherDiscovery(this IServiceCollection services)
        => AddDispatcherDiscoveryCore(services, Assembly.GetCallingAssembly(), static _ => { });

    /// <summary>
    /// Registers the dispatcher engine services, discovers application assemblies automatically,
    /// registers handlers found in them, wires the generated dispatcher when available, and emits
    /// a discovery report for diagnostics.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddDispatcherDiscovery(this IServiceCollection services, Action<DispatcherDiscoveryReport> reportCallback)
        => AddDispatcherDiscoveryCore(services, Assembly.GetCallingAssembly(), reportCallback);

    private static IServiceCollection AddDispatcherDiscoveryCore(
        IServiceCollection services,
        Assembly rootAssembly,
        Action<DispatcherDiscoveryReport> reportCallback)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(rootAssembly);
        ArgumentNullException.ThrowIfNull(reportCallback);

        var assemblies = DispatcherDiscovery.FindCandidateAssemblies(rootAssembly);
        var generatedDispatcherType = DispatcherDiscovery.FindGeneratedDispatcherType(assemblies);

        if (generatedDispatcherType is null)
            services.AddDispatcher();
        else
            DispatcherGeneratedRegistration.Register(services, generatedDispatcherType);

        var registrationSummary = HandlerServiceRegistrar.Register(services, assemblies);
        reportCallback(new DispatcherDiscoveryReport
        {
            RootAssemblyName = rootAssembly.GetName().Name ?? rootAssembly.FullName ?? rootAssembly.ToString(),
            DiscoveredAssemblies = assemblies.Select(static assembly => assembly.GetName().Name ?? assembly.FullName ?? assembly.ToString()).ToArray(),
            GeneratedDispatcherTypeName = generatedDispatcherType?.FullName,
            RegisteredCommandHandlerCount = registrationSummary.RegisteredCommandHandlerCount,
            RegisteredQueryHandlerCount = registrationSummary.RegisteredQueryHandlerCount,
            RegisteredNotificationHandlerCount = registrationSummary.RegisteredNotificationHandlerCount,
        });

        return services;
    }

    /// <summary>
    /// Registers the dispatcher engine services and scans the assembly containing <typeparamref name="TMarker"/>
    /// for command, query, and notification handlers.
    /// </summary>
    public static IServiceCollection AddDispatcherFromAssemblyContaining<TMarker>(this IServiceCollection services)
        => services.AddDispatcherFromAssemblies(typeof(TMarker).Assembly);

    /// <summary>
    /// Registers the dispatcher engine services with a generated dispatcher and scans the assembly containing
    /// <typeparamref name="TMarker"/> for command, query, and notification handlers.
    /// </summary>
    public static IServiceCollection AddDispatcherFromAssemblyContaining<TMarker, TGeneratedDispatcher>(this IServiceCollection services)
        where TGeneratedDispatcher : class, IGeneratedDispatcher
        => services.AddDispatcherFromAssemblies<TGeneratedDispatcher>(typeof(TMarker).Assembly);

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
    /// Registers the dispatcher engine services and scans the provided assemblies for command, query,
    /// and notification handlers.
    /// </summary>
    public static IServiceCollection AddDispatcherFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDispatcher();
        HandlerServiceRegistrar.Register(services, assemblies);
        return services;
    }

    /// <summary>
    /// Registers the dispatcher engine services and wires a generated dispatcher into the runtime fallback strategy.
    /// </summary>
    public static IServiceCollection AddDispatcher<TGeneratedDispatcher>(this IServiceCollection services)
        where TGeneratedDispatcher : class, IGeneratedDispatcher
    {
        ArgumentNullException.ThrowIfNull(services);

        DispatcherGeneratedRegistration.Register(services, typeof(TGeneratedDispatcher));

        return services;
    }

    /// <summary>
    /// Registers the dispatcher engine services with a generated dispatcher and scans the provided assemblies for
    /// command, query, and notification handlers.
    /// </summary>
    public static IServiceCollection AddDispatcherFromAssemblies<TGeneratedDispatcher>(this IServiceCollection services, params Assembly[] assemblies)
        where TGeneratedDispatcher : class, IGeneratedDispatcher
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDispatcher<TGeneratedDispatcher>();
        HandlerServiceRegistrar.Register(services, assemblies);
        return services;
    }
}
