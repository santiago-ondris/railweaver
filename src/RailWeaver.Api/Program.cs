using RailWeaver.Core;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    name = RailWeaverInfo.Name,
    version = RailWeaverInfo.Version,
}));

app.Run();
