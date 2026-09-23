using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Infrastructure.Network;

public static class RailwayNetworkBuilder
{
    public static RailwayTopology Build(IReadOnlyList<TrackSegment> tracks,
        IReadOnlyList<RailwayStation> stations, GeoBoundingBox regionBounds)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        ArgumentNullException.ThrowIfNull(stations);
        ArgumentNullException.ThrowIfNull(regionBounds);
        var ordered = tracks.OrderBy(track => track.Id, StringComparer.Ordinal).ToArray();
        if (ordered.Select(track => track.Id).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
            throw new ArgumentException("Track ids must be unique.", nameof(tracks));
        var resolutions = ResolveGauges(ordered);
        var networks = resolutions.Where(item => item.Gauge != TrackGauge.Unknown)
            .Select(item => item.Gauge).Distinct().OrderBy(gauge => gauge.WidthMillimetres)
            .Select(gauge => BuildNetwork(gauge, ordered, resolutions)).ToArray();
        var summaries = networks.Select(Summarize).ToArray();
        var diagnostics = Diagnose(networks, resolutions, ordered, stations, regionBounds);
        return new RailwayTopology(networks, resolutions, summaries, diagnostics);
    }

    private static GaugeResolution[] ResolveGauges(TrackSegment[] tracks)
    {
        var owners = new Dictionary<GeoCoordinate, int>();
        // A known track must never inherit a gauge through another known track.
        // Collect only gauges directly touching each unknown connected group.
        var unknownDsu = new DisjointSet(tracks.Length);
        for (var i = 0; i < tracks.Length; i++)
            if (tracks[i].Gauge == TrackGauge.Unknown)
                foreach (var point in tracks[i].Geometry)
                {
                    if (owners.TryGetValue(point, out var owner)) unknownDsu.Union(i, owner);
                    else owners.Add(point, i);
                }
        var adjacent = new Dictionary<int, HashSet<TrackGauge>>();
        for (var i = 0; i < tracks.Length; i++)
            if (tracks[i].Gauge == TrackGauge.Unknown)
                adjacent.TryAdd(unknownDsu.Find(i), []);
        for (var i = 0; i < tracks.Length; i++)
            if (tracks[i].Gauge != TrackGauge.Unknown)
                foreach (var point in tracks[i].Geometry)
                    if (owners.TryGetValue(point, out var unknownIndex))
                        adjacent[unknownDsu.Find(unknownIndex)].Add(tracks[i].Gauge);
        var result = new GaugeResolution[tracks.Length];
        for (var i = 0; i < tracks.Length; i++)
        {
            var track = tracks[i];
            if (track.Gauge != TrackGauge.Unknown)
                result[i] = new(track.Id, track.Gauge,
                    track.GaugeInferred ? GaugeSource.InferredFromTags : GaugeSource.Declared);
            else
            {
                var set = adjacent[unknownDsu.Find(i)];
                result[i] = set.Count == 1
                    ? new(track.Id, set.Single(), GaugeSource.InferredFromConnection)
                    : new(track.Id, TrackGauge.Unknown, GaugeSource.Unknown);
            }
        }
        return result;
    }

