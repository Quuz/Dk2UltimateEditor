using System.Buffers.Binary;
using System.Text;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.Core.Formats;

/// <summary>Original DKZ KMAP/KTGS, with an optional bounded extension retaining exact
/// native/project records and script graphs. The historical editor reads the first two sections.</summary>
public static class SelectionFile
{
    public const uint ClipboardSection = 0x50434B44; // DKCP, versions 1–6
    private const int MaximumBytes = 64 * 1024 * 1024;
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

    public static byte[] Serialize(SelectionClipboard clipboard)
        => Serialize(clipboard, directSelection: false);

    /// <summary>Original Areas_WriteThings(FALSE): every creature, then the
    /// first door/trap/object/special item at each cell. The modern extension
    /// retains every raw object and its graph independently of this projection.</summary>
    public static byte[] SerializeDirectSelection(SelectionClipboard clipboard)
        => Serialize(clipboard, directSelection: true);

    private static List<byte[]> CompatibleThings(SelectionClipboard clipboard, bool directSelection, bool upgradeHistorical = true)
    {
        var things = clipboard.Things.Select((item, index) => ToLegacyThing(item, clipboard.Source, (uint)index + 1, upgradeHistorical)).ToList();
        if (!directSelection) return things;
        uint Type(byte[] item) => BinaryPrimitives.ReadUInt32LittleEndian(item.AsSpan(8));
        int X(byte[] item) => BinaryPrimitives.ReadInt32LittleEndian(item.AsSpan(12));
        int Y(byte[] item) => BinaryPrimitives.ReadInt32LittleEndian(item.AsSpan(16));
        var seen = new HashSet<(uint Type, int X, int Y)>();
        return things.OrderBy(Y).ThenBy(X).ThenBy(Type)
            .Where(item => Type(item) == 0 || seen.Add((Type(item), X(item), Y(item)))).ToList();
    }

