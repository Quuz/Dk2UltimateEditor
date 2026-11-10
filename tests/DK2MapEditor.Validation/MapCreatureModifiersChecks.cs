using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class MapCreatureModifiersChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument doc)=>doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> state)=>Expect(state.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Creature modifier draft/cancel/Undo changed full bytes.");
    private static void Reject(Action action){try{action();}catch(Exception error) when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Invalid creature modifiers accepted.");}
    private static MapRuleKey Key(uint species,uint parameter)=>new(MapRuleKind.CreatureModifier,0,species,parameter);
    private static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new("Modifiers","Modifiers",12,12,NativeFormat:native));
    public static void Run()
    {
        using var fixture=JsonDocument.Parse(typeof(MapCreatureModifiersChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.MapCreatureModifiersSource.json")!);
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;
        foreach(var hash in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName,hash.Name)))).Equals(hash.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Original modifier callbacks changed.");
        Expect(fixture.RootElement.GetProperty("ParameterCount").GetInt32()==4&&fixture.RootElement.GetProperty("AverageNonzeroOnly").GetBoolean()
            &&fixture.RootElement.GetProperty("CancelDoesNotPublish").GetBoolean()&&fixture.RootElement.GetProperty("ChildIncrements").GetInt32()==0,"Source modifier context contract differs.");
        foreach(var (text,value) in new[]{("",0u),("bad",0u),("23suffix",23u),("  +45",45u),("-1",0u),("65535",65535u),("65536",65535u),("4294967295",0u),("4294967296",0u),("0x10",0u)})
            Expect(MapCreatureModifiersSession.ParseInput(text)==value,"Source decimal/clamp input differs.");
        foreach(var native in new[]{false,true})
        {
            var doc=New(native);var species=MapRules.Elements(MapRuleKind.CreatureModifier);var ordinary=species.Where(element=>!MapRules.IsElite(Key(element.ID,1))).Take(3).Select(element=>element.ID).ToArray();
            var elite=species.First(element=>MapRules.IsElite(Key(element.ID,1))).ID;
            var literal=new uint[,]{{10,0,100,7},{20,0,0,9},{0,30,200,0}};
            doc.SetMapRules(Enumerable.Range(0,3).SelectMany(i=>Enumerable.Range(0,4).Select(p=>new MapRuleChange(Key(ordinary[i],(uint)p+1),literal[i,p]==0?null:literal[i,p]))));
            var before=Snapshot(doc);var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;var usage=doc.ReadActivityStatistics()!.Encode();
            using(var draft=doc.CreateMapCreatureModifiersSession())
            {
                Expect(draft.Rows.Count==species.Count&&draft.Rows.Count(row=>row.Elite)==13,"Source species/elite rows missing.");
                for(var mask=0;mask<16;mask++)
                {
                    var selected=Enumerable.Range(0,3).Where(i=>(mask&(1<<i))!=0).Select(i=>ordinary[i]).Concat((mask&8)!=0?[elite]:Array.Empty<uint>()).ToArray();draft.Select(selected);
                    var expected=Enumerable.Range(0,4).Select(p=>{var entries=Enumerable.Range(0,3).Where(i=>(mask&(1<<i))!=0&&literal[i,p]!=0).Select(i=>literal[i,p]).ToArray();return entries.Length==0?0u:(uint)(entries.Sum(value=>(long)value)/entries.Length);}).ToArray();
                    Expect(draft.Averages().SequenceEqual(expected),"Independent four source nonzero means/elite exclusion differs.");
                }
                draft.Select([ordinary[0],ordinary[1],elite]);Expect(draft.SetParameter(2,"65536")==2,"Source parameter setter included elite or omitted selected normal species.");
                Expect(draft.Rows.Where(row=>row.ID==ordinary[0]||row.ID==ordinary[1]).All(row=>row.Values[1]==65535)&&draft.Rows.Single(row=>row.ID==ordinary[2]).Values[1]==30,"Private single-parameter write touched unselected species.");
                Exact(before);Expect(doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history&&doc.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(usage),"Private source controls published history/count/activity.");
            }
            Exact(before);
            using(var outer=doc.CreateMapPropertiesSession())
            {
                var working=outer.WorkingDocument;
                using(var draft=working.CreateMapCreatureModifiersSession())
                {draft.Select(ordinary.Concat([elite]));draft.SetParameter(1,"42");draft.SetParameter(4,"0");working.ApplyMapCreatureModifiers(draft);}
                Exact(before);var workingValues=working.MapRuleCells().Where(cell=>cell.Key.Kind==MapRuleKind.CreatureModifier).ToDictionary(cell=>cell.Key,cell=>cell.Value);
                Expect(ordinary.All(id=>workingValues[Key(id,1)]==42&&workingValues[Key(id,4)] is null),"Child OK did not publish complete private parameter changes.");
                var plan=doc.PrepareEditorMapProperties(outer,new(doc.Width,doc.Height));doc.ApplyEditorMapProperties(plan);
                Expect(doc.EditorChangeCount==unchecked(count+1)&&doc.GetHistory().Undo.Count==history+1,"Parent modifier composition added child counters or several Undo steps.");
                var after=Snapshot(doc);doc.Undo();Exact(before);Expect(doc.EditorChangeCount==count,"Modifier parent Undo lost count.");doc.Redo();Exact(after);doc.Undo();Exact(before);
            }
            using(var outer=doc.CreateMapPropertiesSession())
            {using var draft=outer.WorkingDocument.CreateMapCreatureModifiersSession();draft.Select(ordinary);draft.SetParameter(3,"50");outer.WorkingDocument.ApplyMapCreatureModifiers(draft);}
            Exact(before);
            using(var draft=doc.CreateMapCreatureModifiersSession())
            {draft.Select([elite]);Expect(!draft.CanEditSelection&&draft.SetParameter(1,"50")==0&&!draft.Changed,"Elite-only selection was editable.");Reject(()=>draft.Select([uint.MaxValue]));Reject(()=>draft.SetParameter(5,"1"));doc.ApplyMapCreatureModifiers(draft);Reject(()=>draft.Select(ordinary));}
            Exact(before);Expect(doc.GetHistory().Undo.Count==history,"Equal/elite-only child produced map history.");
            using(var stale=doc.CreateMapCreatureModifiersSession())
            {doc.Paint([new(4,4)],new(3,2,1));var changed=Snapshot(doc);Reject(()=>stale.Select(ordinary));Reject(()=>doc.ApplyMapCreatureModifiers(stale));Exact(changed);doc.Undo();}
            Exact(before);
            using var foreign=New(native).CreateMapCreatureModifiersSession();Reject(()=>doc.ApplyMapCreatureModifiers(foreign));
            using(var nested=doc.CreateMapCreatureModifiersSession()){nested.Select(ordinary);nested.SetParameter(1,"60");doc.Edit("Nested modifiers",()=>Reject(()=>doc.ApplyMapCreatureModifiers(nested)));}Exact(before);
            Console.WriteLine($"CREATURE MODIFIER DETAIL native={native} all catalogue rows/13 elites,16 literal selection means, four independent fields, private child/parent Cancel and source acceptance1/oneUndo, decimal clamping, equal/elite/foreign/stale/nested/closed guards");
        }
    }
}
