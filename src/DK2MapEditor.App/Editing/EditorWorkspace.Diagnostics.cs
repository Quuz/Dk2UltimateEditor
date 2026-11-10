using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    public Action PreserveDiagnosticHighlight()
    {
        var source=document;var previous=Canvas.HighlightedRegion;
        return ()=>{if(ReferenceEquals(source,document))Canvas.HighlightRegion(previous);};
    }
    public bool NavigateToDiagnostic(MapDiagnosticRegion region)
    {
        if(document is null||!Canvas.CanRemoveUnderCursor||EditorReportRules.Region(region,document.Width,document.Height) is not {} bounds)return false;
        if(!Canvas.NavigateCursorTo(new(bounds.X,bounds.Y)))return false;
        Canvas.HighlightRegion(bounds);
        MapScroll.ScrollToHorizontalOffset(Math.Max(0,(bounds.X+bounds.Width/2d)*Canvas.CellWidth-MapScroll.ViewportWidth/2));
        MapScroll.ScrollToVerticalOffset(Math.Max(0,(bounds.Y+bounds.Height/2d)*Canvas.CellHeight-MapScroll.ViewportHeight/2));
        return true;
    }
}
