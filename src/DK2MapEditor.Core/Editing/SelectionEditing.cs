using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    public SelectionClipboard CopySelection(TileRect rectangle, bool protectBoundary = false)
    {
        RequireVerifiedNativeCollections();
        var tiles = Copy(rectangle);
        var records = Records();
        // Original Select_Copy / Areas_WriteThings walk rows, then columns,
        // then Creature/Door/Trap/Object/Magic lists. Stable sorting preserves
        // the list insertion order of stacked members within one category.
        var things = records.Where(IsPlacedThing)
            .Select(record => (Record:record,HasPoint:TryThingPoint(record,out var point),Point:point))
            .Where(item => item.HasPoint && rectangle.Contains(item.Point.X,item.Point.Y)
                && (!protectBoundary || !IsEditorProtectedCell(item.Point.X,item.Point.Y)))
            .OrderBy(item=>item.Point.Y).ThenBy(item=>item.Point.X)
            .ThenBy(item=>SelectionThingOrder(item.Record)).Select(item=>item.Record).ToArray();
        var roots = things.SelectMany(record => record.Fields.Where(field => field.Name.EndsWith("Trigger", StringComparison.Ordinal)).Select(field => ushort.Parse(record.Read(field)))).ToList();
        var links = new List<NativeRecord>();
        if (IsLegacyProject)
        {
            var snapshots = things.Select(Capture).ToArray();
            links.AddRange(records.Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal) && SelectionClipboard.IsOwnedLink(Capture(record), snapshots)));
            roots.AddRange(links.Select(record => ushort.Parse(record.Read(record.Fields.Single(field => field.Name == "trigger")))));
        }
        // Only the selected reachable graph needs an unambiguous ID mapping.
        // Unrelated malformed imports must not prevent an unscripted copy or
        // become part of its payload merely because they share a global file.
        var scriptRecords = records.Where(record => record.File.Signature == 0xD2 || record.Category is "Project.TRIGGER" or "Project.ACTION")
            .GroupBy(record => ushort.Parse(record.Read(record.Fields.Single(field => field.Name is "ID" or "id"))))
            .ToDictionary(group => group.Key, group => group.ToArray());
        var graph = new Dictionary<ushort, NativeRecord>(); var pending = new Queue<ushort>(roots);
        while (pending.TryDequeue(out var id))
        {
            if (id == 0 || graph.ContainsKey(id)) continue;
            if (!scriptRecords.TryGetValue(id, out var matches)) throw new InvalidDataException("Selection contains a dangling script link.");
            if (matches.Length != 1) throw new InvalidDataException("Selection contains an ambiguous script ID.");
            var record = matches[0];
            graph.Add(id, record);
            foreach (var edge in ScriptGraph.Edges(record)) pending.Enqueue(edge.Target);
        }
        static SelectionClipboard.Payload Capture(NativeRecord record) => new(record.File.Signature, record.Category, record.File.Bytes.AsSpan(record.Offset, record.Length).ToArray(), record.Fields, record.Offset);
        var mask = protectBoundary ? MapDrawing.Rectangle(rectangle, true).Select(point => IsEditorProtectedCell(point.X, point.Y)).ToArray() : null;
        return new(tiles, rectangle, IsLegacyProject, things.Select(Capture).ToArray(), graph.Values.Concat(links).Select(Capture).ToArray(), mask);
    }

    public void CutSelection(TileRect rectangle, MapTile fill, bool respectEditorRules = false) => Edit("Cut tiles and things", () =>
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0 || rectangle.X < 0 || rectangle.Y < 0
            || (long)rectangle.X + rectangle.Width > Width || (long)rectangle.Y + rectangle.Height > Height) throw new ArgumentOutOfRangeException(nameof(rectangle));
        if (respectEditorRules) fill = EditorCutFill(fill);
        var records = Records().Where(record => IsPlacedThing(record) && TryThingPoint(record, out var point) && rectangle.Contains(point.X, point.Y)
            && (!respectEditorRules || !IsEditorProtectedCell(point.X, point.Y))).ToArray();
        foreach (var record in records.OrderByDescending(record => record.Offset)) DeleteRecord(record);
        if (respectEditorRules) PaintEditor(MapDrawing.Rectangle(rectangle, true), fill); else Paint(MapDrawing.Rectangle(rectangle, true), fill);
    });

    public void PasteSelection(TilePoint position, SelectionClipboard clipboard, bool replaceThings = true, bool clipToMap = false, bool respectEditorRules = false) => Edit("Paste tiles, things and scripts", () =>
    {
        RequireVerifiedNativeCollections();
        if (clipboard.Legacy != IsLegacyProject) throw new ArgumentException("Native and legacy project selections cannot be mixed without compilation.");
        if (clipToMap && position.X >= 0 && position.Y >= 0 && position.X < Width && position.Y < Height)
            clipboard = clipboard.Crop(new(0, 0, Math.Min(clipboard.Tiles.Width, Width - position.X), Math.Min(clipboard.Tiles.Height, Height - position.Y)));
        if (position.X < 0 || position.Y < 0 || (long)position.X + clipboard.Tiles.Width > Width || (long)position.Y + clipboard.Tiles.Height > Height)
            throw new ArgumentOutOfRangeException(nameof(position), "The entire selection must fit inside the map.");
        if (respectEditorRules)
        {
            var source = clipboard.Source; var original = clipboard;
            clipboard = clipboard.FilterThings(point => !original.IsProtectedTile(point.X - source.X, point.Y - source.Y)
                && !IsEditorProtectedCell(position.X + point.X - source.X, position.Y + point.Y - source.Y));
        }
        if (replaceThings)
        {
            var target = new TileRect(position.X, position.Y, clipboard.Tiles.Width, clipboard.Tiles.Height);
            foreach (var record in Records().Where(record => IsPlacedThing(record) && TryThingPoint(record, out var point) && target.Contains(point.X, point.Y)
                && (!respectEditorRules || !IsEditorProtectedCell(point.X, point.Y))).OrderByDescending(record => record.Offset))
                DeleteRecord(record);
        }
        var usedScripts = Records().Where(record => record.File.Signature == 0xD2 || record.Category is "Project.TRIGGER" or "Project.ACTION")
            .Select(record => ushort.Parse(record.Read(record.Fields.Single(field => field.Name is "ID" or "id")))).ToHashSet();
        var mapping = new Dictionary<ushort, ushort>(); var candidate = 1;
        foreach (var item in clipboard.Scripts.Where(item => !item.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)))
        {
            var oldID = ushort.Parse(item.Fields.Single(field => field.Name is "ID" or "id").Read(item.Data));
            ushort replacement;
            if (IsLegacyProject) replacement = AllocateLegacyScriptID(item.Category == "Project.ACTION", usedScripts);
            else
            {
                while (candidate <= ushort.MaxValue && usedScripts.Contains((ushort)candidate)) candidate++;
                if (candidate > ushort.MaxValue) throw new InvalidOperationException("No unused script IDs remain.");
                replacement = (ushort)candidate;
            }
            mapping.Add(oldID, replacement); usedScripts.Add(replacement);
        }
        var uniqueMapping = new Dictionary<(uint Type,uint ID), uint>();
        var uniqueUsed = IsLegacyProject ? UsedProjectUniqueIDs() : new HashSet<uint>();
        uint nextUnique = 1;
        foreach (var item in clipboard.Things)
        {
            var moved = TransformClipboardPoint(item, clipboard.Source, point => new(point.X + position.X - clipboard.Source.X, point.Y + position.Y - clipboard.Source.Y), relativeInput: false);
            var data = moved.Data;
            RemapClipboardScripts(item, data, mapping, false);
            var unique = item.Fields.FirstOrDefault(field => field.Name == "uniqueid");
            if (unique is not null)
            {
                while (uniqueUsed.Contains(nextUnique)) nextUnique = checked(nextUnique + 1);
                var oldID = uint.Parse(unique.Read(data)); var newID = nextUnique; uniqueUsed.Add(newID);
                var type = uint.Parse(item.Fields.Single(field => field.Name == "node.type").Read(data));
                if (oldID != 0) uniqueMapping[(type,oldID)] = newID;
                unique.Encode(newID.ToString()).CopyTo(data, unique.Offset);
            }
            InsertClipboardPayload(item, data);
        }
        foreach (var item in clipboard.Scripts)
        {
            var moved = TransformClipboardPoint(item, clipboard.Source, point => new(point.X + position.X - clipboard.Source.X, point.Y + position.Y - clipboard.Source.Y), relativeInput: false);
            var data = moved.Data;
            RemapClipboardScripts(item, data, mapping, true);
            if (SelectionClipboard.OwnedLinkTarget(item) is { } owner
                && uniqueMapping.TryGetValue((owner.Type,uint.Parse(owner.Field.Read(data))), out var newID))
                owner.Field.Encode(newID.ToString()).CopyTo(data, owner.Field.Offset);
            InsertClipboardPayload(item, data);
        }
        WriteSelectionTiles(position, clipboard, respectEditorRules);
        RefreshProjectBacklinks(mapping.Values.ToArray());
    });

    public void TransformSelection(TileRect rectangle, bool rotate, bool horizontal = false, bool clockwise = true, bool respectEditorRules = false) => Edit("Transform tiles and placed things", () =>
    {
        RequireVerifiedNativeCollections();
        if (rotate && rectangle.Width != rectangle.Height) throw new ArgumentException("In-place rotation requires a square selection.", nameof(rectangle));
        if (!rotate && (horizontal ? rectangle.Width : rectangle.Height) < 2) throw new ArgumentException("Reflection requires at least two tiles along the reflected axis.", nameof(rectangle));
        var source = CopySelection(rectangle);
        if (respectEditorRules) source = source.WithProtectedTiles(MapDrawing.Rectangle(rectangle, true).Select(point => IsEditorProtectedCell(point.X, point.Y)).ToArray());
        var transformed = rotate ? clockwise ? source.RotateClockwise() : source.RotateCounterclockwise() : source.Mirror(horizontal);
        if ((long)rectangle.X + transformed.Tiles.Width > Width || (long)rectangle.Y + transformed.Tiles.Height > Height)
            throw new ArgumentException("The transformed selection does not fit inside the map.");
        // Original Select_Rotate/Select_Symmetry move all selected physical things
        // before applying tile-mask rules, including things moving to/from an edge.
        // Do not reuse protected copy/paste filtering here: it would drop identities.
        // Existing IDs and scope links remain unchanged during in-place transforms.
        foreach (var item in transformed.Things.Concat(transformed.Scripts))
        {
            var record = Records().Single(record => record.File.Signature == item.FileSignature && record.Offset == item.SourceOffset && record.Category == item.Category);
            item.Data.CopyTo(record.File.Bytes, record.Offset);
        }
        WriteSelectionTiles(new(rectangle.X, rectangle.Y), transformed, respectEditorRules);
    });

    private void WriteSelectionTiles(TilePoint position, SelectionClipboard clipboard, bool respectEditorRules)
    {
        if (!respectEditorRules) { Paste(position, clipboard.Tiles); return; }
        for (var y = 0; y < clipboard.Tiles.Height; y++) for (var x = 0; x < clipboard.Tiles.Width; x++)
        {
            if (IsEditorProtectedCell(position.X + x, position.Y + y)) continue;
            PutTile(position.X + x, position.Y + y, clipboard.IsProtectedTile(x, y) ? new MapTile(1, 2, 1) : clipboard.Tiles.Tiles[y * clipboard.Tiles.Width + x]);
        }
    }

    private void InsertClipboardPayload(SelectionClipboard.Payload item, byte[] data)
    {
        var file = CollectionFile(item.FileSignature);
        if (IsLegacyProject)
        {
            data.AsSpan(0, 8).Clear();
            var structure = item.Category[8..];
            if (file.Signature == LegacyEvents && file.UInt32(4) is not (2 or 3))
                throw new InvalidOperationException("Upgrade historical scenario sections before pasting scripts.");
            var expected = LegacyStructure(file, structure.StartsWith("TRIGGERLINK", StringComparison.Ordinal) ? "TriggerLink" : structure == "ACTION" ? "Action" : "Trigger");
            if (LegacyProjectSchemas.Size(structure) != data.Length || expected != structure)
                throw new InvalidDataException("Project selection versions differ; convert them before pasting.");
            InsertLegacyRecord(file, structure, data);
        }
        else InsertRecord(file, data);
    }

    private static void RemapClipboardScripts(SelectionClipboard.Payload item, byte[] data, Dictionary<ushort, ushort> mapping, bool script)
    {
        foreach (var field in SelectionClipboard.LinkFields(item).Concat(script ? item.Fields.Where(field => field.Name is "ID" or "id") : []))
            if (mapping.TryGetValue(ushort.Parse(field.Read(data)), out var newID)) field.Encode(newID.ToString()).CopyTo(data, field.Offset);
    }

    private static bool IsPlacedThing(NativeRecord record) => record.File.Signature == LegacyThings || record.File.Signature == 0xBE && record.Category is "GAMEOBJECT" or "GAMETRAP" or "GAMEDOOR" or "GAMENEUTRAL" or "GAMEHERO" or "GAMEKEEPER";
    private static uint SelectionThingOrder(NativeRecord record)
    {
        if(record.File.Signature==LegacyThings)
            return record.Fields.FirstOrDefault(field=>field.Name=="node.type") is { } type?uint.Parse(record.Read(type)):uint.MaxValue;
        return record.Category switch
        {
            "GAMEDOOR"=>1,"GAMETRAP"=>2,
            "GAMEOBJECT"=>uint.Parse(record.Read(record.Fields.Single(field=>field.Name=="bObjectID"))) is >=0x75 and <=0x81?4u:3u,
            _=>0
        };
    }
    private static bool TryThingPoint(NativeRecord record, out TilePoint point)
    {
        var x = record.Fields.FirstOrDefault(field => field.Name.EndsWith("LocationX", StringComparison.Ordinal) || field.Name == "x");
        var y = record.Fields.FirstOrDefault(field => field.Name.EndsWith("LocationY", StringComparison.Ordinal) || field.Name == "y");
        point = x is not null && y is not null ? new(int.Parse(record.Read(x)), int.Parse(record.Read(y))) : default;
        return x is not null && y is not null;
    }

    internal static (NativeField X, NativeField Y, int Bias)? ClipboardPointFields(SelectionClipboard.Payload item)
    {
        var x = item.Fields.FirstOrDefault(field => field.Name.EndsWith("LocationX", StringComparison.Ordinal) || field.Name == "x");
        var y = item.Fields.FirstOrDefault(field => field.Name.EndsWith("LocationY", StringComparison.Ordinal) || field.Name == "y");
        var bias = 0;
        if (x is null && (item.FileSignature == 0xD2 && BinaryPrimitives.ReadUInt32LittleEndian(item.Data) == 0xD6 || item.Category == "Project.ACTION"))
        {
            var type = item.Data[item.FileSignature == 0xD2 ? 22 : 54];
            if (type is 1 or 0x16 or 0x31 || item.FileSignature == 0xD2 && type is 2 or 0x20)
            {
                x = item.Fields.FirstOrDefault(field => field.Name is "Param5" or "NativeArgument5" or "param5");
                y = item.Fields.FirstOrDefault(field => field.Name is "Param7" or "NativeArgument7" or "param7");
                bias = type == 0x20 && item.FileSignature == 0xD2 ? 1 : 0;
            }
        }
        return x is null || y is null ? null : (x, y, bias);
    }

    internal static SelectionClipboard.Payload TransformClipboardPoint(SelectionClipboard.Payload item, TileRect rectangle, Func<TilePoint, TilePoint> transform, bool relativeInput = true)
    {
        var data = (byte[])item.Data.Clone();
        if (item.LocalPoint == false || ClipboardPointFields(item) is not { } fields) return item with { Data = data };
        var (x, y, bias) = fields;
        var point = new TilePoint(int.Parse(x.Read(data)) - bias, int.Parse(y.Read(data)) - bias);
        if (item.LocalPoint is null && !rectangle.Contains(point.X, point.Y)) return item with { Data = data };
        var mapped = relativeInput ? transform(new(point.X - rectangle.X, point.Y - rectangle.Y)) : transform(point);
        if (relativeInput) mapped = new(mapped.X + rectangle.X, mapped.Y + rectangle.Y);
        x.Encode((mapped.X + bias).ToString()).CopyTo(data, x.Offset); y.Encode((mapped.Y + bias).ToString()).CopyTo(data, y.Offset);
        return item with { Data = data };
    }
}
