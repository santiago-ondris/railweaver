using RailWeaver.Core.Geography;
using RailWeaver.Core.Infrastructure;

namespace RailWeaver.Core.Planning;

public sealed record CorridorRequest(
    GeoCoordinate Origin,
    GeoCoordinate Destination,
    double MaxGradientPermille,
    TrackGauge Gauge,
    double DesignSpeedKmh,
    GeoBoundingBox SearchLimit);

public enum CorridorStatus { Found, NoFeasiblePath, EndpointWithoutElevation }

public sealed record CorridorSearchInfo(
    double GridStepMeters,
    int GridColumns,
    int GridRows,
    int ExploredStates,
    GeoBoundingBox Bounds);

public sealed record CorridorResult(CorridorStatus Status, CandidateCorridor? Corridor,
    CorridorSearchInfo Search, bool? FeasibleWithoutCurveLimit);

public sealed record TrackProfilePoint(
    double DistanceMeters,
    GeoCoordinate Coordinate,
    double ElevationMeters,
    double? GradientPermille,
    double? GradientLimitPermille);

public enum SectionKind { Tangent, Curve }
public enum TurnDirection { Left, Right }
public sealed record AlignmentSection(SectionKind Kind, double FromMeters, double ToMeters,
    double? RadiusMeters, double? DeflectionDegrees, TurnDirection? Direction,
    double SpeedLimitKmh, GeoCoordinate? CurveMidpoint);

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
    double MaxFillMeters,
    TrackGauge Gauge,
    double DesignSpeedKmh,
    double DesignRadiusMeters,
    double AbsoluteMinimumRadiusMeters,
    int CurveCount,
    int ReducedSpeedCurveCount,
    double? MinimumRadiusMeters,
    double MinimumSpeedKmh,
    double CurveLengthMeters);

public sealed record CandidateCorridor(
    IReadOnlyList<GeoCoordinate> Alignment,
    IReadOnlyList<AlignmentSection> Sections,
    IReadOnlyList<TrackProfilePoint> TrackProfile,
    ElevationProfile TerrainProfile,
    CorridorMetrics Metrics);
