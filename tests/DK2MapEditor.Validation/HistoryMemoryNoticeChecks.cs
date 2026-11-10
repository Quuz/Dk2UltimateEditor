using System.Reflection;
using DK2MapEditor.Core.Editing;

internal static class HistoryMemoryNoticeChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Refuse(Action run){try{run();}catch(InvalidOperationException){return;}throw new Exception("Unsafe memory notice accepted.");}
    private static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new("MemoryNotice","MemoryNotice",16,14,NativeFormat:native));
    private static EditorHistoryMemoryException Fail(NativeMapDocument doc)
    {
        var field=typeof(NativeMapDocument).GetField("historyBufferCopy",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var previous=field.GetValue(doc);var failure=new OutOfMemoryException("Injected history allocation");var invoked=false;
        field.SetValue(doc,new Func<byte[],byte[]>(_=>throw failure));
        try
        {
            doc.Edit("Allocation failure",()=>invoked=true);throw new Exception("Expected typed history failure.");
        }
        catch(EditorHistoryMemoryException error){Check(!invoked&&ReferenceEquals(error.InnerException,failure),"Lost failure identity or ran a refused action.");return error;}
        finally{field.SetValue(doc,previous);}
    }
    public static void Run()
    {
        foreach(var native in new[]{false,true})
        {
            var doc=New(native);var failure=Fail(doc);Check(failure.IsSafeToNotify,"Finished failure remained unsafe.");
            var before=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var history=doc.GetHistory();var revision=doc.EditRevision;
            var prefs=new EditorRequestPreferences(0x8000);using var policy=new EditorHistoryMemoryPolicy(prefs);using var sibling=new EditorHistoryMemoryPolicy(prefs);
            using(var notice=policy.Begin(failure,()=>true)){notice.Dispose();Check(!notice.IsAcknowledged&&prefs.Flags==0x8000,"Close remembered a reply.");}
            using(var notice=policy.Begin(failure,()=>true)){Check(notice.RequiresNotice&&notice.Acknowledge(),"Ordinary OK failed.");}
            Check(prefs.Flags==0x8000,"Ordinary OK suppressed future notices.");
            using(var notice=policy.Begin(failure,()=>true)){Refuse(()=>sibling.Begin(failure,()=>true));Refuse(sibling.ResetSuppression);Check(notice.Acknowledge(true),"Remembered OK failed.");}
            using(var notice=sibling.Begin(failure,()=>true)){Check(!notice.RequiresNotice&&notice.Acknowledge(),"Shared source auto reply failed.");}
            Check(prefs.Flags==0x8008,"Automatic OK reset a remembered bit.");
            Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditRevision==revision&&doc.GetHistory().Undo.SequenceEqual(history.Undo)
                &&doc.GetHistory().Redo.SequenceEqual(history.Redo)&&doc.GetHistory().EditorChangeCount==history.EditorChangeCount,"Informational reply changed map/history/counters.");
            policy.ResetSuppression();Check(prefs.Flags==0x8000,"Reset changed a different preference.");
            using(var notice=policy.Begin(failure,()=>true)){doc.Paint([new(2,2)],new(4,2,1));Check(!notice.IsCurrent&&!notice.Acknowledge(true)&&prefs.Flags==0x8000,"Stale OK remembered a bit.");}
            doc.Undo();
            Refuse(()=>policy.Begin(failure,()=>false));
            try{policy.Begin(failure,()=>throw new ApplicationException("Owner failure"));throw new Exception("Unexpected owner acceptance.");}catch(ApplicationException){ }
            using(var notice=policy.Begin(failure,()=>true)){notice.Acknowledge(true);}doc.Redo();doc.Undo();Check((prefs.Flags&8)!=0,"Undo/Redo erased runtime suppression.");policy.ResetSuppression();
            // Error notification does not steal or approve an existing normal
            // confirmation lease. It blocks new commands until it closes.
            using var large=new EditorLargeMapPolicy(prefs);
            using(var confirmation=large.BeginNew(new("Large","Large",301,301),()=>true)!)
            {
                using(var notice=policy.Begin(failure,()=>true))
                {
                    Refuse(()=>sibling.Begin(failure,()=>true));
                    using var other=new EditorLargeMapPolicy(prefs);Refuse(()=>other.BeginNew(new("Other","Other",301,301),()=>true));
                    Check(confirmation.IsCurrent&&!confirmation.IsAccepted,"Error notice changed normal consent.");notice.Acknowledge(true);
                }
                Check(confirmation.IsCurrent&&!confirmation.IsAccepted&&confirmation.Decline(),"Notification lost the original lease.");
            }
            policy.ResetSuppression();
            EditorHistoryMemoryException? deferred=null;
            doc.Edit("Outer operation",()=>{deferred=Fail(doc);Check(!deferred.IsSafeToNotify,"Notice became safe inside an open transaction.");Refuse(()=>policy.Begin(deferred,()=>true));});
            Check(deferred!.IsSafeToNotify,"Notice never became safe after unwinding.");
            using(var notice=policy.Begin(deferred,()=>true)){notice.Dispose();}
            using var disposed=new EditorHistoryMemoryPolicy();disposed.Dispose();Refuse(()=>disposed.Begin(failure,()=>true));
            Console.WriteLine($"HISTORY MEMORY NOTICE DETAIL native={native} typed inner failure post-unwind OK Close default-only remember reset readonly stale owner nested lease and outside Undo");
        }
    }
}
