using RailWeaver.Core.Geography;
using RailWeaver.Core.Planning;

namespace RailWeaver.Core.Tests.Planning;

public sealed class CorridorFinderTests
{
    private static readonly GeoBoundingBox Bounds = new(-1, -1, 1, 1);
    private static readonly GeoCoordinate Origin = new(0, 0);
    private static readonly GeoCoordinate Destination = new(0.18, 0.12);

    [Fact]
    public void FlatTerrain_FindsNearlyDirectDeterministicCorridor()
    {
        var finder = new CorridorFinder(new SyntheticElevation(_ => 100));
        var request = new CorridorRequest(Origin, Destination, 10, Bounds);

        var first = finder.Find(request, TestContext.Current.CancellationToken);
        var second = finder.Find(request, TestContext.Current.CancellationToken);

        Assert.Equal(CorridorStatus.Found, first.Status);
        Assert.NotNull(first.Corridor);
        Assert.True(first.Corridor.Metrics.Sinuosity <= 1.05);
        Assert.Equal(first.Search, second.Search);
        Assert.Equal(first.Corridor.Alignment, second.Corridor!.Alignment);
        Assert.Equal(first.Corridor.TrackProfile, second.Corridor.TrackProfile);
        Assert.Equal(BitConverter.DoubleToInt64Bits(first.Corridor.Metrics.LengthMeters),
            BitConverter.DoubleToInt64Bits(second.Corridor.Metrics.LengthMeters));
        for (var index = 0; index < first.Corridor.TrackProfile.Count; index++)
            Assert.Equal(BitConverter.DoubleToInt64Bits(first.Corridor.TrackProfile[index].DistanceMeters),
                BitConverter.DoubleToInt64Bits(second.Corridor.TrackProfile[index].DistanceMeters));
        AssertGradientWithinLimit(first, request.MaxGradientPermille);
        Assert.All(first.Corridor.TrackProfile, point =>
            Assert.True(Math.Abs(point.GradientPermille ?? 0) <= request.MaxGradientPermille));
        Assert.Equal(first.Corridor.Metrics.LengthMeters,
            first.Corridor.Metrics.DistanceByGradientBand.Sum(band => band.Meters), 6);
    }

    [Fact]
    public void NoDataBarrier_ReturnsNoPath()
    {
        var finder = new CorridorFinder(new SyntheticElevation(point =>
            point.Latitude is > 0.075 and < 0.105 ? null : 100));
        var result = finder.Find(new CorridorRequest(Origin, Destination, 10,
            new GeoBoundingBox(-0.02, -0.02, 0.14, 0.20)), TestContext.Current.CancellationToken);

        Assert.Equal(CorridorStatus.NoFeasiblePath, result.Status);
        Assert.Null(result.Corridor);
    }

    [Fact]
    public void SteepDirectSlope_CanUseLateralDevelopmentWithoutBreakingLimit()
    {
        var finder = new CorridorFinder(new SyntheticElevation(point => point.Latitude * 2_000));
        var request = new CorridorRequest(new GeoCoordinate(0, 0), new GeoCoordinate(0.02, 0),
            10, new GeoBoundingBox(-0.1, -0.1, 0.1, 0.1));

        var result = finder.Find(request, TestContext.Current.CancellationToken);

        Assert.Equal(CorridorStatus.Found, result.Status);
        Assert.True(result.Corridor!.Metrics.LengthMeters > result.Corridor.Metrics.StraightLineDistanceMeters);
        AssertGradientWithinLimit(result, request.MaxGradientPermille);
    }

