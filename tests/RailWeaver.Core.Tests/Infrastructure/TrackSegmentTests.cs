using RailWeaver.Core.Geography;
using RailWeaver.Core.Infrastructure;

namespace RailWeaver.Core.Tests.Infrastructure;

public class TrackSegmentTests
{
    private static readonly GeoCoordinate First = new(-31.4, -64.2);
    private static readonly GeoCoordinate Second = new(-31.3, -64.1);

    [Fact]
    public void Constructor_RejectsGeometryWithFewerThanTwoCoordinates()
    {
        Assert.Throws<ArgumentException>(() => Create([First]));
    }

    [Fact]
    public void Constructor_RejectsConsecutiveDuplicateCoordinates()
    {
        Assert.Throws<ArgumentException>(() => Create([First, First, Second]));
    }

    [Fact]
    public void Constructor_AcceptsNonConsecutiveDuplicateCoordinates()
    {
        var segment = Create([First, Second, First]);

        Assert.Equal(3, segment.Geometry.Count);
    }

    [Fact]
    public void Constructor_RejectsInferredUnknownGauge()
    {
        Assert.Throws<ArgumentException>(() => new TrackSegment(
            "way/1",
            [First, Second],
            TrackGauge.Unknown,
            gaugeInferred: true,
            TrackOperationalStatus.Active,
            TrackUsage.MainLine,
            name: null,
            lineReference: null));
    }

    private static TrackSegment Create(IReadOnlyList<GeoCoordinate> geometry) =>
        new(
            "way/1",
            geometry,
            TrackGauge.Metre,
            gaugeInferred: false,
            TrackOperationalStatus.Active,
            TrackUsage.MainLine,
            "Test track",
            "A1");
}
