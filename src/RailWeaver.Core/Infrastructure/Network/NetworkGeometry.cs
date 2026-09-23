using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Infrastructure.Network;

public static class NetworkRules
{
    public const double MaxDeflectionDegrees = 45;
    public const double BearingLookaheadMeters = 30;
    public const double StationSnapToleranceMeters = 150;
    public const double DataGapSearchRadiusMeters = 500;
    public const double BoundaryToleranceDegrees = 1e-7;
}

internal static class NetworkGeometry
{
    public static double Length(IReadOnlyList<GeoCoordinate> points)
    {
        var length = 0d;
        for (var i = 1; i < points.Count; i++)
            length += ElevationProfileBuilder.GreatCircleDistanceMeters(points[i - 1], points[i]);
        return length;
    }

    public static GeoCoordinate At(IReadOnlyList<GeoCoordinate> points, double offset)
    {
        if (offset <= 0) return points[0];
        if (offset >= Length(points)) return points[^1];
        var traveled = 0d;
        for (var i = 1; i < points.Count; i++)
        {
            var length = ElevationProfileBuilder.GreatCircleDistanceMeters(points[i - 1], points[i]);
            if (traveled + length >= offset)
            {
                var fraction = (offset - traveled) / length;
                if (fraction <= 0) return points[i - 1];
                if (fraction >= 1) return points[i];
                return ElevationProfileBuilder.InterpolateGreatCircle(points[i - 1], points[i], fraction);
            }
            traveled += length;
        }
        return points[^1];
    }

    public static double Bearing(GeoCoordinate start, GeoCoordinate end)
    {
        var latitude1 = start.Latitude * Math.PI / 180;
        var latitude2 = end.Latitude * Math.PI / 180;
        var longitude = (end.Longitude - start.Longitude) * Math.PI / 180;
        var y = Math.Sin(longitude) * Math.Cos(latitude2);
        var x = Math.Cos(latitude1) * Math.Sin(latitude2)
            - Math.Sin(latitude1) * Math.Cos(latitude2) * Math.Cos(longitude);
        return (Math.Atan2(y, x) * 180 / Math.PI + 360) % 360;
    }

    public static double Deflection(double first, double second)
    {
        var angle = Math.Abs(first - second) % 360;
        return 180 - Math.Min(angle, 360 - angle);
    }

    public static (double Distance, double Offset, GeoCoordinate Location) Project(
        GeoCoordinate point, IReadOnlyList<GeoCoordinate> line)
    {
        var radians = Math.PI / 180;
        var scale = ElevationProfileBuilder.MeanEarthRadiusMeters * radians;
        var longitudeScale = Math.Cos(point.Latitude * radians) * scale;
        var bestDistance = double.PositiveInfinity;
        var bestOffset = 0d;
        var traveled = 0d;
        var bestLocation = line[0];
        for (var i = 1; i < line.Count; i++)
        {
            var a = line[i - 1];
            var b = line[i];
            var ax = (a.Longitude - point.Longitude) * longitudeScale;
            var ay = (a.Latitude - point.Latitude) * scale;
            var dx = (b.Longitude - a.Longitude) * longitudeScale;
            var dy = (b.Latitude - a.Latitude) * scale;
            var denominator = dx * dx + dy * dy;
            var t = denominator == 0 ? 0 : Math.Clamp(-(ax * dx + ay * dy) / denominator, 0, 1);
            var distance = Math.Sqrt(Math.Pow(ax + t * dx, 2) + Math.Pow(ay + t * dy, 2));
            var segmentLength = ElevationProfileBuilder.GreatCircleDistanceMeters(a, b);
            var offset = traveled + t * segmentLength;
            if (distance < bestDistance || (distance == bestDistance && offset < bestOffset))
            {
                bestDistance = distance;
                bestOffset = offset;
                bestLocation = ElevationProfileBuilder.InterpolateGreatCircle(a, b, t);
            }
            traveled += segmentLength;
        }
        return (bestDistance, bestOffset, bestLocation);
    }

    public static IReadOnlyList<GeoCoordinate> Slice(NetworkEdge edge, double from, double to)
    {
        var forward = from <= to;
        var low = Math.Min(from, to);
        var high = Math.Max(from, to);
        var points = new List<GeoCoordinate> { At(edge.Geometry, low) };
        var traveled = 0d;
        for (var i = 1; i < edge.Geometry.Count - 1; i++)
        {
            traveled += ElevationProfileBuilder.GreatCircleDistanceMeters(edge.Geometry[i - 1], edge.Geometry[i]);
            if (traveled > low && traveled < high) points.Add(edge.Geometry[i]);
        }
        points.Add(At(edge.Geometry, high));
        if (!forward) points.Reverse();
        return points;
    }
}
