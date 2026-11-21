using PublicApiGenerator;
using System;
using System.Linq;
using System.Reflection;

namespace OpenMacroBoard.Meta.TestUtils;

/// <summary>
/// Creates a text listing of the public API of an assembly, for snapshot tests that make
/// every change of the public API visible in the diff of a commit.
/// </summary>
public static class PublicApiSnapshot
{
    /// <summary>
    /// Lists the public API that is covered by semantic versioning: everything outside of
    /// the <c>Internals</c> namespaces.
    /// </summary>
    public static string GetStableApi(Assembly assembly)
    {
        return Generate(assembly, internals: false);
    }

    /// <summary>
    /// Lists the public types of the <c>Internals</c> namespaces. They are not covered by
    /// semantic versioning.
    /// </summary>
    public static string GetInternalsApi(Assembly assembly)
    {
        return Generate(assembly, internals: true);
    }

    private static string Generate(Assembly assembly, bool internals)
    {
        var types = assembly
            .GetExportedTypes()
            .Where(type => IsInternals(type) == internals)
            .ToArray();

        if (types.Length == 0)
        {
            return "(no public types)";
        }

        var options = new ApiGeneratorOptions
        {
            IncludeTypes = types,
            IncludeAssemblyAttributes = false,
        };

        return assembly.GeneratePublicApi(options);
    }

    private static bool IsInternals(Type type)
    {
        var parts = (type.Namespace ?? string.Empty).Split('.');
        return parts.Contains("Internals", StringComparer.Ordinal);
    }
}
