using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    internal static void RunCreationSession()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioCreationDefaults.json")!;
        using var fixture = JsonDocument.Parse(stream); var oracle = fixture.RootElement;
        var defaults = oracle.GetProperty("Pages").EnumerateArray().ToDictionary(item => item.GetProperty("Type").GetString()!,
            item => Enumerable.Range(1, 8).Select(number => unchecked((uint)item.GetProperty("Values").GetProperty(number.ToString()).GetInt64())).ToArray());
        var flow = oracle.GetProperty("CreationAndSaveFlow");
        var zero = flow.EnumerateObject().SelectMany(item => item.Value.GetProperty("ZeroOnSaveTypes").EnumerateArray().Select(value => value.GetString()!)).ToHashSet();
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Source"))) root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Original source missing.");
        foreach (var input in oracle.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original wizard input changed.");
        Expect(oracle.GetProperty("WizardFlow").GetProperty("SelectionContext").GetString()!.Contains("GlobalAlloc(GPTR,sizeof(TRIGGERSCTX))"), "Selection-dialog retained context is not independently captured.");
        foreach (var native in new[] { false, true })
        {
            var document = NativeMapDocument.CreateNewMap(new("Wizard", "Wizard", 20, 20, NativeFormat: native)); document.SetHistoryDepth(65535);
            var before = Snapshot(document); var revision = document.EditRevision; var creations = 0;
            foreach (var page in pages)
            {
                var type = LegacyEventSchemas.All.Single(item => item.Name == page.Type);
                var kind = page.Signature == 213 ? "Trigger" : "Action";
                var initial = flow.GetProperty(kind);
                var values = initial.GetProperty("InitialValues").EnumerateArray().Select(item => item.GetUInt32()).ToArray();
                if (page.Type != initial.GetProperty("InitialType").GetString() && defaults.TryGetValue(page.Type, out var defined)) values = defined;
                using var session = document.CreateScenarioCreationSession(document.CreateScenarioCreationContext(), page.Signature);
                document.SelectScenarioCreationType(session, type); Exact(before);
                Expect(document.EditRevision == revision && document.GetHistory().Undo.Count == 0, "Private type selection touched the live map.");
                CheckPayload(session.Record, page, values, native);
                var temporary = session.Record.File.Data.Span.Slice(session.Record.Offset, session.Record.Length).ToArray();
                var plan = document.PrepareScenarioCreation(session); Exact(before);
                Expect(temporary.AsSpan().SequenceEqual(session.Record.File.Data.Span.Slice(session.Record.Offset, session.Record.Length)), "Preparing creation normalized the temporary event.");
                var added = document.ApplyScenarioCreation(plan);
                if (zero.Contains(page.Type)) values = new uint[8];
                CheckPayload(added, page, values, native);
                Expect(added.File.Data.Span[added.Offset + (native ? 23 : 55)] == 1 && document.GetHistory().Undo.Count == 1, "Finish did not create one atomic source-default event.");
                var after = Snapshot(document); document.Undo(); Exact(before); document.Redo(); Exact(after); document.Undo(); Exact(before);
                revision = document.EditRevision; creations++;
            }
            // Cancel remembers submitted temporary type/values, but never aliases or live edits.
            var context = document.CreateScenarioCreationContext();
            using (var canceled = document.CreateScenarioCreationSession(context, 0xD5))
            {
                document.SelectScenarioCreationType(canceled, LegacyEventSchemas.All.Single(item => item.Name == "TRIGGER_TIMEELAPSED"));
                var draft = canceled.WorkingDocument.CreateScenarioParameterDraft(canceled.Record);
                canceled.WorkingDocument.ApplyScenarioParameterDraft(canceled.WorkingDocument.UpdateScenarioControlPage(draft, [new(native ? "Param5" : "param5", "123")]));
                document.StoreScenarioCreationRepeat(canceled, ScenarioRepeatMode.Count, "254");
                canceled.WorkingDocument.SetScenarioName(ScenarioNameKind.Timer, 0, "Canceled");
            }
            Exact(before); Expect(document.ReadScenarioNames().Slots.Single(item => item.Kind == ScenarioNameKind.Timer && item.Index == 0).StoredName == "", "Cancel leaked aliases.");
            using (var again = document.CreateScenarioCreationSession(context, 0xD5))
            {
                Expect(again.Type.Name == "TRIGGER_TIMEELAPSED" && again.Record.Read(again.Record.Fields.Single(field => field.Name == (native ? "Param5" : "param5"))) == "123", "Repeated Add lost the canceled submitted temporary parameters.");
                Expect(again.Record.File.Data.Span[again.Record.Offset + (native ? 23 : 55)] == 254, "Repeated Add lost submitted repeat.");
                var stale = document.PrepareScenarioCreation(again);
                document.StoreScenarioCreationRepeat(again, ScenarioRepeatMode.Infinite, ""); Reject(() => document.ApplyScenarioCreation(stale)); Exact(before);
                again.WorkingDocument.SetScenarioName(ScenarioNameKind.Variable, 0, "Counter");
                var record = document.ApplyScenarioCreation(document.PrepareScenarioCreation(again));
                Expect(document.ReadScenarioNames().Slots.Single(item => item.Kind == ScenarioNameKind.Variable && item.Index == 0).StoredName == "Counter", "Finish lost nested aliases.");
                Expect(document.GetHistory().Undo.Count == 1 && record.File.Data.Span[record.Offset + (native ? 23 : 55)] == 255, "Finish split private alias/repeat/event history.");
                var after = Snapshot(document); document.Undo(); Exact(before);
                Expect(document.ReadScenarioNames().Slots.Single(item => item.Kind == ScenarioNameKind.Variable && item.Index == 0).StoredName == "", "Undo retained native alias metadata.");
                document.Redo(); Exact(after); document.Undo(); Exact(before);
            }
            // Reject unrelated private edits, changed collection topology and stale/foreign owners.
            using (var unrelated = document.CreateScenarioCreationSession(context, 0xD5))
            { var tile = unrelated.WorkingDocument.TileFile; Mutable(tile)[36] ^= 1; Reject(() => document.PrepareScenarioCreation(unrelated)); Exact(before); }
            using (var staleOwner = document.CreateScenarioCreationSession(context, 0xD5))
            {
                var plan = document.PrepareScenarioCreation(staleOwner);
                Mutable(document.TileFile)[36] ^= 1; Reject(() => document.ApplyScenarioCreation(plan)); Mutable(document.TileFile)[36] ^= 1;
                var other = NativeMapDocument.CreateNewMap(new("Other", "Other", 8, 8, NativeFormat: native));
                Reject(() => other.ApplyScenarioCreation(plan)); Exact(before);
            }
            using (var closed = document.CreateScenarioCreationSession(context, 0xD5))
            { var plan = document.PrepareScenarioCreation(closed); closed.Dispose(); Reject(() => document.ApplyScenarioCreation(plan)); Exact(before); }
            using (var lifetime = document.CreateScenarioCreationSession(context, 0xD5))
            {
                var plan = document.PrepareScenarioCreation(lifetime);
                document.Paint([new(1, 1)], new(1, 2, 3, 4)); document.Undo();
                Reject(() => document.ApplyScenarioCreation(plan)); Exact(before);
            }
            // Existing graph insertion keeps links in the private result until Finish.
            if (!native) document.AddLegacyRecord("TriggerLink", new(0, 0));
            var edge = ScriptGraph.Build(document).Roots.First(item => item.Scope == 32);
            var graphBefore = Snapshot(document); var history = document.GetHistory().Undo.Count;
            using (var inserted = document.CreateScenarioCreationSession(context, 0xD5,
                LegacyEventSchemas.All.Single(item => item.Name == "TRIGGER_TIMEELAPSED"), edge))
            {
                Expect(!inserted.AllowedTypes.Any(item => item.Name == "TRIGGER_DOORDESTROYED"), "Creation type list ignored the original map scope.");
                Reject(() => document.SelectScenarioCreationType(inserted, LegacyEventSchemas.All.Single(item => item.Name == "TRIGGER_DOORDESTROYED")));
                Exact(graphBefore); var added = document.ApplyScenarioCreation(document.PrepareScenarioCreation(inserted));
                var graph = ScriptGraph.Build(document);
                Expect(graph.Roots.Single(item => item.Scope == 32).Target == ScriptGraph.ID(added) && document.GetHistory().Undo.Count == history + 1, "Private insertion lost its root or split history.");
                var after = Snapshot(document); document.Undo(); Exact(graphBefore); document.Redo(); Exact(after);
            }
            Reject(() => document.CreateScenarioCreationSession(context, 0xD5,
                LegacyEventSchemas.All.Single(item => item.Name == "TRIGGER_DOORDESTROYED"), ScriptGraph.Build(document).Roots.First(item => item.Scope == 32)));
            Expect(creations == 103, "Not all original wizard type paths were checked.");
            Console.WriteLine($"CREATION SESSION DETAIL native={native} types=103 defaults=70 save-zero=26 cancel-retention aliases insertion guards exact-undo");
        }

        void CheckPayload(NativeRecord record, Page page, uint[] values, bool native)
        {
            if (!native)
            {
                for (var index = 0; index < 8; index++) Expect(BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[(record.Offset + 16 + index * 4)..]) == values[index], "Wizard logical parameter differs from independently captured original defaults/Save.");
                return;
            }
            var payload = oracle.GetProperty("CompilerPadding").GetProperty(page.Type).EnumerateArray().Select(item => item.GetByte()).ToArray();
            foreach (var field in page.Native.Where(field => field.Name.StartsWith("Param", StringComparison.Ordinal)))
            {
                var value = values[int.Parse(field.Name[5..], CultureInfo.InvariantCulture) - 1]; var offset = field.Offset - 8;
                if (field.Size == 1) payload[offset] = unchecked((byte)value);
                else if (field.Size == 2) BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(offset), unchecked((ushort)value));
                else BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(offset), value);
            }
            if (page.Type == "ACTION_SHOWCRYSTAL") Array.Fill(payload, (byte)255, 4, 4); // original absent-object X/Y sentinel
            Expect(record.File.Data.Span.Slice(record.Offset + 8, 8).SequenceEqual(payload), "Wizard compiler bytes differ from independent original fixture: " + page.Type);
        }
    }
}
