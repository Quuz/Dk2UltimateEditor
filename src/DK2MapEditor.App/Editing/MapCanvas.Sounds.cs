using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class MapCanvas
{
    public event Action<EditorSoundRequest>? SoundRequested;
    private void RequestDrawingSound(EditorSoundAction action,EditorSoundPaintMode mode=EditorSoundPaintMode.Pen,TileRect? rectangle=null)
    {
        if(document is null||GeometryOnly||IsPickingRegion)return;
        SoundRequested?.Invoke(new(action,mode,PaintTile.Terrain,PaintTile.Owner,Width:rectangle?.Width??1,Height:rectangle?.Height??1));
    }
}
