using RailWeaver.Core.Geography;
using RailWeaver.Core.Infrastructure;
using RailWeaver.Core.Infrastructure.Network;

namespace RailWeaver.Core.Tests.Infrastructure;

public sealed class NetworkTests
{
    private static readonly GeoBoundingBox Bounds = new(-1, -1, 1, 1);
    private static GeoCoordinate P(double latitude, double longitude) => new(latitude, longitude);
    private static TrackSegment Track(string id, TrackGauge gauge, params GeoCoordinate[] points) =>
        new(id, points, gauge, false, TrackOperationalStatus.Active, TrackUsage.MainLine, null, null);
    private static RailwayStation Station(string id, GeoCoordinate point) =>
        new(id, id, point, StationType.Station, TrackGauge.Unknown, false);

    [Fact]
    public void TConnection_SplitsPassingTrackAndCountsNodeLegs()
    {
        var tracks = new[]
        {
            Track("pass", TrackGauge.Broad, P(0, -.01), P(0, 0), P(0, .01)),
            Track("branch", TrackGauge.Broad, P(0, 0), P(.01, 0)),
        };
        var network = Assert.Single(RailwayNetworkBuilder.Build(tracks, [], Bounds).Networks);
        Assert.Equal(3, network.Edges.Count);
        Assert.Equal(3, Assert.Single(network.Nodes, node => node.Location == P(0, 0)).Degree);
        Assert.All(network.Edges, edge => Assert.True(edge.TrackId is "pass" or "branch"));
    }

    [Fact]
    public void CrossedLinesWithoutSharedPoint_DoNotConnect()
    {
        var tracks = new[]
        {
            Track("a", TrackGauge.Broad, P(0, -.01), P(0, .01)),
            Track("b", TrackGauge.Broad, P(-.01, 0), P(.01, 0)),
        };
        var origin = Station("west", P(0, -.01));
        var destination = Station("north", P(.01, 0));
        var topology = RailwayNetworkBuilder.Build(tracks, [origin, destination], Bounds);
        Assert.Equal(2, topology.Summaries[0].Components);
        var result = new NetworkRouteFinder(topology).Find(new(origin, destination, false), TestContext.Current.CancellationToken);
        Assert.Equal(NetworkUnreachableReason.Disconnected, result.Reason);
    }

    [Fact]
    public void DiamondCrossing_DoesNotAllowPerpendicularMovement()
    {
        var tracks = new[]
        {
            Track("west", TrackGauge.Broad, P(0, -.01), P(0, 0)),
            Track("east", TrackGauge.Broad, P(0, 0), P(0, .01)),
            Track("south", TrackGauge.Broad, P(-.01, 0), P(0, 0)),
            Track("north", TrackGauge.Broad, P(0, 0), P(.01, 0)),
        };
        var origin = Station("west", P(0, -.01));
        var destination = Station("north", P(.01, 0));
        var topology = RailwayNetworkBuilder.Build(tracks, [origin, destination], Bounds);
        var result = new NetworkRouteFinder(topology).Find(new(origin, destination, false), TestContext.Current.CancellationToken);
        Assert.Equal(NetworkUnreachableReason.NoFeasibleMovement, result.Reason);
    }

    [Fact]
    public void Turnout_RequiresOneReversalBetweenBranches()
    {
        var tracks = new[]
        {
            Track("trunk", TrackGauge.Broad, P(0, -.01), P(0, 0)),
            Track("direct", TrackGauge.Broad, P(0, 0), P(0, .01)),
            Track("diverge", TrackGauge.Broad, P(0, 0), P(.003, .01)),
        };
        var direct = Station("direct", P(0, .01));
        var diverge = Station("diverge", P(.003, .01));
        var trunk = Station("trunk", P(0, -.01));
        var topology = RailwayNetworkBuilder.Build(tracks, [direct, diverge, trunk], Bounds);
        var finder = new NetworkRouteFinder(topology);
        var straight = finder.Find(new(trunk, direct, false), TestContext.Current.CancellationToken);
        Assert.Equal(NetworkRouteStatus.Found, straight.Status);
        Assert.Empty(straight.Route!.Reversals);
        var reversed = finder.Find(new(direct, diverge, false), TestContext.Current.CancellationToken);
        Assert.Equal(NetworkRouteStatus.Found, reversed.Status);
        Assert.Single(reversed.Route!.Reversals);
        Assert.Equal(P(0, 0), reversed.Route.Reversals[0].Location);
    }

