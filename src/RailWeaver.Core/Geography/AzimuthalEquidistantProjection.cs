namespace RailWeaver.Core.Geography;

public readonly record struct ProjectedPoint(double X, double Y)
{
    public static ProjectedPoint operator +(ProjectedPoint a, ProjectedPoint b) => new(a.X + b.X, a.Y + b.Y);
    public static ProjectedPoint operator -(ProjectedPoint a, ProjectedPoint b) => new(a.X - b.X, a.Y - b.Y);
    public static ProjectedPoint operator *(ProjectedPoint a, double factor) => new(a.X * factor, a.Y * factor);
    public double Length => Math.Sqrt(X * X + Y * Y);
}

public sealed class AzimuthalEquidistantProjection(GeoCoordinate center)
{
    private const double Radius = ElevationProfileBuilder.MeanEarthRadiusMeters;
    private readonly double latitude = center.Latitude * Math.PI / 180;
    private readonly double longitude = center.Longitude * Math.PI / 180;

    public ProjectedPoint Project(GeoCoordinate coordinate)
    {
        var phi = coordinate.Latitude * Math.PI / 180;
        var lambda = coordinate.Longitude * Math.PI / 180 - longitude;
        var cosine = Math.Clamp(Math.Sin(latitude) * Math.Sin(phi) +
            Math.Cos(latitude) * Math.Cos(phi) * Math.Cos(lambda), -1, 1);
        var angle = Math.Acos(cosine);
        var scale = angle < 1e-12 ? 1 : angle / Math.Sin(angle);
        return new ProjectedPoint(Radius * scale * Math.Cos(phi) * Math.Sin(lambda),
            Radius * scale * (Math.Cos(latitude) * Math.Sin(phi) -
                Math.Sin(latitude) * Math.Cos(phi) * Math.Cos(lambda)));
    }

    public GeoCoordinate Unproject(ProjectedPoint point)
    {
        var distance = point.Length;
        if (distance < 1e-12) return center;
        var angle = distance / Radius;
        var phi = Math.Asin(Math.Clamp(Math.Cos(angle) * Math.Sin(latitude) +
            point.Y * Math.Sin(angle) * Math.Cos(latitude) / distance, -1, 1));
        var lambda = longitude + Math.Atan2(point.X * Math.Sin(angle),
            distance * Math.Cos(latitude) * Math.Cos(angle) -
            point.Y * Math.Sin(latitude) * Math.Sin(angle));
        return new GeoCoordinate(phi * 180 / Math.PI, lambda * 180 / Math.PI);
    }
}
