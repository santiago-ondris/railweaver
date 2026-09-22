using System.Buffers.Binary;
using System.IO.Compression;
using RailWeaver.Core.Geography;

namespace RailWeaver.Api.Elevation;

public sealed class ElevationGridFile : IElevationSource, IDisposable
{
    public const int BlockSize = 256;
    public const int NoData = int.MinValue;
    private static readonly byte[] Magic = "RWELEV01"u8.ToArray();
    private readonly FileStream stream;
    private readonly BlockEntry[] blocks;
    private readonly Dictionary<int, int[]> cache = [];
    private readonly LinkedList<int> cacheOrder = [];
    private readonly object sync = new();

    public ElevationGridFile(string path)
    {
        stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        if (!reader.ReadBytes(Magic.Length).SequenceEqual(Magic))
        {
            throw new InvalidDataException("Elevation grid has an invalid signature.");
        }

        Width = reader.ReadInt32();
        Height = reader.ReadInt32();
        var blockSize = reader.ReadInt32();
        West = reader.ReadDouble();
        South = reader.ReadDouble();
        East = reader.ReadDouble();
        North = reader.ReadDouble();
        var noData = reader.ReadInt32();
        BlockColumns = reader.ReadInt32();
        BlockRows = reader.ReadInt32();
        if (Width < 2 || Height < 2 || blockSize != BlockSize || noData != NoData
            || West >= East || South >= North
            || BlockColumns != (Width + BlockSize - 1) / BlockSize
            || BlockRows != (Height + BlockSize - 1) / BlockSize)
        {
            throw new InvalidDataException("Elevation grid header is invalid.");
        }

        blocks = new BlockEntry[BlockColumns * BlockRows];
        for (var index = 0; index < blocks.Length; index++)
        {
            blocks[index] = new BlockEntry(reader.ReadInt64(), reader.ReadInt32(), reader.ReadInt32());
        }
    }

    public int Width { get; }
    public int Height { get; }
    public int BlockColumns { get; }
    public int BlockRows { get; }
    public double West { get; }
    public double South { get; }
    public double East { get; }
    public double North { get; }

    public double? GetElevationMeters(GeoCoordinate coordinate)
    {
        var x = (coordinate.Longitude - West) / (East - West) * Width - 0.5;
        var y = (North - coordinate.Latitude) / (North - South) * Height - 0.5;
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        if (x0 < 0 || y0 < 0 || x0 + 1 >= Width || y0 + 1 >= Height)
        {
            return null;
        }

        var q00 = GetCentimeters(x0, y0);
        var q10 = GetCentimeters(x0 + 1, y0);
        var q01 = GetCentimeters(x0, y0 + 1);
        var q11 = GetCentimeters(x0 + 1, y0 + 1);
        if (q00 == NoData || q10 == NoData || q01 == NoData || q11 == NoData)
        {
            return null;
        }

        var dx = x - x0;
        var dy = y - y0;
        var top = q00 + (q10 - q00) * dx;
        var bottom = q01 + (q11 - q01) * dx;
        return Math.Round((top + (bottom - top) * dy) / 100, 2, MidpointRounding.AwayFromZero);
    }

    public void Dispose() => stream.Dispose();

    public static void Write(
        string path, int width, int height, double west, double south, double east, double north,
        IReadOnlyList<int> centimeters)
    {
        if (centimeters.Count != width * height) throw new ArgumentException("Grid size does not match values.");
        var columns = (width + BlockSize - 1) / BlockSize;
        var rows = (height + BlockSize - 1) / BlockSize;
        var compressed = new List<byte[]>(columns * rows);
        for (var blockY = 0; blockY < rows; blockY++)
            for (var blockX = 0; blockX < columns; blockX++)
            {
                using var memory = new MemoryStream();
                using (var zlib = new ZLibStream(memory, CompressionLevel.SmallestSize, leaveOpen: true))
                using (var writer = new BinaryWriter(zlib))
                {
                    for (var row = 0; row < BlockSize; row++)
                    {
                        var previous = 0;
                        for (var column = 0; column < BlockSize; column++)
                        {
                            var x = blockX * BlockSize + column;
                            var y = blockY * BlockSize + row;
                            var value = x < width && y < height ? centimeters[y * width + x] : NoData;
                            writer.Write(value == NoData ? NoData : unchecked(value - previous));
                            previous = value == NoData ? 0 : value;
                        }
                    }
                }
                compressed.Add(memory.ToArray());
            }

        using var output = File.Create(path);
        using var binary = new BinaryWriter(output);
        binary.Write(Magic);
        binary.Write(width); binary.Write(height); binary.Write(BlockSize);
        binary.Write(west); binary.Write(south); binary.Write(east); binary.Write(north);
        binary.Write(NoData); binary.Write(columns); binary.Write(rows);
        var offset = output.Position + compressed.Count * 16L;
        foreach (var block in compressed)
        {
            binary.Write(offset); binary.Write(block.Length); binary.Write(BlockSize * BlockSize * sizeof(int));
            offset += block.Length;
        }
        foreach (var block in compressed) binary.Write(block);
    }

    private int GetCentimeters(int x, int y)
    {
        var blockIndex = y / BlockSize * BlockColumns + x / BlockSize;
        var values = GetBlock(blockIndex);
        return values[(y % BlockSize) * BlockSize + x % BlockSize];
    }

    private int[] GetBlock(int blockIndex)
    {
        lock (sync)
        {
            if (cache.TryGetValue(blockIndex, out var cached))
            {
                cacheOrder.Remove(blockIndex); cacheOrder.AddLast(blockIndex); return cached;
            }
            var entry = blocks[blockIndex];
            stream.Position = entry.Offset;
            var bytes = new byte[entry.CompressedLength];
            stream.ReadExactly(bytes);
            var values = new int[BlockSize * BlockSize];
            using var zlib = new ZLibStream(new MemoryStream(bytes), CompressionMode.Decompress);
            Span<byte> buffer = stackalloc byte[4];
            for (var row = 0; row < BlockSize; row++)
            {
                var previous = 0;
                for (var column = 0; column < BlockSize; column++)
                {
                    zlib.ReadExactly(buffer);
                    var delta = BinaryPrimitives.ReadInt32LittleEndian(buffer);
                    var value = delta == NoData ? NoData : unchecked(previous + delta);
                    values[row * BlockSize + column] = value;
                    previous = value == NoData ? 0 : value;
                }
            }
            if (cache.Count == 64)
            {
                var oldest = cacheOrder.First!.Value; cacheOrder.RemoveFirst(); cache.Remove(oldest);
            }
            cache[blockIndex] = values; cacheOrder.AddLast(blockIndex); return values;
        }
    }

    private readonly record struct BlockEntry(long Offset, int CompressedLength, int UncompressedLength);
}
