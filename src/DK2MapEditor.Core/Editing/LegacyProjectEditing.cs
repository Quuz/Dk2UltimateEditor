using System.Buffers.Binary;
using DK2MapEditor.Core.Formats;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    public const uint LegacyInfo = 0x464E494B;
    public const uint LegacyPlayers = 0x594C504B;
    public const uint LegacyThings = 0x5347544B;
    public const uint LegacyAreas = 0x4345524B;
    public const uint LegacyBands = 0x48544D4B;
    public const uint LegacyEvents = 0x4752544B;
    public const uint LegacyGlobals = 0x4C42474B;
    private readonly List<NativeFile> projectSections = [];
    private readonly HashSet<NativeFile> optionalProjectSections = [];
    public bool IsLegacyProject => MainFile.Signature is LegacySectionFile.MapFileSignature or LegacySectionFile.AreaFileSignature;

    private static NativeMapDocument LoadLegacyProject(string path,Action<EditorFileProgress>? progress=null)
    {
        var document=FromLegacyProject(path,LegacySectionFile.Read(Path.GetFullPath(path)),progress);
        progress?.Invoke(new(EditorFilePhase.Definitions,path));
        document.BindNativeSourceArchives(EditorSourceArchive.Discover(Path.GetFullPath(path)));
        return document;
    }

    private static NativeMapDocument FromLegacyProject(string path, LegacySectionFile legacy,Action<EditorFileProgress>? progress=null, bool initializeActivity=true)
    {
        var fullPath = Path.GetFullPath(path);
        if (legacy.Signature == LegacySectionFile.AreaFileSignature && legacy.Sections.Any(section => section.Signature == SelectionFile.ClipboardSection))
            throw new InvalidDataException("This enhanced DKZ must be loaded with Edit > Load DKZ into clipboard. Opening it as a legacy project would make its exact records and scripts stale.");
        progress?.Invoke(new(EditorFilePhase.Map,fullPath));var tiles = legacy.ReadTiles();
        var document = new NativeMapDocument();
        document.MainFile = new(fullPath, Path.GetFileName(fullPath), legacy.ToArray()); document.Add(document.MainFile);
        var bytes = new byte[36 + tiles.Tiles.Count * 4];
        WriteUInt32(bytes, 0, 0x64); WriteUInt32(bytes, 4, 4); WriteUInt32(bytes, 8, (uint)bytes.Length);
        WriteUInt32(bytes, 12, 0x65); WriteUInt32(bytes, 16, 8); WriteUInt32(bytes, 20, (uint)tiles.Width); WriteUInt32(bytes, 24, (uint)tiles.Height);
        WriteUInt32(bytes, 28, 0x66); WriteUInt32(bytes, 32, (uint)bytes.Length - 36);
        for (var index = 0; index < tiles.Tiles.Count; index++) WriteUInt32(bytes, 36 + index * 4, tiles.Tiles[index].Packed);
        document.TileFile = new(fullPath + "#KMAP", "KMAP", bytes); document.Add(document.TileFile);
        var completed=0;
        foreach (var section in legacy.Sections)
        {
            progress?.Invoke(new(FilePhase(section.Signature),fullPath,completed++,legacy.Sections.Count));
            var sectionBytes = new byte[12 + section.Payload.Length];
            WriteUInt32(sectionBytes, 0, section.Signature); WriteUInt32(sectionBytes, 4, (uint)section.Version); WriteUInt32(sectionBytes, 8, (uint)section.Payload.Length);
            section.Payload.Span.CopyTo(sectionBytes.AsSpan(12));
            if(section.Signature==LevelModifierDeltaSignature)
            {document.LoadLevelModifierDelta(sectionBytes);continue;}
            if(section.Signature==LevelModifierOwnedSignature)
            {document.LoadLevelModifierOwned(sectionBytes);continue;}
            var file = new NativeFile(fullPath + $"#0x{section.Signature:X}", $"section-{document.projectSections.Count}", sectionBytes);
            document.projectSections.Add(file); document.Add(file);
        }
        foreach (var (signature, version) in new (uint, uint)[] { (LegacyThings, 6), (LegacyAreas, 1), (LegacyBands, 5), (LegacyEvents, 3), (LegacyGlobals, 1) })
        {
            if (document.projectSections.Any(file => file.Signature == signature)) continue;
            var empty = new byte[signature == LegacyEvents ? 24 + 146 * 64 : signature == LegacyGlobals ? 16 : 12];
            WriteUInt32(empty, 0, signature); WriteUInt32(empty, 4, version); WriteUInt32(empty, 8, (uint)empty.Length - 12);
            var file = new NativeFile(fullPath + $"#0x{signature:X}", $"section-{document.projectSections.Count}", empty);
            document.Add(file); document.projectSections.Add(file); document.optionalProjectSections.Add(file);
        }
        progress?.Invoke(new(EditorFilePhase.Validate,fullPath));_ = document.LegacyRecords();
        if(initializeActivity) document.InitializeActivityStatistics();
        return document;
    }

    // Reuse precisely the opening contract, including private bounded sections
    // and the separate enhanced-DKZ route, without activity timers or archive
    // discovery. Preview never starts an editable session or writes files.
    internal static (int Width, int Height, IReadOnlyList<Diagnostics.MapDiagnostic> Diagnostics) InspectLegacyProject(string path, LegacySectionFile legacy)
    {
        var document = FromLegacyProject(path, legacy, initializeActivity: false);
        var diagnostics = document.LegacyRecords().Where(record => record.Category == "Project.Opaque")
            .Select(record => new Diagnostics.MapDiagnostic(Diagnostics.DiagnosticSeverity.Warning, "DK2N008",
                "This definition variant is preserved but has no verified editing schema.", record.File.SourcePath)).ToArray();
        return (document.Width, document.Height, diagnostics);
    }

    public static NativeMapDocument ImportTextMap(string path)=>ImportTextMap(path,null);
    private static NativeMapDocument ImportTextMap(string path,Action<EditorFileProgress>? progress)
    {
        var document = FromLegacyProject(Path.ChangeExtension(path, ".dkm"), TextMapFile.Read(path),progress);
        // An imported text map is a new, unsaved project, not a binary source file.
        document.originals[document.MainFile] = [];
        return document;
    }

    private byte[] SerializeLegacyProject()
    {
        _=EffectiveRetainedLevelVariables();
        var levelDelta=ReadLevelModifierDelta().Count!=0?LevelModifierDeltaFile.Data.ToArray():[];
        var levelOwned=ReadOwnedLevelModifiers().Count!=0?LevelModifierOwnedFile.Data.ToArray():[];
        var sections = projectSections.Where(section => !optionalProjectSections.Contains(section) || !section.Bytes.AsSpan().SequenceEqual(originals[section])).ToArray();
        var length = 12L + sections.Sum(section => (long)section.Length)+levelDelta.Length+levelOwned.Length;
        // KMAP is regenerated from the editable native canvas buffer.
        var mapSection = projectSections.Single(section => section.Signature == LegacySectionFile.TileSectionSignature);
        length += 20L + Width * (long)Height * 4 - mapSection.Length;
        if (length > MaximumFileBytes) throw new InvalidDataException("Legacy project exceeds its safe size limit.");
        var result = new byte[(int)length]; MainFile.Bytes.AsSpan(0, 12).CopyTo(result); WriteUInt32(result, 8, (uint)length - 12);
        var offset = 12;
        foreach (var section in sections)
        {
            if (section == mapSection)
            {
                section.Bytes.AsSpan(0, 12).CopyTo(result.AsSpan(offset)); WriteUInt32(result, offset + 8, (uint)(8 + Width * Height * 4));
                WriteUInt32(result, offset + 12, (uint)Width); WriteUInt32(result, offset + 16, (uint)Height);
                TileFile.Bytes.AsSpan(36).CopyTo(result.AsSpan(offset + 20)); offset += 20 + Width * Height * 4;
            }
            else { section.Bytes.CopyTo(result, offset); offset += section.Length; }
        }
        levelDelta.CopyTo(result,offset);
        levelOwned.CopyTo(result,offset+levelDelta.Length);
        // Validate the projection before publishing the new copy.
        _ = LegacySectionFile.Parse(result);
        return result;
    }

    private IReadOnlyList<NativeRecord> LegacyRecords()
    {
        var records = new List<NativeRecord>();
        foreach (var file in projectSections)
        {
            var version = (int)file.UInt32(4);
            var structure = file.Signature switch
            {
                LegacyInfo => version switch { 1 => "MAPPROPERTIESV1", 2 or 3 => "MAPPROPERTIESV2", 4 => "MAPPROPERTIESV4", 5 => "MAPPROPERTIESV5", 6 => "MAPPROPERTIESV6", 7 => "MAPPROPERTIES", _ => "" },
                LegacyPlayers => version switch { 1 => "MAPOPTIONSV1", 2 => "MAPOPTIONSV2", 3 => "MAPOPTIONS", _ => "" },
                LegacyThings => version switch { 1 => "MAPTHINGV1", 2 or 3 => "MAPTHINGV2", 4 => "MAPTHINGV4", 5 => "MAPTHINGV5", 6 => "MAPTHING", _ => "" },
                LegacyAreas => version == 1 ? "MAPRECTEX" : "",
                LegacyBands => version switch { 1 => "MTHBANDV1", 2 => "MTHBANDV2", 3 => "MTHBANDV3", 4 => "MTHBANDV4", 5 => "MTHBAND", _ => "" },
                _ => ""
            };
            if (structure.Length != 0)
            {
                var size = LegacyProjectSchemas.Size(structure);
                if ((file.Length - 12) % size != 0 || (file.Signature == LegacyInfo && file.Length - 12 != size) || (file.Signature == LegacyPlayers && (file.Length - 12) / size != 7))
                    throw new InvalidDataException($"Invalid {structure} section size.");
                for (var offset = 12; offset <= file.Length - size; offset += size)
                    records.Add(new(file, offset, size, "Project." + structure, LegacyProjectSchemas.Fields(structure).Where(field => file.Signature != LegacyInfo || field.Name is not ("Width" or "Height")).Where(field => !field.Name.StartsWith("heros[16]", StringComparison.Ordinal)).ToArray()));
            }
            else if (file.Signature == LegacyEvents && version is 2 or 3)
            {
                if (file.Length < 24) throw new InvalidDataException("Truncated KTRG counts.");
                var offset = 24;
                foreach (var (name, countOffset) in new[] { (version == 2 ? "TRIGGERLINKV2" : "TRIGGERLINK", 12), ("TRIGGER", 16), ("ACTION", 20) })
                {
                    var size = LegacyProjectSchemas.Size(name); var count = file.UInt32(countOffset);
                    if (count > (file.Length - offset) / size) throw new InvalidDataException("Invalid KTRG record count.");
                    for (var index = 0; index < count; index++, offset += size)
                        records.Add(new(file, offset, size, "Project." + name, LegacyProjectSchemas.Fields(name)));
                }
                if (file.Length - offset != 146 * 64) throw new InvalidDataException("Invalid KTRG variable/timer alias table.");
                for (var index = 0; index < 146; index++, offset += 64)
                    records.Add(new(file, offset, 64, index < 129 ? "Project.VariableNames" : "Project.TimerNames", [new("Name", 0, 64, NativeValueKind.WesternAnsi)]));
            }
            else if (file.Signature == LegacyGlobals && version == 1)
            {
                if (file.Length < 16) throw new InvalidDataException("Truncated KGBL flags.");
                var flags = file.UInt32(12); var offset = 16;
                if ((flags & ~0x1FF1Fu) != 0)
                { records.Add(new(file, 12, file.Length - 12, "Project.Opaque", [])); continue; }
                foreach (var (mask, name, count) in new (uint, string, int)[] { (1, "GLOBALTERRAIN", 38), (2, "GLOBALROOM", 24), (4, "GLOBALDOOR", 8), (8, "GLOBALTRAP", 15), (16, "GLOBALSPELL", 13), (0xFF00, "GLOBALCREATURE", 46), (0x10000, "GLOBALCRSPELLS", 41) })
                {
                    if ((flags & mask) == 0) continue;
                    var size = LegacyProjectSchemas.Size(name);
                    if ((long)size * count > file.Length - offset) throw new InvalidDataException("Truncated KGBL definition array.");
                    for (var index = 0; index < count; index++, offset += size)
                        records.Add(new(file, offset, size, "Project." + name, LegacyProjectSchemas.Fields(name)));
                }
                if (offset != file.Length) throw new InvalidDataException("Unknown KGBL extension or flags: editing is not enabled.");
            }
            else if (file.Signature != LegacySectionFile.TileSectionSignature)
                records.Add(new(file, 12, file.Length - 12, "Project.Opaque", []));
        }
        return records;
    }

    public IReadOnlyList<string> LegacyAddChoices() => projectSections.SelectMany(file => file.Signature switch
    {
        LegacyThings => new[] { "Creature", "Door", "Trap", "Object", "MagicalObject" },
        LegacyAreas => new[] { "Area", "Gate", "Effect" },
        LegacyBands => new[] { "Band" },
        LegacyEvents when file.UInt32(4) is 2 or 3 => new[] { "TriggerLink", "Trigger", "Action" },
        _ => Array.Empty<string>()
    }).ToArray();

    public NativeRecord AddLegacyRecord(string kind, TilePoint position, byte owner = 3)
        => AddLegacyRecordCore(kind,position,owner,false);

    /// <summary>An object inserted by the editor gets its source scenario
    /// identity only when an Action Point is attached.</summary>
    public NativeRecord AddEditorLegacyRecord(string kind,TilePoint position,byte owner=3)
        => AddLegacyRecordCore(kind,position,owner,true);

    private NativeRecord AddLegacyRecordCore(string kind,TilePoint position,byte owner,bool editorCreation)
    {
        if (!IsLegacyProject || !LegacyAddChoices().Contains(kind)) throw new ArgumentException("Unsupported project record type.");
        CheckPoint(position.X, position.Y);
        var signature = kind switch { "Area" or "Gate" or "Effect" => LegacyAreas, "Band" => LegacyBands, "TriggerLink" or "Trigger" or "Action" => LegacyEvents, _ => LegacyThings };
        var file = projectSections.Single(section => section.Signature == signature);
        var structure = LegacyStructure(file, kind);
        var bytes = new byte[LegacyProjectSchemas.Size(structure)];
        var fields = LegacyProjectSchemas.Fields(structure);
        void Set(string name, string value) { var field = fields.FirstOrDefault(field => field.Name == name); field?.Encode(value).CopyTo(bytes, field.Offset); }
        var type = kind switch { "Creature" or "Area" => 0u, "Door" or "Gate" => 1u, "Trap" or "Effect" => 2u, "Object" => 3u, "MagicalObject" => 4u, "Trigger" => 0xD5u, "Action" => 0xD6u, "TriggerLink" => 0x20u, _ => 0u };
        Set("node.type", type.ToString()); Set("owner", owner.ToString()); Set("health", "100"); Set("x", position.X.ToString()); Set("y", position.Y.ToString());
        Set("rect.x1", position.X.ToString()); Set("rect.x2", position.X.ToString()); Set("rect.y1", position.Y.ToString()); Set("rect.y2", position.Y.ToString());
        Set("id", kind is "Area" or "Gate" or "Effect" or "Band" or "Trigger" or "Action" ? NextLegacyID(kind).ToString() : "1");
        Set("uniqueid",editorCreation?"0":NextLegacyUniqueID().ToString()); Set("dataAll", kind == "Creature" ? "1" : "0");
        Set("herosoptions", "9"); Set("creaturesoptions", "9");
        Set("ctrl", kind == "Trigger" ? "1" : kind == "Action" ? "10" : "0"); Set("repeat", "1"); Set("one", "1");
        if (kind == "Effect") Set("freq", "1");
        if (kind == "Trigger") { Set("param1", "3"); Set("param3", "1"); }
        var offset = 0; Edit("Add project " + kind, () => offset = InsertLegacyRecord(file, structure, bytes));
        return Records().Single(record => record.File == file && record.Offset == offset);
    }

    private static string LegacyStructure(NativeFile file, string kind)
    {
        var version = file.UInt32(4);
        return file.Signature switch
        {
            LegacyThings => version switch { 1 => "MAPTHINGV1", 2 or 3 => "MAPTHINGV2", 4 => "MAPTHINGV4", 5 => "MAPTHINGV5", 6 => "MAPTHING", _ => throw new InvalidDataException("Unsupported things version.") },
            LegacyAreas when version == 1 => "MAPRECTEX",
            LegacyBands => version switch { 1 => "MTHBANDV1", 2 => "MTHBANDV2", 3 => "MTHBANDV3", 4 => "MTHBANDV4", 5 => "MTHBAND", _ => throw new InvalidDataException("Unsupported band version.") },
            LegacyEvents => kind == "TriggerLink" ? version == 2 ? "TRIGGERLINKV2" : "TRIGGERLINK" : kind == "Action" ? "ACTION" : "TRIGGER",
            _ => throw new ArgumentException("Select an editable collection.")
        };
    }

    private int InsertLegacyRecord(NativeFile file, string structure, byte[] record)
    {
        var offset = file.Length;
        if (file.Signature == LegacyEvents)
        {
            var linkSize = LegacyProjectSchemas.Size(file.UInt32(4) == 2 ? "TRIGGERLINKV2" : "TRIGGERLINK");
            offset = checked(24 + (int)file.UInt32(12) * linkSize);
            var countOffset = structure.StartsWith("TRIGGERLINK", StringComparison.Ordinal) ? 12 : structure == "TRIGGER" ? 16 : 20;
            if (countOffset >= 16) offset += checked((int)file.UInt32(16) * 56);
            if (countOffset >= 20) offset += checked((int)file.UInt32(20) * 56);
            WriteUInt32(file.Bytes, countOffset, file.UInt32(countOffset) + 1);
        }
        var bytes = new byte[file.Length + record.Length];
        file.Bytes.AsSpan(0, offset).CopyTo(bytes); record.CopyTo(bytes, offset); file.Bytes.AsSpan(offset).CopyTo(bytes.AsSpan(offset + record.Length));
        WriteUInt32(bytes, 8, (uint)bytes.Length - 12); file.Bytes = bytes;
        return offset;
    }

    private uint NextLegacyUniqueID()
    {
        var used = UsedProjectUniqueIDs();
        uint candidate = 1; while (used.Contains(candidate)) candidate = checked(candidate + 1); return candidate;
    }
    private ushort NextLegacyID(string kind)
    {
        var script = kind is "Trigger" or "Action";
        var signature = script ? LegacyEvents : kind == "Band" ? LegacyBands : LegacyAreas;
        var rectangleType = kind switch { "Area" => "0", "Gate" => "1", "Effect" => "2", _ => "" };
        var used = Records().Where(record => record.File.Signature == signature && (!script || record.Category is "Project.TRIGGER" or "Project.ACTION")
            && (signature != LegacyAreas || record.Read(record.Fields.Single(field => field.Name == "node.type")) == rectangleType))
            .SelectMany(record => record.Fields.Where(field => field.Name == "id").Select(record.Read)).Select(ushort.Parse).ToHashSet();
        if (script) return AllocateLegacyScriptID(kind == "Action", used);
        for (var candidate = 1; candidate <= (script ? ushort.MaxValue : byte.MaxValue); candidate++) if (!used.Contains((ushort)candidate)) return (ushort)candidate;
        throw new InvalidOperationException("No unused project IDs remain.");
    }
    private static ushort AllocateLegacyScriptID(bool action, ISet<ushort> used)
    {
        // Original graph navigation distinguishes conditions (>0) from actions
        // (<0) by the signed-short ID, not merely by the stored record category.
        for (var index = 1; index <= (action ? 32768 : 32767); index++)
        {
            var id = (ushort)(action ? 65536 - index : index);
            if (!used.Contains(id)) return id;
        }
        throw new InvalidOperationException("No unused project IDs remain in this signed event category.");
    }

    private void RequireLegacyCollection(NativeRecord record)
    {
        if (!projectSections.Contains(record.File) || record.File.Signature is not (LegacyThings or LegacyAreas or LegacyBands or LegacyEvents) || record.Category is "Project.VariableNames" or "Project.TimerNames" or "Project.Opaque" || !Records().Any(current => current.File == record.File && current.Offset == record.Offset && current.Length == record.Length))
            throw new ArgumentException("Select a current project collection record.");
    }
    private NativeRecord DuplicateLegacyRecord(NativeRecord record)
    {
        RequireLegacyCollection(record);
        if (record.Category is "Project.TRIGGER" or "Project.ACTION")
        {
            var rootID = ushort.Parse(record.Read(record.Fields.Single(field => field.Name == "id")));
            ushort newID = 0;
            Edit("Duplicate project script graph", () => newID = CloneLegacyScripts([rootID])[rootID]);
            return Records().Single(item => item.Category is "Project.TRIGGER" or "Project.ACTION" && item.Read(item.Fields.Single(field => field.Name == "id")) == newID.ToString());
        }
        var structure = record.Category[8..];
        var bytes = record.File.Bytes.AsSpan(record.Offset, record.Length).ToArray();
        bytes.AsSpan(0, 8).Clear(); // Serialized list pointers are never copied into a new node.
        AssignDuplicateProjectIdentities(record, bytes);
        if (record.File.Signature is LegacyAreas or LegacyBands)
        {
            var kind = record.File.Signature == LegacyBands ? "Band" : record.Read(record.Fields.Single(field => field.Name == "node.type")) switch { "0" => "Area", "1" => "Gate", "2" => "Effect", _ => throw new InvalidDataException("Unverified rectangle kind.") };
            var id = record.Fields.Single(field => field.Name == "id"); id.Encode(NextLegacyID(kind).ToString()).CopyTo(bytes, id.Offset);
        }
        var offset = 0;
        Edit("Duplicate project record", () =>
        {
            if (structure.StartsWith("TRIGGERLINK", StringComparison.Ordinal))
            {
                var root = record.Fields.Single(field => field.Name == "trigger"); var oldID = ushort.Parse(record.Read(root));
                if (oldID != 0) root.Encode(CloneLegacyScripts([oldID])[oldID].ToString()).CopyTo(bytes, root.Offset);
            }
            else CloneOwnedProjectLinks(record, bytes);
            offset = InsertLegacyRecord(record.File, structure, bytes);
        });
        return Records().Single(item => item.File == record.File && item.Offset == offset);
    }

    private Dictionary<ushort, ushort> CloneLegacyScripts(IEnumerable<ushort> roots)
    {
        var source = Records().Where(record => record.Category is "Project.TRIGGER" or "Project.ACTION").ToDictionary(record => ushort.Parse(record.Read(record.Fields.Single(field => field.Name == "id"))));
        var graph = new Dictionary<ushort, NativeRecord>(); var pending = new Queue<ushort>(roots);
        while (pending.TryDequeue(out var id))
        {
            if (id == 0 || graph.ContainsKey(id)) continue;
            if (!source.TryGetValue(id, out var record)) throw new InvalidDataException("Cannot clone dangling project script links.");
            graph.Add(id, record);
            foreach (var field in record.Fields.Where(field => field.Name is "or" or "and" or "next")) pending.Enqueue(ushort.Parse(record.Read(field)));
        }
        var used = source.Keys.ToHashSet(); var mapping = new Dictionary<ushort, ushort>();
        foreach (var id in graph.Keys)
        {
            var candidate = AllocateLegacyScriptID(graph[id].Category == "Project.ACTION", used);
            mapping[id] = candidate; used.Add(candidate);
        }
        var copies = graph.Select(pair => (pair.Key, pair.Value, Data: pair.Value.File.Bytes.AsSpan(pair.Value.Offset, pair.Value.Length).ToArray())).ToArray();
        foreach (var (id, record, data) in copies)
        {
            data.AsSpan(0, 8).Clear(); record.Fields.Single(field => field.Name == "id").Encode(mapping[id].ToString()).CopyTo(data, 48);
            foreach (var field in record.Fields.Where(field => field.Name is "or" or "and" or "next"))
            { var target = ushort.Parse(field.Read(data)); if (mapping.TryGetValue(target, out var newID)) field.Encode(newID.ToString()).CopyTo(data, field.Offset); }
            InsertLegacyRecord(record.File, record.Category[8..], data);
        }
        RefreshProjectBacklinks(mapping.Values.ToArray());
        return mapping;
    }

    private void DeleteLegacyRecord(NativeRecord record, bool preserveScopes = false)
    {
        RequireLegacyCollection(record);
        if (record.Category is "Project.TRIGGER" or "Project.ACTION")
        {
            var id = record.Read(record.Fields.Single(field => field.Name == "id"));
            if (Records().Where(item => item.File != record.File || item.Offset != record.Offset).Any(item => item.File.Signature == LegacyEvents && item.Fields.Any(field => field.Name is "or" or "and" or "next" or "trigger" && item.Read(field) == id)))
                throw new InvalidOperationException("Remove or redirect incoming project script links before deleting this record.");
        }
        Edit("Delete project record", () =>
        {
            foreach (var linked in (preserveScopes ? Enumerable.Empty<NativeRecord>() : OwnedProjectLinks(record).Select(pair => pair.Link)).DistinctBy(link => (link.File, link.Offset)).OrderByDescending(link => link.Offset))
                DeleteLegacyRecord(linked);
            var bytes = new byte[record.File.Length - record.Length]; record.File.Bytes.AsSpan(0, record.Offset).CopyTo(bytes); record.File.Bytes.AsSpan(record.Offset + record.Length).CopyTo(bytes.AsSpan(record.Offset));
            if (record.File.Signature == LegacyEvents)
            { var countOffset = record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal) ? 12 : record.Category == "Project.TRIGGER" ? 16 : 20; WriteUInt32(bytes, countOffset, record.File.UInt32(countOffset) - 1); }
            WriteUInt32(bytes, 8, (uint)bytes.Length - 12); record.File.Bytes = bytes;
        });
    }

    private void ResizeLegacyRecords(int width, int height, int shiftX, int shiftY, bool removeOutside, bool editorCaller)
    {
        var records = LegacyRecords();
        if (records.Any(record => record.Category == "Project.Opaque" && record.File.Signature is LegacyThings or LegacyAreas or LegacyPlayers or LegacyInfo or LegacyEvents))
            throw new InvalidOperationException("This project contains an unsupported section version; resize cannot safely relocate it.");
        var remove = new List<NativeRecord>();
        foreach (var record in records.Where(record => record.File.Signature is LegacyThings or LegacyAreas))
        {
            var coordinates = record.Fields.Where(field => field.Name is "x" or "y" or "rect.x1" or "rect.x2" or "rect.y1" or "rect.y2").ToDictionary(field => field, field => long.Parse(record.Read(field)) + (field.Name.Contains('x') ? shiftX : shiftY));
            if (coordinates.Any(pair => pair.Value < 0 || pair.Value >= (pair.Key.Name.Contains('x') ? width : height)))
            { if (!removeOutside) throw new InvalidOperationException("Resizing would crop project objects."); remove.Add(record); continue; }
            foreach (var (field, value) in coordinates) field.Encode(value.ToString()).CopyTo(record.File.Bytes, record.Offset + field.Offset);
        }
        if (!editorCaller) foreach (var record in records.Where(record => record.File.Signature == LegacyPlayers))
            foreach (var field in record.Fields.Where(field => field.Name.Contains("Camera", StringComparison.Ordinal) || field.Name.Contains("CPUArea", StringComparison.Ordinal) || field.Name is "HeartX" or "HeartY"))
            {
                var value = long.Parse(record.Read(field)); if (value == -1) continue;
                var horizontal = field.Name.Contains('X'); value = Math.Clamp(value + (horizontal ? shiftX : shiftY), 0, (horizontal ? width : height) - 1);
                field.Encode(value.ToString()).CopyTo(record.File.Bytes, record.Offset + field.Offset);
            }
        foreach (var record in remove.OrderByDescending(record => record.Offset))
            if (editorCaller && EditorResizeRect(record)) DeleteEditorResizeRegion(record);
            else if (editorCaller && EditorResizeThing(record)) DeleteEditorResizeThing(record);
            else DeleteLegacyRecord(record);
        foreach (var file in projectSections.Where(file => file.Signature == LegacyInfo)) { WriteUInt32(file.Bytes, 12, (uint)width); WriteUInt32(file.Bytes, 16, (uint)height); }
    }

    private void SetLegacyEventType(NativeRecord record, LegacyEventSchemas.EventType type)
    {
        RequireLegacyCollection(record);
        if ((record.Category == "Project.TRIGGER" ? 0xD5u : record.Category == "Project.ACTION" ? 0xD6u : 0) != type.RecordSignature)
            throw new ArgumentException("Event type does not match this project record.");
        foreach (var field in record.Fields.Where(field => field.Name.StartsWith("param", StringComparison.Ordinal)))
            field.Encode("0").CopyTo(record.File.Bytes, record.Offset + field.Offset);
        foreach (var argument in type.Arguments)
        {
            var field = record.Fields.FirstOrDefault(field => field.Name.Equals(argument.Name, StringComparison.OrdinalIgnoreCase));
            if (field is not null) field.Encode(argument.Read(new byte[8].Concat(type.DefaultPayload).ToArray())).CopyTo(record.File.Bytes, record.Offset + field.Offset);
        }
        record.Fields.Single(field => field.Name == "ctrl").Encode(type.Type.ToString()).CopyTo(record.File.Bytes, record.Offset + 54);
    }
}
