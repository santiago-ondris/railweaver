using RailWeaver.Core.Infrastructure;

namespace RailWeaver.Core.Planning;

public sealed record GaugeCurveParameters(double ContactWidthMillimetres, double MaxCantMillimetres,
    double MaxCantDeficiencyMillimetres, double AbsoluteMinimumRadiusMeters, double CurveResistanceConstant)
{
    public double KinematicConstant => ContactWidthMillimetres / (3.6 * 3.6 * CurvatureRules.GravityMetersPerSecondSquared);
}

public static class CurvatureRules
{
    public const double GravityMetersPerSecondSquared = 9.80665;
    public const double MinDesignSpeedKmh = 20;
    public const double MaxDesignSpeedKmh = 120;
    public const double SimplificationToleranceFactor = 0.5;
    public const double MaxArcChordMeters = 10;
    public const double RadiusTolerance = 1e-9;

    public static GaugeCurveParameters For(TrackGauge gauge) => gauge.Kind switch
    {
        TrackGaugeKind.Metre => new(1060, 90, 70, 100, 500),
        TrackGaugeKind.Broad => new(1740, 140, 100, 300, 650),
        _ => throw new ArgumentException("Only metre and broad gauges support candidate corridors.", nameof(gauge)),
    };

    public static double DesignRadius(GaugeCurveParameters parameters, double speedKmh) =>
        Math.Max(parameters.AbsoluteMinimumRadiusMeters,
            parameters.KinematicConstant * speedKmh * speedKmh /
            (parameters.MaxCantMillimetres + parameters.MaxCantDeficiencyMillimetres));

    public static double SpeedLimit(GaugeCurveParameters parameters, double radiusMeters, double designSpeedKmh) =>
        Math.Min(designSpeedKmh, Math.Sqrt(radiusMeters *
            (parameters.MaxCantMillimetres + parameters.MaxCantDeficiencyMillimetres) / parameters.KinematicConstant));

    public static double GradientLimit(GaugeCurveParameters parameters, double radiusMeters, double gradientPermille) =>
        Math.Max(0, gradientPermille - parameters.CurveResistanceConstant / radiusMeters);
}
