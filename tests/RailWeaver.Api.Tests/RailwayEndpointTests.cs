using System.Net;
using System.Net.Http.Json;
using RailWeaver.Api.Railways;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RailWeaver.Api.Tests;

public class RailwayEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public RailwayEndpointTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCordobaRailway_ReturnsValidatedDataset()
    {
        var response = await client.GetAsync("/api/regions/cordoba/railway", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var railway = await response.Content.ReadFromJsonAsync<RailwayResponse>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(railway);
        Assert.Equal("ODbL-1.0", railway.Source.License);
        Assert.Equal("© OpenStreetMap contributors", railway.Source.Attribution);
        Assert.NotEmpty(railway.Tracks);
        Assert.NotEmpty(railway.Stations);
        Assert.Contains(railway.Tracks, track => track.GaugeInferred);
        Assert.Contains(railway.Stations, station => station.GaugeInferred);
    }

    [Fact]
    public async Task GetUnknownRegionRailway_ReturnsNotFound()
    {
        var response = await client.GetAsync("/api/regions/not-a-region/railway", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
