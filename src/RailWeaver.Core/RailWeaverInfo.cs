using System.Reflection;

namespace RailWeaver.Core;

/// <summary>
/// Product identity exposed by the core. Deliberately the only type in RW-000:
/// no railway behaviour exists until it is researched and specified.
/// </summary>
public static class RailWeaverInfo
{
    public const string Name = "RailWeaver";

    /// <summary>SemVer product version, taken from the assembly (set in Directory.Build.props).</summary>
    public static string Version { get; } =
        typeof(RailWeaverInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
        ?? "0.0.0";
}
