namespace RailWeaver.Core.Geography;

public interface IElevationSource
{
    double? GetElevationMeters(GeoCoordinate coordinate);
}

public sealed record ElevationSample(
    double DistanceMeters,
    GeoCoordinate Coordinate,
    double? ElevationMeters,
    double? GradientPermille);

public sealed record ElevationProfile(IReadOnlyList<ElevationSample> Samples)
{
    public double TotalDistanceMeters => Samples.Count == 0 ? 0 : Samples[^1].DistanceMeters;
}

public sealed class ElevationProfileBuilder(IElevationSource elevationSource)
{
    public const double SamplingStepMeters = 30;
    public const double MeanEarthRadiusMeters = 6_371_008.8;

    public ElevationProfile Build(IReadOnlyList<GeoCoordinate> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        if (vertices.Count < 2)
        {
            throw new ArgumentException("An elevation profile requires at least two vertices.", nameof(vertices));
        }

        var points = new List<(GeoCoordinate Coordinate, double Distance)>();
        var totalDistance = 0d;
        points.Add((vertices[0], 0));

        for (var index = 1; index < vertices.Count; index++)
        {
            var start = vertices[index - 1];
            var end = vertices[index];
            var segmentDistance = GreatCircleDistanceMeters(start, end);
            if (segmentDistance == 0)
            {
                throw new ArgumentException("Consecutive profile vertices must be different.", nameof(vertices));
            }

            for (var offset = SamplingStepMeters; offset < segmentDistance; offset += SamplingStepMeters)
            {
                points.Add((InterpolateGreatCircle(start, end, offset / segmentDistance), totalDistance + offset));
            }

            totalDistance += segmentDistance;
            points.Add((end, totalDistance));
        }

        var samples = new ElevationSample[points.Count];
        for (var index = 0; index < points.Count; index++)
        {
            var elevation = elevationSource.GetElevationMeters(points[index].Coordinate);
            double? gradient = null;
            if (index > 0 && elevation is not null && samples[index - 1].ElevationMeters is not null)
            {
                var interval = points[index].Distance - points[index - 1].Distance;
                gradient = Math.Round(
                    (elevation.Value - samples[index - 1].ElevationMeters!.Value) / interval * 1000,
                    1,
                    MidpointRounding.AwayFromZero);
            }

            samples[index] = new ElevationSample(
                points[index].Distance,
                points[index].Coordinate,
                elevation,
                gradient);
        }

        return new ElevationProfile(samples);
    }

    public static double GreatCircleDistanceMeters(GeoCoordinate first, GeoCoordinate second)
    {
        var latitude1 = DegreesToRadians(first.Latitude);
        var latitude2 = DegreesToRadians(second.Latitude);
        var deltaLatitude = latitude2 - latitude1;
        var deltaLongitude = DegreesToRadians(second.Longitude - first.Longitude);
        var a = Math.Pow(Math.Sin(deltaLatitude / 2), 2)
            + Math.Cos(latitude1) * Math.Cos(latitude2) * Math.Pow(Math.Sin(deltaLongitude / 2), 2);
        return 2 * MeanEarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    public static GeoCoordinate InterpolateGreatCircle(GeoCoordinate start, GeoCoordinate end, double fraction)
    {
        var lat1 = DegreesToRadians(start.Latitude);
        var lon1 = DegreesToRadians(start.Longitude);
        var lat2 = DegreesToRadians(end.Latitude);
        var lon2 = DegreesToRadians(end.Longitude);
        var angularDistance = GreatCircleDistanceMeters(start, end) / MeanEarthRadiusMeters;
        var sinDistance = Math.Sin(angularDistance);
        var a = Math.Sin((1 - fraction) * angularDistance) / sinDistance;
        var b = Math.Sin(fraction * angularDistance) / sinDistance;
        var x = a * Math.Cos(lat1) * Math.Cos(lon1) + b * Math.Cos(lat2) * Math.Cos(lon2);
        var y = a * Math.Cos(lat1) * Math.Sin(lon1) + b * Math.Cos(lat2) * Math.Sin(lon2);
        var z = a * Math.Sin(lat1) + b * Math.Sin(lat2);
        return new GeoCoordinate(
            RadiansToDegrees(Math.Atan2(z, Math.Sqrt(x * x + y * y))),
            RadiansToDegrees(Math.Atan2(y, x)));
    }

    private static double DegreesToRadians(double value) => value * Math.PI / 180;
    private static double RadiansToDegrees(double value) => value * 180 / Math.PI;
}
