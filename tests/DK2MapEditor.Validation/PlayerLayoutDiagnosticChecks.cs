using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static class PlayerLayoutDiagnosticChecks
{
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static NativeMapDocument Native(int keepers = 1) => NativeMapDocument.CreateNewMap(new("PlayerLayout", "PlayerLayout", 80, 80,
        Players: (byte)keepers, PlaceHearts: true, NativeFormat: true));
    private static void Raw(NativeRecord record, string name, string value)
    {
        var field = record.Fields.Single(field => field.Name == name);
        Require(MemoryMarshal.TryGetArray(record.File.Data, out var buffer), "Fixture buffer is not array-backed.");
        field.Encode(value).CopyTo(buffer.Array!, buffer.Offset + record.Offset + field.Offset);
    }
    private static IReadOnlyList<MapDiagnostic> ReadOnly(NativeMapDocument document, bool gameplay = false)
    {
        _ = document.Records();
        var bytes = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var history = document.GetHistory(); var revision = document.EditRevision; var dirty = document.HasChanges;
        var changes = 0; var historyChanges = 0;
        EventHandler changed = (_, _) => changes++; EventHandler hc = (_, _) => historyChanges++;
        document.Changed += changed; document.HistoryChanged += hc;
        IReadOnlyList<MapDiagnostic> result;
        try
        {
            result = PlayerLayoutDiagnostics.Validate(document, gameplay);
            var integrated = NativeMapValidator.Validate(document, includeGameplay: gameplay, includeScripts: false)
                .Where(row => int.TryParse(row.Code.AsSpan(4), out var number) && number is >= 33 and <= 38).ToArray();
            Require(integrated.SequenceEqual(result), "Validator integration dropped, reordered or duplicated player layout rows.");
            Require(PlayerLayoutDiagnostics.Validate(document, gameplay).SequenceEqual(result), "Read-only diagnostics are unstable.");
        }
        finally { document.Changed -= changed; document.HistoryChanged -= hc; }
        var after = document.GetHistory();
        Require(bytes.Count == document.Files.Count && bytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value))
            && revision == document.EditRevision && dirty == document.HasChanges && changes == 0 && historyChanges == 0
            && after.EditorChangeCount == history.EditorChangeCount && after.ChangeCount == history.ChangeCount
            && after.Undo.SequenceEqual(history.Undo) && after.Redo.SequenceEqual(history.Redo) && after.SnapshotBytes == history.SnapshotBytes,
            "Diagnosis changed bytes, file set, revision, dirty state, notifications, counters or history.");
        Require(result.All(row => row.Severity != DiagnosticSeverity.Error), "Advisory layout diagnosis became an automatic semantic-edit/export block.");
        return result;
    }
    private static void Has(IReadOnlyList<MapDiagnostic> rows, params string[] codes)
        => Require(rows.Select(row => row.Code).SequenceEqual(codes), "Diagnostic set/order differs: " + string.Join(",", rows.Select(row => row.Code)));

    internal static void Run(Action<string, Action> run)
    {
        run("player layout all five keeper counts and arbitrary physical row order retain stored identities", () =>
        {
            for (var keepers = 1; keepers <= 5; keepers++)
            {
                var document = Native(keepers); Has(ReadOnly(document, true));
                var rows = document.PlayerSettingsRows(); var payloads = rows.Select(row => row.Record.File.Data.Slice(row.Record.Offset, row.Record.Length).ToArray()).Reverse().ToArray();
                for (var i = 0; i < rows.Count; i++)
                {
                    Require(MemoryMarshal.TryGetArray(rows[i].Record.File.Data, out var buffer), "Missing fixture data.");
                    payloads[i].CopyTo(buffer.Array!, buffer.Offset + rows[i].Record.Offset);
                }
                var reordered = document.PlayerSettingsRows();
                Require(reordered.Select(row => row.ID).SequenceEqual(Enumerable.Range(1, keepers + 2).Select(id => (uint)id).Reverse())
                    && reordered.All(row => row.Editable), "Reordered valid IDs were guessed from row positions or made read-only.");
                Has(ReadOnly(document, true));
            }
        });
        run("missing native player count and declared slot diagnostics preserve exact SaveCopy output", () =>
        {
            var document = Native(); var player = document.PlayerSettingsRows().Single(row => row.ID == 3).Record;
            document.DeleteRecord(player); Has(ReadOnly(document), "DK2N035", "DK2N036");
            Has(ReadOnly(document, true), "DK2N035", "DK2N036", "DK2N037");
            var bytes = document.Files.ToDictionary(file => file.ExportPath, file => file.Data.ToArray());
            var directory = Path.Combine(Path.GetTempPath(), "DK2PlayerLayout-" + Guid.NewGuid().ToString("N"));
            document.SaveCopy(directory);
            foreach (var pair in bytes) Require(File.ReadAllBytes(Path.Combine(directory, pair.Key)).AsSpan().SequenceEqual(pair.Value), "Diagnosis/SaveCopy repaired missing players or changed another file.");
            Console.WriteLine("  Player layout fixture: " + directory);
        });
        run("duplicate and invalid native player IDs are explained without editing another player", () =>
        {
            var document = Native(2); var rows = document.PlayerSettingsRows();
            Raw(rows.Single(row => row.ID == 4).Record, "bGlobalPlayer", "3");
            Has(ReadOnly(document), "DK2N034", "DK2N036");
            Require(document.PlayerSettingsRows().Where(row => row.ID == 3).All(row => !row.Editable), "Duplicate IDs became editable.");
            var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); var refused = false;
            try { document.SetPlayerSettings([new(1, "WouldChange", 400, true, null), new(3, "Duplicate", 500, true, null)]); }
            catch (ArgumentException) { refused = true; }
            Require(refused && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Mixed duplicate-player batch partly edited a valid preceding player.");
            Raw(document.PlayerSettingsRows().Last().Record, "bGlobalPlayer", "0");
            Has(ReadOnly(document), "DK2N033", "DK2N036");
        });
        run("native declared count bounds and same-size wrong player set have separate diagnostics", () =>
        {
            foreach (var count in new[] { "0", "6", "255" })
            {
                var document = Native(); var metadata = document.Records().Single(record => record.Category == "Map");
                Raw(metadata, "bMainPlayers", count); Has(ReadOnly(document), "DK2N035");
            }
            var wrongSet = Native(); Raw(wrongSet.PlayerSettingsRows().Single(row => row.ID == 3).Record, "bGlobalPlayer", "4");
            Has(ReadOnly(wrongSet), "DK2N036");
        });
        run("heart diagnostics explain orphan/ambiguous ownership and preserve last matching health", () =>
        {
            var document = Native(); var heart = document.Records().Single(record => record.Category == "GAMEGATE" && record.Read(record.Fields.Single(field => field.Name == "bGateType")) == "14");
            var duplicate = document.DuplicateRecord(heart); Raw(heart, "wGateHealth", "5"); Raw(duplicate, "wGateHealth", "41");
            var messages = ReadOnly(document, true); Has(messages, "DK2N038");
            Require(messages[0].Severity == DiagnosticSeverity.Information && document.PlayerSettingsRows().Single(row => row.ID == 3).Health == 41,
                "Duplicate heart explanation invented an editing ambiguity or changed source last-wins health.");
            Has(ReadOnly(document));
            Raw(duplicate, "bGlobalPlayer", "5"); Has(ReadOnly(document, true), "DK2N037");
        });
        run("project seven positional slots and unsupported native player layouts are not misdiagnosed", () =>
        {
            for (var count = 1; count <= 5; count++)
            {
                var project = NativeMapDocument.CreateNewMap(new("ProjectPlayers", "ProjectPlayers", 80, 80, Players: (byte)count));
                Require(project.PlayerSettingsRows().Count == 7, "Fixture lacks seven project player slots.");
                Has(ReadOnly(project, true));
            }
            var document = Native(); var file = document.PlayerSettingsRows()[0].Record.File;
            Require(MemoryMarshal.TryGetArray(file.Data, out var buffer), "Missing fixture player header.");
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Array!.AsSpan(buffer.Offset + 4), 99);
            Has(ReadOnly(document));
        });
    }
}
