using System.Globalization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Original Config/Manager* process-session preferences, scoped to an editor workspace.
/// Value drafts and runtime request preferences; no document/record/plan is retained when idle.</summary>
public sealed class ObjectManagerSession
{
    public ObjectBatchSession Addition { get; } = new();
    public EditorRequestPreferences RequestPreferences { get; }
    public ObjectModificationSession Modification { get; }
    internal int Family, SubType;
    internal ObjectManagerSort Sort = ObjectManagerSort.Position;
    internal string Search = "", PositionX = "1", PositionY = "1";
    internal bool SelectionOnly;

    public ObjectManagerSession():this(null) { }
    public ObjectManagerSession(EditorRequestPreferences? requestPreferences)
    {RequestPreferences=requestPreferences??new();Modification=new(Addition.Templates);}

    internal PlacedThingKind KindFor(PlacedThingKind kind) => kind switch
    {
        PlacedThingKind.Door or PlacedThingKind.Trap => SubType == 0 ? PlacedThingKind.Door : PlacedThingKind.Trap,
        PlacedThingKind.Object or PlacedThingKind.SpecialItem => SubType == 0 ? PlacedThingKind.Object : PlacedThingKind.SpecialItem,
        _ => kind
    };
    internal void ChooseKind(PlacedThingKind kind)
    {
        if (kind is PlacedThingKind.Door or PlacedThingKind.Object) SubType = 0;
        else if (kind is PlacedThingKind.Trap or PlacedThingKind.SpecialItem) SubType = 1;
    }
    internal void BoundPosition(int width, int height)
    {
        SetDestination(true,PositionX,width);SetDestination(false,PositionY,height);
    }
    // ManagerX/Y are numbers. An intermediate empty field remains visible in
    // the window, while source GetDlgItemInt(FALSE) supplies zero to callers.
    internal void SetDestination(bool horizontal,string text,int extent)
    {
        var number=RegionCoordinateRules.Value(text,extent).ToString(CultureInfo.InvariantCulture);
        if(horizontal)PositionX=number;else PositionY=number;
    }
    internal TilePoint ReadDestination(int width,int height)=>new(RegionCoordinateRules.Value(PositionX,width),RegionCoordinateRules.Value(PositionY,height));
}

public sealed class ObjectModificationSession
{
    internal ObjectBatchSession Template { get; }
    internal ObjectModificationMask Mask = ObjectModificationMask.All & ~ObjectModificationMask.Coordinates;
    internal int Coordinates = 1, CreatureBits, HeroBits;
    internal string CreatureMask = "4294967295", HeroMask = "4294967295";
    public ObjectModificationSession() : this(new Dictionary<PlacedThingKind, string[]>()) { }
    internal ObjectModificationSession(Dictionary<PlacedThingKind, string[]> templates)
        => Template = new(templates) { Flags = ObjectBatchFlags.AskOptions | ObjectBatchFlags.Preview | ObjectBatchFlags.IgnoreAsk | ObjectBatchFlags.IgnoreIncompatible };
}
