using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Planning;

public sealed class CorridorFinder(IElevationSource elevationSource)
{
    public const double GradientPenalty = 1;
    private const int MaximumGridNodes = 1_000_000;
    private static readonly (int Row, int Column)[] NeighborOffsets =
    [
        (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1),
        (-2, -1), (-2, 1), (-1, -2), (-1, 2), (1, -2), (1, 2), (2, -1), (2, 1),
    ];

    public CorridorResult Find(CorridorRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SearchLimit);
        var curveParameters = CurvatureRules.For(request.Gauge);
        if (!double.IsFinite(request.DesignSpeedKmh) || request.DesignSpeedKmh is < 20 or > 120)
            throw new ArgumentException("Design speed must be between 20 and 120 km/h.", nameof(request));
        if (!double.IsFinite(request.MaxGradientPermille) || request.MaxGradientPermille is < 1 or > 40)
            throw new ArgumentException("Maximum gradient must be between 1 and 40 permille.", nameof(request));
        if (!request.SearchLimit.Contains(request.Origin) || !request.SearchLimit.Contains(request.Destination))
            throw new ArgumentException("Both endpoints must be inside the search limit.", nameof(request));

        var directDistance = ElevationProfileBuilder.GreatCircleDistanceMeters(request.Origin, request.Destination);
        if (directDistance < 1_000)
            throw new ArgumentException("Endpoints must be at least 1 km apart.", nameof(request));

        var metersPerLatitude = ElevationProfileBuilder.MeanEarthRadiusMeters * Math.PI / 180;
        var referenceLatitude = (request.Origin.Latitude + request.Destination.Latitude) / 2 * Math.PI / 180;
        var metersPerLongitude = metersPerLatitude * Math.Cos(referenceLatitude);
        if (metersPerLongitude <= 0)
            throw new ArgumentException("A search grid cannot be created at the poles.", nameof(request));
        var margin = Math.Max(directDistance * 0.25, 10_000);
        var limit = request.SearchLimit;
        var bounds = new GeoBoundingBox(
            Math.Max(limit.West, Math.Min(request.Origin.Longitude, request.Destination.Longitude) - margin / metersPerLongitude),
            Math.Max(limit.South, Math.Min(request.Origin.Latitude, request.Destination.Latitude) - margin / metersPerLatitude),
            Math.Min(limit.East, Math.Max(request.Origin.Longitude, request.Destination.Longitude) + margin / metersPerLongitude),
            Math.Min(limit.North, Math.Max(request.Origin.Latitude, request.Destination.Latitude) + margin / metersPerLatitude));

        var step = 1_000d;
        var columns = 0;
        var rows = 0;
        foreach (var candidate in new[] { 250d, 500d, 1_000d })
        {
            var candidateColumns = (int)Math.Floor((bounds.East - bounds.West) * metersPerLongitude / candidate) + 1;
            var candidateRows = (int)Math.Floor((bounds.North - bounds.South) * metersPerLatitude / candidate) + 1;
            if ((long)candidateColumns * candidateRows > MaximumGridNodes) continue;
            step = candidate; columns = candidateColumns; rows = candidateRows;
            break;
        }
        if (columns == 0)
            throw new ArgumentException("Search limit exceeds the grid budget.", nameof(request));

        var nodeCount = columns * rows;
        var originIndex = nodeCount;
        var destinationIndex = nodeCount + 1;
        var latitudeStep = step / metersPerLatitude;
        var longitudeStep = step / metersPerLongitude;
        var coordinates = new GeoCoordinate[nodeCount + 2];
        var elevations = new double?[nodeCount + 2];
        var sampled = new bool[nodeCount + 2];
        for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
                coordinates[row * columns + column] = new GeoCoordinate(
                    bounds.South + row * latitudeStep, bounds.West + column * longitudeStep);
        coordinates[originIndex] = request.Origin;
        coordinates[destinationIndex] = request.Destination;

