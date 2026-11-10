using System.Buffers.Binary;
using System.Text;

namespace DK2MapEditor.Core.Editing;

/// <summary>A lossless native bundle. No source file is ever written by this class.</summary>
public sealed partial class NativeMapDocument
{
    private const int MaximumFileBytes = Formats.NativeDescriptorReferences.MaximumFileBytes;
    private const long MaximumHistoryBytes = 64 * 1024 * 1024;
    // Preserved French Win32 char[64] references use the same Western ANSI
    // byte encoding as historical editor strings, not lossy 7-bit ASCII.
    private static readonly NativeField NativeReference = new("Reference",0,64,NativeValueKind.WesternAnsi);
    private readonly List<NativeFile> files = [];
    private readonly Dictionary<NativeFile, byte[]> originals = [];
    private readonly List<Change> undo = [];
    private readonly List<Change> redo = [];
    private long historyBytes;
    private bool transactionOpen;

    public NativeFile MainFile { get; private set; } = null!;
    public NativeFile TileFile { get; private set; } = null!;
    public IReadOnlyList<NativeFile> Files => files.AsReadOnly();
    public IReadOnlyList<string> MissingReferences { get; private set; } = [];
    public int Width => checked((int)TileFile.UInt32(20));
    public int Height => checked((int)TileFile.UInt32(24));
    public bool CanUndo => activePenStroke is null && undo.Count != 0;
    public bool CanRedo => activePenStroke is null && redo.Count != 0;
    public bool HasChanges => editorChangeCount != 0 || files.Any(HasSemanticFileChanges) || ScenarioNamesChanged || NativeGameTargetChanged || NativeResizeFillChanged || NativeGateBindingsChanged || NativeAreaScopesChanged || NativeOwnerScopesChanged || LevelModifierDeltaChanged || LevelModifierOwnedChanged;
    /// <summary>Monotonic committed-edit lifetime, unlike the reversible history
    /// counter. No-op edits do not invalidate a preview; Undo/Redo always do.</summary>
    public long EditRevision { get; private set; }
    public event EventHandler? Changed;

    public static NativeMapDocument Create(int width, int height, string name, NativeMapDocument? definitionsFrom = null)
    {
        if (width is < 1 or > 1024 || height is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(width));
        if (string.IsNullOrWhiteSpace(name) || name.Length > 32 || name.Any(character => character > 127 || Path.GetInvalidFileNameChars().Contains(character)) || name.Contains('\\') || name.Contains('/') || name is "." or "..")
            throw new ArgumentException("The native map file name must be 1–32 ASCII characters and contain no path separators.", nameof(name));
        var document = new NativeMapDocument();
        var descriptor = new byte[26939];
        WriteUInt32(descriptor, 0, 0xDC); WriteUInt32(descriptor, 4, 4); WriteUInt32(descriptor, 8, (uint)descriptor.Length);
        WriteUInt32(descriptor, 12, 0xDD); WriteUInt32(descriptor, 16, 28); WriteUInt32(descriptor, 20, 0x001C000F); WriteUInt32(descriptor, 24, 0x3000);
        WriteUInt32(descriptor, 48, 0xDF); WriteUInt32(descriptor, 52, 25683);
        LegacySchemas.Field("GAMEINFO", "wstrMainName").Encode(name).CopyTo(descriptor, 56);
        descriptor[4538] = 20; descriptor[25660] = 1;
        WriteUInt32(descriptor, 25739, 0xDE); WriteUInt32(descriptor, 25743, 1192);
        (uint type, string path)[] references =
        [
            (0x6E, "Terrain"), (0xF0, "Objects"), (0x78, "Rooms"), (0xAA, "Creatures"), (0xA0, "CreatureSpells"),
            (0x82, "Traps"), (0x8C, "Doors"), (0x104, "Shots"), (0x96, "KeeperSpells"),
            (0xB4, "Maps/" + name + "Players.kld"), (0x64, "Maps/" + name + "Map.kld"), (0xD2, "Maps/" + name + "Triggers.kld"),
            (0xE6, "GlobalVariables.kwd"), (0xE6, "Maps/" + name + "Variables.kld"), (0xBE, "Maps/" + name + "Things.kld")
        ];
        for (var index = 0; index < references.Length; index++)
        {
            WriteUInt32(descriptor, 25747 + index * 72, references[index].type);
            NativeReference.Encode("Data\\Editor\\" + references[index].path.Replace('/', '\\')).CopyTo(descriptor, 25755 + index * 72);
        }
        document.MainFile = new(name + ".kwd", Path.Combine("Data", "Editor", "Maps", name + ".kwd"), descriptor);
        document.Add(document.MainFile);
        var tiles = new byte[36 + width * height * 4];
        WriteUInt32(tiles, 0, 0x64); WriteUInt32(tiles, 4, 4); WriteUInt32(tiles, 8, (uint)tiles.Length);
        WriteUInt32(tiles, 12, 0x65); WriteUInt32(tiles, 16, 8); WriteUInt32(tiles, 20, (uint)width); WriteUInt32(tiles, 24, (uint)height);
        WriteUInt32(tiles, 28, 0x66); WriteUInt32(tiles, 32, (uint)tiles.Length - 36);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                WriteUInt32(tiles, 36 + (y * width + x) * 4, new MapTile(x == 0 || x == width - 1 || y == 0 || y == height - 1 ? (byte)30 : (byte)2, 2, 1).Packed);
        document.TileFile = new(name + "Map.kld", Path.Combine("Data", "Editor", "Maps", name + "Map.kld"), tiles);
        document.Add(document.TileFile);
        foreach (var (signature, suffix) in new (uint, string)[] { (0xB4, "Players"), (0xBE, "Things"), (0xD2, "Triggers"), (0xE6, "Variables") })
        {
            var length = signature == 0xD2 ? 60 : 56;
            var bytes = new byte[length];
            WriteUInt32(bytes, 0, signature); WriteUInt32(bytes, 4, 4); WriteUInt32(bytes, 8, (uint)length);
            WriteUInt32(bytes, 12, signature + 1); WriteUInt32(bytes, 16, signature == 0xD2 ? 32u : 28u);
            WriteUInt32(bytes, signature == 0xD2 ? 28 : 24, 1);
            WriteUInt32(bytes, length - 8, signature + 2);
            document.Add(new(name + suffix + ".kld", Path.Combine("Data", "Editor", "Maps", name + suffix + ".kld"), bytes));
        }
        if (definitionsFrom is not null && !definitionsFrom.IsLegacyProject)
            foreach (var reference in references.Where(reference => LegacyGlobalSchemas.Find(reference.type) is not null || reference.type is 0xF0 or 0x104 || reference.path == "GlobalVariables.kwd"))
            {
                var definition = CopyNativeDefinition(definitionsFrom, reference.type);
                if (definition is null) continue;
                var targetPath = Path.Combine("Data", "Editor", Path.HasExtension(reference.path) ? reference.path : reference.path + ".kwd");
                document.Add(new(definition.Value.Source, targetPath, definition.Value.Bytes));
            }
        document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEPLAYER"), new(width / 2, height / 2), 3);
        document.ResetHistory();
        document.MissingReferences = references.Select(reference => Path.Combine("Data", "Editor", reference.path.Replace('/', Path.DirectorySeparatorChar)))
            .Select(reference => Path.HasExtension(reference) ? reference : reference + ".kwd")
            .Where(reference => !document.files.Any(file => file.ExportPath.Equals(reference, StringComparison.OrdinalIgnoreCase))).ToArray();
        document.InitializeActivityStatistics(newMap: true);
        return document;
    }

