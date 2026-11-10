using DK2MapEditor.Core.Editing;

SourceFootprintChecks.Run();
void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
void Refuses<T>(Action action) where T : Exception
{ try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
foreach (var native in new[] { false, true })
{
    var doc = NativeMapDocument.CreateNewMap(new("LivePen", "LivePen", 12, 12, NativeFormat: native));
    var before = doc.TileFile.Data.ToArray(); var oldPaint = doc.ReadActivityStatistics()!.UsageOfPaint;
    var initialHistory = doc.GetHistory(); var events = 0;
    doc.Changed += (_, _) => events++;
    var stroke = doc.BeginLivePenStroke(new(4, 2, 1));
    Expect(doc.HasActivePenStroke && !doc.CanUndo && !doc.CanRedo && doc.GetHistory().Undo.Count == initialHistory.Undo.Count, "Live pen did not reserve independent gesture ownership.");
    stroke.Start(new(2, 2));
    Expect(doc.GetTile(2, 2) == new MapTile(4, 2, 1) && doc.HasChanges && events == 1
        && doc.ReadActivityStatistics()!.UsageOfPaint == oldPaint + 1, "First cell was deferred or counter did not precede notification.");
    stroke.Move(new(2, 2)); Expect(doc.ReadActivityStatistics()!.UsageOfPaint == oldPaint + 1 && events == 1, "Stationary movement counted/rewrote.");
    stroke.Move(new(5, 2));
    Expect(Enumerable.Range(2, 4).All(x => doc.GetTile(x, 2) == new MapTile(4, 2, 1))
        && doc.ReadActivityStatistics()!.UsageOfPaint == oldPaint + 2 && events == 2, "One horizontal source segment counted cells or was not painted immediately.");
    stroke.Move(new(2, 2)); Expect(doc.ReadActivityStatistics()!.UsageOfPaint == oldPaint + 3 && events == 3 && doc.EditorChangeCount == 3, "Equal reverse LINE must publish its source change count despite equal bytes.");
    Refuses<InvalidOperationException>(doc.Undo); Refuses<InvalidOperationException>(doc.Redo);
    Refuses<InvalidOperationException>(() => doc.Paint([new(6, 6)], new(3, 2, 1)));
    Refuses<InvalidOperationException>(() => doc.SetHistoryDepth(0));
    Refuses<InvalidOperationException>(() => doc.SaveCopy("unused-live-pen-export"));
    Refuses<InvalidOperationException>(() => doc.BeginLivePenStroke(new(3, 2, 1)));
    Expect(doc.ReadActivityStatistics()!.SavedTimes == 0 && doc.GetHistory().Undo.Count == initialHistory.Undo.Count, "Refused concurrent operations changed counters/history.");
    stroke.Dispose(); stroke.Dispose();
    Expect(!doc.HasActivePenStroke && doc.GetHistory().Undo.Count == initialHistory.Undo.Count + 1 && doc.CanUndo
        && doc.GetHistory().ChangeCount == initialHistory.ChangeCount + 1, "Capture termination did not retain exactly one committed Undo.");
    var after = doc.TileFile.Data.ToArray(); var undoUses = doc.ReadActivityStatistics()!.UsageOfUndo;
    doc.Undo(); Expect(doc.TileFile.Data.Span.SequenceEqual(before) && doc.ReadActivityStatistics()!.UsageOfPaint == oldPaint + 3
        && doc.ReadActivityStatistics()!.UsageOfUndo == undoUses + 1, "One pen Undo lost raw bytes or reverted statistics.");
    doc.Redo(); Expect(doc.TileFile.Data.Span.SequenceEqual(after) && doc.ReadActivityStatistics()!.UsageOfUndo == undoUses + 1, "Pen Redo differs or counted another Undo.");
    Refuses<ObjectDisposedException>(() => stroke.Move(new(7, 7)));
    Console.WriteLine($"PASS {(native ? "native" : "project")} live pen immediate initial/segment source counts, stationary/equal segment, guards, single exact Undo/Redo and lifetime");

    var noChange = NativeMapDocument.CreateNewMap(new("NoChangePen", "NoChangePen", 12, 12, NativeFormat: native));
    var noChangeInitialDirty = noChange.HasChanges;
    var noChangeInitialFiles = noChange.Files.Select(file => file.Data.ToArray()).ToArray();
    using (var equal = noChange.BeginLivePenStroke(new(2, 2, 1)))
    {
        equal.Start(new(3, 3)); Expect(noChange.ReadActivityStatistics()!.UsageOfPaint == 1, "Active source initial LINE did not count equal tile.");
        equal.Move(new(3, 3)); Expect(noChange.ReadActivityStatistics()!.UsageOfPaint == 1, "Stationary frame counted after source sentinel initialization.");
        equal.Move(new(4, 3)); Expect(noChange.ReadActivityStatistics()!.UsageOfPaint == 2, "Accepted equal segment did not count once.");
    }
    Expect(noChange.CanUndo && noChange.GetHistory().ChangeCount == 1 && noChange.HasChanges && noChange.EditorChangeCount == 2, "Equal started source pen must retain one Undo and two source change increments.");
    Expect(noChange.Files.Select((file, index) => file.Data.Span.SequenceEqual(noChangeInitialFiles[index])).All(equal => equal), "Equal pen changed a non-tile file.");
    var equalBytes = noChange.TileFile.Data.ToArray();
    noChange.Undo(); Expect(noChange.TileFile.Data.Span.SequenceEqual(equalBytes) && noChange.ReadActivityStatistics()!.UsageOfUndo == 1
        && noChange.ReadActivityStatistics()!.UsageOfPaint == 2, "Equal source pen Undo changed bytes or lost independent activity.");
    noChange.Redo(); Expect(noChange.TileFile.Data.Span.SequenceEqual(equalBytes) && noChange.ReadActivityStatistics()!.UsageOfUndo == 1, "Equal source pen Redo changed bytes/counters.");
    var empty = noChange.BeginLivePenStroke(new(3, 2, 1));
    Refuses<InvalidOperationException>(() => empty.Move(new(3, 3)));
    Refuses<ArgumentOutOfRangeException>(() => empty.Start(new(-1, 0))); empty.Dispose();
    Refuses<ArgumentException>(() => noChange.BeginLivePenStroke(new(14, 3, 1)));
    Refuses<ArgumentOutOfRangeException>(() => noChange.BeginLivePenStroke(new(3, 2, 1), new(11, 11, 2, 2)));
    using (var limited = noChange.BeginLivePenStroke(new(4, 2, 1), new(5, 5, 2, 2)))
    { limited.Start(new(3, 3)); limited.Move(new(4, 3)); Expect(noChange.ReadActivityStatistics()!.UsageOfPaint == 4 && noChange.HasChanges && noChange.EditorChangeCount == 4, "Masked accepted LINE counts or original change state differ."); }
    noChange.SetHistoryDepth(0);
    using (var disabled = noChange.BeginLivePenStroke(new(4, 2, 1))) { disabled.Start(new(3, 3)); disabled.Move(new(4, 3)); }
    Expect(noChange.GetTile(3, 3) == new MapTile(4, 2, 1) && !noChange.CanUndo && noChange.GetHistory().SnapshotBytes == 0, "Zero history blocked painting or retained snapshots.");
    var edge = NativeMapDocument.CreateNewMap(new("EdgePen", "EdgePen", 12, 12, NativeFormat: native));
    var edgeInitialDirty = edge.HasChanges;
    var edgeBytes = edge.TileFile.Data.ToArray();
    using (var protectedPen = edge.BeginLivePenStroke(new(4, 2, 1)))
    {
        protectedPen.Start(new(0, 3)); protectedPen.Move(new(0, 4));
        Expect(edge.ReadActivityStatistics()!.UsageOfPaint == 2 && edge.TileFile.Data.Span.SequenceEqual(edgeBytes), "Protected initial/segment did not count source LINE or changed locked bytes.");
    }
    Expect(edge.CanUndo && edge.HasChanges && edge.EditorChangeCount == 2, "Protected source LINE must retain Undo and count without locked writes."); edge.Undo();
    using (var origin = edge.BeginLivePenStroke(new(4, 2, 1)))
    {
        origin.Start(new(0, 0)); origin.Move(new(0, 0)); Expect(edge.ReadActivityStatistics()!.UsageOfPaint == 2, "Origin sentinel was not skipped or stationary frame counted.");
        origin.Move(new(1, 0)); Expect(edge.ReadActivityStatistics()!.UsageOfPaint == 3 && edge.HasChanges && edge.EditorChangeCount == 1, "First protected motion after sentinel must count one source change.");
        origin.Move(new(1, 1)); Expect(edge.ReadActivityStatistics()!.UsageOfPaint == 4 && edge.GetTile(1, 1) == new MapTile(4, 2, 1), "Source pen could not enter interior from protected origin.");
    }
    edge.Undo(); Expect(edge.TileFile.Data.Span.SequenceEqual(edgeBytes), "Origin-started pen did not undo in one step.");
    Console.WriteLine($"PASS {(native ? "native" : "project")} equal/empty/limited/protected/invalid pen and zero-history behavior");
}
var callbackDoc = NativeMapDocument.CreateNewMap(new("CallbackPen", "CallbackPen", 12, 12));
var failedOnce = true;
EventHandler failedBegin = (_, _) => { if (failedOnce) { failedOnce = false; throw new IOException("History callback failed"); } };
callbackDoc.HistoryChanged += failedBegin;
Refuses<IOException>(() => callbackDoc.BeginLivePenStroke(new(4, 2, 1)));
Expect(!callbackDoc.HasActivePenStroke && !callbackDoc.CanUndo, "Failed begin callback leaked active ownership.");
callbackDoc.HistoryChanged -= failedBegin;
NativeMapDocument.LivePenStroke? callbackStroke = null;
callbackDoc.Changed += (_, _) =>
{
    if (callbackDoc.HasActivePenStroke)
    {
        Expect(callbackDoc.ReadActivityStatistics()!.UsageOfPaint == 1, "Frame callback preceded source statistics.");
        callbackStroke!.Dispose();
    }
};
callbackStroke = callbackDoc.BeginLivePenStroke(new(4, 2, 1)); callbackStroke.Start(new(3, 3));
Expect(callbackStroke.IsClosed && !callbackDoc.HasActivePenStroke && callbackDoc.CanUndo && callbackDoc.GetTile(3, 3) == new MapTile(4, 2, 1), "Reentrant owner/capture closure lost a completed frame or leaked stroke.");
callbackDoc.Undo(); Expect(callbackDoc.GetTile(3, 3) == new MapTile(2, 2, 1), "Callback-closed pen cannot undo its first frame.");
Console.WriteLine("PASS failed begin recovery, frame notification ordering and reentrant capture closure retaining a single Undo");
Console.WriteLine("LIVE PEN CORE SMOKE PASSED: actual mouse route and full regression still required");
