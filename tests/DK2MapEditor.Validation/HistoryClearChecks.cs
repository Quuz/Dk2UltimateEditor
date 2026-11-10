using DK2MapEditor.Core.Editing;

internal static class HistoryClearChecks
{
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static void Reject(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Active edit allowed history destruction.");}
    public static void Run()
    {
        var directory=Path.Combine(Path.GetTempPath(),"DK2HistoryClear-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
        foreach(var native in new[]{false,true})
        {
            var initial=NativeMapDocument.CreateNewMap(new("ClearHistory","Clear history",18,16,NativeFormat:native));
            var doc=NativeMapDocument.Load(initial.SaveCopy(Path.Combine(directory,"clean-"+native)));
            doc.SetHistoryDepth(37);var historyEvents=0;var changedEvents=0;
            doc.HistoryChanged+=(_,_)=>historyEvents++;doc.Changed+=(_,_)=>changedEvents++;
            doc.ClearHistory();Check(!doc.CanClearHistory&&historyEvents==0&&!doc.HasChanges&&doc.HistoryDepth==37,"Empty clear changed clean state or history settings.");
            doc.Edit("Parent",()=>{doc.Paint([new(3,3)],new(4,1,0));doc.Paint([new(4,3)],new(5,2,0));});
            doc.Paint([new(5,3)],new(6,3,0));doc.Undo();
            var savedHistory=doc.GetHistory();Check(savedHistory.Undo.Count==1&&savedHistory.Redo.Count==1&&savedHistory.SnapshotBytes>0&&doc.CanClearHistory,"Mixed-stack fixture is not meaningful.");
            var bytes=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=doc.EditRevision;var dirty=doc.HasChanges;var changed=changedEvents;var events=historyEvents;
            void LiveUnchanged()=>Check(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditRevision==revision&&doc.HasChanges==dirty
                &&doc.GetHistory().ChangeCount==savedHistory.ChangeCount&&doc.EditorChangeCount==savedHistory.EditorChangeCount&&doc.HistoryDepth==37&&changedEvents==changed,"Clear modified live bytes, counters, revision, dirty state or depth.");
            Reject(()=>doc.Edit("Rejected clear during edit",()=>{doc.Paint([new(6,4)],new(7,1,0));Check(!doc.CanClearHistory,"Clear enabled inside transaction.");doc.ClearHistory();}));
            LiveUnchanged();Check(doc.GetHistory().Undo.Count==1&&doc.GetHistory().Redo.Count==1&&historyEvents==events,"Rejected clear damaged retained stacks or emitted a history event.");
            doc.ClearHistory();LiveUnchanged();
            Check(!doc.CanUndo&&!doc.CanRedo&&!doc.CanClearHistory&&doc.GetHistory() is {SnapshotBytes:0,Undo.Count:0,Redo.Count:0}&&historyEvents==events+1,"Clear did not release both stacks with one history-only event.");
            Check(savedHistory.Undo.Count==1&&savedHistory.Redo.Count==1,"Clear mutated a previously returned immutable metadata snapshot.");
            doc.Undo();doc.Redo();doc.ClearHistory();LiveUnchanged();Check(historyEvents==events+1,"Empty replay/clear emitted an extra event.");
            doc.Paint([new(5,4)],new(8,1,0));Check(doc.CanUndo&&doc.GetHistory().ChangeCount==savedHistory.ChangeCount+1&&doc.EditorChangeCount==unchecked(savedHistory.EditorChangeCount+1),"Clear disabled future tracking or reset cumulative counters.");
            doc.Undo();Check(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.GetHistory().ChangeCount==savedHistory.ChangeCount&&doc.EditorChangeCount==savedHistory.EditorChangeCount,"Undo after clear crossed the discarded boundary or lost old counters.");
            Check(!doc.CanUndo&&doc.CanRedo&&doc.CanClearHistory,"Redo-only history cannot be cleared.");doc.ClearHistory();Check(!doc.CanRedo&&doc.HistoryDepth==37,"Redo-only clear retained an old step or disabled tracking.");
            doc.Paint([new(6,5)],new(9,2,0));
            using(var pen=doc.BeginLivePenStroke(new(4,3,0)))
            {
                pen.Start(new(7,5));var current=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;
                Check(!doc.CanClearHistory,"Clear enabled during live pen.");Reject(doc.ClearHistory);
                Check(current.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count,"Rejected clear rolled back or accepted an unfinished stroke.");
            }
            Check(doc.CanClearHistory&&doc.CanUndo,"Rejected live-pen clear lost the completed stroke.");doc.ClearHistory();
            doc.SetHistoryDepth(0);doc.Paint([new(8,5)],new(5,2,0));var zeroCount=doc.EditorChangeCount;
            doc.ClearHistory();Check(doc.HistoryDepth==0&&!doc.CanUndo&&!doc.CanRedo&&doc.EditorChangeCount==zeroCount,"Empty zero-depth clear enabled tracking or reset changes.");
            doc.SetHistoryDepth(37);doc.Paint([new(9,5)],new(6,1,0));
            var committed=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var countBefore=doc.EditorChangeCount;var revisionBefore=doc.EditRevision;
            EventHandler observer=(_,_)=>throw new InvalidOperationException("Observer after clear");doc.HistoryChanged+=observer;
            try{Reject(doc.ClearHistory);}finally{doc.HistoryChanged-=observer;}
            Check(!doc.CanUndo&&!doc.CanRedo&&doc.HistoryDepth==37&&committed.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==countBefore&&doc.EditRevision==revisionBefore,"Post-clear observer failure restored history, disabled tracking or modified data.");
            using var manager=new EditorObjectManagerHistory(doc);
            manager.Execute(()=>{doc.Paint([new(10,5)],new(7,1,0));return true;});
            Check(manager.CanUndo,"Manager did not own the last new step.");doc.ClearHistory();
            Check(!manager.CanUndo&&!manager.Undo(),"Manager retained authority over discarded history.");
            manager.Execute(()=>{doc.Paint([new(11,5)],new(8,1,0));return true;});
            Check(manager.CanUndo&&manager.Undo()&&!manager.CanUndo,"Manager new-command Undo crossed the clear boundary.");
        }
        }
        finally{Directory.Delete(directory,true);}
    }
}
