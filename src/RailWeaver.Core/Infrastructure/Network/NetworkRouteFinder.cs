using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Infrastructure.Network;

public sealed class NetworkRouteFinder(RailwayTopology topology)
{
    public NetworkRouteResult Find(NetworkRouteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Origin.Id == request.Destination.Id)
            throw new ArgumentException("Origin and destination must be different stations.", nameof(request));
        return FindInternal(request, cancellationToken, checkDisused: true);
    }

    private NetworkRouteResult FindInternal(NetworkRouteRequest request,
        CancellationToken cancellationToken, bool checkDisused)
    {
        var searches = topology.Networks.Select(network =>
        {
            var available = network.Edges.Select(edge => edge.Status == TrackOperationalStatus.Active
                || (request.IncludeDisused && edge.Status == TrackOperationalStatus.Disused)).ToArray();
            return new NetworkSearch(network, available,
                Stops(network, request.Origin, available), Stops(network, request.Destination, available));
        }).ToArray();
        var originGauges = searches.Where(search => search.Origin.Count > 0)
            .Select(search => search.Network.Gauge.WidthMillimetres).ToArray();
        var destinationGauges = searches.Where(search => search.Destination.Count > 0)
            .Select(search => search.Network.Gauge.WidthMillimetres).ToArray();
        NetworkRoute? winner = null;
        var winnerReversals = int.MaxValue;
        var connected = false;
        foreach (var search in searches.Where(search => search.Origin.Count > 0 && search.Destination.Count > 0))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Connected(search)) continue;
            connected = true;
            var route = Search(search, request, cancellationToken);
            if (route is not null && (winner is null
                || route.Reversals.Count < winnerReversals
                || (route.Reversals.Count == winnerReversals && route.LengthMeters < winner.LengthMeters)))
            {
                winner = route;
                winnerReversals = route.Reversals.Count;
            }
        }
        if (winner is not null)
            return new(NetworkRouteStatus.Found, winner, null, originGauges, destinationGauges, null);
        var reason = originGauges.Length == 0 ? NetworkUnreachableReason.OriginWithoutTrack
            : destinationGauges.Length == 0 ? NetworkUnreachableReason.DestinationWithoutTrack
            : !searches.Any(search => search.Origin.Count > 0 && search.Destination.Count > 0)
                ? NetworkUnreachableReason.NoCommonGauge
            : connected ? NetworkUnreachableReason.NoFeasibleMovement
            : NetworkUnreachableReason.Disconnected;
        bool? withDisused = null;
        if (checkDisused && !request.IncludeDisused)
            withDisused = FindInternal(request with { IncludeDisused = true }, cancellationToken, false).Status
                == NetworkRouteStatus.Found;
        return new(NetworkRouteStatus.Unreachable, null, reason, originGauges, destinationGauges, withDisused);
    }

    private static IReadOnlyList<NetworkStop> Stops(RailwayNetwork network,
        RailwayStation station, bool[] available)
    {
        var stops = new List<NetworkStop>();
        for (var i = 0; i < network.Edges.Count; i++)
        {
            if (!available[i]) continue;
            var edge = network.Edges[i];
            var projected = NetworkGeometry.Project(station.Location, edge.Geometry);
            if (projected.Distance <= NetworkRules.StationSnapToleranceMeters)
                stops.Add(new(edge.Id, edge.TrackId, projected.Offset, projected.Location));
        }
        return stops;
    }

    private static bool Connected(NetworkSearch search)
    {
        var network = search.Network;
        var sets = new int[network.Nodes.Count];
        for (var i = 0; i < sets.Length; i++) sets[i] = i;
        int Find(int x) => sets[x] == x ? x : sets[x] = Find(sets[x]);
        for (var i = 0; i < network.Edges.Count; i++)
            if (search.Available[i])
            {
                var edge = network.Edges[i];
                sets[Find(edge.StartNode)] = Find(edge.EndNode);
            }
        var indices = network.Edges.Select((edge, i) => (edge.Id, i)).ToDictionary(x => x.Id, x => x.i);
        var origins = search.Origin.Select(stop =>
        {
            var edge = network.Edges[indices[stop.EdgeId]];
            return Find(edge.StartNode);
        }).ToHashSet();
        return search.Destination.Any(stop =>
        {
            var edge = network.Edges[indices[stop.EdgeId]];
            return origins.Contains(Find(edge.StartNode));
        });
    }

    private static NetworkRoute? Search(NetworkSearch search, NetworkRouteRequest request,
        CancellationToken cancellationToken)
    {
        var network = search.Network;
        var edges = network.Edges;
        var indices = edges.Select((edge, i) => (edge.Id, i)).ToDictionary(x => x.Id, x => x.i);
        var destinations = search.Destination.GroupBy(stop => indices[stop.EdgeId])
            .ToDictionary(group => group.Key, group => group.ToArray());
        var states = new State?[edges.Count * 2];
        var queue = new PriorityQueue<(int Index, State State), (int Reversals, double Meters, int Index)>();
        Candidate? best = null;
        void Consider(State state, int index)
        {
            var edge = edges[index / 2];
            if (!destinations.TryGetValue(index / 2, out var stops)) return;
            foreach (var stop in stops)
            {
                var direction = index % 2 == 0 ? 1 : -1;
                if (direction * (stop.OffsetMeters - state.FromOffset) < -1e-8) continue;
                var toEnd = direction == 1 ? edge.LengthMeters - stop.OffsetMeters : stop.OffsetMeters;
                var meters = state.Meters - toEnd;
                var candidate = new Candidate(state, index, stop, state.Reversals, meters);
                if (Better(candidate, best)) best = candidate;
            }
        }
        void Enqueue(State state, int index)
        {
            var old = states[index];
            if (old is not null && (old.Reversals < state.Reversals
                || (old.Reversals == state.Reversals && old.Meters <= state.Meters))) return;
            states[index] = state;
            queue.Enqueue((index, state), (state.Reversals, state.Meters, index));
            Consider(state, index);
        }
        foreach (var stop in search.Origin)
        {
            var edgeIndex = indices[stop.EdgeId];
            var edge = edges[edgeIndex];
            Enqueue(new State(null, stop, stop.OffsetMeters, 0, edge.LengthMeters - stop.OffsetMeters,
                false), edgeIndex * 2);
            Enqueue(new State(null, stop, stop.OffsetMeters, 0, stop.OffsetMeters,
                false), edgeIndex * 2 + 1);
        }
        while (queue.TryDequeue(out var item, out var priority))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (states[item.Index] != item.State) continue;
            if (best is not null && (priority.Reversals > best.Reversals
                || (priority.Reversals == best.Reversals && priority.Meters > best.Meters))) break;
            var edgeIndex = item.Index / 2;
            var edge = edges[edgeIndex];
            var node = network.Nodes[item.Index % 2 == 0 ? edge.EndNode : edge.StartNode];
            var incoming = node.Legs.Single(leg => leg.EdgeIndex == edgeIndex
                && leg.AtStart == (item.Index % 2 == 1));
            foreach (var outgoing in node.Legs)
            {
                if (outgoing == incoming || !search.Available[outgoing.EdgeIndex]) continue;
                var deflection = NetworkGeometry.Deflection(incoming.BearingDegrees,
                    outgoing.BearingDegrees);
                var reversal = false;
                if (deflection > NetworkRules.MaxDeflectionDegrees)
                {
                    reversal = node.Legs.Any(third => third != incoming && third != outgoing
                        && search.Available[third.EdgeIndex]
                        && NetworkGeometry.Deflection(incoming.BearingDegrees,
                            third.BearingDegrees) <= NetworkRules.MaxDeflectionDegrees
                        && NetworkGeometry.Deflection(third.BearingDegrees,
                            outgoing.BearingDegrees) <= NetworkRules.MaxDeflectionDegrees);
                    if (!reversal) continue;
                }
                var next = edges[outgoing.EdgeIndex];
                var direction = outgoing.AtStart ? 0 : 1;
                Enqueue(new State(item.State, item.State.Origin, direction == 0 ? 0 : next.LengthMeters,
                    item.State.Reversals + (reversal ? 1 : 0), item.State.Meters + next.LengthMeters,
                    reversal, item.Index), outgoing.EdgeIndex * 2 + direction);
            }
        }
        return best is null ? null : BuildRoute(network, best, request);
    }

    private static bool Better(Candidate candidate, Candidate? best) => best is null
        || candidate.Reversals < best.Reversals
        || (candidate.Reversals == best.Reversals && candidate.Meters < best.Meters)
        || (candidate.Reversals == best.Reversals && candidate.Meters == best.Meters
            && candidate.Index < best.Index);

    private static NetworkRoute BuildRoute(RailwayNetwork network, Candidate candidate,
        NetworkRouteRequest request)
    {
        var chain = new List<(State State, int Index)>();
        var state = candidate.State;
        var index = candidate.Index;
        while (state is not null)
        {
            chain.Add((state, index));
            if (state.Previous is null) break;
            var previous = state.Previous;
            // The predecessor index is held on each state to avoid ambiguity on closed edges.
            index = state.PreviousIndex;
            state = previous;
        }
        chain.Reverse();
        var legs = new List<NetworkLeg>();
        var geometry = new List<GeoCoordinate>();
        var reversals = new List<NetworkReversal>();
        var segments = new List<NetworkSegment>();
        var distance = 0d;
        var active = 0d;
        var disused = 0d;
        var inferred = 0d;
        foreach (var (part, partIndex) in chain)
        {
            var edge = network.Edges[partIndex / 2];
            var to = part == candidate.State ? candidate.Destination.OffsetMeters
                : partIndex % 2 == 0 ? edge.LengthMeters : 0;
            var length = Math.Abs(to - part.FromOffset);
            if (part.ReversalBefore)
                reversals.Add(new(partIndex % 2 == 0 ? edge.Geometry[0] : edge.Geometry[^1], distance));
            legs.Add(new(edge.Id, edge.TrackId,
                partIndex % 2 == 0 ? NetworkDirection.Forward : NetworkDirection.Backward,
                part.FromOffset, to));
            foreach (var point in NetworkGeometry.Slice(edge, part.FromOffset, to))
                if (geometry.Count == 0 || ElevationProfileBuilder.GreatCircleDistanceMeters(geometry[^1], point) > 1e-6)
                    geometry.Add(point);
            distance += length;
            if (edge.Status == TrackOperationalStatus.Active) active += length;
            else disused += length;
            if (edge.GaugeSource == GaugeSource.InferredFromConnection) inferred += length;
            if (segments.Count > 0 && segments[^1].TrackId == edge.TrackId)
                segments[^1] = segments[^1] with { LengthMeters = segments[^1].LengthMeters + length };
            else segments.Add(new(edge.TrackId, edge.Track.Name, edge.Track.LineReference, edge.Status, length));
        }
        return new NetworkRoute(network.Gauge, legs, geometry, distance,
            ElevationProfileBuilder.GreatCircleDistanceMeters(request.Origin.Location,
                request.Destination.Location), reversals, new(active, disused), inferred,
            segments, chain[0].State.Origin, candidate.Destination);
    }

    private sealed record NetworkSearch(RailwayNetwork Network, bool[] Available,
        IReadOnlyList<NetworkStop> Origin, IReadOnlyList<NetworkStop> Destination);
    private sealed record State(State? Previous, NetworkStop Origin, double FromOffset,
        int Reversals, double Meters, bool ReversalBefore, int PreviousIndex = -1);
    private sealed record Candidate(State State, int Index, NetworkStop Destination,
        int Reversals, double Meters);
}
