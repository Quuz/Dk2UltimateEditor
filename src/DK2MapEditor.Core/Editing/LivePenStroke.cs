namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private LivePenStroke? activePenStroke;
    public bool HasActivePenStroke => activePenStroke is not null;

    /// <summary>Source pen: writes during the gesture, counts the initial LINE
    /// call/each distinct-cell line segment, and retains one complete Undo.
    /// Ending/canceling capture commits painted cells rather than erasing them.</summary>
    public LivePenStroke BeginLivePenStroke(MapTile selected, TileRect? limit = null)
    {
        if (transactionOpen || activePenStroke is not null) throw new InvalidOperationException("An edit is already active.");
        if (EditorTerrainRules.Footprint(selected).RequiresRectangle)
            throw new ArgumentException("Bounded terrain uses a geometric gesture, not a free pen.", nameof(selected));
        if (limit is { } rectangle && (rectangle.Width < 1 || rectangle.Height < 1 || rectangle.X < 0 || rectangle.Y < 0
            || (long)rectangle.X + rectangle.Width > Width || (long)rectangle.Y + rectangle.Height > Height))
            throw new ArgumentOutOfRangeException(nameof(limit));
        var stroke = new LivePenStroke(this, EditorTerrainRules.NormalizePaint(selected), limit);
        activePenStroke = stroke;
        try { HistoryChanged?.Invoke(this, EventArgs.Empty); }
        catch { activePenStroke = null; stroke.AbortBeforeStart(); throw; }
        return stroke;
    }

    public sealed class LivePenStroke : IDisposable
    {
        private readonly NativeMapDocument owner;
        private readonly NativeFile file;
        private readonly byte[] before;
        private readonly uint beforeEditorChangeCount;
        private readonly MapTile selected;
        private readonly TileRect? limit;
        private readonly int thread = Environment.CurrentManagedThreadId;
        private TilePoint previous;
        private bool started, closed, updating;
        public bool IsClosed => closed;

        internal LivePenStroke(NativeMapDocument owner, MapTile selected, TileRect? limit)
        { this.owner = owner; this.selected = selected; this.limit = limit; file = owner.TileFile; before = owner.PrepareHistory(()=>owner.historyBufferCopy(file.Bytes)); beforeEditorChangeCount = owner.editorChangeCount; }
        internal void AbortBeforeStart() => closed = true;

        private void Current()
        {
            ObjectDisposedException.ThrowIf(closed, this);
            if (thread != Environment.CurrentManagedThreadId || !ReferenceEquals(owner.activePenStroke, this) || updating)
                throw new InvalidOperationException("The pen gesture is not current or is reentrant.");
        }

        public void Start(TilePoint point)
        {
            Current();
            if (started) throw new InvalidOperationException("The pen already has an initial point.");
            owner.CheckPoint(point.X, point.Y);
            started = true; previous = point;
            // The source sentinel is the map cell(0,0), not an invalid point.
            // Mouse_Moved skips that first equal sentinel; other protected
            // initial cells still run LINE and count, while Paint skips writes.
            if (point == new TilePoint(0, 0)) return;
            var changed = Paint([point]);
            owner.editorChangeCount = unchecked(owner.editorChangeCount + 1);
            // Active Mouse_LeftButtonDown resets MapCursorPenOld to(0,0),
            // creates full-map Undo and calls Mouse_Moved immediately. For
            // a non-origin initial cell that is an accepted one-cell LINE,
            // even when its bytes were already equal. The unused simple
            // paint helper's changed-only counter does not govern the pen.
            owner.RecordActivity(ActivityCounterEvent.Paint);
            Notify(changed);
        }

        public void Move(TilePoint point)
        {
            Current();
            if (!started) throw new InvalidOperationException("Start the pen before moving it.");
            owner.CheckPoint(point.X, point.Y);
            if (point == previous) return;
            updating = true;
            TilePoint[] points;
            try
            {
                points = (OperatingSystem.IsWindows()
                    ? OriginalPaintRaster.Points(EditorPaintShape.Line, previous, point, TileRect.Between(previous, point))
                    : MapDrawing.Line(previous, point)).ToArray();
            }
            finally { updating = false; }
            var changed = Paint(points); previous = point;
            owner.editorChangeCount = unchecked(owner.editorChangeCount + 1);
            // Map_PaintElement LINE records one use even when all cells are
            // already equal/limited/protected; it never counts written cells.
            owner.RecordActivity(ActivityCounterEvent.Paint);
            Notify(changed);
        }

        private bool Paint(IEnumerable<TilePoint> points)
        {
            var changed = false;
            foreach (var point in points)
            {
                if (owner.IsEditorProtectedCell(point.X, point.Y) || limit is { } bounds && !bounds.Contains(point.X, point.Y)) continue;
                if (owner.GetTile(point.X, point.Y) == selected) continue;
                owner.PutTile(point.X, point.Y, selected); changed = true;
            }
            return changed;
        }

        private void Notify(bool changed)
        {
            // Accepted equal/protected LINE calls still change MapChanges and dirty state.
            owner.EditRevision++;
            // State/counters are complete before callbacks. A document switch
            // may dispose this stroke here and retain the completed frame.
            owner.Changed?.Invoke(owner, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (closed) return;
            Current();
            // Original Undo_CreateFullMap precedes the first pen LINE;
            // a successfully started equal gesture still has one Undo.
            // A reserved gesture that never starts does not invent one.
            try
            {
                if (started || !file.Data.Span.SequenceEqual(before))
                {
                    var afterCount=checked(owner.changeCount+1);var afterRevision=checked(owner.EditRevision+1);
                    Change? change=null;
                    if(owner.historyDepth!=0)
                    {
                        change=owner.PrepareHistory(()=>
                        {
                            var after=owner.historyBufferCopy(file.Bytes);
                            return new Change(owner.Operation("Brush", []),[new Delta(file,before,after)],owner.changeCount,afterCount)
                            {BeforeEditorChangeCount=beforeEditorChangeCount,AfterEditorChangeCount=owner.editorChangeCount};
                        });
                    }
                    owner.PublishHistoryChange(change);
                    owner.changeCount=afterCount;owner.EditRevision=afterRevision;
                }
            }
            catch
            {
                file.Bytes=before;owner.editorChangeCount=beforeEditorChangeCount;
                closed=true;owner.activePenStroke=null;
                // Paint activity/revisions already published during the gesture
                // stay independent. Redraw the rollback without masking its cause.
                try{owner.HistoryChanged?.Invoke(owner,EventArgs.Empty);owner.Changed?.Invoke(owner,EventArgs.Empty);}catch{ }
                throw;
            }
            finally{closed=true;owner.activePenStroke=null;}
            owner.HistoryChanged?.Invoke(owner, EventArgs.Empty);
            owner.Changed?.Invoke(owner, EventArgs.Empty);
        }
    }
}
