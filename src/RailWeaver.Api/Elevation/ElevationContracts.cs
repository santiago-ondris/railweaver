namespace RailWeaver.Api.Elevation;

public sealed record ElevationResponse(double? Elevation, string? Reason);
public sealed record ProfileRequest(IReadOnlyList<CoordinateRequest> Coordinates);
public sealed record CoordinateRequest(double Latitude, double Longitude);
public sealed record ProfileResponse(double TotalDistanceMeters, IReadOnlyList<ProfileSampleResponse> Samples);
public sealed record ProfileSampleResponse(
    double DistanceMeters, CoordinateRequest Coordinate, double? ElevationMeters, double? GradientPermille);
