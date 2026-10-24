using Microsoft.Extensions.Logging;

namespace DevSource.Dispatcher.Behaviors;

internal static partial class RequestLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Handling {RequestType}")]
    public static partial void Handling(ILogger logger, string requestType);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Handled {RequestType} in {ElapsedMilliseconds} ms")]
    public static partial void Handled(ILogger logger, string requestType, double elapsedMilliseconds);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "{RequestType} failed after {ElapsedMilliseconds} ms")]
    public static partial void Failed(ILogger logger, Exception exception, string requestType, double elapsedMilliseconds);
}
