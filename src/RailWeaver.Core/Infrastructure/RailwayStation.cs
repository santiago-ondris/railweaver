using RailWeaver.Core.Geography;

namespace RailWeaver.Core.Infrastructure;

public enum StationType
{
    Station = 1,
    Halt = 2,
    Junction = 3,
}

public sealed record RailwayStation
{
    public RailwayStation(
        string id,
        string name,
        GeoCoordinate location,
        StationType type,
        TrackGauge gauge,
        bool gaugeInferred)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown station type.");
        }

        if (gaugeInferred && gauge == TrackGauge.Unknown)
        {
            throw new ArgumentException(
                "An unknown track gauge cannot be marked as inferred.",
                nameof(gaugeInferred));
        }

        Id = id;
        Name = name;
        Location = location;
        Type = type;
        Gauge = gauge;
        GaugeInferred = gaugeInferred;
    }

    public string Id { get; }

    public string Name { get; }

    public GeoCoordinate Location { get; }

    public StationType Type { get; }

    public TrackGauge Gauge { get; }

    public bool GaugeInferred { get; }
}
