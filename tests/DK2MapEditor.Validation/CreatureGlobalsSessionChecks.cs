using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class CreatureGlobalsSessionChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; }
        throw new Exception("An invalid creature shared-context action was accepted.");
    }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument doc, Dictionary<NativeFile, byte[]> bytes)
    { Expect(doc.Files.Count == bytes.Count && doc.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Creature context changed unrelated bytes/cancel/undo."); }
    private static GlobalDefinitionRow Row(NativeMapDocument doc, string family, int index = 0) => doc.GlobalDefinitionRows().Single(row => row.Family == family && row.Index == index);
    private static string Value(NativeMapDocument doc, string family, int index, string field) => doc.GlobalDefinitionValues(Row(doc, family, index)).Single(value => value.Name == field).Value;
    private static JsonDocument Oracle()
    { using var stream = typeof(CreatureGlobalsSessionChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.CreatureGlobalsSessionSource.json")!; return JsonDocument.Parse(stream); }
    private static NativeMapDocument[] Documents(string name) => [NativeMapDocument.CreateNewMap(new(name,name,20,20)), GlobalDefinitionNumericChecks.Fixture(Path.Combine(Path.GetTempPath(),"DK2CreatureSession",Guid.NewGuid().ToString("N"),name))];
    private static void Raw(NativeFile file, int offset, Action<byte[],int> edit)
    { Expect(MemoryMarshal.TryGetArray(file.Data,out var bytes),"No fixture backing."); edit(bytes.Array!,bytes.Offset+offset); }
    private static void Store(Dictionary<NativeFile,byte[]> result, NativeRecord record, JsonElement storage, string raw)
    {
        var offset = record.Offset + storage.GetProperty("Offset").GetInt32(); var number = long.Parse(raw,CultureInfo.InvariantCulture); var bytes = result[record.File];
        if (storage.TryGetProperty("Mask",out var flag)) { var mask=flag.GetByte(); bytes[offset]=(byte)((bytes[offset]&~mask)|(number==0?0:mask)); }
        else if (storage.GetProperty("Size").GetInt32()==1) bytes[offset]=(byte)number;
        else if (storage.GetProperty("Size").GetInt32()==2) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset),unchecked((ushort)number));
        else BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset),unchecked((uint)number));
    }

    public static void Composition()
    {
        using var oracle=Oracle(); var scope=oracle.RootElement; var pages=scope.GetProperty("Pages").EnumerateArray().ToArray();
        Expect(pages.Length==9 && pages.Select(page=>page.GetProperty("Section").GetString()).SequenceEqual(CreatureGlobalsSession.Sections.Select(section=>section.Name)),"Nine-page source order differs.");
        foreach (var (source,section) in pages.Zip(CreatureGlobalsSession.Sections))
            Expect(section.Fields.Order().SequenceEqual(source.GetProperty("Fields").EnumerateArray().Select(field=>field.GetString()).Order()),"Incomplete source page permission: "+section.Name);
        foreach (var doc in Documents("CreatureCompose"))
        {
            doc.SetHistoryDepth(65535); var initial=Snapshot(doc); var history=doc.GetHistory().Undo.Count; var notices=0; doc.Changed+=(_,_)=>notices++;
            using(var untouched=doc.CreateCreatureGlobalsSession())
            { var plan=doc.PrepareCreatureGlobals(untouched); Expect(!plan.Changed && plan.JobWarnings.Count==0,"Opening creature context changed defaults."); doc.ApplyCreatureGlobals(plan); Exact(doc,initial); }
            Expect(notices==0 && doc.GetHistory().Undo.Count==history,"Creature no-op consumed history.");
            var session=doc.CreateCreatureGlobalsSession(); var working=session.WorkingDocument; var changes=new List<GlobalDefinitionChange>();
            foreach(var family in CreatureGlobalsSession.Families)
            {
                var rows=working.GlobalDefinitionRows().Where(row=>row.Family==family).ToArray(); var sourceRows=scope.GetProperty(family=="Creatures"?"Creatures":"CrSpells").EnumerateArray().ToArray();
                Expect(rows.Length==sourceRows.Length && rows.All(row=>row.Editable),"Original rows unavailable.");
                foreach(var (row,source) in rows.Zip(sourceRows))
                {
                    Expect(row.ID==source.GetProperty("ID").GetUInt32(),"Original row IDs reordered.");
                    var field=family=="Creatures"?"Health":"ShotData1"; changes.Add(new(family,row.Index,field,Value(doc,family,row.Index,field)=="222"?"223":"222"));
                }
            }
            // Change one source field on every remaining creature page. Jobs
            // use an explicit nonstandard total to exercise the common warning.
            changes.AddRange(new GlobalDefinitionChange[] { new("Creatures",0,"ArmourType","2"),new("Creatures",0,"CanBePicked",Value(doc,"Creatures",0,"CanBePicked")=="0"?"1":"0"),
                new("Creatures",0,"Shot1Delay","6144"),new("Creatures",0,"Res1Rate","72"),new("Creatures",0,"SpecialAb1Data","123"),new("Creatures",0,"LightR","17") });
            working.SetGlobalDefinitionValues(changes);
            var jobs=working.CreateCreatureJobsDraft(Row(working,"Creatures"));
            var job=CreatureJobs.Allowed("Happy").First();
            working.SetGlobalDefinitionValues(Enumerable.Range(0,3).SelectMany(index=>new[] { new GlobalDefinitionChange("Creatures",0,$"HJobs[{index}]",index==0?job.ID.ToString(CultureInfo.InvariantCulture):"0"),new("Creatures",0,$"HJobsChance[{index}]",index==0?"17":"0") }));
            Exact(doc,initial); var plan2=doc.PrepareCreatureGlobals(session); Expect(plan2.ChangedRows==86,"Shared creature/spell context lost rows.");
            Expect(plan2.JobWarnings.Any(warning=>warning.Index==0 && warning.Group=="Happy" && warning.Total==17),"Changed job totals were not presented by common Apply.");
            Reject(()=>doc.ApplyCreatureGlobals(plan2)); Exact(doc,initial);
            var expected=initial.ToDictionary(item=>item.Key,item=>item.Value.ToArray());
            if(!doc.IsLegacyProject) foreach(var change in plan2.Changes) Store(expected,Row(doc,change.Family,change.Index).Record!,scope.GetProperty("NativeStorage").GetProperty(change.Family).GetProperty(change.Field),change.Value);
            var reentrant=false; void Reentry(object? sender,EventArgs args) { Reject(()=>doc.ApplyCreatureGlobals(plan2,true)); reentrant=true; }
            doc.Changed+=Reentry; doc.ApplyCreatureGlobals(plan2,true); doc.Changed-=Reentry;
            Expect(reentrant && notices==1 && doc.GetHistory().Undo.Count==history+1,"Nine-page commit split Undo/notification or accepted reentry.");
            foreach(var change in plan2.Changes) Expect(Value(doc,change.Family,change.Index,change.Field)==change.Value,"Shared Apply lost private values.");
            if(!doc.IsLegacyProject) Exact(doc,expected);
            var applied=Snapshot(doc); var reopened=NativeMapDocument.Load(doc.SaveCopy(Path.Combine(Path.GetTempPath(),"DK2CreatureSession",Guid.NewGuid().ToString("N"))));
            foreach(var change in plan2.Changes) Expect(Value(reopened,change.Family,change.Index,change.Field)==change.Value,"Shared creature save/reopen lost values.");
            if(doc.IsLegacyProject)
            {
                var basis=GlobalDefinitionNumericChecks.Fixture(Path.Combine(Path.GetTempPath(),"DK2CreatureSessionCompiler",Guid.NewGuid().ToString("N")));
                var compiled=doc.CompileLegacyProject("CreatureSharedCompiled",basis); var packed=Snapshot(compiled);
                foreach(var change in plan2.Changes) Store(packed,Row(compiled,change.Family,change.Index).Record!,scope.GetProperty("NativeStorage").GetProperty(change.Family).GetProperty(change.Field),change.Value);
                Exact(compiled,packed); Exact(doc,applied);
            }
            doc.Undo(); Exact(doc,initial); doc.Redo(); Exact(doc,applied); doc.Undo(); Exact(doc,initial); Reject(()=>doc.ApplyCreatureGlobals(plan2,true));
            using(var canceled=doc.CreateCreatureGlobalsSession()) { canceled.WorkingDocument.SetGlobalDefinitionValues(changes); doc.ResetCreatureGlobals(canceled,"Flags"); Exact(doc,initial); }
            Exact(doc,initial);
            if(doc.IsLegacyProject)
            {
                using var reverted=doc.CreateCreatureGlobalsSession(); var original=Value(doc,"Creatures",0,"Health");
                reverted.WorkingDocument.SetGlobalDefinitionValues([new("Creatures",0,"Health","321")]); reverted.WorkingDocument.SetGlobalDefinitionValues([new("Creatures",0,"Health",original)]);
                var noOp=doc.PrepareCreatureGlobals(reverted); Expect(!noOp.Changed,"Private creature revert materialized live defaults."); var count=doc.GetHistory().Undo.Count; doc.ApplyCreatureGlobals(noOp); Exact(doc,initial); Expect(doc.GetHistory().Undo.Count==count,"Private revert consumed Undo.");
            }
            Console.WriteLine($"CREATURE SHARED COMPOSE native={!doc.IsLegacyProject} pages=9 rows=86 fields=156+11 job-warning private-cancel one-undo literal-bytes compiler reopen stale reentrant no-op");
        }
    }

    public static void ResetsAndGuards()
    {
        using var oracle=Oracle(); var scope=oracle.RootElement;
        foreach(var doc in Documents("CreatureReset"))
        {
            doc.SetHistoryDepth(65535);
            var imports=doc.GlobalDefinitionRows().Where(row=>CreatureGlobalsSession.Families.Contains(row.Family)).SelectMany(row=>doc.GlobalDefinitionValues(row).Select(field=>
                new GlobalDefinitionChange(row.Family,row.Index,field.Name,scope.GetProperty("NativeStorage").GetProperty(row.Family).GetProperty(field.Name).TryGetProperty("Mask",out _)?field.Value=="0"?"1":"0":"127"))).ToArray();
            Expect(imports.Length==7471,"Reset scope omitted source fields or rows."); doc.SetGlobalDefinitionValues(imports); var imported=Snapshot(doc); var history=doc.GetHistory().Undo.Count;
            var importedValues=doc.GlobalDefinitionRows().Where(row=>CreatureGlobalsSession.Families.Contains(row.Family))
                .SelectMany(row=>doc.GlobalDefinitionValues(row).Select(field=>(Key:(row.Family,row.Index,field.Name),field.Value))).ToDictionary(item=>item.Key,item=>item.Value);
            foreach(var section in CreatureGlobalsSession.Sections)
            {
                using var session=doc.CreateCreatureGlobalsSession(); doc.ResetCreatureGlobals(session,section.Name,0);
                foreach(var row in session.WorkingDocument.GlobalDefinitionRows().Where(row=>CreatureGlobalsSession.Families.Contains(row.Family)))
                    foreach(var field in session.WorkingDocument.GlobalDefinitionValues(row))
                        Expect(field.Value==(row.Family==section.Family && row.Index==0 && field.Section==section.Name?field.DefaultValue:importedValues[(row.Family,row.Index,field.Name)]),"Current reset escaped its section or row.");
                doc.PrepareCreatureGlobals(session); Exact(doc,imported);
            }
            using(var all=doc.CreateCreatureGlobalsSession())
            {
                foreach(var section in CreatureGlobalsSession.Sections) doc.ResetCreatureGlobals(all,section.Name);
                foreach(var row in all.WorkingDocument.GlobalDefinitionRows().Where(row=>CreatureGlobalsSession.Families.Contains(row.Family))) Expect(all.WorkingDocument.GlobalDefinitionValues(row).All(field=>field.Value==field.DefaultValue),"All-section reset lost defaults.");
                var plan=doc.PrepareCreatureGlobals(all); Exact(doc,imported); doc.ApplyCreatureGlobals(plan,true); Expect(doc.GetHistory().Undo.Count==history+1,"Nine reset pages split Undo."); doc.Undo(); Exact(doc,imported);
            }
            foreach(var family in GlobalVariablesSession.Families)
            {
                using var outside=doc.CreateCreatureGlobalsSession(); var row=Row(outside.WorkingDocument,family); var field=outside.WorkingDocument.GlobalDefinitionValues(row).First();
                outside.WorkingDocument.SetGlobalDefinitionValues([new(family,0,field.Name,field.Value=="1"?"2":"1")]); Reject(()=>doc.PrepareCreatureGlobals(outside)); Exact(doc,imported);
            }
            using(var context=doc.CreateCreatureGlobalsSession())
            {
                Reject(()=>doc.ResetCreatureGlobals(context,"Terrains")); Reject(()=>doc.ResetCreatureGlobals(context,"Attributes",99));
                Reject(()=>NativeMapDocument.CreateNewMap(new("Foreign","Foreign",12,12)).PrepareCreatureGlobals(context));
                doc.Edit("Live transaction",()=>Reject(()=>doc.PrepareCreatureGlobals(context)));
                context.WorkingDocument.Edit("Private transaction",()=>Reject(()=>doc.PrepareCreatureGlobals(context)));
                var plan=doc.PrepareCreatureGlobals(context); context.WorkingDocument.SetGlobalDefinitionValues([new("CrSpells",0,"Range","128")]); Reject(()=>doc.ApplyCreatureGlobals(plan,true)); Exact(doc,imported);
                var next=doc.PrepareCreatureGlobals(context); doc.SetGlobalDefinitionValues([new("Creatures",0,"Health","126")]); var changed=Snapshot(doc); Reject(()=>doc.ApplyCreatureGlobals(next,true)); Exact(doc,changed); doc.Undo(); Exact(doc,imported); Reject(()=>doc.PrepareCreatureGlobals(context));
            }
            using(var context=doc.CreateCreatureGlobalsSession())
            {
                var job=CreatureJobs.Allowed("Happy").First();
                context.WorkingDocument.SetGlobalDefinitionValues(Enumerable.Range(0,3).SelectMany(index=>new[] { new GlobalDefinitionChange("Creatures",0,$"HJobs[{index}]",index==0?job.ID.ToString(CultureInfo.InvariantCulture):"0"),new("Creatures",0,$"HJobsChance[{index}]",index==0?"17":"0") }));
                var warningPlan=doc.PrepareCreatureGlobals(context); var frozenWarnings=warningPlan.JobWarnings.ToArray();
                Expect(frozenWarnings.Any(warning=>warning.Group=="Happy" && warning.Total==17),"Warning plan lost original probability total.");
                context.WorkingDocument.SetGlobalDefinitionValues([new("Creatures",0,"HJobsChance[0]","100")]);
                Expect(warningPlan.JobWarnings.SequenceEqual(frozenWarnings),"Prepared job warnings changed with the mutable private context.");
                Reject(()=>doc.ApplyCreatureGlobals(warningPlan,true)); Exact(doc,imported);
            }
            using(var context=doc.CreateCreatureGlobalsSession())
            { context.Dispose(); Reject(()=>doc.ResetCreatureGlobals(context,"Jobs")); Reject(()=>doc.PrepareCreatureGlobals(context)); }
            Console.WriteLine($"CREATURE SHARED RESET native={!doc.IsLegacyProject} pages=9 values=7471 current-and-all private section-scope lifetime foreign transaction");
        }
    }

    public static void OpaqueAndVariants()
    {
        using var oracle=Oracle(); var scope=oracle.RootElement;
        foreach(var doc in Documents("CreatureOpaque"))
        {
            doc.SetHistoryDepth(65535);
            if(doc.IsLegacyProject)
            {
                doc.SetGlobalDefinitionValues([new("Creatures",0,"Health","111")]); var record=Row(doc,"Creatures").Record!;
                foreach(var hidden in scope.GetProperty("HiddenPreservedCreatureFields").EnumerateArray()) doc.SetField(record,record.Fields.Single(field=>field.Name==hidden.GetString()),"123456789");
                var unused=doc.Records().Where(row=>row.Category=="Project.GLOBALCREATURE").ElementAt(45);
                Raw(unused.File,unused.Offset,(bytes,offset)=>bytes[offset]=0x7a);
            }
            var before=Snapshot(doc);
            using(var context=doc.CreateCreatureGlobalsSession())
            { context.WorkingDocument.SetGlobalDefinitionValues([new("Creatures",0,"Health","222")]); doc.ResetCreatureGlobals(context,"Flags",0); var plan=doc.PrepareCreatureGlobals(context); doc.ApplyCreatureGlobals(plan); doc.Undo(); Exact(doc,before); }
            using(var context=doc.CreateCreatureGlobalsSession())
            {
                var record=Row(context.WorkingDocument,"Creatures").Record!;
                var offset=doc.IsLegacyProject?record.Fields.Single(field=>field.Name=="Emotionless").Offset:10;
                Raw(record.File,record.Offset+offset,(bytes,index)=>bytes[index]^=0x10); Reject(()=>doc.PrepareCreatureGlobals(context)); Exact(doc,before);
            }
            using(var context=doc.CreateCreatureGlobalsSession())
            { var plan=doc.PrepareCreatureGlobals(context); Raw(context.WorkingDocument.MainFile,0,(bytes,index)=>bytes[index]^=1); Reject(()=>doc.ApplyCreatureGlobals(plan,true)); Exact(doc,before); }
            using(var context=doc.CreateCreatureGlobalsSession())
            { var old=doc.MainFile.Data.Span[0]; Raw(doc.MainFile,0,(bytes,index)=>bytes[index]^=1); Reject(()=>doc.PrepareCreatureGlobals(context)); Raw(doc.MainFile,0,(bytes,index)=>bytes[index]=old); Exact(doc,before); }
            Console.WriteLine($"CREATURE SHARED OPAQUE native={!doc.IsLegacyProject} inactive-flags unused-slot full-fingerprint raw-private-owner guards");
        }
        (string Path,int Offset,int Count,int Size)[] variants=[
            (@"D:\Games\DK2\Tools\ORIGIN17Creatures.kwd",0,46,5449),
            (@"D:\Games\DK2\DKII 13\Dungeon Keeper 2\Data\editor\Creatures.kwd",0,44,5449),
            (@"D:\Games\DK2\Dungeon Keeper 2\Data\editor\maps\ANL - 3 FunGlobals.kld",50174,46,5537),
            (@"D:\Games\DK2\DKII 13\Dungeon Keeper 2\Data\editor\AutoSaveGlobals.kld",45353,44,5537)];
        foreach(var fixture in variants)
        {
            var input=File.ReadAllBytes(fixture.Path); var sha=SHA256.HashData(input); var bytes=input.AsSpan(fixture.Offset,56+fixture.Count*fixture.Size).ToArray();
            var directory=Path.Combine(Path.GetTempPath(),"DK2CreatureSession",Guid.NewGuid().ToString("N")); var map=NativeMapDocument.Create(20,20,"Variant").SaveCopy(directory);
            File.WriteAllBytes(Path.Combine(directory,"Data","Editor","Creatures.kwd"),bytes); var doc=NativeMapDocument.Load(map); doc.SetHistoryDepth(65535); var before=Snapshot(doc);
            using var context=doc.CreateCreatureGlobalsSession(); var rows=context.WorkingDocument.GlobalDefinitionRows().Where(row=>row.Family=="Creatures").ToArray();
            Expect(rows.Count(row=>row.Editable)==(fixture.Count==44?44:45),"Historical shared context invented missing species.");
            var unavailable=rows.FirstOrDefault(row=>!row.Editable); if(unavailable is not null) Reject(()=>doc.ResetCreatureGlobals(context,"Attributes",unavailable.Index));
            context.WorkingDocument.SetGlobalDefinitionValues(rows.Where(row=>row.Editable).Select(row=>new GlobalDefinitionChange("Creatures",row.Index,"Health","333")));
            var plan=doc.PrepareCreatureGlobals(context); var expected=before.ToDictionary(item=>item.Key,item=>item.Value.ToArray());
            foreach(var change in plan.Changes) Store(expected,Row(doc,change.Family,change.Index).Record!,scope.GetProperty("NativeStorage").GetProperty(change.Family).GetProperty(change.Field),change.Value);
            doc.ApplyCreatureGlobals(plan); Exact(doc,expected); doc.Undo(); Exact(doc,before);
            using(var reset=doc.CreateCreatureGlobalsSession()) { doc.ResetCreatureGlobals(reset,"Attributes"); doc.PrepareCreatureGlobals(reset); Exact(doc,before); }
            Expect(SHA256.HashData(File.ReadAllBytes(fixture.Path)).AsSpan().SequenceEqual(sha),"Original historical fixture changed.");
            Console.WriteLine($"CREATURE SHARED VARIANT count={fixture.Count} size={fixture.Size} missing-species opaque-tail one-undo reset source-unchanged");
        }
        Reject(()=>NativeMapDocument.Create(12,12,"Missing").CreateCreatureGlobalsSession());
    }
}
