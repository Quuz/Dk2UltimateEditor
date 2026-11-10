using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    private static void VerifySpatialSource()
    {
        var unsupported = pages.Where(page => page.PageID != -1 && !ScenarioControlPages.Supports(page.Type)).Select(page => page.Type).ToArray();
        Expect(unsupported.Length == 0, "The completed source parameter catalogue must contain all69 pages.");
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioSpatialSource.json")!;
        using var fixture = JsonDocument.Parse(stream);
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Source"))) root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Original source missing.");
        foreach (var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Spatial original source changed.");
        foreach (var type in new[] { "ACTION_CHANGESQUARE", "ACTION_CHANGEOWNER" })
        {
            var page = fixture.RootElement.GetProperty("Pages").GetProperty(type);
            Expect(page.GetProperty("Coordinates").GetProperty("StoredOffset").GetInt32() == 1
                && page.GetProperty("Coordinates").GetProperty("DisplayMaximum").GetInt32() == 999, "Source coordinate convention differs.");
            Expect(ScenarioControlPages.Controls(type).Where(control => control.Parameter is 5 or 7)
                .All(control => control.DisplayOffset == 1 && control.Maximum == 1000), "Spatial controls lost source display offset.");
        }
        Expect(fixture.RootElement.GetProperty("Functions").GetProperty("DlgParamsSave").GetString()!.Contains("Context->ActionCtx.ChangeRect = FALSE"), "Source rectangle reset was not captured.");
    }

    public static void RunSpatialPoints()
    {
        VerifySpatialSource();
        var directory = Path.Combine(Path.GetTempPath(), "DK2Spatial-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false, true })
            foreach (var type in new[] { "ACTION_CHANGESQUARE", "ACTION_CHANGEOWNER" })
            {
                var doc = NativeMapDocument.CreateNewMap(new("Spatial", "Spatial", 20, 20, NativeFormat: native)); doc.SetHistoryDepth(65535);
                var area = doc.AddMapRegion(MapRegionKind.Area, new(3, 5, 8, 6)); doc.SetField(area, EntityReferences.Identity(area)!.Field, "13");
                var page = pages.Single(item => item.Type == type); var record = Add(doc, native, page);
                string Name(int n) => (native ? "Param" : "param") + n;
                var before = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
                void Encode(Dictionary<NativeFile, byte[]> expected, int n, uint value)
                {
                    var pos = native ? n == 1 ? 8 : n == 2 ? 9 : n == 5 ? 12 : 14 : 12 + n * 4;
                    if (!native) BinaryPrimitives.WriteUInt32LittleEndian(expected[record.File].AsSpan(record.Offset + pos), value);
                    else if (n is 1 or 2) expected[record.File][record.Offset + pos] = checked((byte)value);
                    else BinaryPrimitives.WriteUInt16LittleEndian(expected[record.File].AsSpan(record.Offset + pos), checked((ushort)value));
                }
                var choicesChecked = 0;
                foreach (var context in ReadNamed().Contexts.Where(item => item.Type == type))
                foreach (var value in context.Values)
                {
                    var expected = before.ToDictionary(item => item.Key, item => item.Value.ToArray()); Encode(expected, context.Parameter, (uint)value);
                    var draft = doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(context.Parameter), value.ToString(CultureInfo.InvariantCulture))]); Exact(before);
                    doc.ApplyScenarioParameterDraft(draft); Exact(expected); Expect(doc.GetHistory().Undo.Count == history + 1, "Spatial choice split history.");
                    doc.Undo(); Exact(before); choicesChecked++;
                }
                foreach (var point in new[] { new TilePoint(0, 0), new(19, 19), new(3, 5) })
                {
                    var expected = before.ToDictionary(item => item.Key, item => item.Value.ToArray()); Encode(expected, 5, (uint)point.X + 1); Encode(expected, 7, (uint)point.Y + 1);
                    var draft = doc.UpdateScenarioSpatialPoint(doc.CreateScenarioParameterDraft(record), point); Exact(before);
                    doc.ApplyScenarioParameterDraft(draft); Exact(expected); Expect(doc.GetHistory().Undo.Count == history + 1, "Spatial point was not one edit.");
                    doc.ApplyScenarioParameterDraft(doc.UpdateScenarioSpatialPoint(doc.CreateScenarioParameterDraft(record), point)); Exact(expected);
                    Expect(doc.GetHistory().Undo.Count == history + 1, "Identical point added history.");
                    var saved = doc.SaveCopy(Path.Combine(directory, (native ? "native-" : "project-") + type + "-" + point.X));
                    var reopened = NativeMapDocument.Load(saved); var rerecord = reopened.Records().Single(item => EntityReferences.EventType(item)?.Name == type);
                    Expect(reopened.CreateScenarioParameterDraft(rerecord).Values.SequenceEqual(doc.CreateScenarioParameterDraft(record).Values), "Spatial save/reopen changed operands.");
                    doc.Undo(); Exact(before); doc.Redo(); Exact(expected); doc.Undo(); Exact(before);
                }
                var areaDraft = doc.UpdateScenarioSpatialArea(doc.CreateScenarioParameterDraft(record), 13);
                Expect(areaDraft.Values[Name(5)] == "4" && areaDraft.Values[Name(7)] == "6", "Spatial area stored ID/centre/raw spawn coordinates.");
                Reject(() => doc.UpdateScenarioSpatialArea(doc.CreateScenarioParameterDraft(record), 14));
                foreach (var n in new[] { 5, 7 })
                {
                    foreach (var value in new[] { "1", "1000" }) Expect(doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(n), value)]).Values[Name(n)] == value, "Typed original range was restricted to map size.");
                    foreach (var value in new[] { "-1", "0", "1001" }) Reject(() => doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(n), value)]));
                }
                foreach (var point in new[] { new TilePoint(-1, 0), new(20, 0), new(0, 20) }) Reject(() => doc.UpdateScenarioSpatialPoint(doc.CreateScenarioParameterDraft(record), point));
                doc.SetField(record, record.Fields.Single(item => item.Name == Name(5)), "0");
                var imported = Snapshot(doc); var ih = doc.GetHistory().Undo.Count;
                doc.ApplyScenarioParameterDraft(doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [])); Exact(imported);
                Expect(doc.GetHistory().Undo.Count == ih, "Opening zero coordinates repaired them."); doc.Undo(); Exact(before);
                var stale = doc.CreateScenarioParameterDraft(record); doc.SetField(record, record.Fields.Single(item => item.Name == Name(5)), "2"); doc.Undo();
                Reject(() => doc.UpdateScenarioSpatialPoint(stale, new(1, 1)));
                var raw = doc.CreateScenarioParameterDraft(record); Mutable(record.File)[record.Offset + 15] ^= 1;
                Reject(() => doc.UpdateScenarioSpatialPoint(raw, new(1, 1))); Mutable(record.File)[record.Offset + 15] ^= 1; Exact(before);
                var foreign = NativeMapDocument.CreateNewMap(new("Foreign", "Foreign", 20, 20, NativeFormat: native));
                Reject(() => foreign.UpdateScenarioSpatialPoint(doc.CreateScenarioParameterDraft(record), new(1, 1)));
                if (!native)
                {
                    var prepared = doc.UpdateScenarioControlPage(doc.UpdateScenarioSpatialPoint(doc.CreateScenarioParameterDraft(record), new(3, 5)), [new("param2", "3")]);
                    if (type == "ACTION_CHANGESQUARE") prepared = doc.UpdateScenarioControlPage(prepared, [new("param1", "6")]);
                    doc.ApplyScenarioParameterDraft(prepared);
                    var compiled = doc.CompileLegacyProject("SpatialCompiled", NativeMapDocument.Create(3, 3, "Basis"));
                    var action = compiled.Records().Single(item => EntityReferences.EventType(item)?.Name == type);
                    Expect(action.File.Data.Span.Slice(action.Offset + 8, 8).SequenceEqual(new byte[] { type == "ACTION_CHANGESQUARE" ? (byte)6 : (byte)0, 3, 0, 0, 4, 0, 6, 0 }), "Spatial compiler differs from literal BYTE/BYTE/zero/zero/WORD/WORD.");
                }
                Console.WriteLine($"SPATIAL POINT DETAIL native={native} type={type} choices={choicesChecked} coordinates area padding repeat imported guards undo reopen compile");
            }
        }
        finally { Directory.Delete(directory, true); }
    }

    public static void RunSpatialGates()
    {
        VerifySpatialSource();
        foreach (var native in new[] { false, true })
        {
            var doc = NativeMapDocument.CreateNewMap(new("Gates", "Gates", 20, 20, NativeFormat: native)); doc.SetHistoryDepth(65535);
            var page = pages.Single(item => item.Type == "ACTION_DESTROYGATE");
            Expect(ScenarioControlPages.MissingRequirements(page.Type, doc).SequenceEqual(new[] { EntityKind.Gate }), "Missing gate was not surfaced.");
            var first = doc.AddMapRegion(MapRegionKind.Gate, new(3, 5, 1, 1));
            var second = doc.AddMapRegion(MapRegionKind.Gate, new(7, 9, 1, 1));
            if (!native)
            {
                doc.SetField(first, EntityReferences.Identity(first)!.Field, "13");
                doc.SetField(second, EntityReferences.Identity(second)!.Field, "27");
            }
            else
            {
                var heart = doc.AddMapRegion(MapRegionKind.Gate, new(11, 11, 1, 1));
                doc.SetField(heart, heart.Fields.Single(item => item.Name == "bGateID"), "0");
                doc.SetField(heart, heart.Fields.Single(item => item.Name == "bGateType"), "14");
            }
            var record = Add(doc, native, page); var before = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
            Expect(doc.ScenarioGateChoices().Count == 2 && ScenarioControlPages.MissingRequirements(page.Type, doc).Count == 0, "Keeper heart was offered as an original gate.");
            Expect(doc.SelectedScenarioGate(doc.CreateScenarioParameterDraft(record)) is null, "Unknown imported gate was silently resolved.");
            foreach (var choice in doc.ScenarioGateChoices())
            {
                var expected = before.ToDictionary(item => item.Key, item => item.Value.ToArray());
                if (native)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(expected[record.File].AsSpan(record.Offset + 12), (ushort)(choice.X + 1));
                    BinaryPrimitives.WriteUInt16LittleEndian(expected[record.File].AsSpan(record.Offset + 14), (ushort)(choice.Y + 1));
                }
                else BinaryPrimitives.WriteUInt32LittleEndian(expected[record.File].AsSpan(record.Offset + 32), choice.ProjectID!.Value);
                var draft = doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(record), choice.Key); Exact(before);
                doc.ApplyScenarioParameterDraft(draft); Exact(expected); Expect(doc.GetHistory().Undo.Count == history + 1, "Gate choice was not atomic.");
                Expect(doc.SelectedScenarioGate(doc.CreateScenarioParameterDraft(record)) == choice.Key, "Gate did not round-trip its actual identity/coordinates.");
                doc.ApplyScenarioParameterDraft(doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(record), choice.Key)); Exact(expected);
                Expect(doc.GetHistory().Undo.Count == history + 1, "Same gate added history.");
                if (!native)
                {
                    var compiled = doc.CompileLegacyProject("GateCompiled", NativeMapDocument.Create(3, 3, "Basis")); var action = compiled.Records().Single(item => EntityReferences.EventType(item)?.Name == page.Type);
                    Expect(action.File.Data.Span.Slice(action.Offset + 8, 8).SequenceEqual(new byte[] { 0, 0, 0, 0, (byte)(choice.X + 1), 0, (byte)(choice.Y + 1), 0 }), "Destroy-gate compiler wrote an ID instead of original one-based coordinates.");
                }
                doc.Undo(); Exact(before); doc.Redo(); Exact(expected); doc.Undo(); Exact(before);
            }
            var initialChoice = doc.ScenarioGateChoices()[0];
            var stale = doc.CreateScenarioParameterDraft(record); doc.SetMapRegionBounds(first, new(4, 5, 1, 1)); doc.Undo(); Reject(() => doc.UpdateScenarioGate(stale, initialChoice.Key)); Exact(before);
            var raw = doc.CreateScenarioParameterDraft(record); Mutable(first.File)[first.Offset + (native ? 8 : 16)] ^= 1;
            Reject(() => doc.UpdateScenarioGate(raw, initialChoice.Key)); Mutable(first.File)[first.Offset + (native ? 8 : 16)] ^= 1; Exact(before);
            Reject(() => doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(record), "missing"));
            var other = NativeMapDocument.CreateNewMap(new("ForeignGate", "ForeignGate", 20, 20, NativeFormat: native));
            Reject(() => other.UpdateScenarioGate(doc.CreateScenarioParameterDraft(record), initialChoice.Key));
            if (native)
            {
                doc.SetMapRegionBounds(second, new(3, 5, 1, 1));
                doc.ApplyScenarioParameterDraft(doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(record), doc.ScenarioGateChoices()[0].Key));
                Expect(doc.SelectedScenarioGate(doc.CreateScenarioParameterDraft(record)) == doc.ScenarioGateChoices()[0].Key, "Duplicate native coordinates did not use the original reader's first physical gate.");
            }
            else
            {
                var identity = EntityReferences.Identity(second)!;
                // Normal editing correctly prevents duplicate IDs; model an
                // imported malformed record without bypassing production guards.
                identity.Field.Encode("13").CopyTo(Mutable(second.File), second.Offset + identity.Field.Offset);
                var duplicate = Snapshot(doc);
                Reject(() => doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(record), doc.ScenarioGateChoices()[0].Key)); Exact(duplicate);
            }
            Console.WriteLine($"SPATIAL GATE DETAIL native={native} real-ID-or-XY keeper-heart unknown duplicate stale raw foreign padding undo compiler");
        }
    }

    public static void RunSpatialRectangles()
    {
        VerifySpatialSource();
        foreach (var native in new[] { false, true })
        {
            var doc = NativeMapDocument.CreateNewMap(new("Rectangle", "Rectangle", 20, 20, NativeFormat: native)); doc.SetHistoryDepth(65535);
            var type = LegacyEventSchemas.All.Single(item => item.Name == "ACTION_CHANGESQUARE");
            var context = doc.CreateScenarioCreationContext(); var before = Snapshot(doc);
            using (var cancel = doc.CreateScenarioCreationSession(context, 0xD6, type))
            { doc.UpdateScenarioCreationRectangle(cancel, new(3, 5, 2, 3)); doc.PrepareScenarioCreation(cancel); Exact(before); }
            using (var retained = doc.CreateScenarioCreationSession(context, 0xD6, type))
            {
                Expect(retained.Rectangle == new TileRect(3, 5, 2, 3), "Cancel lost original ChangeRect/context.");
                var stale = doc.PrepareScenarioCreation(retained);
                doc.UpdateScenarioCreationRectangle(retained, new(3, 5, 3, 2)); Reject(() => doc.ApplyScenarioCreation(stale)); Exact(before);
                foreach (var bad in new[] { new TileRect(-1, 0, 1, 1), new(0, 0, 0, 1), new(19, 0, 2, 1) }) Reject(() => doc.UpdateScenarioCreationRectangle(retained, bad));
                var plan = doc.PrepareScenarioCreation(retained);
                var sourcePayload = retained.Record.File.Data.Span.Slice(retained.Record.Offset + (native ? 8 : 16), native ? 8 : 32).ToArray();
                var last = doc.ApplyScenarioCreation(plan);
                Expect(plan.EventIDs.Count == 6 && ScriptGraph.ID(last) == plan.EventIDs[^1] && doc.GetHistory().Undo.Count == 1, "Rectangle did not create row-major chain in one edit.");
                var graph = ScriptGraph.Build(doc);
                for (var i = 0; i < 6; i++)
                {
                    var item = graph.Events[plan.EventIDs[i]];
                    var draft = doc.CreateScenarioParameterDraft(item.Record);
                    Expect(draft.Values[(native ? "Param" : "param") + 5] == (4 + i % 3).ToString()
                        && draft.Values[(native ? "Param" : "param") + 7] == (6 + i / 3).ToString(), "Rectangle coordinate order differs from source x-first Save.");
                    Expect(item.Edges.Single().Target == (i == 5 ? 0 : plan.EventIDs[i + 1]), "Rectangle chain lost next link.");
                    Expect(item.Record.File.Data.Span[item.Record.Offset + (native ? 23 : 55)] == 1, "Rectangle changed repeat.");
                    var expectedPayload = sourcePayload.ToArray();
                    if (native)
                    {
                        BinaryPrimitives.WriteUInt16LittleEndian(expectedPayload.AsSpan(4), (ushort)(4 + i % 3));
                        BinaryPrimitives.WriteUInt16LittleEndian(expectedPayload.AsSpan(6), (ushort)(6 + i / 3));
                    }
                    else
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(expectedPayload.AsSpan(16), (uint)(4 + i % 3));
                        BinaryPrimitives.WriteUInt32LittleEndian(expectedPayload.AsSpan(24), (uint)(6 + i / 3));
                    }
                    Expect(item.Record.File.Data.Span.Slice(item.Record.Offset + (native ? 8 : 16), native ? 8 : 32).SequenceEqual(expectedPayload), "Rectangle copied or normalized another operand.");
                }
                if (!native)
                {
                    var compiled = doc.CompileLegacyProject("RectangleCompiled", NativeMapDocument.Create(3, 3, "Basis")); var compiledGraph = ScriptGraph.Build(compiled);
                    for (var i = 0; i < 6; i++)
                    {
                        var cell = compiledGraph.Events[plan.EventIDs[i]].Record;
                        Expect(cell.File.Data.Span.Slice(cell.Offset + 8, 8).SequenceEqual(new byte[] { 6, 2, 0, 0, (byte)(4 + i % 3), 0, (byte)(6 + i / 3), 0 }), "Rectangle compiler lost literal terrain/owner/coordinates.");
                    }
                }
                var after = Snapshot(doc); doc.Undo(); Exact(before); doc.Redo(); Exact(after); doc.Undo(); Exact(before);
            }
            using (var again = doc.CreateScenarioCreationSession(context, 0xD6, type))
            {
                Expect(again.Rectangle is null && again.Record.Read(again.Record.Fields.Single(field => field.Name == (native ? "Param5" : "param5"))) == "4"
                    && again.Record.Read(again.Record.Fields.Single(field => field.Name == (native ? "Param7" : "param7"))) == "6", "Finish retained ChangeRect or last-cell coordinates instead of resetting to first.");
            }
            var condition = doc.AddScenarioEvent(0xD5, new(0, 0), LegacyEventSchemas.All.Single(item => item.Name == "TRIGGER_TIMEELAPSED"));
            var tail = doc.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(item => item.Name == "ACTION_VICTORY"));
            var child = condition.Fields.Single(field => field.Name is "ChildAnd" or "and");
            doc.SetScriptLink(condition, child, ScriptGraph.ID(tail));
            var insertion = ScriptGraph.Build(doc).Events[ScriptGraph.ID(condition)].Edges.Single(edge => edge.Field.Name == child.Name);
            var graphBefore = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
            using (var inserted = doc.CreateScenarioCreationSession(context, 0xD6, type, insertion))
            {
                doc.UpdateScenarioCreationRectangle(inserted, new(2, 4, 2, 2)); var plan = doc.PrepareScenarioCreation(inserted); Exact(graphBefore);
                doc.ApplyScenarioCreation(plan); var graph = ScriptGraph.Build(doc);
                Expect(graph.Events[ScriptGraph.ID(condition)].Edges.Single(edge => edge.Field.Name == child.Name).Target == plan.EventIDs[0]
                    && graph.Events[plan.EventIDs[^1]].Edges.Single().Target == ScriptGraph.ID(tail), "Rectangle insertion lost original prefix/tail.");
                Expect(doc.GetHistory().Undo.Count == history + 1, "Rectangle insertion split history.");
                var after = Snapshot(doc); doc.Undo(); Exact(graphBefore); doc.Redo(); Exact(after); doc.Undo(); Exact(graphBefore);
            }
            using (var stale = doc.CreateScenarioCreationSession(context, 0xD6, type))
            {
                doc.UpdateScenarioCreationRectangle(stale, new(1, 1, 2, 2)); var plan = doc.PrepareScenarioCreation(stale);
                doc.Paint([new(1, 1)], new(1, 2, 3, 4)); doc.Undo(); Reject(() => doc.ApplyScenarioCreation(plan)); Exact(graphBefore);
            }
            using (var closed = doc.CreateScenarioCreationSession(context, 0xD6, type))
            { var plan = doc.PrepareScenarioCreation(closed); closed.Dispose(); Reject(() => doc.ApplyScenarioCreation(plan)); Exact(graphBefore); }
            using (var raw = doc.CreateScenarioCreationSession(context, 0xD6, type))
            {
                var plan = doc.PrepareScenarioCreation(raw);
                var other = NativeMapDocument.CreateNewMap(new("ForeignRectangle", "ForeignRectangle", 20, 20, NativeFormat: native));
                Reject(() => other.ApplyScenarioCreation(plan));
                Mutable(doc.TileFile)[36] ^= 1; Reject(() => doc.ApplyScenarioCreation(plan)); Mutable(doc.TileFile)[36] ^= 1; Exact(graphBefore);
                doc.SelectScenarioCreationType(raw, LegacyEventSchemas.All.Single(item => item.Name == "ACTION_CHANGEOWNER"));
                Expect(raw.Rectangle is null, "Another type retained rectangle mode.");
                Reject(() => doc.UpdateScenarioCreationRectangle(raw, new(1, 1, 2, 2)));
            }
            var large = NativeMapDocument.CreateNewMap(new("Capacity", "Capacity", 300, 300, NativeFormat: native));
            using (var overflow = large.CreateScenarioCreationSession(large.CreateScenarioCreationContext(), 0xD6, type))
            { large.UpdateScenarioCreationRectangle(overflow, new(0, 0, 300, 300)); var unchanged = Snapshot(large); Reject(() => large.PrepareScenarioCreation(overflow)); Exact(unchanged); }
            Console.WriteLine($"SPATIAL RECTANGLE DETAIL native={native} x-first ordered-chain preserved-tail one-undo cancel-retention finish-reset metadata stale closed capacity");
        }
    }
}
