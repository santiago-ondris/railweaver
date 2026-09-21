using RailWeaver.Api.Regions;
using RailWeaver.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(
    new RegionFileStore(Path.Combine(AppContext.BaseDirectory, "data", "regions")));

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

app.Run();
