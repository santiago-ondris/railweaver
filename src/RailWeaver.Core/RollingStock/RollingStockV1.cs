namespace RailWeaver.Core.RollingStock;

public sealed record RollingStockV1
{
    public string Name { get; }
    public double LengthMeters { get; }
    public double MaxSpeedKmh { get; }
    public double AccelerationMetersPerSecondSquared { get; }
    public double BrakingMetersPerSecondSquared { get; }

    public RollingStockV1(string name, double lengthMeters, double maxSpeedKmh,
        double accelerationMetersPerSecondSquared, double brakingMetersPerSecondSquared)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Train name is required.", nameof(name));
        Check(lengthMeters, 10, 1500, nameof(lengthMeters));
        Check(maxSpeedKmh, 10, 160, nameof(maxSpeedKmh));
        Check(accelerationMetersPerSecondSquared, 0.01, 1.5, nameof(accelerationMetersPerSecondSquared));
        Check(brakingMetersPerSecondSquared, 0.05, 1.5, nameof(brakingMetersPerSecondSquared));
        Name = name;
        LengthMeters = lengthMeters;
        MaxSpeedKmh = maxSpeedKmh;
        AccelerationMetersPerSecondSquared = accelerationMetersPerSecondSquared;
        BrakingMetersPerSecondSquared = brakingMetersPerSecondSquared;
    }

    private static void Check(double value, double min, double max, string name)
    {
        if (!double.IsFinite(value) || value < min || value > max)
            throw new ArgumentException($"{name} must be between {min} and {max}.", name);
    }
}

public sealed record RollingStockPreset(string Id, RollingStockV1 Train);

public static class RollingStockPresets
{
    public static IReadOnlyList<RollingStockPreset> All { get; } = Array.AsReadOnly<RollingStockPreset>([
        new("intercity-passenger", new("Pasajeros troncal", 200, 120, 0.25, 0.50)),
        new("mountain-railcar", new("Coche motor serrano", 42, 80, 0.50, 0.70)),
        new("bulk-freight", new("Carga granelero", 550, 60, 0.06, 0.25)),
    ]);
}
