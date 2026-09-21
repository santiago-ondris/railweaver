namespace RailWeaver.Core.Geography;

/// <summary>
/// An axis-aligned geographic bounding box in WGS84 (EPSG:4326).
/// Bounding boxes that cross the antimeridian are not supported.
/// </summary>
public sealed record GeoBoundingBox
{
    public GeoBoundingBox(double west, double south, double east, double north)
    {
        ValidateLongitude(west, nameof(west));
        ValidateLatitude(south, nameof(south));
        ValidateLongitude(east, nameof(east));
        ValidateLatitude(north, nameof(north));

        if (west >= east)
        {
            throw new ArgumentException(
                "West must be less than east; antimeridian-spanning boxes are not supported.",
                nameof(west));
        }

        if (south >= north)
        {
            throw new ArgumentException("South must be less than north.", nameof(south));
        }

        West = west;
        South = south;
        East = east;
        North = north;
    }

    public double West { get; }

    public double South { get; }

    public double East { get; }

    public double North { get; }

    public bool Contains(GeoCoordinate coordinate) =>
        coordinate.Longitude >= West
        && coordinate.Longitude <= East
        && coordinate.Latitude >= South
        && coordinate.Latitude <= North;

    private static void ValidateLatitude(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Latitude must be finite and between -90 and 90 degrees.");
        }
    }

    private static void ValidateLongitude(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Longitude must be finite and between -180 and 180 degrees.");
        }
    }
}
