using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckActivityEvents(string language, bool native)
    {
        var doc = NativeMapDocument.CreateNewMap(new("EventActivity", "EventActivity", 24, 24, NativeFormat: native));
        var workspace = new EditorWorkspace { Document = doc }; var canvas = Control<MapCanvas>(workspace, "Canvas");
        void Layout() { workspace.Measure(new Size(1400, 900)); workspace.Arrange(new Rect(0, 0, 1400, 900)); workspace.UpdateLayout(); }
        ActivityStatisticsPayload Stats() => doc.ReadActivityStatistics()!;
        Layout(); canvas.PaintTile = new(4, 2, 1); canvas.Tool = DrawingTool.Brush;
        canvas.PaintStroke([new(2, 2), new(3, 2), new(2, 2)]);
        Expect(Stats().UsageOfPaint == 1 && Stats().LMBClicks == 0 && Stats().UsageOfPaintFill == 0, "Prepared stroke counted cells/duplicates or fabricated a mouse click.");
        canvas.PaintStroke([new(2, 2), new(3, 2)]); Expect(Stats().UsageOfPaint == 1, "Equal simple paint counted.");
        var undo = doc.GetHistory().Undo.Count;
        canvas.Tool = DrawingTool.Line; canvas.DrawGesture(new(2, 2), new(3, 2));
        Expect(Stats().UsageOfPaint == 2 && doc.GetHistory().Undo.Count == undo + 1, "Accepted equal geometry must keep its preallocated source Undo.");
        canvas.RandomPainting = true; canvas.DrawGesture(new(4, 4), new(7, 4), randomSample: _ => 1);
        Expect(Stats().UsageOfPaintRnd == 1 && Stats().UsageOfPaint == 2 && doc.GetHistory().Undo.Count == undo + 2, "Source random no-hit must retain one separate Undo and one random use.");
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        try { canvas.DrawGesture(new(5, 5), new(7, 5), randomSample: _ => 2); throw new Exception("Invalid random sample accepted."); }
        catch (ArgumentException) { }
        Expect(Stats().UsageOfPaintRnd == 1 && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Failed random preparation changed counters/bytes.");
        canvas.RandomPainting = false; canvas.Tool = DrawingTool.Fill;
        canvas.FillAt(new(2, 2)); Expect(Stats().UsageOfPaintFill == 1, "Accepted equal source fill was omitted.");
        canvas.PaintTile = new(3, 2, 1); canvas.FillAt(new(2, 2));
        Expect(Stats().UsageOfPaintFill == 2 && Stats().UsageOfPaint == 2, "Actual fill uses were conflated with ordinary painting.");
        canvas.PaintTile = new(14, 3, 1); var paint = Stats().UsageOfPaint;
        canvas.SetSelection(new(2, 2, 1, 1)); canvas.FillAt(new(2, 2));
        Expect(Stats().UsageOfPaint == paint && canvas.LastPlacementResult != EditorPlacementResult.Painted, "Rejected source footprint counted.");
        canvas.ClearSelection(); canvas.FillAt(new(5, 5));
        Expect(Stats().UsageOfPaint == paint + 1 && Stats().UsageOfPaintFill == 2, "Forced terrain footprint was counted as flood fill.");
        canvas.GeometryOnly = true; var geometry = Stats(); canvas.FillAt(new(6, 6)); canvas.DrawGesture(new(6, 6), new(8, 8));
        Expect(Stats().Encode().AsSpan().SequenceEqual(geometry.Encode()), "Read-only geometry canvas counted editing activity."); canvas.GeometryOnly = false;

        bool Invoke(string name, params object[] values) => (bool)typeof(MapCanvas).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(canvas, values)!;
        Point Pointer(int x, int y) => canvas.CellBounds(new(x, y, 1, 1)).TopLeft + new Vector(1, 1);
        var left = Stats().LMBClicks; var right = Stats().RMBClicks;
        Expect(!Invoke("BeginLeftMapPointer", new Point(-1, 1)) && !Invoke("BeginRightMapPointer", new Point(double.NaN, 1)), "Out-of-map/nonfinite pointer accepted.");
        Expect(Stats().LMBClicks == left && Stats().RMBClicks == right, "Out-of-map pointer counted.");
        canvas.Tool = DrawingTool.Select;
        Expect(Invoke("BeginLeftMapPointer", Pointer(2, 2)) && Stats().LMBClicks == left + 1, "Shared real left-pointer handler did not count accepted click.");
        Expect(Invoke("BeginRightMapPointer", Pointer(3, 3)) && Stats().RMBClicks == right, "Right cancellation of a pending gesture counted a new selection.");
        Expect(Invoke("BeginRightMapPointer", Pointer(3, 3)) && Stats().RMBClicks == right + 1, "Shared right-pointer selection handler did not count.");
        Expect(Invoke("CompleteRightMapPointer", Pointer(5, 4), false, false)
            && canvas.Selection == new TileRect(3, 3, 3, 2) && canvas.Tool == DrawingTool.Select, "Right selection painted/switched tools or has incorrect bounds.");
        Expect(!Invoke("CompleteRightMapPointer", Pointer(6, 6), false, false) && Stats().RMBClicks == right + 1, "Release without a right gesture counted again.");
        canvas.BeginRegionPick(true); Invoke("BeginRightMapPointer", Pointer(2, 2));
        Expect(!canvas.IsPickingRegion && Stats().RMBClicks == right + 1, "Right region-pick cancellation counted as selection.");
        canvas.Tool = DrawingTool.Fill; canvas.PaintTile = new(3, 2, 1);
        left = Stats().LMBClicks; Invoke("BeginLeftMapPointer", Pointer(0, 0));
        Expect(Stats().LMBClicks == left + 1 && Stats().UsageOfPaintFill == 2, "Free fill border click was omitted or protected fill counted.");
        left = Stats().LMBClicks;
        canvas.ClearSelection(); canvas.PaintTile = new(4, 2, 1); Invoke("BeginLeftMapPointer", Pointer(10, 10));
        Expect(Stats().LMBClicks == left + 1 && Stats().UsageOfPaintFill == 3, "Actual left-handler fill did not count independent mouse and operation uses.");
        // Use the actual single-object dialog's Use-as-brush command.
        doc.Paint([new(12, 12)], new(3, 2, 1));
        var window = workspace.CreateObjectPaintingWindow(new(12, 12))!;
        T Field<T>(string name) => (T)window.FindName(name);
        Field<ComboBox>("BatchFamily").SelectedValue = "1";
        Field<TextBox>("BatchTypeID").Text = "6"; Field<TextBox>("BatchOwner").Text = "0";
        Field<Button>("SingleUseBrush").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var things = Stats().UsageOfPaintThings; canvas.PaintObjectAt(new(12, 12));
        Expect(Stats().UsageOfPaintThings == things + 1, "Actual object brush did not record successful source painting.");
        canvas.PaintObjectAt(new(0, 0)); Expect(Stats().UsageOfPaintThings == things + 1, "Blocked object brush counted.");
        left = Stats().LMBClicks; Invoke("BeginLeftMapPointer", Pointer(0, 0));
        Expect(Stats().LMBClicks == left && Stats().UsageOfPaintThings == things + 1, "Fixed1x1 source object admitted/counts protected initial footprint.");
        var undoUses = Stats().UsageOfUndo; workspace.Undo();
        Expect(Stats().UsageOfUndo == undoUses + 1 && Stats().UsageOfPaintThings == things + 1, "Workspace Undo double-counted or reverted independent object activity.");
        workspace.Redo(); Expect(Stats().UsageOfUndo == undoUses + 1, "Workspace Redo counted as Undo.");
        window.Close();
        canvas.ClearSelection(); canvas.Tool = DrawingTool.Brush; canvas.PaintTile = new(4, 2, 1);
        var boundaryBytes = doc.TileFile.Data.ToArray(); var boundaryHistory = doc.GetHistory().Undo.Count;
        left = Stats().LMBClicks; paint = Stats().UsageOfPaint;
        Invoke("BeginLeftMapPointer", Pointer(0, 3));
        Expect(doc.HasActivePenStroke && Stats().LMBClicks == left + 1 && Stats().UsageOfPaint == paint + 1
            && doc.TileFile.Data.Span.SequenceEqual(boundaryBytes), "Protected free pen start was rejected, uncounted or wrote locked bytes.");
        Invoke("CompleteLeftMapPointer", Pointer(0, 3), false);
        Expect(doc.GetHistory().Undo.Count == boundaryHistory + 1, "Protected source pen did not retain one source Undo.");
        Invoke("BeginLeftMapPointer", Pointer(0, 0));
        Expect(doc.HasActivePenStroke && Stats().LMBClicks == left + 2 && Stats().UsageOfPaint == paint + 1, "Initial origin sentinel counted LINE or rejected source click.");
        typeof(MapCanvas).GetMethod("MoveMapPointer", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(canvas, [Pointer(0, 0)]);
        Expect(Stats().UsageOfPaint == paint + 1, "Origin stationary movement counted.");
        typeof(MapCanvas).GetMethod("MoveMapPointer", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(canvas, [Pointer(1, 0)]);
        Expect(Stats().UsageOfPaint == paint + 2 && doc.TileFile.Data.Span.SequenceEqual(boundaryBytes), "Protected first move after sentinel lost source count/protection.");
        Invoke("CompleteLeftMapPointer", Pointer(1, 0), false);
        // Actual mouse overrides call these shared point handlers. Assert
        // intermediate frames, not just a prepared/final stroke snapshot.
        canvas.ClearSelection(); canvas.Tool = DrawingTool.Brush; canvas.PaintTile = new(4, 2, 1);
        // Earlier fill may already equal the pen colour. Give the live-frame
        // and release-exclusion checks a known contrasting patch.
        doc.Paint(MapDrawing.Rectangle(new(16, 16, 5, 6), true), new(3, 2, 1));
        Expect(doc.GetTile(16, 16) == new MapTile(3, 2, 1) && doc.GetTile(20, 16) == new MapTile(3, 2, 1), "Live mouse fixture is not contrasting.");
        var penBefore = doc.TileFile.Data.ToArray(); var penHistory = doc.GetHistory().Undo.Count;
        paint = Stats().UsageOfPaint; left = Stats().LMBClicks;
        Expect(Invoke("BeginLeftMapPointer", Pointer(16, 16)) && doc.HasActivePenStroke
            && doc.GetTile(16, 16) == new MapTile(4, 2, 1) && Stats().UsageOfPaint == paint + 1
            && Stats().LMBClicks == left + 1 && !doc.CanUndo && doc.GetHistory().Undo.Count == penHistory,
            "Shared mouse down deferred initial paint, lost ownership or counted cells/history.");
        void Move(int x, int y) => typeof(MapCanvas).GetMethod("MoveMapPointer", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(canvas, [Pointer(x, y)]);
        Move(16, 16); Expect(Stats().UsageOfPaint == paint + 1, "Stationary mouse frame counted a segment.");
        Move(19, 16);
        Expect(Enumerable.Range(16, 4).All(x => doc.GetTile(x, 16) == new MapTile(4, 2, 1))
            && Stats().UsageOfPaint == paint + 2 && doc.HasActivePenStroke, "Real move deferred segment or counted its cells.");
        Move(16, 16); Expect(Stats().UsageOfPaint == paint + 3, "Equal reverse mouse segment omitted source count.");
        Expect(Invoke("CompleteLeftMapPointer", Pointer(20, 16), true) && !doc.HasActivePenStroke
            && doc.GetTile(20, 16) != new MapTile(4, 2, 1) && Stats().UsageOfPaint == paint + 3
            && doc.GetHistory().Undo.Count == penHistory + 1, "Mouse up invented a segment, applied Shift random or split history.");
        var penAfter = doc.TileFile.Data.ToArray(); undoUses = Stats().UsageOfUndo;
        workspace.Undo(); Expect(doc.TileFile.Data.Span.SequenceEqual(penBefore) && Stats().UsageOfPaint == paint + 3
            && Stats().UsageOfUndo == undoUses + 1, "Mouse pen Undo lost exact bytes or reverted activity.");
        workspace.Redo(); Expect(doc.TileFile.Data.Span.SequenceEqual(penAfter), "Mouse pen Redo differs.");
        // Follow the active source call graph, not the uncalled simple helper:
        // an equal initial one-cell LINE counts and retains one source Undo.
        penHistory = doc.GetHistory().Undo.Count; paint = Stats().UsageOfPaint;
        Expect(Invoke("BeginLeftMapPointer", Pointer(16, 16)) && doc.HasActivePenStroke && Stats().UsageOfPaint == paint + 1,
            "Actual equal pen down omitted the source initial LINE use.");
        Move(16, 16); Expect(Stats().UsageOfPaint == paint + 1, "Stationary event repeated the initialized source first LINE.");
        Invoke("CompleteLeftMapPointer", Pointer(16, 16), false);
        Expect(doc.TileFile.Data.Span.SequenceEqual(penAfter) && doc.GetHistory().Undo.Count == penHistory + 1,
            "Equal active pen changed bytes or omitted its source full-map Undo.");
        undoUses = Stats().UsageOfUndo; workspace.Undo();
        Expect(doc.TileFile.Data.Span.SequenceEqual(penAfter) && Stats().UsageOfPaint == paint + 1 && Stats().UsageOfUndo == undoUses + 1,
            "Actual equal pen Undo changed bytes/activity or was missing."); workspace.Redo();
        penHistory = doc.GetHistory().Undo.Count; right = Stats().RMBClicks;
        Invoke("BeginLeftMapPointer", Pointer(16, 18)); Move(19, 18);
        Expect(Invoke("BeginRightMapPointer", Pointer(19, 18)) && !doc.HasActivePenStroke
            && Stats().RMBClicks == right && doc.GetHistory().Undo.Count == penHistory + 1
            && doc.GetTile(19, 18) == new MapTile(4, 2, 1), "Right capture cancellation erased frames, counted selection or split Undo.");
        penHistory = doc.GetHistory().Undo.Count;
        Invoke("BeginLeftMapPointer", Pointer(16, 20)); Move(19, 20);
        canvas.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        Expect(!doc.HasActivePenStroke && doc.GetHistory().Undo.Count == penHistory + 1
            && doc.GetTile(19, 20) == new MapTile(4, 2, 1), "Canvas unload leaked pen or discarded completed frames.");
        penHistory = doc.GetHistory().Undo.Count;
        Invoke("BeginLeftMapPointer", Pointer(16, 21)); Move(19, 21);
        var replacement = NativeMapDocument.CreateNewMap(new("EventReplacement", "EventReplacement", 24, 24, NativeFormat: native));
        workspace.Document = replacement;
        Expect(!doc.HasActivePenStroke && doc.GetHistory().Undo.Count == penHistory + 1
            && !replacement.HasActivePenStroke && replacement.ReadActivityStatistics()!.UsageOfPaint == 0
            && doc.GetTile(19, 21) == new MapTile(4, 2, 1), "Document switch leaked ownership, discarded frames or painted replacement.");
        var areas = new[] { new TileRect(2, 5, 3, 3), new TileRect(10, 5, 3, 3), new TileRect(16, 5, 3, 3) }
            .Select(bounds => replacement.AddMapRegion(MapRegionKind.Area, bounds)).ToArray();
        uint ID(int index) => replacement.PatrolAreas().Single(point => point.Record.File == areas[index].File && point.Record.Offset == areas[index].Offset).ID;
        uint Next(int index) => replacement.PatrolAreas().Single(point => point.ID == ID(index)).NextID;
        void Right(int x, bool shift, bool control)
        {
            Expect(Invoke("BeginRightMapPointer", Pointer(x, 6)) && Invoke("CompleteRightMapPointer", Pointer(x, 6), shift, control),
                "Right patrol selection was not completed by shared actual handlers.");
        }
        var patrolHistory = replacement.GetHistory().Undo.Count;
        Right(3, false, true); Expect(Next(0) == 0 && replacement.GetHistory().Undo.Count == patrolHistory, "Control start invented a path edit.");
        Right(11, true, false);
        Expect(Next(0) == ID(1) && Next(1) == 0 && replacement.GetHistory().Undo.Count == patrolHistory + 1, "Shift failed to continue selected source path in one edit.");
        var patrolBytes = replacement.Files.ToDictionary(file => file, file => file.Data.ToArray());
        Right(3, false, true);
        Expect(patrolBytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Control selected an already linked source area.");
        Right(17, false, true); Right(11, true, false);
        Expect(Next(2) == ID(1) && Next(0) == ID(1), "Control restart/Shift join destroyed another path.");
        Right(3, true, false); Expect(Next(1) == ID(0), "Source Shift loop/join refused a linked target.");
        patrolBytes = replacement.Files.ToDictionary(file => file, file => file.Data.ToArray());
        Right(17, true, false);
        Expect(patrolBytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Shift overwrote an already linked source endpoint.");
        Expect(replacement.ReadActivityStatistics()!.RMBClicks == 7 && replacement.ReadActivityStatistics()!.UsageOfPaint == 0,
            "Right patrol selection counted links as terrain paint or missed accepted selections.");
        workspace.Document = null;
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} activity operation/selection and live mouse pen: simple/geometric/random/fill/footprint/object counters, failures/no-ops/protection, bounded left/right/cancel, immediate source segments, one exact Undo/Redo, unload/replacement and Control/Shift patrol start/join/loop/refusal");
    }
}
