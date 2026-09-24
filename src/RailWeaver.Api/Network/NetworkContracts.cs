using System.Text.Json.Serialization;
using RailWeaver.Api.Elevation;
using RailWeaver.Api.Railways;
using RailWeaver.Core.Infrastructure.Network;

namespace RailWeaver.Api.Network;

public sealed record NetworkRouteRequestBody(string? OriginStationId, string? DestinationStationId,
    bool IncludeDisused);

public sealed record NetworkResponse(IReadOnlyList<object> Networks,
    IReadOnlyList<object> InferredGauges, IReadOnlyList<object> Diagnostics)
{
    public static NetworkResponse FromDomain(RailwayTopology topology)
    {
        static double Meters(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return new NetworkResponse(
            topology.Summaries.Select(summary => (object)new
            {
                gauge = new TrackGaugeResponse(summary.Gauge.WidthMillimetres, summary.Gauge.Kind.ToString()),
                nodes = summary.Nodes, edges = summary.Edges,
                lengthMeters = Meters(summary.LengthMeters), components = summary.Components,
                largestComponentLengthMeters = Meters(summary.LargestComponentLengthMeters),
            }).ToArray(),
            topology.GaugeResolutions.Where(item => item.Source == GaugeSource.InferredFromConnection)
                .Select(item => (object)new { trackId = item.TrackId,
                    widthMillimetres = item.Gauge.WidthMillimetres }).ToArray(),
            topology.Diagnostics.Select(item => (object)new
            {
                kind = Snake(item.Kind.ToString()), gaugeMillimetres = item.GaugeMillimetres,
                location = new GeoCoordinateResponse(item.Location.Latitude, item.Location.Longitude),
                trackId = item.TrackId, stationId = item.StationId,
                distanceMeters = item.DistanceMeters is { } distance ? Meters(distance) : (double?)null,
                deflectionDegrees = item.DeflectionDegrees is { } angle ? Math.Round(angle, 1,
                    MidpointRounding.AwayFromZero) : (double?)null,
            }).ToArray());
    }

    public static string Snake(string name) => string.Concat(name.Select((character, index) =>
        index > 0 && char.IsUpper(character) ? "_" + char.ToLowerInvariant(character)
            : char.ToLowerInvariant(character).ToString()));
}

public sealed record NetworkRouteResponse
{
    public required string Status { get; init; }
    public required bool IncludeDisused { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Route { get; init; }
    public ProfileResponse? Profile { get; init; }
    public string? ProfileUnavailableReason { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? OriginGauges { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? DestinationGauges { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ReachableWithDisused { get; init; }

    public static NetworkRouteResponse FromDomain(NetworkRouteResult result, bool includeDisused,
        ProfileResponse? profile, string? unavailableReason)
    {
        if (result.Route is not { } route)
            return new NetworkRouteResponse
            {
                Status = "unreachable", IncludeDisused = includeDisused,
                Reason = NetworkResponse.Snake(result.Reason!.Value.ToString()),
                OriginGauges = result.OriginGauges, DestinationGauges = result.DestinationGauges,
                ReachableWithDisused = result.ReachableWithDisused,
            };
        static double Meters(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
        static object Point(Core.Geography.GeoCoordinate point) =>
            new GeoCoordinateResponse(point.Latitude, point.Longitude);
        return new NetworkRouteResponse
        {
            Status = "found", IncludeDisused = includeDisused, Profile = profile,
            ProfileUnavailableReason = unavailableReason,
            Route = new
            {
                gauge = new TrackGaugeResponse(route.Gauge.WidthMillimetres, route.Gauge.Kind.ToString()),
                legs = route.Legs.Select(leg => new
                {
                    edgeId = leg.EdgeId, trackId = leg.TrackId,
                    direction = leg.Direction.ToString(),
                    fromOffsetMeters = Meters(leg.FromOffsetMeters), toOffsetMeters = Meters(leg.ToOffsetMeters),
                }).ToArray(),
                geometry = route.Geometry.Select(Point).ToArray(),
                lengthMeters = Meters(route.LengthMeters),
                straightLineDistanceMeters = Meters(route.StraightLineDistanceMeters),
                reversals = route.Reversals.Select(reversal => new
                {
                    location = Point(reversal.Location),
                    distanceAlongMeters = Meters(reversal.DistanceAlongMeters),
                    maneuverTrackMeters = Meters(reversal.ManeuverTrackMeters),
                }).ToArray(),
                distanceByStatus = new
                {
                    active = Meters(route.DistanceByStatus.Active),
                    disused = Meters(route.DistanceByStatus.Disused),
                },
                inferredGaugeDistanceMeters = Meters(route.InferredGaugeDistanceMeters),
                segments = route.Segments.Select(segment => new
                {
                    trackId = segment.TrackId, name = segment.Name,
                    lineReference = segment.LineReference, status = segment.Status.ToString(),
                    lengthMeters = Meters(segment.LengthMeters),
                }).ToArray(),
                originStop = new { trackId = route.OriginStop.TrackId,
                    location = Point(route.OriginStop.Location) },
                destinationStop = new { trackId = route.DestinationStop.TrackId,
                    location = Point(route.DestinationStop.Location) },
            },
        };
    }
}
