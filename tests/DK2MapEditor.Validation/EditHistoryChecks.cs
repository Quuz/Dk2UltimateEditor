using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static class EditHistoryChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { return; }
        throw new Exception("Invalid history operation accepted.");
    }
    public static void Run()
    {
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Source"))) root = Path.GetDirectoryName(root) ?? throw new Exception("Source missing.");
        var header = File.ReadAllText(Path.Combine(root,"Source/Includes/Undo.h"));
        var source = File.ReadAllText(Path.Combine(root,"Source/Sources/Map/Map_Undo.c"));
        Expect(Regex.IsMatch(header, @"#define\s+UNDO_LEVELS\s+200\b") && NativeMapDocument.DefaultHistoryDepth == 200, "Original history default mismatch.");
        Expect(source.Contains("MessageBox.min = 0;") && source.Contains("MessageBox.max = 65535;")
            && NativeMapDocument.MaximumHistoryDepth == 65535 && source.Contains("Undo_HistoryBuildList(hList,Undo->childs.next,hItem)"), "Original range/hierarchy evidence changed.");
        var directory = Path.Combine(Path.GetTempPath(),"DK2MapEditor.History",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try { foreach (var native in new[] { false,true }) Check(native,directory); CheckBudget(directory); }
        finally { Directory.Delete(directory,true); }
    }
    private static void Check(bool native, string directory)
    {
        var document = NativeMapDocument.CreateNewMap(new("History","History",20,20,NativeFormat:native));
        var before = document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        void Exact() => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)),"History operation changed baseline bytes.");
        var changed = 0; var historyChanged = 0; document.Changed += (_,_) => changed++; document.HistoryChanged += (_,_) => historyChanged++;
        Expect(document.GetHistory() is { ChangeCount:0,SnapshotBytes:0,Depth:200 } && !document.CanUndo && !document.CanRedo,"New document has initialization history.");
        document.Edit("Paste tiles, things and scripts", () =>
        {
            document.Paint([new(2,2)],new(4,1,0));
            document.Edit("Change global definitions", () =>
            {
                document.Paint([new(3,3)],new(5,2,0));
                document.Paint([new(3,3)],new(5,2,0),"No-op child");
            });
        });
        var history = document.GetHistory(); var operation = history.Undo.Single();
        var expectedCost = document.Files.Where(file => !file.Data.Span.SequenceEqual(before[file])).Sum(file => (long)before[file].Length + file.Length);
        Expect(history.ChangeCount == 1 && history.SnapshotBytes == expectedCost && changed == 1 && historyChanged == 1
            && operation.Description == "Paste tiles, things and scripts" && operation.Children.Count == 2
            && operation.Children[1].Children.Single().Description == "Paint", "Atomic nested history or exact snapshot accounting wrong.");
        var after = document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        document.Undo(); Exact(); Expect(document.GetHistory().ChangeCount == 0 && document.GetHistory().Redo.Single() == operation,"Undo lost hierarchy/count.");
        var calls = historyChanged;
        document.Paint([new(2,2)],document.GetTile(2,2)); Expect(historyChanged == calls && document.CanRedo,"No-op cleared redo/history.");
        Reject(() => document.Edit("Failed", () => { document.Paint([new(4,4)],new(6,3,0)); document.SetHistoryDepth(0); }));
        Exact(); Expect(document.CanRedo && historyChanged == calls && document.GetHistory().SnapshotBytes == expectedCost,"Failed transaction retained history/data/settings changes.");
        Reject(() => document.Edit("Undo inside edit",document.Undo)); Reject(() => document.Edit("Redo inside edit",document.Redo)); Exact();
        document.Redo(); Expect(after.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && document.GetHistory().ChangeCount == 1,"Redo not byte exact.");
        var saved = document.SaveCopy(Path.Combine(directory,"nested-"+native)); var reopened = NativeMapDocument.Load(saved);
        Expect(reopened.GetHistory() is { ChangeCount:0,SnapshotBytes:0,Depth:200 } && !reopened.CanUndo,"Undo metadata leaked into map files.");
        Expect(reopened.GetTile(2,2) == new MapTile(4,1,0) && reopened.GetTile(3,3) == new MapTile(5,2,0),"Saved edits lost.");
        document.Undo(); document.Paint([new(6,6)],new(6,3,0)); Expect(!document.CanRedo && document.GetHistory().Undo.Single().Children.Count == 0,"Branch retained old redo or nested trace.");
        document.Undo(); Exact();
        document.SetHistoryDepth(0); Expect(!document.CanRedo && document.GetHistory().SnapshotBytes == 0,"Zero did not clear redo.");
        var baselineCalls = changed;
        document.Paint([new(7,7)],new(7,1,0)); Expect(!document.CanUndo && !document.CanRedo && document.GetHistory().ChangeCount == 1 && changed == baselineCalls+1,"Disabled history blocked editing/count.");
        document.Undo(); Expect(document.GetTile(7,7) == new MapTile(7,1,0),"Disabled Undo changed data.");
        document.SetHistoryDepth(200);
        MapTile third = default;
        for(var index=1;index<=203;index++) { document.Paint([new(8,8)],new((byte)(index%2+4),1,0),"Step "+index); if(index==3) third=document.GetTile(8,8); }
        history = document.GetHistory(); Expect(history.Undo.Count==200 && history.Undo[0].Description=="Step 4" && history.ChangeCount==204,"Default depth not enforced.");
        for(var index=0;index<200;index++) document.Undo();
        Expect(document.GetTile(8,8)==third && document.GetHistory().ChangeCount==4 && !document.CanUndo && document.GetHistory().Redo[0].Description=="Step 4","Trimmed Undo crossed retained boundary.");
        document.SetHistoryDepth(2); Expect(document.GetHistory().Redo.Count==2 && document.GetHistory().Redo[0].Description=="Step 4","Lower depth retained distant redo instead of next steps.");
        document.Redo(); document.SetHistoryDepth(1); Expect(!document.CanUndo && document.CanRedo,"Mixed stacks did not respect total depth.");
        document.Redo(); Expect(document.GetHistory().Undo.Single().Description=="Step 5","Retained redo order wrong.");
        document.SetHistoryDepth(65535); var immutable = document.GetHistory();
        Reject(()=>document.SetHistoryDepth(-1)); Reject(()=>document.SetHistoryDepth(65536));
        Expect(document.HistoryDepth==65535 && immutable.Undo.Count==1,"Invalid settings or snapshot mutated.");
        if(immutable.Undo is IList<HistoryOperation> list) { try { list.Clear(); throw new Exception("Mutable history leaked."); } catch(NotSupportedException){} }
        document.SetHistoryDepth(0); Expect(immutable.Undo.Count==1 && document.GetHistory().SnapshotBytes==0,"Previously returned snapshot changed.");
        var sameCalls=historyChanged; document.SetHistoryDepth(0); Expect(sameCalls==historyChanged,"Identical depth emitted event.");
    }
    private static void CheckBudget(string directory)
    {
        var document = NativeMapDocument.Create(1024,1024,"HistoryBudget");
        for(var index=0;index<10;index++) document.Paint([new(2,2)],new((byte)(index%2+4),1,0));
        var history=document.GetHistory(); var perEntry=2L*document.TileFile.Length;
        Expect(history.SnapshotBytes==history.Undo.Count*perEntry && history.SnapshotBytes<=64L*1024*1024
            && history.Undo.Count==(64L*1024*1024)/perEntry,"Snapshot byte budget not enforced exactly.");
        // Opaque native tile tails are legal and must remain byte exact. This
        // one operation exceeds the budget but still needs atomic Undo.
        document = NativeMapDocument.Create(4,4,"Oversized");
        var saved=document.SaveCopy(Path.Combine(directory,"opaque-source"));
        var tilePath=Path.Combine(Path.GetDirectoryName(saved)!,"OversizedMap.kld");
        Expect(File.Exists(tilePath),"Synthetic tile path missing.");
        using(var stream=new FileStream(tilePath,FileMode.Open,FileAccess.Write)) stream.SetLength(34L*1024*1024);
        document=NativeMapDocument.Load(saved); var original=document.TileFile.Data.ToArray();
        document.Paint([new(2,2)],new(4,1,0)); Expect(document.GetHistory().Undo.Count==1 && document.GetHistory().SnapshotBytes>64L*1024*1024,"Oversized atomic operation was discarded.");
        document.Undo(); Expect(document.TileFile.Data.Span.SequenceEqual(original),"Oversized Undo lost opaque bytes.");
        document.SetHistoryDepth(0); Expect(document.GetHistory().SnapshotBytes==0,"Zero did not release oversized snapshots.");
    }
}