    private static byte[] Serialize(SelectionClipboard clipboard, bool directSelection)
    {
        var tiles = new byte[8 + clipboard.Tiles.Tiles.Count * 4];
        BinaryPrimitives.WriteInt32LittleEndian(tiles, clipboard.Tiles.Width);
        BinaryPrimitives.WriteInt32LittleEndian(tiles.AsSpan(4), clipboard.Tiles.Height);
        for (var index = 0; index < clipboard.Tiles.Tiles.Count; index++)
            BinaryPrimitives.WriteUInt32LittleEndian(tiles.AsSpan(8 + index * 4), (clipboard.ProtectedTiles[index] ? new MapTile(30, 2, 1) : NativeMapDocument.SelectionCompatibleTile(clipboard.Tiles.Tiles[index])).Packed);
        var projected = CompatibleThings(clipboard, directSelection);
        var things = new byte[projected.Count * 64];
        for (var index = 0; index < projected.Count; index++) projected[index].CopyTo(things, index * 64);
        using var extension = new MemoryStream();
        using (var writer = new BinaryWriter(extension, Utf8, true))
        {
            writer.Write(clipboard.Legacy ? 1 : 0);
            writer.Write(clipboard.Source.X); writer.Write(clipboard.Source.Y);
            writer.Write(clipboard.Source.Width); writer.Write(clipboard.Source.Height);
            writer.Write(clipboard.ObjectCount); writer.Write(clipboard.ScriptCount);
            foreach (var item in clipboard.Things.Concat(clipboard.Scripts))
            {
                var category = Utf8.GetBytes(item.Category);
                writer.Write(item.FileSignature); writer.Write(category.Length); writer.Write(item.Data.Length);
                writer.Write(category); writer.Write(item.Data);
            }
            // v2 retains local/external coordinate provenance across rectangle
            // rotations: numeric bounds alone cannot disambiguate external targets.
            foreach (var item in clipboard.Things.Concat(clipboard.Scripts)) writer.Write(item.LocalPoint is null ? (byte)0 : item.LocalPoint == true ? (byte)2 : (byte)1);
            // v3 retains the transient editor mask separately from native padding.
            // Historical KMAP receives canonical boundary terrain; original raw
            // bytes of masked cells stay in the extension for exact snapshots.
            foreach (var value in clipboard.ProtectedTiles) writer.Write(value ? (byte)1 : (byte)0);
            for (var index = 0; index < clipboard.ProtectedTiles.Count; index++) if (clipboard.ProtectedTiles[index]) writer.Write(clipboard.Tiles.Tiles[index].Packed);
            // v5 preserves every raw cell, independently of historical KMAP
            // normalization. v3 protected-byte prefix remains unchanged.
            foreach(var tile in clipboard.Tiles.Tiles)writer.Write(tile.Packed);
            // v6 marks the direct-selection projection. Counts above still
            // describe all raw objects; earlier buffer archives remain v5.
            if (directSelection) writer.Write(1);
        }
        // v4 uses the verified original-reader upgrade for historical KTGS in
        // the compatible projection; the extension retains each original record.
        var sections = new List<LegacySection> { new(LegacySectionFile.TileSectionSignature, 1, tiles) };
        // The source stops without KTGS when nothing follows KMAP. Our raw
        // extension follows, so retain an empty supported KTGS first: the old
        // reader would reject DKCP as the next header. Size names written bytes.
        sections.Add(new(NativeMapDocument.LegacyThings, 6, things));
        sections.Add(new(ClipboardSection, directSelection ? 6 : 5, extension.ToArray()));
        var length = 12L + sections.Sum(section => 12L + section.Payload.Length);
        if (length > MaximumBytes) throw new InvalidDataException("Selection exceeds its safe size limit.");
        var result = new byte[(int)length];
        BinaryPrimitives.WriteUInt32LittleEndian(result, LegacySectionFile.AreaFileSignature);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(4), 1);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), result.Length - 12);
        var offset = 12;
        foreach (var section in sections)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), section.Signature);
            BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(offset + 4), section.Version);
            BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(offset + 8), section.Payload.Length);
            section.Payload.Span.CopyTo(result.AsSpan(offset + 12)); offset += section.Payload.Length + 12;
        }
        _ = Parse(result); // Validate the export before it can be written.
        return result;
    }

    public static SelectionClipboard Read(string path) => Parse(ReadBytes(path));

    public enum ImportRecovery { None, HistoricalData, TerrainOnly }

    /// <summary>A fully read file candidate. Recovery requires explicit UI
    /// consent; valid original terrain/things survive damaged extended data.</summary>
    public sealed class ImportCandidate
    {
        private readonly string path;
        private readonly byte[] original;
        internal ImportCandidate(string path, byte[] original, SelectionClipboard selection, ImportRecovery recovery)
        { this.path=path;this.original=original;Selection=selection;Recovery=recovery; }
        public SelectionClipboard Selection { get; }
        public ImportRecovery Recovery { get; }
        public bool RequiresRecoveryConsent => Recovery != ImportRecovery.None;
        public bool SourceIsUnchanged()
        {
            try { return original.AsSpan().SequenceEqual(ReadBytes(path)); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException) { return false; }
        }
    }

    public static ImportCandidate ReadForImport(string path)
    {
        var fullPath=Path.GetFullPath(path);var bytes=ReadBytes(fullPath);
        // Original Areas_ExtractMap validates AREA1 and the FIRST KMAP1
        // independently of any malformed following thing header or payload.
        var terrain=ReadFirstTerrain(bytes);
        try { return new(fullPath,bytes,Parse(bytes),ImportRecovery.None); }
        catch (InvalidDataException)
        {
            // Original Areas_ExtractThings reads the first KTGS and ignores
            // later editor extensions. Keep those valid objects when only the
            // extended data fails; expose the loss before any UI mutation.
            try { return new(fullPath,bytes,ReadCompatibleThings(bytes),ImportRecovery.HistoricalData); }
            catch (InvalidDataException) { return new(fullPath,bytes,terrain,ImportRecovery.TerrainOnly); }
        }
    }

    private static byte[] ReadBytes(string path)
    {
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(stream.Length>MaximumBytes)throw new InvalidDataException("Selection exceeds its safe size limit.");
        var bytes=new byte[(int)stream.Length];stream.ReadExactly(bytes);return bytes;
    }

    private static SelectionClipboard ReadFirstTerrain(ReadOnlySpan<byte> bytes)
    {
        if(bytes.Length<32||BinaryPrimitives.ReadUInt32LittleEndian(bytes)!=LegacySectionFile.AreaFileSignature
            ||BinaryPrimitives.ReadInt32LittleEndian(bytes[4..])!=1
            ||BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..])!=bytes.Length-12)
            throw new InvalidDataException("Invalid selection AREA header or size.");
        if(BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..])!=LegacySectionFile.TileSectionSignature
            ||BinaryPrimitives.ReadInt32LittleEndian(bytes[16..])!=1)
            throw new InvalidDataException("The first selection section must be KMAP version 1.");
        var size=BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..]);
        if(size<8||size>bytes.Length-24)throw new InvalidDataException("Invalid first selection tile payload size.");
        var tiles=LegacySectionFile.Create([new(LegacySectionFile.TileSectionSignature,1,bytes.Slice(24,(int)size).ToArray())]).ReadTiles();
        return new(tiles,new(0,0,tiles.Width,tiles.Height),true,[],[]);
    }

    private static SelectionClipboard ReadCompatibleThings(ReadOnlySpan<byte> bytes)
    {
        var start=24+(int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..]);
        if(start>bytes.Length-12||BinaryPrimitives.ReadUInt32LittleEndian(bytes[start..])!=NativeMapDocument.LegacyThings)
            throw new InvalidDataException("The original selection thing header is unavailable.");
        var size=BinaryPrimitives.ReadUInt32LittleEndian(bytes[(start+8)..]);
        if(size>bytes.Length-start-12)throw new InvalidDataException("The original selection thing payload is incomplete.");
        var original=bytes[..(start+12+(int)size)].ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(original.AsSpan(8),(uint)(original.Length-12));
        return Parse(original);
    }

    public static SelectionClipboard Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("Selection exceeds its safe size limit.");
        var file = LegacySectionFile.Parse(bytes);
        if (file.Signature != LegacySectionFile.AreaFileSignature) throw new InvalidDataException("A DKZ selection file is required.");
        if (file.Sections.Any(section => section.Signature is not (LegacySectionFile.TileSectionSignature or NativeMapDocument.LegacyThings or ClipboardSection)) || file.Sections.GroupBy(section => section.Signature).Any(group => group.Count() > 1))
            throw new InvalidDataException("Unknown or repeated selection sections cannot be imported without data loss.");
        var tiles = file.ReadTiles();
        var enhanced = file.Sections.SingleOrDefault(section => section.Signature == ClipboardSection);
        if (enhanced is null)
        {
            var things = new List<SelectionClipboard.Payload>();
            var section = file.Sections.SingleOrDefault(section => section.Signature == NativeMapDocument.LegacyThings);
            if (section is not null)
            {
                var structure = section.Version switch { 1 => "MAPTHINGV1", 2 or 3 => "MAPTHINGV2", 4 => "MAPTHINGV4", 5 => "MAPTHINGV5", 6 => "MAPTHING", _ => throw new InvalidDataException("Unsupported DKZ thing version.") };
                var size = LegacyProjectSchemas.Size(structure);
                if (section.Payload.Length % size != 0) throw new InvalidDataException("Truncated DKZ things.");
                for (var offset = 0; offset < section.Payload.Length; offset += size)
                    things.Add(new(NativeMapDocument.LegacyThings, "Project." + structure, section.Payload.Slice(offset, size).ToArray(), LegacyProjectSchemas.Fields(structure), 0));
            }
            var clipboard = new SelectionClipboard(tiles, new(0, 0, tiles.Width, tiles.Height), true, things, []);
            Validate(clipboard); return clipboard;
        }
        if (enhanced.Version is not (1 or 2 or 3 or 4 or 5 or 6)) throw new InvalidDataException("Unsupported enhanced selection version.");
        try
        {
            using var stream = new MemoryStream(enhanced.Payload.ToArray(), false);
            using var reader = new BinaryReader(stream, Utf8);
            var format = reader.ReadInt32();
            if (format is not (0 or 1)) throw new InvalidDataException("Invalid selection format.");
            var source = new TileRect(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            if (source.X is < 0 or > 1024 || source.Y is < 0 or > 1024 || source.Width != tiles.Width || source.Height != tiles.Height)
                throw new InvalidDataException("Invalid selection source bounds.");
            var thingCount = reader.ReadInt32(); var scriptCount = reader.ReadInt32();
            if (thingCount < 0 || scriptCount < 0 || (long)thingCount + scriptCount > stream.Length / 12) throw new InvalidDataException("Invalid selection record counts.");
            var things = new List<SelectionClipboard.Payload>(); var scripts = new List<SelectionClipboard.Payload>();
            for (var index = 0; index < (long)thingCount + scriptCount; index++)
            {
                var signature = reader.ReadUInt32(); var nameLength = reader.ReadInt32(); var dataLength = reader.ReadInt32();
                if (nameLength is < 1 or > 128 || dataLength is < 1 or > 1024 || (long)nameLength + dataLength > stream.Length - stream.Position)
                    throw new InvalidDataException("Invalid selection record bounds.");
                var category = Utf8.GetString(reader.ReadBytes(nameLength)); var data = reader.ReadBytes(dataLength);
                var fields = PayloadFields(signature, category, data, format == 1, index >= thingCount);
                var item = new SelectionClipboard.Payload(signature, category, data, fields, 0);
                (index < thingCount ? things : scripts).Add(item);
            }
            if (enhanced.Version >= 2)
            {
                foreach (var list in new[] { things, scripts }) for (var index = 0; index < list.Count; index++)
                {
                    var flag = reader.ReadByte(); var hasPoint = NativeMapDocument.ClipboardPointFields(list[index]) is not null;
                    if (flag > 2 || (flag == 0) == hasPoint) throw new InvalidDataException("Invalid selection coordinate provenance.");
                    list[index] = list[index] with { LocalPoint = flag == 0 ? null : flag == 2 };
                }
            }
            bool[]? mask = null;
            var historicalTiles=tiles;
            if (enhanced.Version >= 3)
            {
                mask = new bool[tiles.Tiles.Count]; var raw = tiles.Tiles.ToArray();
                for (var index = 0; index < mask.Length; index++)
                {
                    var value = reader.ReadByte();
                    if (value > 1) throw new InvalidDataException("Invalid selection protection flag.");
                    mask[index] = value == 1;
                }
                for (var index = 0; index < mask.Length; index++) if (mask[index])
                {
                    if (tiles.Tiles[index] != new MapTile(30, 2, 1)) throw new InvalidDataException("Protected selection tiles disagree with their historical projection.");
                    raw[index] = MapTile.FromPacked(reader.ReadUInt32());
                }
                tiles = new(tiles.Width, tiles.Height, raw);
            }
            if(enhanced.Version>=5)
            {
                var raw=new MapTile[tiles.Tiles.Count];
                for(var index=0;index<raw.Length;index++)
                {
                    raw[index]=MapTile.FromPacked(reader.ReadUInt32());
                    var projected=mask![index]?new MapTile(30,2,1):NativeMapDocument.SelectionCompatibleTile(raw[index]);
                    if(historicalTiles.Tiles[index]!=projected||mask[index]&&tiles.Tiles[index]!=raw[index])
                        throw new InvalidDataException("Raw selection cells disagree with their historical projection.");
                }
                tiles=new(tiles.Width,tiles.Height,raw);
            }
            var directSelection = enhanced.Version == 6;
            if (directSelection && reader.ReadInt32() != 1) throw new InvalidDataException("Unknown direct selection projection mode.");
            if (stream.Position != stream.Length) throw new InvalidDataException("Unknown selection extension tail.");
            var clipboard = new SelectionClipboard(tiles, source, format == 1, things, scripts, mask);
            Validate(clipboard);
            var compatibleThings = file.Sections.SingleOrDefault(section => section.Signature == NativeMapDocument.LegacyThings);
            var expectedThings = CompatibleThings(clipboard, directSelection, upgradeHistorical: enhanced.Version >= 4);
            if (compatibleThings is null || compatibleThings.Version != 6 || compatibleThings.Payload.Length != expectedThings.Count * 64)
                throw new InvalidDataException("Enhanced and legacy selection things disagree.");
            for (var index = 0; index < expectedThings.Count; index++)
                if (!compatibleThings.Payload.Span.Slice(index * 64, 64).SequenceEqual(expectedThings[index]))
                    throw new InvalidDataException("Enhanced selection records no longer match their legacy representation.");
            return clipboard;
        }
        catch (Exception exception) when (exception is EndOfStreamException or DecoderFallbackException or KeyNotFoundException or ArgumentException)
        { throw new InvalidDataException("Invalid enhanced selection payload.", exception); }
    }

    public static SelectionClipboard ConvertToFormat(SelectionClipboard clipboard, bool legacy)
    {
        if (clipboard.Legacy == legacy) return clipboard;
        if (clipboard.ScriptCount != 0) throw new InvalidOperationException("Selections with scripts require the same map format. Compile the full DKM project before copying into a native map.");
        if (legacy)
        {
            var things = clipboard.Things.Select((item, index) => new SelectionClipboard.Payload(NativeMapDocument.LegacyThings, "Project.MAPTHING", ToLegacyThing(item, clipboard.Source, (uint)index + 1), LegacyProjectSchemas.Fields("MAPTHING"), 0)).ToArray();
            return new(clipboard.Tiles, new(0, 0, clipboard.Tiles.Width, clipboard.Tiles.Height), true, things, [], clipboard.ProtectedTiles);
        }
        return new(clipboard.Tiles, clipboard.Source, false, clipboard.Things.Select(ToNativeThing).ToArray(), [], clipboard.ProtectedTiles);
    }

    private static SelectionClipboard.Payload ToNativeThing(SelectionClipboard.Payload item)
    {
        item=NormalizeHistoricalThing(item);
        string Value(string name, string fallback = "0") => item.Fields.FirstOrDefault(field => field.Name == name) is { } field ? field.Read(item.Data) : fallback;
        var owner = byte.Parse(Value("owner", "2"));
        var structure = Value("node.type") switch { "0" => owner == 1 ? "GAMEHERO" : owner == 2 ? "GAMENEUTRAL" : "GAMEKEEPER", "1" => "GAMEDOOR", "2" => "GAMETRAP", "3" or "4" => "GAMEOBJECT", _ => throw new InvalidDataException("Invalid project thing type.") };
        var template = RecordTemplate.All.Single(item => item.Structure == structure);
        var data = template.Create(new(int.Parse(Value("x")), int.Parse(Value("y"))), owner, 1);
        var fields = LegacySchemas.Fields(structure);
        foreach (var field in fields)
        {
            var sourceName = field.Name switch
            {
                "bGlobalCreatureID" or "bObjectID" or "bTrapID" or "bDoorID" => "id", "bGlobalGoldHeld" => "goldheld", "bGlobalLevel" or "bTrapShots" or "bDoorLocked" => "dataAll", "bGlobalBehavior" => "creaturesoptions", "bHeroTargetArea" => "area", "dwGlobalHealth" => "health", "bHeroObjective" => "objective", "bHeroOptions" => "herosoptions", "bGlobalPlayer" when structure == "GAMEHERO" => "ennemy", "dwObjectGoldValue" when Value("node.type") == "3" && Value("id") is "1" or "2" or "3" => "dataAll", "dwObjectSpell" when Value("node.type") == "3" && Value("id") == "4" => "dataAll", _ => ""
            };
            if (sourceName.Length == 0 || item.Fields.All(original => original.Name != sourceName)) continue;
            var number = long.Parse(Value(sourceName));
            var encoded = field.Kind switch { NativeValueKind.Byte => unchecked((byte)number).ToString(), NativeValueKind.UInt16 => unchecked((ushort)number).ToString(), NativeValueKind.UInt32 => unchecked((uint)number).ToString(), _ => unchecked((int)number).ToString() };
            field.Encode(encoded).CopyTo(data, field.Offset);
        }
        return new(0xBE, structure, data, fields, 0);
    }

    private static IReadOnlyList<NativeField> PayloadFields(uint signature, string category, byte[] data, bool legacy, bool script)
    {
        if (legacy)
        {
            var structure = category.StartsWith("Project.", StringComparison.Ordinal) ? category[8..] : "";
            var valid = script ? signature == NativeMapDocument.LegacyEvents && structure is "TRIGGER" or "ACTION" or "TRIGGERLINK" or "TRIGGERLINKV2"
                : signature == NativeMapDocument.LegacyThings && structure is "MAPTHING" or "MAPTHINGV1" or "MAPTHINGV2" or "MAPTHINGV4" or "MAPTHINGV5";
            if (!valid || data.Length != LegacyProjectSchemas.Size(structure)) throw new InvalidDataException("Unverified project selection record.");
            return LegacyProjectSchemas.Fields(structure);
        }
        var recordSignature = BinaryPrimitives.ReadUInt32LittleEndian(data);
        if (script)
        {
            if (signature != 0xD2 || data.Length != 24 || recordSignature is not (0xD5 or 0xD6) || category != $"Record 0x{recordSignature:X}" || BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)) != 16)
                throw new InvalidDataException("Invalid native selection event.");
            return LegacyEventSchemas.Fields(recordSignature, data[22]);
        }
        var template = RecordTemplate.All.SingleOrDefault(item => item.Structure == category && item.FileSignature == 0xBE && item.RecordSignature == recordSignature);
        if (signature != 0xBE || template is null || category is not ("GAMEOBJECT" or "GAMETRAP" or "GAMEDOOR" or "GAMENEUTRAL" or "GAMEHERO" or "GAMEKEEPER") || data.Length != template.Size || BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)) != data.Length - 8)
            throw new InvalidDataException("Invalid native selection thing.");
        return LegacySchemas.Fields(category);
    }

    private static void Validate(SelectionClipboard clipboard)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var script in clipboard.Scripts.Where(item => !item.Category.Contains("TRIGGERLINK", StringComparison.Ordinal)))
        {
            var id = script.Fields.Single(field => field.Name is "id" or "ID").Read(script.Data);
            if (id == "0" || !ids.Add(id)) throw new InvalidDataException("Selection script IDs must be unique and nonzero.");
        }
        foreach (var item in clipboard.Things.Concat(clipboard.Scripts))
        {
            foreach (var field in SelectionClipboard.LinkFields(item))
                if (field.Read(item.Data) is var id && id != "0" && !ids.Contains(id)) throw new InvalidDataException("Selection contains a dangling script reference.");
            if (item.LocalPoint == true && NativeMapDocument.ClipboardPointFields(item) is { } point
                && !clipboard.Source.Contains(int.Parse(point.X.Read(item.Data)) - point.Bias, int.Parse(point.Y.Read(item.Data)) - point.Bias))
                throw new InvalidDataException("A local selection coordinate lies outside the selected rectangle.");
        }
        foreach (var link in clipboard.Scripts.Where(item => item.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)))
            if (!SelectionClipboard.IsOwnedLink(link, clipboard.Things)) throw new InvalidDataException("A selection scenario link has no matching physical owner.");
        foreach (var thing in clipboard.Things)
        {
            var x = thing.Fields.First(field => field.Name == "x" || field.Name.EndsWith("LocationX", StringComparison.Ordinal)).Read(thing.Data);
            var y = thing.Fields.First(field => field.Name == "y" || field.Name.EndsWith("LocationY", StringComparison.Ordinal)).Read(thing.Data);
            if (thing.LocalPoint != true || !clipboard.Source.Contains(int.Parse(x), int.Parse(y))) throw new InvalidDataException("A selection thing lies outside the selected rectangle.");
            if (clipboard.IsProtectedTile(int.Parse(x) - clipboard.Source.X, int.Parse(y) - clipboard.Source.Y)) throw new InvalidDataException("A selection thing lies on a protected source cell.");
        }
    }

    private static SelectionClipboard.Payload NormalizeHistoricalThing(SelectionClipboard.Payload item)
    {
        if(item.FileSignature!=NativeMapDocument.LegacyThings)return item;
        var version=item.Category switch {"Project.MAPTHINGV1"=>1,"Project.MAPTHINGV2"=>2,"Project.MAPTHINGV4"=>4,"Project.MAPTHINGV5"=>5,_=>6};
        return version==6?item:item with {Category="Project.MAPTHING",Data=NativeMapDocument.UpgradeHistoricalThing(item.Data,version),Fields=LegacyProjectSchemas.Fields("MAPTHING")};
    }

    internal static byte[] ToLegacyThing(SelectionClipboard.Payload item, TileRect source, uint uniqueID,bool upgradeHistorical=true)
    {
        if(upgradeHistorical)item=NormalizeHistoricalThing(item);
        var result = new byte[64]; var fields = LegacyProjectSchemas.Fields("MAPTHING");
        void Set(string name, string value) => fields.Single(field => field.Name == name).Encode(value).CopyTo(result, fields.Single(field => field.Name == name).Offset);
        if (item.FileSignature == NativeMapDocument.LegacyThings)
        {
            foreach (var field in fields)
                if (item.Fields.FirstOrDefault(candidate => candidate.Name == field.Name) is { } original) field.Encode(original.Read(item.Data)).CopyTo(result, field.Offset);
        }
        else
        {
            foreach (var field in item.Fields)
            {
                var name = field.Name switch
                {
                    "bGlobalPlayer" => item.Category == "GAMEHERO" ? "ennemy" : "owner", "bGlobalGoldHeld" => "goldheld", "bGlobalLevel" or "bTrapShots" or "bDoorLocked" => "dataAll", "bGlobalBehavior" => "creaturesoptions", "bHeroTargetArea" => "area", "dwGlobalHealth" => "health", "bHeroObjective" => "objective", "bHeroOptions" => "herosoptions", "bGlobalCreatureID" or "bObjectID" or "bDoorID" or "bTrapID" => "id", _ when field.Name.EndsWith("LocationX", StringComparison.Ordinal) => "x", _ when field.Name.EndsWith("LocationY", StringComparison.Ordinal) => "y", _ => ""
                };
                if (name.Length != 0) Set(name, field.Read(item.Data));
            }
            var type = item.Category switch { "GAMEDOOR" => 1, "GAMETRAP" => 2, "GAMEOBJECT" => LegacyCompilerRules.Elements("Magics").Any(entry => entry.ID.ToString() == fields.Single(field => field.Name == "id").Read(result)) ? 4 : 3, _ => 0 };
            Set("node.type", type.ToString()); Set("uniqueid", uniqueID.ToString());
            if (item.Category is "GAMEHERO" or "GAMENEUTRAL") Set("owner", item.Category == "GAMEHERO" ? "1" : "2");
            if (item.Category == "GAMEOBJECT")
            {
                var id = fields.Single(field => field.Name == "id").Read(result);
                var original = item.Fields.FirstOrDefault(field => field.Name == (id == "4" ? "dwObjectSpell" : "dwObjectGoldValue"));
                if (original is not null) Set("dataAll", unchecked((int)uint.Parse(original.Read(item.Data))).ToString());
            }
        }
        result.AsSpan(0, 8).Clear();
        Set("x", (int.Parse(fields.Single(field => field.Name == "x").Read(result)) - source.X).ToString());
        Set("y", (int.Parse(fields.Single(field => field.Name == "y").Read(result)) - source.Y).ToString());
        return result;
    }

    public static void SaveNew(string path, SelectionClipboard clipboard) => Save(path, clipboard, overwrite: false);

    /// <summary>Writes the complete archive after the caller's Save dialog has
    /// accepted this destination, including its existing-file replacement prompt.</summary>
    public static void SaveConfirmed(string path, SelectionClipboard clipboard) => Save(path, clipboard, overwrite: true);

    public static void SaveDirectSelectionConfirmed(string path, SelectionClipboard clipboard)
        => Save(path, clipboard, overwrite: true, directSelection: true);

    private static void Save(string path, SelectionClipboard clipboard, bool overwrite, bool directSelection = false)
    {
        var bytes = Serialize(clipboard, directSelection); var target = Path.GetFullPath(path);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(bytes); stream.Flush(true); }
            File.Move(temporary, target, overwrite);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
