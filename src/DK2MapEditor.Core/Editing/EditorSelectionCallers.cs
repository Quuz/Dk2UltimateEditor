namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private uint EditorSelectionRemovalCount(TileRect rectangle) => checked((uint)Records().Count(record =>
        IsPlacedThing(record) && TryThingPoint(record, out var point) && rectangle.Contains(point.X, point.Y)
        && !IsEditorProtectedCell(point.X, point.Y)));

    /// <summary>Source Select_Copy(Cut): one accepted cut plus each removed
    /// placed object, retaining one parent Undo even when all cells are equal.
    /// Copy buffer confirmation belongs to the caller and precedes this edit.</summary>
    public void CutEditorSelection(TileRect rectangle, MapTile fill)
        => EditEditorDrawing("Cut tiles and things", () =>
        {
            var removed = EditorSelectionRemovalCount(rectangle);
            CutSelection(rectangle, fill, respectEditorRules: true);
            return new(true, unchecked(1u + removed));
        });

    /// <summary>Source Select_Paste: one acceptance plus target removals.
    /// Inserted objects do not add source increments here. Clipping, protection,
    /// graph cloning and raw payload rules remain the generic paste's behavior.</summary>
    public void PasteEditorSelection(TilePoint position, SelectionClipboard clipboard)
    {
        ArgumentNullException.ThrowIfNull(clipboard);
        EditEditorDrawing("Paste tiles, things and scripts", () =>
        {
            CheckPoint(position.X, position.Y);
            var target = new TileRect(position.X, position.Y, Math.Min(clipboard.Tiles.Width, Width - position.X),
                Math.Min(clipboard.Tiles.Height, Height - position.Y));
            var removed = EditorSelectionRemovalCount(target);
            PasteSelection(position, clipboard, clipToMap: true, respectEditorRules: true);
            return new(true, unchecked(1u + removed));
        });
    }

    /// <summary>Source Rotate/Symmetry accept once and retain one parent Undo
    /// even for a symmetric/equal result. Things_RemoveAtMapPosEx adds one
    /// increment per selected physical thing, including protected-edge objects.
    /// Reinsertions add none; raw geometry/identity rules stay unchanged.</summary>
    public void TransformEditorSelection(TileRect rectangle, bool rotate, bool horizontal = false, bool clockwise = true)
        => EditEditorDrawing("Transform tiles and placed things", () =>
        {
            var moved = checked((uint)Records().Count(record => IsPlacedThing(record)
                && TryThingPoint(record, out var point) && rectangle.Contains(point.X, point.Y)));
            TransformSelection(rectangle, rotate, horizontal, clockwise, respectEditorRules: true);
            return new(true, unchecked(1u + moved));
        });
}
