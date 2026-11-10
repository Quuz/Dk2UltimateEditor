using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class MapGeneralSettingsChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException or OverflowException) { return; } throw new InvalidOperationException("Unsafe general setting accepted."); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(bytes.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "General settings changed unexpected game bytes.");
    private static void Raw(NativeFile file, int offset, Action<byte[],int> write)
    { Expect(MemoryMarshal.TryGetArray(file.Data, out var backing), "Fixture backing unavailable."); write(backing.Array!, backing.Offset + offset); }
    internal static IEnumerable<MapGeneralInput> Inputs(MapGeneralDraft draft, string key, string value) => draft.Values.Select(row => new MapGeneralInput(row.Key, row.Key == key ? value : row.Value));
    public static string ReadOnlyProjection(NativeMapDocument doc)
    {
        var bytes = Snapshot(doc); var revision = doc.EditRevision; var dirty = doc.HasChanges; var history = doc.GetHistory(); var draft = doc.CreateMapGeneralDraft();
        var result = JsonSerializer.Serialize(new { draft.Editable, draft.Partial, doc.TargetGameVersion, Values = draft.Values.OrderBy(row => row.Key) });
        Exact(bytes); Expect(revision == doc.EditRevision && dirty == doc.HasChanges && history.Undo.Count == doc.GetHistory().Undo.Count && history.Redo.Count == doc.GetHistory().Redo.Count, "General projection changed history.");
        return result;
    }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2GeneralSettings", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        foreach (var native in new[] { false, true }) { Combinations(native); Guards(native, directory); }
        NativeSession(directory); Historical(directory); MetadataOnlyTarget();
    }
    private static void Combinations(bool native)
    {
        var doc = NativeMapDocument.CreateNewMap(new("General", "General", 32, 30, Players: 5, PlaceHearts: true, NativeFormat: native));
        var initial = Snapshot(doc); var draft = doc.CreateMapGeneralDraft();
        Expect(draft.Values.Count == 3 && !draft.Partial && doc.TargetGameVersion == 17, "Complete general mapping.");
        var record = draft.Record!; var name = record.Fields.Single(field => field.Name == (native ? "wstrMainName" : "Name"));
        var players = record.Fields.Single(field => field.Name == (native ? "bMainPlayers" : "Players"));
        Expect(name.Offset == (native ? 56 : 16) && name.Size == (native ? 128 : 64) && players.Offset == (native ? 25660 : 8) && players.Size == (native ? 1 : 4), "Independent source Name/Players layout mismatch.");
        if (!native) Expect(record.Fields.Single(field => field.Name == "Version") is { Offset: 11661, Size: 1 }, "Independent source target offset mismatch.");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        foreach (var version in new[] { 13, 16, 17 }) foreach (var keeper in Enumerable.Range(1, 5))
        {
            draft = doc.CreateMapGeneralDraft(); var title = native ? "Русская карта / Éditeur" : "Éditeur";
            var expected = initial.ToDictionary(row => row.Key, row => row.Value.ToArray());
            var target = expected[record.File]; var nameStart = record.Offset + (native ? 56 : 16);
            Array.Clear(target, nameStart, native ? 128 : 64); (native ? Encoding.Unicode : Encoding.GetEncoding(1252)).GetBytes(title).CopyTo(target, nameStart);
            if (native) target[25660] = (byte)keeper;
            else { BinaryPrimitives.WriteInt32LittleEndian(target.AsSpan(record.Offset + 8), keeper); target[record.Offset + 11661] = (byte)version; }
            var notifications = 0; EventHandler observer = (_, _) => notifications++; doc.Changed += observer;
            var inputs = draft.Values.Select(row => new MapGeneralInput(row.Key, row.Key switch { "Name" => title, "Players" => keeper.ToString(CultureInfo.InvariantCulture), _ => version.ToString(CultureInfo.InvariantCulture) }));
            doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, inputs)); doc.Changed -= observer;
            Expect(notifications == 1 && doc.GetHistory().Undo.Count == 1 && doc.TargetGameVersion == version && ValueCatalogues.GameVersion(doc) == version, "Whole general edit not one history/notification."); Exact(expected);
            doc.Undo(); Exact(initial); Expect(doc.TargetGameVersion == 17, "Undo did not restore working target."); doc.Redo(); Exact(expected); Expect(doc.TargetGameVersion == version, "Redo did not restore working target."); doc.Undo();
        }
        draft = doc.CreateMapGeneralDraft(); var rev = doc.EditRevision; doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, Inputs(draft, "Players", " +5 ")));
        Exact(initial); Expect(doc.EditRevision == rev && doc.CanRedo, "Canonical no-op destroyed redo.");
        foreach (var title in new[] { "", new string('X', 63) })
        { draft = doc.CreateMapGeneralDraft(); doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, Inputs(draft, "Name", title))); Expect(doc.CreateMapGeneralDraft().Values["Name"] == title, "Blank/63-character source name refused."); doc.Undo(); Exact(initial); }
    }
    private static void Guards(bool native, string directory)
    {
        var doc = NativeMapDocument.CreateNewMap(new("Guards", "Guards", 24, 24, NativeFormat: native)); var draft = doc.CreateMapGeneralDraft(); var initial = Snapshot(doc);
        foreach (var (key, value) in new[] { ("Name", new string('X', 64)), ("Name", "N\0UL"), ("Name", "\uD800"), ("Players", "0"), ("Players", "6"), ("Players", "7"), ("Players", "-1"), ("Players", "2147483648"), ("Version", "14"), ("Version", "256"), ("Version", "bad") })
            Reject(() => doc.UpdateMapGeneralSettings(draft, Inputs(draft, key, value)));
        if (!native) Reject(() => doc.UpdateMapGeneralSettings(draft, Inputs(draft, "Name", "Русская карта")));
        Reject(() => doc.UpdateMapGeneralSettings(draft, [new("Name", "Only")])); Reject(() => doc.UpdateMapGeneralSettings(draft, draft.Values.Select(row => new MapGeneralInput("Name", row.Value)))); Exact(initial);
        var field = draft.Record!.Fields.Single(item => item.Name == (native ? "wstrMainName" : "Name"));
        Raw(draft.Record.File, draft.Record.Offset + field.Offset, (bytes, offset) => { if (native) for (var i = 0; i < 64; i++) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset + i * 2), 'Z'); else Array.Fill(bytes, (byte)'Z', offset, 64); });
        doc.SetField(draft.Record, draft.Record.Fields.Single(item => item.Name == (native ? "bMainPlayers" : "Players")), "7");
        if (!native) doc.SetField(draft.Record, draft.Record.Fields.Single(item => item.Name == "Version"), "250");
        var malformed = Snapshot(doc); draft = doc.CreateMapGeneralDraft(); var rev = doc.EditRevision;
        doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, draft.Values.Select(row => new MapGeneralInput(row.Key, row.Value)))); Exact(malformed); Expect(doc.EditRevision == rev, "No-op normalized imported name/count/target.");
        doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, Inputs(draft, "Players", "2"))); Expect(doc.CreateMapGeneralDraft().Values["Name"] == new string('Z', 64) && (native || doc.TargetGameVersion == 250), "Unrelated edit coerced imported data."); doc.Undo(); Exact(malformed);
        // Preserve nonzero padding after NUL too, not just full-width names.
        Raw(draft.Record!.File, draft.Record.Offset + field.Offset, (bytes, offset) => { bytes[offset] = 0; bytes[offset + 1] = 0; bytes[offset + 8] = 0xE9; }); malformed = Snapshot(doc); draft = doc.CreateMapGeneralDraft();
        doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, Inputs(draft, "Players", "1"))); doc.Undo(); Exact(malformed);
        draft = doc.CreateMapGeneralDraft(); Reject(() => NativeMapDocument.Create(24, 24, "Foreign").ApplyMapGeneralSettings(draft));
        var speech = doc.CreateMapSpeechTextDraft(); var levels = doc.CreateMapLevelOptionsDraft();
        var names = doc.CreateScenarioNameDraft(ScenarioNameKind.Variable, 1);
        doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, Inputs(draft, "Version", "13"))); doc.Undo();
        Reject(() => doc.ApplyMapGeneralSettings(draft)); Reject(() => doc.ApplyMapSpeechText(speech)); Reject(() => doc.ApplyMapLevelOptions(levels)); Reject(() => doc.ApplyScenarioNameDraft(names, "stale")); Exact(malformed);
        draft = doc.CreateMapGeneralDraft(); Raw(doc.TileFile, 36, (bytes, offset) => bytes[offset] ^= 1); Reject(() => doc.ApplyMapGeneralSettings(draft)); Raw(doc.TileFile, 36, (bytes, offset) => bytes[offset] ^= 1);
        draft = doc.CreateMapGeneralDraft(); var prepared = doc.UpdateMapGeneralSettings(draft, Inputs(draft, "Version", "16")); var redo = doc.GetHistory().Redo.Count; var priorVersion = doc.TargetGameVersion;
        Reject(() => doc.Edit("Mixed general rollback", () => { doc.ApplyMapGeneralSettings(prepared); doc.Paint([new(20, 20)], new(4, 2, 1)); throw new InvalidOperationException("rollback"); })); Exact(malformed); Expect(doc.TargetGameVersion == priorVersion && doc.GetHistory().Redo.Count == redo, "Mixed rollback lost session/redo.");
        var saved = SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory, "general-guards-" + native)); Expect(ReadOnlyProjection(doc) == ReadOnlyProjection(NativeMapDocument.Load(saved)), "General save/reopen changed baseline.");
        doc.SetHistoryDepth(0); draft = doc.CreateMapGeneralDraft(); doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, Inputs(draft, "Version", "16"))); Expect(!doc.CanUndo && doc.TargetGameVersion == 16, "Disabled history disabled editing.");
    }
    private static void NativeSession(string directory)
    {
        var factory = NativeMapDocument.Create(24, 24, "Session");
        factory.Paint([new(2, 2)], new(3, 3, 1));
        var doc = NativeMapDocument.Load(SaveProjectionChecks.SaveCopy(factory, Path.Combine(directory, "session-baseline"))); var bytes = Snapshot(doc); Expect(!doc.HasChanges, "Loaded baseline unexpectedly dirty.");
        var draft = doc.CreateMapGeneralDraft(); var versionPlan = doc.UpdateMapGeneralSettings(draft, Inputs(draft, "Version", "13")); var notifications = 0; doc.Changed += (_, _) => notifications++;
        var painting = doc.PlanObjectPainting(ObjectBatchTemplate.Default(PlacedThingKind.Creature), new(2, 2));
        Expect(!painting.Locked && painting.Permission == ThingTerrainPermission.Allowed, "Stale-target fixture must initially be a valid painting plan.");
        doc.ApplyMapGeneralSettings(versionPlan); Exact(bytes);
        Expect(doc.HasChanges && notifications == 1 && doc.GetHistory().SnapshotBytes == 8 && doc.GetHistory().Undo.Single().Children.Single().Description == "Change target game version", "Native session state missing dirty/history or leaked full-file snapshots.");
        Reject(() => doc.ApplyObjectPainting(painting));
        var saved = SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory, "session-native")); var root = Path.Combine(directory, "session-native");
        Expect(!Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Any(path => path.Contains("GameTarget", StringComparison.Ordinal) || path.EndsWith(".dk2names", StringComparison.Ordinal)), "Native working target invented an exported field/companion.");
        var reopened = NativeMapDocument.Load(saved); Expect(reopened.TargetGameVersion == 17 && BinaryPrimitives.ReadUInt32LittleEndian(reopened.MainFile.Data.Span[4..]) == 4 && !reopened.HasChanges, "Native reload must reset working target to 17, keeping KWD version 4.");
        doc.Undo(); Exact(bytes); Expect(!doc.HasChanges && doc.TargetGameVersion == 17, "Session undo baseline not clean."); doc.Redo(); Exact(bytes); Expect(doc.TargetGameVersion == 13, "Session redo missing.");
        foreach (var version in new byte[] { 13, 16, 17 })
        {
            var project = NativeMapDocument.CreateNewMap(new("Compiled", "Compiled", 24, 24, Version: version)); var compiled = project.CompileLegacyProject("Compiled", reopened);
            Expect(compiled.TargetGameVersion == version && BinaryPrimitives.ReadUInt32LittleEndian(compiled.MainFile.Data.Span[4..]) == 4, "Compilation lost working target or altered KWD header.");
            Expect(NativeMapDocument.CreateNewMap(new("NativeNew", "NativeNew", 24, 24, Version: version, NativeFormat: true)).TargetGameVersion == version, "Native New ignored selected working target.");
        }
        Raw(doc.MainFile, 4, (data, offset) => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), 99));
        Expect(!doc.CreateMapGeneralDraft().Editable && doc.TargetGameVersion is null, "Unknown native container inferred editable metadata.");
    }
    private static void MetadataOnlyTarget()
    {
        // A deterministic regression for the former all-record reparse: an
        // unrelated broken collection must not be visited just to read the
        // independently verified game target. Normal validation still rejects
        // the collection; this does not bless its malformed payload.
        foreach (var native in new[] { false, true })
        {
            var doc = NativeMapDocument.CreateNewMap(new("TargetLookup", "TargetLookup", 24, 24, Version: 16, NativeFormat: native));
            var file = doc.Files.Single(item => item.Signature == (native ? 0xBEu : NativeMapDocument.LegacyEvents));
            Raw(file, native ? 20 : 16, (bytes, offset) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), uint.MaxValue));
            Expect(doc.TargetGameVersion == 16 && ValueCatalogues.GameVersion(doc) == 16, "Target lookup reparsed unrelated collections.");
            var rejected = false; try { _ = doc.Records(); } catch (InvalidDataException) { rejected = true; }
            Expect(rejected, "Metadata-only lookup concealed malformed ordinary records.");
        }
    }
    private static void Historical(string directory)
    {
        var source = NativeMapDocument.CreateNewMap(new("Versions", "Versions", 24, 24, Version: 16)); var path = SaveProjectionChecks.SaveCopy(source, Path.Combine(directory, "versions-source")); var file = LegacySectionFile.Read(path); var info = file.Sections.Single(row => row.Signature == NativeMapDocument.LegacyInfo);
        foreach (var version in Enumerable.Range(1, 8))
        {
            var structure = version switch { 1 => "MAPPROPERTIESV1", 2 or 3 => "MAPPROPERTIESV2", 4 => "MAPPROPERTIESV4", 5 => "MAPPROPERTIESV5", 6 => "MAPPROPERTIESV6", _ => "MAPPROPERTIES" };
            var replacement = new LegacySection(info.Signature, version, info.Payload[..LegacyProjectSchemas.Size(structure)]); var bytes = LegacySectionFile.Create(file.Sections.Select(row => row == info ? replacement : row)).ToArray(); var name = Path.Combine(directory, $"general-v{version}.dkm"); File.WriteAllBytes(name, bytes);
            var doc = NativeMapDocument.Load(name); var draft = doc.CreateMapGeneralDraft(); Expect(draft.Values.Count == (version <= 5 ? 2 : version <= 7 ? 3 : 0) && doc.TargetGameVersion == (version <= 5 ? 17 : version <= 7 ? 16 : null), "Historical Version inferred from padding or real field lost.");
            if (draft.Editable) { doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, Inputs(draft, "Name", ""))); doc.Undo(); } else Reject(() => doc.ApplyMapGeneralSettings(draft));
            if (version <= 6) { doc.UpgradeLegacyProject(); Expect(doc.CreateMapGeneralDraft().Values.Count == 3 && doc.TargetGameVersion == (version <= 5 ? 17 : 16), "Explicit upgrade target mismatch."); doc.Undo(); }
            var saved = SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory, $"general-v{version}-exact")); Expect(SaveProjectionChecks.BeforeEquals(doc,bytes), "Historical Undo/save changed original bytes.");
        }
    }
}
