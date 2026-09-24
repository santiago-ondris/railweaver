using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RailWeaver.Api.Railways;

namespace RailWeaver.Api.Tests;

public sealed class RunningTimeEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    private static readonly object Train = new
    {
        name = "Pasajeros troncal", lengthMeters = 200, maxSpeedKmh = 120,
        accelerationMetersPerSecondSquared = 0.25, brakingMetersPerSecondSquared = 0.5,
    };

    public RunningTimeEndpointTests(WebApplicationFactory<Program> factory) => client = factory.CreateClient();

    [Fact]
    public async Task PresetsAndCorridorCalculation_ValidateContracts()
    {
        var presets = await client.GetAsync("/api/rolling-stock/presets", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, presets.StatusCode);
        using var list = JsonDocument.Parse(await presets.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(3, list.RootElement.GetArrayLength());
        Assert.Equal("intercity-passenger", list.RootElement[0].GetProperty("id").GetString());

        var missing = await client.PostAsJsonAsync("/api/regions/unknown/corridors/running-time",
            new { sections = new object[0], train = Train }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var gap = await client.PostAsJsonAsync("/api/regions/cordoba/corridors/running-time",
            new { sections = new[]
            {
                new { kind = "tangent", fromMeters = 0, toMeters = 1000, speedLimitKmh = 100, radiusMeters = (double?)null },
                new { kind = "tangent", fromMeters = 1001, toMeters = 2000, speedLimitKmh = 100, radiusMeters = (double?)null },
            }, train = Train }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, gap.StatusCode);
        var invalidTrain = await client.PostAsJsonAsync("/api/regions/cordoba/corridors/running-time",
            new { sections = new[] { new { kind = "tangent", fromMeters = 0, toMeters = 1000,
                speedLimitKmh = 100, radiusMeters = (double?)null } },
                train = new { name = "Bad", lengthMeters = 0, maxSpeedKmh = 120,
                    accelerationMetersPerSecondSquared = 0.25, brakingMetersPerSecondSquared = 0.5 } },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidTrain.StatusCode);
        var good = await client.PostAsJsonAsync("/api/regions/cordoba/corridors/running-time",
            new { sections = new[] { new { kind = "tangent", fromMeters = 0, toMeters = 10000,
                speedLimitKmh = 100, radiusMeters = (double?)null } }, train = Train },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        using var result = JsonDocument.Parse(await good.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.True(result.RootElement.GetProperty("metrics").GetProperty("totalSeconds").GetDouble() > 360);
    }

    [Theory]
    [InlineData("Villa María", 0, 5181)]
    [InlineData("Río Cuarto", 1, 9980)]
    [InlineData("Alta Córdoba", 1, 0)]
    public async Task RealRoutes_MatchAnalyticalReference(string destinationName, int reversalCount, double expectedSeconds)
    {
        var railway = await client.GetFromJsonAsync<RailwayResponse>("/api/regions/cordoba/railway",
            TestContext.Current.CancellationToken);
        var origin = Assert.Single(railway!.Stations, item => item.Name == "Córdoba");
        var destination = Assert.Single(railway.Stations, item => item.Name == destinationName);
        var response = await client.PostAsJsonAsync("/api/regions/cordoba/network/routes/running-time",
            new { originStationId = origin.Id, destinationStationId = destination.Id,
                includeDisused = false, lineSpeedKmh = 100, reversalDwellMinutes = 0, train = Train },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var metrics = json.RootElement.GetProperty("metrics");
        if (reversalCount > 0)
            Assert.Equal(1500, json.RootElement.GetProperty("reversals")[0]
                .GetProperty("maneuverTrackMeters").GetDouble());
        if (expectedSeconds > 0)
            Assert.InRange(metrics.GetProperty("totalSeconds").GetDouble(), expectedSeconds * .995, expectedSeconds * 1.005);
        Assert.Equal(reversalCount, json.RootElement.GetProperty("reversals").GetArrayLength());
    }

    [Fact]
    public async Task RouteEndpoint_RejectsInvalidInputAndMissingRegion()
    {
        var body = new { originStationId = "a", destinationStationId = "b", lineSpeedKmh = 200,
            reversalDwellMinutes = 0, train = Train };
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(
            "/api/regions/unknown/network/routes/running-time", body,
            TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(
            "/api/regions/cordoba/network/routes/running-time", body,
            TestContext.Current.CancellationToken)).StatusCode);
        var railway = await client.GetFromJsonAsync<RailwayResponse>("/api/regions/cordoba/railway",
            TestContext.Current.CancellationToken);
        var origin = Assert.Single(railway!.Stations, item => item.Name == "Córdoba");
        var destination = Assert.Single(railway.Stations, item => item.Name == "Cruz del Eje");
        var unreachable = await client.PostAsJsonAsync("/api/regions/cordoba/network/routes/running-time",
            new { originStationId = origin.Id, destinationStationId = destination.Id,
                lineSpeedKmh = 100, reversalDwellMinutes = 0, train = Train },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, unreachable.StatusCode);
        Assert.Contains("No hay ruta", await unreachable.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
    }
}
