using DK2MapEditor.Core.Editing;

var directory = Path.Combine(Path.GetTempPath(), "DK2EditorDrawingHistory", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
void Refuses<T>(Action action) where T : Exception
{ try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
Dictionary<NativeFile, byte[]> Bytes(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
bool Same(NativeMapDocument doc, Dictionary<NativeFile, byte[]> bytes) => bytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value));
var fixture = 0;
NativeMapDocument Clean(bool native)
{
    var created = NativeMapDocument.CreateNewMap(new("DrawingHistory", "DrawingHistory", 16, 16, NativeFormat: native));
    var path = created.SaveCopy(Path.Combine(directory, "fixture-" + fixture++), temporary: true);
    var doc = NativeMapDocument.Load(path);
    Expect(!doc.HasChanges && !doc.CanUndo && !doc.CanRedo && doc.EditorChangeCount == 0, "Loaded fixture is not the original clean state.");
    return doc;
}
void EqualShape(NativeMapDocument doc, EditorPaintShape shape, bool random = false)
{
    var selected = doc.GetTile(3, 3);
    doc.EditEditorDrawing(random ? "Random " + shape : shape.ToString(), () =>
    {
        var result = doc.PaintEditorGesture(new(3, 3), new(5, 4), selected, shape, randomized: random, randomSample: _ => 1);
        Expect(result == EditorPlacementResult.Painted, "Legal source geometric caller was refused.");
        return new(true, 1);
    });
}
try
{
    HistoryFailureChecks.Run(directory);
    // Independent caller evidence: allocating Undo is not conditional on byte deltas.
    var sourceRoot = new DirectoryInfo(AppContext.BaseDirectory);
    while (sourceRoot is not null && !File.Exists(Path.Combine(sourceRoot.FullName, "Source", "Sources", "Map", "Map_Mouse.c"))) sourceRoot = sourceRoot.Parent;
    Expect(sourceRoot is not null, "Original source root not found.");
    var mouse = File.ReadAllText(Path.Combine(sourceRoot!.FullName, "Source", "Sources", "Map", "Map_Mouse.c"));
    var release = mouse[mouse.IndexOf("void Mouse_LeftButtonUp(", StringComparison.Ordinal)..];
    Expect(release.IndexOf("Undo_CreateMapRect(Undo_PaintModeToText(FillID)", StringComparison.Ordinal)
        < release.IndexOf("Map_PaintElement(hWnd,FillID)", StringComparison.Ordinal), "Original geometric history allocation order differs.");
    var down = mouse[mouse.IndexOf("void Mouse_LeftButtonDown(", StringComparison.Ordinal)..mouse.IndexOf("void Mouse_LeftButtonUp(", StringComparison.Ordinal)];
    Expect(down.IndexOf("Undo_CreateFullMap(szMainMenuFill", StringComparison.Ordinal)
        < down.IndexOf("Map_FillArea(hWnd,X,Y)", StringComparison.Ordinal), "Original protected fill history allocation order differs.");

    foreach (var native in new[] { false, true })
    {
        var kind = native ? "native" : "project";
        var doc = Clean(native); var clean = Bytes(doc);
        doc.Paint([new(3, 3)], new(4, 2, 1)); doc.Undo();
        var revision = doc.EditRevision; var history = doc.GetHistory(); var notices = 0;
        doc.Changed += (_, _) => notices++;
        doc.Edit("Generic empty", () => { }); doc.PaintEditor([new(3, 3)], doc.GetTile(3, 3));
        doc.EditEditorDrawing("Refused", () => new(false, 0));
        Expect(Same(doc, clean) && doc.CanRedo && doc.GetHistory().Redo.Count == history.Redo.Count && doc.EditorChangeCount == 0
            && !doc.HasChanges && doc.EditRevision == revision && notices == 0, "Generic/refused no-op damaged redo, clean state or lifetime.");
        Refuses<IOException>(() => doc.EditEditorDrawing("Failed", () => { doc.Paint([new(4, 4)], new(4, 2, 1)); throw new IOException("Injected failure"); }));
        Refuses<InvalidOperationException>(() => doc.EditEditorDrawing("Invalid refusal", () => { doc.Paint([new(4, 4)], new(4, 2, 1)); return new(false, 0); }));
        Refuses<InvalidOperationException>(() => doc.EditEditorDrawing("Invalid increment", () => new(false, 1)));
        Expect(Same(doc, clean) && doc.CanRedo && !doc.HasChanges && doc.EditorChangeCount == 0 && doc.EditRevision == revision,
            "Failed/refused drawing lost exact rollback or previous redo.");
        Console.WriteLine($"PASS {kind} generic no-op/redo retention, refused and failed drawing exact rollback and lifetime");

        foreach (var shape in new[] { EditorPaintShape.Line, EditorPaintShape.Rectangle, EditorPaintShape.FilledRectangle, EditorPaintShape.Ellipse, EditorPaintShape.FilledEllipse })
        foreach (var random in new[] { false, true })
        {
            doc = Clean(native); clean = Bytes(doc); revision = doc.EditRevision;
            EqualShape(doc, shape, random);
            Expect(Same(doc, clean) && doc.GetHistory().Undo.Count == 1 && doc.GetHistory().ChangeCount == 1 && doc.EditorChangeCount == 1
                && doc.GetHistory().EditorChangeCount == 1 && doc.HasChanges && doc.EditRevision == revision + 1
                && doc.GetHistory().SnapshotBytes == 2L * doc.TileFile.Data.Length, "Accepted equal/no-hit source shape lost Undo, counter, dirty state or payload.");
            doc.Undo(); Expect(Same(doc, clean) && doc.EditorChangeCount == 0 && !doc.HasChanges && doc.CanRedo, "Empty shape Undo did not restore original clean change state.");
            doc.Redo(); Expect(Same(doc, clean) && doc.EditorChangeCount == 1 && doc.HasChanges && doc.CanUndo, "Empty shape Redo lost original change state.");
        }
        Console.WriteLine($"PASS {kind} five source equal shapes and deterministic random no-hit: one Undo, exact all files, dirty/count/lifetime and Redo");

        doc = Clean(native); clean = Bytes(doc);
        doc.EditEditorDrawing("Fill", () => { doc.FloodFillEditor(new(3, 3), doc.GetTile(3, 3)); return new(true, 1); });
        Expect(Same(doc, clean) && doc.CanUndo && doc.EditorChangeCount == 1 && doc.HasChanges, "Equal successful fill lost source Undo/change state.");
        doc.Undo(); Expect(!doc.HasChanges && doc.EditorChangeCount == 0 && Same(doc, clean), "Equal fill Undo did not restore clean state.");
        doc.EditEditorDrawing("Protected fill", () => { doc.FloodFillEditor(new(0, 3), new(4, 2, 1)); return new(true, 0); });
        Expect(Same(doc, clean) && doc.CanUndo && !doc.CanRedo && doc.GetHistory().Undo.Count == 1 && doc.EditorChangeCount == 0 && !doc.HasChanges,
            "Protected fill must retain preallocated Undo, replace redo and omit MapChanges.");
        var uses = doc.ReadActivityStatistics()!.UsageOfUndo;
        doc.Undo(); doc.Redo();
        Expect(Same(doc, clean) && !doc.HasChanges && doc.EditorChangeCount == 0 && doc.ReadActivityStatistics()!.UsageOfUndo == uses + 1,
            "Protected fill replay changed bytes/change state or failed to record a successful Undo.");
        Console.WriteLine($"PASS {kind} equal successful versus protected fill: independent preallocated Undo/change state and exact replay");

        doc = Clean(native); clean = Bytes(doc); notices = 0; doc.Changed += (_, _) => notices++;
        using (var pen = doc.BeginLivePenStroke(doc.GetTile(3, 3)))
        {
            pen.Start(new(3, 3)); Expect(doc.EditorChangeCount == 1 && doc.HasChanges && notices == 1, "Initial equal LINE did not publish dirty/change state immediately.");
            pen.Move(new(3, 3)); Expect(doc.EditorChangeCount == 1 && notices == 1, "Stationary movement incremented source changes.");
            pen.Move(new(5, 3)); pen.Move(new(3, 3));
            Expect(doc.EditorChangeCount == 3 && notices == 3 && Same(doc, clean) && !doc.CanUndo, "Accepted equal LINE calls did not count independently of bytes/Undo.");
        }
        Expect(doc.GetHistory().Undo.Count == 1 && doc.GetHistory().ChangeCount == 1 && doc.GetHistory().EditorChangeCount == 3,
            "Three LINE calls were confused with three Undo operations.");
        doc.Undo(); Expect(Same(doc, clean) && doc.EditorChangeCount == 0 && !doc.HasChanges && doc.ReadActivityStatistics()!.UsageOfPaint == 3,
            "Pen Undo lost original count/clean state or reverted independent activity.");
        doc.Redo(); Expect(Same(doc, clean) && doc.EditorChangeCount == 3 && doc.HasChanges, "Pen Redo lost its captured source change count.");
        doc = Clean(native); clean = Bytes(doc);
        using (var origin = doc.BeginLivePenStroke(new(4, 2, 1))) { origin.Start(new(0, 0)); origin.Move(new(0, 0)); }
        Expect(Same(doc, clean) && doc.CanUndo && doc.GetHistory().ChangeCount == 1 && doc.EditorChangeCount == 0
            && !doc.HasChanges && doc.ReadActivityStatistics()!.UsageOfPaint == 0, "Origin sentinel must keep one Undo with no LINE/change/dirty increment.");
        doc.Undo(); Expect(Same(doc, clean) && !doc.HasChanges && doc.EditorChangeCount == 0, "Origin Undo changed the zero source count.");
        using (var boundary = doc.BeginLivePenStroke(new(4, 2, 1))) { boundary.Start(new(0, 3)); boundary.Move(new(0, 4)); }
        Expect(Same(doc, clean) && doc.EditorChangeCount == 2 && doc.HasChanges && doc.GetHistory().Undo.Count == 1,
            "Protected nonorigin LINE calls must count despite skipping locked writes.");
        Console.WriteLine($"PASS {kind} live equal/protected LINE increments, one Undo, immediate dirty notification, origin sentinel and independent activity");

        doc = Clean(native); clean = Bytes(doc); doc.SetHistoryDepth(0);
        EqualShape(doc, EditorPaintShape.Line);
        Expect(Same(doc, clean) && doc.HasChanges && doc.EditorChangeCount == 1 && !doc.CanUndo && !doc.CanRedo && doc.GetHistory().SnapshotBytes == 0,
            "Zero history blocked original change state or kept Undo payload.");
        doc.SetHistoryDepth(2); EqualShape(doc, EditorPaintShape.Line); EqualShape(doc, EditorPaintShape.Line); EqualShape(doc, EditorPaintShape.Line);
        Expect(doc.GetHistory().Undo.Count == 2 && doc.EditorChangeCount == 4, "Retained empty history depth or source counter differs.");
        doc.Undo(); doc.Undo(); Expect(doc.EditorChangeCount == 2 && doc.HasChanges && !doc.CanUndo && Same(doc, clean),
            "Truncated empty history lost its captured prehistory source changes.");
        EqualShape(doc, EditorPaintShape.Rectangle);
        Expect(doc.EditorChangeCount == 3 && doc.GetHistory().Undo.Count == 1 && !doc.CanRedo, "New equal drawing did not replace redo branch.");
        Console.WriteLine($"PASS {kind} zero/re-enabled/depth-limited equal history, captured source counts and redo branch replacement");
    }
    Console.WriteLine("EDITOR DRAWING HISTORY CORE SMOKE PASSED: actual mouse/full regression/save/nonpainting fidelity remain required");
    return 0;
}
catch (Exception error) { Console.Error.WriteLine(error); return 1; }
finally
{
    var permittedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DK2EditorDrawingHistory")) + Path.DirectorySeparatorChar;
    var finalDirectory = Path.GetFullPath(directory);
    if (!finalDirectory.StartsWith(permittedRoot, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Private fixture cleanup escaped its named test root.");
    Directory.Delete(finalDirectory, recursive: true);
}
