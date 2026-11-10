namespace DK2MapEditor.App.Settings;

public sealed class AppSettings
{
    public string? Language { get; set; }
    public string? LastOpenedFile { get; set; }
    public uint AutoSaveMinutes { get; set; } = 5;
    public List<string>? RecentFiles { get; set; } = [];
    public int HistoryDepth { get; set; } = Core.Editing.NativeMapDocument.DefaultHistoryDepth;
    public Core.Editing.MapThumbnailOptions? Thumbnail { get; set; }
    public Core.Editing.MapObjectDisplayOptions? MapObjects { get; set; }
    public Core.Editing.MapOverlayOptions? MapOverlays { get; set; }
    public Core.Editing.MapZoomOptions? MapZoom { get; set; }
    public bool ShowTerrain { get; set; } = true;
    public bool ConfirmCopyBufferReplacement { get; set; } = true;
    public Editing.GameResourceSettings? GameResources { get; set; }
}
