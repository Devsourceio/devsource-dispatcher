namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Represents the unified generated dispatch contract used by the engine.
/// </summary>
public interface IGeneratedDispatcher :
    IGeneratedCommandDispatcher,
    IGeneratedQueryDispatcher,
    IGeneratedNotificationDispatcher
{
}
