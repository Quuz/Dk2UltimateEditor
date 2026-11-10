namespace DK2MapEditor.Core.Editing;

/// <summary>Four native bytes: terrain ID, owner, bridge/terrain variant, reserved byte.</summary>
public readonly record struct MapTile(byte Terrain, byte Owner, byte Variant, byte Reserved = 1)
{
    public uint Packed => (uint)(Terrain | Owner << 8 | Variant << 16 | Reserved << 24);
    public static MapTile FromPacked(uint value) => new((byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24));
}

public readonly record struct TilePoint(int X, int Y);
public readonly record struct TileRect(int X, int Y, int Width, int Height)
{
    public bool Contains(int x, int y) => x >= X && y >= Y && x < (long)X + Width && y < (long)Y + Height;
    public static TileRect Between(TilePoint left, TilePoint right) => new(Math.Min(left.X, right.X), Math.Min(left.Y, right.Y), Math.Abs(left.X - right.X) + 1, Math.Abs(left.Y - right.Y) + 1);
}

public sealed record TileClipboard(int Width, int Height, IReadOnlyList<MapTile> Tiles)
{
    public TileClipboard RotateClockwise()
    {
        var result = new MapTile[Tiles.Count];
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++) result[x * Height + Height - 1 - y] = Tiles[y * Width + x];
        return new(Height, Width, result);
    }
    public TileClipboard RotateCounterclockwise()
    {
        var result = new MapTile[Tiles.Count];
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++) result[(Width - 1 - x) * Height + y] = Tiles[y * Width + x];
        return new(Height, Width, result);
    }
    public TileClipboard Mirror(bool horizontal)
    {
        var result = new MapTile[Tiles.Count];
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++) result[y * Width + x] = Tiles[(horizontal ? y : Height - y - 1) * Width + (horizontal ? Width - x - 1 : x)];
        return new(Width, Height, result);
    }
}
