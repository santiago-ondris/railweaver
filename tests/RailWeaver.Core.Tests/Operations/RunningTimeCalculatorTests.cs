using RailWeaver.Core.Operations;
using RailWeaver.Core.RollingStock;

namespace RailWeaver.Core.Tests.Operations;

public class RunningTimeCalculatorTests
{
    private static readonly RollingStockV1 Train = new("Test", 200, 120, 0.25, 0.5);

    [Fact]
    public void LongSingleSectionMatchesAnalyticalTime()
    {
        const double distance = 100_000;
        var result = Run([new(0, distance, 100, SpeedLimitSource.LineSpeed)]);
        var v = 100 / 3.6;
        var expected = distance / v + v / (2 * 0.25) + v / (2 * 0.5);
        Assert.Equal(expected, result.Metrics.TotalSeconds, 8);
        Assert.Equal(0, result.Phases[0].FromSpeedKmh);
        Assert.Equal(0, result.Phases[^1].ToSpeedKmh);
        Assert.Equal(3, result.Phases.Count);
        Assert.Equal(result.Metrics.TotalSeconds, result.Metrics.Regimes.Sum(item => item.Seconds), 8);
        Assert.Equal(distance, result.Metrics.Regimes.Sum(item => item.Meters), 7);
        var repeated = Run([new(0, distance, 100, SpeedLimitSource.LineSpeed)]);
        Assert.Equal(result.Phases, repeated.Phases);
        Assert.Equal(result.SpeedProfile, repeated.SpeedProfile);
        Assert.Equal(result.Metrics.TotalSeconds, repeated.Metrics.TotalSeconds);
    }

    [Fact]
    public void ShortSectionHasTriangularProfile()
    {
        var result = Run([new(0, 100, 120, SpeedLimitSource.LineSpeed)]);
        Assert.Equal([PhaseKind.Accelerate, PhaseKind.Brake], result.Phases.Select(p => p.Kind));
        Assert.Equal(Math.Sqrt(2 * 0.25 * 0.5 * 100 / (0.25 + 0.5)) * 3.6,
            result.Metrics.MaxReachedSpeedKmh, 8);
    }

    [Fact]
    public void CurveLimitRemainsUntilTailClears()
    {
        var result = Run([
            new(0, 2000, 120, SpeedLimitSource.DesignSpeed),
            new(2000, 2100, 40, SpeedLimitSource.Curve, 200),
            new(2100, 5000, 120, SpeedLimitSource.DesignSpeed),
        ]);
        Assert.All(result.SpeedProfile.Where(point => point.DistanceMeters >= 2100
            && point.DistanceMeters < 2300), point => Assert.True(point.SpeedKmh <= 40 + 1e-7));
        Assert.Contains(result.Phases, phase => phase.Reason?.TailClearing == true);
        Assert.NotNull(result.Metrics.CostliestCurve);
    }

    [Fact]
    public void ReversalAddsManeuverAndDwell()
    {
        var request = new RunningTimeRequest([new(0, 10_000, 100, SpeedLimitSource.LineSpeed)],
            [new(5000, 150)], Train, 60);
        var result = RunningTimeCalculator.Calculate(request);
        Assert.Equal(10_200, result.Metrics.TravelledMeters);
        Assert.Equal(60, result.Metrics.DwellSeconds);
        Assert.False(result.Reversals[0].ManeuverFits);
        Assert.Equal(10_200, result.Metrics.Regimes.Sum(item => item.Meters), 7);
        Assert.Equal(result.Metrics.TotalSeconds, result.Metrics.Regimes.Sum(item => item.Seconds), 8);
        Assert.Contains(result.Phases, phase => phase.Kind == PhaseKind.Dwell);
        Assert.Equal(0, result.Phases[0].FromSpeedKmh);
        Assert.Equal(0, result.Phases[^1].ToSpeedKmh);
        Assert.Equal(0, result.Phases.Last(phase => phase.Stage == 0
            && phase.Kind != PhaseKind.Dwell).ToSpeedKmh);
        Assert.Equal(0, result.Phases.First(phase => phase.Stage == 1).FromSpeedKmh);
        for (var index = 1; index < result.Phases.Count; index++)
            Assert.Equal(result.Phases[index - 1].EndSeconds, result.Phases[index].StartSeconds, 7);
    }

