using System.Windows;
using System.Windows.Input;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;
namespace DK2MapEditor.App;
public partial class MainWindow
{
    public bool RequestSourceKeyboardShortcut(Key key,ModifierKeys modifiers)
    {
        if(saveOwnerClosed||(modifiers&~(ModifierKeys.Control|ModifierKeys.Shift))!=0)return false;
        var special=EditorSpecialKeyboard.Resolve(key.ToString(),modifiers.HasFlag(ModifierKeys.Control),modifiers.HasFlag(ModifierKeys.Shift));
        if(special is not null)
        {
            if(document is null||!ReferenceEquals(Editor.Document,document)||!Editor.HasMapKeyboardFocus)return false;
            if(special.Page is {} page)return !document.HasActivePenStroke&&Editor.RequestCreatureBrushQuick(page);
            return Editor.HasMapKeyboardFocus&&Editor.RequestMapScroll(special);
        }
        var command=EditorKeyboardRoutes.Resolve(key.ToString(),modifiers.HasFlag(ModifierKeys.Control),modifiers.HasFlag(ModifierKeys.Shift));
        if(command is null)return false;
        if(command==EditorKeyboardCommand.Help){RequestGuide();return true;}
        if(command==EditorKeyboardCommand.New){New_Click(this,new RoutedEventArgs());return true;}
        if(document is null||!ReferenceEquals(Editor.Document,document)||document.HasActivePenStroke)return false;
        switch(command)
        {
            case EditorKeyboardCommand.Compile:Compile_Click(this,new RoutedEventArgs());break;
            case EditorKeyboardCommand.Test:Validate_Click(this,new RoutedEventArgs());break;
            case EditorKeyboardCommand.RotateClockwise:Editor.Rotate();break;
            case EditorKeyboardCommand.RotateCounterclockwise:Editor.RotateCounterclockwise();break;
            case EditorKeyboardCommand.MirrorVertical:Editor.MirrorVertical();break;
            case EditorKeyboardCommand.Rectangle:Editor.RequestDrawingTool(DrawingTool.Rectangle);break;
            case EditorKeyboardCommand.FilledRectangle:Editor.RequestDrawingTool(DrawingTool.FilledRectangle);break;
            case EditorKeyboardCommand.Ellipse:Editor.RequestDrawingTool(DrawingTool.Ellipse);break;
            case EditorKeyboardCommand.FilledEllipse:Editor.RequestDrawingTool(DrawingTool.FilledEllipse);break;
            case EditorKeyboardCommand.Line:Editor.RequestDrawingTool(DrawingTool.Line);break;
            case EditorKeyboardCommand.Pen:Editor.RequestDrawingTool(DrawingTool.Brush);break;
            case EditorKeyboardCommand.Fill:Editor.RequestDrawingTool(DrawingTool.Fill);break;
            case EditorKeyboardCommand.PlaceCreature:Editor.RequestObjectPlacementKind(PlacedThingKind.Creature);break;
            case EditorKeyboardCommand.PlaceDoor:Editor.RequestObjectPlacementKind(PlacedThingKind.Door);break;
            case EditorKeyboardCommand.PlaceTrap:Editor.RequestObjectPlacementKind(PlacedThingKind.Trap);break;
            case EditorKeyboardCommand.PlaceObject:Editor.RequestObjectPlacementKind(PlacedThingKind.Object);break;
            case EditorKeyboardCommand.PlaceSpecialItem:Editor.RequestObjectPlacementKind(PlacedThingKind.SpecialItem);break;
        }
        return true;
    }
}
