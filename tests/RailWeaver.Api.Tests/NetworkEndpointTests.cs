using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RailWeaver.Api.Network;
using RailWeaver.Api.Railways;
using RailWeaver.Core.Infrastructure.Network;

namespace RailWeaver.Api.Tests;

public sealed class NetworkEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public NetworkEndpointTests(WebApplicationFactory<Program> factory) => client = factory.CreateClient();

    [Fact]
    public async Task NetworkEndpoints_ValidateRegionAndStationIds()
    {
        var missingNetwork = await client.GetAsync("/api/regions/unknown/network", TestContext.Current.CancellationToken);
        var missingRoute = await client.PostAsJsonAsync("/api/regions/unknown/network/routes",
            new { originStationId = "a", destinationStationId = "b" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, missingNetwork.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingRoute.StatusCode);
        var missingField = await client.PostAsJsonAsync("/api/regions/cordoba/network/routes",
            new { originStationId = "a" }, TestContext.Current.CancellationToken);
        var unknownStation = await client.PostAsJsonAsync("/api/regions/cordoba/network/routes",
            new { originStationId = "a", destinationStationId = "b" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, missingField.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownStation.StatusCode);
        Assert.Contains("message", await missingField.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(NetworkUnreachableReason.OriginWithoutTrack, "origin_without_track")]
    [InlineData(NetworkUnreachableReason.DestinationWithoutTrack, "destination_without_track")]
    [InlineData(NetworkUnreachableReason.NoCommonGauge, "no_common_gauge")]
    [InlineData(NetworkUnreachableReason.Disconnected, "disconnected")]
    [InlineData(NetworkUnreachableReason.NoFeasibleMovement, "no_feasible_movement")]
    public void UnreachableReasons_HaveStableHttpNames(NetworkUnreachableReason reason, string expected)
    {
        var result = new NetworkRouteResult(NetworkRouteStatus.Unreachable, null, reason, [1000], [1676], false);
        var response = NetworkRouteResponse.FromDomain(result, false, null, null);
        Assert.Equal("unreachable", response.Status);
        Assert.Equal(expected, response.Reason);
    }

    [Fact]
    public async Task CordobaNetwork_HasExpectedTopologyAndDiagnostics()
    {
        var response = await client.GetAsync("/api/regions/cordoba/network", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = json.RootElement;
        var networks = root.GetProperty("networks").EnumerateArray().ToArray();
        Assert.Equal(2, networks.Length);
        Assert.Equal(1168, networks[0].GetProperty("edges").GetInt32());
        Assert.Equal(2785, networks[1].GetProperty("edges").GetInt32());
        Assert.Equal(504, root.GetProperty("inferredGauges").GetArrayLength());
        Assert.Equal(44, root.GetProperty("diagnostics").EnumerateArray().Count(item =>
            item.GetProperty("kind").GetString() == "track_without_gauge"));
        var diagnostics = root.GetProperty("diagnostics").EnumerateArray().ToArray();
        foreach (var (gauge, boundary, gap, end, joint) in new[]
        {
            (1000, 10, 7, 132, 4),
            (1676, 26, 96, 297, 3),
        })
        {
            Assert.Equal(boundary, Count("region_boundary"));
            Assert.Equal(gap, Count("possible_data_gap"));
            Assert.Equal(end, Count("end_of_track"));
            Assert.Equal(joint, Count("sharp_joint"));
            int Count(string kind) => diagnostics.Count(item =>
                item.GetProperty("kind").GetString() == kind &&
                item.GetProperty("gaugeMillimetres").GetInt32() == gauge);
        }
        Assert.Equal(1, diagnostics.Count(item => item.GetProperty("kind").GetString() == "station_without_track"));
    }

    [Theory]
    [InlineData("Córdoba", "Villa María", false, "found", 1676, 0, 141_600)]
    [InlineData("Villa María", "Río Cuarto", false, "found", 1676, 0, 131_600)]
    [InlineData("Córdoba", "Río Cuarto", false, "found", 1676, 1, 272_400)]
    [InlineData("Alta Córdoba", "Cosquín", false, "found", 1000, 0, 56_900)]
    [InlineData("Córdoba", "Alta Córdoba", false, "found", 1000, 1, 6_400)]
    [InlineData("Córdoba", "Cruz del Eje", false, "unreachable", 0, 0, 0)]
    [InlineData("Córdoba", "Cruz del Eje", true, "found", 1000, 0, 153_000)]
    [InlineData("Córdoba", "Deán Funes", true, "found", 1000, 0, 218_000)]
    [InlineData("Cosquín", "Villa María", false, "unreachable", 0, 0, 0)]
    public async Task CordobaRoutes_MatchMeasuredCases(string originName, string destinationName,
        bool includeDisused, string status, int gauge, int reversals, double length)
    {
        var railway = await client.GetFromJsonAsync<RailwayResponse>("/api/regions/cordoba/railway",
            TestContext.Current.CancellationToken);
        Assert.NotNull(railway);
        var origin = Assert.Single(railway.Stations, station => station.Name == originName);
        var destination = Assert.Single(railway.Stations, station => station.Name == destinationName);
        var watch = Stopwatch.StartNew();
        var response = await client.PostAsJsonAsync("/api/regions/cordoba/network/routes", new
        {
            originStationId = origin.Id, destinationStationId = destination.Id, includeDisused,
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = json.RootElement;
        Assert.Equal(status, root.GetProperty("status").GetString());
        if (status == "found")
        {
            var route = root.GetProperty("route");
            Assert.Equal(gauge, route.GetProperty("gauge").GetProperty("widthMillimetres").GetInt32());
            Assert.Equal(reversals, route.GetProperty("reversals").GetArrayLength());
            Assert.InRange(route.GetProperty("lengthMeters").GetDouble(), length * .99, length * 1.01);
            if (reversals == 1)
            {
                var location = route.GetProperty("reversals")[0].GetProperty("location");
                var expected = destinationName == "Río Cuarto"
                    ? (-32.4092, -63.2478) : (-31.3878, -64.1919);
                var latitude = location.GetProperty("latitude").GetDouble();
                var longitude = location.GetProperty("longitude").GetDouble();
                var metres = RailWeaver.Core.Geography.ElevationProfileBuilder.GreatCircleDistanceMeters(
                    new(latitude, longitude), new(expected.Item1, expected.Item2));
                Assert.True(metres < 1000, $"Reversal was {metres:F0} m from its reference point.");
            }
            Assert.True(root.TryGetProperty("profile", out _) ||
                root.TryGetProperty("profileUnavailableReason", out _));
        }
        else
        {
            Assert.Equal(destinationName == "Villa María" ? "no_common_gauge" : "disconnected",
                root.GetProperty("reason").GetString());
            if (destinationName == "Cruz del Eje")
                Assert.True(root.GetProperty("reachableWithDisused").GetBoolean());
        }
        Assert.True(watch.Elapsed.TotalSeconds <= 5,
            $"Route including terrain profile took {watch.Elapsed.TotalSeconds:F2}s");
    }
}
