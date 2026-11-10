namespace DK2MapEditor.App.Editing;
public enum EditorKeyboardCommand
{
    New,Compile,Test,Help,RotateClockwise,RotateCounterclockwise,MirrorVertical,
    Rectangle,FilledRectangle,Ellipse,FilledEllipse,Line,Pen,Fill,
    PlaceCreature,PlaceDoor,PlaceTrap,PlaceObject,PlaceSpecialItem
}
public static class EditorKeyboardRoutes
{
    public static IReadOnlyList<(string Gesture,EditorKeyboardCommand Command)> Bindings {get;}=Array.AsReadOnly(new[]{
        ("Ctrl+Shift+N",EditorKeyboardCommand.New),("Ctrl+Shift+C",EditorKeyboardCommand.Compile),("Ctrl+Shift+T",EditorKeyboardCommand.Test),
        ("F1",EditorKeyboardCommand.Help),("Ctrl+R",EditorKeyboardCommand.RotateClockwise),("Ctrl+Shift+R",EditorKeyboardCommand.RotateCounterclockwise),
        ("Ctrl+Shift+Y",EditorKeyboardCommand.MirrorVertical),("Ctrl+F1",EditorKeyboardCommand.Rectangle),("Ctrl+F2",EditorKeyboardCommand.FilledRectangle),
        ("Ctrl+F3",EditorKeyboardCommand.Ellipse),("Ctrl+F4",EditorKeyboardCommand.FilledEllipse),("Ctrl+F5",EditorKeyboardCommand.Line),
        ("Ctrl+F6",EditorKeyboardCommand.Pen),("Ctrl+F7",EditorKeyboardCommand.Fill),("Ctrl+Shift+F1",EditorKeyboardCommand.PlaceCreature),
        ("Ctrl+Shift+F2",EditorKeyboardCommand.PlaceDoor),("Ctrl+Shift+F3",EditorKeyboardCommand.PlaceTrap),("Ctrl+Shift+F4",EditorKeyboardCommand.PlaceObject),
        ("Ctrl+Shift+F5",EditorKeyboardCommand.PlaceSpecialItem)});
    public static EditorKeyboardCommand? Resolve(string key,bool control,bool shift,bool alt=false)
    {
        if(alt)return null;var gesture=(control?"Ctrl+":"")+(shift?"Shift+":"")+key;
        foreach(var binding in Bindings)if(binding.Gesture==gesture)return binding.Command;
        return null;
    }
}
