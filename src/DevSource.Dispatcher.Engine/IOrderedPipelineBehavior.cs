namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Defines an explicit execution order for pipeline behaviors.
/// </summary>
public interface IOrderedPipelineBehavior
{
    /// <summary>
    /// Gets the behavior order. Lower values run first.
    /// </summary>
    int Order { get; }
}
