namespace DevSource.Dispatcher.Notifications;

/// <summary>
/// Defines a notification interface with a covariant result type.
/// Used to represent notifications with associated return types, allowing natural inheritance-based polymorphism.
/// </summary>
/// <typeparam name="T">The covariant return type associated with the notification.</typeparam>
public interface INotification<out T> : INotification;