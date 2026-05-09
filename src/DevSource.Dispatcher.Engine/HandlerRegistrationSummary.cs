namespace DevSource.Dispatcher.Engine;

internal sealed class HandlerRegistrationSummary
{
    public int RegisteredCommandHandlerCount { get; set; }

    public int RegisteredQueryHandlerCount { get; set; }

    public int RegisteredNotificationHandlerCount { get; set; }
}
