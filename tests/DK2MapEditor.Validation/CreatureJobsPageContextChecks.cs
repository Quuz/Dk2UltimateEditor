using DK2MapEditor.Core.Editing;

internal static class CreatureJobsPageContextChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is InvalidOperationException or ArgumentException){return;}throw new InvalidOperationException("Invalid jobs page buffer accepted.");}
    public static void Run()
    {
        using(var empty=new CreatureJobsPageContext())
        {
            Expect(!empty.Initialized&&empty.Index is null&&empty.Warnings.Count==0,"Unvisited Jobs page invented a request.");
            var input=new List<CreatureGlobalsJobWarning>{new(2,"Angry",51),new(2,"Happy",37),new(2,"Unhappy",29)};
            empty.Capture(2,input);input.Clear();
            Expect(empty.Initialized&&empty.Index==2&&empty.Warnings.Select(row=>row.Group).SequenceEqual(new[]{"Happy","Unhappy","Angry"}),"Source buffer ordering/copy differs.");
            Expect(CreatureJobsWarningPolicy.FirstWarning(empty.Warnings) is{Index:2,Group:"Happy",Total:37},"First stored buffer warning differs.");
            var revision=empty.Revision;var retained=empty.Warnings.ToArray();
            foreach(var malformed in new[]{new[]{new CreatureGlobalsJobWarning(1,"Happy",37)},new[]{new CreatureGlobalsJobWarning(2,"Hate",37)},new[]{new CreatureGlobalsJobWarning(2,"Happy",37),new(2,"Happy",38)}})
            {Reject(()=>empty.Capture(2,malformed));Expect(empty.Revision==revision&&empty.Warnings.SequenceEqual(retained),"Failed capture changed buffer authority.");}
            Reject(()=>empty.Capture(-1,Array.Empty<CreatureGlobalsJobWarning>()));
            empty.Capture(3,Array.Empty<CreatureGlobalsJobWarning>());Expect(empty.Initialized&&empty.Index==3&&empty.Warnings.Count==0,"Valid loaded row did not replace prior bad buffer.");
            empty.Dispose();Expect(empty.IsClosed&&!empty.Initialized&&empty.Index is null&&empty.Warnings.Count==0,"Closing retained source job buffers.");Reject(()=>empty.Capture(0,Array.Empty<CreatureGlobalsJobWarning>()));
        }
        var directory=Path.Combine(Path.GetTempPath(),"DK2-JobsPage-"+Guid.NewGuid().ToString("N"));
        var native=GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory,"basis"));
        var project=NativeMapDocument.CreateNewMap(new("JobsPage","JobsPage",20,20));
        foreach(var document in new[]{native,project})
        {
            document.SetHistoryDepth(65535);
            using var session=document.CreateCreatureGlobalsSession();using var context=new CreatureJobsPageContext();using var policy=new CreatureJobsWarningPolicy();
            var working=session.WorkingDocument;
            CreatureJobsDraft Pending(int index,int chance)
            {
                var row=working.GlobalDefinitionRows().Single(item=>item.Family=="Creatures"&&item.Index==index);var draft=working.CreateCreatureJobsDraft(row);
                foreach(var group in new[]{"Happy","Unhappy","Angry"})
                {var slots=Enumerable.Range(0,draft.Slots(group).Count).Where(slot=>draft.Slots(group)[slot].Job!="0").ToArray();draft=working.RemoveCreatureJobs(draft,group,slots);}
                return working.AddCreatureJob(draft,"Happy",CreatureJobs.Allowed("Happy")[0].ID,chance.ToString(System.Globalization.CultureInfo.InvariantCulture),"0","0");
            }
            void Capture(CreatureJobsDraft draft)=>context.Capture(draft.Index,draft.Totals.Where(total=>total.NeedsAcknowledgement).Select(total=>new CreatureGlobalsJobWarning(draft.Index,total.Group,total.Total)));
            void ReadOnly(Action action)
            {
                var live=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var draft=working.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
                var history=document.GetHistory();var privateHistory=working.GetHistory();var count=document.EditorChangeCount;var privateCount=working.EditorChangeCount;
                var revision=document.EditRevision;var privateRevision=working.EditRevision;var activity=document.ReadActivityStatistics()!.Encode();var notifications=0;
                EventHandler handler=(_,_)=>notifications++;document.Changed+=handler;try{action();}finally{document.Changed-=handler;}
                Expect(live.All(row=>row.Key.Data.Span.SequenceEqual(row.Value))&&draft.All(row=>row.Key.Data.Span.SequenceEqual(row.Value))
                    &&document.EditRevision==revision&&working.EditRevision==privateRevision&&document.EditorChangeCount==count&&working.EditorChangeCount==privateCount&&notifications==0
                    &&document.GetHistory().Undo.SequenceEqual(history.Undo)&&document.GetHistory().Redo.SequenceEqual(history.Redo)
                    &&working.GetHistory().Undo.SequenceEqual(privateHistory.Undo)&&document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity),"Source job callback consent mutated live/private map/history/activity.");
            }
            bool Consent(bool accepted,bool remember=false)
            {
                var bufferRevision=context.Revision;var plan=document.PrepareCreatureGlobals(session);
                using var request=policy.Begin(context.Warnings,()=>context.Revision==bufferRevision&&document.IsCreatureGlobalsPlanCurrent(plan));
                if(request is null)return true;
                if(!request.RequiresPrompt)return request.Continue();
                if(accepted)return request.Continue(remember);
                request.Cancel();return false;
            }
            var first=Pending(0,37);ReadOnly(()=>Capture(first));ReadOnly(()=>Expect(!Consent(false),"Source page cancellation accepted navigation."));
            ReadOnly(()=>Expect(Consent(true),"First navigation consent failed."));working.ApplyCreatureJobs(first,true);
            var second=Pending(1,73);Capture(second);ReadOnly(()=>Expect(Consent(true),"Second navigation consent failed."));working.ApplyCreatureJobs(second,true);
            var plan=document.PrepareCreatureGlobals(session);
            Expect(plan.JobWarnings.First() is{Index:0,Total:37}&&context.Warnings.First() is{Index:1,Total:73},"Independent two-row fixture did not distinguish all-row plan from retained Jobs buffer.");
            // Selecting Attributes for another creature cannot replace the source
            // HappyJobs arrays. An inactive Jobs PSN_APPLY checks last row1=73,
            // not the plan's first modified row0=37.
            ReadOnly(()=>{Expect(context.Index==1,"Unrelated row selection changed retained buffer.");Expect(!Consent(false),"Canceled inactive Jobs Apply accepted.");});
            ReadOnly(()=>Expect(Consent(true),"Inactive Jobs Apply consent failed."));
            var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var history=document.GetHistory().Undo.Count;var count=document.EditorChangeCount;
            document.ApplyCreatureGlobals(plan,true);
            Expect(document.GetHistory().Undo.Count==history+1&&document.EditorChangeCount==unchecked(count+1),"One accepted common Apply split history/counters.");
            document.Undo();Expect(before.All(row=>row.Key.Data.Span.SequenceEqual(row.Value))&&document.EditorChangeCount==count,"Common Apply Undo lost exact original bytes/counter.");document.Redo();
        }
        // Independent callback trace: source PSN_KILLACTIVE first, then Jobs
        // PSN_APPLY. Without remember there are two prompted requests for the
        // same active buffer; remembering the first auto-replies to the second.
        foreach(var remember in new[]{false,true})foreach(var replies in new[]{new[]{false,true},new[]{true,false},new[]{true,true}})
        {
            using var context=new CreatureJobsPageContext();context.Capture(0,new[]{new CreatureGlobalsJobWarning(0,"Happy",37)});
            using var policy=new CreatureJobsWarningPolicy();var prompts=0;var accepted=true;
            foreach(var reply in replies)
            {
                using var request=policy.Begin(context.Warnings,()=>true)!;
                if(!request.RequiresPrompt){request.Continue();continue;}
                prompts++;if(reply)request.Continue(remember);else{request.Cancel();accepted=false;break;}
            }
            var expectedPrompts=!replies[0]||remember?1:2;
            Expect(prompts==expectedPrompts&&accepted==(replies[0]&&(remember||replies[1])),"Documented KillActive/Apply trace, cancel or auto reply differs.");
        }
        using(var context=new CreatureJobsPageContext())using(var policy=new CreatureJobsWarningPolicy())
        {context.Capture(0,new[]{new CreatureGlobalsJobWarning(0,"Happy",37)});var revision=context.Revision;using var request=policy.Begin(context.Warnings,()=>context.Revision==revision&&!context.IsClosed)!;
            context.Capture(1,new[]{new CreatureGlobalsJobWarning(1,"Happy",73)});Expect(!request.IsCurrent,"Replaced source buffer retained consent.");Reject(()=>request.Continue(true));Expect(!policy.Suppressed,"Stale buffer remembered old consent.");}
    }
}
