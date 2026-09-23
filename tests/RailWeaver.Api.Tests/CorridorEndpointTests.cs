using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RailWeaver.Api.Elevation;
using RailWeaver.Api.Planning;

namespace RailWeaver.Api.Tests;

public sealed class CorridorEndpointTests : IDisposable
{
    private readonly string regionsDirectory = Path.Combine(AppContext.BaseDirectory, "data", "regions");
    private readonly string idPrefix = $"corridor-test-{Guid.NewGuid():N}";
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public CorridorEndpointTests()
    {
        WriteRegion("flat", (_, _) => 10_000);
        WriteRegion("slope", (_, row) => 10_000 + row * 37);
        WriteRegion("wall", (_, row) => row is >= 45 and <= 55 ? ElevationGridFile.NoData : 10_000);
        WriteRegion("empty", (column, row) => column < 4 && row >= 96 ? ElevationGridFile.NoData : 10_000);
        WriteRegion("missing", null);
        factory = new WebApplicationFactory<Program>();
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Found_ReturnsProfileAndRoundedMetrics()
    {
        var response = await Post("flat", -31.08, -64.08, -31.02, -64.02, 15);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CorridorResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("found", result!.Status);
        Assert.NotNull(result.Corridor);
        Assert.True(result.Corridor.Alignment.Count > 2);
        Assert.Equal(result.Corridor.TrackProfile.Count, result.Corridor.Alignment.Count);
        Assert.True(result.Corridor.TerrainProfile.Samples.Count > result.Corridor.Alignment.Count);
        Assert.Equal(Math.Round(result.Corridor.Metrics.LengthMeters, 2), result.Corridor.Metrics.LengthMeters);
        Assert.Equal(15, result.Corridor.Metrics.MaxGradientLimitPermille);
    }

    [Fact]
    public async Task Response_RoundsOnlySerializedMeasurements()
    {
        var response = await Post("slope", -31.08, -64.08, -31.02, -64.02, 15);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CorridorResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("found", result!.Status);
        var corridor = result.Corridor!;
        Assert.True(corridor.Metrics.MaxGradientPermille > 0);
        Assert.Equal(Math.Round(corridor.Metrics.Sinuosity, 3), corridor.Metrics.Sinuosity);
        Assert.Equal(Math.Round(corridor.Metrics.MaxGradientPermille, 1), corridor.Metrics.MaxGradientPermille);
        Assert.All(corridor.TrackProfile, point =>
        {
            Assert.Equal(Math.Round(point.DistanceMeters, 2), point.DistanceMeters);
            Assert.Equal(Math.Round(point.ElevationMeters, 2), point.ElevationMeters);
            if (point.GradientPermille is { } gradient)
                Assert.Equal(Math.Round(gradient, 1), gradient);
        });
    }

    [Fact]
    public async Task NoPathAndMissingEndpoint_AreDomainResults()
    {
        var wall = await Post("wall", -31.08, -64.08, -31.02, -64.02, 15);
        var wallResult = await wall.Content.ReadFromJsonAsync<CorridorResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, wall.StatusCode);
        Assert.Equal("no_feasible_path", wallResult!.Status);
        Assert.Null(wallResult.Corridor);

        var missing = await Post("empty", -31.099, -64.099, -31.02, -64.02, 15);
        var missingResult = await missing.Content.ReadFromJsonAsync<CorridorResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, missing.StatusCode);
        Assert.Equal("endpoint_without_elevation", missingResult!.Status);
        Assert.Null(missingResult.Corridor);
    }

    [Fact]
    public async Task InvalidUnknownAndMissingDataset_ReturnExpectedErrors()
    {
        var invalid = await Post("flat", -31.08, -64.08, -31.02, -64.02, 0);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("message", await invalid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var outside = await Post("flat", -32, -64.08, -31.02, -64.02, 15);
        Assert.Equal(HttpStatusCode.BadRequest, outside.StatusCode);

        var missingField = await client.PostAsJsonAsync($"/api/regions/{Id("flat")}/corridors",
            new { destination = new { latitude = -31.02, longitude = -64.02 }, maxGradientPermille = 15 },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, missingField.StatusCode);
        Assert.Contains("message", await missingField.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var unknown = await Post("unknown", -31.08, -64.08, -31.02, -64.02, 15);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var unavailable = await Post("missing", -31.08, -64.08, -31.02, -64.02, 15);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
    }

    private Task<HttpResponseMessage> Post(string suffix, double originLatitude, double originLongitude,
        double destinationLatitude, double destinationLongitude, double limit) =>
        client.PostAsJsonAsync($"/api/regions/{(suffix == "unknown" ? suffix : Id(suffix))}/corridors", new
        {
            origin = new { latitude = originLatitude, longitude = originLongitude },
            destination = new { latitude = destinationLatitude, longitude = destinationLongitude },
            maxGradientPermille = limit,
        }, TestContext.Current.CancellationToken);

    private string Id(string suffix) => $"{idPrefix}-{suffix}";

    private void WriteRegion(string suffix, Func<int, int, int>? heights)
    {
        var id = Id(suffix);
        File.WriteAllText(Path.Combine(regionsDirectory, $"{id}.json"), JsonSerializer.Serialize(new
        {
            id, name = "Test", boundingBox = new { west = -64.1, south = -31.1, east = -64d, north = -31d },
            initialCamera = new { latitude = -31.05, longitude = -64.05, heightMeters = 10_000 },
            source = new { name = "Test", url = "https://example.com", accessedOn = "2026-09-22", license = "Test" },
        }));
        if (heights is null) return;
        var directory = Path.Combine(regionsDirectory, id);
        Directory.CreateDirectory(Path.Combine(directory, "elevation"));
        File.WriteAllText(Path.Combine(directory, "elevation.source.json"),
            JsonSerializer.Serialize(new { sha256 = "fixture", attribution = "Test" }));
        ElevationGridFile.Write(Path.Combine(directory, "elevation", "elevation.rwe"),
            100, 100, -64.1, -31.1, -64, -31,
            Enumerable.Range(0, 10_000).Select(index => heights(index % 100, index / 100)).ToArray());
    }

    public void Dispose()
    {
        client.Dispose(); factory.Dispose();
        foreach (var suffix in new[] { "flat", "slope", "wall", "empty", "missing" })
        {
            var id = Id(suffix);
            var directory = Path.Combine(regionsDirectory, id);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            File.Delete(Path.Combine(regionsDirectory, $"{id}.json"));
        }
    }
}
