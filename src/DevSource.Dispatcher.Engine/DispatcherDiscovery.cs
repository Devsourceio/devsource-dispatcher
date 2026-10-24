using System.Reflection;

namespace DevSource.Dispatcher.Engine;

internal static class DispatcherDiscovery
{
    private const string DispatcherAssemblyAttributeFullName = "DevSource.Dispatcher.DispatcherAssemblyAttribute";
    private const string GeneratedDispatcherFullName = "DevSource.Dispatcher.Generated.GeneratedDispatcher";
    private static readonly string[] FrameworkPrefixes = ["System", "Microsoft", "Windows"];

    public static Assembly[] FindCandidateAssemblies(Assembly rootAssembly)
        => FindCandidateAssemblies(rootAssembly, out _);

    // Loaded dispatcher assemblies are trusted by their explicit marker, so they are accepted even when the root's
    // metadata no longer references them (the compiler drops references that the root never uses in code).
    public static Assembly[] FindCandidateAssemblies(Assembly rootAssembly, out IReadOnlyList<string> skippedAssemblies)
    {
        ArgumentNullException.ThrowIfNull(rootAssembly);

        var assemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase)
        {
            [rootAssembly.GetName().Name!] = rootAssembly,
        };
        var skipped = new List<string>();

        foreach (var reference in rootAssembly.GetReferencedAssemblies())
        {
            if (string.IsNullOrWhiteSpace(reference.Name) || IsFrameworkAssembly(reference.Name))
                continue;

            var assembly = TryLoadAssembly(reference);
            if (assembly is null)
                skipped.Add(reference.Name);
            else if (IsDispatcherAssembly(assembly))
                assemblies[assembly.GetName().Name!] = assembly;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && IsDispatcherAssembly(assembly))
                assemblies.TryAdd(assembly.GetName().Name!, assembly);
        }

        skippedAssemblies = skipped;
        return assemblies.Values.ToArray();
    }

    public static Type? FindGeneratedDispatcherType(IEnumerable<Assembly> assemblies)
    {
        Type? generatedDispatcherType = null;

        foreach (var assembly in assemblies)
        {
            if (!IsDispatcherAssembly(assembly))
                continue;

            var candidateType = assembly.GetType(GeneratedDispatcherFullName, throwOnError: false, ignoreCase: false);
            if (candidateType is null)
                continue;

            if (!typeof(IGeneratedDispatcher).IsAssignableFrom(candidateType) || candidateType.IsAbstract)
                continue;

            if (generatedDispatcherType is not null)
                throw new InvalidOperationException($"Multiple generated dispatchers were discovered. Use AddDispatcher<TGeneratedDispatcher>() or AddDispatcherFromAssemblies<TGeneratedDispatcher>() to register the intended dispatcher explicitly.");

            generatedDispatcherType = candidateType;
        }

        return generatedDispatcherType;
    }

    // An optional or platform-specific reference that cannot load cannot hold handlers we could register either.
    private static Assembly? TryLoadAssembly(AssemblyName assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            return null;
        }
    }

    private static bool IsFrameworkAssembly(string assemblyName)
        => assemblyName is "netstandard" or "mscorlib" or "WindowsBase"
           || FrameworkPrefixes.Any(prefix =>
               assemblyName.Equals(prefix, StringComparison.OrdinalIgnoreCase)
               || assemblyName.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase));

    private static bool IsDispatcherAssembly(Assembly assembly)
        => assembly.GetCustomAttributesData().Any(static attribute =>
            string.Equals(attribute.AttributeType.FullName, DispatcherAssemblyAttributeFullName, StringComparison.Ordinal));
}
