using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class ScenarioInsertionChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception e)when(e is ArgumentException or InvalidOperationException or InvalidDataException){return;}throw new Exception("Invalid insertion destination accepted.");}
    private static ScriptEdge Next(ScriptEvent e)=>e.Edges.Single(f=>f.Field.Name is "NextOr" or "or" or "next");
    private static ScriptEdge Child(ScriptEvent e)=>e.Edges.Single(f=>f.Field.Name is "ChildAnd" or "and");
    private static bool Same(ScriptEdge a,ScriptEdge b)=>a.Owner.File==b.Owner.File&&a.Owner.Offset==b.Owner.Offset&&a.Field==b.Field&&a.Target==b.Target;
    private static IEnumerable<ScriptTreeEntry> Entries(ScriptTreeEntry root)
    {var stack=new Stack<ScriptTreeEntry>();stack.Push(root);while(stack.TryPop(out var e)){yield return e;foreach(var c in e.Children.Reverse())stack.Push(c);}}
    public static void Run()
    {
        foreach(var native in new[]{false,true})Matrix(native);
    }
    private static void Matrix(bool native)
    {
        var doc=NativeMapDocument.CreateNewMap(new("InsertCommands","Insert commands",20,20,NativeFormat:native));
        var binding=doc.EnsureScriptScope(doc.ScriptOwners().Single(o=>o.Scope==32));
        var ids=new Dictionary<char,ushort>();
        foreach(var (name,condition) in new[]{('A',true),('B',true),('C',true),('D',false),('E',true),('F',false),('G',false),('H',false)})
            ids[name]=ScriptGraph.ID(doc.AddScenarioEvent(condition?0xD5u:0xD6u,new(0,0)));
        ScriptGraph Graph()=>ScriptGraph.Build(doc);
        ScriptEdge Root()=>Graph().Roots.Single(e=>e.Scope==32);
        void Link(char owner,bool child,char target)
        {var item=Graph().Events[ids[owner]];var edge=child?Child(item):Next(item);doc.SetField(edge.Owner,edge.Field,ids[target].ToString());}
        doc.Edit("Source tree fixture",()=>
        {
            var r=Root();doc.SetField(r.Owner,r.Field,ids['A'].ToString());
            Link('A',false,'B');Link('A',true,'C');Link('C',false,'D');Link('D',false,'E');Link('C',true,'F');Link('F',false,'G');Link('B',true,'H');
        });
        var bytes=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var revision=doc.EditRevision;var history=doc.GetHistory();
        void Exact()=>Check(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history.Undo.Count,"Planning/cancel/Undo changed live state.");
        ScriptTreeEntry Project()=>ScriptTreeProjection.Build(Graph(),[Root()]).Roots.Single();
        var cases=new[]{
            (ScenarioInsertCommand.NewRoot,'A','B',false),(ScenarioInsertCommand.NewRoot,'D','B',false),
            (ScenarioInsertCommand.AppendCondition,'A','E',false),(ScenarioInsertCommand.AppendAction,'A','E',false),
            (ScenarioInsertCommand.AppendCondition,'D','E',false),(ScenarioInsertCommand.AppendAction,'D','E',false),
            (ScenarioInsertCommand.AppendCondition,'C','G',false),(ScenarioInsertCommand.AppendAction,'C','G',false),
            (ScenarioInsertCommand.AppendCondition,'E','E',true),(ScenarioInsertCommand.AppendAction,'B','H',false),
            (ScenarioInsertCommand.InsertCondition,'D','C',false),(ScenarioInsertCommand.InsertAction,'D','C',false),
            (ScenarioInsertCommand.InsertAction,'C','A',true)};
        foreach(var (command,selection,owner,child) in cases)
        {
            var graph=Graph();var scope=Project();var selected=Entries(scope).Single(e=>e.EventID==ids[selection]);var stats=doc.ReadActivityStatistics()!.Encode();
            var destination=ScenarioInsertion.Resolve(graph,scope,selected,command);var expected=child?Child(graph.Events[ids[owner]]):Next(graph.Events[ids[owner]]);
            Check(Same(destination,expected),$"Source {command}/{selection} chose a head or another sibling list.");Exact();
            Check(doc.EditRevision==revision&&doc.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(stats),"Read-only resolver changed revision/activity.");
            var signature=command is ScenarioInsertCommand.AppendAction or ScenarioInsertCommand.InsertAction?0xD6u:0xD5u;
            var type=LegacyEventSchemas.All.Single(t=>t.Name==(signature==0xD5?"TRIGGER_TIMEELAPSED":"ACTION_SETVARIABLE"));
            using(var cancel=doc.CreateScenarioCreationSession(doc.CreateScenarioCreationContext(),signature,type,destination))doc.PrepareScenarioCreation(cancel);
            Exact();
            var prior=graph.Events.ToDictionary(p=>p.Key,p=>p.Value.Record.File.Data.Slice(p.Value.Record.Offset,p.Value.Record.Length).ToArray());
            // DKM stores all conditions before actions. A new condition moves
            // every old ACTION offset: capture source identities before Finish.
            var destinationOwnerID=ScriptGraph.IsEvent(destination.Owner)?ScriptGraph.ID(destination.Owner):(ushort)0;
            var otherIncoming=graph.Roots.Select(e=>(Edge:e,Owner:(ushort)0))
                .Concat(graph.Events.Values.SelectMany(item=>item.Edges.Select(e=>(Edge:e,Owner:item.ID))))
                .Where(row=>row.Edge.Target==destination.Target&&!Same(row.Edge,destination)).Select(row=>row.Owner).ToArray();
            using var session=doc.CreateScenarioCreationSession(doc.CreateScenarioCreationContext(),signature,type,destination);
            var added=doc.ApplyScenarioEditorCreation(doc.PrepareScenarioCreation(session));var id=ScriptGraph.ID(added);var after=Graph();
            Check(after.Events.Count==graph.Events.Count+1&&doc.GetHistory().Undo.Count==history.Undo.Count+1&&doc.EditorChangeCount==unchecked(count+1),"Shared wizard did not make one insertion/history/source count.");
            Check(Next(after.Events[id]).Target==destination.Target&&(!after.Events[id].Edges.Any(e=>e.Field.Name is "ChildAnd" or "and")||Child(after.Events[id]).Target==0),"Inserted event did not retain the old continuation.");
            foreach(var old in graph.Events.Values)
            {
                var expectedBytes=prior[old.ID].ToArray();
                if(destinationOwnerID!=0&&old.ID==destinationOwnerID)
                    BinaryPrimitives.WriteUInt16LittleEndian(expectedBytes.AsSpan(native?(destination.Field.Name=="ChildAnd"?20:18):(destination.Field.Name=="and"?52:50)),id);
                if(!native&&old.ID==destination.Target)
                {
                    var incoming=otherIncoming.Append(id).Distinct().ToArray();
                    BinaryPrimitives.WriteUInt16LittleEndian(expectedBytes.AsSpan(12),incoming.Length==1?incoming[0]:(ushort)0);
                }
                var r=after.Events[old.ID].Record;var actual=r.File.Data.Slice(r.Offset,r.Length).ToArray();
                Check(actual.AsSpan().SequenceEqual(expectedBytes),$"Insertion differs native={native} command={command} selection={selection} oldID={old.ID}: "+
                    string.Join(",",Enumerable.Range(0,r.Length).Where(i=>actual[i]!=expectedBytes[i]).Select(i=>$"{i}:expected={expectedBytes[i]},actual={actual[i]}")));
            }
            var committed=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());doc.Undo();Exact();doc.Redo();Check(committed.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Redo lost the exact insertion.");doc.Undo();Exact();revision=doc.EditRevision;
        }
        var root=Project();
        foreach(var c in new[]{'A','B'})Reject(()=>ScenarioInsertion.Resolve(Graph(),root,Entries(root).Single(e=>e.EventID==ids[c]),ScenarioInsertCommand.InsertAction));
        Reject(()=>ScenarioInsertion.Resolve(Graph(),root,null,ScenarioInsertCommand.AppendCondition));
        Reject(()=>ScenarioInsertion.Resolve(Graph(),root,Entries(Project()).First(e=>e.EventID==ids['C']),ScenarioInsertCommand.AppendAction));
        Reject(()=>ScenarioInsertion.Resolve(Graph(),root,root,(ScenarioInsertCommand)99));
        Check(Same(ScenarioInsertion.Resolve(Graph(),root,root,ScenarioInsertCommand.NewRoot),Next(Graph().Events[ids['B']])) ,"Scope selection did not append a root condition.");Exact();
        // Shared C has two different visible incoming occurrences. Insertion
        // before its second occurrence must target B.and, never A.and.
        Link('B',true,'C');root=Project();var occurrences=Entries(root).Where(e=>e.EventID==ids['C']).ToArray();
        Check(occurrences.Length==2&&Same(ScenarioInsertion.Resolve(Graph(),root,occurrences[1],ScenarioInsertCommand.InsertAction),Child(Graph().Events[ids['B']])),"Shared occurrence lost its actual visual parent.");
        doc.Undo();Exact();revision=doc.EditRevision;
        root=Project();var oldD=Entries(root).Single(e=>e.EventID==ids['D']);var cNext=Next(Graph().Events[ids['C']]);doc.SetField(cNext.Owner,cNext.Field,"0");
        Reject(()=>ScenarioInsertion.Resolve(Graph(),root,oldD,ScenarioInsertCommand.InsertCondition));doc.Undo();Exact();
        root=Project();var cEntry=Entries(root).Single(e=>e.EventID==ids['C']);var gNext=Next(Graph().Events[ids['G']]);doc.SetField(gNext.Owner,gNext.Field,ids['F'].ToString());
        Reject(()=>ScenarioInsertion.Resolve(Graph(),root,cEntry,ScenarioInsertCommand.AppendAction));doc.Undo();Exact();
        root=Project();cEntry=Entries(root).Single(e=>e.EventID==ids['C']);gNext=Next(Graph().Events[ids['G']]);doc.SetField(gNext.Owner,gNext.Field,"32000");
        Reject(()=>ScenarioInsertion.Resolve(Graph(),root,cEntry,ScenarioInsertCommand.AppendAction));doc.Undo();Exact();
        var idRecord=Graph().Events[ids['A']].Record;Check(MemoryMarshal.TryGetArray(idRecord.File.Data,out var array),"Missing raw fixture.");
        var offset=array.Offset+idRecord.Offset+(native?16:48);var oldID=BinaryPrimitives.ReadUInt16LittleEndian(array.Array!.AsSpan(offset));BinaryPrimitives.WriteUInt16LittleEndian(array.Array.AsSpan(offset),0);
        Reject(()=>ScenarioInsertion.Resolve(Graph(),root,root,ScenarioInsertCommand.NewRoot));BinaryPrimitives.WriteUInt16LittleEndian(array.Array.AsSpan(offset),oldID);Exact();
        var empty=NativeMapDocument.CreateNewMap(new("EmptyInsert","Empty insert",20,20,NativeFormat:native));empty.EnsureScriptScope(empty.ScriptOwners().Single(o=>o.Scope==32));var emptyGraph=ScriptGraph.Build(empty);
        var emptyRoot=ScriptTreeProjection.Build(emptyGraph,[emptyGraph.Roots.Single(e=>e.Scope==32)]).Roots.Single();
        Check(ScenarioInsertion.Resolve(emptyGraph,emptyRoot,emptyRoot,ScenarioInsertCommand.NewRoot).Target==0,"Empty point cannot create its first root condition.");
        Reject(()=>ScenarioInsertion.Resolve(Graph(),emptyRoot,emptyRoot,ScenarioInsertCommand.NewRoot));
    }
}
