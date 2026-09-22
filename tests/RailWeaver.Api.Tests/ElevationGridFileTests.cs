using RailWeaver.Api.Elevation;
using RailWeaver.Core.Geography;

namespace RailWeaver.Api.Tests;

public sealed class ElevationGridFileTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"railweaver-{Guid.NewGuid():N}.rwe");

    [Fact]
    public void WriteAndRead_RoundTripsValuesAcrossBlockBoundaryAndNoData()
    {
        const int width = 258;
        const int height = 3;
        var values = Enumerable.Range(0, width * height).Select(value => value * 3).ToArray();
        values[width + 100] = ElevationGridFile.NoData;
        ElevationGridFile.Write(path, width, height, -129, 0, 129, 3, values);

        using var grid = new ElevationGridFile(path);

        Assert.Equal(15.42, grid.GetElevationMeters(new GeoCoordinate(1.5, 127.5)));
        Assert.Null(grid.GetElevationMeters(new GeoCoordinate(1.5, -28.5)));
        Assert.Null(grid.GetElevationMeters(new GeoCoordinate(4, 1)));
    }

    [Fact]
    public void GetElevation_InterpolatesFourCellsBilinearly()
    {
        ElevationGridFile.Write(path, 2, 2, 0, 0, 2, 2, [0, 100, 200, 300]);
        using var grid = new ElevationGridFile(path);

        Assert.Equal(1.5, grid.GetElevationMeters(new GeoCoordinate(1, 1)));
    }

    public void Dispose()
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
