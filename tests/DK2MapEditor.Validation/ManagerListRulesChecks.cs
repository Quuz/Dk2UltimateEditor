using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class ManagerListRulesChecks
{
    public static void Run()
    {
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        const int width=12,height=10;
        var inside=new[]{new TilePoint(0,0),new(11,9),new(4,5)};
        var outside=new[]{new TilePoint(-1,3),new(3,-1),new(width,3),new(3,height),new(int.MaxValue,int.MaxValue)};
        foreach(var p in inside)Check(ObjectManagerListRules.Includes(p,width,height,false,null),"Source whole-map list omitted a boundary/interior record.");
        foreach(var p in outside)Check(!ObjectManagerListRules.Includes(p,width,height,false,null),"Source whole-map list included an out-of-map record.");
        var selected=new TileRect(3,4,2,2);
        Check(ObjectManagerListRules.SelectionAvailable(selected,width,height)&&!ObjectManagerListRules.SelectionAvailable(null,width,height)
            &&!ObjectManagerListRules.SelectionAvailable(new(0,0,0,1),width,height)&&!ObjectManagerListRules.SelectionAvailable(new(12,0,1,1),width,height),"Initial source selection availability differs.");
        Check(ObjectManagerListRules.Includes(new(3,4),width,height,true,selected)&&ObjectManagerListRules.Includes(new(4,5),width,height,true,selected)
            &&!ObjectManagerListRules.Includes(new(5,5),width,height,true,selected)&&!ObjectManagerListRules.Includes(new(4,6),width,height,true,selected),"Selection rectangle is not inclusive at its first/exclusive at its last boundary.");
        Check(!ObjectManagerListRules.Includes(new(3,4),width,height,true,null)&&!ObjectManagerListRules.Includes(new(3,4),width,height,true,new(0,0,-1,2)),"Missing/invalid selection silently became whole map.");
        Check(ObjectManagerListRules.Includes(new(0,0),width,height,true,new(-2,-2,3,3))&&!ObjectManagerListRules.Includes(new(-1,0),width,height,true,new(-2,-2,3,3)),"Partially outside selection bypassed map bounds.");
        Check(ObjectManagerListRules.Includes(new(int.MaxValue-1,1),int.MaxValue,3,true,new(int.MaxValue-2,0,int.MaxValue,3)),"Selection end overflow discarded a valid point.");
        foreach(var kind in Enum.GetValues<PlacedThingKind>())
        {
            var expected=kind==PlacedThingKind.Creature?0:kind is PlacedThingKind.Door or PlacedThingKind.Trap?1:2;
            Check(Enumerable.Range(0,3).Count(f=>ObjectManagerListRules.MatchesFamily(kind,f))==1&&ObjectManagerListRules.MatchesFamily(kind,expected)
                &&ObjectManagerListRules.MatchesFamily(kind,3),"A physical kind belongs to zero or multiple source list families.");
        }
        Check(!ObjectManagerListRules.MatchesFamily((PlacedThingKind)99,3),"Unverified kind was guessed into a manager list.");
        try{ObjectManagerListRules.MatchesFamily(PlacedThingKind.Creature,4);throw new Exception("Unsupported family accepted.");}catch(ArgumentOutOfRangeException){}
        Console.WriteLine("MANAGER LIST DETAIL source whole-map and selected-area boundaries, first/last cells, excluded invalid imported coordinates, missing/empty/overflow-safe selection, five physical kinds in exactly one of three families; pure canonical App policy, no WPF/map writes");
    }
}
