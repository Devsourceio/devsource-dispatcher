using DevSource.Dispatcher.Notifications;

namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Publishes notifications using a generated path when available and falls back to runtime resolution.
/// </summary>
public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IGeneratedNotificationDispatcher? _generatedDispatcher;
    private readonly IRequestHandlerResolver _handlerResolver;

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an explicit runtime resolver.
    /// </summary>
    public NotificationDispatcher(IRequestHandlerResolver handlerResolver, IGeneratedNotificationDispatcher? generatedDispatcher = null)
    {
        _handlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));
        _generatedDispatcher = generatedDispatcher;
    }

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an <see cref="IServiceProvider"/>.
    /// </summary>
    public NotificationDispatcher(IServiceProvider serviceProvider, IGeneratedNotificationDispatcher? generatedDispatcher = null)
        : this(new ServiceProviderRequestHandlerResolver(serviceProvider), generatedDispatcher)
    {
    }

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an explicit runtime resolver and a unified generated dispatcher.
    /// </summary>
    public NotificationDispatcher(IRequestHandlerResolver handlerResolver, IGeneratedDispatcher generatedDispatcher)
        : this(handlerResolver, (IGeneratedNotificationDispatcher)generatedDispatcher)
    {
    }

    /// <summary>
    /// Creates a dispatcher that resolves handlers through an <see cref="IServiceProvider"/> and a unified generated dispatcher.
    /// </summary>
    public NotificationDispatcher(IServiceProvider serviceProvider, IGeneratedDispatcher generatedDispatcher)
        : this(serviceProvider, (IGeneratedNotificationDispatcher)generatedDispatcher)
    {
    }

    /// <inheritdoc />
    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        if (notification is null)
            throw new ArgumentNullException(nameof(notification));

        if (_generatedDispatcher is null)
            return NotificationDispatchCache<TNotification>.ExecuteAsync(_handlerResolver, notification, cancellationToken);

        var generated = _generatedDispatcher.TryPublishAsync(notification, cancellationToken);
        if (!generated.IsCompletedSuccessfully)
            return PublishAfterGeneratedAsync(generated, notification, cancellationToken);

        return generated.Result
            ? ValueTask.CompletedTask
            : NotificationDispatchCache<TNotification>.ExecuteAsync(_handlerResolver, notification, cancellationToken);
    }

    private async ValueTask PublishAfterGeneratedAsync<TNotification>(ValueTask<bool> generated, TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (!await generated.ConfigureAwait(false))
            await NotificationDispatchCache<TNotification>.ExecuteAsync(_handlerResolver, notification, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask<TResult> PublishAsync<TNotification, TResult>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification<TResult>
    {
        if (notification is null)
            throw new ArgumentNullException(nameof(notification));

        return NotificationDispatchCache<TNotification, TResult>.ExecuteAsync(
            _handlerResolver,
            notification,
            cancellationToken);
    }
}
