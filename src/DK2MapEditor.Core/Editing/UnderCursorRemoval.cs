namespace DK2MapEditor.Core.Editing;

public enum UnderCursorFamily { Creatures, DoorsAndTraps, ObjectsAndSpecials }

public sealed class UnderCursorRemovalPlan
{
    internal UnderCursorRemovalPlan(NativeMapDocument owner,TilePoint position,UnderCursorFamily preferred,UnderCursorFamily family,IReadOnlyList<NativeRecord> records,byte[] fingerprint,long revision)
    {Owner=owner;Position=position;Preferred=preferred;Family=family;Records=records;Fingerprint=fingerprint;Revision=revision;}
    internal NativeMapDocument Owner {get;}
    internal byte[] Fingerprint {get;}
    internal long Revision {get;}
    public TilePoint Position {get;}
    public UnderCursorFamily Preferred {get;}
    public UnderCursorFamily Family {get;}
    public IReadOnlyList<NativeRecord> Records {get;}
    public bool IsCurrent=>Owner.IsUnderCursorRemovalCurrent(this);
}

public sealed partial class NativeMapDocument
{
    private bool deferCursorNotifications;
    public static UnderCursorFamily? UnderCursorPreference(bool shift,bool control)=>shift&&control?null:shift?UnderCursorFamily.DoorsAndTraps:control?UnderCursorFamily.ObjectsAndSpecials:UnderCursorFamily.Creatures;
    public UnderCursorRemovalPlan PlanUnderCursorRemoval(TilePoint position,UnderCursorFamily preferred)
    {
        if(IsSaveLocked)throw new InvalidOperationException("Finish the current edit before removing under the cursor.");
        if(!Enum.IsDefined(preferred))throw new ArgumentOutOfRangeException(nameof(preferred));CheckPoint(position.X,position.Y);
        var fingerprint=RegionRemovalFingerprint();var revision=EditRevision;
        static UnderCursorFamily Family(PlacedThingKind kind)=>kind==PlacedThingKind.Creature?UnderCursorFamily.Creatures:kind is PlacedThingKind.Door or PlacedThingKind.Trap?UnderCursorFamily.DoorsAndTraps:UnderCursorFamily.ObjectsAndSpecials;
        var entries=ObjectManagerEntries().Where(entry=>entry.Thing.Position==position).ToArray();
        var occupied=entries.Select(entry=>Family(entry.Thing.Kind)).Distinct().ToArray();
        var family=occupied.Length==1?occupied[0]:preferred;
        var chosen=entries.Where(entry=>Family(entry.Thing.Kind)==family).OrderBy(entry=>entry.Thing.Kind is PlacedThingKind.Trap or PlacedThingKind.SpecialItem?1:0).Select(entry=>entry.Record).ToArray();
        var records=chosen.Length==0?Array.AsReadOnly(chosen):SnapshotThingRequestSelection(chosen);
        if(chosen.Length!=0)RequireKnownEntityReferences(Records());
        if(revision!=EditRevision || !fingerprint.AsSpan().SequenceEqual(RegionRemovalFingerprint()))throw new InvalidOperationException("The objects changed while preparing cursor removal.");
        return new(this,position,preferred,family,records,fingerprint,revision);
    }
    internal bool IsUnderCursorRemovalCurrent(UnderCursorRemovalPlan plan)
    {
        if(plan.Owner!=this || plan.Revision!=EditRevision || IsSaveLocked || !plan.Fingerprint.AsSpan().SequenceEqual(RegionRemovalFingerprint()))return false;
        var current=PlanUnderCursorRemoval(plan.Position,plan.Preferred);
        return current.Family==plan.Family && current.Records.Count==plan.Records.Count && current.Records.Select((row,index)=>SameRecord(row,plan.Records[index]) && row.Category==plan.Records[index].Category && row.Fields.SequenceEqual(plan.Records[index].Fields)).All(value=>value);
    }
    private void RemoveCursorEntries(IReadOnlyList<NativeRecord> selected)
    {
        var removed=new List<NativeRecord>();
        foreach(var original in selected)
        {
            var offset=original.Offset-removed.Where(row=>row.File==original.File && row.Offset<original.Offset).Sum(row=>row.Length);
            var row=Records().Single(row=>row.File==original.File && row.Offset==offset && row.Length==original.Length && row.Category==original.Category);
            Edit("Delete object under cursor",()=>ApplyObjectRemovalCore(PlanObjectRemoval([row]),true,editorCaller:true));
            removed.Add(original);
        }
    }
    internal int ApplyUnderCursorRemoval(EditorThingRequest request,IReadOnlyList<NativeRecord> selected)
    {
        if(!IsThingRequestCurrent(request) || request.CursorPlan is not {IsCurrent:true} || deferCursorNotifications)
            throw new InvalidOperationException("Refresh the objects under the cursor before removing them.");
        if(selected.Count==0)return 0;
        _=checked(changeCount+selected.Count);_=checked(EditRevision+selected.Count);RequireKnownEntityReferences(Records());
        var copy=ClonePropertiesDocument();copy.Document.SetHistoryDepth(0);
        try{copy.Document.RemoveCursorEntries(selected.Select(row=>row with{File=copy.Files[row.File]}).ToArray());}
        finally{copy.Document.activityStatistics?.Dispose();}
        var before=TransactionFiles().ToDictionary(file=>file,file=>file.Data.ToArray());
        var priorUndo=undo.ToArray();var priorRedo=redo.ToArray();var priorBytes=historyBytes;var priorCount=changeCount;var priorEditor=editorChangeCount;var priorRevision=EditRevision;
        deferCursorNotifications=true;
        try{RemoveCursorEntries(selected);}
        catch
        {
            foreach(var pair in before)pair.Key.Bytes=pair.Value;
            undo.Clear();undo.AddRange(priorUndo);redo.Clear();redo.AddRange(priorRedo);historyBytes=priorBytes;changeCount=priorCount;editorChangeCount=priorEditor;EditRevision=priorRevision;throw;
        }
        finally{deferCursorNotifications=false;}
        // Publish one refresh after all original per-object Undo steps exist.
        // Observer failures are postcommit failures, never partial acceptance.
        HistoryChanged?.Invoke(this,EventArgs.Empty);Changed?.Invoke(this,EventArgs.Empty);
        return selected.Count;
    }
}
