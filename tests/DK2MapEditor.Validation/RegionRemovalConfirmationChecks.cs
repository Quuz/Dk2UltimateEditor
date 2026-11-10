using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class RegionRemovalConfirmationChecks
{
    private const uint OtherBits = 0xA5000004;
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static NativeMapDocument New(bool native) => NativeMapDocument.CreateNewMap(new("RemovalRequest", "RemovalRequest", 24, 24, NativeFormat: native));
    private static NativeRecord Add(NativeMapDocument doc, MapRegionKind kind, int x = 3) => doc.AddMapRegion(kind, new(x, 4, 1, 1));
    private static Dictionary<NativeFile, byte[]> Bytes(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> before) => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "A request changed game buffers before acceptance or failed to restore them.");
    private static void Refused(Action operation)
    {
        try { operation(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException) { return; }
        throw new InvalidOperationException("A stale, nested, foreign or consumed removal request was accepted.");
    }
    private static void OnlyRemoved(NativeMapDocument doc, Dictionary<NativeFile, byte[]> before, params NativeRecord[] rows)
    {
        foreach (var (file, data) in before)
        {
            var removed = rows.Where(row => row.File == file).OrderByDescending(row => row.Offset).ToArray(); var expected = data.ToArray();
            foreach (var row in removed) expected = expected.Take(row.Offset).Concat(expected.Skip(row.Offset + row.Length)).ToArray();
            if (removed.Length != 0)
            {
                var length = removed.Sum(row => row.Length);
                if (doc.IsLegacyProject) BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(8), (uint)expected.Length - 12);
                else
                {
                    var stored = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(8));
                    BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(8), stored >= length ? stored - (uint)length : (uint)expected.Length);
                    foreach (var (offset, delta) in new[] { (20, removed.Length), (52, length) })
                        BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(offset), BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset)) - (uint)delta);
                }
            }
            Expect(file.Data.Span.SequenceEqual(expected), "Confirmed removal changed unexpected records/header bytes.");
        }
    }
    public static void Run()
    {
        foreach (var native in new[] { false, true }) foreach (var kind in Enum.GetValues<MapRegionKind>()) Matrix(native, kind);
        Lifetime(); FailuresAndObservers(); PatrolRewiring(); UsedAreaRemovalChecks.Run();
        Console.WriteLine("REGION REMOVAL CONFIRMATION DETAIL six native/project x area/gate/effect matrices; literal0x10/defaultYes, No/remember/close/reset/shared-bit lifecycle, immutable batch selection, canonical field-schema snapshots, complete removal/header bytes, one count/Undo/notification, history0, stale/raw/foreign/mixed/nested guards and failure/committed-observer distinction");
    }
    private static void Matrix(bool native, MapRegionKind kind)
    {
        var doc = New(native); var a = Add(doc, kind); _ = Add(doc, kind, 8); var c = Add(doc, kind, 13);
        using var policy = new EditorRegionRemovalPolicy(OtherBits); var before = Bytes(doc); var count = doc.EditorChangeCount; var revision = doc.EditRevision; var history = doc.GetHistory().Undo.Count;
        using (var no = policy.Begin(doc, [a])) { Expect(no.RequiresPrompt && no.CanDecline(false) && !no.CanDecline(true), "Source No/remember state differs."); Expect(!no.Decline(true) && no.IsCurrent, "Checked No consumed a request."); Expect(no.Decline(), "Normal No was refused."); Refused(() => no.Accept()); }
        using (var close = policy.Begin(doc, [a])) { Expect(close.IsCurrent && close.Count == 1 && close.Kind == kind, "Request selection was not captured."); }
        Exact(before); Expect(policy.Flags == OtherBits && doc.EditorChangeCount == count && doc.EditRevision == revision && doc.GetHistory().Undo.Count == history, "No/close changed flags, counters or history.");
        var input = new List<NativeRecord> { c, a }; using var yes = policy.Begin(doc, input); input.Clear();
        var notifications = 0; EventHandler handler = (_, _) => notifications++; doc.Changed += handler;
        try { Expect(yes.Count == 2 && yes.Accept(true) == 2, "Immutable batch selection was changed by its caller."); }
        finally { doc.Changed -= handler; }
        OnlyRemoved(doc, before, a, c);
        Expect(yes.IsAccepted && yes.IsClosed && yes.RemovedCount == 2 && policy.Flags == (OtherBits | 16) && policy.Suppressed, "Default Yes did not remember exactly bit0x10.");
        Expect(doc.EditorChangeCount == unchecked(count + 1) && doc.GetHistory().Undo.Count == history + 1 && notifications == 1, "The shared request added a second count/Undo/notification.");
        var after = Bytes(doc); doc.Undo(); Exact(before); Expect(policy.Suppressed, "Undo reversed the runtime confirmation preference."); doc.Redo(); Exact(after);
        Refused(() => yes.Accept());
        var otherDoc = New(!native); var otherKind = kind == MapRegionKind.Gate ? MapRegionKind.Effect : MapRegionKind.Gate; var row = Add(otherDoc, otherKind);
        using (var automatic = policy.Begin(otherDoc, [row])) { Expect(!automatic.RequiresPrompt, "Shared suppression was lost across region kind/document."); Expect(automatic.Accept() == 1, "Suppressed default Yes did not accept."); }
        Expect(policy.Flags == (OtherBits | 16), "An automatic reply cleared the remembered default.");
        policy.ResetSuppression(); Expect(policy.Flags == OtherBits, "Reset changed unrelated request bits.");
        var last = doc.EditorRegionRemovalRecords().Single(); doc.SetHistoryDepth(0);
        using (var depth0 = policy.Begin(doc, [last])) { Expect(depth0.RequiresPrompt, "Reset did not restore the prompt."); depth0.Accept(); }
        Expect(!doc.CanUndo && policy.Flags == OtherBits, "History0 retained an Undo or unchecked Yes changed flags.");
    }
    private static void Lifetime()
    {
        var doc = New(true); var gate = Add(doc, MapRegionKind.Gate); var area = Add(doc, MapRegionKind.Area, 8);
        using var policy = new EditorRegionRemovalPolicy(OtherBits);
        Refused(() => policy.Begin(doc, [])); Refused(() => policy.Begin(doc, [gate, gate])); Refused(() => policy.Begin(doc, [gate, area]));
        Refused(() => policy.Begin(doc, [Add(New(true), MapRegionKind.Gate)]));
        var objectRow = doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEOBJECT"), new(2, 2)); Refused(() => policy.Begin(doc, [objectRow]));
        var heart = doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEGATE"), new(2, 2)); doc.SetField(heart, heart.Fields.Single(field => field.Name == "bGateType"), "14"); Refused(() => policy.Begin(doc, [heart]));
        doc.Edit("Nested request guard", () => Refused(() => policy.Begin(doc, [gate])));
        using (doc.BeginLivePenStroke(new(2, 2, 1))) Refused(() => policy.Begin(doc, [gate]));
        using (var stale = policy.Begin(doc, [gate]))
        {
            Refused(() => policy.Begin(doc, [area])); doc.SetMapRegionBounds(gate, new(5, 6, 1, 1)); var changed = Bytes(doc);
            Expect(!stale.IsCurrent, "Changed map retained a current removal request."); Refused(() => stale.Accept(true)); Exact(changed); Expect(policy.Flags == OtherBits, "Stale acceptance remembered a reply.");
        }
        using (var reset = policy.Begin(doc, [gate])) { policy.ResetSuppression(); Expect(reset.IsClosed && !reset.IsCurrent, "Reset left an old request active."); Refused(() => reset.Accept()); }
        using (var raw = policy.Begin(doc, [gate]))
        {
            Expect(MemoryMarshal.TryGetArray(gate.File.Data, out var bytes), "Raw guard fixture has no buffer."); bytes.Array![bytes.Offset + gate.Offset + 8] ^= 1;
            Expect(!raw.IsCurrent, "Raw mutation without revision bypassed the fingerprint."); Refused(() => raw.Accept(true)); bytes.Array[bytes.Offset + gate.Offset + 8] ^= 1;
        }
        using var disposed = new EditorRegionRemovalPolicy(); var pending = disposed.Begin(doc, [gate]); disposed.Dispose(); Expect(pending.IsClosed, "Disposal retained an active request."); Refused(() => pending.Accept()); Refused(() => disposed.Begin(doc, [gate]));
    }
    private static void FailuresAndObservers()
    {
        // Unknown-layout guards are independent of the general
        // confirmation; remembering Yes occurs before a refused deletion.
        var doc = New(false); var area = Add(doc, MapRegionKind.Area);
        var rule = EntityReferences.EventRules.First(rule => rule.Kind == EntityKind.Area);
        var incoming = doc.AddScenarioEvent(rule.Event.RecordSignature, new(0, 0), rule.Event); doc.SetField(incoming, incoming.Fields.Single(field => field.Name == rule.Parameter), "1");
        doc.SetField(incoming, incoming.Fields.Single(field => field.Name == "ctrl"), "255");
        using var policy = new EditorRegionRemovalPolicy(OtherBits); var before = Bytes(doc); var count = doc.EditorChangeCount;
        using (var failure = policy.Begin(doc, [area])) { Refused(() => failure.Accept(true)); Expect(!failure.IsAccepted && failure.IsClosed && failure.RemovedCount == 0, "A rejected command was marked committed."); }
        Exact(before); Expect(doc.EditorChangeCount == count && policy.Flags == (OtherBits | 16), "Command failure changed map data or lost the earlier remembered reply.");
        var canonical = New(true); var target = Add(canonical, MapRegionKind.Area); var predecessor = Add(canonical, MapRegionKind.Area, 8);
        canonical.SetField(predecessor, predecessor.Fields.Single(field => field.Name == "bAreaNextID"), "1"); var unchanged = Bytes(canonical);
        Refused(() => canonical.DeleteRecord(target)); Exact(unchanged);
        using (var canonicalPolicy = new EditorRegionRemovalPolicy())
        using (var selection = canonicalPolicy.Begin(canonical, [target with { Fields = [] }]))
        {
            Expect(selection.Records[0].Fields.Any(field => field.Name == "bAreaID"), "Request trusted a caller's replacement field schema.");
            Expect(selection.Accept() == 1 && selection.IsAccepted, "Canonical area selection did not use the editor patrol-removal route.");
            var remaining = canonical.EditorRegionRemovalRecords().Single();
            Expect(remaining.Read(remaining.Fields.Single(field => field.Name == "bAreaNextID")) == "0", "Canonical selection did not rewire its predecessor.");
            canonical.Undo(); Exact(unchanged);
        }
        var committed = New(true); var gate = Add(committed, MapRegionKind.Gate); using var observerPolicy = new EditorRegionRemovalPolicy(OtherBits); using var request = observerPolicy.Begin(committed, [gate]);
        var prior = Bytes(committed); count = committed.EditorChangeCount;
        EventHandler observer = (_, _) => { Refused(observerPolicy.ResetSuppression); Refused(() => observerPolicy.Begin(committed, [gate])); request.Dispose(); throw new IOException("fixture committed observer"); };
        committed.Changed += observer;
        try { request.Accept(true); throw new InvalidOperationException("Observer failure was swallowed."); }
        catch (IOException error) when (error.Message == "fixture committed observer") { }
        finally { committed.Changed -= observer; }
        Expect(request.IsAccepted && request.IsClosed && request.RemovedCount == 1 && committed.EditorChangeCount == unchecked(count + 1), "A committed observer failure could be retried as an unaccepted command.");
        OnlyRemoved(committed, prior, gate); Refused(() => request.Accept()); committed.Undo(); Exact(prior); Expect(observerPolicy.Suppressed, "Undo of the command erased its runtime answer.");
    }

    private static void PatrolRewiring()
    {
        // Independent packed-buffer oracle: source unlinks each displayed row,
        // then rewires the first surviving physical predecessor only.
        uint[][] graphs = [[2, 3, 4, 0], [3, 3, 4, 0], [1, 1, 1, 1], [2, 1, 2, 0], [0, 0, 0, 0], [255, 3, 255, 3]];
        int[][] selections = [[0], [0, 2], [2, 0], [3, 1, 0], [0, 1, 2, 3]];
        var cases = 0;
        foreach (var native in new[] { false, true }) foreach (var graph in graphs) foreach (var selection in selections)
        {
            var doc = New(native); var areas = Enumerable.Range(0, 4).Select(index => Add(doc, MapRegionKind.Area, 2 + index * 4)).ToArray();
            var nextName = native ? "bAreaNextID" : "nextid";
            for (var i = 0; i < areas.Length; i++)
            {
                doc.SetField(areas[i], areas[i].Fields.Single(field => field.Name == nextName), graph[i].ToString());
                Expect(MemoryMarshal.TryGetArray(areas[i].File.Data, out var raw), "Patrol fixture has no buffer.");
                raw.Array![raw.Offset + areas[i].Offset + (native ? 27 : 105)] = (byte)(0xE0 + i);
            }
            var before = Bytes(doc); var expected = before.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            var live = Enumerable.Range(0, 4).ToList(); var next = graph.ToArray();
            foreach (var index in selection)
            {
                live.Remove(index);
                var predecessorIndex = live.FindIndex(candidate => next[candidate] == index + 1);
                if (predecessorIndex >= 0) next[live[predecessorIndex]] = next[index];
            }
            foreach (var index in live)
            {
                var data = expected[areas[index].File];
                if (native) data[areas[index].Offset + 31] = (byte)next[index];
                else BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(areas[index].Offset + 52), next[index]);
            }
            foreach (var file in doc.Files)
            {
                var rows = selection.Select(index => areas[index]).Where(row => row.File == file).OrderByDescending(row => row.Offset).ToArray();
                foreach (var row in rows) expected[file] = expected[file].Take(row.Offset).Concat(expected[file].Skip(row.Offset + row.Length)).ToArray();
                if (rows.Length == 0) continue;
                var data = expected[file]; var length = rows.Sum(row => row.Length);
                if (!native) BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), (uint)data.Length - 12);
                else foreach (var (offset, delta) in new[] { (8, length), (20, rows.Length), (52, length) })
                    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), BinaryPrimitives.ReadUInt32LittleEndian(before[file].AsSpan(offset)) - (uint)delta);
            }
            using var policy = new EditorRegionRemovalPolicy(); using var request = policy.Begin(doc, selection.Select(index => areas[index]).ToArray());
            var count = doc.EditorChangeCount; var history = doc.GetHistory().Undo.Count; var notifications = 0; EventHandler observer = (_, _) => notifications++; doc.Changed += observer;
            try { Expect(request.Accept() == selection.Length, "Patrol removal count differs."); }
            finally { doc.Changed -= observer; }
            Exact(expected);
            Expect(doc.EditorChangeCount == unchecked(count + 1) && doc.GetHistory().Undo.Count == history + 1 && notifications == 1, "Patrol rewiring added a second counter/history/notification.");
            doc.Undo(); Exact(before); doc.Redo(); Exact(expected); cases++;
        }
        // Later failure must roll back an earlier deletion and its predecessor edit.
        foreach (var native in new[] { false, true })
        {
            var doc = New(native); var first = Add(doc, MapRegionKind.Area); var blocked = Add(doc, MapRegionKind.Area, 8); var predecessor = Add(doc, MapRegionKind.Area, 13);
            doc.SetField(predecessor, predecessor.Fields.Single(field => field.Name == (native ? "bAreaNextID" : "nextid")), "1");
            var rule = EntityReferences.EventRules.First(rule => rule.Kind == EntityKind.Area);
            var incoming = doc.AddScenarioEvent(rule.Event.RecordSignature, new(0, 0), rule.Event);
            doc.SetField(incoming, incoming.Fields.Single(field => field.Name == (native ? "Param" + rule.Parameter[5..] : rule.Parameter)), "2");
            doc.Paint([new(19, 19)], new(5, 2, 1)); doc.Undo();
            var modelCount = typeof(NativeMapDocument).GetField("changeCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            modelCount.SetValue(doc, long.MaxValue); // fail at outer commit, after both deletions and predecessor rewiring
            var before = Bytes(doc); var count = doc.EditorChangeCount; var revision = doc.EditRevision; var history = doc.GetHistory().Undo.Count;
            var redo = doc.GetHistory().Redo.Count;
            using var policy = new EditorRegionRemovalPolicy(); using var request = policy.Begin(doc, [first, blocked]); Refused(() => request.Accept(true));
            Exact(before); Expect(doc.EditorChangeCount == count && doc.EditRevision == revision && doc.GetHistory().Undo.Count == history && doc.GetHistory().Redo.Count == redo && policy.Suppressed, "Partial patrol removal escaped rollback, lost Redo or lost the earlier confirmation preference.");
        }
        Console.WriteLine($"AREA REMOVAL PATROL DETAIL {cases} independent packed native/project graph/order cases; first surviving predecessor only, display-order rebasing, cycles/missing/self/multiple links, full bytes and Undo/Redo, one counter/notification, generic guard and late-failure rollback");
    }
}
