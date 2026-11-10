using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class ManagerDestinationChecks
{
    public static void Run()
    {
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        var session=new ObjectManagerSession();var independent=new ObjectManagerSession();
        session.Family=1;session.SelectionOnly=true;session.Addition.Mode=2;session.Addition.LastKind=PlacedThingKind.Trap;
        session.Modification.CreatureBits=7;session.Modification.HeroBits=9;
        var flags=session.Addition.Flags;var modification=session.Modification.Mask;var requests=session.RequestPreferences.Flags;
        session.Addition.Templates[PlacedThingKind.Creature]=["opaque-template"];
        foreach(var (horizontal,text,extent,expected) in new[]{(true,"999",20,"19"),(false,"999",14,"13"),
            (true,"",20,"0"),(false,"  7tail",14,"7"),(true,"4294967295",20,"0"),(false,"4294967296",14,"0"),
            (true,"003",20,"3"),(false,"-1",14,"0"),(true,"2147483647",20,"19")})
        {
            var other=horizontal?session.PositionY:session.PositionX;session.SetDestination(horizontal,text,extent);
            Check((horizontal?session.PositionX:session.PositionY)==expected&&(horizontal?session.PositionY:session.PositionX)==other,"Destination write used the wrong axis or retained nonnumeric session data.");
            Check(session.Addition.Flags==flags&&session.Modification.Mask==modification&&session.RequestPreferences.Flags==requests
                &&session.Addition.Templates[PlacedThingKind.Creature].Single()=="opaque-template"&&session.Family==1&&session.SelectionOnly
                &&session.Addition.Mode==2&&session.Addition.LastKind==PlacedThingKind.Trap&&session.Modification.CreatureBits==7&&session.Modification.HeroBits==9,
                "Coordinate callbacks changed unrelated source preferences/template values.");
        }
        session.PositionX="  8tail";session.PositionY="999";session.BoundPosition(6,4);
        Check(session.PositionX=="5"&&session.PositionY=="3"&&session.ReadDestination(6,4)==new TilePoint(5,3),"Smaller-map reopen did not bound each shared coordinate.");
        Check(independent.PositionX=="1"&&independent.PositionY=="1"&&independent.Addition.Templates.Count==0,"Another workspace inherited destination/template state.");
        foreach(var native in new[]{false,true})
        {
            var d=NativeMapDocument.CreateNewMap(new("ManagerCoordinates","Manager Coordinates",20,14,NativeFormat:native));
            var before=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var history=d.GetHistory();var revision=d.EditRevision;var events=0;
            d.Changed+=(_,_)=>events++;
            void Exact()=>Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&d.EditorChangeCount==history.EditorChangeCount
                &&d.GetHistory().Undo.Count==history.Undo.Count,"Destination/preflight/Cancel changed map bytes or history.");
            var draft=new ObjectManagerSession();draft.SetDestination(true,"999",d.Width);draft.SetDestination(false,"999",d.Height);
            Check(draft.ReadDestination(d.Width,d.Height)==new TilePoint(19,13),"Rectangular manager destination clamped Y by width.");
            var template=ObjectBatchTemplate.Default(PlacedThingKind.Creature) with{Owner=3};
            var plan=d.PlanObjectBatchInsertion(template,new(Position:draft.ReadDestination(d.Width,d.Height),Flags:(ObjectBatchFlags)127));
            Check(plan.Placement.Candidates.Single().Position==new TilePoint(19,13),"Add planning did not consume the accepted shared destination.");
            Exact();Check(d.EditRevision==revision&&events==0,"Coordinate/preview emitted a map change.");
            draft.SetDestination(true,"",d.Width);draft.SetDestination(false,"  9suffix",d.Height);
            Check(draft.ReadDestination(d.Width,d.Height)==new TilePoint(0,9),"Empty/partial source input was not a numeric destination.");Exact();
            draft.SetDestination(true,"8",d.Width);draft.SetDestination(false,"9",d.Height);
            plan=d.PlanObjectBatchInsertion(template,new(Position:draft.ReadDestination(d.Width,d.Height),Flags:(ObjectBatchFlags)127));
            d.ApplyObjectBatchInsertion(plan,[0]);
            Check(d.ObjectManagerEntries().Single().Thing.Position==new TilePoint(8,9)&&d.EditorChangeCount==unchecked(history.EditorChangeCount+1)
                &&d.GetHistory().Undo.Count==history.Undo.Count+1,"Manager destination split atomic Add or placed at another point.");
            d.Undo();Exact();d.Redo();d.Undo();Exact();
        }
        Console.WriteLine("MANAGER DESTINATION DETAIL canonical App session linked without WPF, independent X/Y extents, empty/prefix/UINT and smaller-map inputs, unchanged template/options/request preferences, workspace isolation; native/DKM Add preflight/private bytes and one atomic Add/Undo/Redo; no UI process");
    }
}
