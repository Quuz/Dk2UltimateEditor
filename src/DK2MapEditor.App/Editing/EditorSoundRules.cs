namespace DK2MapEditor.App.Editing;

[Flags]
public enum EditorSoundCategories { None=0,Interface=1,Terrain=2,All=3 }
public enum EditorSoundAction { ToolChoice,TerrainChoice,DefineRectangle,Paint }
public enum EditorSoundPaintMode { Pen,Shape,Fill,Creature,Door,Trap,Object,SpecialItem }
public enum EditorSoundCue { Claimed,Creature,Destroy,Gems,Gold,Heart,Imp,Lava,Room,Selection,Tiles,Wall,Water,ButtonPaint,ButtonTerrain }
public readonly record struct EditorSoundRequest(EditorSoundAction Action,EditorSoundPaintMode Mode=EditorSoundPaintMode.Pen,
    byte Terrain=0,byte Owner=0,uint ObjectID=0,int Width=1,int Height=1);

/// <summary>Pure routing from original Map_Sounds.c Sound_Play. No audio device or WPF dependency.</summary>
public static class EditorSoundRules
{
    public const int DefaultVolume=-500;
    public static double LinearVolume(int centibels)=>centibels<=-10000?0:Math.Pow(10,Math.Clamp(centibels,-10000,0)/2000d);
    public static string FileName(EditorSoundCue cue)=>cue switch
    {
        EditorSoundCue.ButtonPaint=>"Button - Paint.wav",EditorSoundCue.ButtonTerrain=>"Button - Terrain.wav",
        _ when Enum.IsDefined(cue)=>"Build - "+cue+".wav",
        _=>throw new ArgumentOutOfRangeException(nameof(cue))
    };
    public static EditorSoundCue? Choose(EditorSoundRequest request,EditorSoundCategories categories=EditorSoundCategories.All)
    {
        if(request.Action is EditorSoundAction.ToolChoice or EditorSoundAction.TerrainChoice)
            return (categories&EditorSoundCategories.Interface)==0?null:
                request.Action==EditorSoundAction.ToolChoice?EditorSoundCue.ButtonPaint:EditorSoundCue.ButtonTerrain;
        if((categories&EditorSoundCategories.Terrain)==0)return null;
        if(request.Action==EditorSoundAction.DefineRectangle)return EditorSoundCue.Selection;
        if(request.Action!=EditorSoundAction.Paint)return null;
        if(request.Mode==EditorSoundPaintMode.Creature)return request.ObjectID==1?EditorSoundCue.Imp:EditorSoundCue.Creature;
        if(request.Mode==EditorSoundPaintMode.Object)return request.ObjectID is 1 or 2 or 3?EditorSoundCue.Gold:null;
        if(request.Mode is not (EditorSoundPaintMode.Pen or EditorSoundPaintMode.Shape))return null;
        return request.Terrain switch
        {
            0x08=>EditorSoundCue.Claimed,0x09=>EditorSoundCue.Wall,0x04=>EditorSoundCue.Water,0x05=>EditorSoundCue.Lava,
            0x06 or 0x07=>EditorSoundCue.Gems,0x01 or 0x02 or 0x03 or 0x1E=>EditorSoundCue.Destroy,
            0x0E when request.Owner>1=>EditorSoundCue.Heart,
            0x0C or 0x0E or 0x1C or 0x1D or 0x21 or 0x22 or 0x25 or 0x28=>EditorSoundCue.Room,
            _=>request.Mode==EditorSoundPaintMode.Pen||request.Width<2&&request.Height<2?EditorSoundCue.Room:EditorSoundCue.Tiles
        };
    }
}
