using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Planning;

public sealed record BuiltAlignment(IReadOnlyList<GeoCoordinate> Alignment,
    IReadOnlyList<AlignmentSection> Sections);

public sealed class AlignmentBuilder
{
    private sealed record Curve(int Index, double Radius, double Tangent, double Deflection,
        TurnDirection Direction);

    public BuiltAlignment Build(IReadOnlyList<GeoCoordinate> path, double gridStepMeters,
        GaugeCurveParameters parameters, double designSpeedKmh, AzimuthalEquidistantProjection projection)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Count < 2) throw new ArgumentException("An alignment needs two endpoints.", nameof(path));
        var points = path.Select(projection.Project).ToArray();
        var kept = new SortedSet<int> { 0, points.Length - 1 };
        void Simplify(int first, int last)
        {
            if (last - first < 2) return;
            var chord = points[last] - points[first];
            var denominator = chord.X * chord.X + chord.Y * chord.Y;
            var farthest = 0d;
            var farthestIndex = -1;
            for (var index = first + 1; index < last; index++)
            {
                var offset = points[index] - points[first];
                var fraction = denominator == 0 ? 0 : Math.Clamp(Dot(chord, offset) / denominator, 0, 1);
                var distance = (offset - chord * fraction).Length;
                if (distance > farthest)
                {
                    farthest = distance;
                    farthestIndex = index;
                }
            }
            if (farthest <= gridStepMeters * CurvatureRules.SimplificationToleranceFactor) return;
            kept.Add(farthestIndex);
            Simplify(first, farthestIndex);
            Simplify(farthestIndex, last);
        }
        Simplify(0, points.Length - 1);
        var designRadius = CurvatureRules.DesignRadius(parameters, designSpeedKmh);
        Curve?[] curves;
        while (true)
        {
            var indices = kept.ToArray();
            curves = new Curve?[indices.Length];
            var tangents = new double[indices.Length];
            var deflections = new double[indices.Length];
            var directions = new TurnDirection[indices.Length];
            for (var k = 1; k < indices.Length - 1; k++)
            {
                var incoming = points[indices[k]] - points[indices[k - 1]];
                var outgoing = points[indices[k + 1]] - points[indices[k]];
                var cross = Cross(incoming, outgoing);
                var angle = Math.Abs(Math.Atan2(cross, Dot(incoming, outgoing)));
                if (angle < 1e-12) continue;
                tangents[k] = Math.Tan(angle / 2);
                deflections[k] = angle;
                directions[k] = cross > 0 ? TurnDirection.Left : TurnDirection.Right;
            }
            var worst = -1;
            var worstRadius = double.PositiveInfinity;
            for (var k = 1; k < indices.Length - 1; k++)
            {
                if (tangents[k] == 0) continue;
                var previousLength = (points[indices[k]] - points[indices[k - 1]]).Length;
                var nextLength = (points[indices[k + 1]] - points[indices[k]]).Length;
                var radius = Math.Min(designRadius, Math.Min(
                    previousLength / (tangents[k - 1] + tangents[k]),
                    nextLength / (tangents[k] + tangents[k + 1])));
                curves[k] = new Curve(k, radius, radius * tangents[k], deflections[k], directions[k]);
                if (radius < worstRadius) { worstRadius = radius; worst = k; }
            }
            if (worst < 0 || worstRadius >= parameters.AbsoluteMinimumRadiusMeters * (1 - CurvatureRules.RadiusTolerance)) break;
            var before = kept.Count;
            for (var index = indices[worst - 1] + 1; index < indices[worst + 1]; index++) kept.Add(index);
            if (before == kept.Count) throw new InvalidOperationException("The path cannot meet its absolute curve radius.");
        }

        var pi = kept.Select(index => points[index]).ToArray();
        var output = new List<GeoCoordinate>();
        var sections = new List<AlignmentSection>();
        var length = 0d;
        void Append(ProjectedPoint point)
        {
            var coordinate = projection.Unproject(point);
            if (output.Count > 0)
            {
                var increment = ElevationProfileBuilder.GreatCircleDistanceMeters(output[^1], coordinate);
                if (increment < 0.001) return;
                length += increment;
            }
            output.Add(coordinate);
        }
        void Tangent(ProjectedPoint endpoint)
        {
            var from = length;
            Append(endpoint);
            if (length > from) sections.Add(new AlignmentSection(SectionKind.Tangent, from, length,
                null, null, null, designSpeedKmh, null));
        }
        Append(pi[0]);
        for (var k = 1; k < pi.Length - 1; k++)
        {
            if (curves[k] is not { } curve) { Tangent(pi[k]); continue; }
            var incoming = pi[k] - pi[k - 1];
            var unit = incoming * (1 / incoming.Length);
            var pc = pi[k] - unit * curve.Tangent;
            Tangent(pc);
            var left = curve.Direction == TurnDirection.Left;
            var normal = new ProjectedPoint(-unit.Y, unit.X) * (left ? 1 : -1);
            var center = pc + normal * curve.Radius;
            var radial = pc - center;
            var subdivisions = Math.Max(1, (int)Math.Ceiling(curve.Radius * curve.Deflection / CurvatureRules.MaxArcChordMeters));
            var from = length;
            for (var part = 1; part <= subdivisions; part++)
            {
                var angle = (left ? 1 : -1) * curve.Deflection * part / subdivisions;
                Append(center + new ProjectedPoint(
                    radial.X * Math.Cos(angle) - radial.Y * Math.Sin(angle),
                    radial.X * Math.Sin(angle) + radial.Y * Math.Cos(angle)));
            }
            var to = length;
            if (to > from)
            {
                var halfway = (from + to) / 2;
                var segment = 0;
                var distance = 0d;
                while (segment < output.Count - 1)
                {
                    var interval = ElevationProfileBuilder.GreatCircleDistanceMeters(output[segment], output[segment + 1]);
                    if (distance + interval >= halfway) break;
                    distance += interval;
                    segment++;
                }
                var span = ElevationProfileBuilder.GreatCircleDistanceMeters(output[segment], output[segment + 1]);
                var midpoint = ElevationProfileBuilder.InterpolateGreatCircle(output[segment], output[segment + 1],
                    (halfway - distance) / span);
                sections.Add(new AlignmentSection(SectionKind.Curve, from, to, curve.Radius,
                    curve.Deflection * 180 / Math.PI, curve.Direction,
                    CurvatureRules.SpeedLimit(parameters, curve.Radius, designSpeedKmh), midpoint));
            }
        }
        Tangent(pi[^1]);
        return new BuiltAlignment(output, sections);
    }

    private static double Cross(ProjectedPoint a, ProjectedPoint b) => a.X * b.Y - a.Y * b.X;
    private static double Dot(ProjectedPoint a, ProjectedPoint b) => a.X * b.X + a.Y * b.Y;
}
