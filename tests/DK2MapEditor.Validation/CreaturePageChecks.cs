using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class CreaturePageChecks
{
    private static void Expect(bool value,string message) { if (!value) throw new Exception(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid creature page action accepted."); }
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file,file => file.Data.ToArray());
    private static void Exact(NativeMapDocument doc,Dictionary<NativeFile,byte[]> bytes) => Expect(doc.Files.Count == bytes.Count && doc.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)),"Creature page changed unrelated/private/no-op/undo bytes.");
    private static GlobalDefinitionRow Row(NativeMapDocument doc,int index=0) => doc.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == index);
    private static CreaturePageDraft Read(NativeMapDocument doc,string section,int index=0) => doc.CreateCreaturePageDraft(Row(doc,index),section);
    private static CreaturePageInput[] Inputs(CreaturePageDraft draft,params (string Field,string Value)[] edits) => CreaturePageControls.Fields(draft.Section).Select(field => new CreaturePageInput(field,
        edits.LastOrDefault(edit => edit.Field == field) is var change && change.Field is not null ? change.Value : draft.Display(field))).ToArray();
    private static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static void Store(Dictionary<NativeFile,byte[]> bytes,NativeRecord record,JsonElement storage,string raw)
    {
        var offset=record.Offset+storage.GetProperty("Offset").GetInt32(); var value=long.Parse(raw,CultureInfo.InvariantCulture);var data=bytes[record.File];
        if(storage.TryGetProperty("Mask",out var bit)){var mask=bit.GetByte();data[offset]=(byte)((data[offset]&~mask)|(value==0?0:mask));}
        else if(storage.GetProperty("Size").GetInt32()==1)data[offset]=unchecked((byte)value);
        else if(storage.GetProperty("Size").GetInt32()==2)BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset),unchecked((ushort)value));
        else BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset),unchecked((uint)value));
    }
    public static void Run()
    {
        using var source=JsonDocument.Parse(typeof(CreaturePageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.CreatureRemainingPagesSource.json")!);
        var oracle=source.RootElement;var pages=oracle.GetProperty("Pages").EnumerateArray().ToArray();
        Expect(pages.Length==4 && pages.Sum(page=>page.GetProperty("FieldCount").GetInt32())==75,"Independent four-page scope incomplete.");
        foreach(var page in pages)
        {
            var section=page.GetProperty("Section").GetString()!;
            Expect(CreaturePageControls.Fields(section).Order().SequenceEqual(page.GetProperty("Fields").EnumerateArray().Select(field=>field.GetString()!).Order()),"Incomplete source page:"+section);
        }
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"Source")))root=root.Parent;
        foreach(var entry in oracle.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName,entry.Name)))).Equals(entry.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Source page input changed.");
        foreach(var (field,catalogue,type) in new[]{("ArmourType","GDoorsMats","0"),("FightStyle","GFightStyle","0"),("JobClass","GJobClass","0"),("LairObject","GBeds","0"),("SlapEffect","Effects","0"),("HealthEffect","Effects","0"),("EntranceEffect","Effects","0"),("DeathEffect","Effects","0"),("Attract1ID","GARooms","1"),("Attract2ID","GARooms","1"),("Attract2ID","GCreatures","4")})
        {
            var choices=CreaturePageControls.Choices(field,type);
            Expect(choices.Select(choice=>choice.Value).SequenceEqual(oracle.GetProperty("Catalogues").GetProperty(catalogue).EnumerateArray().Select(item=>item.GetProperty("ID").GetUInt32().ToString(CultureInfo.InvariantCulture))),"Complete parameter catalogue differs:"+field);
            Expect(choices.All(choice=>!string.IsNullOrWhiteSpace(choice.English)&&!string.IsNullOrWhiteSpace(choice.Russian)),"Missing bilingual parameter choices.");
        }
        Expect(CreaturePageControls.Choices("Attract1Type").Select(choice=>choice.Value).SequenceEqual(new[]{"0","1"})&&CreaturePageControls.Choices("Attract2Type").Select(choice=>choice.Value).SequenceEqual(new[]{"0","1","2","3","4"}),"Attraction source types reordered.");
        foreach(var type in new[]{"0","1","2","3","4","255"})Expect(CreaturePageControls.Enabled("Attract1ID",type)==(type=="1")&&CreaturePageControls.Enabled("Attract2ID",type)==(type is "1" or "4")&&CreaturePageControls.Enabled("Attract2Data",type)==(type is "1" or "2" or "3" or "4"),"Attraction control availability differs.");
        var directory=Path.Combine(Path.GetTempPath(),"DK2CreaturePages",Guid.NewGuid().ToString("N"));
        var native=GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory,"basis"));
        foreach(var doc in new[]{NativeMapDocument.CreateNewMap(new("CreaturePages","CreaturePages",20,20)),native})
        {
            doc.SetHistoryDepth(65535);var initial=Snapshot(doc);var noOps=0;var normalized=0;
            CreatureAttractionChecks.Run(doc,oracle,root!.FullName);Exact(doc,initial);
            foreach(var index in Enumerable.Range(0,45))foreach(var section in CreaturePageControls.Sections)
            {
                var draft=Read(doc,section,index);var prepared=doc.UpdateCreaturePageDraft(draft,Inputs(draft));Exact(doc,initial);
                var expected=CreaturePageControls.Fields(section).ToDictionary(field=>field,draft.Raw);
                if(section=="Parameters")
                {
                    if(draft.Raw("Attract1Type")=="0")expected["Attract1ID"]="0";
                    if(draft.Raw("Attract2Type")=="0"){expected["Attract2ID"]="0";expected["Attract2Data"]="0";}
                    if(draft.Raw("Attract2Type") is "2" or "3")expected["Attract2ID"]="0";
                }
                Expect(expected.All(item=>prepared.Raw(item.Key)==item.Value),"Source re-entry/cleanup changed other values.");
                var history=doc.GetHistory().Undo.Count;doc.ApplyCreaturePageDraft(prepared);
                Expect(doc.GetHistory().Undo.Count==history+(prepared.Changed?1:0),"Page re-entry split/materialized history.");
                if(prepared.Changed){normalized++;doc.Undo();}else noOps++;Exact(doc,initial);
            }
            // All four typed pages compose privately, preserving the original live bytes.
            var session=doc.CreateCreatureGlobalsSession();var working=session.WorkingDocument;var changes=new Dictionary<string,string>();
            foreach(var section in CreaturePageControls.Sections)
            {
                var draft=Read(working,section);var edits=new List<(string,string)>();
                foreach(var field in CreaturePageControls.Fields(section))
                {
                    string value;
                    if(CreaturePageControls.Boolean(section,field))value=draft.Display(field)=="0"?"1":"0";
                    else if(section=="Parameters")value=field switch{"Attract1Type"=>"1","Attract2Type"=>"4","Attract1ID"=>"2","Attract2ID"=>CreaturePageControls.Choices(field,"4").Last().Value,"Attract2Data"=>"16000",_=>CreaturePageControls.Choices(field).Last().Value};
                    else if(field is "LightR" or "LightG" or "LightB")value=field switch{"LightR"=>"17","LightG"=>"34",_=>"51"};
                    else {var format=CreaturePageControls.Format(field,!working.IsLegacyProject);value=format.Fractional?"1.25":format.SignedAttribute?"-9":"123";}
                    edits.Add((field,value));
                }
                var prepared=working.UpdateCreaturePageDraft(draft,Inputs(draft,edits.ToArray()));
                foreach(var field in CreaturePageControls.Fields(section))changes[field]=prepared.Raw(field);
                working.ApplyCreaturePageDraft(prepared);Exact(doc,initial);
            }
            var plan=doc.PrepareCreatureGlobals(session);var priorHistory=doc.GetHistory().Undo.Count;
            Expect(plan.JobWarnings.Count==0,"Non-job pages acquired a job warning.");doc.ApplyCreatureGlobals(plan);
            Expect(doc.GetHistory().Undo.Count==priorHistory+1,"Four-page Apply is not one Undo.");
            foreach(var section in CreaturePageControls.Sections)Expect(CreaturePageControls.Fields(section).All(field=>Read(doc,section).Raw(field)==changes[field]),"Common Apply lost a typed page.");
            var applied=Snapshot(doc);
            if(!doc.IsLegacyProject){var expected=initial.ToDictionary(item=>item.Key,item=>item.Value.ToArray());foreach(var item in changes)Store(expected,Row(doc).Record!,oracle.GetProperty("NativeStorage").GetProperty(item.Key),item.Value);Exact(doc,expected);}
            var reopened=NativeMapDocument.Load(doc.SaveCopy(Path.Combine(directory,doc.IsLegacyProject?"project":"native")));
            foreach(var section in CreaturePageControls.Sections)Expect(CreaturePageControls.Fields(section).All(field=>Read(reopened,section).Raw(field)==changes[field]),"Save/reopen lost a typed page.");
            if(doc.IsLegacyProject)
            {
                var compiled=doc.CompileLegacyProject("CreaturePagesCompiled",native);var record=Row(compiled).Record!;
                var expected=Snapshot(compiled);foreach(var item in changes)Store(expected,record,oracle.GetProperty("NativeStorage").GetProperty(item.Key),item.Value);Exact(compiled,expected);
            }
            doc.Undo();Exact(doc,initial);doc.Redo();Exact(doc,applied);doc.Undo();Exact(doc,initial);
            foreach(var section in CreaturePageControls.Sections)
            {
                var current=Read(doc,section);
                foreach(var field in CreaturePageControls.Fields(section).Where(field=>!CreaturePageControls.Named(field)&&!CreaturePageControls.Boolean(section,field)))
                {
                    var format=CreaturePageControls.Format(field,!doc.IsLegacyProject);
                    var extras=section=="Parameters"?new[]{("Attract2Type","4"),("Attract2ID",CreaturePageControls.Choices("Attract2ID","4").First().Value)}:Array.Empty<(string,string)>();
                    foreach(var endpoint in new[]{format.Minimum!.Value,format.Maximum!.Value})
                    {
                        var prepared=doc.UpdateCreaturePageDraft(current,Inputs(current,extras.Concat(new[]{(field,Number(endpoint))}).ToArray()));
                        doc.ApplyCreaturePageDraft(prepared);Expect(decimal.Parse(Read(doc,section).Display(field),CultureInfo.InvariantCulture)==endpoint,"Numeric source/storage endpoint differs:"+field);
                        if(prepared.Changed)doc.Undo();Exact(doc,initial);
                    }
                    foreach(var invalid in new[]{Number(format.Minimum!.Value-1),Number(format.Maximum!.Value+1),"bad","1e2"})Reject(()=>doc.UpdateCreaturePageDraft(current,Inputs(current,extras.Concat(new[]{(field,invalid)}).ToArray())));
                }
                Reject(()=>doc.UpdateCreaturePageDraft(current,Inputs(current).Skip(1)));
                Reject(()=>doc.UpdateCreaturePageDraft(current,Inputs(current).Append(Inputs(current)[0])));
                Reject(()=>NativeMapDocument.CreateNewMap(new("Foreign","Foreign",12,12)).ApplyCreaturePageDraft(current));
            }
            var parameters=Read(doc,"Parameters");
            foreach(var type in new[]{"0","1","2","3","4"})
            {
                var edits=new[]{("Attract1Type","0"),("Attract1ID","bad"),("Attract2Type",type),("Attract2ID",type=="1"?"2":type=="4"?CreaturePageControls.Choices("Attract2ID","4").First().Value:"bad"),("Attract2Data",type=="0"?"bad":"123")};
                var prepared=doc.UpdateCreaturePageDraft(parameters,Inputs(parameters,edits));
                Expect(prepared.Raw("Attract1ID")=="0"&&prepared.Raw("Attract2Data")== (type=="0"?"0":"123")&&(type is "0" or "2" or "3"?prepared.Raw("Attract2ID")=="0":prepared.Raw("Attract2ID")!="0"),"Source attraction Save cleanup differs.");Exact(doc,initial);
            }
            var flags=Read(doc,"Flags");var trainable=doc.UpdateCreaturePageDraft(flags,Inputs(flags,("DoNotTrain",flags.Display("DoNotTrain")=="0"?"1":"0")));
            Expect((trainable.Raw("DoNotTrain")=="0")==(trainable.Display("DoNotTrain")=="1"),"Trainable checkbox is not inverted.");
            var light=Read(doc,"Light");var colour=doc.UpdateCreaturePageColour(light,0x332211);Expect(CreaturePageControls.Colour(colour)==0x332211&&colour.Raw("LightR")=="17"&&colour.Raw("LightG")=="34"&&colour.Raw("LightB")=="51","COLORREF order differs.");Exact(doc,initial);
            Reject(()=>doc.UpdateCreaturePageColour(light,0x1000000));Reject(()=>doc.UpdateCreaturePageColour(flags,0));
            Expect(CreaturePageControls.Step("Light","LightX","-1.75",1,!doc.IsLegacyProject)=="0"&&CreaturePageControls.Step("Light","LightRadius","2.75",1,!doc.IsLegacyProject)=="3","Original whole-part spinner conversion differs.");
            Reject(()=>CreaturePageControls.Step("Parameters","FightStyle","0",1,!doc.IsLegacyProject));
            foreach(var section in CreaturePageControls.Sections)
            {
                var stale=Read(doc,section);var field=CreaturePageControls.Fields(section).First();
                doc.SetGlobalDefinitionValues([new("Creatures",0,field,stale.Raw(field)=="1"?"0":"1")]);var changed=Snapshot(doc);
                Reject(()=>doc.ApplyCreaturePageDraft(stale));Reject(()=>doc.UpdateCreaturePageDraft(stale,Inputs(stale)));Exact(doc,changed);doc.Undo();Exact(doc,initial);
            }
            var imports=new List<GlobalDefinitionChange>{new("Creatures",0,"Health",doc.IsLegacyProject?"100000":"65535"),new("Creatures",0,"ArmourType","254"),new("Creatures",0,"FightStyle","253"),new("Creatures",0,"JobClass","252"),new("Creatures",0,"LairObject","251"),new("Creatures",0,"DeathEffect","63000"),new("Creatures",0,"Attract1Type","254"),new("Creatures",0,"Attract1ID","253"),new("Creatures",0,"Attract2Type","254"),new("Creatures",0,"Attract2ID","253"),new("Creatures",0,"Attract2Data",doc.IsLegacyProject?"100000":"65535")};
            doc.Edit("Imported page fixture",()=>
            {
                doc.SetGlobalDefinitionValues(imports);
                if(doc.IsLegacyProject)
                {
                    var record=Row(doc).Record!;
                    foreach(var field in new[]{"DoNotTrain","LightPulse","Emotionless","ImmuneLightning"})doc.SetField(record,record.Fields.Single(item=>item.Name==field),"2");
                }
            });
            var imported=Snapshot(doc);var historyAfterImport=doc.GetHistory().Undo.Count;
            foreach(var section in CreaturePageControls.Sections)
            {
                var retained=Read(doc,section);var prepared=doc.UpdateCreaturePageDraft(retained,Inputs(retained));
                Expect(!prepared.Changed,"Unknown/wide/BOOL import re-entry became a repair:"+section);doc.ApplyCreaturePageDraft(prepared);Exact(doc,imported);
            }
            Expect(doc.GetHistory().Undo.Count==historyAfterImport,"Imported no-op consumed history.");
            var repaired=Read(doc,"Parameters");doc.ApplyCreaturePageDraft(doc.UpdateCreaturePageDraft(repaired,Inputs(repaired,("FightStyle","4"))));
            Expect(Read(doc,"Parameters").Raw("DeathEffect")=="63000"&&Read(doc,"Parameters").Raw("Attract2Data")== (doc.IsLegacyProject?"100000":"65535"),"One parameter repair normalized unrelated imports.");
            if(doc.IsLegacyProject)Expect(doc.GlobalDefinitionValues(Row(doc)).Where(value=>value.Name is "Emotionless" or "ImmuneLightning").All(value=>value.Value=="2"),"Typed page repaired hidden flags.");
            doc.Undo();Exact(doc,imported);doc.Undo();Exact(doc,initial);
            Console.WriteLine($"CREATURE PAGES DETAIL native={!doc.IsLegacyProject} pages=4 fields=75 rows=45 reentry={noOps} source-cleanup={normalized} typed-catalogues ranges trainable-inversion COLORREF private-compose packed compiler reopen one-undo imports hidden-flags stale foreign");
        }
    }
}
