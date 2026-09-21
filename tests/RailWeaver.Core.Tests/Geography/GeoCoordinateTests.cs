using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Tests.Geography;

public class GeoCoordinateTests
{
    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    public void Constructor_AcceptsValidCoordinates(double latitude, double longitude)
    {
        var coordinate = new GeoCoordinate(latitude, longitude);

        Assert.Equal(latitude, coordinate.Latitude);
        Assert.Equal(longitude, coordinate.Longitude);
    }

    [Theory]
    [InlineData(-90.000001)]
    [InlineData(90.000001)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_RejectsInvalidLatitude(double latitude)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new GeoCoordinate(latitude, 0));

        Assert.Equal("latitude", exception.ParamName);
    }

    [Theory]
    [InlineData(-180.000001)]
    [InlineData(180.000001)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_RejectsInvalidLongitude(double longitude)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new GeoCoordinate(0, longitude));

        Assert.Equal("longitude", exception.ParamName);
    }
}
