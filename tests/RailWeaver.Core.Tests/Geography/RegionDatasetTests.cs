using System.Globalization;
using System.Text.Json;
using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Tests.Geography;

public class RegionDatasetTests
{
    [Fact]
    public void CordobaDataset_DefinesAValidRegion()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "regions", "cordoba.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        Assert.Equal("cordoba", root.GetProperty("id").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("name").GetString()));

        var boundingBoxElement = root.GetProperty("boundingBox");
        var boundingBox = new GeoBoundingBox(
            boundingBoxElement.GetProperty("west").GetDouble(),
            boundingBoxElement.GetProperty("south").GetDouble(),
            boundingBoxElement.GetProperty("east").GetDouble(),
            boundingBoxElement.GetProperty("north").GetDouble());

        var cameraElement = root.GetProperty("initialCamera");
        var cameraCoordinate = new GeoCoordinate(
            cameraElement.GetProperty("latitude").GetDouble(),
            cameraElement.GetProperty("longitude").GetDouble());

        Assert.True(cameraElement.GetProperty("heightMeters").GetDouble() > 0);
        Assert.True(boundingBox.Contains(cameraCoordinate));

        var source = root.GetProperty("source");
        Assert.False(string.IsNullOrWhiteSpace(source.GetProperty("name").GetString()));
        Assert.True(
            Uri.TryCreate(source.GetProperty("url").GetString(), UriKind.Absolute, out var sourceUri)
            && sourceUri.Scheme is "http" or "https");
        Assert.True(
            DateOnly.TryParseExact(
                source.GetProperty("accessedOn").GetString(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _));
        Assert.False(string.IsNullOrWhiteSpace(source.GetProperty("license").GetString()));
    }
}
