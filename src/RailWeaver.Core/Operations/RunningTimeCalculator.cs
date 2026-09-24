using RailWeaver.Core.RollingStock;

namespace RailWeaver.Core.Operations;

public static class RunningTimeCalculator
{
    private const double Epsilon = 1e-9;

    public static RunningTimeResult Calculate(RunningTimeRequest request)
    {
        Validate(request);
        var baseline = CalculateCore(request, sampleProfile: true);
        var design = request.Sections.Where(section => section.Source == SpeedLimitSource.DesignSpeed)
            .Select(section => section.SpeedLimitKmh).DefaultIfEmpty(0).Max();
        CostliestCurve? costliest = null;
        if (design > 0)
            for (var i = 0; i < request.Sections.Count; i++)
            {
                var section = request.Sections[i];
                if (section.Source != SpeedLimitSource.Curve
                    || section.SpeedLimitKmh >= Math.Min(design, request.Train.MaxSpeedKmh)) continue;
                var changed = request.Sections.ToArray();
                changed[i] = section with { SpeedLimitKmh = design };
                var alternative = CalculateCore(request with { Sections = changed }, sampleProfile: false);
                var lost = baseline.Metrics.TotalSeconds - alternative.Metrics.TotalSeconds;
                if (lost > Epsilon && (costliest is null || lost > costliest.TimeLostSeconds + Epsilon))
                    costliest = new(section.FromMeters, section.ToMeters, section.CurveRadiusMeters,
                        section.SpeedLimitKmh, lost);
            }
        return baseline with { Metrics = baseline.Metrics with { CostliestCurve = costliest } };
    }

