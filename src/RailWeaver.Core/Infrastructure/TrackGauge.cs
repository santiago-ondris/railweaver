namespace RailWeaver.Core.Infrastructure;

public enum TrackGaugeKind
{
    Unknown = 0,
    Metre = 1000,
    Standard = 1435,
    Broad = 1676,
}

public readonly record struct TrackGauge
{
    private TrackGauge(int widthMillimetres, TrackGaugeKind kind)
    {
        WidthMillimetres = widthMillimetres;
        Kind = kind;
    }

    public int WidthMillimetres { get; }

    public TrackGaugeKind Kind { get; }

    public static TrackGauge Metre { get; } = new(1000, TrackGaugeKind.Metre);

    public static TrackGauge Standard { get; } = new(1435, TrackGaugeKind.Standard);

    public static TrackGauge Broad { get; } = new(1676, TrackGaugeKind.Broad);

    public static TrackGauge Unknown { get; } = new(0, TrackGaugeKind.Unknown);

    public static TrackGauge FromMillimetres(int widthMillimetres)
    {
        if (widthMillimetres <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(widthMillimetres),
                widthMillimetres,
                "Track gauge must be greater than zero millimetres.");
        }

        return widthMillimetres switch
        {
            1000 => Metre,
            1435 => Standard,
            1676 => Broad,
            _ => new TrackGauge(widthMillimetres, TrackGaugeKind.Unknown),
        };
    }

    public bool IsCompatibleWith(TrackGauge other) =>
        WidthMillimetres > 0 && WidthMillimetres == other.WidthMillimetres;
}
