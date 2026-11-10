using DK2MapEditor.Core.Editing;

internal static class CreatureJobsWarningChecks
{
    private static void Expect(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is InvalidOperationException or ArgumentException){return;}throw new InvalidOperationException("Invalid source job request accepted.");}
    public static void Run()
    {
        // Literal source LONG totals. This tests the request's source projection;
        // existing raw long totals and all generic creature edit APIs stay intact.
        foreach(var (raw,wire,invalid) in new (long,int,bool)[]{(0,0,false),(100,100,false),(37,37,true),(-1,-1,true),
            (101,101,true),(int.MaxValue,int.MaxValue,true),(2147483648L,int.MinValue,true),
            (4294967296L,0,false),(4294967396L,100,false),(4294967333L,37,true),(-4294967297L,-1,true)})
        {
            var result=CreatureJobsWarningPolicy.FirstWarning(new[]{new CreatureGlobalsJobWarning(0,"Happy",raw)});
            Expect((result is not null)==invalid&&(!invalid||result!.Total==wire),"Source signed LONG total/warning projection differs.");
        }
        var order=CreatureJobsWarningPolicy.FirstWarning(new[]{new CreatureGlobalsJobWarning(2,"Happy",100),new(2,"Unhappy",0),new(2,"Angry",37)});
        Expect(order is{Index:2,Group:"Angry",Total:37},"First source Happy/Unhappy/Angry warning order differs.");
        Reject(()=>CreatureJobsWarningPolicy.FirstWarning(new[]{new CreatureGlobalsJobWarning(0,"Hate",1)}));
        Reject(()=>CreatureJobsWarningPolicy.FirstWarning(new[]{new CreatureGlobalsJobWarning(-1,"Happy",1)}));
        var directory=Path.Combine(Path.GetTempPath(),"DK2-JobsWarning-"+Guid.NewGuid().ToString("N"));
        var native=GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory,"basis"));
        var project=NativeMapDocument.CreateNewMap(new("JobsWarning","JobsWarning",20,20));
        var caseCount=0;
        foreach(var document in new[]{native,project})
        {
            using var session=document.CreateCreatureGlobalsSession();var privateDocument=session.WorkingDocument;
            var row=privateDocument.GlobalDefinitionRows().Single(item=>item.Family=="Creatures"&&item.Index==0);
            var draft=privateDocument.CreateCreatureJobsDraft(row);
            var occupied=Enumerable.Range(0,3).Where(slot=>draft.Slots("Happy")[slot].Job!="0").ToArray();
            if(occupied.Length>0)draft=privateDocument.RemoveCreatureJobs(draft,"Happy",occupied);
            draft=privateDocument.AddCreatureJob(draft,"Happy",CreatureJobs.Allowed("Happy")[0].ID,"37","-9","11");
            var warning=new CreatureGlobalsJobWarning(0,"Happy",draft.Totals.Single(total=>total.Group=="Happy").Total);
            Expect(warning.Total==37,"Independent private job tuple warning fixture failed.");
            void ReadOnly(Action action)
            {
                var bytes=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
                var privateBytes=privateDocument.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
                var revision=document.EditRevision;var privateRevision=privateDocument.EditRevision;var count=document.EditorChangeCount;
                var privateCount=privateDocument.EditorChangeCount;var history=document.GetHistory();var privateHistory=privateDocument.GetHistory();
                var dirty=document.HasChanges;var activity=document.ReadActivityStatistics()!.Encode();var notices=0;
                EventHandler observer=(_,_)=>notices++;document.Changed+=observer;
                try{action();}finally{document.Changed-=observer;}
                var after=document.GetHistory();var afterPrivate=privateDocument.GetHistory();
                Expect(bytes.All(item=>item.Key.Data.Span.SequenceEqual(item.Value))&&privateBytes.All(item=>item.Key.Data.Span.SequenceEqual(item.Value))
                    &&document.EditRevision==revision&&privateDocument.EditRevision==privateRevision&&document.EditorChangeCount==count
                    &&privateDocument.EditorChangeCount==privateCount&&notices==0&&document.HasChanges==dirty
                    &&history.Undo.SequenceEqual(after.Undo)&&history.Redo.SequenceEqual(after.Redo)
                    &&privateHistory.Undo.SequenceEqual(afterPrivate.Undo)&&privateHistory.Redo.SequenceEqual(afterPrivate.Redo)
                    &&document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity),"Job consent changed live/private bytes/history/activity/counters.");
            }
            foreach(var initial in new uint[]{0,0x10000,0x1000,0x2000,0xFFFFFFFF,0x80000000,0x80010000,0x12345678})
            foreach(var remember in new[]{false,true})foreach(var reply in new[]{"continue","cancel","close"})
            {
                using var policy=new CreatureJobsWarningPolicy(initial);var ownerCurrent=true;
                ReadOnly(()=>
                {
                    using var request=policy.Begin(new[]{warning},()=>ownerCurrent)!;
                    Expect(request.IsCurrent&&request.Warning==warning&&request.RequiresPrompt==((initial&0x10000)==0),"Request prompt/default/source payload mismatch.");
                    Reject(()=>policy.Begin(new[]{warning},()=>true));
                    if(!request.RequiresPrompt)
                    {Expect(request.Continue(remember)&&request.IsAccepted&&policy.Flags==initial,"Auto-Continue rewrote flags or failed consent.");}
                    else if(reply=="continue")
                    {Expect(request.Continue(remember)&&request.IsAccepted&&policy.Flags==((initial&~0x10000u)|(remember?0x10000u:0)),"Continue did not copy only checkbox/statebit.");}
                    else if(reply=="cancel")
                    {
                        Expect(request.CanCancel(remember)==!remember&&request.Cancel(remember)==!remember,"Checked remember did not disable Cancel.");
                        if(remember)request.Dispose();Expect(!request.IsAccepted&&policy.Flags==initial,"Cancel/close remembered checkbox.");
                    }
                    else{request.Dispose();Expect(!request.IsAccepted&&policy.Flags==initial,"Close changed flags or accepted.");}
                    Expect(request.IsClosed&&!request.IsCurrent,"Reply retained live request authority.");Reject(()=>request.Continue());
                });caseCount++;
            }
            using(var policy=new CreatureJobsWarningPolicy(0x80002000))
            {
                var ownerCurrent=true;
                ReadOnly(()=>
                {
                    using(var stale=policy.Begin(new[]{warning},()=>ownerCurrent)!){ownerCurrent=false;Expect(!stale.IsCurrent&&!stale.Cancel()&&!stale.CanCancel(false),"Stale owner retained Cancel authority.");Reject(()=>stale.Continue(true));}
                    Expect(policy.Flags==0x80002000,"Stale reply changed unrelated request bits.");ownerCurrent=true;
                    using(var reset=policy.Begin(new[]{warning},()=>ownerCurrent)!){policy.ResetSuppression();Expect(!reset.IsCurrent&&reset.IsClosed,"Reset retained old consent.");Reject(()=>reset.Continue(true));}
                    using(var accepted=policy.Begin(new[]{warning},()=>ownerCurrent)!){accepted.Continue(true);}
                    using(var reopened=policy.Begin(new[]{new CreatureGlobalsJobWarning(1,"Unhappy",29)},()=>ownerCurrent)!)
                    {Expect(!reopened.RequiresPrompt,"Reopened/new-map request lost process preference.");reopened.Continue();}
                    Expect(policy.Flags==0x80012000,"Map replacement/default reply rewrote unrelated bits.");
                    policy.ResetSuppression();Expect(policy.Flags==0x80002000&&!policy.Suppressed,"Explicit reset changed another request bit.");
                    using var restart=new CreatureJobsWarningPolicy();Expect(!restart.Suppressed&&restart.Flags==0,"Independent/restarted session inherited suppression.");
                    using var last=policy.Begin(new[]{warning},()=>ownerCurrent)!;policy.Dispose();Expect(!last.IsCurrent&&last.IsClosed,"Disposed policy retained active request.");Reject(()=>last.Continue());
                });
            }
            // Owner-plan and pending-row guards are evaluated again when Continue
            // is clicked. An unchanged consent creates no global override/Undo.
            using(var policy=new CreatureJobsWarningPolicy())
            {
                var plan=document.PrepareCreatureGlobals(session);var inputRevision=0;var captured=inputRevision;
                using var pending=policy.Begin(new[]{warning},()=>document.IsCreatureGlobalsPlanCurrent(plan)&&inputRevision==captured)!;
                ReadOnly(()=>{inputRevision++;Expect(!pending.IsCurrent,"Pending row edit retained consent.");Reject(()=>pending.Continue(true));});
                Expect(!policy.Suppressed,"Pending edit remembered stale choice.");
            }
        }
        Expect(caseCount==96,"Full request flag/reply/remember/formats matrix incomplete.");
        Console.WriteLine("SOURCE JOB REQUEST MATRIX: 96; native/project consent is read-only; UI/audio/source-C not executed.");
    }
}
