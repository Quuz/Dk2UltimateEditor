using DK2MapEditor.App.Editing;

internal static class EditorSoundRulesChecks
{
    public static void Run()
    {
        var cases=new (string Name,EditorSoundRequest Request,EditorSoundCategories Categories,EditorSoundCue? Expected)[]
        {
            ("tool button",new(EditorSoundAction.ToolChoice),EditorSoundCategories.All,EditorSoundCue.ButtonPaint),
            ("terrain button",new(EditorSoundAction.TerrainChoice),EditorSoundCategories.All,EditorSoundCue.ButtonTerrain),
            ("tool muted with terrain only",new(EditorSoundAction.ToolChoice),EditorSoundCategories.Terrain,null),
            ("terrain button muted",new(EditorSoundAction.TerrainChoice),EditorSoundCategories.None,null),
            ("define rectangle",new(EditorSoundAction.DefineRectangle),EditorSoundCategories.Terrain,EditorSoundCue.Selection),
            ("rectangle muted with interface only",new(EditorSoundAction.DefineRectangle),EditorSoundCategories.Interface,null),
            ("imp",new(EditorSoundAction.Paint,EditorSoundPaintMode.Creature,ObjectID:1),EditorSoundCategories.Terrain,EditorSoundCue.Imp),
            ("other creature",new(EditorSoundAction.Paint,EditorSoundPaintMode.Creature,ObjectID:15),EditorSoundCategories.Terrain,EditorSoundCue.Creature),
            ("creature muted",new(EditorSoundAction.Paint,EditorSoundPaintMode.Creature,ObjectID:1),EditorSoundCategories.Interface,null),
            ("door is quiet",new(EditorSoundAction.Paint,EditorSoundPaintMode.Door),EditorSoundCategories.All,null),
            ("trap is quiet",new(EditorSoundAction.Paint,EditorSoundPaintMode.Trap),EditorSoundCategories.All,null),
            ("special is quiet",new(EditorSoundAction.Paint,EditorSoundPaintMode.SpecialItem),EditorSoundCategories.All,null),
            ("gold pile",new(EditorSoundAction.Paint,EditorSoundPaintMode.Object,ObjectID:1),EditorSoundCategories.Terrain,EditorSoundCue.Gold),
            ("gold bag",new(EditorSoundAction.Paint,EditorSoundPaintMode.Object,ObjectID:2),EditorSoundCategories.Terrain,EditorSoundCue.Gold),
            ("gold chest",new(EditorSoundAction.Paint,EditorSoundPaintMode.Object,ObjectID:3),EditorSoundCategories.Terrain,EditorSoundCue.Gold),
            ("other object is quiet",new(EditorSoundAction.Paint,EditorSoundPaintMode.Object,ObjectID:4),EditorSoundCategories.Terrain,null),
            ("ordinary fill is quiet",new(EditorSoundAction.Paint,EditorSoundPaintMode.Fill,Terrain:8),EditorSoundCategories.All,null),
            ("solid rock",new(EditorSoundAction.Paint,Terrain:1),EditorSoundCategories.All,EditorSoundCue.Destroy),
            ("earth",new(EditorSoundAction.Paint,Terrain:2),EditorSoundCategories.All,EditorSoundCue.Destroy),
            ("unclaimed",new(EditorSoundAction.Paint,Terrain:3),EditorSoundCategories.All,EditorSoundCue.Destroy),
            ("boundary",new(EditorSoundAction.Paint,Terrain:0x1E),EditorSoundCategories.All,EditorSoundCue.Destroy),
            ("water",new(EditorSoundAction.Paint,Terrain:4),EditorSoundCategories.All,EditorSoundCue.Water),
            ("lava",new(EditorSoundAction.Paint,Terrain:5),EditorSoundCategories.All,EditorSoundCue.Lava),
            ("gold terrain uses gems",new(EditorSoundAction.Paint,Terrain:6),EditorSoundCategories.All,EditorSoundCue.Gems),
            ("gems",new(EditorSoundAction.Paint,Terrain:7),EditorSoundCategories.All,EditorSoundCue.Gems),
            ("claimed",new(EditorSoundAction.Paint,Terrain:8),EditorSoundCategories.All,EditorSoundCue.Claimed),
            ("wall",new(EditorSoundAction.Paint,Terrain:9),EditorSoundCategories.All,EditorSoundCue.Wall),
            ("keeper heart",new(EditorSoundAction.Paint,Terrain:14,Owner:2),EditorSoundCategories.All,EditorSoundCue.Heart),
            ("neutral heart",new(EditorSoundAction.Paint,Terrain:14,Owner:1),EditorSoundCategories.All,EditorSoundCue.Room),
            ("pen room ignores size",new(EditorSoundAction.Paint,Terrain:16,Width:4,Height:6),EditorSoundCategories.All,EditorSoundCue.Room),
            ("single room shape",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:16),EditorSoundCategories.All,EditorSoundCue.Room),
            ("tall room shape",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:16,Height:2),EditorSoundCategories.All,EditorSoundCue.Tiles),
            ("wide room shape",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:16,Width:2),EditorSoundCategories.All,EditorSoundCue.Tiles),
            ("large keeper heart",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:14,Owner:7,Width:5,Height:5),EditorSoundCategories.All,EditorSoundCue.Heart),
            ("large neutral heart",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:14,Owner:1,Width:5,Height:5),EditorSoundCategories.All,EditorSoundCue.Room),
            ("portal",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:0x0C,Width:4,Height:4),EditorSoundCategories.All,EditorSoundCue.Room),
            ("hero portal1",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:0x1C,Width:4,Height:4),EditorSoundCategories.All,EditorSoundCue.Room),
            ("hero portal2",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:0x1D,Width:4,Height:4),EditorSoundCategories.All,EditorSoundCue.Room),
            ("hero portal3",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:0x21,Width:4,Height:4),EditorSoundCategories.All,EditorSoundCue.Room),
            ("hero portal4",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:0x22,Width:4,Height:4),EditorSoundCategories.All,EditorSoundCue.Room),
            ("hero portal6/7",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:0x25,Width:4,Height:4),EditorSoundCategories.All,EditorSoundCue.Room),
            ("mercenary portal",new(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,Terrain:0x28,Width:4,Height:4),EditorSoundCategories.All,EditorSoundCue.Room)
        };
        foreach(var c in cases)if(EditorSoundRules.Choose(c.Request,c.Categories)!=c.Expected)throw new InvalidOperationException(c.Name);
        var files=new Dictionary<EditorSoundCue,string>
        {
            [EditorSoundCue.Claimed]="Build - Claimed.wav",[EditorSoundCue.Creature]="Build - Creature.wav",
            [EditorSoundCue.Destroy]="Build - Destroy.wav",[EditorSoundCue.Gems]="Build - Gems.wav",
            [EditorSoundCue.Gold]="Build - Gold.wav",[EditorSoundCue.Heart]="Build - Heart.wav",
            [EditorSoundCue.Imp]="Build - Imp.wav",[EditorSoundCue.Lava]="Build - Lava.wav",
            [EditorSoundCue.Room]="Build - Room.wav",[EditorSoundCue.Selection]="Build - Selection.wav",
            [EditorSoundCue.Tiles]="Build - Tiles.wav",[EditorSoundCue.Wall]="Build - Wall.wav",
            [EditorSoundCue.Water]="Build - Water.wav",[EditorSoundCue.ButtonPaint]="Button - Paint.wav",
            [EditorSoundCue.ButtonTerrain]="Button - Terrain.wav"
        };
        foreach(var pair in files)if(EditorSoundRules.FileName(pair.Key)!=pair.Value)throw new InvalidOperationException("Wrong source sound filename: "+pair.Key);
        if(EditorSoundRules.LinearVolume(-500) is var volume && Math.Abs(volume-0.5623413251903491)>1e-12
            ||EditorSoundRules.LinearVolume(0)!=1||EditorSoundRules.LinearVolume(-10000)!=0)
            throw new InvalidOperationException("Wrong DirectSound centibel conversion.");
        Console.WriteLine($"EDITOR SOUNDS SOURCE DETAIL {cases.Length} routing outcomes, {files.Count} original filenames and 3 volume values; no WPF or audio device execution");
    }
}
