using System.Globalization;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class PlayerImportPolicyChecks
{
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static string Text(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
    private static long Number(NativeRecord record, string name) => long.Parse(Text(record, name), CultureInfo.InvariantCulture);
    private static void Raw(NativeRecord record, string name, string value)
    {
        var field = record.Fields.Single(field => field.Name == name);
        Require(MemoryMarshal.TryGetArray(record.File.Data, out var buffer), "Fixture buffer missing.");
        field.Encode(value).CopyTo(buffer.Array!, buffer.Offset + record.Offset + field.Offset);
    }
    private static NativeMapDocument Native(int keepers = 1) => NativeMapDocument.CreateNewMap(new("PlayerImport", "PlayerImport", 80, 80,
        Players: (byte)keepers, PlaceHearts: true, NativeFormat: true));
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document)
    { _ = document.Records(); return document.Files.ToDictionary(file => file, file => file.Data.ToArray()); }
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> before)
        => Require(before.Count == document.Files.Count && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Player projection/Save changed source files.");
    private static void Retained(PreparedProjectConversion conversion, Dictionary<NativeFile, byte[]> before)
    {
        foreach (var pair in before)
        {
            var retained = conversion.SourceFiles.Single(file => file.ExportPath == pair.Key.ExportPath);
            Require(retained.Bytes.AsSpan().SequenceEqual(pair.Value), "Source snapshot dropped or normalized an original file.");
        }
        var archive = EditorSourceArchive.Capture(conversion);
        foreach (var original in conversion.SourceFiles)
            Require(archive.SourceFiles.Single(file => file.ExportPath == original.ExportPath).Bytes.AsSpan().SequenceEqual(original.Bytes.AsSpan()),
                "Companion archive lost original player/source bytes.");
    }
    private static void DefaultsReported(PreparedProjectConversion conversion, params uint[] ids)
    {
        var rows = conversion.Issues.Where(issue => issue.Code == "PlayerSlotDefaults").ToArray();
        Require(rows.Length == (ids.Length == 0 ? 0 : 1), "Native-to-DKM did not explicitly explain positional slots filled from defaults.");
        if (ids.Length != 0)
            Require(rows[0].Severity == ProjectConversionSeverity.Information && rows[0].Detail.Contains("[" + string.Join(", ", ids) + "]", StringComparison.Ordinal),
                "Default-slot report has incorrect identities/severity.");
    }
    private static PlayerSettingsRow Slot(NativeMapDocument project, uint id) => project.PlayerSettingsRows().Single(row => row.ID == id);

    internal static void Run(Action<string, Action> run)
    {
        run("native player import all keeper counts uses stored IDs after physical reversal and explains dormant slots", () =>
        {
            for (var count = 1; count <= 5; count++)
            {
                var source = Native(count); var rows = source.PlayerSettingsRows();
                foreach (var row in rows)
                { Raw(row.Record, "szPlayerName", "José " + row.ID); Raw(row.Record, "bPlayerGold", (row.ID * 101).ToString()); }
                var payloads = rows.Select(row => row.Record.File.Data.Slice(row.Record.Offset, row.Record.Length).ToArray()).Reverse().ToArray();
                for (var index = 0; index < rows.Count; index++)
                {
                    Require(MemoryMarshal.TryGetArray(rows[index].Record.File.Data, out var buffer), "Fixture data missing.");
                    payloads[index].CopyTo(buffer.Array!, buffer.Offset + rows[index].Record.Offset);
                }
                var before = Snapshot(source); var revision = source.EditRevision; var history = source.GetHistory();
                var conversion = source.PrepareProjectConversion();
                Require(!conversion.HasBlockingIssues && conversion.Project.PlayerSettingsRows().Count == 7, "Valid reordered native players did not project to seven slots.");
                foreach (var row in source.PlayerSettingsRows())
                {
                    var target = Slot(conversion.Project, row.ID);
                    Require(target.Name == "José " + row.ID && target.Gold == row.ID * 101, "Projection used physical ordinal instead of stored player ID or lost CP1252 name.");
                }
                DefaultsReported(conversion, Enumerable.Range(count + 3, 5 - count).Select(id => (uint)id).ToArray());
                Exact(source, before); Retained(conversion, before);
                Require(revision == source.EditRevision && history.EditorChangeCount == source.EditorChangeCount
                    && history.Undo.SequenceEqual(source.GetHistory().Undo) && history.Redo.SequenceEqual(source.GetHistory().Redo), "Preparation changed live history/counters.");
            }
        });
        run("missing active/native NPC records become explicit source-default project slots with exact originals", () =>
        {
            var source = Native(); source.DeleteRecord(source.PlayerSettingsRows().Single(row => row.ID == 1).Record);
            source.DeleteRecord(source.PlayerSettingsRows().Single(row => row.ID == 3).Record);
            var before = Snapshot(source); var conversion = source.PrepareProjectConversion();
            DefaultsReported(conversion, 1, 3, 4, 5, 6, 7);
            Require(Slot(conversion.Project, 1).Name == "Good Player" && Slot(conversion.Project, 3).Name == "Keeper 1"
                && Slot(conversion.Project, 3).Gold == 3000, "Missing slots did not retain original defaults.");
            Require(!conversion.HasBlockingIssues, "Missing native rows invented a new normal-Save block.");
            Exact(source, before); Retained(conversion, before);
        });
        run("duplicate native player projection preserves source last assignment and reports both duplicate/default policies", () =>
        {
            var source = Native(2); var rows = source.PlayerSettingsRows();
            Raw(rows.Single(row => row.ID == 3).Record, "bPlayerGold", "111");
            var last = rows.Single(row => row.ID == 4).Record; Raw(last, "bGlobalPlayer", "3"); Raw(last, "bPlayerGold", "222"); Raw(last, "szPlayerName", "Last Café");
            var before = Snapshot(source); var conversion = source.PrepareProjectConversion();
            Require(conversion.Issues.Any(issue => issue.Code == "DuplicatePlayer" && issue.Severity == ProjectConversionSeverity.Loss)
                && Slot(conversion.Project, 3).Gold == 222 && Slot(conversion.Project, 3).Name == "Last Café", "Duplicate projection guessed first record or hid its explicit loss policy.");
            DefaultsReported(conversion, 4, 5, 6, 7); Exact(source, before); Retained(conversion, before);
        });
        run("invalid player IDs block normal DKM adoption while retaining complete source snapshots", () =>
        {
            foreach (var id in new[] { "0", "8", "255" })
            {
                var source = Native(); Raw(source.PlayerSettingsRows().Single(row => row.ID == 3).Record, "bGlobalPlayer", id);
                var before = Snapshot(source); var conversion = source.PrepareProjectConversion();
                Require(conversion.HasBlockingIssues && conversion.Issues.Any(issue => issue.Code == "InvalidPlayer" && issue.Severity == ProjectConversionSeverity.Error), "Out-of-range player was silently indexed or admitted.");
                DefaultsReported(conversion, 3, 4, 5, 6, 7); Exact(source, before); Retained(conversion, before);
            }
        });
        run("normal native SaveAs DKM retains reversed player files in a real companion and reopens correct slots", () =>
        {
            var source = Native(2); var rows = source.PlayerSettingsRows();
            foreach (var row in rows) Raw(row.Record, "szPlayerName", "Source " + row.ID);
            var data = rows.Select(row => row.Record.File.Data.Slice(row.Record.Offset, row.Record.Length).ToArray()).Reverse().ToArray();
            for (var i = 0; i < rows.Count; i++)
            { Require(MemoryMarshal.TryGetArray(rows[i].Record.File.Data, out var bytes), "Missing player buffer."); data[i].CopyTo(bytes.Array!, bytes.Offset + rows[i].Record.Offset); }
            var before = Snapshot(source); var directory = Path.Combine(Path.GetTempPath(), "DK2PlayerImportPolicy-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "players.dkm"); using var save = new EditorSaveSession(source); var plan = save.PrepareSaveAs(path);
            Require(plan.ConversionIssues.Any(issue => issue.Code == "PlayerSlotDefaults") && !plan.HasBlockingIssues, "Normal Save did not expose default-slot explanation before adoption.");
            Require(save.Save(plan, true), "Explicit Save acceptance failed.");
            var reopened = NativeMapDocument.Load(path);
            foreach (var row in rows) Require(Slot(reopened, row.ID).Name == "Source " + row.ID, "Real Save/reload lost semantic player assignment.");
            var archives = save.LastSave!.SourceArchives.Select(EditorSourceArchive.Read).ToArray(); Require(archives.Length == 1, "Real conversion did not publish its source companion.");
            foreach (var pair in before) Require(archives[0].SourceFiles.Single(file => file.ExportPath == pair.Key.ExportPath).Bytes.AsSpan().SequenceEqual(pair.Value), "Real companion changed native file order/payload.");
            Exact(source, before); Console.WriteLine("  Player import fixture: " + directory);
        });
        run("compilation player presence and camera remain heart-derived while declared metadata stays literal", () =>
        {
            var project = NativeMapDocument.CreateNewMap(new("PlayerCompile", "PlayerCompile", 32, 32, Players: 5, PlaceHearts: false));
            // The source uses the first heart tile plus (2,2), rather than a later complete square.
            project.Paint([new(3, 3), new(5, 5)], new(14, 3, 1));
            var before = Snapshot(project); var compiled = project.CompileLegacyProject("HeartPlayers", NativeMapDocument.Create(32, 32, "Basis"));
            Require(compiled.PlayerSettingsRows().Select(row => row.ID).SequenceEqual(new uint[] { 1, 2, 3 })
                && compiled.PlayerGeometries().Single(row => row.ID == 3).Camera == new TilePoint(5, 5), "Compiler replaced source heart-derived players/camera with declared count.");
            var metadata = compiled.Records().Single(row => row.Category == "Map"); Require(Number(metadata, "bMainPlayers") == 5, "Compilation silently rewrote declared keepers.");
            Require(PlayerLayoutDiagnostics.Validate(compiled).Any(row => row.Code == "DK2N035"), "Count mismatch was hidden after source-compatible compilation.");
            Exact(project, before);
            project.Paint([new(2, 1)], new(14, 3, 1)); // Its (+2,+2) centre is not an existing heart tile.
            var malformedBefore = Snapshot(project);
            var malformed = project.CompileLegacyProject("MalformedFirst", NativeMapDocument.Create(32, 32, "Basis2"));
            Require(malformed.PlayerSettingsRows().Select(row => row.ID).SequenceEqual(new uint[] { 1, 2 }), "Compiler skipped malformed first heart in favor of later heart tiles.");
            Exact(project, malformedBefore);
        });
    }
}
