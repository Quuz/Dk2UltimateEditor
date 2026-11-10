using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class MapPropertiesSessionChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException) { return; }
        throw new InvalidOperationException("Unsafe parent-properties operation accepted.");
    }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> snapshot) => Expect(snapshot.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Parent properties changed unexpected bytes.");
    private static void Same(NativeMapDocument left, NativeMapDocument right)
    {
        Expect(left.Files.Count == right.Files.Count && left.TargetGameVersion == right.TargetGameVersion && left.ResizeFillPreset == right.ResizeFillPreset, "Parent clone lost topology/session metadata.");
        foreach (var (a, b) in left.Files.Zip(right.Files)) Expect(a.ExportPath == b.ExportPath && a.SourcePath == b.SourcePath && a.Data.Span.SequenceEqual(b.Data.Span), "Parent clone differs from child bytes/paths.");
    }
    private static void Raw(NativeFile file, int offset, Action<byte[], int> write)
    { Expect(MemoryMarshal.TryGetArray(file.Data, out var backing), "Fixture backing unavailable."); write(backing.Array!, backing.Offset + offset); }
    private static NativeMapDocument New(bool native, int width = 32, int height = 30, bool hearts = false) =>
        NativeMapDocument.CreateNewMap(new("Properties", "Properties", width, height, Players: 5, PlaceHearts: hearts, NativeFormat: native));
    private static void General(NativeMapDocument doc, string key, string value)
    {
        var draft = doc.CreateMapGeneralDraft();
        doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, MapGeneralSettingsChecks.Inputs(draft, key, value)));
    }
    public static void Run()
    {
        LargeMapWarningChecks.Run();
        PlayerGoldNoticeChecks.Run();
        MapNameOverflowChecks.Run();
        MapCreatureModifiersChecks.Run();
        EditorResizeScriptChecks.Run();
        EditorResizePlayerChecks.Run();
        NativeResizeDerivedChecks.Run();
        var directory = Path.Combine(Path.GetTempPath(), "DK2Properties", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        foreach (var native in new[] { false, true }) { Composition(native, directory); Lifetimes(native); Scope(native); Anchors(native); Consent(native); }
        NativeVariables(); NativeOpaqueVariables(directory); Historical(directory); ScriptResize();
    }
    private static void Children(NativeMapDocument working)
    {
        General(working, "Name", working.IsLegacyProject ? "Éditeur" : "Общие свойства"); General(working, "Players", "2"); General(working, "Version", "13");
        var level = working.CreateMapLevelOptionsDraft();
        working.ApplyMapLevelOptions(working.UpdateMapLevelOptions(level, level.Values.Select(row => new MapLevelInput(row.Key, row.Key == "Soundtrack" ? "3" : row.Value))));
        var speech = working.CreateMapSpeechTextDraft();
        working.ApplyMapSpeechText(working.UpdateMapSpeechText(speech, speech.Values.Select(row => new MapSpeechTextInput(row.Key, row.Key == "TextTitleID" ? "21" : row.Value))));
        working.SetPlayerSettings(working.PlayerSettingsRows().Select(row => new PlayerSettingsChange(row.ID, "Player " + row.ID, 12345, false, row.Health is null ? null : 71)));
        working.ApplyPlayerAIProfile(Enumerable.Range(1, 7).Select(id => (uint)id), 2);
        working.SetPlayerCamera(3, new(7, 8)); working.SetPlayerBuildArea(1, new(3, 4, 12, 13));
        var changes = working.MapRuleCells().Where(row => row.Editable).GroupBy(row => row.Key.Kind).Select(group => group.First())
            .Select(row => new MapRuleChange(row.Key, row.Key.Kind == MapRuleKind.Alliance ? 1 : row.Key.Kind == MapRuleKind.CreaturePool ? 17 : 3));
        working.SetMapRules(changes);
        var variable = working.MapVariableCells().First(row => row.Editable && row.Definition.Minimum != row.Definition.Maximum);
        working.SetMapVariables([new(variable.Definition.ID, variable.Definition.Minimum)]);
    }
    private static void Composition(bool native, string directory)
    {
        var doc = New(native, hearts: true); var initial = Snapshot(doc); var target = doc.TargetGameVersion; var history = doc.GetHistory().Undo.Count; var revision = doc.EditRevision;
        using (var canceled = doc.CreateMapPropertiesSession())
        {
            Same(doc, canceled.WorkingDocument); Expect(doc.Files.Zip(canceled.WorkingDocument.Files).All(pair => !ReferenceEquals(pair.First, pair.Second)), "Child shares live file identities.");
            Children(canceled.WorkingDocument); Exact(initial); Expect(doc.EditRevision == revision && doc.GetHistory().Undo.Count == history && doc.TargetGameVersion == target, "Child acceptance leaked into live owner.");
        }
        Exact(initial);
        var session = doc.CreateMapPropertiesSession(); Children(session.WorkingDocument); var expected = Snapshot(session.WorkingDocument); var plan = doc.PrepareMapProperties(session);
        Expect(plan.Changed && !plan.Resized && !plan.NeedsCropConsent && !plan.NeedsLargeMapConsent, "Ordinary composed properties requested resize consent.");
        var notifications = 0; doc.Changed += (_, _) => notifications++; doc.ApplyMapProperties(plan);
        Same(doc, session.WorkingDocument); Exact(expected);
        Expect(notifications == 1 && doc.GetHistory().Undo.Count == history + 1 && doc.GetHistory().Undo.First().Children.Count >= 7, "Children were not a single nested live operation.");
        Reject(() => doc.ApplyMapProperties(plan)); Expect(!doc.IsMapPropertiesSessionCurrent(session), "Applied parent session remained current.");
        var changed = Snapshot(doc); doc.Undo(); Exact(initial); Expect(doc.TargetGameVersion == target && notifications == 2, "Parent undo lost native working target.");
        doc.Redo(); Exact(changed); Expect(doc.TargetGameVersion == 13 && notifications == 3, "Parent redo lost changes.");
        var saved = SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory, "composed-" + native)); var reopened = NativeMapDocument.Load(saved);
        Expect(reopened.CreateMapGeneralDraft().Values["Name"] == (native ? "Общие свойства" : "Éditeur") && reopened.TargetGameVersion == (native ? 17 : 13), "Parent save/reopen general values.");
        Expect(reopened.PlayerSettingsRows().All(row => row.Gold == 12345) && reopened.CreateMapSpeechTextDraft().Values["TextTitleID"] == "21", "Parent save/reopen child values.");
        doc.Undo(); var redo = doc.GetHistory().Redo.Count; revision = doc.EditRevision;
        var noOp = doc.CreateMapPropertiesSession(); doc.ApplyMapProperties(doc.PrepareMapProperties(noOp)); Exact(initial);
        Expect(doc.EditRevision == revision && doc.GetHistory().Redo.Count == redo, "No-op parent consumed redo/history.");
        doc.SetHistoryDepth(0); session = doc.CreateMapPropertiesSession(); General(session.WorkingDocument, "Players", "1"); doc.ApplyMapProperties(doc.PrepareMapProperties(session));
        Expect(!doc.CanUndo && doc.CreateMapGeneralDraft().Values["Players"] == "1", "Disabled history disabled parent editing.");
    }
    private static void Lifetimes(bool native)
    {
        var doc = New(native); var initial = Snapshot(doc); var session = doc.CreateMapPropertiesSession(); var plan = doc.PrepareMapProperties(session);
        General(doc, "Name", "Changed"); doc.Undo(); Exact(initial); Reject(() => doc.ApplyMapProperties(plan)); Reject(() => doc.PrepareMapProperties(session));
        session = doc.CreateMapPropertiesSession(); plan = doc.PrepareMapProperties(session); General(session.WorkingDocument, "Name", "Draft"); session.WorkingDocument.Undo();
        Reject(() => doc.ApplyMapProperties(plan)); Expect(doc.IsMapPropertiesSessionCurrent(session), "A child change invalidated the unchanged live owner.");
        plan = doc.PrepareMapProperties(session); Raw(session.WorkingDocument.TileFile, 36, (bytes, offset) => bytes[offset] ^= 1); Reject(() => doc.ApplyMapProperties(plan));
        session.Dispose(); Reject(() => doc.PrepareMapProperties(session));
        session = doc.CreateMapPropertiesSession(); plan = doc.PrepareMapProperties(session); Raw(doc.TileFile, 36, (bytes, offset) => bytes[offset] ^= 1); Reject(() => doc.ApplyMapProperties(plan)); Raw(doc.TileFile, 36, (bytes, offset) => bytes[offset] ^= 1);
        session = doc.CreateMapPropertiesSession(); plan = doc.PrepareMapProperties(session); Reject(() => New(native).ApplyMapProperties(plan));
        Reject(() => doc.Edit("Inside another transaction", () => doc.ApplyMapProperties(plan))); Exact(initial);
        using var discarded = doc.CreateMapPropertiesSession(); General(discarded.WorkingDocument, "Name", "Discarded"); var prepared = doc.PrepareMapProperties(discarded); discarded.Dispose(); Reject(() => doc.ApplyMapProperties(prepared)); Exact(initial);
    }
    private static void Scope(bool native)
    {
        var doc = New(native, hearts: true); var initial = Snapshot(doc);
        void Outside(Action<NativeMapDocument> edit)
        { using var session = doc.CreateMapPropertiesSession(); edit(session.WorkingDocument); Reject(() => doc.PrepareMapProperties(session)); Exact(initial); }
        Outside(working => working.Paint([new(4, 4)], new(4, 2, 1)));
        Outside(working => working.ApplyScenarioNameDraft(working.CreateScenarioNameDraft(ScenarioNameKind.Variable, 1), "Alias"));
        Outside(working => Raw(working.MainFile, 8, (bytes, offset) => bytes[offset] ^= 1));
        if (native)
        {
            Outside(working => { var heart = working.Records().First(row => row.Category == "GAMEGATE"); Raw(heart.File, heart.Offset + 8, (bytes, offset) => bytes[offset] ^= 1); });
            Outside(working => { var player = working.PlayerSettingsRows().First().Record; var field = player.Fields.Single(row => row.Name == "bGlobalPlayer"); working.SetField(player, field, "7"); });
        }
        else Outside(working => { var record = working.Records().Single(row => row.File.Signature == NativeMapDocument.LegacyInfo); var mask = new bool[record.Length]; foreach (var field in record.Fields) Array.Fill(mask, true, field.Offset, field.Size); var padding = Enumerable.Range(0, mask.Length).First(index => !mask[index]); Raw(record.File, record.Offset + padding, (bytes, offset) => bytes[offset] ^= 1); });
        Outside(working => { if (native) working.AddRecord(RecordTemplate.All.First(row => row.Structure == "GAMEOBJECT"), new(4, 4)); else working.AddLegacyRecord("Object", new(4, 4)); });
    }
    private static void Anchors(bool native)
    {
        foreach (var (width, height) in new[] { (35, 33), (29, 27), (35, 27) }) foreach (var anchor in Enumerable.Range(0, 9))
        {
            var doc = New(native); doc.Paint([new(8, 9), new(11, 14)], new(4, 3, 1)); doc.SetPlayerCamera(3, new(8, 9)); doc.SetPlayerBuildArea(3, new(4, 5, 10, 12));
            var before = Snapshot(doc); var tiles = Enumerable.Range(0, doc.Height).SelectMany(y => Enumerable.Range(0, doc.Width).Select(x => doc.GetTile(x, y))).ToArray();
            var session = doc.CreateMapPropertiesSession(); General(session.WorkingDocument, "Name", "Resized");
            var plan = doc.PrepareMapProperties(session, new(width, height, anchor)); Exact(before); doc.ApplyMapProperties(plan);
            var shiftX = (width - 32) * (anchor % 3) / 2; var shiftY = (height - 30) * (anchor / 3) / 2;
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var sx = x - shiftX; var sy = y - shiftY;
                var expected = x == 0 || y == 0 || x == width - 1 || y == height - 1 ? new MapTile(30, 2, 1)
                    : sx > 0 && sx < 31 && sy > 0 && sy < 29 ? tiles[sy * 32 + sx] : EditorTerrainRules.DecodeFillPreset(0x02020101);
                Expect(doc.GetTile(x, y) == expected, $"Independent anchor tile mismatch: native={native}, anchor={anchor}, ({x},{y}).");
            }
            var geometry = doc.PlayerGeometries().Single(row => row.ID == 3);
            Expect(geometry.Camera == new TilePoint(8 + shiftX, 9 + shiftY) && geometry.BuildStart == new TilePoint(4 + shiftX, 5 + shiftY), "Parent resize did not shift child/player coordinates.");
            var after = Snapshot(doc); doc.Undo(); Exact(before); doc.Redo(); Exact(after);
        }
    }
    private static void Consent(bool native)
    {
        var doc = New(native); var initial = Snapshot(doc); var session = doc.CreateMapPropertiesSession(); General(session.WorkingDocument, "Name", "Large");
        var large = doc.PrepareMapProperties(session, new(121, 120)); Expect(large.NeedsLargeMapConsent, "Large-map threshold missing."); Reject(() => doc.ApplyMapProperties(large)); Exact(initial);
        doc.ApplyMapProperties(large, largeMapAccepted: true); Expect(doc.Width == 121 && doc.Height == 120, "Large accepted resize absent.");
        session = doc.CreateMapPropertiesSession(); var unchanged = doc.PrepareMapProperties(session); Expect(!unchanged.NeedsLargeMapConsent, "Unchanged large map requested resize consent."); session.Dispose(); doc.Undo(); Exact(initial);
        foreach (var request in new[] { new MapPropertiesResize(6, 30), new(0, 30), new(1025, 30), new(32, 30, 9), new(35, 30, Fill: new MapTile(1, 0, 1)) })
        { using var invalid = doc.CreateMapPropertiesSession(); Reject(() => doc.PrepareMapProperties(invalid, request)); Exact(initial); }
        if (native) doc.AddRecord(RecordTemplate.All.First(row => row.Structure == "GAMEOBJECT"), new(29, 26)); else doc.AddLegacyRecord("Object", new(29, 26));
        initial = Snapshot(doc); session = doc.CreateMapPropertiesSession(); General(session.WorkingDocument, "Name", "Cropped");
        Reject(() => doc.PrepareMapProperties(session, new(24, 24))); Exact(initial);
        var crop = doc.PrepareMapProperties(session, new(24, 24, RemoveOutside: true)); Expect(crop.NeedsCropConsent && crop.Crops.Sum(row => row.Count) == 1, "Crop preview omitted placed entity.");
        Reject(() => doc.ApplyMapProperties(crop)); Exact(initial); doc.ApplyMapProperties(crop, croppingAccepted: true); Expect(doc.Width == 24, "Accepted crop absent."); doc.Undo(); Exact(initial);
    }
    private static void NativeVariables()
    {
        var doc = New(true); var variable = doc.MapVariableCells().First(row => row.Editable && row.Definition.Minimum != row.Definition.Maximum);
        doc.SetMapVariables([new(variable.Definition.ID, variable.Definition.Minimum)]);
        var record = doc.Records().Last(row => row.Category == "GAMEGLOBALVARIABLE" && BinaryPrimitives.ReadUInt32LittleEndian(row.File.Data.Span[row.Offset..]) == variable.Definition.ID);
        var mask = new bool[16]; Array.Fill(mask, true, 0, 4); foreach (var field in record.Fields) Array.Fill(mask, true, field.Offset, field.Size);
        var padding = Enumerable.Range(0, 16).First(index => !mask[index]); Raw(record.File, record.Offset + padding, (bytes, offset) => bytes[offset] = 0xA5);
        Raw(record.File, 8, (bytes, offset) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), 0));
        Raw(record.File, 52, (bytes, offset) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), 7)); var initial = Snapshot(doc);
        var session = doc.CreateMapPropertiesSession(); session.WorkingDocument.SetMapVariables([new(variable.Definition.ID, variable.Definition.Maximum)]);
        doc.ApplyMapProperties(doc.PrepareMapProperties(session)); Expect(record.File.Data.Span[record.Offset + padding] == 0xA5, "Imported variable padding normalized."); doc.Undo(); Exact(initial);
        session = doc.CreateMapPropertiesSession(); var draftRecord = session.WorkingDocument.Records().Last(row => row.Category == "GAMEGLOBALVARIABLE" && BinaryPrimitives.ReadUInt32LittleEndian(row.File.Data.Span[row.Offset..]) == variable.Definition.ID);
        Raw(draftRecord.File, draftRecord.Offset + padding, (bytes, offset) => bytes[offset] = 0xA4); Reject(() => doc.PrepareMapProperties(session)); Exact(initial);
        session = doc.CreateMapPropertiesSession();
        // Native collection deletion now uses the actual record payload when
        // stale imported metadata would underflow. The parent must accept that
        // same verified fallback and retain complete operation history.
        session.WorkingDocument.ClearMapVariableOverrides([variable.Definition.ID]);
        doc.ApplyMapProperties(doc.PrepareMapProperties(session));
        var resulting=record.File.Data.ToArray(); Expect(BinaryPrimitives.ReadUInt32LittleEndian(resulting.AsSpan(52))==BinaryPrimitives.ReadUInt32LittleEndian(resulting.AsSpan(20))*16,"Parent deletion wrapped stale variable payload metadata.");
        doc.Undo(); Exact(initial); doc.Redo(); Expect(record.File.Data.Span.SequenceEqual(resulting),"Parent variable-deletion Redo changed bytes."); doc.Undo(); Exact(initial);
    }
    private static void NativeOpaqueVariables(string directory)
    {
        var source = New(true); var unknown = source.AddRecord(new("GAMEGLOBALVARIABLE", 0xE6, 0xFFFF, 16), new(0, 0));
        Raw(unknown.File, unknown.Offset + 8, (bytes, offset) => bytes[offset] = 0xDC);
        var entry = SaveProjectionChecks.SaveCopy(source, Path.Combine(directory, "opaque-vars-input"));
        var variablePath = Directory.EnumerateFiles(Path.GetDirectoryName(entry)!, "*", SearchOption.AllDirectories).Single(path => Path.GetFileName(path).Equals("PropertiesVariables.kld", StringComparison.OrdinalIgnoreCase));
        var raw = File.ReadAllBytes(variablePath); var tail = new byte[] { 0x81, 0xFF, 0x13, 0x37 }; File.WriteAllBytes(variablePath, raw.Concat(tail).ToArray());
        var doc = NativeMapDocument.Load(entry); var initial = Snapshot(doc); var variableFile = doc.Files.Single(file => file.SourcePath == variablePath);
        var session = doc.CreateMapPropertiesSession(); var cell = session.WorkingDocument.MapVariableCells().First(row => row.Editable && row.Definition.Minimum != row.Definition.Maximum);
        session.WorkingDocument.SetMapVariables([new(cell.Definition.ID, cell.Definition.Minimum)]);
        doc.ApplyMapProperties(doc.PrepareMapProperties(session)); Expect(variableFile.Data.Span[^4..].SequenceEqual(tail), "Known insertion consumed opaque variable tail."); doc.Undo(); Exact(initial);
        session = doc.CreateMapPropertiesSession(); var bad = session.WorkingDocument.Records().Single(row => row.File.ExportPath == variableFile.ExportPath && BinaryPrimitives.ReadUInt32LittleEndian(row.File.Data.Span[row.Offset..]) == 0xFFFF);
        Raw(bad.File, bad.Offset + 8, (bytes, offset) => bytes[offset] ^= 1); Reject(() => doc.PrepareMapProperties(session)); Exact(initial);
        session = doc.CreateMapPropertiesSession(); var cloned = session.WorkingDocument.Files.Single(file => file.ExportPath == variableFile.ExportPath);
        Raw(cloned, cloned.Length - 1, (bytes, offset) => bytes[offset] ^= 1); Reject(() => doc.PrepareMapProperties(session)); Exact(initial);
        session = doc.CreateMapPropertiesSession(); bad = session.WorkingDocument.Records().Single(row => row.File.ExportPath == variableFile.ExportPath && BinaryPrimitives.ReadUInt32LittleEndian(row.File.Data.Span[row.Offset..]) == 0xFFFF);
        session.WorkingDocument.DeleteRecord(bad); Reject(() => doc.PrepareMapProperties(session)); Exact(initial);
    }
    private static void ScriptResize()
    {
        foreach (var native in new[] { false, true })
        {
            var doc = New(native); var action = native ? doc.AddRecord(RecordTemplate.All.First(row => row.Structure == "Action"), new(0, 0)) : doc.AddLegacyRecord("Action", new(0, 0));
            doc.SetEventType(action, LegacyEventSchemas.All.Single(row => row.RecordSignature == 0xD6 && row.Type == 1));
            action = doc.Records().Single(row => row.File == action.File && row.Offset == action.Offset);
            if (native) Raw(action.File, action.Offset + 12, (bytes, offset) => { BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), 29); BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset + 2), 27); });
            else { doc.SetField(action, action.Fields.Single(row => row.Name == "param5"), "29"); doc.SetField(action, action.Fields.Single(row => row.Name == "param7"), "27"); }
            var initial = Snapshot(doc); var session = doc.CreateMapPropertiesSession(); General(session.WorkingDocument, "Name", "Pending children"); var child = Snapshot(session.WorkingDocument);
            Reject(() => doc.PrepareMapProperties(session, new(24, 24, RemoveOutside: true))); Exact(initial); Exact(child);
        }
    }
    private static void Historical(string directory)
    {
        var original = New(false); var file = LegacySectionFile.Read(SaveProjectionChecks.SaveCopy(original, Path.Combine(directory, "historic"))); var info = file.Sections.Single(row => row.Signature == NativeMapDocument.LegacyInfo);
        foreach (var version in Enumerable.Range(1, 8))
        {
            var structure = version switch { 1 => "MAPPROPERTIESV1", 2 or 3 => "MAPPROPERTIESV2", 4 => "MAPPROPERTIESV4", 5 => "MAPPROPERTIESV5", 6 => "MAPPROPERTIESV6", _ => "MAPPROPERTIES" };
            var bytes = LegacySectionFile.Create(file.Sections.Select(row => row == info ? new LegacySection(info.Signature, version, info.Payload[..LegacyProjectSchemas.Size(structure)]) : row)).ToArray();
            var path = Path.Combine(directory, "parent-v" + version + ".dkm"); File.WriteAllBytes(path, bytes); var doc = NativeMapDocument.Load(path);
            if (version == 8) { Reject(() => doc.CreateMapPropertiesSession()); continue; }
            var session = doc.CreateMapPropertiesSession(); General(session.WorkingDocument, "Name", "Historical"); doc.ApplyMapProperties(doc.PrepareMapProperties(session)); doc.Undo();
            Expect(SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory, "parent-v" + version + "-saved")) is { } && SaveProjectionChecks.BeforeEquals(doc,bytes), "Parent implicitly upgraded historic metadata.");
        }
    }
    public static void FillState()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2ResizeFill", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        foreach (var preset in LegacyCompilerRules.Elements("NewMap").Select(row => row.ID)) foreach (var native in new[] { false, true })
        {
            var doc = NativeMapDocument.CreateNewMap(new("Fill", "Fill", 20, 20, FillID: preset, NativeFormat: native)); Expect(doc.ResizeFillPreset == preset, "New lost selected full background.");
            var before = Snapshot(doc); var session = doc.CreateMapPropertiesSession(); Expect(session.WorkingDocument.ResizeFillPreset == preset, "Parent clone lost background session.");
            doc.ApplyMapProperties(doc.PrepareMapProperties(session, new(23, 23))); Expect(doc.GetTile(21, 21) == EditorTerrainRules.DecodeFillPreset(preset), "Parent expansion used paint/default instead of stored background.");
            doc.Undo(); Exact(before); Expect(doc.ResizeFillPreset == preset, "Resize undo changed background.");
            var saved = SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory, $"fill-{native}-{preset:X8}")); var reopened = NativeMapDocument.Load(saved);
            Expect(reopened.ResizeFillPreset == (native ? 0x02020101u : preset), "Native reload must use original MAP_DEFAULTID; project must retain FillID.");
            Expect(!Directory.EnumerateFiles(Path.GetDirectoryName(saved)!, "*", SearchOption.AllDirectories).Any(path => path.Contains("#ResizeFill", StringComparison.Ordinal)), "Working fill leaked an exported file.");
            if (!native) Expect(doc.CompileLegacyProject("CompiledFill", NativeMapDocument.Create(20, 20, "Definitions")).ResizeFillPreset == preset, "Compiler discarded selected working fill.");
        }
    }
}
