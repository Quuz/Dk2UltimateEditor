using DK2MapEditor.Core.Editing;

internal static class EditorFloodComponentChecks
{
    private const string GameName = "DK2Preview_6a88240eaf814f78";
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static NativeMapDocument New(bool project) => project
        ? NativeMapDocument.CreateNewMap(new(Width: 9, Height: 8, DisplayName: GameName, FileName: GameName))
        : NativeMapDocument.Create(9, 8, GameName);
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> snapshot)
    { foreach (var (file, bytes) in snapshot) Require(file.Data.Span.SequenceEqual(bytes), "Flood history/refusal changed complete buffers."); }
    private static void Background(NativeMapDocument document) => document.Paint(MapDrawing.Rectangle(new(0, 0, document.Width, document.Height), true), new(1, 2, 1, 0xA7));
    private static void Region(NativeMapDocument document, IReadOnlyList<MapTile> cells)
    { for (var index = 0; index < cells.Count; index++) document.Paint([new(2 + index, 3)], cells[index]); }
    private static void Verify(NativeMapDocument document, Dictionary<NativeFile, byte[]> before, IReadOnlySet<TilePoint> filled, MapTile selected)
    {
        for (var y = 0; y < document.Height; y++) for (var x = 0; x < document.Width; x++)
        {
            var expected = filled.Contains(new(x, y)) ? selected : MapTile.FromPacked(System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(before[document.TileFile].AsSpan(36 + (y * document.Width + x) * 4)));
            Require(document.GetTile(x, y) == expected, $"Source flood component differs at {x},{y}.");
        }
        foreach (var (file, bytes) in before.Where(pair => pair.Key != document.TileFile)) Require(file.Data.Span.SequenceEqual(bytes), "Flood changed a player/object/script/region buffer.");
        var after = Snapshot(document); document.Undo(); Exact(before); document.Redo(); Exact(after);
    }
    private static void AddUnrelated(NativeMapDocument document)
    {
        if (document.IsLegacyProject)
            foreach (var kind in new[] { "Creature", "Area", "Gate", "Effect", "Band" }) document.AddLegacyRecord(kind, new(6, 5));
        else
            foreach (var kind in new[] { "GAMEKEEPER", "GAMEAREA", "GAMEGATE", "GAMEEFFECT", "GAMEBAND" }) document.AddRecord(RecordTemplate.All.Single(template => template.Structure == kind), new(6, 5));
    }
    public static void Run(Action<string, Action> run)
    {
        run("editor flood: reserved bytes cannot split an otherwise identical connected native/project terrain region", () =>
        {
            foreach (var project in new[] { false, true })
            {
                var document = New(project); Background(document); AddUnrelated(document); Region(document, [new(8, 3, 1, 0), new(8, 3, 1, 0xA5), new(8, 3, 1, 255)]);
                var before = Snapshot(document); var selected = new MapTile(11, 6, 1, 203); document.FloodFillEditor(new(2, 3), selected);
                Verify(document, before, new HashSet<TilePoint> { new(2, 3), new(3, 3), new(4, 3) }, selected);
            }
        });
        run("editor flood: source fixed-world owners share a region while different owned paths remain separate", () =>
        {
            foreach (var project in new[] { false, true })
            {
                var document = New(project); Background(document); Region(document, [new(4, 1, 1, 1), new(4, 7, 1, 2), new(4, 2, 1, 3)]);
                var before = Snapshot(document); var selected = new MapTile(11, 3, 1, 211); document.FloodFillEditor(new(2, 3), selected);
                Verify(document, before, new HashSet<TilePoint> { new(2, 3), new(3, 3), new(4, 3) }, selected);
                document = New(project); Background(document); Region(document, [new(8, 3, 1, 1), new(8, 4, 1, 1), new(8, 3, 1, 1)]); before = Snapshot(document);
                document.FloodFillEditor(new(2, 3), selected); Verify(document, before, new HashSet<TilePoint> { new(2, 3) }, selected);
            }
        });
        run("editor flood: bridge variants and four-neighbour connectivity stay distinct after ignoring reserved bytes", () =>
        {
            foreach (var project in new[] { false, true })
            {
                var document = New(project); Background(document); Region(document, [new(17, 3, 1, 1), new(17, 3, 2, 1), new(17, 3, 1, 1)]); document.Paint([new(3, 4)], new(17, 3, 1, 254));
                var before = Snapshot(document); var selected = new MapTile(11, 6, 1, 213); document.FloodFillEditor(new(2, 3), selected);
                Verify(document, before, new HashSet<TilePoint> { new(2, 3) }, selected);
            }
        });
        run("editor flood: source import normalization affects component comparison only, not unselected raw cells", () =>
        {
            foreach (var project in new[] { false, true })
            {
                var document = New(project); Background(document); Region(document, [new(2, 2, 1, 1), new(2, 255, 0, 91), new(0xF8, 4, 2, 17)]);
                document.Paint([new(6, 5)], new(0xF8, 5, 2, 77)); var before = Snapshot(document); var selected = new MapTile(11, 3, 1, 181); document.FloodFillEditor(new(2, 3), selected);
                Verify(document, before, new HashSet<TilePoint> { new(2, 3), new(3, 3), new(4, 3) }, selected);
            }
        });
        run("editor flood: an equal start still traverses same source region with distinct raw reserved values", () =>
        {
            foreach (var project in new[] { false, true })
            {
                var document = New(project); Background(document); var selected = new MapTile(8, 3, 1, 19); Region(document, [selected, selected with { Reserved = 20 }, selected with { Reserved = 21 }]);
                var before = Snapshot(document); document.FloodFillEditor(new(2, 3), selected);
                Verify(document, before, new HashSet<TilePoint> { new(2, 3), new(3, 3), new(4, 3) }, selected);
            }
        });
        run("editor flood: selection, positional border and all raw API comparisons remain unchanged", () =>
        {
            foreach (var project in new[] { false, true })
            {
                var document = New(project); Background(document); Region(document, [new(8, 3, 1, 1), new(8, 3, 1, 2), new(8, 3, 1, 3)]); var before = Snapshot(document); var selected = new MapTile(11, 3, 1, 147);
                document.FloodFillEditor(new(2, 3), selected, new(2, 3, 2, 1)); Verify(document, before, new HashSet<TilePoint> { new(2, 3), new(3, 3) }, selected);
                document.Undo(); before = Snapshot(document); document.FloodFill(new(2, 3), selected);
                Verify(document, before, new HashSet<TilePoint> { new(2, 3) }, selected);
                document.Undo(); before = Snapshot(document); var history = document.GetHistory(); document.FloodFillEditor(new(0, 0), selected); Exact(before);
                Require(document.GetHistory().Undo.SequenceEqual(history.Undo) && document.GetHistory().Redo.SequenceEqual(history.Redo), "Protected editor flood invented a raw API history step.");
            }
        });
        run("editor flood: modified semantic components survive actual raw Save/reopen and retain caller acceptance/Undo", () =>
        {
            var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DK2EditorFlood-" + Guid.NewGuid().ToString("N"))); Directory.CreateDirectory(root);
            try
            {
                foreach (var project in new[] { false, true })
                {
                    var document = New(project); Background(document); Region(document, [new(8, 3, 1, 1), new(8, 3, 1, 2)]); var before = Snapshot(document); var selected = new MapTile(11, 3, 1, 157); var count = document.EditorChangeCount;
                    document.EditEditorDrawing("Fill", () => { document.FloodFillEditor(new(2, 3), selected); return new(true, 1); });
                    Require(document.EditorChangeCount == unchecked(count + 1), "Editor flood counted cells instead of the accepted source call."); Verify(document, before, new HashSet<TilePoint> { new(2, 3), new(3, 3) }, selected);
                    var path = document.SaveCopy(Path.Combine(root, project.ToString()), temporary: true); var reopened = NativeMapDocument.Load(path);
                    Require(reopened.GetTile(2, 3) == selected && reopened.GetTile(3, 3) == selected, "Source component changes were lost during Save/reopen.");
                }
            }
            finally { Require(root.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase), "Unexpected fixture cleanup target."); Directory.Delete(root, recursive: true); }
        });
    }
}
