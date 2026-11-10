using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class PlayerGeometryChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> expected)
    { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]), "Player geometry changed unexpected bytes: " + file.ExportPath); }
    private static void Reject(Action operation)
    { try { operation(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException) { return; } throw new Exception("Invalid/stale player geometry edit was accepted."); }
    private static string Name(NativeRecord record, bool project, string suffix) => project
        ? record.Category == "Project.MAPOPTIONSV1" ? suffix : "Player" + suffix
        : suffix.StartsWith("Camera", StringComparison.Ordinal) ? "bPlayer" + suffix : "bPlayerBuildArea" + suffix[7..];
    private static void Expected(Dictionary<NativeFile, byte[]> expected, NativeRecord record, bool project, string suffix, int value)
    {
        var field = record.Fields.Single(field => field.Name == Name(record, project, suffix));
        var bytes = expected[record.File].AsSpan(record.Offset + field.Offset, field.Size);
        if (project) BinaryPrimitives.WriteInt32LittleEndian(bytes, value); else BinaryPrimitives.WriteUInt16LittleEndian(bytes, checked((ushort)value));
    }
    private static TilePoint SourceHeart(NativeMapDocument document, uint id)
    {
        // Independent direct-row reference to preserved Compiler_InfoHeartPosition.
        for (var y = 0; y < document.Height; y++) for (var x = 0; x < document.Width; x++)
        {
            var tile = document.GetTile(x, y);
            if (tile.Terrain != 14 || tile.Owner != id || tile.Variant != 1) continue;
            if (x + 2 >= document.Width || y + 2 >= document.Height) return new(0, 0);
            var center = document.GetTile(x + 2, y + 2);
            return center.Terrain == 14 && center.Owner == id && center.Variant == 1 ? new(x + 2, y + 2) : new(0, 0);
        }
        return new(0, 0);
    }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2-PlayerGeometry-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var project in new[] { false, true })
            {
                var document = NativeMapDocument.CreateNewMap(new("Geometry", "Geometry", 38, 20, Players: 5, PlaceHearts: true, NativeFormat: !project));
                document.Edit("Opaque coordinate fixture", () =>
                {
                    foreach (var row in document.PlayerSettingsRows())
                    {
                        var known = new bool[row.Record.Length]; foreach (var field in row.Record.Fields) Array.Fill(known, true, field.Offset, field.Size);
                        Expect(MemoryMarshal.TryGetArray(row.Record.File.Data, out var segment), "Missing player buffer.");
                        for (var index = 0; index < known.Length; index++) if (!known[index]) segment.Array![segment.Offset + row.Record.Offset + index] = 0xAD;
                    }
                });
                var history = document.CanUndo;
                var before = Snapshot(document); var notifications = 0; document.Changed += (_, _) => notifications++;
                Expect(document.PlayerGeometries().Count == 7 && document.PlayerGeometries().All(row => row.CameraEditable == (row.ID >= 3) && row.BuildAreaEditable), "Current player coordinates/source NPC camera guards wrong.");
                Exact(document, before); Expect(document.CanUndo == history && notifications == 0, "Projection edited the map.");
                foreach (var id in Enumerable.Range(1, 7).Select(value => (uint)value))
                {
                    var row = document.PlayerGeometries().Single(row => row.ID == id); var record = row.Record;
                    if (id <= 2) { Reject(() => document.SetPlayerCamera(id, new(1, 1))); Reject(() => document.SetPlayerCamera(id, null)); }
                    foreach (var point in id <= 2 ? Array.Empty<TilePoint>() : new[] { new TilePoint(0, 0), new(37, 19), new((int)id, (int)id + 1) })
                    {
                        before = Snapshot(document); var expected = Snapshot(document); var changed = notifications;
                        Expected(expected, record, project, "CameraX", point.X); Expected(expected, record, project, "CameraY", point.Y);
                        var changeCount = before.Any(pair => !pair.Value.AsSpan().SequenceEqual(expected[pair.Key])) ? 1 : 0;
                        document.SetPlayerCamera(id, point); Exact(document, expected); Expect(notifications == changed + changeCount, "Camera edit not one transaction.");
                        Expect(document.PlayerGeometries().Single(row => row.ID == id).Camera == point, "Wrong camera/player projection.");
                        document.SetPlayerCamera(id, point); Expect(notifications == changed + changeCount, "No-op camera added history.");
                        if (changeCount != 0) { document.Undo(); Exact(document, before); document.Redo(); Exact(document, expected); }
                    }
                    foreach (var bounds in new[] { new TileRect(0, 0, 38, 20), new(37, 19, 1, 1), new(3, 5, 7, 8) })
                    {
                        before = Snapshot(document); var expected = Snapshot(document); var changed = notifications;
                        Expected(expected, record, project, "CPUAreaX1", bounds.X); Expected(expected, record, project, "CPUAreaY1", bounds.Y);
                        Expected(expected, record, project, "CPUAreaX2", bounds.X + bounds.Width - 1); Expected(expected, record, project, "CPUAreaY2", bounds.Y + bounds.Height - 1);
                        document.SetPlayerBuildArea(id, bounds); Exact(document, expected); Expect(notifications == changed + 1, "Build-area edit not one transaction.");
                        document.SetPlayerBuildArea(id, bounds); Expect(notifications == changed + 1, "No-op build area added history.");
                        document.Undo(); Exact(document, before); document.Redo(); Exact(document, expected);
                    }
                    var defaults = Snapshot(document); var defaultCamera = project ? new TilePoint(-1, -1) : id <= 2 ? new(0, 0) : SourceHeart(document, id);
                    if (id >= 3)
                    {
                        Expected(defaults, record, project, "CameraX", defaultCamera.X); Expected(defaults, record, project, "CameraY", defaultCamera.Y);
                        document.SetPlayerCamera(id, null); Exact(document, defaults);
                    }
                    foreach (var (suffix, value) in new[] { ("CPUAreaX1", 2), ("CPUAreaY1", 2), ("CPUAreaX2", 36), ("CPUAreaY2", 18) }) Expected(defaults, record, project, suffix, project ? -1 : value);
                    document.SetPlayerBuildArea(id, null); Exact(document, defaults);
                    row = document.PlayerGeometries().Single(row => row.ID == id);
                    Expect(row.DefaultCamera == project && row.DefaultBuildArea == project && row.EffectiveCamera == (id <= 2 ? new(0, 0) : SourceHeart(document, id))
                        && row.EffectiveBuildStart == new TilePoint(2, 2) && row.EffectiveBuildEnd == new TilePoint(36, 18), "Wrong source/resolved defaults.");
                }
                before = Snapshot(document); var priorNotifications = notifications;
                foreach (var point in new[] { new TilePoint(-1, 0), new(0, -1), new(38, 0), new(0, 20), new(int.MaxValue, int.MaxValue) }) Reject(() => document.SetPlayerCamera(3, point));
                foreach (var bounds in new[] { new TileRect(-1, 0, 1, 1), new(0, -1, 1, 1), new(0, 0, 0, 1), new(37, 19, 2, 1), new(0, 0, int.MaxValue, int.MaxValue) }) Reject(() => document.SetPlayerBuildArea(3, bounds));
                Reject(() => document.SetPlayerCamera(0, new(1, 1))); Reject(() => document.SetPlayerBuildArea(8, new(1, 1, 1, 1)));
                Exact(document, before); Expect(notifications == priorNotifications, "Rejected geometry edited bytes/history.");
                var selected = document.PlayerGeometries().Single(row => row.ID == 3); var payload = selected.Record.File.Data.Slice(selected.Record.Offset, selected.Record.Length).ToArray();
                var gold = selected.Record.Fields.Single(field => field.Name == (project ? "PlayerGold" : "bPlayerGold")); document.SetField(selected.Record, gold, "1234"); before = Snapshot(document);
                Reject(() => document.SetPlayerCamera(3, new(3, 4), payload)); Reject(() => document.SetPlayerBuildArea(3, new(1, 2, 4, 5), payload)); Exact(document, before);
                document.Undo();
                // Read malformed imported coordinates without rewriting; explicit editing repairs only chosen coordinates.
                var bad = selected.Record.Fields.Single(field => field.Name == Name(selected.Record, project, "CameraX")); document.SetField(selected.Record, bad, project ? "-9" : "65535");
                before = Snapshot(document); Expect(document.PlayerGeometries().Single(row => row.ID == 3).Camera.X == (project ? -9 : 65535), "Malformed camera silently normalized."); Exact(document, before);
                document.SetPlayerCamera(3, new(6, 7)); document.SetPlayerBuildArea(3, new(8, 9, 2, 3));
                var values = document.PlayerGeometries().Select(row => (row.ID, row.Camera, row.BuildStart, row.BuildEnd)).ToArray(); var saved = document.SaveCopy(Path.Combine(directory, project ? "project" : "native"));
                Expect(values.SequenceEqual(NativeMapDocument.Load(saved).PlayerGeometries().Select(row => (row.ID, row.Camera, row.BuildStart, row.BuildEnd))), "Saved coordinates changed.");
                if (project)
                {
                    var compiled = document.CompileLegacyProject("GeometryCompiled", NativeMapDocument.Create(38, 20, "Basis"));
                    var compiledRow = compiled.PlayerGeometries().Single(row => row.ID == 3);
                    Expect(compiledRow.Camera == new TilePoint(6, 7) && compiledRow.BuildStart == new TilePoint(8, 9) && compiledRow.BuildEnd == new TilePoint(9, 11), "Compiler did not retain chosen coordinates.");
                    Historical(saved, directory);
                }
                else
                {
                    // Semantic IDs, not physical row ordinal; duplicate IDs are never guessed.
                    var seventh = document.PlayerGeometries().Single(row => row.ID == 7).Record;
                    var identity = seventh.Fields.Single(field => field.Name == "bGlobalPlayer"); document.SetField(seventh, identity, "5"); before = Snapshot(document);
                    Expect(document.PlayerGeometries().Where(row => row.ID == 5).All(row => !row.CameraEditable && !row.BuildAreaEditable), "Duplicate geometry ID enabled.");
                    Reject(() => document.SetPlayerCamera(5, new(1, 1))); Exact(document, before); document.Undo();
                    document.DeleteRecord(document.PlayerGeometries().Single(row => row.ID == 4).Record);
                    document.SetPlayerCamera(7, new(17, 18)); Expect(document.PlayerGeometries().Single(row => row.ID == 7).Camera == new TilePoint(17, 18), "Noncontiguous ID edited ordinal player.");
                    document.Undo(); document.Undo();
                    var file = document.Files.Single(file => file.Signature == 0xB4);
                    document.Edit("Unknown player version fixture", () => { Expect(MemoryMarshal.TryGetArray(file.Data, out var segment), "Missing buffer."); BinaryPrimitives.WriteUInt32LittleEndian(segment.Array!.AsSpan(segment.Offset + 4), 99); });
                    before = Snapshot(document); Expect(document.PlayerGeometries().All(row => !row.CameraEditable && !row.BuildAreaEditable), "Unknown native version enabled geometry.");
                    Reject(() => document.SetPlayerCamera(3, new(1, 1))); Reject(() => document.SetPlayerBuildArea(3, null)); Exact(document, before); document.Undo();
                }
            }
            HeartSourceOrder();
            foreach (var native in new[] { false, true })
            {
                var tiny = NativeMapDocument.CreateNewMap(new("Tiny", "Tiny", 7, 7, NativeFormat: native));
                tiny.Resize(3, 3, 0, 0, new(2, 2, 1), removeOutside: true);
                tiny.SetPlayerBuildArea(3, new(0, 0, 1, 1)); tiny.SetPlayerBuildArea(3, null);
                var row = tiny.PlayerGeometries().Single(row => row.ID == 3);
                Expect(row.DefaultBuildArea == !native && row.EffectiveBuildStart == new TilePoint(2, 2) && row.EffectiveBuildEnd == new TilePoint(1, 1), "Tiny-map source defaults silently clamped/rejected.");
            }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
    private static void Historical(string path, string directory)
    {
        var sections = LegacySectionFile.Read(path); var original = sections.Sections.Single(section => section.Signature == NativeMapDocument.LegacyPlayers);
        foreach (var version in new[] { 1, 2, 99 })
        {
            var payload = version == 1 ? new byte[7 * LegacyProjectSchemas.Size("MAPOPTIONSV1")] : original.Payload.ToArray();
            var target = Path.Combine(directory, "geometry-v" + version + ".dkm");
            File.WriteAllBytes(target, LegacySectionFile.Create(sections.Sections.Select(section => section.Signature == original.Signature ? new LegacySection(original.Signature, version, payload) : section)).ToArray());
            var document = NativeMapDocument.Load(target); var before = Snapshot(document);
            Expect(document.PlayerGeometries().All(row => !row.CameraEditable && row.BuildAreaEditable == (version != 99)), "Historical geometry guards wrong.");
            Reject(() => document.SetPlayerCamera(3, new(1, 2))); Exact(document, before);
            if (version == 99) { Reject(() => document.SetPlayerBuildArea(3, new(1, 2, 3, 4))); Exact(document, before); continue; }
            var record = document.PlayerGeometries().Single(row => row.ID == 3).Record;
            var expected = Snapshot(document);
            foreach (var (name, value) in new[] { ("CPUAreaX1", 1), ("CPUAreaY1", 2), ("CPUAreaX2", 3), ("CPUAreaY2", 5) }) Expected(expected, record, true, name, value);
            document.SetPlayerBuildArea(3, new(1, 2, 3, 4)); Exact(document, expected); document.Undo(); Exact(document, before);
            document.UpgradeLegacyProject(); Expect(document.PlayerGeometries().All(row => row.CameraEditable == (row.ID >= 3)), "Upgrade did not enable real keeper camera fields.");
        }
    }
    private static void HeartSourceOrder()
    {
        foreach (var native in new[] { false, true })
        {
            var document = NativeMapDocument.CreateNewMap(new("Heart", "Heart", 16, 16, NativeFormat: native));
            document.Paint([new(1, 1)], new(14, 3, 2)); // Non-room variant must not be a camera heart.
            document.Paint([new(5, 5), new(7, 7)], new(14, 3, 1));
            document.SetPlayerCamera(3, null);
            Expect(document.PlayerGeometries().Single(row => row.ID == 3).EffectiveCamera == new TilePoint(7, 7), "Source centre test was replaced by full-square search.");
            document.Paint([new(2, 2)], new(14, 3, 1)); // First malformed heart must not skip to the later one.
            document.SetPlayerCamera(3, null);
            Expect(document.PlayerGeometries().Single(row => row.ID == 3).EffectiveCamera == new TilePoint(0, 0), "Malformed first heart skipped source order.");
            // Remaining terrain/shape diagnostics are intentionally independent and read-only.
            Expect(document.GetTile(5, 5).Terrain == 14 && document.GetTile(7, 7).Terrain == 14, "Default-camera operation rewrote heart terrain.");
        }
    }
}
