using System.Text.Json;
using System.Text.Json.Serialization;
using RailWeaver.Core.Geography;
using RailWeaver.Core.Infrastructure;

namespace RailWeaver.Api.Railways;

public sealed class RailwayFileStore(string regionsDirectory)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public async Task<RailwayResponse?> FindAsync(
        string regionId,
        CancellationToken cancellationToken)
    {
        if (!IsValidId(regionId) || !File.Exists(Path.Combine(regionsDirectory, $"{regionId}.json")))
        {
            return null;
        }

        var datasetDirectory = Path.Combine(regionsDirectory, regionId);
        if (!Directory.Exists(datasetDirectory))
        {
            return null;
        }

        var tracks = await ReadAsync<TrackFeatureCollectionDocument>(
            Path.Combine(datasetDirectory, "tracks.geojson"),
            cancellationToken);
        var stations = await ReadAsync<StationFeatureCollectionDocument>(
            Path.Combine(datasetDirectory, "stations.geojson"),
            cancellationToken);
        var source = await ReadAsync<RailwaySourceDocument>(
            Path.Combine(datasetDirectory, "railway.source.json"),
            cancellationToken);

        return ValidateAndMap(tracks, stations, source, datasetDirectory);
    }

    private static async Task<T> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new InvalidDataException($"Railway dataset file '{path}' does not exist.");
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken)
                ?? throw new InvalidDataException($"Railway dataset file '{path}' is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Railway dataset file '{path}' contains invalid JSON.",
                exception);
        }
    }

    private static RailwayResponse ValidateAndMap(
        TrackFeatureCollectionDocument tracksDocument,
        StationFeatureCollectionDocument stationsDocument,
        RailwaySourceDocument source,
        string datasetDirectory)
    {
        RequireType(tracksDocument.Type, "FeatureCollection", "tracks.geojson");
        RequireType(stationsDocument.Type, "FeatureCollection", "stations.geojson");

        var tracks = tracksDocument.Features.Select(MapTrack).ToArray();
        var stations = stationsDocument.Features.Select(MapStation).ToArray();

        if (source.Counts.Tracks != tracks.Length || source.Counts.Stations != stations.Length)
        {
            throw new InvalidDataException(
                "Railway source counts do not match the derived GeoJSON files.");
        }

        RequireText(source.Attribution, nameof(source.Attribution));
        RequireText(source.OsmDataTimestamp, nameof(source.OsmDataTimestamp));
        RequireText(source.Query, nameof(source.Query));
        if (!string.Equals(source.License, "ODbL-1.0", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Railway dataset must declare the ODbL-1.0 license.");
        }

        if (!File.Exists(Path.Combine(datasetDirectory, source.RawResponse)))
        {
            throw new InvalidDataException("Railway source metadata references a missing raw response.");
        }

        if (!Uri.TryCreate(source.LicenseUrl, UriKind.Absolute, out var licenseUri)
            || licenseUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidDataException("Railway dataset has an invalid license URL.");
        }

        return new RailwayResponse(
            new RailwayDatasetSourceResponse(
                source.ExtractedOn,
                source.OsmDataTimestamp,
                source.Attribution,
                source.License,
                licenseUri.AbsoluteUri),
            tracks,
            stations);
    }

    private static TrackSegmentResponse MapTrack(TrackFeatureDocument feature)
    {
        RequireType(feature.Type, "Feature", feature.Id);
        RequireType(feature.Geometry.Type, "LineString", feature.Id);
        if (!string.Equals(feature.Id, feature.Properties.Id, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Track feature '{feature.Id}' has inconsistent ids.");
        }

        var geometry = feature.Geometry.Coordinates
            .Select(coordinate => ParseCoordinate(coordinate, feature.Id))
            .ToArray();
        var properties = feature.Properties;
        var track = new TrackSegment(
            properties.Id,
            geometry,
            ParseGauge(properties.GaugeMillimetres, properties.Id),
            properties.GaugeInferred,
            ParseEnum<TrackOperationalStatus>(properties.Status, properties.Id),
            ParseEnum<TrackUsage>(properties.Usage, properties.Id),
            properties.Name,
            properties.LineReference);

        return new TrackSegmentResponse(
            track.Id,
            track.Geometry.Select(MapCoordinate).ToArray(),
            MapGauge(track.Gauge),
            track.GaugeInferred,
            track.Status.ToString(),
            track.Usage.ToString(),
            track.Name,
            track.LineReference);
    }

    private static RailwayStationResponse MapStation(StationFeatureDocument feature)
    {
        RequireType(feature.Type, "Feature", feature.Id);
        RequireType(feature.Geometry.Type, "Point", feature.Id);
        if (!string.Equals(feature.Id, feature.Properties.Id, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Station feature '{feature.Id}' has inconsistent ids.");
        }

        var properties = feature.Properties;
        var station = new RailwayStation(
            properties.Id,
            properties.Name,
            ParseCoordinate(feature.Geometry.Coordinates, feature.Id),
            ParseEnum<StationType>(properties.Type, properties.Id),
            ParseGauge(properties.GaugeMillimetres, properties.Id),
            properties.GaugeInferred);

        return new RailwayStationResponse(
            station.Id,
            station.Name,
            MapCoordinate(station.Location),
            station.Type.ToString(),
            MapGauge(station.Gauge),
            station.GaugeInferred);
    }

    private static TrackGauge ParseGauge(int widthMillimetres, string id)
    {
        try
        {
            return widthMillimetres == 0
                ? TrackGauge.Unknown
                : TrackGauge.FromMillimetres(widthMillimetres);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidDataException($"Railway feature '{id}' has an invalid gauge.", exception);
        }
    }

    private static GeoCoordinate ParseCoordinate(double[] coordinate, string id)
    {
        if (coordinate.Length != 2)
        {
            throw new InvalidDataException(
                $"Railway feature '{id}' must use [longitude, latitude] coordinates.");
        }

        try
        {
            return new GeoCoordinate(coordinate[1], coordinate[0]);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidDataException(
                $"Railway feature '{id}' has invalid geographic coordinates.",
                exception);
        }
    }

    private static T ParseEnum<T>(string value, string id)
        where T : struct, Enum
    {
        if (!Enum.TryParse<T>(value, ignoreCase: false, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new InvalidDataException(
                $"Railway feature '{id}' has invalid {typeof(T).Name} value '{value}'.");
        }
        return parsed;
    }

    private static TrackGaugeResponse MapGauge(TrackGauge gauge) =>
        new(gauge.WidthMillimetres, gauge.Kind.ToString());

    private static GeoCoordinateResponse MapCoordinate(GeoCoordinate coordinate) =>
        new(coordinate.Latitude, coordinate.Longitude);

    private static void RequireType(string? actual, string expected, string source)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"'{source}' must have GeoJSON type '{expected}'.");
        }
    }

    private static void RequireText(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Railway source must define a non-empty {field}.");
        }
    }

    private static bool IsValidId(string id) =>
        !string.IsNullOrEmpty(id)
        && id[0] != '-'
        && id[^1] != '-'
        && id.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
}
