namespace DevSource.Dispatcher.Engine;

/// <summary>
/// Describes the outcome of dispatcher discovery and auto-registration.
/// </summary>
public sealed class DispatcherDiscoveryReport
{
    /// <summary>
    /// Gets the assembly name used as the root of discovery.
    /// </summary>
    public required string RootAssemblyName { get; init; }

    /// <summary>
    /// Gets the assemblies accepted for discovery.
    /// </summary>
    public required IReadOnlyList<string> DiscoveredAssemblies { get; init; }

    /// <summary>
    /// Gets the generated dispatcher type selected during discovery, when available.
    /// </summary>
    public string? GeneratedDispatcherTypeName { get; init; }

    /// <summary>
    /// Gets the number of command handlers registered during discovery.
    /// </summary>
    public int RegisteredCommandHandlerCount { get; init; }

    /// <summary>
    /// Gets the number of query handlers registered during discovery.
    /// </summary>
    public int RegisteredQueryHandlerCount { get; init; }

    /// <summary>
    /// Gets the number of notification handlers registered during discovery.
    /// </summary>
    public int RegisteredNotificationHandlerCount { get; init; }
}
