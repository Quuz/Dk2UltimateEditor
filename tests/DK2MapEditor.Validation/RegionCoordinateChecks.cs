using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class RegionCoordinateChecks
{
    public static void Run()
    {
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        foreach(var (text,read,normalized,value) in new (string,int,string,int)[]{
            ("0",0,"0",0),("11",11,"11",11),("12",12,"11",11),("-1",0,"-1",0),
            ("2147483647",int.MaxValue,"11",11),("2147483648",int.MinValue,"0",0),
            ("4294967295",-1,"0",0),("4294967296",0,"4294967296",0),
            ("999999999999999999999999999999999999",0,"999999999999999999999999999999999999",0),
            ("",0,"",0),("  ",0,"  ",0),("oops",0,"oops",0),("  7tail",7,"  7tail",7),
            ("003",3,"003",3),("1.5",1,"1.5",1),("12x",12,"11",11),("9 7",9,"9 7",9)})
        {
            Check(RegionCoordinateRules.Read(text)==read,"Unsigned source prefix conversion changed: "+text);
            Check(RegionCoordinateRules.Normalize(text,12)==normalized&&RegionCoordinateRules.Value(text,12)==value,"EN_CHANGE clamp or accepted value changed: "+text);
        }
        Check(RegionCoordinateRules.Normalize("9",10)=="9"&&RegionCoordinateRules.Normalize("10",10)=="9"
            &&RegionCoordinateRules.Normalize("9",24)=="9","X/Y extents were interchanged.");
        foreach(var text in new[]{"0","1","999","oops","2147483648"})
            Check(RegionCoordinateRules.Value(text,1)==0,"Single-cell extent was not clamped to zero.");
        foreach(var (text,delta,expected) in new[]{("0",-1,"0"),("11",1,"11"),("5",1,"6"),("5",-1,"4"),
            ("",1,"1"),("oops",-1,"0"),("  7tail",1,"8"),("2147483647",1,"0")})
            Check(RegionCoordinateRules.Step(text,12,delta)==expected,"Source one-unit up/down semantics changed.");
        try{RegionCoordinateRules.Value("0",0);throw new Exception("Invalid extent accepted.");}catch(ArgumentOutOfRangeException){}
        try{RegionCoordinateRules.Step("0",12,2);throw new Exception("Unverified step accepted.");}catch(ArgumentOutOfRangeException){}
        foreach(var native in new[]{false,true})foreach(var kind in Enum.GetValues<MapRegionKind>())
        {
            var doc=NativeMapDocument.CreateNewMap(new("CoordinateDraft","Coordinate Draft",24,16,NativeFormat:native));
            var record=doc.AddMapRegion(kind,new(3,4,1,1));var bytes=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
            var count=doc.EditorChangeCount;var revision=doc.EditRevision;var history=doc.GetHistory().Undo.Count;
            void Exact()=>Check(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.EditRevision==revision&&doc.GetHistory().Undo.Count==history,"Coordinate entry/Cancel changed owner bytes or history.");
            using(var canceled=doc.CreateRegionPropertiesSession(record))
            {
                var point=new TileRect(RegionCoordinateRules.Value("999",24),RegionCoordinateRules.Value("999",16),1,1);
                doc.UpdateRegionProperties(canceled,canceled.Values with{Bounds=point});Exact();
            }
            Exact();
            using(var session=doc.CreateRegionPropertiesSession(record))
            {
                // Negative/overflowing input reads as zero; independent X/Y
                // limits produce the opposite corner of this rectangular map.
                var bounds=kind==MapRegionKind.Gate?new TileRect(23,15,1,1):new(0,0,24,16);
                doc.UpdateRegionProperties(session,session.Values with{Bounds=bounds});Exact();
                doc.ApplyEditorRegionProperties(doc.PrepareRegionProperties(session));
                Check(doc.MapRegions().Single(r=>r.Kind==kind).Bounds==bounds&&doc.EditorChangeCount==unchecked(count+1)
                    &&doc.GetHistory().Undo.Count==history+1,"Normalized coordinates split the existing atomic apply.");
            }
            doc.Undo();revision=doc.EditRevision;Exact();doc.Redo();doc.Undo();revision=doc.EditRevision;Exact();
            using(var creation=doc.CreateRegionCreationSession(kind,new(0,0,1,1)))
            {
                creation.WorkingDocument.UpdateRegionProperties(creation.Properties,creation.Properties.Values with{Bounds=new(23,15,1,1)});Exact();
            }
            Exact();
        }
        Console.WriteLine("REGION COORDINATE DETAIL unsigned-prefix read, bounded immediate clamp, partial tokens, zero/UINT overflow, separate rectangular X/Y extents, one-unit steps, native/DKM three-region private draft Cancel and atomic Apply/Undo/Redo; canonical App policy only, no WPF execution");
    }
}
