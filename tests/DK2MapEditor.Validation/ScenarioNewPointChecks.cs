using DK2MapEditor.Core.Editing;

internal static class ScenarioNewPointChecks
{
    public static void Run()
    {
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Reject(Action action)
        {try{action();}catch(Exception error)when(error is InvalidOperationException or ArgumentException){return;}throw new Exception("Occupied/stale point choice accepted.");}
        foreach(var native in new[]{false,true})
        {
            var d=NativeMapDocument.CreateNewMap(new("NewPoint","New Point",20,20,NativeFormat:native));
            if(native&&!d.ScriptOwners().Any(o=>o.Scope==1&&o.Record.Read(o.Record.Fields.Single(f=>f.Name=="bGlobalPlayer"))=="4"))
                d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEPLAYER"),new(0,0),4);
            var level=d.ScriptOwners().Single(o=>o.Scope==32);var root=d.EnsureScriptScope(level);
            var type=LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMEELAPSED");
            var first=d.InsertScriptEvent(root.Owner,root.Field,type);var originalID=ScriptGraph.ID(first);
            var players=d.ScriptOwners().Where(o=>o.Scope==1).Take(2).ToArray();Check(players.Length==2,"Two distinct global player owners required.");
            var pointCount=ScriptGraph.Build(d).Roots.Count;var initial=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
            var count=d.EditorChangeCount;var history=d.GetHistory().Undo.Count;var revision=d.EditRevision;
            using(var canceled=d.CreateEditorScenarioPointSession(initialScope:1))
            {
                Check(!canceled.IsModifying&&canceled.SelectedOwner is null,"New point inherited the selected old owner.");
                canceled.SelectScope(32);Check(canceled.Choices.All(c=>!c.CanSelect),"An existing level allowed a second level point.");
                Reject(()=>canceled.SelectOwner(level));canceled.SelectScope(1);canceled.SelectOwner(players[0]);
                d.PrepareEditorScenarioPoint(canceled);
                Check(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&d.EditorChangeCount==count&&d.EditRevision==revision
                    &&d.GetHistory().Undo.Count==history,"Canceled New allocated a point or changed the source.");
            }
            foreach(var player in players)
            {
                var before=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var beforeCount=d.EditorChangeCount;var beforeUndo=d.GetHistory().Undo.Count;
                var oldRoots=ScriptGraph.Build(d).Roots.Select(r=>(r.Scope,r.Target,Bytes:r.Owner.File.Data.Slice(r.Owner.Offset,r.Owner.Length).ToArray())).ToArray();
                var events=ScriptGraph.Build(d).Events.ToDictionary(p=>p.Key,p=>p.Value.Record.File.Data.Slice(p.Value.Record.Offset,p.Value.Record.Length).ToArray());
                using var session=d.CreateEditorScenarioPointSession(initialScope:1);
                session.SelectOwner(player);var plan=d.PrepareEditorScenarioPoint(session);
                Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"New point preflight wrote owner bytes.");
                var saved=d.ApplyEditorScenarioPoint(plan);
                Check(saved.Scope==1&&saved.Target==0&&d.EditorChangeCount==unchecked(beforeCount+1),"New point did not start empty or count one acceptance.");
                var graph=ScriptGraph.Build(d);
                Check(graph.Events.Count==events.Count&&events.All(p=>graph.Events[p.Key].Record.File.Data.Slice(graph.Events[p.Key].Record.Offset,graph.Events[p.Key].Record.Length).Span.SequenceEqual(p.Value))
                    &&d.FindScriptScope(level)!.Target==originalID,"New point changed an existing event/level branch.");
                if(!native)
                {
                    Check(graph.Roots.Count==pointCount+1&&d.GetHistory().Undo.Count==beforeUndo+1,"Additional DKM point was selected instead of appended.");
                    var prefix=graph.Roots.Take(oldRoots.Length).ToArray();
                    Check(prefix.Length==oldRoots.Length&&prefix.Select((r,i)=>r.Scope==oldRoots[i].Scope&&r.Target==oldRoots[i].Target
                        &&r.Owner.File.Data.Slice(r.Owner.Offset,r.Owner.Length).Span.SequenceEqual(oldRoots[i].Bytes)).All(v=>v),"New DKM point rewrote/reordered earlier points.");
                    d.Undo();Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Undo New did not restore exact bytes.");d.Redo();
                    saved=d.FindScriptScope(player)!;pointCount++;
                }
                else Check(graph.Roots.Count==pointCount&&before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Native New invented an Action Point companion/order.");
                d.InsertScriptEvent(saved.Owner,saved.Field,type);
                using var occupied=d.CreateEditorScenarioPointSession(initialScope:1);
                Check(occupied.Choices.Single(c=>c.Owner.Record.File==player.Record.File&&c.Owner.Record.Offset==player.Record.Offset&&c.Owner.PlayerID==player.PlayerID).Status==ScenarioPointChoiceStatus.AlreadyBound,"Used global player was offered as a second point.");
                Reject(()=>occupied.SelectOwner(player));
            }
            Check(ScriptGraph.Build(d).Events.Count==3&&d.FindScriptScope(level)!.Target==originalID,"Three independent level/player branches were not retained.");
            using var stale=d.CreateEditorScenarioPointSession(initialScope:1);d.Paint([new(8,8)],new(4,1,1));Reject(()=>stale.SelectScope(4));
        }
        Console.WriteLine("SCENARIO NEW POINT DETAIL existing level remains independent; private New/Cancel, two additional DKM player points append with exact previous point/event bytes, empty new target, one acceptance/Undo, occupied level/player refusal; native existing roots/no invented list; WPF caller prepared separately");
    }
}