    public static NativeMapDocument Load(string path)=>Load(path,null);
    public static NativeMapDocument Load(string path,Action<EditorFileProgress>? progress)
    {
        progress?.Invoke(new(EditorFilePhase.Read,path));
        if (Path.GetExtension(path).Equals(".map", StringComparison.OrdinalIgnoreCase))
        {
            var loaded = ImportTextMap(path,progress); loaded.MarkLoadedTextMap();progress?.Invoke(new(EditorFilePhase.Done,path));return loaded;
        }
        using (var probe = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Span<byte> signature = stackalloc byte[4]; probe.ReadExactly(signature);
            if (BinaryPrimitives.ReadUInt32LittleEndian(signature) is Formats.LegacySectionFile.MapFileSignature or Formats.LegacySectionFile.AreaFileSignature)
            {
                var loaded=LoadLegacyProject(path,progress);progress?.Invoke(new(EditorFilePhase.Done,path));return loaded;
            }
        }
        var document = new NativeMapDocument();
        var fullPath = Path.GetFullPath(path);
        var main = ReadFile(fullPath, Path.GetFileName(fullPath));
        document.Add(main);
        document.MainFile = main;
        progress?.Invoke(new(FilePhase(main.Signature),fullPath));
        if (main.Signature == 0xDC)
        {
            var links = Formats.NativeDescriptorReferences.Resolve(main.Bytes, fullPath);
            var readable=links.Where(link=>link.SourcePath is not null).ToArray();var completed=0;
            foreach (var link in readable)
            {
                progress?.Invoke(new(EditorFilePhase.Read,link.SourcePath,completed,readable.Length));
                var file=ReadFile(link.SourcePath!,link.ExportPath);
                if(link.Type!=0 && file.Signature!=link.Type)
                    throw new InvalidDataException($"Unexpected companion signature; KWD reference expects 0x{link.Type:X}: {link.SourcePath}");
                document.Add(file);
                progress?.Invoke(new(FilePhase(file.Signature),link.SourcePath,++completed,readable.Length));
            }
            // Place the descriptor beside the per-map files in an export game root.
            main = new NativeFile(main.SourcePath, Path.Combine("Data", "Editor", "Maps", Path.GetFileName(fullPath)), main.Bytes);
            document.originals.Remove(document.MainFile);
            document.files[0] = main;
            document.originals.Add(main, (byte[])main.Bytes.Clone());
            document.MainFile = main;
            document.MissingReferences = links.Where(link => link.SourcePath is null).Select(link => link.Reference).ToArray();
            document.TileFile = document.files.FirstOrDefault(file => file.Signature == 0x64)
                ?? throw new InvalidDataException("The map tile file referenced by this KWD is missing.");
        }
        else if (main.Signature == 0x64) document.TileFile = main;
        else throw new InvalidDataException("Native editing currently requires a KWD descriptor or Map.kld.");
        progress?.Invoke(new(EditorFilePhase.Validate,fullPath));document.CheckTiles();
        // Parse every editable collection now; refuse truncated records before editing is enabled.
        _ = document.Records();
        document.LoadNativeScenarioNames();
        document.InitializeActivityStatistics();
        progress?.Invoke(new(EditorFilePhase.Done,fullPath));
        return document;
    }

    public MapTile GetTile(int x, int y)
    {
        CheckPoint(x, y);
        return MapTile.FromPacked(TileFile.UInt32(36 + (y * Width + x) * 4));
    }

    public void Paint(IEnumerable<TilePoint> points, MapTile tile, string description = "Paint") => Edit(description, () =>
    {
        foreach (var point in points)
        {
            if (point.X < 0 || point.X >= Width || point.Y < 0 || point.Y >= Height) continue;
            PutTile(point.X, point.Y, tile);
        }
    });

    public void Resize(int width, int height, int shiftX, int shiftY, MapTile fill, bool removeOutside = false)
        => ResizeCore(width, height, shiftX, shiftY, fill, removeOutside, editorCaller: false);