    private static void Validate(RunningTimeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Train is null || request.Sections is null || request.Reversals is null
            || request.Sections.Count == 0) throw new ArgumentException("Train and nonempty sections are required.");
        if (request.Sections[0].FromMeters != 0) throw new ArgumentException("Sections must start at zero.");
        var end = 0d;
        foreach (var section in request.Sections)
        {
            if (!double.IsFinite(section.FromMeters) || !double.IsFinite(section.ToMeters)
                || section.FromMeters != end || section.ToMeters <= section.FromMeters
                || !double.IsFinite(section.SpeedLimitKmh) || section.SpeedLimitKmh <= 0
                || (section.Source != SpeedLimitSource.Curve && section.CurveRadiusMeters is not null)
                || (section.CurveRadiusMeters is { } radius
                    && (!double.IsFinite(radius) || radius <= 0)))
                throw new ArgumentException("Sections must be contiguous, positive, and have valid limits.");
            end = section.ToMeters;
        }
        var previous = 0d;
        foreach (var reversal in request.Reversals)
        {
            if (!double.IsFinite(reversal.DistanceMeters) || reversal.DistanceMeters <= previous
                || reversal.DistanceMeters >= end
                || reversal.ManeuverTrackMeters is { } track
                    && (!double.IsFinite(track) || track < 0))
                throw new ArgumentException("Reversals must be ordered inside the route.");
            previous = reversal.DistanceMeters;
        }
        if (!double.IsFinite(request.ReversalDwellSeconds) || request.ReversalDwellSeconds is < 0 or > 7200)
            throw new ArgumentException("Reversal dwell must be between 0 and 7200 seconds.");
    }

    private static RunningTimeResult CalculateCore(RunningTimeRequest request, bool sampleProfile)
    {
        var train = request.Train;
        var phases = new List<RunningPhase>();
        var profile = new List<SpeedProfilePoint>();
        var outcomes = new List<ReversalOutcome>();
        var clock = 0d;
        var routeStart = 0d;
        for (var stage = 0; stage <= request.Reversals.Count; stage++)
        {
            var reversal = stage < request.Reversals.Count ? request.Reversals[stage] : null;
            var routeEnd = reversal?.DistanceMeters ?? request.Sections[^1].ToMeters;
            var routeLength = routeEnd - routeStart;
            var sections = new List<LocalSection>();
            foreach (var section in request.Sections)
            {
                var from = Math.Max(section.FromMeters, routeStart);
                var to = Math.Min(section.ToMeters, routeEnd);
                if (to <= from) continue;
                sections.Add(new(from - routeStart, to - routeStart, section));
            }
            if (reversal is not null)
            {
                var last = sections[^1].Source;
                sections.Add(new(routeLength, routeLength + train.LengthMeters,
                    new(routeEnd, routeEnd + train.LengthMeters, last.SpeedLimitKmh,
                        SpeedLimitSource.ReversalManeuver)));
            }
            var stageLength = sections[^1].To;
            var limits = BuildLimits(sections, stageLength, train);
            var a = train.AccelerationMetersPerSecondSquared;
            var b = train.BrakingMetersPerSecondSquared;
            var count = limits.Count;
            var forward = new double[count + 1];
            var speed = new double[count + 1];
            for (var j = 1; j <= count; j++)
            {
                var previous = limits[j - 1];
                var cap = j == count ? 0 : Math.Min(previous.Kmh, limits[j].Kmh) / 3.6;
                forward[j] = Math.Min(cap, Math.Sqrt(forward[j - 1] * forward[j - 1]
                    + 2 * a * (previous.To - previous.From)));
            }
            speed[count] = 0;
            for (var j = count - 1; j >= 0; j--)
            {
                var segment = limits[j];
                speed[j] = Math.Min(forward[j], Math.Sqrt(speed[j + 1] * speed[j + 1]
                    + 2 * b * (segment.To - segment.From)));
            }
            for (var j = 0; j < count; j++)
            {
                var segment = limits[j];
                var v0 = speed[j];
                var v1 = speed[j + 1];
                var cap = segment.Kmh / 3.6;
                var length = segment.To - segment.From;
                var accelEnd = Math.Max(0, (cap * cap - v0 * v0) / (2 * a));
                var brakeStart = length - Math.Max(0, (cap * cap - v1 * v1) / (2 * b));
                if (accelEnd <= brakeStart + Epsilon)
                {
                    Add(PhaseKind.Accelerate, segment.From, segment.From + accelEnd, v0, cap, null);
                    Add(PhaseKind.Cruise, segment.From + accelEnd, segment.From + brakeStart,
                        cap, cap, segment.Reason);
                    Add(PhaseKind.Brake, segment.From + brakeStart, segment.To, cap, v1,
                        BrakeReason(j, limits, reversal is not null));
                }
                else
                {
                    var crossing = Math.Clamp((v1 * v1 - v0 * v0 + 2 * b * length) / (2 * (a + b)), 0, length);
                    var peak = Math.Sqrt(v0 * v0 + 2 * a * crossing);
                    Add(PhaseKind.Accelerate, segment.From, segment.From + crossing, v0, peak, null);
                    Add(PhaseKind.Brake, segment.From + crossing, segment.To, peak, v1,
                        BrakeReason(j, limits, reversal is not null));
                }
            }
            if (reversal is not null)
            {
                var fits = reversal.ManeuverTrackMeters is { } track
                    ? track >= train.LengthMeters : (bool?)null;
                outcomes.Add(new(routeEnd, train.LengthMeters, reversal.ManeuverTrackMeters,
                    fits, request.ReversalDwellSeconds));
                phases.Add(new(PhaseKind.Dwell, stage, routeEnd, routeEnd, 0, 0,
                    clock, clock + request.ReversalDwellSeconds, null));
                clock += request.ReversalDwellSeconds;
            }
            routeStart = routeEnd;

            void Add(PhaseKind kind, double from, double to, double fromSpeed, double toSpeed,
                PhaseReason? reason)
            {
                if (to - from <= Epsilon) return;
                var duration = kind switch
                {
                    PhaseKind.Accelerate => (toSpeed - fromSpeed) / a,
                    PhaseKind.Brake => (fromSpeed - toSpeed) / b,
                    _ => (to - from) / fromSpeed,
                };
                if (duration < 0 && duration > -Epsilon) duration = 0;
                var maneuver = from >= routeLength - Epsilon;
                if (maneuver) reason = new(SpeedLimitSource.ReversalManeuver, false, false, null, null,
                    reason?.Target);
                var routeFrom = routeStart + Math.Min(from, routeLength);
                var routeTo = routeStart + Math.Min(to, routeLength);
                var phase = new RunningPhase(kind, stage, routeFrom, routeTo, fromSpeed * 3.6,
                    toSpeed * 3.6, clock, clock + duration, reason);
                phases.Add(phase);
                clock += duration;
                if (maneuver || !sampleProfile) return;
                var samples = Math.Max(1, (int)Math.Ceiling((to - from) / 25));
                for (var n = 0; n <= samples; n++)
                {
                    var x = from + (to - from) * n / samples;
                    var v = n == 0 ? fromSpeed : n == samples ? toSpeed : kind switch
                    {
                        PhaseKind.Accelerate => Math.Sqrt(fromSpeed * fromSpeed + 2 * a * (x - from)),
                        PhaseKind.Brake => Math.Sqrt(Math.Max(0, fromSpeed * fromSpeed - 2 * b * (x - from))),
                        _ => fromSpeed,
                    };
                    profile.Add(new(routeStart + x, v * 3.6,
                        RawLimit(request.Sections, routeStart + x)));
                }
            }
        }
        phases = MergePhases(phases);
        var running = phases.Where(p => p.Kind != PhaseKind.Dwell).Sum(p => p.EndSeconds - p.StartSeconds);
        var dwell = phases.Where(p => p.Kind == PhaseKind.Dwell).Sum(p => p.EndSeconds - p.StartSeconds);
        var routeLengthMeters = request.Sections[^1].ToMeters;
        var travelled = routeLengthMeters + request.Reversals.Count * train.LengthMeters;
        var regimes = Enum.GetValues<RunningRegime>().Select(regime =>
        {
            var matching = phases.Where(phase => Regime(phase) == regime).ToArray();
            var seconds = matching.Sum(phase => phase.EndSeconds - phase.StartSeconds);
            // Maneuver phases have zero route-coordinate length but still contribute travelled metres.
            var meters = matching.Sum(phase => phase.ToMeters - phase.FromMeters);
            if (regime != RunningRegime.Dwell)
                meters += ManeuverMetersFor(regime, phases);
            return new RegimeMetric(regime, seconds, meters);
        }).ToArray();
        var max = phases.Max(phase => Math.Max(phase.FromSpeedKmh, phase.ToSpeedKmh));
        var metrics = new RunningTimeMetrics(clock, running, dwell, routeLengthMeters, travelled,
            routeLengthMeters / clock * 3.6, max, regimes, null);
        return new(phases, profile, outcomes, metrics);
    }

    private static List<RunningPhase> MergePhases(IReadOnlyList<RunningPhase> phases)
    {
        var merged = new List<RunningPhase>();
        foreach (var phase in phases)
        {
            if (merged.Count > 0)
            {
                var previous = merged[^1];
                var maneuver = phase.Reason?.Source == SpeedLimitSource.ReversalManeuver
                    || previous.Reason?.Source == SpeedLimitSource.ReversalManeuver;
                if (!maneuver && phase.Kind == previous.Kind && phase.Kind != PhaseKind.Dwell
                    && phase.Stage == previous.Stage
                    && Math.Abs(phase.FromMeters - previous.ToMeters) <= Epsilon
                    && Math.Abs(phase.StartSeconds - previous.EndSeconds) <= Epsilon
                    && (phase.Kind != PhaseKind.Cruise || phase.Reason == previous.Reason
                        && phase.FromSpeedKmh == previous.ToSpeedKmh))
                {
                    merged[^1] = previous with
                    {
                        ToMeters = phase.ToMeters,
                        ToSpeedKmh = phase.ToSpeedKmh,
                        EndSeconds = phase.EndSeconds,
                        Reason = phase.Kind == PhaseKind.Brake ? phase.Reason : previous.Reason,
                    };
                    continue;
                }
            }
            merged.Add(phase);
        }
        return merged;
    }

    private static double ManeuverMetersFor(RunningRegime regime, IReadOnlyList<RunningPhase> phases)
    {
        // Physical phase lengths are reconstructed from constant acceleration or braking.
        var total = 0d;
        foreach (var phase in phases.Where(p => p.FromMeters == p.ToMeters
            && p.Reason?.Source == SpeedLimitSource.ReversalManeuver && Regime(p) == regime))
        {
            var seconds = phase.EndSeconds - phase.StartSeconds;
            total += (phase.FromSpeedKmh + phase.ToSpeedKmh) / 7.2 * seconds;
        }
        return total;
    }

    private static RunningRegime Regime(RunningPhase phase) => phase.Kind switch
    {
        PhaseKind.Accelerate => RunningRegime.Accelerating,
        PhaseKind.Brake => RunningRegime.Braking,
        PhaseKind.Dwell => RunningRegime.Dwell,
        _ => phase.Reason?.TrainMaximum == true ? RunningRegime.TrainMaximum : RunningRegime.TrackLimited,
    };

    private static PhaseReason BrakeReason(int index, IReadOnlyList<LimitSegment> limits, bool reversal)
    {
        if (index == limits.Count - 1)
            return new(null, false, false, null, null,
                reversal ? BrakeTarget.Reversal : BrakeTarget.Destination);
        var next = limits[index + 1].Reason;
        return next with { Target = BrakeTarget.LowerLimit };
    }

    private static double RawLimit(IReadOnlyList<SpeedSection> sections, double distance)
    {
        foreach (var section in sections)
            if (distance < section.ToMeters) return section.SpeedLimitKmh;
        return sections[^1].SpeedLimitKmh;
    }

    private static List<LimitSegment> BuildLimits(IReadOnlyList<LocalSection> sections,
        double stageLength, RollingStockV1 train)
    {
        var cuts = sections.SelectMany(section => new[] { section.From, section.To,
            Math.Min(stageLength, section.To + train.LengthMeters) })
            .Append(0).Append(stageLength).Distinct().Order().ToArray();
        var result = new List<LimitSegment>();
        for (var i = 0; i < cuts.Length - 1; i++)
        {
            var from = cuts[i];
            var to = cuts[i + 1];
            if (to - from <= Epsilon) continue;
            var midpoint = (from + to) / 2;
            var active = sections.Select((section, index) => (section, index))
                .Where(item => item.section.From <= midpoint
                    && midpoint < Math.Min(stageLength, item.section.To + train.LengthMeters))
                .OrderBy(item => item.section.Source.SpeedLimitKmh).ThenBy(item => item.index).First();
            var section = active.section;
            var trainMaximum = train.MaxSpeedKmh <= section.Source.SpeedLimitKmh;
            var reason = trainMaximum
                ? new PhaseReason(null, true, false, null, null, null)
                : new PhaseReason(section.Source.Source, false, midpoint >= section.To,
                    section.Source.CurveRadiusMeters,
                    section.Source.Source == SpeedLimitSource.Curve
                        ? section.Source.FromMeters : null, null);
            var kmh = Math.Min(train.MaxSpeedKmh, section.Source.SpeedLimitKmh);
            if (result.Count > 0 && result[^1].Kmh == kmh && result[^1].Reason == reason)
                result[^1] = result[^1] with { To = to };
            else result.Add(new(from, to, kmh, reason));
        }
        return result;
    }

    private sealed record LocalSection(double From, double To, SpeedSection Source);
    private sealed record LimitSegment(double From, double To, double Kmh, PhaseReason Reason);
}
