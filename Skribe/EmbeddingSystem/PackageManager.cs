using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

/// <summary>
/// Manages loading of embedded package assemblies at runtime.
/// Designed to be used within a class library (DLL) that has other DLLs embedded as resources.
/// </summary>
public static class PackageManager
{
    private static readonly Dictionary<string, Assembly> _loadedAssemblies = new(StringComparer.OrdinalIgnoreCase);

    // Static constructor hooks assembly resolve and loads embedded DLLs immediately upon first access
    static PackageManager()
    {
        AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
        LoadEmbeddedAssemblies();
    }

    /// <summary>
    /// Loads all embedded .dll resources from this class library.
    /// </summary>
    private static void LoadEmbeddedAssemblies()
    {
        var thisAssembly = typeof(PackageManager).Assembly;
        var resourceNames = thisAssembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

        foreach (var resourceName in resourceNames)
        {
            using var stream = thisAssembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                continue;

            var assemblyData = new byte[stream.Length];
            stream.Read(assemblyData, 0, assemblyData.Length);
            var assembly = Assembly.Load(assemblyData);
            _loadedAssemblies[assembly.FullName] = assembly;
        }
    }

    /// <summary>
    /// Resolves assembly loading by returning already loaded embedded assemblies when requested.
    /// </summary>
    private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
    {
        var requestedName = new AssemblyName(args.Name).Name;
        return _loadedAssemblies.Values
            .FirstOrDefault(a => a.GetName().Name.Equals(requestedName, StringComparison.OrdinalIgnoreCase));
    }
}