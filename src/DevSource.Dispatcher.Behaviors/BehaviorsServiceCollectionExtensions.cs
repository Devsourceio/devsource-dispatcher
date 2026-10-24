using DevSource.Dispatcher.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace DevSource.Dispatcher.Behaviors;

/// <summary>
/// Registers the ready-made pipeline behaviors.
/// </summary>
public static class BehaviorsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="LoggingBehavior{TRequest, TResponse}"/> and <see cref="LoggingCommandBehavior{TRequest}"/> for every request.
    /// Requires an <see cref="Microsoft.Extensions.Logging.ILogger{TCategoryName}"/> registration, for example through <c>AddLogging()</c>.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    public static IServiceCollection AddDispatcherLogging(this IServiceCollection services)
        => services
            .AddDispatcherBehavior(typeof(LoggingBehavior<,>))
            .AddDispatcherBehavior(typeof(LoggingCommandBehavior<>));

    /// <summary>
    /// Registers <see cref="ValidationBehavior{TRequest, TResponse}"/> and <see cref="ValidationCommandBehavior{TRequest}"/> for every request.
    /// Register <see cref="IRequestValidator{TRequest}"/> implementations separately; requests without a validator pass through.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    public static IServiceCollection AddDispatcherValidation(this IServiceCollection services)
        => services
            .AddDispatcherBehavior(typeof(ValidationBehavior<,>))
            .AddDispatcherBehavior(typeof(ValidationCommandBehavior<>));
}
