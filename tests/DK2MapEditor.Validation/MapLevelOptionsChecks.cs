using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class MapLevelOptionsChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject(Action action, string message)
    { var refused = false; try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or OverflowException) { refused = true; } Expect(refused, message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument doc, Dictionary<NativeFile, byte[]> bytes) => Expect(bytes.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Level editing changed unrelated bytes or Undo was not exact.");
    private static readonly string[] nativeNames = ["bMainOptions", "bMainOptions2", "bMainHornyTalismanPieces", "bMainGameTicks", "bMainSoundtrack", "bMainSpecialLevelNumber", "bMainRewardPrev1", "bMainRewardPrev2", "bMainRewardPrev3", "bMainRewardNext1", "bMainRewardNext2", "bMainRewardNext3"];
    private static NativeField Field(NativeMapDocument doc, MapLevelOptionsDraft draft, string key) => draft.Record!.Fields.Single(field => field.Name == (doc.IsLegacyProject ? key : nativeNames[MapLevelOptions.Fields.ToList().IndexOf(key)]));
    private static IEnumerable<MapLevelInput> Inputs(MapLevelOptionsDraft draft, string key, string value) => draft.Values.Select(row => new MapLevelInput(row.Key, row.Key == key ? value : row.Value));
    public static string ReadOnlyProjection(NativeMapDocument doc)
    {
        var bytes = Snapshot(doc); var revision = doc.EditRevision; var history = doc.GetHistory(); var dirty = doc.HasChanges;
        var draft = doc.CreateMapLevelOptionsDraft();
        Expect(revision == doc.EditRevision && dirty == doc.HasChanges && history.Undo.Count == doc.GetHistory().Undo.Count && history.Redo.Count == doc.GetHistory().Redo.Count, "Reading level options changed document/history."); Exact(doc, bytes);
        return JsonSerializer.Serialize(new { draft.Editable, draft.Partial, Values = draft.Values.OrderBy(row => row.Key) });
    }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2LevelOptions", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        Expect(MapLevelOptions.Rewards.Select(row => int.Parse(row.Value)).SequenceEqual(new[] { 0,24,12,43,8,6,41,18,39,21,42,48,19,29,5,27,2,47,4,36,26,32,1,15,25,44,14,17,35,20,3,33,16,11,28,34,40,22,7,13,10,45,23,9,37,49,50,51,30 }), "Independent original reward IDs/order differ.");
        Expect(MapLevelOptions.Rewards.All(row => row.English.Length != 0 && row.Russian.Length != 0) && MapLevelOptions.Flags.Count == 12, "Incomplete names/flags.");
        foreach (var native in new[] { false, true }) Check(native, directory);
        Historical(directory);
    }
    private static void Check(bool native, string directory)
    {
        var doc = NativeMapDocument.CreateNewMap(new("LevelOptions", "LevelOptions", 24, 24, PlaceHearts: true, NativeFormat: native));
        var draft = doc.CreateMapLevelOptionsDraft(); Expect(draft.Editable && !draft.Partial && draft.Values.Count == 12, "Full level options unavailable.");
        doc.SetField(draft.Record!, Field(doc, draft, "Options"), "3"); doc.SetField(draft.Record!, Field(doc, draft, "Options2"), "192"); doc.SetHistoryDepth(1);
        var initial = Snapshot(doc); var info = doc.CreateMapLevelOptionsDraft().Record!; var notifications = 0; doc.Changed += (_, _) => notifications++;
        // Independently encoded checkbox truth table, including inverted possession.
        for (var combination = 0; combination < 4096; combination++)
        {
            var primary = 3; var secondary = 192;
            foreach (var (index, second, bit) in new[] { (0,false,0x20),(1,false,0x40),(2,true,4),(3,true,8),(4,true,0x20),(5,false,8),(6,true,2),(7,false,0x10),(8,true,1),(9,true,0x10),(10,false,4),(11,false,0x80) })
                if (((combination & (1 << index)) != 0) != (index == 10)) { if (second) secondary |= bit; else primary |= bit; }
            draft = doc.CreateMapLevelOptionsDraft();
            var prepared = doc.UpdateMapLevelOptions(draft, draft.Values.Select(row => new MapLevelInput(row.Key, row.Key == "Options" ? primary.ToString() : row.Key == "Options2" ? secondary.ToString() : row.Value)));
            var expected = (byte[])initial[info.File].Clone(); expected[info.Offset + Field(doc, draft, "Options").Offset] = (byte)primary; expected[info.Offset + Field(doc, draft, "Options2").Offset] = (byte)secondary;
            var oldNotifications = notifications; doc.ApplyMapLevelOptions(prepared);
            Expect(info.File.Data.Span.SequenceEqual(expected) && initial.Where(row => row.Key != info.File).All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Named flag mapping changed unknown bits/unrelated data.");
            Expect(notifications == oldNotifications + (prepared.Changed ? 1 : 0), "Whole-group flags not one/no-op notification.");
            for (var index = 0; index < 12; index++) Expect(MapLevelOptions.Checked(MapLevelOptions.Flags[index], MapLevelOptions.Flags[index].Field == "Options" ? primary : secondary) == ((combination & (1 << index)) != 0), "Flag/inverted display differs.");
            if (prepared.Changed) { Expect(doc.GetHistory().Undo[^1].Description == "Change map level options", "Unnamed level history."); doc.Undo(); Exact(doc, initial); }
        }
        Console.WriteLine($"      source level flag combinations=4096 format={(native ? "native" : "project")}");
        foreach (var key in MapLevelOptions.RewardFields) foreach (var reward in MapLevelOptions.Rewards)
        {
            draft = doc.CreateMapLevelOptionsDraft(); var prepared = doc.UpdateMapLevelOptions(draft, Inputs(draft, key, reward.Value));
            var expected = (byte[])initial[info.File].Clone(); expected[info.Offset + Field(doc, draft, key).Offset] = byte.Parse(reward.Value);
            doc.ApplyMapLevelOptions(prepared); Expect(info.File.Data.Span.SequenceEqual(expected), "Reward ID/slot is not the original stored byte."); if (prepared.Changed) doc.Undo(); Exact(doc, initial);
        }
        foreach (var scalar in MapLevelOptions.Scalars)
        {
            foreach (var value in new[] { scalar.Minimum, scalar.Maximum })
            {
                draft = doc.CreateMapLevelOptionsDraft(); var prepared = doc.UpdateMapLevelOptions(draft, Inputs(draft, scalar.Field, value.ToString())); doc.ApplyMapLevelOptions(prepared);
                Expect(info.Read(Field(doc, draft, scalar.Field)) == value.ToString(), "Original scalar endpoint not stored."); if (prepared.Changed) doc.Undo(); Exact(doc, initial);
            }
            foreach (var value in new[] { scalar.Minimum - 1, scalar.Maximum + 1 }) { draft = doc.CreateMapLevelOptionsDraft(); Reject(() => doc.UpdateMapLevelOptions(draft, Inputs(draft, scalar.Field, value.ToString())), "Out-of-range level value accepted."); }
        }
        draft = doc.CreateMapLevelOptionsDraft();
        Reject(() => doc.UpdateMapLevelOptions(draft, Inputs(draft, "Options", "0")), "Unknown flag bits cleared.");
        Reject(() => doc.UpdateMapLevelOptions(draft, Inputs(draft, "RewardPrev1", "250")), "Unknown new reward invented.");
        Reject(() => doc.UpdateMapLevelOptions(draft, draft.Values.Skip(1).Select(row => new MapLevelInput(row.Key, row.Value))), "Missing field accepted.");
        Reject(() => doc.UpdateMapLevelOptions(draft, draft.Values.Select(row => new MapLevelInput("Options", row.Value))), "Duplicate fields accepted.");
        // Existing malformed scalars/rewards stay exact during unrelated edits.
        doc.SetField(info, Field(doc, draft, "RewardPrev1"), "250"); doc.SetField(info, Field(doc, draft, "GameTicks"), "0");
        draft = doc.CreateMapLevelOptionsDraft(); var raw = Snapshot(doc); var invalidRetained = doc.UpdateMapLevelOptions(draft, Inputs(draft, "Soundtrack", "3")); doc.ApplyMapLevelOptions(invalidRetained);
        Expect(info.Read(Field(doc,draft,"RewardPrev1")) == "250" && info.Read(Field(doc,draft,"GameTicks")) == "0", "Unrelated apply coerced imported invalid values."); doc.Undo(); Exact(doc, raw);
        var noOpRevision = doc.EditRevision; doc.ApplyMapLevelOptions(doc.UpdateMapLevelOptions(draft = doc.CreateMapLevelOptionsDraft(), draft.Values.Select(row => new MapLevelInput(row.Key, row.Value)))); Expect(doc.EditRevision == noOpRevision, "No-op changed revision.");
        var foreign = NativeMapDocument.Create(24, 24, "ForeignLevel"); Reject(() => foreign.ApplyMapLevelOptions(draft), "Foreign draft accepted.");
        doc.Paint([new(20,20)], new(4,2,1)); doc.Undo(); Reject(() => doc.ApplyMapLevelOptions(draft), "Edit+Undo reused level authority.");
        draft = doc.CreateMapLevelOptionsDraft(); Expect(MemoryMarshal.TryGetArray(info.File.Data, out var backing), "Synthetic backing unavailable.");
        var offset = backing.Offset + info.Offset + Field(doc,draft,"Options").Offset; var old = backing.Array![offset]; backing.Array[offset] ^= 1;
        Reject(() => doc.ApplyMapLevelOptions(draft), "Raw mutation reused level authority."); backing.Array[offset] = old;
        draft = doc.CreateMapLevelOptionsDraft(); var preparedRollback = doc.UpdateMapLevelOptions(draft, Inputs(draft, "GameTicks", "50"));
        var rollback = Snapshot(doc); var redo = doc.GetHistory().Redo.Count;
        Reject(() => doc.Edit("Fixture level rollback", () => { doc.ApplyMapLevelOptions(preparedRollback); doc.Paint([new(20,20)],new(4,2,1)); throw new InvalidOperationException("rollback"); }), "Nested failure swallowed."); Exact(doc, rollback); Expect(doc.GetHistory().Redo.Count == redo, "Failure consumed redo.");
        var final = doc.UpdateMapLevelOptions(doc.CreateMapLevelOptionsDraft(), doc.CreateMapLevelOptionsDraft().Values.Select(row => new MapLevelInput(row.Key, row.Key == "GameTicks" ? "50" : row.Key == "RewardPrev1" ? "51" : row.Value))); doc.ApplyMapLevelOptions(final);
        var saved = SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory, "level-saved-" + native)); var reopened = NativeMapDocument.Load(saved); Expect(ReadOnlyProjection(doc) == ReadOnlyProjection(reopened), "Level options changed after save/reopen.");
        if (!native)
        {
            var compiled = doc.CompileLegacyProject("LevelCompiled", NativeMapDocument.Create(24,24,"Basis"));
            Expect(compiled.CreateMapLevelOptionsDraft().Values.OrderBy(row => row.Key).SequenceEqual(doc.CreateMapLevelOptionsDraft().Values.OrderBy(row => row.Key)), "Compiler changed level scalars/flags/rewards.");
        }
        doc.SetHistoryDepth(0); draft = doc.CreateMapLevelOptionsDraft(); doc.ApplyMapLevelOptions(doc.UpdateMapLevelOptions(draft, Inputs(draft,"GameTicks","49"))); Expect(!doc.CanUndo && doc.CreateMapLevelOptionsDraft().Values["GameTicks"] == "49", "Zero history lost level edit.");
    }
    private static void Historical(string directory)
    {
        var source = NativeMapDocument.CreateNewMap(new("LevelVersions","LevelVersions",24,24)); var entry = SaveProjectionChecks.SaveCopy(source, Path.Combine(directory,"versions-source")); var file = LegacySectionFile.Read(entry);
        var info = file.Sections.Single(row => row.Signature == NativeMapDocument.LegacyInfo);
        foreach (var version in Enumerable.Range(1,8))
        {
            var structure = version switch { 1 => "MAPPROPERTIESV1", 2 or 3 => "MAPPROPERTIESV2", 4 => "MAPPROPERTIESV4", 5 => "MAPPROPERTIESV5", 6 => "MAPPROPERTIESV6", _ => "MAPPROPERTIES" };
            var replacement = new LegacySection(info.Signature, version, info.Payload[..LegacyProjectSchemas.Size(structure)]);
            var bytes = LegacySectionFile.Create(file.Sections.Select(row => row == info ? replacement : row)).ToArray(); var path = Path.Combine(directory,$"level-v{version}.dkm"); File.WriteAllBytes(path,bytes);
            var doc = NativeMapDocument.Load(path); var draft = doc.CreateMapLevelOptionsDraft();
            Expect(draft.Values.Count == (version == 4 ? 1 : version is >= 5 and <= 7 ? 12 : 0), "Historical padding guessed as level options.");
            if (version == 4) { doc.ApplyMapLevelOptions(doc.UpdateMapLevelOptions(draft,[new("Options","8")])); doc.Undo(); }
            else if (!draft.Editable) Reject(() => doc.ApplyMapLevelOptions(draft), "Unsupported metadata edited.");
            if (version <= 6) { doc.UpgradeLegacyProject(); Expect(doc.CreateMapLevelOptionsDraft().Values.Count == 12,"Historical upgrade did not introduce level fields."); doc.Undo(); }
            var saved = SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory,$"level-v{version}-exact")); Expect(SaveProjectionChecks.BeforeEquals(doc,bytes), "Historical level upgrade/edit Undo changed bytes.");
        }
    }
}
