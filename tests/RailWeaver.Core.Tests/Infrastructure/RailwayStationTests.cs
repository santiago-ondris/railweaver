using RailWeaver.Core.Geography;
using RailWeaver.Core.Infrastructure;

namespace RailWeaver.Core.Tests.Infrastructure;

public class RailwayStationTests
{
    [Fact]
    public void Constructor_RejectsEmptyName()
    {
        Assert.Throws<ArgumentException>(() => new RailwayStation(
            "node/1",
            " ",
            new GeoCoordinate(-31.4, -64.2),
            StationType.Station,
            TrackGauge.Metre,
            gaugeInferred: false));
    }

    [Fact]
    public void Constructor_RejectsInferredUnknownGauge()
    {
        Assert.Throws<ArgumentException>(() => new RailwayStation(
            "node/1",
            "Córdoba",
            new GeoCoordinate(-31.4, -64.2),
            StationType.Station,
            TrackGauge.Unknown,
            gaugeInferred: true));
    }
}
