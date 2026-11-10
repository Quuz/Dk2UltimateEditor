using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

/// <summary>A document-independent snapshot: tiles, placed things and attached script graphs.</summary>
public sealed class SelectionClipboard
{
    internal sealed record Payload(uint FileSignature, string Category, byte[] Data, IReadOnlyList<NativeField> Fields, int SourceOffset, bool? LocalPoint = null);
    internal SelectionClipboard(TileClipboard tiles, TileRect source, bool legacy, IReadOnlyList<Payload> things, IReadOnlyList<Payload> scripts, IReadOnlyList<bool>? protectedTiles = null)
    {
        Tiles = tiles; Source = source; Legacy = legacy;
        Payload Classify(Payload item) => item.LocalPoint is not null || NativeMapDocument.ClipboardPointFields(item) is not { } fields ? item
            : item with { LocalPoint = source.Contains(int.Parse(fields.X.Read(item.Data)) - fields.Bias, int.Parse(fields.Y.Read(item.Data)) - fields.Bias) };
        Things = things.Select(Classify).ToArray(); Scripts = scripts.Select(Classify).ToArray();
        if (protectedTiles is not null && protectedTiles.Count != tiles.Tiles.Count) throw new ArgumentException("Invalid selection protection mask.", nameof(protectedTiles));
        ProtectedTiles = Array.AsReadOnly(protectedTiles?.ToArray() ?? new bool[tiles.Tiles.Count]);
    }
    public TileClipboard Tiles { get; }
    public int ObjectCount => Things.Count;
    public int ScriptCount => Scripts.Count;
    public bool IsLegacyProject => Legacy;
    public int ProtectedTileCount => ProtectedTiles.Count(value => value);
    internal IReadOnlyList<bool> ProtectedTiles { get; }
    public bool IsProtectedTile(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Tiles.Width || y >= Tiles.Height) throw new ArgumentOutOfRangeException(nameof(x));
        return ProtectedTiles[y * Tiles.Width + x];
    }
    internal SelectionClipboard WithProtectedTiles(IReadOnlyList<bool> mask) => new(Tiles, Source, Legacy, Things, Scripts, mask);
    internal TileRect Source { get; }
    internal bool Legacy { get; }
    internal IReadOnlyList<Payload> Things { get; }
    internal IReadOnlyList<Payload> Scripts { get; }
    internal static IEnumerable<NativeField> LinkFields(Payload item)
    {
        var nativeAction = item.FileSignature == 0xD2 && BinaryPrimitives.ReadUInt32LittleEndian(item.Data) == 0xD6;
        return item.Fields.Where(field => !(nativeAction && field.Name == "ChildAnd") &&
            (field.Name.EndsWith("Trigger", StringComparison.Ordinal) || field.Name is "NextOr" or "ChildAnd" or "or" or "and" or "next" or "trigger"));
    }
    internal static (NativeField Field,uint Type)? OwnedLinkTarget(Payload item)
    {
        if (!item.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)) return null;
        var scope = uint.Parse(item.Fields.Single(field => field.Name == "node.type").Read(item.Data));
        var (target, type) = scope switch { 2 => ("creature", 0u), 64 => ("door", 1u), 128 => ("object", 3u), 256 => ("object", 4u), _ => ("", 0u) };
        if (target.Length == 0 || item.Fields.FirstOrDefault(field => field.Name == target) is not { } ownerField) return null;
        return (ownerField,type);
    }
    internal static bool IsOwnedLink(Payload item, IReadOnlyList<Payload> things)
    {
        if (OwnedLinkTarget(item) is not { } target) return false;
        var ownerField=target.Field;
        var identity = ownerField.Read(item.Data);
        if (identity == "0") return false;
        var owners = things.Count(thing => thing.Fields.FirstOrDefault(field => field.Name == "uniqueid") is { } unique
            && unique.Read(thing.Data) == identity && uint.Parse(thing.Fields.Single(field => field.Name == "node.type").Read(thing.Data)) == target.Type);
        if (owners > 1) throw new InvalidDataException("A selection scenario owner has an ambiguous identity.");
        return owners == 1;
    }
    /// <summary>Retain a local rectangular slice, the things inside it and only
    /// their owned scope links/reachable events. Absolute external targets stay
    /// external; original bytes/identities are kept for later paste remapping.</summary>
    public SelectionClipboard Crop(TileRect rectangle)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0 || rectangle.X < 0 || rectangle.Y < 0
            || (long)rectangle.X + rectangle.Width > Tiles.Width || (long)rectangle.Y + rectangle.Height > Tiles.Height)
            throw new ArgumentOutOfRangeException(nameof(rectangle));
        if (rectangle == new TileRect(0, 0, Tiles.Width, Tiles.Height)) return this;
        var source = new TileRect(Source.X + rectangle.X, Source.Y + rectangle.Y, rectangle.Width, rectangle.Height);
        var tiles = MapDrawing.Rectangle(rectangle, true).Select(point => Tiles.Tiles[point.Y * Tiles.Width + point.X]).ToArray();
        var mask = MapDrawing.Rectangle(rectangle, true).Select(point => ProtectedTiles[point.Y * Tiles.Width + point.X]).ToArray();
        var things = Things.Where(item =>
        {
            var x = item.Fields.Single(field => field.Name == "x" || field.Name.EndsWith("LocationX", StringComparison.Ordinal));
            var y = item.Fields.Single(field => field.Name == "y" || field.Name.EndsWith("LocationY", StringComparison.Ordinal));
            return source.Contains(int.Parse(x.Read(item.Data)), int.Parse(y.Read(item.Data)));
        }).ToArray();
        return Retain(new(rectangle.Width, rectangle.Height, tiles), source, things, mask);
    }

    internal SelectionClipboard FilterThings(Func<TilePoint, bool> keep)
    {
        var things = Things.Where(item =>
        {
            var fields = NativeMapDocument.ClipboardPointFields(item) ?? throw new InvalidDataException("A selection thing has no coordinates.");
            return keep(new(int.Parse(fields.X.Read(item.Data)), int.Parse(fields.Y.Read(item.Data))));
        }).ToArray();
        return Retain(Tiles, Source, things, ProtectedTiles);
    }

    private SelectionClipboard Retain(TileClipboard tiles, TileRect source, IReadOnlyList<Payload> things, IReadOnlyList<bool> mask)
    {
        var links = Scripts.Where(item => IsOwnedLink(item, things)).ToArray();
        var events = Scripts.Where(item => !item.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal))
            .ToDictionary(item => ushort.Parse(item.Fields.Single(field => field.Name is "ID" or "id").Read(item.Data)));
        var reachable = new HashSet<ushort>();
        var pending = new Stack<ushort>();
        foreach (var item in things.Concat(links)) foreach (var field in LinkFields(item)) pending.Push(ushort.Parse(field.Read(item.Data)));
        while (pending.TryPop(out var id))
        {
            if (id == 0 || !reachable.Add(id)) continue;
            if (!events.TryGetValue(id, out var item)) throw new InvalidDataException("A cropped selection contains a dangling scenario reference.");
            foreach (var field in LinkFields(item)) pending.Push(ushort.Parse(field.Read(item.Data)));
        }
        var scripts = Scripts.Where(item => item.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal) ? links.Contains(item)
            : reachable.Contains(ushort.Parse(item.Fields.Single(field => field.Name is "ID" or "id").Read(item.Data)))).ToArray();
        // A formerly local script target outside the smaller slice becomes external.
        // An already external target must never become local just because it happens
        // to fall inside bounds after a previous rotation.
        Payload CropPoint(Payload item) => item.LocalPoint == true && NativeMapDocument.ClipboardPointFields(item) is { } fields
            ? item with { LocalPoint = source.Contains(int.Parse(fields.X.Read(item.Data)) - fields.Bias, int.Parse(fields.Y.Read(item.Data)) - fields.Bias) } : item;
        return new(tiles, source, Legacy, things.Select(CropPoint).ToArray(), scripts.Select(CropPoint).ToArray(), mask);
    }
    public SelectionClipboard RotateClockwise() => Transform(true, false);
    public SelectionClipboard RotateCounterclockwise() => Transform(true, false, clockwise: false);
    public SelectionClipboard Mirror(bool horizontal) => Transform(false, horizontal);
    private SelectionClipboard Transform(bool rotate, bool horizontal, bool clockwise = true)
    {
        var tiles = rotate ? clockwise ? Tiles.RotateClockwise() : Tiles.RotateCounterclockwise() : Tiles.Mirror(horizontal);
        TilePoint Map(TilePoint point) => rotate ? clockwise ? new(Tiles.Height - 1 - point.Y, point.X) : new(point.Y, Tiles.Width - 1 - point.X)
            : horizontal ? new(Tiles.Width - 1 - point.X, point.Y) : new(point.X, Tiles.Height - 1 - point.Y);
        var mask = new bool[ProtectedTiles.Count];
        for (var y = 0; y < Tiles.Height; y++) for (var x = 0; x < Tiles.Width; x++)
        { var point = Map(new(x, y)); mask[point.Y * tiles.Width + point.X] = ProtectedTiles[y * Tiles.Width + x]; }
        return new(tiles, Source with { Width = tiles.Width, Height = tiles.Height }, Legacy,
            Things.Select(item => NativeMapDocument.TransformClipboardPoint(item, Source, Map)).ToArray(),
            Scripts.Select(item => NativeMapDocument.TransformClipboardPoint(item, Source, Map)).ToArray(), mask);
    }
}
