using RailWeaver.Api.Elevation;
using RailWeaver.Api.Regions;
using RailWeaver.Core.Planning;

namespace RailWeaver.Api.Planning;

public sealed record CorridorCoordinateRequest(double? Latitude, double? Longitude);
public sealed record CorridorRequestBody(CorridorCoordinateRequest? Origin,
    CorridorCoordinateRequest? Destination, double? MaxGradientPermille,
    int? GaugeMillimetres, double? DesignSpeedKmh);

public sealed record CorridorResponse(string Status, CorridorSearchResponse Search,
    CandidateCorridorResponse? Corridor, bool? FeasibleWithoutCurveLimit)
{
    public static CorridorResponse FromDomain(CorridorResult result)
    {
        var search = result.Search;
        var searchResponse = new CorridorSearchResponse(search.GridStepMeters, search.GridColumns,
            search.GridRows, search.ExploredStates,
            new BoundingBoxResponse(search.Bounds.West, search.Bounds.South, search.Bounds.East, search.Bounds.North));
        if (result.Corridor is not { } corridor)
            return new CorridorResponse(result.Status switch
            {
                CorridorStatus.NoFeasiblePath => "no_feasible_path",
                CorridorStatus.EndpointWithoutElevation => "endpoint_without_elevation",
                _ => throw new InvalidOperationException("A found result requires a corridor."),
            }, searchResponse, null, result.FeasibleWithoutCurveLimit);

        static double Meters(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
        static double Gradient(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
        static double Decimal(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
        var metrics = corridor.Metrics;
        return new CorridorResponse("found", searchResponse, new CandidateCorridorResponse(
            corridor.Alignment.Select(point => new CoordinateRequest(point.Latitude, point.Longitude)).ToArray(),
            corridor.Sections.Select(section => new AlignmentSectionResponse(
                section.Kind == SectionKind.Curve ? "curve" : "tangent",
                Meters(section.FromMeters), Meters(section.ToMeters),
                section.RadiusMeters is { } radius ? Meters(radius) : null,
                section.DeflectionDegrees is { } deflection ? Decimal(deflection) : null,
                section.Direction?.ToString().ToLowerInvariant(), Decimal(section.SpeedLimitKmh),
                section.CurveMidpoint is { } midpoint ? new CorridorCoordinateRequest(midpoint.Latitude, midpoint.Longitude) : null)).ToArray(),
            corridor.TrackProfile.Select(point => new TrackProfilePointResponse(
                Meters(point.DistanceMeters), point.Coordinate.Latitude, point.Coordinate.Longitude,
                Meters(point.ElevationMeters), point.GradientPermille is { } gradient ? Gradient(gradient) : null,
                point.GradientLimitPermille is { } gradientLimit ? Gradient(gradientLimit) : null)).ToArray(),
            new ProfileResponse(Meters(corridor.TerrainProfile.TotalDistanceMeters),
                corridor.TerrainProfile.Samples.Select(sample => new ProfileSampleResponse(
                    Meters(sample.DistanceMeters),
                    new CoordinateRequest(sample.Coordinate.Latitude, sample.Coordinate.Longitude),
                    sample.ElevationMeters is { } elevation ? Meters(elevation) : null,
                    sample.GradientPermille is { } gradient ? Gradient(gradient) : null)).ToArray()),
            new CorridorMetricsResponse(Meters(metrics.LengthMeters), Meters(metrics.StraightLineDistanceMeters),
                Math.Round(metrics.Sinuosity, 3, MidpointRounding.AwayFromZero),
                Gradient(metrics.MaxGradientPermille), Gradient(metrics.MaxGradientLimitPermille),
                Meters(metrics.AscentMeters), Meters(metrics.DescentMeters),
                Meters(metrics.MinElevationMeters), Meters(metrics.MaxElevationMeters),
                metrics.DistanceByGradientBand.Select(band => new GradientBandResponse(
                    band.FromPercentOfLimit, band.ToPercentOfLimit, Meters(band.Meters))).ToArray(),
                Meters(metrics.MaxCutMeters), Meters(metrics.MaxFillMeters),
                metrics.Gauge.WidthMillimetres, Decimal(metrics.DesignSpeedKmh), Meters(metrics.DesignRadiusMeters),
                Meters(metrics.AbsoluteMinimumRadiusMeters), metrics.CurveCount, metrics.ReducedSpeedCurveCount,
                metrics.MinimumRadiusMeters is { } minimum ? Meters(minimum) : null,
                Decimal(metrics.MinimumSpeedKmh), Meters(metrics.CurveLengthMeters))), null);
    }
}

public sealed record CorridorSearchResponse(double GridStepMeters, int GridColumns, int GridRows,
    int ExploredStates, BoundingBoxResponse Bounds);
public sealed record AlignmentSectionResponse(string Kind, double FromMeters, double ToMeters,
    double? RadiusMeters, double? DeflectionDegrees, string? Direction, double SpeedLimitKmh,
    CorridorCoordinateRequest? CurveMidpoint);
public sealed record CandidateCorridorResponse(IReadOnlyList<CoordinateRequest> Alignment,
    IReadOnlyList<AlignmentSectionResponse> Sections, IReadOnlyList<TrackProfilePointResponse> TrackProfile,
    ProfileResponse TerrainProfile, CorridorMetricsResponse Metrics);
public sealed record TrackProfilePointResponse(double DistanceMeters, double Latitude, double Longitude,
    double ElevationMeters, double? GradientPermille, double? GradientLimitPermille);
public sealed record GradientBandResponse(double FromPercentOfLimit, double ToPercentOfLimit, double Meters);
public sealed record CorridorMetricsResponse(double LengthMeters, double StraightLineDistanceMeters,
    double Sinuosity, double MaxGradientPermille, double MaxGradientLimitPermille,
    double AscentMeters, double DescentMeters, double MinElevationMeters, double MaxElevationMeters,
    IReadOnlyList<GradientBandResponse> DistanceByGradientBand, double MaxCutMeters, double MaxFillMeters,
    int GaugeMillimetres, double DesignSpeedKmh, double DesignRadiusMeters,
    double AbsoluteMinimumRadiusMeters, int CurveCount, int ReducedSpeedCurveCount,
    double? MinimumRadiusMeters, double MinimumSpeedKmh, double CurveLengthMeters);
