namespace DevSource.Dispatcher;

/// <summary>
/// Marks an assembly as a trusted dispatcher discovery target.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class DispatcherAssemblyAttribute : Attribute
{
}
