using System.Globalization;
using System.Text.Json;
using RailWeaver.Core.Geography;
using RailWeaver.Core.Infrastructure;

namespace RailWeaver.Core.Tests.Infrastructure;

public class RailwayDatasetTests
{
    private static readonly GeoBoundingBox CordobaBounds =
        new(-65.7720, -35.0002, -61.7708, -29.5004);

    [Fact]
    public void CordobaTracks_LoadIntoCoreDomain()
    {
        using var document = Load("tracks.geojson");
        var tracks = document.RootElement.GetProperty("features")
            .EnumerateArray()
            .Select(ParseTrack)
            .ToList();

        Assert.NotEmpty(tracks);
        Assert.Contains(tracks, track => track.GaugeInferred);
        Assert.Contains(tracks, track => track.Status == TrackOperationalStatus.Active);
        Assert.Contains(tracks, track => track.Status == TrackOperationalStatus.Disused);
        Assert.Contains(tracks, track => track.Status == TrackOperationalStatus.Abandoned);
        Assert.All(tracks.SelectMany(track => track.Geometry), coordinate =>
            Assert.True(CordobaBounds.Contains(coordinate)));

        var dualGaugePairs = tracks
            .Where(track => track.Id.Contains("#gauge-", StringComparison.Ordinal))
            .GroupBy(track => track.Id[..track.Id.LastIndexOf("#gauge-", StringComparison.Ordinal)])
            .Where(group => group.Select(track => track.Gauge.WidthMillimetres).Distinct().Count() > 1);
        Assert.NotEmpty(dualGaugePairs);
    }

    [Fact]
    public void CordobaStations_LoadIntoCoreDomain()
    {
        using var document = Load("stations.geojson");
        var stations = document.RootElement.GetProperty("features")
            .EnumerateArray()
            .Select(ParseStation)
            .ToList();

        Assert.NotEmpty(stations);
        Assert.Contains(stations, station => station.GaugeInferred);
        Assert.All(stations, station => Assert.True(CordobaBounds.Contains(station.Location)));
    }

    [Fact]
    public void CordobaRailwaySource_DeclaresLicenseAttributionAndRawResponse()
    {
        using var document = Load("railway.source.json");
        var root = document.RootElement;

        Assert.Equal("ODbL-1.0", root.GetProperty("license").GetString());
        Assert.Equal("© OpenStreetMap contributors", root.GetProperty("attribution").GetString());
        Assert.True(
            DateOnly.TryParseExact(
                root.GetProperty("extractedOn").GetString(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _));

        var rawFileName = root.GetProperty("rawResponse").GetString();
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("query").GetString()));
        Assert.True(File.Exists(DatasetPath(rawFileName!)));
        Assert.NotEmpty(root.GetProperty("dualGaugeFinding").GetProperty("cordobaMitreWayIds").EnumerateArray());
    }

    private static TrackSegment ParseTrack(JsonElement feature)
    {
        var properties = feature.GetProperty("properties");
        var geometry = feature.GetProperty("geometry").GetProperty("coordinates")
            .EnumerateArray()
            .Select(ParseCoordinate)
            .ToArray();
        var gaugeWidth = properties.GetProperty("gaugeMillimetres").GetInt32();

        return new TrackSegment(
            properties.GetProperty("id").GetString()!,
            geometry,
            ParseGauge(gaugeWidth),
            properties.GetProperty("gaugeInferred").GetBoolean(),
            Enum.Parse<TrackOperationalStatus>(properties.GetProperty("status").GetString()!),
            Enum.Parse<TrackUsage>(properties.GetProperty("usage").GetString()!),
            properties.GetProperty("name").GetString(),
            properties.GetProperty("lineReference").GetString());
    }

    private static RailwayStation ParseStation(JsonElement feature)
    {
        var properties = feature.GetProperty("properties");
        var gaugeWidth = properties.GetProperty("gaugeMillimetres").GetInt32();

        return new RailwayStation(
            properties.GetProperty("id").GetString()!,
            properties.GetProperty("name").GetString()!,
            ParseCoordinate(feature.GetProperty("geometry").GetProperty("coordinates")),
            Enum.Parse<StationType>(properties.GetProperty("type").GetString()!),
            ParseGauge(gaugeWidth),
            properties.GetProperty("gaugeInferred").GetBoolean());
    }

    private static GeoCoordinate ParseCoordinate(JsonElement coordinate)
    {
        var values = coordinate.EnumerateArray().Select(value => value.GetDouble()).ToArray();
        Assert.Equal(2, values.Length);
        return new GeoCoordinate(values[1], values[0]);
    }

    private static TrackGauge ParseGauge(int widthMillimetres) =>
        widthMillimetres == 0 ? TrackGauge.Unknown : TrackGauge.FromMillimetres(widthMillimetres);

    private static JsonDocument Load(string fileName) =>
        JsonDocument.Parse(File.ReadAllText(DatasetPath(fileName)));

    private static string DatasetPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "data", "regions", "cordoba", fileName);
}
