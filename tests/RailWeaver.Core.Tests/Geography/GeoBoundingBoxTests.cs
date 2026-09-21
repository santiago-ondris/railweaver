using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Tests.Geography;

public class GeoBoundingBoxTests
{
    [Fact]
    public void Constructor_AcceptsFullCoordinateRange()
    {
        var boundingBox = new GeoBoundingBox(-180, -90, 180, 90);

        Assert.Equal(-180, boundingBox.West);
        Assert.Equal(-90, boundingBox.South);
        Assert.Equal(180, boundingBox.East);
        Assert.Equal(90, boundingBox.North);
    }

    [Theory]
    [InlineData(-180.000001, -10, 10, 10, "west")]
    [InlineData(double.NaN, -10, 10, 10, "west")]
    [InlineData(-10, -90.000001, 10, 10, "south")]
    [InlineData(-10, double.NegativeInfinity, 10, 10, "south")]
    [InlineData(-10, -10, 180.000001, 10, "east")]
    [InlineData(-10, -10, double.PositiveInfinity, 10, "east")]
    [InlineData(-10, -10, 10, 90.000001, "north")]
    [InlineData(-10, -10, 10, double.NaN, "north")]
    public void Constructor_RejectsBoundsOutsideCoordinateRanges(
        double west,
        double south,
        double east,
        double north,
        string expectedParameter)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new GeoBoundingBox(west, south, east, north));

        Assert.Equal(expectedParameter, exception.ParamName);
    }

    [Theory]
    [InlineData(10, -10, 10, 10)]
    [InlineData(20, -10, 10, 10)]
    public void Constructor_RejectsEmptyOrAntimeridianSpanningLongitudeRange(
        double west,
        double south,
        double east,
        double north)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new GeoBoundingBox(west, south, east, north));

        Assert.Equal("west", exception.ParamName);
    }

    [Theory]
    [InlineData(-10, 10, 10, 10)]
    [InlineData(-10, 20, 10, 10)]
    public void Constructor_RejectsEmptyOrReversedLatitudeRange(
        double west,
        double south,
        double east,
        double north)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new GeoBoundingBox(west, south, east, north));

        Assert.Equal("south", exception.ParamName);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-10, -5)]
    [InlineData(20, -5)]
    [InlineData(20, 15)]
    [InlineData(-10, 15)]
    [InlineData(5, -5)]
    [InlineData(5, 15)]
    [InlineData(-10, 5)]
    [InlineData(20, 5)]
    public void Contains_ReturnsTrueForInteriorAndBoundaryPoints(
        double longitude,
        double latitude)
    {
        var boundingBox = new GeoBoundingBox(-10, -5, 20, 15);

        Assert.True(boundingBox.Contains(new GeoCoordinate(latitude, longitude)));
    }

    [Theory]
    [InlineData(-10.000001, 0)]
    [InlineData(20.000001, 0)]
    [InlineData(0, -5.000001)]
    [InlineData(0, 15.000001)]
    public void Contains_ReturnsFalseForExteriorPoints(double longitude, double latitude)
    {
        var boundingBox = new GeoBoundingBox(-10, -5, 20, 15);

        Assert.False(boundingBox.Contains(new GeoCoordinate(latitude, longitude)));
    }
}
