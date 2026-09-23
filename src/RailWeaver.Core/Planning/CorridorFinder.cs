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
            return new CorridorResult(CorridorStatus.EndpointWithoutElevation, null, Search(0));

        var costs = new double[nodeCount + 2];
        Array.Fill(costs, double.PositiveInfinity);
        var parents = new int[nodeCount + 2];
        Array.Fill(parents, -1);
        var closed = new bool[nodeCount + 2];
        var queue = new PriorityQueue<(int Index, double Cost), (double F, int Index)>();
        costs[startIndex] = 0;
        queue.Enqueue((startIndex, 0), (ElevationProfileBuilder.GreatCircleDistanceMeters(coordinates[startIndex], coordinates[targetIndex]), startIndex));
        var explored = 0;
        while (queue.TryDequeue(out var entry, out _))
        {
            explored++;
            if (explored % 10_000 == 0) cancellationToken.ThrowIfCancellationRequested();
            var current = entry.Index;
            if (closed[current] || entry.Cost != costs[current]) continue;
            if (current == targetIndex)
                return new CorridorResult(CorridorStatus.Found,
                    BuildCorridor(current, parents, coordinates, elevations, request.MaxGradientPermille, directDistance), Search(explored));
            closed[current] = true;
            IEnumerable<int> Neighbors()
            {
                if (current == originIndex) return originCorners;
                if (current == destinationIndex) return destinationCorners;
                var row = current / columns;
                var column = current % columns;
                var neighbors = new List<int>(18);
                foreach (var (rowOffset, columnOffset) in NeighborOffsets)
                {
                    var nextRow = row + rowOffset;
                    var nextColumn = column + columnOffset;
                    if (nextRow >= 0 && nextRow < rows && nextColumn >= 0 && nextColumn < columns)
                        neighbors.Add(nextRow * columns + nextColumn);
                }
                if (startIndex == originIndex && originCorners.Contains(current)) neighbors.Add(originIndex);
                if (targetIndex == destinationIndex && destinationCorners.Contains(current)) neighbors.Add(destinationIndex);
                return neighbors;
            }
            foreach (var next in Neighbors())
            {
                if (closed[next] || Elevation(next) is not { } nextElevation) continue;
                var distance = ElevationProfileBuilder.GreatCircleDistanceMeters(coordinates[current], coordinates[next]);
                if (distance == 0) continue;
                var gradient = Math.Abs(nextElevation - elevations[current]!.Value) / distance * 1_000;
                if (gradient > request.MaxGradientPermille) continue;
                var ratio = gradient / request.MaxGradientPermille;
                var cost = costs[current] + distance * (1 + GradientPenalty * ratio * ratio);
                if (cost >= costs[next]) continue;
                costs[next] = cost;
                parents[next] = current;
                var heuristic = ElevationProfileBuilder.GreatCircleDistanceMeters(coordinates[next], coordinates[targetIndex]);
                queue.Enqueue((next, cost), (cost + heuristic, next));
            }
        }
        return new CorridorResult(CorridorStatus.NoFeasiblePath, null, Search(explored));
    }

    private CandidateCorridor BuildCorridor(int destination, int[] parents, GeoCoordinate[] coordinates,
        double?[] elevations, double limit, double directDistance)
    {
        var indices = new List<int>();
        for (var current = destination; current >= 0; current = parents[current]) indices.Add(current);
        indices.Reverse();
        var alignment = indices.Select(index => coordinates[index]).ToArray();
        var track = new TrackProfilePoint[indices.Count];
        var length = 0d;
        var ascent = 0d;
        var descent = 0d;
        var maxGradient = 0d;
        var bands = new double[4];
        for (var index = 0; index < indices.Count; index++)
        {
            var elevation = elevations[indices[index]]!.Value;
            double? gradient = null;
            if (index > 0)
            {
                var distance = ElevationProfileBuilder.GreatCircleDistanceMeters(alignment[index - 1], alignment[index]);
                var change = elevation - track[index - 1].ElevationMeters;
                length += distance;
                ascent += Math.Max(0, change);
                descent += Math.Max(0, -change);
                gradient = change / distance * 1_000;
                maxGradient = Math.Max(maxGradient, Math.Abs(gradient.Value));
                var band = Math.Min(3, Math.Max(0, (int)Math.Ceiling(Math.Abs(gradient.Value) / limit * 4) - 1));
                bands[band] += distance;
            }
            track[index] = new TrackProfilePoint(length, alignment[index], elevation, gradient);
        }
        var terrain = new ElevationProfileBuilder(elevationSource).Build(alignment);
        var segment = 0;
        var maxCut = 0d;
        var maxFill = 0d;
        foreach (var sample in terrain.Samples)
        {
            if (sample.ElevationMeters is not { } ground) continue;
            while (segment < track.Length - 2 && sample.DistanceMeters > track[segment + 1].DistanceMeters) segment++;
            var first = track[segment];
            var second = track[segment + 1];
            var fraction = (sample.DistanceMeters - first.DistanceMeters) / (second.DistanceMeters - first.DistanceMeters);
            var trackElevation = first.ElevationMeters + (second.ElevationMeters - first.ElevationMeters) * fraction;
            maxCut = Math.Max(maxCut, ground - trackElevation);
            maxFill = Math.Max(maxFill, trackElevation - ground);
        }
        var metrics = new CorridorMetrics(length, directDistance, length / directDistance, maxGradient, limit,
            ascent, descent, track.Min(point => point.ElevationMeters), track.Max(point => point.ElevationMeters),
            Enumerable.Range(0, 4).Select(index => new GradientBand(index * 25, (index + 1) * 25, bands[index])).ToArray(),
            maxCut, maxFill);
        return new CandidateCorridor(alignment, track, terrain, metrics);
    }
}