    [Fact]
    public void TrainMaximumAndOverlappingTailLimitsHaveExpectedReasons()
    {
        var slowTrain = new RollingStockV1("Slow", 200, 60, 0.25, 0.5);
        var maximum = RunningTimeCalculator.Calculate(new([
            new(0, 10_000, 120, SpeedLimitSource.LineSpeed),
        ], [], slowTrain, 0));
        Assert.Contains(maximum.Phases, phase => phase.Kind == PhaseKind.Cruise
            && phase.Reason?.TrainMaximum == true);

        var curves = Run([
            new(0, 2000, 120, SpeedLimitSource.DesignSpeed),
            new(2000, 2050, 50, SpeedLimitSource.Curve, 300),
            new(2050, 2100, 120, SpeedLimitSource.DesignSpeed),
            new(2100, 2150, 40, SpeedLimitSource.Curve, 200),
            new(2150, 5000, 120, SpeedLimitSource.DesignSpeed),
        ]);
        Assert.All(curves.SpeedProfile.Where(point => point.DistanceMeters >= 2150
            && point.DistanceMeters < 2350), point => Assert.True(point.SpeedKmh <= 40 + 1e-7));
        Assert.Equal(2100, curves.Metrics.CostliestCurve?.FromMeters);
    }

    [Fact]
    public void ReversalFitCanBeTrueOrUnknown()
    {
        var sections = new SpeedSection[] { new(0, 2000, 80, SpeedLimitSource.LineSpeed) };
        var fits = RunningTimeCalculator.Calculate(new(sections, [new(1000, 250)], Train, 0));
        var unknown = RunningTimeCalculator.Calculate(new(sections, [new(1000, null)], Train, 0));
        Assert.True(fits.Reversals[0].ManeuverFits);
        Assert.Null(unknown.Reversals[0].ManeuverFits);
        Assert.Contains(fits.Phases, phase => phase.Kind == PhaseKind.Dwell
            && phase.StartSeconds == phase.EndSeconds);
    }

    [Fact]
    public void InvalidSectionsAndReversalsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => Run([
            new(0, 100, 100, SpeedLimitSource.LineSpeed),
            new(101, 200, 100, SpeedLimitSource.LineSpeed),
        ]));
        Assert.Throws<ArgumentException>(() => Run([
            new(0, 100, 0, SpeedLimitSource.LineSpeed),
        ]));
        Assert.Throws<ArgumentException>(() => RunningTimeCalculator.Calculate(new([
            new(0, 1000, 100, SpeedLimitSource.LineSpeed),
        ], [new(0, null)], Train, 0)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(1501)]
    public void InvalidTrainLengthIsRejected(double length)
    {
        Assert.Throws<ArgumentException>(() => new RollingStockV1("Test", length, 100, 0.25, 0.5));
    }

    private static RunningTimeResult Run(IReadOnlyList<SpeedSection> sections)
    {
        var result = RunningTimeCalculator.Calculate(new(sections, [], Train, 0));
        Assert.Equal(0, result.Phases[0].FromSpeedKmh);
        Assert.Equal(0, result.Phases[^1].ToSpeedKmh);
        Assert.Equal(result.Metrics.TotalSeconds, result.Phases[^1].EndSeconds);
        Assert.Equal(result.Metrics.TotalSeconds,
            result.Phases.Sum(phase => phase.EndSeconds - phase.StartSeconds), 7);
        for (var i = 1; i < result.Phases.Count; i++)
        {
            Assert.Equal(result.Phases[i - 1].EndSeconds, result.Phases[i].StartSeconds, 7);
            Assert.Equal(result.Phases[i - 1].ToMeters, result.Phases[i].FromMeters, 7);
        }
        foreach (var phase in result.Phases)
        {
            var from = phase.FromSpeedKmh / 3.6;
            var to = phase.ToSpeedKmh / 3.6;
            var distance = phase.ToMeters - phase.FromMeters;
            if (phase.Kind == PhaseKind.Accelerate)
                Assert.True(to * to - from * from <= 2 * Train.AccelerationMetersPerSecondSquared
                    * distance + 1e-6);
            if (phase.Kind == PhaseKind.Brake)
                Assert.True(from * from - to * to <= 2 * Train.BrakingMetersPerSecondSquared
                    * distance + 1e-6);
        }
        foreach (var point in result.SpeedProfile)
        {
            var effective = sections.Where(section => section.FromMeters <= point.DistanceMeters
                && point.DistanceMeters < section.ToMeters + Train.LengthMeters)
                .Select(section => section.SpeedLimitKmh).DefaultIfEmpty(sections[^1].SpeedLimitKmh).Min();
            Assert.True(point.SpeedKmh <= effective + 1e-6);
            Assert.True(point.SpeedKmh <= Train.MaxSpeedKmh + 1e-6);
        }
        return result;
    }
}
