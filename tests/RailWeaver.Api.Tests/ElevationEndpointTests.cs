using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RailWeaver.Api.Elevation;

namespace RailWeaver.Api.Tests;

public sealed class ElevationEndpointTests : IDisposable
{
    private readonly string RegionId = $"elevation-test-{Guid.NewGuid():N}";
    private readonly string MissingRegionId = $"missing-elevation-test-{Guid.NewGuid():N}";
    private readonly string regionsDirectory = Path.Combine(AppContext.BaseDirectory, "data", "regions");
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public ElevationEndpointTests()
    {
        Directory.CreateDirectory(Path.Combine(regionsDirectory, RegionId, "elevation"));
        File.WriteAllText(Path.Combine(regionsDirectory, $"{RegionId}.json"), "{}");
        File.WriteAllText(Path.Combine(regionsDirectory, $"{MissingRegionId}.json"), "{}");
        File.WriteAllText(Path.Combine(regionsDirectory, RegionId, "elevation.source.json"),
            JsonSerializer.Serialize(new { sha256 = "fixture-hash", attribution = "Fixture" }));
        ElevationGridFile.Write(
            Path.Combine(regionsDirectory, RegionId, "elevation", "elevation.rwe"),
            4, 4, -65, -33, -64, -32,
            Enumerable.Range(0, 16).Select(value => value * 100).ToArray());
        factory = new WebApplicationFactory<Program>();
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetElevation_ReturnsInterpolatedMeters()
    {
        var response = await client.GetAsync(
            $"/api/regions/{RegionId}/elevation?lat=-32.5&lon=-64.5",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ElevationResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(7.5, result!.Elevation);
    }

    [Fact]
    public async Task PostProfile_ValidatesLimitsAndReturnsSamples()
    {
        var invalid = await client.PostAsJsonAsync(
            $"/api/regions/{RegionId}/elevation/profile",
            new { coordinates = new[] { new { latitude = -32.5, longitude = -64.5 } } },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var response = await client.PostAsJsonAsync(
            $"/api/regions/{RegionId}/elevation/profile",
            new
            {
                coordinates = new[] {
                new { latitude = -32.6, longitude = -64.6 },
                new { latitude = -32.4, longitude = -64.4 },
            }
            }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProfileResponse>(TestContext.Current.CancellationToken);
        Assert.True(result!.Samples.Count > 2);
    }

    [Fact]
    public async Task GetTerrain_ReturnsHeightmapAndCacheHeaders()
    {
        var response = await client.GetAsync(
            $"/api/regions/{RegionId}/terrain/0/0/0",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(65 * 65 * sizeof(float), (await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).Length);
        Assert.Equal("\"fixture-hash\"", response.Headers.ETag!.Tag);
        Assert.Contains("immutable", response.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task MissingDatasetReturnsServiceUnavailableAndUnknownRegionReturnsNotFound()
    {
        var missing = await client.GetAsync(
            $"/api/regions/{MissingRegionId}/elevation?lat=-31.4&lon=-64.2",
            TestContext.Current.CancellationToken);
        var unknown = await client.GetAsync(
            "/api/regions/unknown/elevation?lat=-31.4&lon=-64.2",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, missing.StatusCode);
        Assert.Contains("python3 tools/fetch-elevation.py", await missing.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    public void Dispose()
    {
        client.Dispose(); factory.Dispose();
        var datasetDirectory = Path.Combine(regionsDirectory, RegionId);
        if (Directory.Exists(datasetDirectory)) Directory.Delete(datasetDirectory, recursive: true);
        File.Delete(Path.Combine(regionsDirectory, $"{RegionId}.json"));
        File.Delete(Path.Combine(regionsDirectory, $"{MissingRegionId}.json"));
    }
}
