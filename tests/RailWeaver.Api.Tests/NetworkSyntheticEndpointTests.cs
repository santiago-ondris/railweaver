using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RailWeaver.Api.Tests;

public sealed class NetworkSyntheticEndpointTests : IDisposable
{
    private readonly string regionsDirectory = Path.Combine(AppContext.BaseDirectory, "data", "regions");
    private readonly string regionId = $"network-test-{Guid.NewGuid():N}";
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public NetworkSyntheticEndpointTests()
    {
        WriteDataset();
        factory = new WebApplicationFactory<Program>();
        client = factory.CreateClient();
    }

    [Theory]
    [InlineData("far", "west", "origin_without_track")]
    [InlineData("west", "far", "destination_without_track")]
    [InlineData("west", "metre", "no_common_gauge")]
    [InlineData("west", "detached", "disconnected")]
    [InlineData("west", "north", "no_feasible_movement")]
    public async Task Routes_ReturnEachUnreachableReason(string origin, string destination, string reason)
    {
        var response = await Post(origin, destination);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("unreachable", json.RootElement.GetProperty("status").GetString());
        Assert.Equal(reason, json.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task FoundRouteWithoutDem_KeepsRouteAndExplainsMissingProfile()
    {
        var response = await Post("west", "center");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("found", json.RootElement.GetProperty("status").GetString());
        Assert.Equal("elevation_unavailable", json.RootElement.GetProperty("profileUnavailableReason").GetString());
    }

    [Fact]
    public async Task SameStation_ReturnsBadRequestMessage()
    {
        var response = await Post("west", "west");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("message", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private Task<HttpResponseMessage> Post(string origin, string destination) =>
        client.PostAsJsonAsync($"/api/regions/{regionId}/network/routes", new
        {
            originStationId = origin,
            destinationStationId = destination,
        }, TestContext.Current.CancellationToken);

    private void WriteDataset()
    {
        var directory = Path.Combine(regionsDirectory, regionId);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(regionsDirectory, $"{regionId}.json"), JsonSerializer.Serialize(new
        {
            id = regionId, name = "Test", boundingBox = new { west = -64.1, south = -31.1, east = -63.9, north = -30.9 },
            initialCamera = new { latitude = -31d, longitude = -64d, heightMeters = 10_000 },
            source = new { name = "Test", url = "https://example.com", accessedOn = "2026-09-22", license = "Test" },
        }));
        var tracks = new[]
        {
            Track("west-track", 1676, new[] { -64.01, -31d }, new[] { -64d, -31d }),
            Track("north-track", 1676, new[] { -64d, -31d }, new[] { -64d, -30.99 }),
            Track("detached-track", 1676, new[] { -64.03, -31.03 }, new[] { -64.02, -31.03 }),
            Track("metre-track", 1000, new[] { -64.03, -30.98 }, new[] { -64.02, -30.98 }),
        };
        var stations = new[]
        {
            Station("west", -64.01, -31), Station("center", -64, -31),
            Station("north", -64, -30.99), Station("detached", -64.03, -31.03),
            Station("metre", -64.03, -30.98), Station("far", -64.08, -31.08),
        };
        File.WriteAllText(Path.Combine(directory, "tracks.geojson"),
            JsonSerializer.Serialize(new { type = "FeatureCollection", features = tracks }));
        File.WriteAllText(Path.Combine(directory, "stations.geojson"),
            JsonSerializer.Serialize(new { type = "FeatureCollection", features = stations }));
        var source = JsonNode.Parse(File.ReadAllText(Path.Combine(regionsDirectory,
            "cordoba", "railway.source.json")))!;
        source["counts"]!["tracks"] = tracks.Length;
        source["counts"]!["stations"] = stations.Length;
        source["rawResponse"] = "raw.json";
        File.WriteAllText(Path.Combine(directory, "railway.source.json"), source.ToJsonString());
        File.WriteAllText(Path.Combine(directory, "raw.json"), "{}");
    }

    private static object Track(string id, int gauge, double[] start, double[] end) => new
    {
        type = "Feature", id,
        properties = new { id, gaugeMillimetres = gauge, gaugeInferred = false,
            status = "Active", usage = "MainLine", name = (string?)null, lineReference = (string?)null },
        geometry = new { type = "LineString", coordinates = new[] { start, end } },
    };

    private static object Station(string id, double longitude, double latitude) => new
    {
        type = "Feature", id,
        properties = new { id, name = id, type = "Station", gaugeMillimetres = 0, gaugeInferred = false },
        geometry = new { type = "Point", coordinates = new[] { longitude, latitude } },
    };

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        var directory = Path.Combine(regionsDirectory, regionId);
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        File.Delete(Path.Combine(regionsDirectory, $"{regionId}.json"));
    }
}
