using RailWeaver.Core;

namespace RailWeaver.Core.Tests;

/// <summary>
/// Guards ADR-001/ADR-002: the core must stay independent of hosting, persistence and rendering.
/// </summary>
public class CoreBoundaryTests
{
    private static readonly string[] ForbiddenPrefixes =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "RailWeaver.Api",
    ];

    [Fact]
    public void Core_DoesNotReferenceInfrastructureAssemblies()
    {
        var references = typeof(RailWeaverInfo).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        var violations = references
            .Where(name => ForbiddenPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            .ToList();

        Assert.Empty(violations);
    }
}
