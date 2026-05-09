using System.Reflection;

namespace DevSource.Dispatcher.Engine;

internal static class DispatcherDiscovery
{
    private const string DispatcherAssemblyAttributeFullName = "DevSource.Dispatcher.DispatcherAssemblyAttribute";
    private const string GeneratedDispatcherFullName = "DevSource.Dispatcher.Generated.GeneratedDispatcher";

    public static Assembly[] FindCandidateAssemblies(Assembly rootAssembly)
    {
        ArgumentNullException.ThrowIfNull(rootAssembly);

        var assemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase)
        {
            [rootAssembly.GetName().Name!] = rootAssembly,
        };

        foreach (var reference in rootAssembly.GetReferencedAssemblies())
        {
            if (string.IsNullOrWhiteSpace(reference.Name) || IsFrameworkAssembly(reference.Name))
                continue;

            var assembly = LoadAssembly(reference);
            if (assembly == rootAssembly || IsDispatcherAssembly(assembly))
                assemblies[assembly.GetName().Name!] = assembly;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
                continue;

            var assemblyName = assembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(assemblyName) || assemblies.ContainsKey(assemblyName))
                continue;

            if (ReferencesRootAssembly(assembly, rootAssembly) && IsDispatcherAssembly(assembly))
                assemblies[assemblyName] = assembly;
        }

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

    private static Assembly LoadAssembly(AssemblyName assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to load referenced assembly '{assemblyName.FullName}' during dispatcher discovery. Use AddDispatcherFromAssemblies for explicit registration if this dependency should be excluded.", exception);
        }
    }

    private static bool IsFrameworkAssembly(string assemblyName)
        => assemblyName.StartsWith("System", StringComparison.OrdinalIgnoreCase)
           || assemblyName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
           || assemblyName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
           || assemblyName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)
           || assemblyName.StartsWith("Windows", StringComparison.OrdinalIgnoreCase);

    private static bool IsDispatcherAssembly(Assembly assembly)
        => assembly.GetCustomAttributesData().Any(static attribute =>
            string.Equals(attribute.AttributeType.FullName, DispatcherAssemblyAttributeFullName, StringComparison.Ordinal));

    private static bool ReferencesRootAssembly(Assembly assembly, Assembly rootAssembly)
    {
        var rootAssemblyName = rootAssembly.GetName().Name;
        if (string.IsNullOrWhiteSpace(rootAssemblyName))
            return false;

        return assembly.GetReferencedAssemblies().Any(reference =>
            string.Equals(reference.Name, rootAssemblyName, StringComparison.OrdinalIgnoreCase));
    }
}
