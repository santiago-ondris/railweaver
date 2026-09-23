using RailWeaver.Core.Infrastructure;

namespace RailWeaver.Api.Railways;

public sealed record RailwayDomainDataset(IReadOnlyList<TrackSegment> Tracks,
    IReadOnlyList<RailwayStation> Stations);

internal sealed record TrackFeatureCollectionDocument
{
    public required string Type { get; init; }

    public required TrackFeatureDocument[] Features { get; init; }
}

internal sealed record TrackFeatureDocument
{
    public required string Type { get; init; }

    public required string Id { get; init; }

    public required TrackPropertiesDocument Properties { get; init; }

    public required LineStringGeometryDocument Geometry { get; init; }
}

internal sealed record TrackPropertiesDocument
{
    public required string Id { get; init; }

    public required int GaugeMillimetres { get; init; }

    public required bool GaugeInferred { get; init; }

    public required string Status { get; init; }

    public required string Usage { get; init; }

    public string? Name { get; init; }

    public string? LineReference { get; init; }
}

internal sealed record LineStringGeometryDocument
{
    public required string Type { get; init; }

    public required double[][] Coordinates { get; init; }
}

internal sealed record StationFeatureCollectionDocument
{
    public required string Type { get; init; }

    public required StationFeatureDocument[] Features { get; init; }
}

internal sealed record StationFeatureDocument
{
    public required string Type { get; init; }

    public required string Id { get; init; }

    public required StationPropertiesDocument Properties { get; init; }

    public required PointGeometryDocument Geometry { get; init; }
}

internal sealed record StationPropertiesDocument
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Type { get; init; }

    public required int GaugeMillimetres { get; init; }

    public required bool GaugeInferred { get; init; }
}

internal sealed record PointGeometryDocument
{
    public required string Type { get; init; }

    public required double[] Coordinates { get; init; }
}

internal sealed record RailwaySourceDocument
{
    public required DateOnly ExtractedOn { get; init; }

    public required string Source { get; init; }

    public required string SourceUrl { get; init; }

    public required string Attribution { get; init; }

    public required string License { get; init; }

    public required string LicenseUrl { get; init; }

    public required string OverpassUrl { get; init; }

    public required string OsmDataTimestamp { get; init; }

    public required string Query { get; init; }

    public required string RawResponse { get; init; }

    public required string[] DiscardRules { get; init; }

    public required RailwayDiscardedDocument Discarded { get; init; }

    public required RailwayCountsDocument Counts { get; init; }

    public required DualGaugeFindingDocument DualGaugeFinding { get; init; }
}

internal sealed record RailwayDiscardedDocument
{
    public required TrackDiscardedDocument Tracks { get; init; }

    public required StationDiscardedDocument Stations { get; init; }
}

internal sealed record TrackDiscardedDocument
{
    public required int Noise { get; init; }

    public required int MissingNodes { get; init; }

    public required int OutsideBounds { get; init; }
}

internal sealed record StationDiscardedDocument
{
    public required int Unnamed { get; init; }

    public required int OutsideBounds { get; init; }
}

internal sealed record RailwayCountsDocument
{
    public required int Tracks { get; init; }

    public required int Stations { get; init; }
}

internal sealed record DualGaugeFindingDocument
{
    public required string Summary { get; init; }

    public required int DualGaugeWayCount { get; init; }

    public required long[] OsmWayIds { get; init; }

    public required string CordobaMitreEvidence { get; init; }

    public required long[] CordobaMitreWayIds { get; init; }
}

public sealed record RailwayResponse(
    RailwayDatasetSourceResponse Source,
    IReadOnlyList<TrackSegmentResponse> Tracks,
    IReadOnlyList<RailwayStationResponse> Stations);

public sealed record RailwayDatasetSourceResponse(
    DateOnly ExtractedOn,
    string OsmDataTimestamp,
    string Attribution,
    string License,
    string LicenseUrl);

public sealed record TrackSegmentResponse(
    string Id,
    IReadOnlyList<GeoCoordinateResponse> Geometry,
    TrackGaugeResponse Gauge,
    bool GaugeInferred,
    string Status,
    string Usage,
    string? Name,
    string? LineReference);

public sealed record RailwayStationResponse(
    string Id,
    string Name,
    GeoCoordinateResponse Location,
    string Type,
    TrackGaugeResponse Gauge,
    bool GaugeInferred);

public sealed record TrackGaugeResponse(int WidthMillimetres, string Kind);

public sealed record GeoCoordinateResponse(double Latitude, double Longitude);
