using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Infrastructure.Network;

public enum GaugeSource { Declared, InferredFromTags, InferredFromConnection, Unknown }
public enum NetworkDiagnosticKind { RegionBoundary, PossibleDataGap, EndOfTrack, SharpJoint, StationWithoutTrack, TrackWithoutGauge }
public enum NetworkRouteStatus { Found, Unreachable }
public enum NetworkUnreachableReason { OriginWithoutTrack, DestinationWithoutTrack, NoCommonGauge, Disconnected, NoFeasibleMovement }
public enum NetworkDirection { Forward, Backward }

public sealed record GaugeResolution(string TrackId, TrackGauge Gauge, GaugeSource Source);
public sealed record NetworkLeg(string EdgeId, string TrackId, NetworkDirection Direction, double FromOffsetMeters, double ToOffsetMeters);
public sealed record NetworkReversal(GeoCoordinate Location, double DistanceAlongMeters,
    double ManeuverTrackMeters);
public sealed record NetworkStop(string EdgeId, string TrackId, double OffsetMeters, GeoCoordinate Location);
public sealed record NetworkSegment(string TrackId, string? Name, string? LineReference, TrackOperationalStatus Status, double LengthMeters);
public sealed record NetworkDistanceByStatus(double Active, double Disused);
public sealed record NetworkDiagnostic(NetworkDiagnosticKind Kind, int? GaugeMillimetres, GeoCoordinate Location,
    string? TrackId, string? StationId, double? DistanceMeters, double? DeflectionDegrees);
public sealed record NetworkSummary(TrackGauge Gauge, int Nodes, int Edges, double LengthMeters,
    int Components, double LargestComponentLengthMeters);
public sealed record NetworkRouteRequest(RailwayStation Origin, RailwayStation Destination, bool IncludeDisused);
public sealed record NetworkRoute(
    TrackGauge Gauge, IReadOnlyList<NetworkLeg> Legs, IReadOnlyList<GeoCoordinate> Geometry,
    double LengthMeters, double StraightLineDistanceMeters, IReadOnlyList<NetworkReversal> Reversals,
    NetworkDistanceByStatus DistanceByStatus, double InferredGaugeDistanceMeters,
    IReadOnlyList<NetworkSegment> Segments, NetworkStop OriginStop, NetworkStop DestinationStop);
public sealed record NetworkRouteResult(
    NetworkRouteStatus Status, NetworkRoute? Route, NetworkUnreachableReason? Reason,
    IReadOnlyList<int> OriginGauges, IReadOnlyList<int> DestinationGauges, bool? ReachableWithDisused);

public sealed record NetworkLegAtNode(int EdgeIndex, bool AtStart, double BearingDegrees);
public sealed record NetworkNode(GeoCoordinate Location, IReadOnlyList<NetworkLegAtNode> Legs)
{
    public int Degree => Legs.Count;
}

public sealed record NetworkEdge(
    string Id, TrackSegment Track, IReadOnlyList<GeoCoordinate> Geometry, double LengthMeters,
    GaugeSource GaugeSource, int StartNode, int EndNode, int Component)
{
    public TrackOperationalStatus Status => Track.Status;
    public string TrackId => Track.Id;
}

public sealed class RailwayNetwork(
    TrackGauge gauge, IReadOnlyList<NetworkNode> nodes, IReadOnlyList<NetworkEdge> edges)
{
    public TrackGauge Gauge { get; } = gauge;
    public IReadOnlyList<NetworkNode> Nodes { get; } = Array.AsReadOnly(nodes.ToArray());
    public IReadOnlyList<NetworkEdge> Edges { get; } = Array.AsReadOnly(edges.ToArray());
}

public sealed class RailwayTopology(
    IReadOnlyList<RailwayNetwork> networks,
    IReadOnlyList<GaugeResolution> gaugeResolutions,
    IReadOnlyList<NetworkSummary> summaries,
    IReadOnlyList<NetworkDiagnostic> diagnostics)
{
    public IReadOnlyList<RailwayNetwork> Networks { get; } = Array.AsReadOnly(networks.ToArray());
    public IReadOnlyList<GaugeResolution> GaugeResolutions { get; } = Array.AsReadOnly(gaugeResolutions.ToArray());
    public IReadOnlyList<NetworkSummary> Summaries { get; } = Array.AsReadOnly(summaries.ToArray());
    public IReadOnlyList<NetworkDiagnostic> Diagnostics { get; } = Array.AsReadOnly(diagnostics.ToArray());
}
