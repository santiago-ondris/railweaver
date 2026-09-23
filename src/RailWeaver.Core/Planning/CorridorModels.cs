using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Planning;

public sealed record CorridorRequest(
    GeoCoordinate Origin,
    GeoCoordinate Destination,
    double MaxGradientPermille,
    GeoBoundingBox SearchLimit);

public enum CorridorStatus { Found, NoFeasiblePath, EndpointWithoutElevation }

public sealed record CorridorSearchInfo(
    double GridStepMeters,
    int GridColumns,
    int GridRows,
    int ExploredNodes,
    GeoBoundingBox Bounds);

public sealed record CorridorResult(CorridorStatus Status, CandidateCorridor? Corridor, CorridorSearchInfo Search);

public sealed record TrackProfilePoint(
    double DistanceMeters,
    GeoCoordinate Coordinate,
    double ElevationMeters,
    double? GradientPermille);

public sealed record GradientBand(double FromPercentOfLimit, double ToPercentOfLimit, double Meters);

public sealed record CorridorMetrics(
    double LengthMeters,
    double StraightLineDistanceMeters,
    double Sinuosity,
    double MaxGradientPermille,
    double MaxGradientLimitPermille,
    double AscentMeters,
    double DescentMeters,
    double MinElevationMeters,
    double MaxElevationMeters,
    IReadOnlyList<GradientBand> DistanceByGradientBand,
    double MaxCutMeters,
    double MaxFillMeters);

public sealed record CandidateCorridor(
    IReadOnlyList<GeoCoordinate> Alignment,
    IReadOnlyList<TrackProfilePoint> TrackProfile,
    ElevationProfile TerrainProfile,
    CorridorMetrics Metrics);
