using System.Buffers.Binary;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static class CompilerGateFallbackChecks
{
    private const string GameName = "DK2Preview_6a88240eaf814f78";
    private static readonly Lazy<NativeMapDocument> basis = new(() => NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd"));
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static NativeMapDocument New(int version = 17, bool hearts = false)
    {
        var document = NativeMapDocument.CreateNewMap(new("GateFallback", "GateFallback", 32, 30, PlaceHearts: hearts));
        var info = document.Records().Single(row => row.Category == "Project.MAPPROPERTIES");
        Set(document, info, "Version", version.ToString()); return document;
    }
    private static string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
    private static void Set(NativeMapDocument document, NativeRecord record, string name, string value)
        => document.SetField(record, record.Fields.Single(field => field.Name == name), value);
    private static NativeRecord Gate(NativeMapDocument document, int x, int y, string? id = null)
    {
        var record = document.AddMapRegion(MapRegionKind.Gate, new(x, y, 1, 1));
        if (id is not null) Set(document, record, "id", id);
        return record;
    }
    private static NativeRecord Action(NativeMapDocument document, string id)
    {
        var record = document.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(type => type.Name == "ACTION_DESTROYGATE"));
        Set(document, record, "param5", id); Set(document, record, "one", "1");
        // All of these remain stored project operands, but the original writer
        // writes a zero DWORD followed by the two derived coordinate WORDs.
        Set(document, record, "param2", "117"); Set(document, record, "param4", "233"); Set(document, record, "param6", "-12345");
        return record;
    }
    private static NativeMapDocument CompileReadOnly(NativeMapDocument document)
    {
        var bytes = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var history = document.GetHistory(); var revision = document.EditRevision; var dirty = document.HasChanges;
        var changes = 0; var histories = 0;
        EventHandler changed = (_, _) => changes++; EventHandler hc = (_, _) => histories++;
        document.Changed += changed; document.HistoryChanged += hc;
        NativeMapDocument result;
        try { result = document.CompileLegacyProject(GameName, basis.Value); }
        finally { document.Changed -= changed; document.HistoryChanged -= hc; }
        var after = document.GetHistory();
        Require(bytes.Count == document.Files.Count && bytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value))
            && revision == document.EditRevision && dirty == document.HasChanges && changes == 0 && histories == 0
            && history.EditorChangeCount == after.EditorChangeCount && history.ChangeCount == after.ChangeCount
            && history.Undo.SequenceEqual(after.Undo) && history.Redo.SequenceEqual(after.Redo) && history.SnapshotBytes == after.SnapshotBytes,
            "Compilation changed source bytes, counters, revision, dirty state, notifications or Undo/Redo.");
        return result;
    }
    private static NativeRecord NativeAction(NativeMapDocument document, NativeRecord projectAction) => document.Records()
        .Single(record => EntityReferences.EventType(record)?.Name == "ACTION_DESTROYGATE" && Read(record, "ID") == Read(projectAction, "id"));
    private static void Wire(NativeRecord native, NativeRecord project, ushort x, ushort y)
    {
        var expected = new byte[24];
        BinaryPrimitives.WriteUInt32LittleEndian(expected, 0xD6); BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(4), 16);
        BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(12), x); BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(14), y);
        BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(16), ushort.Parse(Read(project, "id")));
        BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(18), ushort.Parse(Read(project, "next")));
        expected[22] = 32; expected[23] = 1;
        Require(native.Length == 24 && native.File.Data.Span.Slice(native.Offset, 24).SequenceEqual(expected), "DESTROYGATE differs from the complete literal 24-byte source recipe.");
    }
    public static void Run(Action<string, Action> run)
    {
        run("compiler gate fallback: absent IDs0/1/255 emit exact source1,1 in all three target versions", () =>
        {
            foreach (var version in new[] { 13, 16, 17 }) foreach (var missing in new[] { "0", "1", "255" })
            {
                var document = New(version); var action = Action(document, missing);
                var output = CompileReadOnly(document); var native = NativeAction(output, action);
                Wire(native, action, 1, 1);
                Require(Read(action, "param5") == missing && output.TargetGameVersion == version && BinaryPrimitives.ReadUInt32LittleEndian(output.MainFile.Data.Span[4..]) == 4,
                    "Compiler repaired the missing ID or confused the target game version with GAMEINFO version4.");
                if (missing != "0") Require(NativeMapValidator.Validate(document).Any(row => row.Code == "DK2N026" && row.Severity == DiagnosticSeverity.Warning), "Missing-target warning disappeared.");
            }
        });
        run("compiler gate fallback: working target stays absent when an unrelated gate occupies source fallback coordinates", () =>
        {
            foreach (var missing in new[] { "0", "99" })
            {
                var document = New(); _ = Gate(document, 0, 0); var action = Action(document, missing);
                var output = CompileReadOnly(document); var native = NativeAction(output, action); Wire(native, action, 1, 1);
                Require(output.SelectedScenarioGate(output.CreateScenarioParameterDraft(native)) is null, "Fallback coordinates silently acquired a different working gate identity.");
                var snapshot = output.CreateCompilationSnapshot(); var snapshotAction = NativeAction(snapshot, action);
                Require(snapshot.SelectedScenarioGate(snapshot.CreateScenarioParameterDraft(snapshotAction)) is null, "Independent snapshot lost the intentionally missing gate target.");
            }
        });
        run("compiler gate fallback: sparse known IDs use their own coordinates and missing IDs never choose the first gate", () =>
        {
            var document = New(); _ = Gate(document, 3, 4, "11"); var selected = Gate(document, 14, 17, "42");
            var known = Action(document, "42"); var absent = Action(document, "41");
            var output = CompileReadOnly(document); Wire(NativeAction(output, known), known, 15, 18); Wire(NativeAction(output, absent), absent, 1, 1);
            document.DeleteRecord(selected);
            var removed = CompileReadOnly(document); Wire(NativeAction(removed, known), known, 1, 1);
            document.Undo(); var restored = CompileReadOnly(document); Wire(NativeAction(restored, known), known, 15, 18);
            document.Redo(); var redone = CompileReadOnly(document); Wire(NativeAction(redone, known), known, 1, 1);
        });
        run("compiler gate fallback: real DKM Save/reopen retains the missing ID and official export remains exactly seven files", () =>
        {
            var document = New(hearts: true); var action = Action(document, "73");
            Require(!NativeMapValidator.Validate(document, includeGameplay: true).Any(row => row.Severity == DiagnosticSeverity.Error), "Save/export fixture is not valid under gameplay terrain validation.");
            var directory = Path.Combine(Path.GetTempPath(), "DK2CompilerGateFallback-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "working.dkm"); using (var save = new EditorProjectSaveSession(document)) save.SaveAs(path);
            var reopened = NativeMapDocument.Load(path); var retained = reopened.Records().Single(record => record.Category == "Project.ACTION");
            Require(Read(retained, "param5") == "73" && Read(retained, "id") == Read(action, "id"), "DKM publication changed source ID/operand.");
            var output = CompileReadOnly(reopened); Wire(NativeAction(output, retained), retained, 1, 1);
            var game = output.SaveCopy(Path.Combine(directory, "game"));
            Require(Path.GetFileNameWithoutExtension(game) == GameName && Directory.GetFiles(Path.GetDirectoryName(game)!).Length == 7, "Fallback changed the required preview name or official seven-file bundle.");
            var gameDocument = NativeMapDocument.Load(game); Wire(NativeAction(gameDocument, retained), retained, 1, 1);
        });
        run("compiler gate fallback: native-source companion cannot restore the deleted target's old coordinates", () =>
        {
            var native = NativeMapDocument.Create(32, 30, GameName, basis.Value);
            var gate = native.AddMapRegion(MapRegionKind.Gate, new(8, 10, 1, 1));
            var sourceAction = native.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(type => type.Name == "ACTION_DESTROYGATE"));
            native.ApplyScenarioEditorParameterDraft(native.UpdateScenarioGate(native.CreateScenarioParameterDraft(sourceAction), gate.File.ExportPath + ":" + gate.Offset));
            var sourceBytes = native.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var directory = Path.Combine(Path.GetTempPath(), "DK2GateFallbackCompanion-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "working.dkm");
            using (var save = new EditorSaveSession(native)) Require(save.Save(save.PrepareSaveAs(path), acceptLosses: true), "Native-to-DKM save refused the known gate fixture.");
            var project = NativeMapDocument.Load(path); var action = project.Records().Single(record => record.Category == "Project.ACTION");
            var importedGate = project.MapRegions().Single(region => region.Kind == MapRegionKind.Gate).Record;
            var id = Read(action, "param5"); Require(id == Read(importedGate, "id"), "Native import lost the gate ID before deletion.");
            project.DeleteRecord(importedGate);
            using (var save = new EditorProjectSaveSession(project)) save.SaveAs(path);
            var reopened = NativeMapDocument.Load(path); var retained = reopened.Records().Single(record => record.Category == "Project.ACTION");
            Require(Read(retained, "param5") == id, "Repeated DKM Save/reopen repaired the missing target.");
            var compiled = CompileReadOnly(reopened); var compiledAction = NativeAction(compiled, retained); Wire(compiledAction, retained, 1, 1);
            Require(compiled.SelectedScenarioGate(compiled.CreateScenarioParameterDraft(compiledAction)) is null, "Companion retention reassigned the absent gate.");
            Require(sourceBytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Source companion workflow changed the original native map bytes.");
        });
    }
}
