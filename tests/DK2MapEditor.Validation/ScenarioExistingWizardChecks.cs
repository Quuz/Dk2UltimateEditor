using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    internal static void RunExistingWizard()
    {
        using var original = JsonDocument.Parse(typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioExistingWizardSource.json")!);
        var oracle = original.RootElement;
        Expect(oracle.GetProperty("Functions").EnumerateObject().Count() == 19 && oracle.GetProperty("ResourcePages").EnumerateObject().Count() == 3, "Original existing wizard evidence is incomplete.");
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Source"))) root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Original source missing.");
        foreach (var input in oracle.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original existing-event wizard source changed.");
        using var defaults = JsonDocument.Parse(typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioCreationDefaults.json")!);
        var zero = defaults.RootElement.GetProperty("CreationAndSaveFlow").EnumerateObject().SelectMany(item => item.Value.GetProperty("ZeroOnSaveTypes").EnumerateArray().Select(value => value.GetString()!)).ToHashSet();
        Expect(zero.Count == 26, "Original Save cleanup count differs.");
        var scopeSource = File.ReadAllText(Path.Combine(root, "Source/Sources/Datas/Datas_Triggers.c"));
        Expect(System.Text.RegularExpressions.Regex.IsMatch(scopeSource, @"\{TRIGGER_CREATURES,\s*szTriggersTypeCreatures,\s*7104,\s*SCOPE_PLAYER\}"), "Independent CREATURES player-only source scope differs.");
        foreach (var native in new[] { false, true })
        {
            var paths = 0;
            foreach (var page in pages)
            {
                var doc = NativeMapDocument.CreateNewMap(new("Existing", "Existing", 12, 12, NativeFormat: native)); doc.SetHistoryDepth(65535);
                var record = Add(doc, native, page); var before = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
                var graph = ScriptGraph.Build(doc); var id = ScriptGraph.ID(record); var count = graph.Events.Count;
                var context = doc.CreateScenarioCreationContext();
                using (var session = doc.CreateScenarioEditingSession(context, record))
                {
                    Expect(session.IsEditingExisting && session.EventID == id && ScriptGraph.Build(session.WorkingDocument).Events.Count == count, "Modify fabricated another event.");
                    Expect(session.Record.File.Data.Span.Slice(session.Record.Offset, session.Record.Length).SequenceEqual(record.File.Data.Span.Slice(record.Offset, record.Length)), "Modify did not start from exact imported bytes.");
                    doc.SelectScenarioCreationType(session, session.Type); Exact(before);
                    var expected = before.ToDictionary(item => item.Key, item => item.Value.ToArray());
                    if (zero.Contains(page.Type)) Array.Clear(expected[record.File], record.Offset + (native ? 8 : 16), native ? 8 : 32);
                    var plan = doc.PrepareScenarioCreation(session); Exact(before);
                    var saved = doc.ApplyScenarioCreation(plan); Exact(expected);
                    Expect(ScriptGraph.ID(saved) == id && saved.Offset == record.Offset && saved.Length == record.Length && ScriptGraph.Build(doc).Events.Count == count, "Modify changed event identity, position or collection length.");
                    Expect(doc.GetHistory().Undo.Count == history + (zero.Contains(page.Type) ? 1 : 0), "Existing Finish split history or consumed no-op Undo.");
                    if (zero.Contains(page.Type)) { doc.Undo(); Exact(before); doc.Redo(); Exact(expected); doc.Undo(); Exact(before); }
                }
                paths++;
            }
            var document = NativeMapDocument.CreateNewMap(new("ExistingFlow", "ExistingFlow", 20, 20, NativeFormat: native)); document.SetHistoryDepth(65535);
            var condition = document.AddScenarioEvent(0xD5, new(0, 0)); var conditionID = ScriptGraph.ID(condition);
            var action = Add(document, native, pages.Single(page => page.Type == "ACTION_SAY")); var actionID = ScriptGraph.ID(action);
            var beforeFlow = Snapshot(document); var contextFlow = document.CreateScenarioCreationContext(); var flowHistory = document.GetHistory().Undo.Count;
            using (var canceled = document.CreateScenarioEditingSession(contextFlow, condition))
            {
                document.SelectScenarioCreationType(canceled, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
                var draft = canceled.WorkingDocument.CreateScenarioParameterDraft(canceled.Record);
                canceled.WorkingDocument.ApplyScenarioParameterDraft(canceled.WorkingDocument.UpdateScenarioControlPage(draft, [new(native ? "Param5" : "param5", "123")]));
                document.StoreScenarioCreationRepeat(canceled, ScenarioRepeatMode.Count, "254");
                canceled.WorkingDocument.SetScenarioName(ScenarioNameKind.Timer, 0, "CanceledEdit");
            }
            Exact(beforeFlow);
            using (var add = document.CreateScenarioCreationSession(contextFlow, 0xD5))
                Expect(add.Type.Name == "TRIGGER_TIMEELAPSED" && add.Record.Read(add.Record.Fields.Single(field => field.Name == (native ? "Param5" : "param5"))) == "123" && add.Record.File.Data.Span[add.Record.Offset + (native ? 23 : 55)] == 254, "Submitted canceled Modify did not feed subsequent Add.");
            using (var reopen = document.CreateScenarioEditingSession(contextFlow, ScriptGraph.Build(document).Events[conditionID].Record))
                Expect(reopen.Type.Name == "TRIGGER_VARIABLE", "Reopening Modify used remembered creation instead of selected live event.");
            using (var changed = document.CreateScenarioEditingSession(contextFlow, ScriptGraph.Build(document).Events[conditionID].Record))
            {
                var target = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
                document.SelectScenarioCreationType(changed, target);
                var draft = changed.WorkingDocument.CreateScenarioParameterDraft(changed.Record);
                changed.WorkingDocument.ApplyScenarioParameterDraft(changed.WorkingDocument.UpdateScenarioControlPage(draft, [new(native ? "Param5" : "param5", "123")]));
                document.StoreScenarioCreationRepeat(changed, ScenarioRepeatMode.Count, "254");
                var expected = beforeFlow.ToDictionary(item => item.Key, item => item.Value.ToArray());
                if (native)
                {
                    var payload = defaults.RootElement.GetProperty("CompilerPadding").GetProperty("TRIGGER_TIMEELAPSED").EnumerateArray().Select(item => item.GetByte()).ToArray();
                    // Original compiler: comparison BYTE at8, duration LONG at12.
                    payload[0] = 5; BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4), 123); payload.CopyTo(expected[condition.File], condition.Offset + 8);
                }
                else
                {
                    var values = defaults.RootElement.GetProperty("Pages").EnumerateArray().Single(item => item.GetProperty("Type").GetString() == "TRIGGER_TIMEELAPSED").GetProperty("Values");
                    for (var number = 1; number <= 8; number++) BinaryPrimitives.WriteUInt32LittleEndian(expected[condition.File].AsSpan(condition.Offset + 16 + (number - 1) * 4), number == 5 ? 123u : unchecked((uint)values.GetProperty(number.ToString()).GetInt64()));
                }
                expected[condition.File][condition.Offset + (native ? 22 : 54)] = (byte)target.Type;
                expected[condition.File][condition.Offset + (native ? 23 : 55)] = 254;
                var saved = document.ApplyScenarioCreation(document.PrepareScenarioCreation(changed)); Exact(expected);
                Expect(ScriptGraph.ID(saved) == conditionID && EntityReferences.EventType(saved) == target && document.GetHistory().Undo.Count == flowHistory + 1, "Changed existing type/parameters/repeat were not one same-identity edit.");
                var directory = Path.Combine(Path.GetTempPath(), "DK2ExistingWizard", Guid.NewGuid().ToString("N"));
                var reopened = NativeMapDocument.Load(document.SaveCopy(directory));
                var reopenedEvent = ScriptGraph.Build(reopened).Events[conditionID];
                Expect(reopenedEvent.Type == target && reopenedEvent.Record.File.Data.Span.Slice(reopenedEvent.Record.Offset, reopenedEvent.Record.Length).SequenceEqual(saved.File.Data.Span.Slice(saved.Offset, saved.Length)), "Existing type/parameters/repeat changed on save/reopen.");
                if (!native)
                {
                    var compiled = document.CompileLegacyProject("ExistingCompiled", NativeMapDocument.Create(20, 20, "Basis"));
                    var compiledEvent = ScriptGraph.Build(compiled).Events[conditionID];
                    Expect(compiledEvent.Type == target && compiledEvent.Record.File.Data.Span[compiledEvent.Record.Offset + 23] == 254 && compiledEvent.Record.File.Data.Span[compiledEvent.Record.Offset + 8] == 5 && BinaryPrimitives.ReadUInt32LittleEndian(compiledEvent.Record.File.Data.Span[(compiledEvent.Record.Offset + 12)..]) == 123, "Existing type/repeat/duration compiler literal differs.");
                }
                document.Undo(); Exact(beforeFlow); document.Redo(); Exact(expected); document.Undo(); Exact(beforeFlow);
            }
            using (var edit = document.CreateScenarioEditingSession(contextFlow, action))
            {
                var privateDoc = edit.WorkingDocument;
                var draft = privateDoc.CreateScenarioParameterDraft(edit.Record);
                privateDoc.ApplyScenarioParameterDraft(privateDoc.UpdateScenarioControlPage(draft, [new(native ? "Param1" : "param1", "2"), new(native ? "Param5" : "param5", "1")]));
                privateDoc.SetScenarioName(ScenarioNameKind.Variable, 0, "EditedAlias");
                var plan = document.PrepareScenarioCreation(edit); Exact(beforeFlow);
                var saved = document.ApplyScenarioCreation(plan);
                Expect(ScriptGraph.ID(saved) == actionID && document.GetHistory().Undo.Count == flowHistory + 1 && document.ReadScenarioNames().Slots.Single(slot => slot.Kind == ScenarioNameKind.Variable && slot.Index == 0).StoredName == "EditedAlias", "Existing event/alias Finish was not one atomic edit.");
                var bytes = saved.File.Data.Span;
                Expect((native ? BinaryPrimitives.ReadUInt16LittleEndian(bytes[(saved.Offset + 8)..]) : BinaryPrimitives.ReadUInt32LittleEndian(bytes[(saved.Offset + 16)..])) == 2, "Literal saved speech ID differs.");
                Expect((native ? bytes[saved.Offset + 12] : BinaryPrimitives.ReadUInt32LittleEndian(bytes[(saved.Offset + 32)..])) == 1 && bytes[saved.Offset + (native ? 23 : 55)] == 255, "Literal flag/repeat lost imported action bytes.");
                var after = Snapshot(document); document.Undo(); Exact(beforeFlow); document.Redo(); Exact(after); document.Undo(); Exact(beforeFlow);
            }
            // A shared condition is restricted by every actual linked root.
            if (!native)
                foreach (var scope in new[] { 1, 32 })
                { var link = document.AddLegacyRecord("TriggerLink", new(0, 0)); document.SetField(link, link.Fields.Single(field => field.Name == "node.type"), scope.ToString()); }
            var roots = ScriptGraph.Build(document).Roots;
            var player = roots.First(edge => edge.Scope == 1); var level = roots.First(edge => edge.Scope == 32);
            document.SetScriptLink(player.Owner, player.Field, conditionID);
            var playerOnly = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_CREATURES");
            using (var playerEdit = document.CreateScenarioEditingSession(contextFlow, ScriptGraph.Build(document).Events[conditionID].Record))
                Expect(playerEdit.AllowedTypes.Contains(playerOnly), "Original player-only type was excluded from player root.");
            document.SetScriptLink(level.Owner, level.Field, conditionID);
            var linkedBefore = Snapshot(document);
            using (var shared = document.CreateScenarioEditingSession(contextFlow, ScriptGraph.Build(document).Events[conditionID].Record))
            {
                Expect(!shared.AllowedTypes.Contains(playerOnly), "Shared event ignored its second root scope.");
                Reject(() => document.SelectScenarioCreationType(shared, playerOnly)); Exact(linkedBefore);
                shared.WorkingDocument.ChangeScenarioEventType(shared.Record, playerOnly);
                Reject(() => document.PrepareScenarioCreation(shared)); Exact(linkedBefore);
            }
            using (var rectangle = document.CreateScenarioEditingSession(contextFlow, ScriptGraph.Build(document).Events[actionID].Record))
            {
                document.SelectScenarioCreationType(rectangle, LegacyEventSchemas.All.Single(type => type.Name == "ACTION_CHANGESQUARE"));
                Reject(() => document.UpdateScenarioCreationRectangle(rectangle, new(1, 1, 2, 2))); Exact(linkedBefore);
            }
            foreach (var guard in new[] { "unrelated", "identity", "raw", "late", "closed", "owner-undo", "foreign" })
            {
                using var guarded = document.CreateScenarioEditingSession(contextFlow, ScriptGraph.Build(document).Events[actionID].Record);
                var plan = document.PrepareScenarioCreation(guarded);
                if (guard == "unrelated") { Mutable(guarded.WorkingDocument.TileFile)[36] ^= 1; Reject(() => document.PrepareScenarioCreation(guarded)); }
                if (guard == "identity") { var field = guarded.Record.Fields.Single(field => field.Name is "ID" or "id"); guarded.WorkingDocument.SetField(guarded.Record, field, "60000"); Reject(() => document.PrepareScenarioCreation(guarded)); }
                if (guard == "raw") { Mutable(document.TileFile)[36] ^= 1; Reject(() => document.ApplyScenarioCreation(plan)); Mutable(document.TileFile)[36] ^= 1; }
                if (guard == "late") { guarded.WorkingDocument.SetScenarioName(ScenarioNameKind.Variable, 0, "Late"); Reject(() => document.ApplyScenarioCreation(plan)); }
                if (guard == "closed") { guarded.Dispose(); Reject(() => document.ApplyScenarioCreation(plan)); }
                if (guard == "owner-undo") { document.Paint([new(1, 1)], new(1, 2, 3, 4)); document.Undo(); Reject(() => document.ApplyScenarioCreation(plan)); }
                if (guard == "foreign") { var other = NativeMapDocument.CreateNewMap(new("Other", "Other", 8, 8, NativeFormat: native)); Reject(() => other.ApplyScenarioCreation(plan)); }
                Exact(linkedBefore);
            }
            Expect(paths == 103, "Incomplete existing-event type paths.");
            Console.WriteLine($"EXISTING WIZARD DETAIL native={native} types=103 save-zero=26 same-ID position links imports no-op atomic type alias repeat cancel-to-add source-reopen compiler shared-scopes rectangle-disabled stale raw foreign closed");
        }
    }
}
