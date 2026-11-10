using System.Globalization;
using DK2MapEditor.Core.Editing;
namespace DK2MapEditor.App.Editing;
public partial class EditorWorkspace
{
    private CreatureBrushQuickWindow? brushQuickWindow;
    public Action<CreatureBrushQuickWindow>? BrushQuickPresenter{get;set;}
    private void CloseBrushQuick(){brushQuickWindow?.Abort();brushQuickWindow=null;}
    public bool RequestCreatureBrushQuick(BrushQuickPage page)
    {
        if(!Enum.IsDefined(page))throw new ArgumentOutOfRangeException(nameof(page));
        if(scenarioNamesOwnerClosed||document is not {} source||source.HasActivePenStroke||!CommitFields()||!ReferenceEquals(document,source)||source.HasActivePenStroke)return false;
        CloseBrushQuick();var revision=source.EditRevision;var template=ReadPaintingTemplate(PlacedThingKind.Creature);
        var window=new CreatureBrushQuickWindow(source,template,page,()=>!scenarioNamesOwnerClosed&&ReferenceEquals(document,source)&&source.EditRevision==revision&&!source.HasActivePenStroke,
            accepted=>
            {
                objectPaintingSession.Templates[PlacedThingKind.Creature]=new[]{accepted.TypeID.ToString(),accepted.Owner.ToString(),accepted.Health.ToString(),accepted.GoldHeld.ToString(),accepted.DataAll.ToString(),
                    accepted.HeroOptions.ToString(),accepted.CreatureOptions.ToString(),accepted.Objective.ToString(),accepted.Area.ToString(),accepted.Enemy.ToString()};
                if(objectBrushTemplate?.Kind==PlacedThingKind.Creature)objectBrushTemplate=accepted;
            });
        brushQuickWindow=window;window.Closed+=(_,_)=>{if(ReferenceEquals(brushQuickWindow,window))brushQuickWindow=null;};
        if(BrushQuickPresenter is {} present)present(window);else{window.Owner=System.Windows.Window.GetWindow(this);window.ShowDialog();}return true;
    }
    private ObjectBatchTemplate ReadPaintingTemplate(PlacedThingKind kind)
    {
        if(!objectPaintingSession.Templates.TryGetValue(kind,out var values))return ObjectBatchTemplate.Default(kind);
        uint U(int index)=>uint.Parse(values[index],NumberStyles.Integer,CultureInfo.InvariantCulture);
        return new(kind,U(0),checked((byte)U(1)),U(2),U(3),int.Parse(values[4],NumberStyles.Integer,CultureInfo.InvariantCulture),U(5),U(6),U(7),U(8),U(9));
    }
    public bool RequestDrawingTool(DrawingTool tool)
    {
        if(!Enum.IsDefined(tool))throw new ArgumentOutOfRangeException(nameof(tool));
        if(scenarioNamesOwnerClosed||document is not {} source||source.HasActivePenStroke||!CommitFields()||!ReferenceEquals(document,source)||source.HasActivePenStroke)return false;
        CancelRegionEditing();WorkspaceTabs.SelectedIndex=0;ToolPicker.SelectedValue=(int)tool;return true;
    }
    public bool RequestObjectPlacementKind(PlacedThingKind kind)
    {
        if(!Enum.IsDefined(kind))throw new ArgumentOutOfRangeException(nameof(kind));
        if(scenarioNamesOwnerClosed||document is not {} source||source.HasActivePenStroke||!CommitFields()||!ReferenceEquals(document,source)||source.HasActivePenStroke)return false;
        if(Canvas.Tool==DrawingTool.PlaceObject&&objectBrushTemplate?.Kind==kind)return RequestObjectBrush(kind,source);
        var template=ReadPaintingTemplate(kind);
        CancelRegionEditing();objectBrushTemplate=template;objectBrushUpgrade=false;objectPaintingStatusKey="Single.BrushReady";
        WorkspaceTabs.SelectedIndex=0;ToolPicker.SelectedValue=(int)DrawingTool.PlaceObject;RefreshPlacementStatus();return true;
    }
    private void RefreshSourceScrollPadding()
    {
        if(Canvas is null||MapScroll is null)return;
        var right=document is null?0:EditorSpecialKeyboard.Padding(document.Width,MapScroll.ViewportWidth,Canvas.CellWidth);
        var bottom=document is null?0:EditorSpecialKeyboard.Padding(document.Height,MapScroll.ViewportHeight,Canvas.CellHeight);
        var margin=new System.Windows.Thickness(0,0,right,bottom);if(Canvas.Margin!=margin)Canvas.Margin=margin;
    }
    public bool RequestMapScroll(EditorSpecialCommand command)
    {
        if(command.Scroll is not {} step||document is not {} source||scenarioNamesOwnerClosed||WorkspaceTabs.SelectedIndex!=0)return false;
        RefreshSourceScrollPadding();MapScroll.UpdateLayout();
        var viewport=command.Vertical?MapScroll.ViewportHeight:MapScroll.ViewportWidth;var cell=command.Vertical?Canvas.CellHeight:Canvas.CellWidth;
        var target=EditorSpecialKeyboard.Target(command.Vertical?source.Height:source.Width,viewport,cell,command.Vertical?MapScroll.VerticalOffset:MapScroll.HorizontalOffset,step);
        if(target is {} position)
        {
            var old=command.Vertical?MapScroll.VerticalOffset:MapScroll.HorizontalOffset;
            if(command.Vertical)MapScroll.ScrollToVerticalOffset(position);else MapScroll.ScrollToHorizontalOffset(position);
            MapScroll.UpdateLayout();
            if(ReferenceEquals(source,document)&&old!=(command.Vertical?MapScroll.VerticalOffset:MapScroll.HorizontalOffset))
            {
                var viewerPoint=System.Windows.Input.Mouse.GetPosition(MapScroll);var point=System.Windows.Input.Mouse.GetPosition(Canvas);
                if(viewerPoint.X>=0&&viewerPoint.Y>=0&&viewerPoint.X<MapScroll.ViewportWidth&&viewerPoint.Y<MapScroll.ViewportHeight
                    &&point.X>=0&&point.Y>=0&&point.X<Canvas.Width&&point.Y<Canvas.Height)Canvas.MoveMapPointer(point);
            }
        }
        return true;
    }
    public bool HasMapKeyboardFocus=>Canvas.IsKeyboardFocusWithin;
}