    private void ResizeCore(int width, int height, int shiftX, int shiftY, MapTile fill, bool removeOutside, bool editorCaller) => Edit("Resize map", () =>
    {
        if (!editorCaller) retainRawCrystalTargets = true;
        if (width is < 3 or > 1024 || height is < 3 or > 1024) throw new ArgumentOutOfRangeException(nameof(width));
        if (shiftX is < -1024 or > 1024 || shiftY is < -1024 or > 1024) throw new ArgumentOutOfRangeException(nameof(shiftX));
        RequireVerifiedNativeCollections();
        NativeResizeDerivedTarget[] derivedTargets = editorCaller && !IsLegacyProject ? CaptureNativeResizeDerivedTargets() : [];
        var oldWidth = Width; var oldHeight = Height;
        var oldTiles = TileFile.Bytes;
        var oldTileEnd = 36 + oldWidth * oldHeight * 4;
        var newPayloadSize = width * height * 4;
        var newFileSize = 36 + newPayloadSize + oldTiles.Length - oldTileEnd;
        if (newFileSize > MaximumFileBytes) throw new InvalidOperationException("Resized tile data and opaque tail exceed safe file-size bounds.");
        var tileBytes = new byte[newFileSize];
        oldTiles.AsSpan(0, 36).CopyTo(tileBytes);
        oldTiles.AsSpan(oldTileEnd).CopyTo(tileBytes.AsSpan(36 + newPayloadSize));
        WriteUInt32(tileBytes, 8, (uint)tileBytes.Length); WriteUInt32(tileBytes, 20, (uint)width); WriteUInt32(tileBytes, 24, (uint)height); WriteUInt32(tileBytes, 32, (uint)newPayloadSize);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var sourceX = x - shiftX; var sourceY = y - shiftY;
                var tile = x == 0 || y == 0 || x == width - 1 || y == height - 1 ? new MapTile(30, 2, 1) : fill;
                if (x > 0 && y > 0 && x < width - 1 && y < height - 1 && sourceX > 0 && sourceY > 0 && sourceX < oldWidth - 1 && sourceY < oldHeight - 1)
                    tile = MapTile.FromPacked(BinaryPrimitives.ReadUInt32LittleEndian(oldTiles.AsSpan(36 + (sourceY * oldWidth + sourceX) * 4)));
                WriteUInt32(tileBytes, 36 + (y * width + x) * 4, tile.Packed);
            }
        var objects = Records().Where(record => record.File.Signature == 0xBE && !record.Category.StartsWith("BandHero", StringComparison.Ordinal)).ToArray();
        var remove = new List<NativeRecord>();
        foreach (var record in objects)
        {
            var coordinates = record.Fields.Where(field => field.Name.EndsWith("LocationX", StringComparison.Ordinal) || field.Name.EndsWith("LocationY", StringComparison.Ordinal) || field.Name.EndsWith("StartX", StringComparison.Ordinal) || field.Name.EndsWith("StartY", StringComparison.Ordinal) || field.Name.EndsWith("EndX", StringComparison.Ordinal) || field.Name.EndsWith("EndY", StringComparison.Ordinal)).ToArray();
            var shifted = coordinates.ToDictionary(field => field, field => long.Parse(record.Read(field)) + (field.Name.EndsWith('X') ? shiftX : shiftY));
            var outside = shifted.Any(pair => pair.Value < 0 || pair.Value >= (pair.Key.Name.EndsWith('X') ? width : height));
            if (record.Category == "GAMEEFFECT" && !outside)
            {
                var x = shifted.Single(pair => pair.Key.Name.EndsWith("StartX", StringComparison.Ordinal)).Value;
                var y = shifted.Single(pair => pair.Key.Name.EndsWith("StartY", StringComparison.Ordinal)).Value;
                outside = x + int.Parse(record.Read(record.Fields.Single(field => field.Name == "wEffectWidth"))) >= width || y + int.Parse(record.Read(record.Fields.Single(field => field.Name == "wEffectHeight"))) >= height;
            }
            if (outside)
            {
                if (!removeOutside) throw new InvalidOperationException("Resizing would crop placed objects. Enable removal or choose a larger map.");
                remove.Add(record); continue;
            }
            foreach (var (field, value) in shifted) field.Encode(value.ToString()).CopyTo(record.File.Bytes, record.Offset + field.Offset);
        }
        if (editorCaller) RelocateEditorResizePlayers(shiftX, shiftY);
        else foreach (var record in Records().Where(record => record.File.Signature == 0xB4))
            foreach (var field in record.Fields.Where(field => field.Name.Contains("Camera", StringComparison.Ordinal) || field.Name.Contains("BuildArea", StringComparison.Ordinal)))
            {
                var value = int.Parse(record.Read(field));
                if (value == ushort.MaxValue) continue; // Generic compatibility policy retains imported FFFF independently.
                var horizontal = field.Name.Contains('X');
                value = Math.Clamp(value + (horizontal ? shiftX : shiftY), 0, (horizontal ? width : height) - 1);
                field.Encode(value.ToString()).CopyTo(record.File.Bytes, record.Offset + field.Offset);
            }
        RelocateResizeScriptTargets(width, height, shiftX, shiftY, editorCaller);
        // Descending offsets keep remaining record addresses stable during removal.
        // Resize owns its complete derived-target projection; generic Resize
        // retains its raw-payload contract even when a gate is cropped.
        nativeGateMutationDepth++;
        try
        {
            foreach (var record in remove.OrderByDescending(record => record.Offset))
                if (editorCaller && EditorResizeRect(record)) DeleteEditorResizeRegion(record);
                else if (editorCaller && EditorResizeThing(record)) DeleteEditorResizeThing(record);
                else DeleteRecord(record);
        }
        finally { nativeGateMutationDepth--; }
        if (IsLegacyProject) ResizeLegacyRecords(width, height, shiftX, shiftY, removeOutside, editorCaller);
        TileFile.Bytes = tileBytes;
        if (derivedTargets.Length != 0) ReprojectNativeResizeDerivedTargets(derivedTargets, shiftX, shiftY);
    });

    public void FloodFill(TilePoint start, MapTile replacement, TileRect? limit = null, bool protectBoundary = false)
        => FloodFillCore(start, replacement, limit, protectBoundary, null);

    private void FloodFillCore(TilePoint start, MapTile replacement, TileRect? limit, bool protectBoundary, Func<MapTile, MapTile>? componentKey) => Edit("Flood fill", () =>
    {
        CheckPoint(start.X, start.Y);
        var target = GetTile(start.X, start.Y);
        if (componentKey is null && target == replacement) return;
        var key = componentKey is null ? target : componentKey(target);
        var visited = new bool[Width * Height];
        var pending = new Queue<TilePoint>();
        pending.Enqueue(start);
        while (pending.TryDequeue(out var point))
        {
            if (point.X < 0 || point.X >= Width || point.Y < 0 || point.Y >= Height || (limit.HasValue && !limit.Value.Contains(point.X, point.Y))) continue;
            var index = point.Y * Width + point.X;
            if (visited[index]) continue;
            visited[index] = true;
            if (protectBoundary && IsEditorProtectedCell(point.X, point.Y)) continue;
            var current = GetTile(point.X, point.Y);
            if ((componentKey is null ? current : componentKey(current)) != key) continue;
            PutTile(point.X, point.Y, replacement);
            pending.Enqueue(new(point.X - 1, point.Y)); pending.Enqueue(new(point.X + 1, point.Y));
            pending.Enqueue(new(point.X, point.Y - 1)); pending.Enqueue(new(point.X, point.Y + 1));
        }
    });

    public TileClipboard Copy(TileRect rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0 || rect.X < 0 || rect.Y < 0 || (long)rect.X + rect.Width > Width || (long)rect.Y + rect.Height > Height)
            throw new ArgumentOutOfRangeException(nameof(rect));
        return new(rect.Width, rect.Height, MapDrawing.Rectangle(rect, true).Select(point => GetTile(point.X, point.Y)).ToArray());
    }

    public void Paste(TilePoint start, TileClipboard clipboard) => Edit("Paste", () =>
    {
        if (clipboard.Width <= 0 || clipboard.Height <= 0 || (long)clipboard.Width * clipboard.Height != clipboard.Tiles.Count)
            throw new ArgumentException("Invalid tile clipboard.", nameof(clipboard));
        for (var y = 0; y < clipboard.Height; y++)
            for (var x = 0; x < clipboard.Width; x++)
                if (start.X + x >= 0 && start.X + x < Width && start.Y + y >= 0 && start.Y + y < Height)
                    PutTile(start.X + x, start.Y + y, clipboard.Tiles[y * clipboard.Width + x]);
    });

    public void SetField(NativeRecord record, NativeField field, string value) => Edit(field.Name, () =>
    {
        if (SetNativeOrphanAreaRoot(record, field, value)) return;
        if (SetNativeOrphanOwnerRoot(record, field, value)) return;
        if (!files.Contains(record.File) || !record.Fields.Contains(field)) throw new ArgumentException("The field does not belong to this document.");
        if (!IsLegacyProject && Formats.NativeOpaqueFile.IsUnknown(record.File.Signature))
            throw new InvalidDataException("This native file has no verified editing schema.");
        if (!IsLegacyProject && Formats.NativeCollectionLayout.IsCollection(record.File.Signature))
        {
            _ = NativeCollectionDataEnd(record.File);
            if (!Records().Any(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length
                && item.Category == record.Category))
                throw new ArgumentException("Select a current record with a verified editing layout.");
        }
        var encoded = field.Encode(value);
        RemapEntityIdentity(record, field, encoded);
        RemapProjectUniqueIdentity(record, field, encoded);
        RemapScriptIdentity(record, field, encoded);
        void Write()
        {
            if (field.BitMask != 0)
            {
                var offset = record.Offset + field.Offset;
                record.File.Bytes[offset] = (byte)((record.File.Bytes[offset] & ~field.BitMask) | (encoded[0] == 0 ? 0 : field.BitMask));
            }
            else encoded.CopyTo(record.File.Bytes, record.Offset + field.Offset);
        }
        if (field.Name is "dwGateLocationX" or "dwGateLocationY" or "bGateType") MutateNativeGate(record, Write);
        else Write();
    });

    public NativeRecord AddRecord(RecordTemplate template, TilePoint position, byte owner = 3)
    {
        CheckPoint(position.X, position.Y);
        var file = CollectionFile(template.FileSignature);
        var id = NextID(template.Structure);
        var bytes = template.Create(position, owner, id);
        var offset = 0;
        Edit("Add " + template.Structure, () => offset = InsertRecord(file, bytes));
        return Records().First(record => record.File == file && record.Offset == offset);
    }

    private NativeFile CollectionFile(uint signature) =>
        files.LastOrDefault(file => file.Signature == signature && file.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        ?? files.LastOrDefault(file => file.Signature == signature)
        ?? throw new InvalidOperationException("This map does not contain the required collection file.");

    public NativeRecord DuplicateRecord(NativeRecord record)
    {
        if (IsLegacyProject) return DuplicateLegacyRecord(record);
        RequireCollectionRecord(record);
        if (record.File.Signature == 0xD2)
        {
            var rootID = ushort.Parse(record.Read(record.Fields.Single(field => field.Name == "ID")));
            var newID = (ushort)0;
            Edit("Duplicate script graph", () => newID = CloneScriptGraphs([rootID])[rootID]);
            return Records().Single(item => item.File.Signature == 0xD2 && item.Read(item.Fields.Single(field => field.Name == "ID")) == newID.ToString());
        }
        var bytes = record.File.Bytes.AsSpan(record.Offset, record.Length).ToArray();
        var copyAreaScope = record.Category == "GAMEAREA" && HasNativeAreaScope(record);
        var idField = record.Fields.FirstOrDefault(field => field.Name is "ID" or "bAreaID" or "bBandID" or "bEffectID");
        if (idField is not null)
        {
            var id = NextID(record.Category);
            idField.Encode(id.ToString()).CopyTo(bytes, idField.Offset);
            if (record.Category == "GAMEBAND") for (var index = 0; index < 16; index++) bytes[51 + index * 32 + 30] = (byte)id;
        }
        var offset = 0;
        Edit("Duplicate " + record.Category, () =>
        {
            var triggerOffsets = record.Fields.Where(field => field.Name.EndsWith("Trigger", StringComparison.Ordinal)).Select(field => field.Offset).ToList();
            if (record.Category == "GAMEBAND") triggerOffsets.AddRange(Enumerable.Range(0, 16).Select(index => 51 + index * 32 + 24));
            var roots = triggerOffsets.Select(position => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(position))).Where(id => id != 0).ToArray();
            var mapping = CloneScriptGraphs(roots);
            foreach (var position in triggerOffsets)
            {
                var oldID = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(position));
                if (mapping.TryGetValue(oldID, out var newID)) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(position), newID);
            }
            offset = InsertRecord(record.File, bytes, copyAreaScope);
            CopyNativeOwnerScopes(record, Records().Single(item => item.File == record.File && item.Offset == offset && item.Length == bytes.Length));
        });
        return Records().First(item => item.File == record.File && item.Offset == offset);
    }

    private IReadOnlyDictionary<ushort, ushort> CloneScriptGraphs(IEnumerable<ushort> roots)
    {
        var source = Records().Where(item => item.File.Signature == 0xD2).ToDictionary(item => ushort.Parse(item.Read(item.Fields.Single(field => field.Name == "ID"))));
        var pending = new Queue<ushort>(roots);
        var graph = new Dictionary<ushort, NativeRecord>();
        while (pending.TryDequeue(out var id))
        {
            if (id == 0 || graph.ContainsKey(id)) continue;
            if (!source.TryGetValue(id, out var item)) throw new InvalidDataException("Cannot clone a script graph with dangling links.");
            graph.Add(id, item);
            foreach (var edge in ScriptGraph.Edges(item)) pending.Enqueue(edge.Target);
        }
        var mapping = new Dictionary<ushort, ushort>();
        var used = source.Keys.ToHashSet();
        var candidate = 1;
        foreach (var id in graph.Keys)
        {
            while (candidate <= ushort.MaxValue && used.Contains((ushort)candidate)) candidate++;
            if (candidate > ushort.MaxValue) throw new InvalidOperationException("No unused script IDs remain.");
            mapping[id] = (ushort)candidate; used.Add((ushort)candidate);
        }
        // Copy each source record before insertion, as the file's byte buffer changes.
        var copied = graph.ToDictionary(pair => pair.Key, pair => (pair.Value.File, Data: pair.Value.File.Bytes.AsSpan(pair.Value.Offset, pair.Value.Length).ToArray(), GateID: NativeGateTargetID(pair.Value)));
        foreach (var (id, copy) in copied)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(copy.Data.AsSpan(16), mapping[id]);
            foreach (var link in BinaryPrimitives.ReadUInt32LittleEndian(copy.Data) == 0xD6 ? new[] { 18 } : new[] { 18, 20 })
            {
                var target = BinaryPrimitives.ReadUInt16LittleEndian(copy.Data.AsSpan(link));
                if (mapping.TryGetValue(target, out var replacement)) BinaryPrimitives.WriteUInt16LittleEndian(copy.Data.AsSpan(link), replacement);
            }
            var inserted = InsertRecord(copy.File, copy.Data);
            var added = NativeGateBindingRecords().Single(record => record.File == copy.File && record.Offset == inserted);
            if (IsNativeDestroyAction(added)) SetNativeGateTargetID(added, copy.GateID);
        }
        return mapping;
    }

    public void DeleteRecord(NativeRecord record)
        => DeleteRecordCore(record, editorAreaRemoval: false);

    private void DeleteRecordCore(NativeRecord record, bool editorAreaRemoval)
    {
        RequireNoIncomingEntityReferences(record, editorAreaRemoval);
        if (IsLegacyProject) { DeleteLegacyRecord(record, preserveScopes: editorAreaRemoval); return; }
        RequireCollectionRecord(record);
        if (record.File.Signature == 0xD2)
        {
            var id = record.Read(record.Fields.Single(field => field.Name == "ID"));
            var graph = ScriptGraph.Build(this);
            if (graph.Roots.Concat(graph.Events.Values.SelectMany(item => item.Edges)).Any(edge =>
                (edge.Owner.File != record.File || edge.Owner.Offset != record.Offset) && edge.Target.ToString() == id))
                throw new InvalidOperationException("This script record is still referenced. Remove or redirect its incoming links before deleting it.");
        }
        // The compiler's misleadingly named Things_GetThingPos returns coordinates,
        // not collection indices. Native deletion therefore does not shift script indices.
        Edit("Delete " + record.Category, () => MutateNativeGate(record, () =>
        {
            RebaseNativeGateDeletion(record); RebaseNativeActionDeletion([record]); RebaseNativeAreaDeletion(record);
            RebaseNativeOwnerRemovals([record], preservePoints: editorAreaRemoval);
            var action = record.File.UInt32(record.Offset) == 0xD6;
            var bytes = new byte[record.File.Length - record.Length];
            record.File.Bytes.AsSpan(0, record.Offset).CopyTo(bytes);
            record.File.Bytes.AsSpan(record.Offset + record.Length).CopyTo(bytes.AsSpan(record.Offset));
            record.File.Bytes = bytes;
            UpdateCollectionHeader(record.File, -record.Length, -1, action);
        }, removing: true));
    }

    private void RequireCollectionRecord(NativeRecord record)
    {
        if (!files.Contains(record.File) || record.File.Signature is not (0xBE or 0xB4 or 0xE6 or 0xD2) || record.Category.StartsWith("BandHero", StringComparison.Ordinal) || !Records().Any(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length))
            throw new ArgumentException("Select a current top-level collection record.");
        _ = NativeCollectionDataEnd(record.File);
    }

    private int InsertRecord(NativeFile file, byte[] record, bool copyAreaScope = false)
    {
        var end = NativeCollectionDataEnd(file);
        if (file.Signature == 0xD2 && BinaryPrimitives.ReadUInt32LittleEndian(record) == 0xD5)
            end = checked(60 + (int)file.UInt32(20) * 24);
        RebaseNativeActionInsertion(file, end, record.Length);
        var bytes = new byte[file.Length + record.Length];
        file.Bytes.AsSpan(0, end).CopyTo(bytes); record.CopyTo(bytes, end);
        file.Bytes.AsSpan(end).CopyTo(bytes.AsSpan(end + record.Length)); file.Bytes = bytes;
        UpdateCollectionHeader(file, record.Length, 1, BinaryPrimitives.ReadUInt32LittleEndian(record) == 0xD6);
        if (file.Signature == 0xBE && BinaryPrimitives.ReadUInt32LittleEndian(record) == 0xCC && !compilingNativeGateIdentities)
            CompleteNativeGateInsertion(NativeGateBindingRecords().Single(item => item.File == file && item.Offset == end));
        if (file.Signature == 0xBE && BinaryPrimitives.ReadUInt32LittleEndian(record) == 0xC5 && !compilingNativeGateIdentities)
        {
            var area = NativeAreaAddresses().Values.Single(item => item.File == file && item.Offset == end);
            if (copyAreaScope) EnsureNativeAreaScope(area); else CompleteNativeAreaInsertion(area);
        }
        return end;
    }
    private static void UpdateCollectionHeader(NativeFile file, int byteDelta, int countDelta, bool action)
    {
        var headerLength = file.Signature == 0xD2 ? 60 : 56;
        var countOffset = file.Signature == 0xD2 && action ? 24 : 20;
        WriteUInt32(file.Bytes, 8, AdjustedNativeStoredSize(file.UInt32(8), byteDelta, file.Length));
        WriteUInt32(file.Bytes, countOffset, checked((uint)(file.UInt32(countOffset) + (long)countDelta)));
        WriteUInt32(file.Bytes, headerLength - 4, AdjustedNativeStoredSize(file.UInt32(headerLength - 4), byteDelta, NativeCollectionDataEnd(file) - headerLength));
    }

    // Original loaders ignore understated generic file-size metadata, including
    // zero. Retain its delta when representable; deletion cannot encode a
    // negative unsigned size, so write the actual resulting size in that case.
    private static uint AdjustedNativeStoredSize(uint stored, long byteDelta, int actualLength)
    {
        var adjusted = stored + byteDelta;
        return adjusted is >= 0 and <= uint.MaxValue ? (uint)adjusted : checked((uint)actualLength);
    }
    private ushort NextID(string category)
    {
        var script = category is "Trigger" or "Action" || category.StartsWith("Record 0xD", StringComparison.Ordinal);
        var used = Records().Where(item => script ? item.File.Signature == 0xD2 : item.Category == category)
            .SelectMany(item => item.Fields.Where(field => field.Name is "ID" or "bAreaID" or "bBandID" or "bEffectID").Select(item.Read))
            .Select(ushort.Parse).ToHashSet();
        for (var value = 1; value <= (script ? ushort.MaxValue : byte.MaxValue); value++)
            if (!used.Contains((ushort)value)) return (ushort)value;
        throw new InvalidOperationException("No unused record IDs remain.");
    }

    public IReadOnlyList<NativeRecord> Records()
    {
        if (IsLegacyProject) return LegacyRecords();
        var records = new List<NativeRecord>();
        foreach (var file in files)
        {
            if (NativeDefinitionSignature(file.Signature))
            {
                AppendDefinitionRecords(file, records);
                continue;
            }
            if (file.Signature == 0xDC)
            {
                var parametersEnd = 56L + file.UInt32(52);
                records.Add(new(file, 0, file.Length, "Map", LegacySchemas.Fields("GAMEINFO").Where(field => field.Offset + field.Size <= parametersEnd).ToArray()));
                continue;
            }
            if (file.Signature is not (0xBE or 0xB4 or 0xE6 or 0xD2))
            {
                if (Formats.NativeOpaqueFile.IsUnknown(file.Signature)) records.Add(new(file, 0, file.Length, Formats.NativeOpaqueFile.Category, []));
                continue;
            }
            AppendNativeCollectionRecords(file, records);
        }
        return records;
    }

    private static void AppendNativeCollectionRecords(NativeFile file, List<NativeRecord> records)
    {
        if (!Formats.NativeCollectionLayout.VerifiedHeader(file.Bytes, file.SourcePath)
            || !Formats.NativeCollectionLayout.VerifiedScriptPartition(file.Bytes, file.SourcePath))
        {
            records.Add(new(file, 0, file.Length, Formats.NativeCollectionLayout.OpaqueCategory, []));
            return;
        }
        foreach (var (offset, signature, length) in NativeCollectionEntries(file))
        {
            if (!Formats.NativeCollectionLayout.VerifiedRecord(file.Signature, signature, length))
            {
                records.Add(new(file, offset, length, Formats.NativeCollectionLayout.OpaqueCategory, []));
                continue;
            }
            var structure = file.Signature switch
            {
                0xB4 => "GAMEPLAYER",
                0xE6 => signature switch { 1 => "GAMEVARCREATURECOUNT", 2 => "GAMEVARTHINGSTATUS", 0x42 => "GAMEVARALLIANCE", 0x4D => "GAMEVARCREATURE", _ => "GAMEGLOBALVARIABLE" },
                0xBE => signature switch
                {
                    0xC2 => "GAMEOBJECT",
                    0xC3 => "GAMETRAP",
                    0xC4 => "GAMEDOOR",
                    0xC5 => "GAMEAREA",
                    0xC6 => "GAMENEUTRAL",
                    0xC7 => "GAMEHERO",
                    0xC8 => "GAMEKEEPER",
                    0xC9 => "GAMEBAND",
                    0xCB => "GAMEEFFECT",
                    0xCC => "GAMEGATE",
                    _ => ""
                },
                _ => ""
            };
            var fields = structure.Length == 0 && signature is 0xD5 or 0xD6 && length == 24
                ? LegacyEventSchemas.Fields(signature, file.Bytes[offset + 22]) : structure.Length == 0 ? [] : LegacySchemas.Fields(structure);
            fields = fields.Where(field => field.Offset + field.Size <= length).ToArray();
            records.Add(new(file, offset, length, structure.Length == 0 ? $"Record 0x{signature:X}" : structure, fields));
            if (signature == 0xC9 && file.Signature == 0xBE && length == 563)
                for (var hero = 0; hero < 16; hero++)
                    records.Add(new(file, offset + 51 + hero * 32, 32, $"BandHero {hero + 1}", LegacySchemas.Fields("GAMEBANDHERO")));
        }
        // Retail files can have opaque extensions after their counted records.
        // Keep those bytes untouched rather than discarding them on a round-trip.
    }

    // Stored payload sizes in shipped maps are sometimes stale. Reading and
    // insertion must share the same counted-record boundaries, before any suffix.
    private static int NativeCollectionDataEnd(NativeFile file)
    {
        if (!Formats.NativeCollectionLayout.VerifiedScriptPartition(file.Bytes, file.SourcePath))
            throw new InvalidDataException($"The native condition/action partition has no verified editing layout: {file.SourcePath}");
        var end = file.Signature == 0xD2 ? 60 : 56;
        foreach (var entry in NativeCollectionEntries(file))
        {
            if (!Formats.NativeCollectionLayout.VerifiedRecord(file.Signature, entry.Signature, entry.Length))
                throw new InvalidDataException($"The native collection contains a record with no verified editing layout: {file.SourcePath}, 0x{entry.Offset:X}.");
            end = entry.Offset + entry.Length;
        }
        return end;
    }

    private static IEnumerable<(int Offset, uint Signature, int Length)> NativeCollectionEntries(NativeFile file)
        => Formats.NativeCollectionLayout.Entries(file.Bytes, file.SourcePath);

    private void RequireVerifiedNativeCollections()
    {
        if (IsLegacyProject) return;
        foreach (var file in files.Where(file => Formats.NativeCollectionLayout.IsCollection(file.Signature)))
            _ = NativeCollectionDataEnd(file);
    }

    // Some specialised editors write typed spans directly. Enforce the same
    // collection contract at the outer transaction boundary before history is
    // published, so a failed check restores the complete original operation.
    private void VerifyNativeCollectionChanges(IReadOnlyDictionary<NativeFile, byte[]> before)
    {
        if (IsLegacyProject) return;
        foreach (var file in files)
        {
            var original = before[file];
            if (original.AsSpan().SequenceEqual(file.Bytes)) continue;
            var oldSignature = BinaryPrimitives.ReadUInt32LittleEndian(original);
            if (Formats.NativeOpaqueFile.IsUnknown(oldSignature) || Formats.NativeOpaqueFile.IsUnknown(file.Signature))
                throw new InvalidDataException("A semantic edit cannot change an unverified native file.");
            if (!Formats.NativeCollectionLayout.IsCollection(oldSignature) && !Formats.NativeCollectionLayout.IsCollection(file.Signature)) continue;
            if (oldSignature != file.Signature) throw new InvalidDataException("A semantic edit cannot change a native collection category.");
            var oldEnd = NativeCollectionDataEnd(new NativeFile(file.SourcePath, file.ExportPath, original));
            var newEnd = NativeCollectionDataEnd(file);
            if (!original.AsSpan(oldEnd).SequenceEqual(file.Bytes.AsSpan(newEnd)))
                throw new InvalidDataException("A semantic edit cannot alter opaque trailing collection data.");
        }
    }

    public void SetEventType(NativeRecord record, LegacyEventSchemas.EventType type) => Edit(type.Name, () =>
    {
        if (IsLegacyProject) { SetLegacyEventType(record, type); return; }
        RequireCollectionRecord(record);
        if (record.File.Signature != 0xD2 || record.Length != 24 || record.File.UInt32(record.Offset) != type.RecordSignature)
            throw new ArgumentException("This event type does not match the record category.");
        type.DefaultPayload.CopyTo(record.File.Bytes, record.Offset + 8);
        record.File.Bytes[record.Offset + 22] = type.Type;
        // A type change introduces new source operands, not the old gate link.
        if (type.Type == 32 && type.RecordSignature == 0xD6)
        {
            var point = NativeDerivedPayload(record);
            SetNativeGateTarget(record, NativeGateBindingRecords().FirstOrDefault(gate => NativeSourceGate(gate) && NativeDerivedPosition(gate, true) == new TilePoint(point.X - 1, point.Y - 1)));
        }
    });

    private static void AppendDefinitionRecords(NativeFile file, List<NativeRecord> records)
    {
        foreach (var frame in ScanNativeDefinitionSections(file).Sections)
        {
            var section=frame.Offset;var signature=frame.Signature;var length=frame.Length;
            var definition = LegacyGlobalSchemas.Find(signature);
            if (definition is not null)
            {
                if(file.UInt32(section+4)!=4 || file.UInt32(section+12)!=signature+1 || file.UInt32(section+16)!=28 || file.UInt32(section+48)!=signature+2)
                {
                    records.Add(new(file,section,length,"Definition."+definition.Group,[]));
                    continue;
                }
                var count = file.UInt32(section + 20);
                if (count == 0 || count > length - 56 || (length - 56) % count != 0 || file.UInt32(section + 52) != length - 56)
                    throw new InvalidDataException($"Unsupported definition layout: {definition.Group} in {file.SourcePath}");
                var recordSize = checked((int)((length - 56) / count));
                if (!definition.SupportsRecordSize(recordSize))
                {
                    records.Add(new(file, section, length, "Definition." + definition.Group, []));
                    continue;
                }
                var verified = definition.SupportsRecordSize(recordSize)
                    && VerifiedDefinitionHeader(file,section,signature,checked((int)count),recordSize)
                    && (signature != 0xAA || VerifiedCreatureCatalogue(file, section, checked((int)count), recordSize));
                var fields = verified ? definition.Fields : [];
                for (var index = 0; index < count; index++)
                    records.Add(new(file, section + 56 + index * recordSize, recordSize, "Definition." + definition.Group, fields));
            }
        }
    }

    public void Undo()
    {
        if (transactionOpen || activePenStroke is not null) throw new InvalidOperationException("Undo cannot run during an edit.");
        if (!CanUndo) return;
        ReplayHistory(backward:true);
    }

    public void Redo()
    {
        if (transactionOpen || activePenStroke is not null) throw new InvalidOperationException("Redo cannot run during an edit.");
        if (!CanRedo) return;
        ReplayHistory(backward:false);
    }

    /// <summary>Creates a NEW export game root with Data/Editor/... paths. Existing paths are never overwritten.</summary>
    public string SaveCopy(string newDirectory, bool temporary = false)
    {
        if (transactionOpen || activePenStroke is not null) throw new InvalidOperationException("Finish the active edit before saving.");
        var destination = Path.GetFullPath(newDirectory);
        if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException("Choose a new, unused export directory.");
        var parent = Path.GetDirectoryName(destination) ?? throw new ArgumentException("Invalid export directory.");
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, ".dk2-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (relativePath, data) in ExportFiles())
            {
                var relative = SafeRelativePath(relativePath);
                if (!paths.Add(relative)) throw new InvalidDataException("Two different native files have the same export path.");
                var target = Path.Combine(staging, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                using var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                if (IsLegacyProject && activityStatistics is not null)
                {
                    // Original counters change after file creation, before the
                    // KSTS write. A later I/O failure retains those effects;
                    // temporary Save restores only the count on disposal.
                    using var statisticsSave = activityStatistics.BeginProjectWrite(temporary);
                    stream.Write(ProjectSavedActivity(data, statisticsSave.Output));
                }
                else stream.Write(data);
                stream.Flush(flushToDisk: true);
            }
            if(IsLegacyProject)
            {
                var projectPath = Path.Combine(staging, MainFile.ExportPath);
                nativePayloadSource?.Publish(projectPath, createOutput: null);
                PublishCreatureLevelModifierSource(projectPath);
            }
            Directory.Move(staging, destination);
        }
        catch
        {
            // Only this generated staging directory is removed; never an existing user directory.
            Directory.Delete(staging, recursive: true);
            throw;
        }
        return Path.Combine(destination, MainFile.ExportPath);
    }

    private IReadOnlyList<(string path, byte[] data)> ExportFiles()
    {
        if (IsLegacyProject) return [(MainFile.ExportPath, SerializeLegacyProject())];
        if (officialDefinitionExtras is not null) return OfficialExportFiles();
        var descriptor = (byte[])MainFile.Bytes.Clone();
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var output = new List<(string, byte[])>();
        foreach (var file in files.Where(file => file != MainFile))
        {
            var path = file.ExportPath;
            if (MainFile.Signature == 0xDC && !path.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !file.Bytes.AsSpan().SequenceEqual(originals[file]))
            {
                var stem = Path.GetFileNameWithoutExtension(MainFile.ExportPath);
                var definition = Path.GetFileNameWithoutExtension(path);
                var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(path)))[..6];
                var leaf = stem[..Math.Min(stem.Length, 14)] + "_" + definition[..Math.Min(definition.Length, 14)] + "_" + hash + ".kld";
                path = Path.Combine("Data", "Editor", "Maps", leaf);
                overrides.Add(file.ExportPath, path);
            }
            output.Add((path, file.Bytes));
        }
        if (MainFile.Signature == 0xDC && overrides.Count != 0)
        {
            var start = checked(56 + (int)MainFile.UInt32(52) + 8);
            var count = (int)(MainFile.UInt32(20) & 0xFFFF);
            for (var index = 0; index < count; index++)
            {
                var offset = start + index * 72 + 8;
                var reference = NativeReference.Read(descriptor.AsSpan(offset,64));
                var oldPath = SafeRelativePath(reference);
                if (!Path.HasExtension(oldPath)) oldPath += ".kwd";
                if (!overrides.TryGetValue(oldPath, out var replacement)) continue;
                NativeReference.Encode(replacement.Replace(Path.DirectorySeparatorChar, '\\')).CopyTo(descriptor,offset);
            }
        }
        output.Add((MainFile.ExportPath, descriptor));
        if (ScenarioNamesExport() is { } names) output.Add(names);
        return output;
    }

    public void Edit(string description, Action action) => EditCore(description, action, null);

    private void EditCore(string description, Action action, Func<EditorDrawingCompletion>? completed)
    {
        ArgumentNullException.ThrowIfNull(description); ArgumentNullException.ThrowIfNull(action);
        if (activePenStroke is not null) throw new InvalidOperationException("Finish the active pen gesture before another edit.");
        if (transactionOpen)
        {
            if (historyDepth == 0) { action(); return; }
            var prior = PrepareHistory(()=>TransactionFiles().ToDictionary(file => file, file => historyBufferCopy(file.Bytes)));
            var children = PushHistoryChildren();
            try
            {
                action();
                if (TransactionFiles().Any(file => !file.Bytes.AsSpan().SequenceEqual(prior[file])))
                    PrepareHistory(()=>{operationChildren.ElementAt(1).Add(Operation(description, children));return true;});
            }
            finally { operationChildren.Pop(); }
            return;
        }
        var crystalOrder = NativeCrystalOrder(); var priorRawCrystalTargets = retainRawCrystalTargets;
        var before = PrepareHistory(()=>TransactionFiles().ToDictionary(file => file, file => historyBufferCopy(file.Bytes)));
        var rootChildren = PushHistoryChildren();
        transactionOpen = true;
        try
        {
            action();
            CompleteNativeCrystalMutation(crystalOrder);
            VerifyNativeCollectionChanges(before);
            var deltas = PrepareHistory(()=>TransactionFiles().Where(file => !file.Bytes.AsSpan().SequenceEqual(before[file]))
                .Select(file => new Delta(file, before[file], historyBufferCopy(file.Bytes))).ToArray());
            var completion = completed?.Invoke() ?? new EditorDrawingCompletion(false, 1);
            if (deltas.Length == 0 && !completion.RetainUndo) return;
            if (completed is not null && !completion.RetainUndo)
                throw new InvalidOperationException("A refused editor drawing must not modify bytes or increment changes.");
            if (deltas.Length == 0 && historyDepth != 0)
                deltas = PrepareHistory(()=>new[]{new Delta(TileFile, before[TileFile], historyBufferCopy(TileFile.Bytes))});
            var afterEditorCount = unchecked(editorChangeCount + completion.ChangeIncrements);
            var afterCount = checked(changeCount + 1);
            var afterRevision=checked(EditRevision+1);
            Change? entry=null;
            if (historyDepth != 0)
            {
                entry = PrepareHistory(()=>new Change(Operation(description, rootChildren), deltas, changeCount, afterCount)
                { BeforeEditorChangeCount = editorChangeCount, AfterEditorChangeCount = afterEditorCount });
            }
            PublishHistoryChange(entry);
            changeCount = afterCount; editorChangeCount = afterEditorCount;
            EditRevision=afterRevision;
        }
        catch
        {
            foreach (var pair in before) pair.Key.Bytes = pair.Value;
            throw;
        }
        finally { operationChildren.Pop(); transactionOpen = false; retainRawCrystalTargets = priorRawCrystalTargets; }
        if(deferCursorNotifications)return;
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Add(NativeFile file)
    {
        if (files.Sum(item => (long)item.Length) + file.Length > 128L * 1024 * 1024)
            throw new InvalidDataException("The native bundle exceeds its safe memory limit.");
        files.Add(file); originals.Add(file, (byte[])file.Bytes.Clone());
    }
    private static void WriteUInt32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private void PutTile(int x, int y, MapTile tile) => BinaryPrimitives.WriteUInt32LittleEndian(TileFile.Bytes.AsSpan(36 + (y * Width + x) * 4), tile.Packed);
    private void CheckPoint(int x, int y) { if (x < 0 || x >= Width || y < 0 || y >= Height) throw new ArgumentOutOfRangeException(nameof(x)); }
    private void CheckTiles()
    {
        if (TileFile.Length < 36 || TileFile.UInt32(12) != 0x65 || TileFile.UInt32(28) != 0x66 || TileFile.UInt32(16) != 8)
            throw new InvalidDataException("Unsupported native tile header.");
        var width = TileFile.UInt32(20); var height = TileFile.UInt32(24);
        // Original Game_ExtractMap bounds cells by actual payload size, not
        // dwMapDefineSize (retail/custom maps may store zero there). Retain
        // opaque trailing bytes while never accepting missing cell data.
        if (width is 0 or > 1024 || height is 0 or > 1024 || 36L + width * (long)height * 4 > TileFile.Length)
            throw new InvalidDataException("Invalid map dimensions or tile data length.");
    }
    private static NativeFile ReadFile(string path, string exportPath)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length < 12 || stream.Length > MaximumFileBytes) throw new InvalidDataException("Native file size is outside safe bounds.");
        var data = new byte[(int)stream.Length]; stream.ReadExactly(data);
        if (BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)) != 4 || BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(8)) > data.Length)
            throw new InvalidDataException($"Unsupported version or corrupt file size: {path}");
        return new(path, exportPath, data);
    }
    private static string SafeRelativePath(string reference)
    {
        return Formats.NativeDescriptorReferences.SafeRelativePath(reference);
    }
    private sealed record Delta(NativeFile File, byte[] Before, byte[] After);
    private sealed record Change(HistoryOperation Operation, IReadOnlyList<Delta> Deltas, long BeforeCount, long AfterCount)
    { public uint BeforeEditorChangeCount { get; init; } public uint AfterEditorChangeCount { get; init; }
      public long ByteCost => Deltas.Sum(delta => (long)delta.Before.Length + delta.After.Length); }
}
