using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Infrastructure;

public enum TrackOperationalStatus
{
    Active = 1,
    Disused = 2,
    Abandoned = 3,
}

public enum TrackUsage
{
    Unknown = 0,
    MainLine = 1,
    BranchLine = 2,
    Siding = 3,
    Yard = 4,
    IndustrialSpur = 5,
}

public sealed record TrackSegment
{
    public TrackSegment(
        string id,
        IReadOnlyList<GeoCoordinate> geometry,
        TrackGauge gauge,
        bool gaugeInferred,
        TrackOperationalStatus status,
        TrackUsage usage,
        string? name,
        string? lineReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(geometry);

        if (geometry.Count < 2)
        {
            throw new ArgumentException(
                "A track segment requires at least two coordinates.",
                nameof(geometry));
        }

        for (var index = 1; index < geometry.Count; index++)
        {
            if (geometry[index - 1] == geometry[index])
            {
                throw new ArgumentException(
                    "A track segment cannot contain identical consecutive coordinates.",
                    nameof(geometry));
            }
        }

        if (gaugeInferred && gauge == TrackGauge.Unknown)
        {
            throw new ArgumentException(
                "An unknown track gauge cannot be marked as inferred.",
                nameof(gaugeInferred));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown track status.");
        }

        if (!Enum.IsDefined(usage))
        {
            throw new ArgumentOutOfRangeException(nameof(usage), usage, "Unknown track usage.");
        }

        Id = id;
        Geometry = geometry.ToArray();
        Gauge = gauge;
        GaugeInferred = gaugeInferred;
        Status = status;
        Usage = usage;
        Name = name;
        LineReference = lineReference;
    }

    public string Id { get; }

    public IReadOnlyList<GeoCoordinate> Geometry { get; }

    public TrackGauge Gauge { get; }

    public bool GaugeInferred { get; }

    public TrackOperationalStatus Status { get; }

    public TrackUsage Usage { get; }

    public string? Name { get; }

    public string? LineReference { get; }
}