    private static RailwayNetwork BuildNetwork(TrackGauge gauge, TrackSegment[] tracks,
        GaugeResolution[] resolutions)
    {
        var included = Enumerable.Range(0, tracks.Length).Where(i => resolutions[i].Gauge == gauge).ToArray();
        var occurrences = new Dictionary<GeoCoordinate, int>();
        var endpoints = new HashSet<GeoCoordinate>();
        foreach (var index in included)
        {
            var points = tracks[index].Geometry;
            endpoints.Add(points[0]);
            endpoints.Add(points[^1]);
            foreach (var point in points) occurrences[point] = occurrences.GetValueOrDefault(point) + 1;
        }
        var locations = occurrences.Keys.Where(point => endpoints.Contains(point) || occurrences[point] > 1)
            .OrderBy(point => point.Latitude).ThenBy(point => point.Longitude).ToArray();
        var nodeIndex = locations.Select((point, i) => (point, i)).ToDictionary(x => x.point, x => x.i);
        var pieces = new List<(string Id, TrackSegment Track, GeoCoordinate[] Geometry, GaugeSource Source, int Start, int End)>();
        foreach (var index in included)
        {
            var track = tracks[index];
            var start = 0;
            var number = 0;
            for (var i = 1; i < track.Geometry.Count; i++)
            {
                if (!nodeIndex.ContainsKey(track.Geometry[i])) continue;
                var geometry = track.Geometry.Skip(start).Take(i - start + 1).ToArray();
                pieces.Add(($"{track.Id}@{++number}", track, geometry, resolutions[index].Source,
                    nodeIndex[geometry[0]], nodeIndex[geometry[^1]]));
                start = i;
            }
        }
        pieces.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
        var dsu = new DisjointSet(locations.Length);
        foreach (var piece in pieces) dsu.Union(piece.Start, piece.End);
        var components = dsu.Roots().Distinct().Order().Select((root, index) => (root, index))
            .ToDictionary(x => x.root, x => x.index);
        var edges = pieces.Select(piece => new NetworkEdge(piece.Id, piece.Track,
            Array.AsReadOnly(piece.Geometry),
            NetworkGeometry.Length(piece.Geometry), piece.Source, piece.Start, piece.End,
            components[dsu.Find(piece.Start)])).ToArray();
        var legs = Enumerable.Range(0, locations.Length).Select(_ => new List<NetworkLegAtNode>()).ToArray();
        for (var i = 0; i < edges.Length; i++)
        {
            var edge = edges[i];
            var lookahead = Math.Min(NetworkRules.BearingLookaheadMeters, edge.LengthMeters);
            legs[edge.StartNode].Add(new NetworkLegAtNode(i, true,
                NetworkGeometry.Bearing(locations[edge.StartNode], NetworkGeometry.At(edge.Geometry, lookahead))));
            var backward = edge.Geometry.Reverse().ToArray();
            legs[edge.EndNode].Add(new NetworkLegAtNode(i, false,
                NetworkGeometry.Bearing(locations[edge.EndNode], NetworkGeometry.At(backward, lookahead))));
        }
        var nodes = locations.Select((point, i) => new NetworkNode(point,
            Array.AsReadOnly(legs[i].ToArray()))).ToArray();
        return new RailwayNetwork(gauge, nodes, edges);
    }

    private static NetworkSummary Summarize(RailwayNetwork network)
    {
        var lengths = network.Edges.GroupBy(edge => edge.Component)
            .Select(group => group.Sum(edge => edge.LengthMeters)).ToArray();
        return new NetworkSummary(network.Gauge, network.Nodes.Count, network.Edges.Count,
            network.Edges.Sum(edge => edge.LengthMeters), lengths.Length, lengths.DefaultIfEmpty().Max());
    }

