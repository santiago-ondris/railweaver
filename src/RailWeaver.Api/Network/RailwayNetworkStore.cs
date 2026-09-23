using System.Collections.Concurrent;
using RailWeaver.Api.Railways;
using RailWeaver.Api.Regions;
using RailWeaver.Core.Geography;
using RailWeaver.Core.Infrastructure;
using RailWeaver.Core.Infrastructure.Network;

namespace RailWeaver.Api.Network;

public sealed class RailwayNetworkStore(RegionFileStore regions, RailwayFileStore railways)
{
    private readonly ConcurrentDictionary<string, Lazy<Task<NetworkDataset?>>> datasets = new(StringComparer.Ordinal);

    public Task<NetworkDataset?> FindAsync(string regionId) =>
        datasets.GetOrAdd(regionId, id => new Lazy<Task<NetworkDataset?>>(() => BuildAsync(id))).Value;

    private async Task<NetworkDataset?> BuildAsync(string id)
    {
        var region = await regions.FindAsync(id, CancellationToken.None);
        if (region is null) return null;
        var railway = await railways.FindDomainAsync(id, CancellationToken.None);
        if (railway is null) return null;
        var box = region.BoundingBox;
        var topology = RailwayNetworkBuilder.Build(railway.Tracks, railway.Stations,
            new GeoBoundingBox(box.West, box.South, box.East, box.North));
        return new NetworkDataset(topology,
            railway.Stations.ToDictionary(station => station.Id, StringComparer.Ordinal));
    }
}

public sealed record NetworkDataset(RailwayTopology Topology,
    IReadOnlyDictionary<string, RailwayStation> Stations);
