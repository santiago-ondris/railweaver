using System.Text.Json;
using System.Text.Json.Serialization;
using RailWeaver.Core.Geography;

namespace RailWeaver.Api.Regions;

public sealed class RegionFileStore(string regionsDirectory)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public async Task<RegionResponse?> FindAsync(string id, CancellationToken cancellationToken)
    {
        if (!IsValidId(id))
        {
            return null;
        }

        var path = Path.Combine(regionsDirectory, $"{id}.json");
        if (!File.Exists(path))
        {
            return null;
        }

        RegionDocument? document;
        try
        {
            await using var stream = File.OpenRead(path);
            document = await JsonSerializer.DeserializeAsync<RegionDocument>(
                stream,
                SerializerOptions,
                cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Region file '{path}' contains invalid JSON.", exception);
        }

        if (document is null)
        {
            throw new InvalidDataException($"Region file '{path}' is empty.");
        }

        return ValidateAndMap(document, id, path);
    }

    private static RegionResponse ValidateAndMap(
        RegionDocument document,
        string requestedId,
        string path)
    {
        if (!string.Equals(document.Id, requestedId, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Region file '{path}' declares id '{document.Id}' instead of '{requestedId}'.");
        }

        if (document.BoundingBox is null
            || document.InitialCamera is null
            || document.Source is null)
        {
            throw new InvalidDataException($"Region file '{path}' is missing a required section.");
        }

        RequireText(document.Id, nameof(document.Id), path);
        RequireText(document.Name, nameof(document.Name), path);
        RequireText(document.Source.Name, nameof(document.Source.Name), path);
        RequireText(document.Source.License, nameof(document.Source.License), path);

        if (!Uri.TryCreate(document.Source.Url, UriKind.Absolute, out var sourceUri)
            || sourceUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidDataException($"Region file '{path}' has an invalid source URL.");
        }

        GeoBoundingBox boundingBox;
        GeoCoordinate cameraCoordinate;
        try
        {
            boundingBox = new GeoBoundingBox(
                document.BoundingBox.West,
                document.BoundingBox.South,
                document.BoundingBox.East,
                document.BoundingBox.North);

            cameraCoordinate = new GeoCoordinate(
                document.InitialCamera.Latitude,
                document.InitialCamera.Longitude);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                $"Region file '{path}' contains invalid geographic coordinates.",
                exception);
        }

        if (!double.IsFinite(document.InitialCamera.HeightMeters)
            || document.InitialCamera.HeightMeters <= 0)
        {
            throw new InvalidDataException(
                $"Region file '{path}' must define a positive, finite camera height.");
        }

        if (!boundingBox.Contains(cameraCoordinate))
        {
            throw new InvalidDataException(
                $"Region file '{path}' defines an initial camera outside its bounding box.");
        }

        return new RegionResponse(
            document.Id,
            document.Name,
            new BoundingBoxResponse(
                boundingBox.West,
                boundingBox.South,
                boundingBox.East,
                boundingBox.North),
            new CameraViewResponse(
                cameraCoordinate.Latitude,
                cameraCoordinate.Longitude,
                document.InitialCamera.HeightMeters),
            new RegionSourceResponse(
                document.Source.Name,
                sourceUri.AbsoluteUri,
                document.Source.AccessedOn,
                document.Source.License));
    }

    private static bool IsValidId(string id) =>
        !string.IsNullOrEmpty(id)
        && id[0] != '-'
        && id[^1] != '-'
        && id.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    private static void RequireText(string? value, string field, string path)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException(
                $"Region file '{path}' must define a non-empty {field}.");
        }
    }
}
