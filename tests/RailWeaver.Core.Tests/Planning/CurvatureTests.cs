using RailWeaver.Core.Geography;
using RailWeaver.Core.Infrastructure;
using RailWeaver.Core.Planning;

namespace RailWeaver.Core.Tests.Planning;

public sealed class CurvatureTests
{
    [Fact]
    public void Rules_UseGaugeSpecificLimits()
    {
        var broad = CurvatureRules.For(TrackGauge.Broad);
        var metre = CurvatureRules.For(TrackGauge.Metre);
        Assert.InRange(CurvatureRules.DesignRadius(broad, 120), 820, 822);
        Assert.Equal(100, CurvatureRules.DesignRadius(metre, 40));
        Assert.Equal(300, broad.AbsoluteMinimumRadiusMeters);
        Assert.Equal(100, metre.AbsoluteMinimumRadiusMeters);
        Assert.Equal(8, CurvatureRules.GradientLimit(metre, 250, 10));
        Assert.Equal(0, CurvatureRules.GradientLimit(metre, 100, 1));
        Assert.Equal(80, CurvatureRules.SpeedLimit(broad, 1000, 80));
        Assert.Throws<ArgumentException>(() => CurvatureRules.For(TrackGauge.Standard));
    }

    [Fact]
    public void Projection_RoundTripsCórdobaPoints()
    {
        var projection = new AzimuthalEquidistantProjection(new GeoCoordinate(-31.5, -64.5));
        foreach (var point in new[] { new GeoCoordinate(-31.5, -64.5),
            new GeoCoordinate(-32.3, -63.2), new GeoCoordinate(-30.8, -65.1) })
        {
            var restored = projection.Unproject(projection.Project(point));
            Assert.True(ElevationProfileBuilder.GreatCircleDistanceMeters(point, restored) < 0.001);
        }
    }

    [Fact]
    public void Alignment_ContainsTangentCurvesAndContinuousSections()
    {
        var center = new GeoCoordinate(-31, -64);
        var projection = new AzimuthalEquidistantProjection(center);
        var path = new[] { new ProjectedPoint(0, 0), new ProjectedPoint(1000, 0),
            new ProjectedPoint(1000, 1000), new ProjectedPoint(1000, 2000) }
            .Select(projection.Unproject).ToArray();
        var built = new AlignmentBuilder().Build(path, 250, CurvatureRules.For(TrackGauge.Broad), 80, projection);
        var curve = Assert.Single(built.Sections, section => section.Kind == SectionKind.Curve);
        Assert.True(curve.RadiusMeters >= 300 * (1 - CurvatureRules.RadiusTolerance));
        Assert.Equal(TurnDirection.Left, curve.Direction);
        Assert.NotNull(curve.CurveMidpoint);
        Assert.All(built.Sections, section => Assert.True(section.ToMeters > section.FromMeters));
        Assert.Equal(0, built.Sections[0].FromMeters);
        for (var i = 1; i < built.Sections.Count; i++)
            Assert.Equal(built.Sections[i - 1].ToMeters, built.Sections[i].FromMeters);
        Assert.True(curve.SpeedLimitKmh <= 80);
    }

    [Fact]
    public void AdjacentCurves_ShareTheirTangentWithoutOverlappingAndReduceSpeed()
    {
        var projection = new AzimuthalEquidistantProjection(new GeoCoordinate(-31, -64));
        var path = new[] { new ProjectedPoint(0, 0), new ProjectedPoint(1000, 0),
            new ProjectedPoint(1400, 400), new ProjectedPoint(1400, 1400) }
            .Select(projection.Unproject).ToArray();
        var parameters = CurvatureRules.For(TrackGauge.Broad);

        var built = new AlignmentBuilder().Build(path, 250, parameters, 120, projection);

        var curves = built.Sections.Where(section => section.Kind == SectionKind.Curve).ToArray();
        Assert.Equal(2, curves.Length);
        Assert.All(curves, curve =>
        {
            Assert.InRange(curve.RadiusMeters!.Value, 682, 684);
            Assert.Equal(CurvatureRules.SpeedLimit(parameters, curve.RadiusMeters.Value, 120), curve.SpeedLimitKmh);
            Assert.True(curve.SpeedLimitKmh < 120);
        });
        Assert.Equal(curves[0].ToMeters, curves[1].FromMeters, 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(19.9)]
    [InlineData(120.1)]
    [InlineData(double.NaN)]
    public void InvalidDesignSpeed_IsRejected(double speed)
    {
        var source = new FlatElevation();
        var finder = new CorridorFinder(source);
        Assert.Throws<ArgumentException>(() => finder.Find(new CorridorRequest(
            new GeoCoordinate(0, 0), new GeoCoordinate(0, 0.02), 10, TrackGauge.Broad,
            speed, new GeoBoundingBox(-1, -1, 1, 1)), TestContext.Current.CancellationToken));
    }

    private sealed class FlatElevation : IElevationSource
    {
        public double? GetElevationMeters(GeoCoordinate coordinate) => 100;
    }
}