        int[] EndpointCorners(GeoCoordinate coordinate)
        {
            var row = Math.Clamp((int)Math.Floor((coordinate.Latitude - bounds.South) / latitudeStep), 0, rows - 1);
            var column = Math.Clamp((int)Math.Floor((coordinate.Longitude - bounds.West) / longitudeStep), 0, columns - 1);
            return new[] { row * columns + column,
                row * columns + Math.Min(column + 1, columns - 1),
                Math.Min(row + 1, rows - 1) * columns + column,
                Math.Min(row + 1, rows - 1) * columns + Math.Min(column + 1, columns - 1) }
                .Distinct().ToArray();
        }
        var originCorners = EndpointCorners(request.Origin);
        var destinationCorners = EndpointCorners(request.Destination);
        var startIndex = originCorners.FirstOrDefault(corner =>
            ElevationProfileBuilder.GreatCircleDistanceMeters(request.Origin, coordinates[corner]) < 1, -1);
        if (startIndex < 0) startIndex = originIndex;
        var targetIndex = destinationCorners.FirstOrDefault(corner =>
            ElevationProfileBuilder.GreatCircleDistanceMeters(request.Destination, coordinates[corner]) < 1, -1);
        if (targetIndex < 0) targetIndex = destinationIndex;

        double? Elevation(int index)
        {
            if (!sampled[index])
            {
                elevations[index] = elevationSource.GetElevationMeters(coordinates[index]);
                sampled[index] = true;
            }
            return elevations[index];
        }

        CorridorSearchInfo Search(int explored) => new(step, columns, rows, explored, bounds);
        if (Elevation(originIndex) is null || Elevation(destinationIndex) is null
            || Elevation(startIndex) is null || Elevation(targetIndex) is null)
            return new CorridorResult(CorridorStatus.EndpointWithoutElevation, null, Search(0), null);

        var projection = new AzimuthalEquidistantProjection(new GeoCoordinate(
            (request.Origin.Latitude + request.Destination.Latitude) / 2,
            (request.Origin.Longitude + request.Destination.Longitude) / 2));
        var projected = coordinates.Select(projection.Project).ToArray();
        var startState = startIndex * 17 + 16;

        (int[]? Parents, int Final, int Explored) Run(double minimumRadius)
        {
            var stateCount = (nodeCount + 2) * 17;
            var costs = new double[stateCount];
            Array.Fill(costs, double.PositiveInfinity);
            var parents = new int[stateCount];
            Array.Fill(parents, -1);
            var closed = new bool[stateCount];
            var queue = new PriorityQueue<(int State, double Cost), (double F, int State)>();
            costs[startState] = 0;
            queue.Enqueue((startState, 0),
                (ElevationProfileBuilder.GreatCircleDistanceMeters(coordinates[startIndex], coordinates[targetIndex]), startState));
            var explored = 0;
            while (queue.TryDequeue(out var entry, out _))
            {
                explored++;
                if (explored % 10_000 == 0) cancellationToken.ThrowIfCancellationRequested();
                var state = entry.State;
                if (closed[state] || entry.Cost != costs[state]) continue;
                var current = state / 17;
                if (current == targetIndex) return (parents, state, explored);
                closed[state] = true;
                var previousState = parents[state];
                var previous = previousState < 0 ? -1 : previousState / 17;
                void Visit(int next, int heading)
                {
                    var nextState = next * 17 + heading;
                    if (closed[nextState] || Elevation(next) is not { } nextElevation) return;
                    var distance = ElevationProfileBuilder.GreatCircleDistanceMeters(coordinates[current], coordinates[next]);
                    if (distance == 0) return;
                    var gradient = Math.Abs(nextElevation - elevations[current]!.Value) / distance * 1_000;
                    if (gradient > request.MaxGradientPermille) return;
                    if (previous >= 0 && minimumRadius > 0)
                    {
                        var incoming = projected[current] - projected[previous];
                        var outgoing = projected[next] - projected[current];
                        var angle = Math.Abs(Math.Atan2(incoming.X * outgoing.Y - incoming.Y * outgoing.X,
                            incoming.X * outgoing.X + incoming.Y * outgoing.Y));
                        var availableIn = incoming.Length * (previous == originIndex ? 1 : 0.5);
                        var availableOut = outgoing.Length * (next == destinationIndex ? 1 : 0.5);
                        if (minimumRadius * Math.Tan(angle / 2) > Math.Min(availableIn, availableOut)) return;
                    }
                    var ratio = gradient / request.MaxGradientPermille;
                    var cost = costs[state] + distance * (1 + GradientPenalty * ratio * ratio);
                    if (cost >= costs[nextState]) return;
                    costs[nextState] = cost;
                    parents[nextState] = state;
                    var heuristic = ElevationProfileBuilder.GreatCircleDistanceMeters(coordinates[next], coordinates[targetIndex]);
                    queue.Enqueue((nextState, cost), (cost + heuristic, nextState));
                }
                if (current == originIndex)
                {
                    foreach (var next in originCorners) Visit(next, 16);
                    continue;
                }
                var row = current / columns;
                var column = current % columns;
                for (var heading = 0; heading < NeighborOffsets.Length; heading++)
                {
                    var (rowOffset, columnOffset) = NeighborOffsets[heading];
                    var nextRow = row + rowOffset;
                    var nextColumn = column + columnOffset;
                    if (nextRow >= 0 && nextRow < rows && nextColumn >= 0 && nextColumn < columns)
                        Visit(nextRow * columns + nextColumn, heading);
                }
                if (targetIndex == destinationIndex && destinationCorners.Contains(current))
                    Visit(destinationIndex, 16);
            }
            return (null, -1, explored);
        }

