using DK2MapEditor.App.Editing;
internal static class SourceKeyboardChecks
{
    public static void Run()
    {
        var expected=new[]{("N",true,true,EditorKeyboardCommand.New),("C",true,true,EditorKeyboardCommand.Compile),("T",true,true,EditorKeyboardCommand.Test),
            ("F1",false,false,EditorKeyboardCommand.Help),("R",true,false,EditorKeyboardCommand.RotateClockwise),("R",true,true,EditorKeyboardCommand.RotateCounterclockwise),
            ("Y",true,true,EditorKeyboardCommand.MirrorVertical)};
        foreach(var (key,ctrl,shift,command) in expected)if(EditorKeyboardRoutes.Resolve(key,ctrl,shift)!=command)throw new Exception("Wrong source shortcut "+key);
        var tools=new[]{EditorKeyboardCommand.Rectangle,EditorKeyboardCommand.FilledRectangle,EditorKeyboardCommand.Ellipse,EditorKeyboardCommand.FilledEllipse,EditorKeyboardCommand.Line,EditorKeyboardCommand.Pen,EditorKeyboardCommand.Fill};
        for(var i=0;i<tools.Length;i++)if(EditorKeyboardRoutes.Resolve("F"+(i+1),true,false)!=tools[i])throw new Exception("Wrong source drawing key.");
        var kinds=new[]{EditorKeyboardCommand.PlaceCreature,EditorKeyboardCommand.PlaceDoor,EditorKeyboardCommand.PlaceTrap,EditorKeyboardCommand.PlaceObject,EditorKeyboardCommand.PlaceSpecialItem};
        for(var i=0;i<kinds.Length;i++)if(EditorKeyboardRoutes.Resolve("F"+(i+1),true,true)!=kinds[i])throw new Exception("Wrong source placement key.");
        foreach(var key in new[]{"N","Y","F1","F8","R","Insert","Delete","O","S","F"})
            foreach(var ctrl in new[]{false,true})foreach(var shift in new[]{false,true})
                if(EditorKeyboardRoutes.Resolve(key,ctrl,shift,true)!=null)throw new Exception("Alt chord unexpectedly captured.");
        if(EditorKeyboardRoutes.Resolve("N",true,false)!=null||EditorKeyboardRoutes.Resolve("Y",true,false)!=null
            ||EditorKeyboardRoutes.Resolve("F",true,false)!=null||EditorKeyboardRoutes.Resolve("F8",true,false)!=null)
            throw new Exception("Conflict, source search stub or unassigned shortcut silently captured.");
        if(EditorKeyboardRoutes.Bindings.Count!=19||EditorKeyboardRoutes.Bindings.Select(v=>v.Gesture).Distinct().Count()!=19)throw new Exception("Duplicate/incomplete source route table.");
    }
}