    private static NetworkDiagnostic[] Diagnose(RailwayNetwork[] networks,
        GaugeResolution[] resolutions, TrackSegment[] tracks,
        IReadOnlyList<RailwayStation> stations, GeoBoundingBox bounds)
    {
        var result = new List<NetworkDiagnostic>();
        foreach (var network in networks)
        {
            var boxes = network.Edges.Select(edge => (
                South: edge.Geometry.Min(point => point.Latitude),
                North: edge.Geometry.Max(point => point.Latitude),
                West: edge.Geometry.Min(point => point.Longitude),
                East: edge.Geometry.Max(point => point.Longitude))).ToArray();
            foreach (var node in network.Nodes)
            {
                if (node.Degree == 1)
                {
                    var leg = node.Legs[0];
                    var edge = network.Edges[leg.EdgeIndex];
                    var point = node.Location;
                    if (Math.Abs(point.Latitude - bounds.South) <= NetworkRules.BoundaryToleranceDegrees
                        || Math.Abs(point.Latitude - bounds.North) <= NetworkRules.BoundaryToleranceDegrees
                        || Math.Abs(point.Longitude - bounds.West) <= NetworkRules.BoundaryToleranceDegrees
                        || Math.Abs(point.Longitude - bounds.East) <= NetworkRules.BoundaryToleranceDegrees)
                    {
                        result.Add(new(NetworkDiagnosticKind.RegionBoundary, network.Gauge.WidthMillimetres,
                            point, edge.TrackId, null, null, null));
                        continue;
                    }
                    var nearest = double.PositiveInfinity;
                    for (var otherIndex = 0; otherIndex < network.Edges.Count; otherIndex++)
                    {
                        var other = network.Edges[otherIndex];
                        if (other.Component == edge.Component) continue;
                        var box = boxes[otherIndex];
                        var latitudeMargin = NetworkRules.DataGapSearchRadiusMeters / 111_195d;
                        var longitudeMargin = latitudeMargin / Math.Max(0.01, Math.Cos(point.Latitude * Math.PI / 180));
                        if (point.Latitude < box.South - latitudeMargin || point.Latitude > box.North + latitudeMargin
                            || point.Longitude < box.West - longitudeMargin || point.Longitude > box.East + longitudeMargin)
                            continue;
                        nearest = Math.Min(nearest, NetworkGeometry.Project(point, other.Geometry).Distance);
                    }
                    var gap = nearest <= NetworkRules.DataGapSearchRadiusMeters;
                    result.Add(new(gap ? NetworkDiagnosticKind.PossibleDataGap : NetworkDiagnosticKind.EndOfTrack,
                        network.Gauge.WidthMillimetres, point, edge.TrackId, null, gap ? nearest : null, null));
                }
                else if (node.Degree == 2)
                {
                    var deflection = NetworkGeometry.Deflection(node.Legs[0].BearingDegrees,
                        node.Legs[1].BearingDegrees);
                    if (deflection > NetworkRules.MaxDeflectionDegrees)
                    {
                        var trackId = node.Legs.Select(leg => network.Edges[leg.EdgeIndex].TrackId)
                            .Order(StringComparer.Ordinal).First();
                        result.Add(new(NetworkDiagnosticKind.SharpJoint, network.Gauge.WidthMillimetres,
                            node.Location, trackId, null, null, deflection));
                    }
                }
            }
        }
        foreach (var station in stations)
        {
            var nearest = networks.SelectMany(network => network.Edges)
                .Select(edge => NetworkGeometry.Project(station.Location, edge.Geometry).Distance)
                .DefaultIfEmpty(double.PositiveInfinity).Min();
            if (nearest > NetworkRules.StationSnapToleranceMeters)
                result.Add(new(NetworkDiagnosticKind.StationWithoutTrack, null,
                    station.Location, null, station.Id, double.IsFinite(nearest) ? nearest : null, null));
        }
        for (var i = 0; i < tracks.Length; i++)
            if (resolutions[i].Source == GaugeSource.Unknown)
                result.Add(new(NetworkDiagnosticKind.TrackWithoutGauge, null,
                    tracks[i].Geometry[0], tracks[i].Id, null, null, null));
        return result.OrderBy(item => item.Kind).ThenBy(item => item.GaugeMillimetres)
            .ThenBy(item => item.Location.Latitude).ThenBy(item => item.Location.Longitude)
            .ThenBy(item => item.TrackId, StringComparer.Ordinal).ToArray();
    }

    private sealed class DisjointSet(int count)
    {
        private readonly int[] parent = Enumerable.Range(0, count).ToArray();
        public int Find(int index) => parent[index] == index ? index : parent[index] = Find(parent[index]);
        public void Union(int first, int second)
        {
            var a = Find(first);
            var b = Find(second);
            if (a != b) parent[Math.Max(a, b)] = Math.Min(a, b);
        }
        public IEnumerable<int> Roots() => Enumerable.Range(0, parent.Length).Select(Find);
    }
}