    [Fact]
    public void NoDataBarrierWithGap_RoutesThroughGap()
    {
        var finder = new CorridorFinder(new SyntheticElevation(point =>
            point.Latitude is > 0.075 and < 0.105 && point.Longitude is < 0.04 or > 0.06
                ? null : 100));
        var request = new CorridorRequest(Origin, new GeoCoordinate(0.18, 0), 10,
            new GeoBoundingBox(-0.02, -0.02, 0.1, 0.20));

        var result = finder.Find(request, TestContext.Current.CancellationToken);

        Assert.Equal(CorridorStatus.Found, result.Status);
        AssertGradientWithinLimit(result, request.MaxGradientPermille);
        Assert.Contains(result.Corridor!.Alignment, point => point.Latitude is > 0.075 and < 0.105
            && point.Longitude is >= 0.04 and <= 0.06);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SteepRidge_IsBlockedUnlessThereIsAPass(bool hasPass)
    {
        var finder = new CorridorFinder(new SyntheticElevation(point =>
            point.Latitude is > 0.075 and < 0.105
            && (!hasPass || point.Longitude is < 0.04 or > 0.06) ? 1_000 : 100));
        var request = new CorridorRequest(Origin, new GeoCoordinate(0.18, 0), 10,
            new GeoBoundingBox(-0.02, -0.02, 0.1, 0.20));

        var result = finder.Find(request, TestContext.Current.CancellationToken);

        if (hasPass)
        {
            Assert.Equal(CorridorStatus.Found, result.Status);
            AssertGradientWithinLimit(result, request.MaxGradientPermille);
            Assert.Contains(result.Corridor!.Alignment, point => point.Latitude is > 0.075 and < 0.105
                && point.Longitude is >= 0.04 and <= 0.06);
        }
        else
        {
            Assert.Equal(CorridorStatus.NoFeasiblePath, result.Status);
            Assert.Null(result.Corridor);
        }
    }

    [Fact]
    public void Metrics_SeparateTrackFromTerrainBetweenVertices()
    {
        var longitudeStep = 250 / (ElevationProfileBuilder.MeanEarthRadiusMeters * Math.PI / 180);
        var finder = new CorridorFinder(new SyntheticElevation(point =>
            100 + 5 * Math.Pow(Math.Sin(Math.PI * point.Longitude / longitudeStep), 2)));
        var request = new CorridorRequest(new GeoCoordinate(0, 2 * longitudeStep),
            new GeoCoordinate(0, 12 * longitudeStep), 15,
            new GeoBoundingBox(0, -0.01, 0.1, 0.01));

        var result = finder.Find(request, TestContext.Current.CancellationToken);

        Assert.Equal(CorridorStatus.Found, result.Status);
        AssertGradientWithinLimit(result, request.MaxGradientPermille);
        Assert.True(result.Corridor!.Metrics.MaxCutMeters > 4);
        Assert.Equal(0, result.Corridor.Metrics.MaxFillMeters, 6);
        Assert.True(result.Corridor.TerrainProfile.Samples.Count > result.Corridor.TrackProfile.Count);
    }

    [Fact]
    public void SymmetricDetour_UsesLowerIndexOnEqualCost()
    {
        var latitudeStep = 250 / (ElevationProfileBuilder.MeanEarthRadiusMeters * Math.PI / 180);
        var referenceLatitude = 5 * latitudeStep * Math.PI / 180;
        var longitudeStep = 250 / (ElevationProfileBuilder.MeanEarthRadiusMeters * Math.PI / 180
            * Math.Cos(referenceLatitude));
        var bounds = new GeoBoundingBox(-20 * longitudeStep, -5 * latitudeStep,
            20 * longitudeStep, 15 * latitudeStep);
        var finder = new CorridorFinder(new SyntheticElevation(point =>
            point.Latitude is > 0.005 and < 0.015 && Math.Abs(point.Longitude) < 0.003
                ? null : 100));
        var request = new CorridorRequest(new GeoCoordinate(0, 0), new GeoCoordinate(10 * latitudeStep, 0), 15, bounds);

        var result = finder.Find(request, TestContext.Current.CancellationToken);

        Assert.Equal(CorridorStatus.Found, result.Status);
        AssertGradientWithinLimit(result, request.MaxGradientPermille);
        Assert.Contains(result.Corridor!.Alignment, point => point.Longitude < -0.003);
        Assert.DoesNotContain(result.Corridor.Alignment, point => point.Longitude > 0.003);
    }

    [Fact]
    public void MissingEndpoint_IsReportedBeforeSearch()
    {
        var finder = new CorridorFinder(new SyntheticElevation(point => point == Origin ? null : 100));
        var result = finder.Find(new CorridorRequest(Origin, Destination, 15, Bounds), TestContext.Current.CancellationToken);
        Assert.Equal(CorridorStatus.EndpointWithoutElevation, result.Status);
        Assert.Equal(0, result.Search.ExploredNodes);
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(0, 0, 40)]
    [InlineData(-2, 0, 15)]
    public void InvalidRequests_Throw(double originLatitude, double originLongitude, double limit)
    {
        var finder = new CorridorFinder(new SyntheticElevation(_ => 100));
        Assert.Throws<ArgumentException>(() => finder.Find(new CorridorRequest(
            new GeoCoordinate(originLatitude, originLongitude), Origin, limit, Bounds), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.99)]
    [InlineData(40.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidGradient_Throws(double limit)
    {
        var finder = new CorridorFinder(new SyntheticElevation(_ => 100));
        Assert.Throws<ArgumentException>(() => finder.Find(
            new CorridorRequest(Origin, Destination, limit, Bounds), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(40)]
    public void GradientBoundaries_AreAllowed(double limit)
    {
        var finder = new CorridorFinder(new SyntheticElevation(_ => 100));
        var result = finder.Find(new CorridorRequest(Origin, Destination, limit, Bounds), TestContext.Current.CancellationToken);
        Assert.Equal(CorridorStatus.Found, result.Status);
        AssertGradientWithinLimit(result, limit);
    }

    [Theory]
    [InlineData(0.18, 0.12, 250)]
    [InlineData(2, 2, 500)]
    [InlineData(4, 4, 1000)]
    public void GridStep_RespectsMillionNodeBudget(double latitudeSpan, double longitudeSpan, double expectedStep)
    {
        var source = new SyntheticElevation(_ => null);
        var finder = new CorridorFinder(source);
        var request = new CorridorRequest(new GeoCoordinate(-latitudeSpan / 2, -longitudeSpan / 2),
            new GeoCoordinate(latitudeSpan / 2, longitudeSpan / 2), 15,
            new GeoBoundingBox(-5, -5, 5, 5));

        var result = finder.Find(request, TestContext.Current.CancellationToken);

        Assert.Equal(expectedStep, result.Search.GridStepMeters);
        Assert.True((long)result.Search.GridColumns * result.Search.GridRows <= 1_000_000);
    }

    private sealed class SyntheticElevation(Func<GeoCoordinate, double?> height) : IElevationSource
    {
        public double? GetElevationMeters(GeoCoordinate coordinate) => height(coordinate);
    }

    private static void AssertGradientWithinLimit(CorridorResult result, double limit)
    {
        Assert.Null(result.Corridor!.TrackProfile[0].GradientPermille);
        Assert.All(result.Corridor.TrackProfile.Skip(1), point =>
            Assert.True(Math.Abs(point.GradientPermille!.Value) <= limit));
    }
}