        var run = Run(curveParameters.AbsoluteMinimumRadiusMeters);
        if (run.Parents is null)
        {
            var fallback = Run(0);
            return new CorridorResult(CorridorStatus.NoFeasiblePath, null,
                Search(run.Explored), fallback.Parents is not null);
        }
        var indices = new List<int>();
        for (var state = run.Final; state >= 0; state = run.Parents[state]) indices.Add(state / 17);
        indices.Reverse();
        var path = indices.Select(index => coordinates[index]).ToArray();
        return new CorridorResult(CorridorStatus.Found,
            BuildCorridor(path, step, request, curveParameters, projection, directDistance),
            Search(run.Explored), null);
    }

    private CandidateCorridor BuildCorridor(IReadOnlyList<GeoCoordinate> path, double step,
        CorridorRequest request, GaugeCurveParameters parameters,
        AzimuthalEquidistantProjection projection, double directDistance)
    {
        var built = new AlignmentBuilder().Build(path, step, parameters, request.DesignSpeedKmh, projection);
        var alignment = built.Alignment;
        var sections = built.Sections;
        var terrain = new ElevationProfileBuilder(elevationSource).Build(alignment);
        var length = terrain.TotalDistanceMeters;
        var intervals = Math.Max(1, (int)Math.Ceiling(length / step));
        var delta = length / intervals;
        var reference = new double?[intervals + 1];
        var sums = new double[reference.Length];
        var counts = new int[reference.Length];
        foreach (var sample in terrain.Samples)
        {
            if (sample.ElevationMeters is not { } elevation) continue;
            var first = Math.Max(0, (int)Math.Ceiling((sample.DistanceMeters - delta / 2) / delta));
            var last = Math.Min(intervals, (int)Math.Floor((sample.DistanceMeters + delta / 2) / delta));
            for (var i = first; i <= last; i++) { sums[i] += elevation; counts[i]++; }
        }
        for (var i = 0; i < reference.Length; i++)
            if (counts[i] > 0) reference[i] = sums[i] / counts[i];
        var limits = new double[intervals];
        for (var i = 0; i < intervals; i++)
        {
            var resistance = 0d;
            foreach (var section in sections)
                if (section.Kind == SectionKind.Curve && section.FromMeters < (i + 1) * delta
                    && section.ToMeters > i * delta)
                    resistance = Math.Max(resistance, parameters.CurveResistanceConstant / section.RadiusMeters!.Value);
            limits[i] = Math.Max(0, request.MaxGradientPermille - resistance);
        }
        var upper = new double[reference.Length];
        var lower = new double[reference.Length];
        for (var i = 0; i < reference.Length; i++)
        {
            upper[i] = reference[i] ?? double.PositiveInfinity;
            lower[i] = reference[i] ?? double.NegativeInfinity;
        }
        for (var i = 1; i < reference.Length; i++)
        {
            var change = limits[i - 1] / 1000 * delta;
            upper[i] = Math.Min(upper[i], upper[i - 1] + change);
            lower[i] = Math.Max(lower[i], lower[i - 1] - change);
        }
        for (var i = intervals - 1; i >= 0; i--)
        {
            var change = limits[i] / 1000 * delta;
            upper[i] = Math.Min(upper[i], upper[i + 1] + change);
            lower[i] = Math.Max(lower[i], lower[i + 1] - change);
        }
        var track = new TrackProfilePoint[reference.Length];
        var segment = 0;
        var segmentStart = 0d;
        var ascent = 0d;
        var descent = 0d;
        var maxGradient = 0d;
        var bands = new double[4];
        for (var i = 0; i < track.Length; i++)
        {
            var distance = i == intervals ? length : i * delta;
            while (segment < alignment.Count - 2 &&
                   segmentStart + ElevationProfileBuilder.GreatCircleDistanceMeters(alignment[segment], alignment[segment + 1]) < distance)
            {
                segmentStart += ElevationProfileBuilder.GreatCircleDistanceMeters(alignment[segment], alignment[segment + 1]);
                segment++;
            }
            var span = ElevationProfileBuilder.GreatCircleDistanceMeters(alignment[segment], alignment[segment + 1]);
            var coordinate = ElevationProfileBuilder.InterpolateGreatCircle(alignment[segment], alignment[segment + 1],
                Math.Clamp((distance - segmentStart) / span, 0, 1));
            var elevation = (upper[i] + lower[i]) / 2;
            double? gradient = null;
            if (i > 0)
            {
                var change = elevation - track[i - 1].ElevationMeters;
                ascent += Math.Max(0, change);
                descent += Math.Max(0, -change);
                gradient = change / delta * 1000;
                maxGradient = Math.Max(maxGradient, Math.Abs(gradient.Value));
                var band = Math.Clamp((int)Math.Ceiling(Math.Abs(gradient.Value) / request.MaxGradientPermille * 4) - 1, 0, 3);
                bands[band] += delta;
            }
            track[i] = new TrackProfilePoint(distance, coordinate, elevation, gradient,
                i == 0 ? null : limits[i - 1]);
        }
        var maxCut = 0d;
        var maxFill = 0d;
        foreach (var sample in terrain.Samples)
        {
            if (sample.ElevationMeters is not { } ground) continue;
            var index = Math.Min(intervals - 1, (int)(sample.DistanceMeters / delta));
            var fraction = (sample.DistanceMeters - track[index].DistanceMeters) / delta;
            var height = track[index].ElevationMeters +
                (track[index + 1].ElevationMeters - track[index].ElevationMeters) * fraction;
            maxCut = Math.Max(maxCut, ground - height);
            maxFill = Math.Max(maxFill, height - ground);
        }
        var curves = sections.Where(section => section.Kind == SectionKind.Curve).ToArray();
        var metrics = new CorridorMetrics(length, directDistance, length / directDistance, maxGradient,
            request.MaxGradientPermille, ascent, descent, track.Min(point => point.ElevationMeters),
            track.Max(point => point.ElevationMeters),
            Enumerable.Range(0, 4).Select(i => new GradientBand(i * 25, (i + 1) * 25, bands[i])).ToArray(),
            maxCut, maxFill, request.Gauge, request.DesignSpeedKmh,
            CurvatureRules.DesignRadius(parameters, request.DesignSpeedKmh), parameters.AbsoluteMinimumRadiusMeters,
            curves.Length, curves.Count(curve => curve.SpeedLimitKmh < request.DesignSpeedKmh),
            curves.Length == 0 ? null : curves.Min(curve => curve.RadiusMeters),
            sections.Min(section => section.SpeedLimitKmh), curves.Sum(curve => curve.ToMeters - curve.FromMeters));
        return new CandidateCorridor(alignment, sections, track, terrain, metrics);
    }
}
