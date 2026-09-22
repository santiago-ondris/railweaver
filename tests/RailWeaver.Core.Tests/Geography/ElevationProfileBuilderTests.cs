using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Tests.Geography;

public class ElevationProfileBuilderTests
{
    [Fact]
    public void Build_IncludesVerticesIntermediateSamplesAndFinalPoint()
    {
        var source = new DelegateElevationSource(point => 100 + point.Latitude * 1000);
        var builder = new ElevationProfileBuilder(source);
        var vertices = new[]
        {
            new GeoCoordinate(0, 0),
            new GeoCoordinate(0.0004, 0),
            new GeoCoordinate(0.0008, 0),
        };

        var profile = builder.Build(vertices);

        Assert.Equal(5, profile.Samples.Count);
        Assert.Equal(vertices[1], profile.Samples[2].Coordinate);
        Assert.Equal(vertices[2], profile.Samples[^1].Coordinate);
        Assert.All(profile.Samples.Skip(1), sample => Assert.InRange(sample.GradientPermille!.Value, 8.9, 9.1));
    }

    [Fact]
    public void Build_PreservesNoDataAndRemovesGradientAtEitherSide()
    {
        var builder = new ElevationProfileBuilder(
            new DelegateElevationSource(point => point.Latitude is > 0.0002 and < 0.00035 ? null : 10));

        var profile = builder.Build([new GeoCoordinate(0, 0), new GeoCoordinate(0.0006, 0)]);

        Assert.Null(profile.Samples[1].ElevationMeters);
        Assert.Null(profile.Samples[1].GradientPermille);
        Assert.Null(profile.Samples[2].GradientPermille);
    }

    [Fact]
    public void Build_IsDeterministic()
    {
        var builder = new ElevationProfileBuilder(new DelegateElevationSource(point => point.Longitude * 2));
        var path = new[] { new GeoCoordinate(-31.4, -64.2), new GeoCoordinate(-31.39, -64.19) };

        Assert.Equal(builder.Build(path).Samples, builder.Build(path).Samples);
    }

    [Fact]
    public void GreatCircleDistance_UsesWgs84MeanRadius()
    {
        var distance = ElevationProfileBuilder.GreatCircleDistanceMeters(
            new GeoCoordinate(0, 0), new GeoCoordinate(0, 1));

        Assert.Equal(111_195.08, distance, 2);
    }

    private sealed class DelegateElevationSource(Func<GeoCoordinate, double?> getElevation) : IElevationSource
    {
        public double? GetElevationMeters(GeoCoordinate coordinate) => getElevation(coordinate);
    }
}
