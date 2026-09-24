using RailWeaver.Core.RollingStock;

namespace RailWeaver.Core.Operations;

public enum SpeedLimitSource { LineSpeed, DesignSpeed, Curve, ReversalManeuver }
public enum PhaseKind { Accelerate, Cruise, Brake, Dwell }
public enum BrakeTarget { Destination, Reversal, LowerLimit }
public enum RunningRegime { Accelerating, TrainMaximum, TrackLimited, Braking, Dwell }

public sealed record SpeedSection(double FromMeters, double ToMeters, double SpeedLimitKmh,
    SpeedLimitSource Source, double? CurveRadiusMeters = null);
public sealed record ReversalPoint(double DistanceMeters, double? ManeuverTrackMeters);
public sealed record RunningTimeRequest(IReadOnlyList<SpeedSection> Sections,
    IReadOnlyList<ReversalPoint> Reversals, RollingStockV1 Train, double ReversalDwellSeconds);
public sealed record PhaseReason(SpeedLimitSource? Source, bool TrainMaximum, bool TailClearing,
    double? CurveRadiusMeters, double? CurveFromMeters, BrakeTarget? Target);
public sealed record RunningPhase(PhaseKind Kind, int Stage, double FromMeters, double ToMeters,
    double FromSpeedKmh, double ToSpeedKmh, double StartSeconds, double EndSeconds, PhaseReason? Reason);
public sealed record SpeedProfilePoint(double DistanceMeters, double SpeedKmh, double TrackLimitKmh);
public sealed record ReversalOutcome(double DistanceMeters, double ManeuverMeters, double? ManeuverTrackMeters,
    bool? ManeuverFits, double DwellSeconds);
public sealed record RegimeMetric(RunningRegime Regime, double Seconds, double Meters);
public sealed record CostliestCurve(double FromMeters, double ToMeters, double? CurveRadiusMeters,
    double SpeedLimitKmh, double TimeLostSeconds);
public sealed record RunningTimeMetrics(double TotalSeconds, double RunningSeconds, double DwellSeconds,
    double RouteLengthMeters, double TravelledMeters, double CommercialSpeedKmh,
    double MaxReachedSpeedKmh, IReadOnlyList<RegimeMetric> Regimes, CostliestCurve? CostliestCurve);
public sealed record RunningTimeResult(IReadOnlyList<RunningPhase> Phases,
    IReadOnlyList<SpeedProfilePoint> SpeedProfile, IReadOnlyList<ReversalOutcome> Reversals,
    RunningTimeMetrics Metrics);
