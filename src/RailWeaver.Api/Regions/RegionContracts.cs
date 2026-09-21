namespace RailWeaver.Api.Regions;

internal sealed record RegionDocument
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required BoundingBoxDocument BoundingBox { get; init; }

    public required CameraViewDocument InitialCamera { get; init; }

    public required RegionSourceDocument Source { get; init; }
}

internal sealed record BoundingBoxDocument
{
    public required double West { get; init; }

    public required double South { get; init; }

    public required double East { get; init; }

    public required double North { get; init; }
}

internal sealed record CameraViewDocument
{
    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public required double HeightMeters { get; init; }
}

internal sealed record RegionSourceDocument
{
    public required string Name { get; init; }

    public required string Url { get; init; }

    public required DateOnly AccessedOn { get; init; }

    public required string License { get; init; }
}

public sealed record RegionResponse(
    string Id,
    string Name,
    BoundingBoxResponse BoundingBox,
    CameraViewResponse InitialCamera,
    RegionSourceResponse Source);

public sealed record BoundingBoxResponse(double West, double South, double East, double North);

public sealed record CameraViewResponse(double Latitude, double Longitude, double HeightMeters);

public sealed record RegionSourceResponse(string Name, string Url, DateOnly AccessedOn, string License);
