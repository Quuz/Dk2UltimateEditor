using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static class EditorResizeScriptChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception error) when (error is InvalidOperationException or InvalidDataException or ArgumentException) { return; } throw new InvalidOperationException("Unsafe resize/compile was accepted."); }
    private static NativeMapDocument New(bool native) => NativeMapDocument.CreateNewMap(new("ScriptResize", "ScriptResize", 32, 30, NativeFormat: native));
    private static NativeRecord Add(NativeMapDocument document, int type, int x, int y)
    {
        var record = document.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(item => item.RecordSignature == 0xD6 && item.Type == type));
        foreach (var (number, value) in new[] { (5, x), (7, y) })
        {
            var field = record.Fields.Single(item => item.Name == (document.IsLegacyProject ? "param" : "Param") + number);
            document.SetField(record, field, (document.IsLegacyProject ? value : unchecked((ushort)value)).ToString(CultureInfo.InvariantCulture));
        }
        return document.Records().Single(item => item.File == record.File && item.Offset == record.Offset);
    }
    private static int Read(NativeRecord record, int number, bool project) => int.Parse(record.Read(record.Fields.Single(item => item.Name == (project ? "param" : "Param") + number)), CultureInfo.InvariantCulture);
    public static void Run()
    {
        using var capture = JsonDocument.Parse(typeof(EditorResizeScriptChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.EditorResizeScriptsSource.json")!);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DK2MapEditor.slnx"))) root = root.Parent;
        foreach (var input in capture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original resize-script source changed.");
        Expect(capture.RootElement.GetProperty("ActionTypes").EnumerateArray().Select(item => item.GetInt32()).SequenceEqual(new[] { 1, 22, 49 })
            && capture.RootElement.GetProperty("SourceCounterIncrements").GetInt32() == 0
            && capture.RootElement.GetProperty("CompilerUpperBoundOnly").GetBoolean(), "Unverified source resize/compile recipe.");
        // Expected LONG results and their WORD projections are literal, including
        // both signed overflows and negative/outside targets. No product helper
        // calculates the expected coordinates.
        var cases = new[] {
            (36, 34, 8, 5, 7, 9, 11, 9, 11),
            (24, 22, 8, 5, 7, -3, -1, 65533, 65535),
            (24, 22, 0, 29, 27, 29, 27, 29, 27),
            (24, 22, 4, 5, 7, 1, 3, 1, 3),
            (36, 34, 4, 32, 30, 34, 32, 34, 32),
            (36, 34, 0, 0, 0, 0, 0, 0, 0),
            (36, 34, 8, -1, -1, 3, 3, 3, 3),
            (36, 34, 8, int.MaxValue, int.MaxValue, -2147483645, -2147483645, 3, 3),
            (24, 22, 8, int.MinValue, int.MinValue, 2147483640, 2147483640, 65528, 65528),
            (24, 34, 2, 31, 29, 23, 29, 23, 29),
            (36, 22, 6, 31, 29, 31, 21, 31, 21),
            (31, 29, 4, 5, 7, 5, 7, 5, 7),
            (33, 31, 8, 31, 29, 32, 30, 32, 30)
        };
        foreach (var native in new[] { false, true }) foreach (var type in new[] { 1, 22, 49 }) foreach (var row in cases)
        {
            var (width, height, anchor, x, y, px, py, nx, ny) = row;
            var doc = New(native); var action = Add(doc, type, x, y); var id = ScriptGraph.ID(action);
            var originalAction = action.File.Data.Span.Slice(action.Offset, action.Length).ToArray();
            var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var counter = doc.EditorChangeCount; var count = doc.GetHistory().Undo.Count; var revision = doc.EditRevision;
            var activity = doc.ReadActivityStatistics()!.Encode();
            var session = doc.CreateMapPropertiesSession(); var child = session.WorkingDocument.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var plan = doc.PrepareEditorMapProperties(session, new(width, height, anchor));
            Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && child.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value))
                && doc.EditRevision == revision && doc.GetHistory().Undo.Count == count, "Resize preview changed the live or private map.");
            Expect(!plan.NeedsCropConsent && doc.EditorMapPropertiesChangeIncrements(plan) == 2, "Script relocation invented a crop/removal counter or consent.");
            var notifications = 0; EventHandler listener = (_, _) => notifications++; doc.Changed += listener;
            doc.ApplyEditorMapProperties(plan);
            var current = ScriptGraph.Build(doc).Events[id].Record; var expected = (byte[])originalAction.Clone();
            if (native)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(12), (ushort)nx); BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(14), (ushort)ny);
            }
            else
            {
                BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(32), px); BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(40), py);
            }
            Expect(current.File.Data.Span.Slice(current.Offset, current.Length).SequenceEqual(expected), $"Source target relocation changed extra event bytes: native={native}, type={type}, case={row}.");
            Expect(Read(current, 5, !native) == (native ? nx : px) && Read(current, 7, !native) == (native ? ny : py), "Literal resize-script coordinates differ.");
            Expect(doc.EditorChangeCount == unchecked(counter + 2) && doc.GetHistory().Undo.Count == count + 1 && notifications == 1, "Script resize was not one parent Undo/two source counts/one notification.");
            Expect(doc.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity), "Script relocation invented activity statistics.");
            doc.Changed -= listener; var after = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
            doc.Undo(); Expect(doc.EditorChangeCount == counter && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Source script resize Undo lost bytes/count.");
            doc.Redo(); Expect(doc.EditorChangeCount == unchecked(counter + 2) && after.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Source script resize Redo lost bytes/count.");
            var output = Path.Combine(Path.GetTempPath(), "DK2ScriptResize", Guid.NewGuid().ToString("N"));
            var saved = SaveProjectionChecks.SaveCopy(doc, output); var reopened = NativeMapDocument.Load(saved);
            var reloaded = ScriptGraph.Build(reopened).Events[id].Record;
            Expect(Read(reloaded, 5, !native) == (native ? nx : px) && Read(reloaded, 7, !native) == (native ? ny : py), "Save/reopen normalized relocated script operands.");
        }
        foreach (var native in new[] { false, true }) foreach (var type in new[] { 1, 22, 49 })
        {
            var doc = New(native); var action = Add(doc, type, 29, 27); var actionID = ScriptGraph.ID(action);
            var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var count = doc.GetHistory().Undo.Count;
            Reject(() => doc.Resize(24, 22, 0, 0, new(2, 2, 1), removeOutside: true));
            using (var generic = doc.CreateMapPropertiesSession()) Reject(() => doc.PrepareMapProperties(generic, new(24, 22, RemoveOutside: true)));
            Expect(doc.GetHistory().Undo.Count == count && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Strict generic resize changed its prior crop/refusal behavior.");
            using (var canceled = doc.CreateMapPropertiesSession()) _ = doc.PrepareEditorMapProperties(canceled, new(24, 22));
            Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Canceled editor target resize changed live bytes.");
            var rootEdge = doc.EnsureScriptScope(doc.ScriptOwners().Single(owner => owner.Scope == 32));
            doc.SetScriptLink(rootEdge.Owner, rootEdge.Field, actionID);
            var session = doc.CreateMapPropertiesSession(); var plan = doc.PrepareEditorMapProperties(session, new(24, 22));
            doc.ApplyEditorMapProperties(plan);
            var bytes = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var diagnostic = NativeMapValidator.Validate(doc).Single(item => item.Code == "DK2N029");
            Expect(diagnostic.Severity == (native ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error), "Source bound-project compiler target check or native warning differs.");
            Expect(!NativeMapValidator.Validate(doc, includeScripts: false).Any(item => item.Code == "DK2N029"), "No-scenario projection acquired a script check.");
            if (!native) Reject(() => doc.CompileLegacyProject("InvalidScriptTarget", NativeMapDocument.Create(24, 22, "Definitions")));
            Expect(bytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Read-only target diagnostic/failed compiler altered map bytes.");
        }
        foreach (var type in new[] { 1, 22, 49 })
        {
            var doc = New(false); var action = Add(doc, type, 11, 13); var id = ScriptGraph.ID(action);
            var edge = doc.EnsureScriptScope(doc.ScriptOwners().Single(owner => owner.Scope == 32)); doc.SetScriptLink(edge.Owner, edge.Field, id);
            var session = doc.CreateMapPropertiesSession(); var plan = doc.PrepareEditorMapProperties(session, new(36, 34, 8));
            doc.ApplyEditorMapProperties(plan);
            var compiled = doc.CompileLegacyProject("ValidScriptResize", NativeMapDocument.Create(36, 34, "Definitions"));
            var record = ScriptGraph.Build(compiled).Events[id].Record;
            Expect(BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span.Slice(record.Offset + 12, 2)) == 15
                && BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span.Slice(record.Offset + 14, 2)) == 17, "Valid resized project lost target during game compilation.");
            session = doc.CreateMapPropertiesSession(); plan = doc.PrepareEditorMapProperties(session, new(32, 30));
            doc.SetHistoryDepth(0); doc.ApplyEditorMapProperties(plan); Expect(!doc.CanUndo && doc.GetHistory().SnapshotBytes == 0, "Zero-depth source resize retained a history payload.");
        }
        // Other actions and conditions carry numbers and IDs in the same slots;
        // resizing must not treat those operands as map coordinates.
        foreach (var native in new[] { false, true })
        {
            var doc = New(native);
            foreach (var type in LegacyEventSchemas.All.Where(type => type.RecordSignature != 0xD6 || type.Type is not (1 or 2 or 22 or 32 or 49)))
                doc.AddScenarioEvent(type.RecordSignature, new(0, 0), type);
            var before = doc.Records().Where(ScriptGraph.IsEvent).Select(record => record.File.Data.Span.Slice(record.Offset, record.Length).ToArray()).ToArray();
            var session = doc.CreateMapPropertiesSession(); doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(session, new(24, 22, 8)));
            Expect(before.Zip(doc.Records().Where(ScriptGraph.IsEvent)).All(pair => pair.Second.File.Data.Span.Slice(pair.Second.Offset, pair.Second.Length).SequenceEqual(pair.First)), "Resize changed a non-spatial event's payload, identity or links.");
        }
        Console.WriteLine("EDITOR RESIZE SCRIPT DETAIL 78 literal native/project x three actions x 13 targets/anchors; source LONG/WORD wrap, out-of-map retention, strict raw refusal, preview/cancel, full Undo/Redo/save/reopen, two parent counts, 98 non-spatial event types unchanged and read-only bound compiler diagnostics");
    }
}
