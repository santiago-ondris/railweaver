using RailWeaver.Api.Elevation;
using RailWeaver.Api.Regions;
using RailWeaver.Core.Planning;

namespace RailWeaver.Api.Planning;

public sealed record CorridorCoordinateRequest(double? Latitude, double? Longitude);
public sealed record CorridorRequestBody(
    CorridorCoordinateRequest? Origin,
    CorridorCoordinateRequest? Destination,
    double? MaxGradientPermille);

public sealed record CorridorResponse(string Status, CorridorSearchResponse Search, CandidateCorridorResponse? Corridor)
{
    public static CorridorResponse FromDomain(CorridorResult result)
    {
        var search = result.Search;
        var searchResponse = new CorridorSearchResponse(search.GridStepMeters, search.GridColumns,
            search.GridRows, search.ExploredNodes,
            new BoundingBoxResponse(search.Bounds.West, search.Bounds.South, search.Bounds.East, search.Bounds.North));
        if (result.Corridor is not { } corridor)
            return new CorridorResponse(result.Status switch
            {
                CorridorStatus.NoFeasiblePath => "no_feasible_path",
                CorridorStatus.EndpointWithoutElevation => "endpoint_without_elevation",
                _ => throw new InvalidOperationException("A found result requires a corridor."),
            }, searchResponse, null);

        static double Meters(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
        static double Gradient(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
        var metrics = corridor.Metrics;
        return new CorridorResponse("found", searchResponse, new CandidateCorridorResponse(
            corridor.Alignment.Select(point => new CoordinateRequest(point.Latitude, point.Longitude)).ToArray(),
            corridor.TrackProfile.Select(point => new TrackProfilePointResponse(
                Meters(point.DistanceMeters), point.Coordinate.Latitude, point.Coordinate.Longitude,
                Meters(point.ElevationMeters), point.GradientPermille is { } gradient ? Gradient(gradient) : null)).ToArray(),
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
                Meters(metrics.MaxCutMeters), Meters(metrics.MaxFillMeters))));
    }
}

public sealed record CorridorSearchResponse(double GridStepMeters, int GridColumns, int GridRows,
    int ExploredNodes, BoundingBoxResponse Bounds);
public sealed record CandidateCorridorResponse(IReadOnlyList<CoordinateRequest> Alignment,
    IReadOnlyList<TrackProfilePointResponse> TrackProfile, ProfileResponse TerrainProfile,
    CorridorMetricsResponse Metrics);
public sealed record TrackProfilePointResponse(double DistanceMeters, double Latitude, double Longitude,
    double ElevationMeters, double? GradientPermille);
public sealed record GradientBandResponse(double FromPercentOfLimit, double ToPercentOfLimit, double Meters);
public sealed record CorridorMetricsResponse(double LengthMeters, double StraightLineDistanceMeters,
    double Sinuosity, double MaxGradientPermille, double MaxGradientLimitPermille,
    double AscentMeters, double DescentMeters, double MinElevationMeters, double MaxElevationMeters,
    IReadOnlyList<GradientBandResponse> DistanceByGradientBand, double MaxCutMeters, double MaxFillMeters);
