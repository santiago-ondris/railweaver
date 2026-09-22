using RailWeaver.Core.Infrastructure;

namespace RailWeaver.Core.Tests.Infrastructure;

public class TrackGaugeTests
{
    [Theory]
    [InlineData(1000, TrackGaugeKind.Metre)]
    [InlineData(1435, TrackGaugeKind.Standard)]
    [InlineData(1676, TrackGaugeKind.Broad)]
    [InlineData(750, TrackGaugeKind.Unknown)]
    public void FromMillimetres_ClassifiesGauge(int widthMillimetres, TrackGaugeKind expectedKind)
    {
        var gauge = TrackGauge.FromMillimetres(widthMillimetres);

        Assert.Equal(widthMillimetres, gauge.WidthMillimetres);
        Assert.Equal(expectedKind, gauge.Kind);
    }

    [Fact]
    public void FromMillimetres_RejectsNonPositiveWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TrackGauge.FromMillimetres(0));
    }

    [Fact]
    public void IsCompatibleWith_RequiresSameKnownNominalWidth()
    {
        Assert.True(TrackGauge.Metre.IsCompatibleWith(TrackGauge.Metre));
        Assert.False(TrackGauge.Metre.IsCompatibleWith(TrackGauge.Broad));
        Assert.False(TrackGauge.Unknown.IsCompatibleWith(TrackGauge.Unknown));
    }
}
