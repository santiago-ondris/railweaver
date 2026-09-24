using RailWeaver.Core.Operations;
using RailWeaver.Core.RollingStock;

namespace RailWeaver.Api.Operations;

public sealed record TrainRequest(string? Name, double? LengthMeters, double? MaxSpeedKmh,
    double? AccelerationMetersPerSecondSquared, double? BrakingMetersPerSecondSquared)
{
    public RollingStockV1 ToDomain()
    {
        if (Name is null || LengthMeters is not { } length || MaxSpeedKmh is not { } speed
            || AccelerationMetersPerSecondSquared is not { } acceleration
            || BrakingMetersPerSecondSquared is not { } braking)
            throw new ArgumentException("Train name, length, maximum speed, acceleration, and braking are required.");
        return new(Name, length, speed, acceleration, braking);
    }
}

public sealed record CorridorRunningTimeRequest(IReadOnlyList<CorridorSpeedSectionRequest>? Sections,
    TrainRequest? Train);
public sealed record CorridorSpeedSectionRequest(string? Kind, double? FromMeters,
    double? ToMeters, double? SpeedLimitKmh, double? RadiusMeters);
public sealed record NetworkRunningTimeRequest(string? OriginStationId, string? DestinationStationId,
    bool IncludeDisused, double? LineSpeedKmh, double? ReversalDwellMinutes, TrainRequest? Train);

public static class RunningTimeResponse
{
    private static double Round(double value, int digits) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);
    private static double Meters(double value) => Round(value, 2);
    private static double Seconds(double value) => Round(value, 1);
    private static double Kmh(double value) => Round(value, 1);
    private static string Snake(Enum value) => string.Concat(value.ToString().Select((character, index) =>
        index > 0 && char.IsUpper(character) ? "_" + char.ToLowerInvariant(character)
            : char.ToLowerInvariant(character).ToString()));

    public static object FromDomain(RollingStockV1 train, RunningTimeResult result)
    {
        var metrics = result.Metrics;
        return new
        {
            train = new
            {
                name = train.Name, lengthMeters = Meters(train.LengthMeters),
                maxSpeedKmh = Kmh(train.MaxSpeedKmh),
                accelerationMetersPerSecondSquared = Round(train.AccelerationMetersPerSecondSquared, 2),
                brakingMetersPerSecondSquared = Round(train.BrakingMetersPerSecondSquared, 2),
            },
            metrics = new
            {
                totalSeconds = Seconds(metrics.TotalSeconds),
                runningSeconds = Seconds(metrics.RunningSeconds),
                dwellSeconds = Seconds(metrics.DwellSeconds),
                routeLengthMeters = Meters(metrics.RouteLengthMeters),
                travelledMeters = Meters(metrics.TravelledMeters),
                commercialSpeedKmh = Kmh(metrics.CommercialSpeedKmh),
                maxReachedSpeedKmh = Kmh(metrics.MaxReachedSpeedKmh),
                regimes = metrics.Regimes.Select(item => new
                {
                    regime = Snake(item.Regime), seconds = Seconds(item.Seconds),
                    meters = Meters(item.Meters),
                }).ToArray(),
                costliestCurve = metrics.CostliestCurve is { } curve ? new
                {
                    fromMeters = Meters(curve.FromMeters), toMeters = Meters(curve.ToMeters),
                    radiusMeters = curve.CurveRadiusMeters is { } radius ? Meters(radius) : (double?)null,
                    speedLimitKmh = Kmh(curve.SpeedLimitKmh),
                    timeLostSeconds = Seconds(curve.TimeLostSeconds),
                } : null,
            },
            phases = result.Phases.Select(phase => new
            {
                kind = Snake(phase.Kind), stage = phase.Stage,
                fromMeters = Meters(phase.FromMeters), toMeters = Meters(phase.ToMeters),
                fromSpeedKmh = Kmh(phase.FromSpeedKmh), toSpeedKmh = Kmh(phase.ToSpeedKmh),
                startSeconds = Seconds(phase.StartSeconds), endSeconds = Seconds(phase.EndSeconds),
                reason = phase.Reason is { } reason ? new
                {
                    source = reason.Source is { } source ? Snake(source) : null,
                    trainMaximum = reason.TrainMaximum, tailClearing = reason.TailClearing,
                    curveRadiusMeters = reason.CurveRadiusMeters is { } radius ? Meters(radius) : (double?)null,
                    curveFromMeters = reason.CurveFromMeters is { } from ? Meters(from) : (double?)null,
                    brakeTarget = reason.Target is { } target ? Snake(target) : null,
                } : null,
            }).ToArray(),
            speedProfile = result.SpeedProfile.Select(point => new
            {
                distanceMeters = Meters(point.DistanceMeters), speedKmh = Kmh(point.SpeedKmh),
                trackLimitKmh = Kmh(point.TrackLimitKmh),
            }).ToArray(),
            reversals = result.Reversals.Select(reversal => new
            {
                distanceMeters = Meters(reversal.DistanceMeters),
                maneuverMeters = Meters(reversal.ManeuverMeters),
                maneuverTrackMeters = reversal.ManeuverTrackMeters is { } track ? Meters(track) : (double?)null,
                maneuverFits = reversal.ManeuverFits, dwellSeconds = Seconds(reversal.DwellSeconds),
            }).ToArray(),
        };
    }
}
