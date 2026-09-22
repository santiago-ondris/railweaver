using System.Buffers.Binary;
using RailWeaver.Api.Regions;
using RailWeaver.Api.Railways;
using RailWeaver.Api.Elevation;
using RailWeaver.Core.Geography;
using RailWeaver.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(
    new RegionFileStore(Path.Combine(AppContext.BaseDirectory, "data", "regions")));
builder.Services.AddSingleton(
    new RailwayFileStore(Path.Combine(AppContext.BaseDirectory, "data", "regions")));
builder.Services.AddSingleton(
    new ElevationFileStore(Path.Combine(AppContext.BaseDirectory, "data", "regions")));

var app = builder.Build();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    name = RailWeaverInfo.Name,
    version = RailWeaverInfo.Version,
}));

app.MapGet(
    "/api/regions/{id}",
    async (string id, RegionFileStore regions, CancellationToken cancellationToken) =>
    {
        var region = await regions.FindAsync(id, cancellationToken);
        return region is null ? Results.NotFound() : Results.Ok(region);
    });

const string elevationSetupMessage = "Elevation dataset is unavailable. Run: python3 tools/fetch-elevation.py";

app.MapGet("/api/regions/{id}/elevation", (string id, double lat, double lon, ElevationFileStore elevations) =>
{
    var dataset = elevations.Find(id);
    if (dataset is null) return Results.NotFound();
    if (!dataset.IsAvailable) return Results.Json(new { message = elevationSetupMessage }, statusCode: 503);
    GeoCoordinate coordinate;
    try { coordinate = new GeoCoordinate(lat, lon); }
    catch (ArgumentOutOfRangeException exception) { return Results.BadRequest(new { message = exception.Message }); }
    var elevation = dataset.Grid!.GetElevationMeters(coordinate);
    return Results.Ok(new ElevationResponse(elevation, elevation is null ? "outside_coverage_or_nodata" : null));
});

app.MapPost("/api/regions/{id}/elevation/profile", (string id, ProfileRequest request, ElevationFileStore elevations) =>
{
    var dataset = elevations.Find(id);
    if (dataset is null) return Results.NotFound();
    if (!dataset.IsAvailable) return Results.Json(new { message = elevationSetupMessage }, statusCode: 503);
    if (request.Coordinates is null || request.Coordinates.Count is < 2 or > 100)
        return Results.BadRequest(new { message = "A profile requires between 2 and 100 vertices." });
    GeoCoordinate[] coordinates;
    try { coordinates = request.Coordinates.Select(item => new GeoCoordinate(item.Latitude, item.Longitude)).ToArray(); }
    catch (ArgumentOutOfRangeException exception) { return Results.BadRequest(new { message = exception.Message }); }
    var estimatedSamples = coordinates.Length;
    for (var index = 1; index < coordinates.Length; index++)
        estimatedSamples += (int)Math.Ceiling(
            ElevationProfileBuilder.GreatCircleDistanceMeters(coordinates[index - 1], coordinates[index])
            / ElevationProfileBuilder.SamplingStepMeters);
    if (estimatedSamples > 20_000)
        return Results.BadRequest(new { message = "A profile cannot exceed 20,000 samples." });
    ElevationProfile profile;
    try { profile = new ElevationProfileBuilder(dataset.Grid!).Build(coordinates); }
    catch (ArgumentException exception) { return Results.BadRequest(new { message = exception.Message }); }
    return Results.Ok(new ProfileResponse(profile.TotalDistanceMeters, profile.Samples.Select(sample =>
        new ProfileSampleResponse(sample.DistanceMeters,
            new CoordinateRequest(sample.Coordinate.Latitude, sample.Coordinate.Longitude),
            sample.ElevationMeters, sample.GradientPermille)).ToArray()));
});

app.MapGet("/api/regions/{id}/terrain/{level:int}/{x:int}/{y:int}",
    (string id, int level, int x, int y, ElevationFileStore elevations, HttpContext context) =>
{
    var dataset = elevations.Find(id);
    if (dataset is null) return Results.NotFound();
    if (!dataset.IsAvailable) return Results.Json(new { message = elevationSetupMessage }, statusCode: 503);
    if (level is < 0 or > 20 || x < 0 || y < 0 || x >= 2 << level || y >= 1 << level)
        return Results.BadRequest(new { message = "Invalid terrain tile coordinates." });
    var xTiles = 2 << level;
    var yTiles = 1 << level;
    var west = -180d + 360d * x / xTiles;
    var east = -180d + 360d * (x + 1) / xTiles;
    var north = 90d - 180d * y / yTiles;
    var south = 90d - 180d * (y + 1) / yTiles;
    var bytes = new byte[65 * 65 * sizeof(float)];
    for (var row = 0; row < 65; row++)
        for (var column = 0; column < 65; column++)
        {
            var coordinate = new GeoCoordinate(north + (south - north) * row / 64, west + (east - west) * column / 64);
            var value = (float)(dataset.Grid!.GetElevationMeters(coordinate) ?? 0);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan((row * 65 + column) * 4, 4), BitConverter.SingleToInt32Bits(value));
        }
    context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
    context.Response.Headers.ETag = $"\"{dataset.Sha256}\"";
    return Results.File(bytes, "application/octet-stream");
});

app.MapGet(
    "/api/regions/{id}/railway",
    async (string id, RailwayFileStore railways, CancellationToken cancellationToken) =>
    {
        var railway = await railways.FindAsync(id, cancellationToken);
        return railway is null ? Results.NotFound() : Results.Ok(railway);
    });

app.Run();

public partial class Program;
