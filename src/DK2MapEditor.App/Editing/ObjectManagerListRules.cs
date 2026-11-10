using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Source Things_ManagerInit and the three Set*List functions. These
/// predicates affect only list membership; raw map records are never repaired.</summary>
public static class ObjectManagerListRules
{
    public static bool SelectionAvailable(TileRect? selection,int width,int height)=>width>0&&height>0
        &&selection is {Width:>0,Height:>0} rect&&rect.X<width&&rect.Y<height
        &&(long)rect.X+rect.Width>0&&(long)rect.Y+rect.Height>0;
    public static bool Includes(TilePoint point,int width,int height,bool selectionOnly,TileRect? selection)
    {
        if(point.X<0||point.Y<0||point.X>=width||point.Y>=height)return false;
        if(!selectionOnly)return true;
        return selection is {Width:>0,Height:>0} rect&&point.X>=rect.X&&point.Y>=rect.Y
            &&(long)point.X<(long)rect.X+rect.Width&&(long)point.Y<(long)rect.Y+rect.Height;
    }
    public static bool MatchesFamily(PlacedThingKind kind,int family)
    {
        if(family is <0 or >3)throw new ArgumentOutOfRangeException(nameof(family));
        var actual=kind switch{PlacedThingKind.Creature=>0,PlacedThingKind.Door or PlacedThingKind.Trap=>1,
            PlacedThingKind.Object or PlacedThingKind.SpecialItem=>2,_=>-1};
        return actual>=0&&(family==3||family==actual);
    }
}
