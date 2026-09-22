using System.Text.Json;
using RailWeaver.Core.Geography;

namespace RailWeaver.Api.Elevation;

public sealed class ElevationFileStore(string regionsDirectory) : IDisposable
{
    private readonly Dictionary<string, ElevationDataset> datasets = [];
    private readonly object sync = new();

    public ElevationDataset? Find(string regionId)
    {
        if (!IsValidId(regionId) || !File.Exists(Path.Combine(regionsDirectory, $"{regionId}.json"))) return null;
        lock (sync)
        {
            if (datasets.TryGetValue(regionId, out var cached)) return cached;
            var directory = Path.Combine(regionsDirectory, regionId);
            var gridPath = Path.Combine(directory, "elevation", "elevation.rwe");
            var manifestPath = Path.Combine(directory, "elevation.source.json");
            if (!File.Exists(gridPath) || !File.Exists(manifestPath)) return new ElevationDataset(null, null, null);
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var root = document.RootElement;
            var dataset = new ElevationDataset(
                new ElevationGridFile(gridPath),
                root.GetProperty("sha256").GetString(),
                root.GetProperty("attribution").GetString());
            datasets[regionId] = dataset;
            return dataset;
        }
    }

    public void Dispose()
    {
        foreach (var dataset in datasets.Values) dataset.Grid?.Dispose();
    }

    private static bool IsValidId(string id) => !string.IsNullOrEmpty(id) && id[0] != '-' && id[^1] != '-'
        && id.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
}

public sealed record ElevationDataset(ElevationGridFile? Grid, string? Sha256, string? Attribution)
{
    public bool IsAvailable => Grid is not null;
}
