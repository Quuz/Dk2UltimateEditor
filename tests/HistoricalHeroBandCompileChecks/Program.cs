using DK2MapEditor.Core.Editing;

internal static class Program
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static int Main(string[] args)
    {
        if(args.Length!=2 || Directory.Exists(args[0]) || !Directory.Exists(args[1]))throw new ArgumentException("Require fresh private native artifacts and accepted historical fixtures.");
        Directory.CreateDirectory(args[0]);
        try
        {
            for(var version=1;version<=5;version++)
            {
                var source=Path.Combine(args[1],$"v{version}-preview/fixture/historical.dkm");var copy=Path.Combine(args[0],$"input-v{version}.dkm");File.Copy(source,copy);
                var document=NativeMapDocument.Load(copy);var band=document.Records().Where(NativeMapDocument.IsBand).ElementAt(1);var before=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
                using var draft=document.CreateEditorHeroBandSession(band);var values=draft.Members[1].Values with{Level=9};draft.Replace([1],values);var expected=draft.Members.Select(m=>m.Values).ToArray();var id=draft.ID;
                document.ApplyEditorHeroBand(document.PrepareEditorHeroBand(draft,"Historical compile"));
                var compiled=document.CompileLegacyProject("HistoricalCompiledV"+version,NativeMapDocument.Create(3,3,"Basis"));
                var actual=compiled.Records().Single(r=>NativeMapDocument.IsBand(r) && r.Read(r.Fields.Single(f=>f.Name=="bBandID"))==id.ToString());
                void Verify(NativeMapDocument map,NativeRecord current)
                {
                    Check(map.BandMembers(current).Count(m=>m.Active)==2,"Native compilation truncated historical members.");
                    for(var index=0;index<2;index++)
                    {
                        var record=map.Records().Single(r=>r.File==current.File && r.Offset==current.Offset+51+index*32);var v=expected[index];
                        foreach(var (field,value) in new (string,uint)[]{("bGlobalCreatureID",v.CreatureID),("bGlobalLevel",(uint)v.Level),("bGlobalPlayer",v.Enemy),("dwGlobalHealth",v.Health),("bGlobalGoldHeld",v.GoldHeld),("bGlobalBehavior",v.Options2),("bHeroOptions",v.Options),("bHeroObjective",v.Objective),("bHeroTargetArea",v.Area),("bHeroBandID",id),("wHeroTrigger",0)})
                            Check(record.Read(record.Fields.Single(f=>f.Name==field))==value.ToString(),"Original Compiler_Generate.c975..990 field differs: "+field+" version"+version);
                    }
                }
                Verify(compiled,actual);var saved=compiled.SaveCopy(Path.Combine(args[0],$"native-v{version}"));var reopened=NativeMapDocument.Load(saved);
                var restored=reopened.Records().Single(r=>NativeMapDocument.IsBand(r) && r.Read(r.Fields.Single(f=>f.Name=="bBandID"))==id.ToString());Verify(reopened,restored);
                document.Undo();Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Native export affected historical parent Undo/original buffers.");
                Console.WriteLine($"PASS project-v{version} source native band11 fields/member, private compile/save/reopen and original historical exact Undo");
            }
            Console.WriteLine("HISTORICAL HERO BAND NATIVE COMPILE CHECKS PASSED: 5");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
    }
}