    [Fact]
    public void GaugeInference_OnlyUsesUnknownGroupDirectContacts()
    {
        var tracks = new[]
        {
            Track("broad", TrackGauge.Broad, P(0, 0), P(0, .01)),
            Track("unknown", TrackGauge.Unknown, P(0, .01), P(0, .02)),
            Track("metre", TrackGauge.Metre, P(.01, .01), P(.01, .02)),
        };
        var topology = RailwayNetworkBuilder.Build(tracks, [], Bounds);
        Assert.Equal(GaugeSource.InferredFromConnection,
            Assert.Single(topology.GaugeResolutions, resolution => resolution.TrackId == "unknown").Source);
        Assert.Equal(TrackGauge.Broad,
            Assert.Single(topology.GaugeResolutions, resolution => resolution.TrackId == "unknown").Gauge);
        var conflicting = tracks.Append(Track("contact", TrackGauge.Metre, P(0, .02), P(0, .03))).ToArray();
        var unresolved = RailwayNetworkBuilder.Build(conflicting, [], Bounds);
        Assert.Equal(GaugeSource.Unknown,
            Assert.Single(unresolved.GaugeResolutions, resolution => resolution.TrackId == "unknown").Source);
    }

    [Fact]
    public void DifferentGauges_NeverShareARoute()
    {
        var origin = Station("metre", P(0, -.01));
        var destination = Station("broad", P(0, .01));
        var tracks = new[]
        {
            Track("metre", TrackGauge.Metre, P(0, -.01), P(0, 0)),
            Track("broad", TrackGauge.Broad, P(0, 0), P(0, .01)),
        };
        var result = new NetworkRouteFinder(RailwayNetworkBuilder.Build(tracks, [origin, destination], Bounds))
            .Find(new(origin, destination, false), TestContext.Current.CancellationToken);
        Assert.Equal(NetworkUnreachableReason.NoCommonGauge, result.Reason);
    }

    [Fact]
    public void DisusedRequiresOptInAndAbandonedIsNeverAvailable()
    {
        var origin = Station("origin", P(0, -.01));
        var destination = Station("destination", P(0, .01));
        var active = Track("active", TrackGauge.Broad, P(0, -.01), P(0, 0));
        var disused = new TrackSegment("disused", [P(0, 0), P(0, .01)], TrackGauge.Broad,
            false, TrackOperationalStatus.Disused, TrackUsage.MainLine, null, null);
        var topology = RailwayNetworkBuilder.Build([active, disused], [origin, destination], Bounds);
        var finder = new NetworkRouteFinder(topology);
        var closed = finder.Find(new(origin, destination, false), TestContext.Current.CancellationToken);
        Assert.Equal(NetworkRouteStatus.Unreachable, closed.Status);
        Assert.True(closed.ReachableWithDisused);
        var opened = finder.Find(new(origin, destination, true), TestContext.Current.CancellationToken);
        Assert.Equal(NetworkRouteStatus.Found, opened.Status);
        Assert.True(opened.Route!.DistanceByStatus.Disused > 0);
        var abandoned = new TrackSegment("disused", [P(0, 0), P(0, .01)], TrackGauge.Broad,
            false, TrackOperationalStatus.Abandoned, TrackUsage.MainLine, null, null);
        var impossible = new NetworkRouteFinder(RailwayNetworkBuilder.Build(
            [active, abandoned], [origin, destination], Bounds))
            .Find(new(origin, destination, true), TestContext.Current.CancellationToken);
        Assert.Equal(NetworkRouteStatus.Unreachable, impossible.Status);
    }

    [Fact]
    public void StationCanStopOnMultipleEdges()
    {
        var tracks = new[]
        {
            Track("one", TrackGauge.Broad, P(0, -.01), P(0, .01)),
            Track("two", TrackGauge.Broad, P(.0005, -.01), P(.0005, .01)),
            Track("join", TrackGauge.Broad, P(0, .01), P(.0005, .01)),
        };
        var origin = Station("origin", P(.00025, -.009));
        var destination = Station("destination", P(.0005, .009));
        var result = new NetworkRouteFinder(RailwayNetworkBuilder.Build(tracks, [origin, destination], Bounds))
            .Find(new(origin, destination, false), TestContext.Current.CancellationToken);
        Assert.Equal(NetworkRouteStatus.Found, result.Status);
        Assert.Equal("two", result.Route!.OriginStop.TrackId);
    }

    [Fact]
    public void ShuffledInput_ProducesIdenticalTopologyAndDiagnostics()
    {
        var tracks = new[]
        {
            Track("b", TrackGauge.Broad, P(0, 0), P(0, .01)),
            Track("a", TrackGauge.Broad, P(0, -.01), P(0, 0)),
        };
        var first = RailwayNetworkBuilder.Build(tracks, [], Bounds);
        var second = RailwayNetworkBuilder.Build(tracks.Reverse().ToArray(), [], Bounds);
        Assert.Equal(first.Networks[0].Edges.Select(edge => edge.Id),
            second.Networks[0].Edges.Select(edge => edge.Id));
        Assert.Equal(first.Diagnostics, second.Diagnostics);
    }

    [Fact]
    public void SameStationIsRejected()
    {
        var station = Station("same", P(0, 0));
        var topology = RailwayNetworkBuilder.Build([], [station], Bounds);
        Assert.Throws<ArgumentException>(() => new NetworkRouteFinder(topology).Find(
            new(station, station, false), TestContext.Current.CancellationToken));
    }
}
