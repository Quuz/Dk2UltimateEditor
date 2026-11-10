using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class PlayerGoldNoticeChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException){return;}throw new InvalidOperationException("Unsafe player-gold operation accepted.");}
    private static NativeMapDocument New(bool native)
    {
        var d=NativeMapDocument.CreateNewMap(new("GoldNotice","GoldNotice",56,20,Players:5,NativeFormat:native));
        if(native)for(byte id=1;id<=7;id++)if(d.PlayerSettingsRows().All(row=>row.ID!=id))d.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="GAMEPLAYER"),new(0,0),id);
        d.SetMapVariables([new(0x18,100),new(0xB2,50)]);return d;
    }
    private static PlayerSettingsChange Change(NativeMapDocument d,uint id,int gold,string? name=null)
    {var row=d.PlayerSettingsRows().Single(row=>row.ID==id);return new(id,name??"Gold "+id,gold,row.Computer,row.Health is { } health?(int)health:null);}
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument d)=>d.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> before)=>Check(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Notice altered map bytes or Undo lost them.");
    private static void Rooms(NativeMapDocument d,byte owner,int x,bool heart)
    {
        d.Paint([new(x,2),new(x,3)],new(10,owner,1));
        if(heart)d.Paint(Enumerable.Range(7,5).SelectMany(y=>Enumerable.Range(x,5).Select(xx=>new TilePoint(xx,y))),new(14,owner,1));
    }
    public static void Run()
    {
        var cases=0;
        foreach(var native in new[]{false,true})
        {
            var d=New(native);for(byte id=1;id<=7;id++)Rooms(d,id,1+7*(id-1),true);
            for(uint id=1;id<=7;id++)
            {
                var capacity=d.CalculateEditorPlayerGoldCapacity(id);Check(capacity.Capacity==1000&&capacity.TreasuryTiles==2&&capacity.HeartPresent,"Source treasury/16-heart arithmetic differs.");
                foreach(var gold in new[]{1000,1001})
                {
                    var before=Snapshot(d);var history=d.GetHistory();var revision=d.EditRevision;var plan=d.PrepareEditorPlayerSettings(Change(d,id,gold));using var policy=new EditorPlayerGoldPolicy();
                    using var notice=policy.Begin(plan,()=>true);var expected=id>=3&&gold>1000;
                    Check((notice is not null)==expected&&plan.NeedsGoldNotice==expected,"Keeper-only/strict greater-than notice differs.");
                    if(notice is not null){Check(notice.RequiresNotice&&notice.IsCurrent,"New information not active.");notice.Dispose();Check(!notice.IsAcknowledged&&plan.IsCurrent&&policy.Flags==0,"Closing information became a veto or remembered reply.");}
                    Exact(before);Check(d.EditRevision==revision&&d.GetHistory().Undo.SequenceEqual(history.Undo),"Preparing/closing information allocated history.");
                    d.ApplyEditorPlayerSettings(plan);Check(plan.IsApplied&&!plan.IsCurrent&&d.PlayerSettingsRows().Single(row=>row.ID==id).Gold==gold,"Informational close cancelled or clamped Save.");
                    Reject(()=>d.ApplyEditorPlayerSettings(plan));d.Undo();Exact(before);cases++;
                }
            }
            Shapes(native);Session(native);Guards(native);HostedContext(native);Saved(native);
        }
        Console.WriteLine($"PLAYER GOLD NOTICE DETAIL {cases} native/project x seven players x equal/excess capacity matrices; source signed32, owned treasury/first heart16, OK-only close still saves without clamping, private prepare, once-only apply, session0x40/Undo/reset/cross-policy lease, stale/raw/working/callback/observer guards, save/reopen/compiler");
    }
    private static void RawVariable(NativeMapDocument d,uint id,int value)
    {
        var row=d.IsLegacyProject?d.Records().Single(record=>record.File.Signature==NativeMapDocument.LegacyInfo)
            :d.Records().Last(record=>record.Category=="GAMEGLOBALVARIABLE"&&record.File.Data.Span.Slice(record.Offset,4).SequenceEqual(BitConverter.GetBytes(id)));
        var field=row.Fields.Single(field=>field.Name==(d.IsLegacyProject?$"Variables[{id}]":"dwGlobalData"));
        Check(MemoryMarshal.TryGetArray(row.File.Data,out var raw),"Raw variable fixture missing.");
        field.Encode(d.IsLegacyProject?value.ToString(CultureInfo.InvariantCulture):unchecked((uint)value).ToString(CultureInfo.InvariantCulture)).CopyTo(raw.Array!,raw.Offset+row.Offset+field.Offset);
    }
    private static void Shapes(bool native)
    {
        var d=New(native);Rooms(d,3,2,false);Rooms(d,4,10,false);Check(d.CalculateEditorPlayerGoldCapacity(3).Capacity==200,"Other owners' treasuries counted.");
        d.Paint([new(1,5)],new(14,3,1));Rooms(d,3,20,true);
        Check(d.CalculateEditorPlayerGoldCapacity(3) is {Capacity:400,HeartPresent:false},"Source first incomplete heart incorrectly searched for a later valid one.");
        var wide=New(native);Rooms(wide,3,2,false);RawVariable(wide,0x18,int.MaxValue);
        Check(wide.CalculateEditorPlayerGoldCapacity(3).Capacity==-2,"Source signed LONG overflow was widened or clamped.");
        Check(wide.CalculateStatistics().Players.Single(player=>player.ID==3).TreasuryCapacity==2L*int.MaxValue,"Existing wide statistics were changed by source notice arithmetic.");
        Check(wide.PrepareEditorPlayerSettings(Change(wide,3,0)).NeedsGoldNotice,"Source zero gold versus wrapped negative capacity differed.");
        if(native)
        {
            var opaque=New(true);Rooms(opaque,3,2,true);var file=opaque.Files.First(file=>file.Signature==0xE6&&Path.GetExtension(file.ExportPath).Equals(".kld",StringComparison.OrdinalIgnoreCase));
            Check(MemoryMarshal.TryGetArray(file.Data,out var raw),"Opaque variable fixture missing.");BinaryPrimitives.WriteUInt32LittleEndian(raw.Array!.AsSpan(raw.Offset+4,4),99);
            var before=Snapshot(opaque);var plan=opaque.PrepareEditorPlayerSettings(Change(opaque,3,4000));
            Check(plan.Capacity.Capacity is null&&!plan.NeedsGoldNotice,"Unverified variable layout invented a capacity.");
            opaque.ApplyEditorPlayerSettings(plan);opaque.Undo();Exact(before);
        }
    }
    private static void Session(bool native)
    {
        var d=New(native);var shared=new EditorRequestPreferences();using var a=new EditorPlayerGoldPolicy(shared);using var b=new EditorPlayerGoldPolicy(shared);
        var plan=d.PrepareEditorPlayerSettings(Change(d,3,4000));var before=Snapshot(d);
        using(var notice=a.Begin(plan,()=>true)!)Check(notice.Acknowledge(true)&&notice.IsAcknowledged&&a.Flags==0x40,"Remembered OK failed.");
        d.ApplyEditorPlayerSettings(plan);d.Undo();Exact(before);Check(a.Flags==0x40,"Undo restored a process preference.");
        var other=New(native);var otherPlan=other.PrepareEditorPlayerSettings(Change(other,4,4000));
        using(var suppressed=b.Begin(otherPlan,()=>true)!)Check(!suppressed.RequiresNotice&&suppressed.Acknowledge(),"Suppression did not cross policy/document contexts.");
        using(var fresh=new EditorPlayerGoldPolicy())using(var notice=fresh.Begin(otherPlan,()=>true)!)Check(notice.RequiresNotice,"Fresh session inherited suppression.");
        using(var reset=new EditorPlayerGoldPolicy(new EditorRequestPreferences(0xF72)))
        {using var notice=reset.Begin(otherPlan,()=>true)!;reset.ResetSuppression();Check(notice.IsClosed&&reset.Flags==0xF32&&otherPlan.IsCurrent,"Reset changed other bits or vetoed the pending data plan.");}
        b.ResetSuppression();using(var asks=a.Begin(otherPlan,()=>true)!)Check(asks.RequiresNotice,"Shared reset did not propagate.");
        var row=d.PlayerSettingsRows().Single(row=>row.ID==3);var equal=d.PrepareEditorPlayerSettings(new(row.ID,row.Name,(int)row.Gold,row.Computer,row.Health is { } h?(int)h:null));var history=d.GetHistory();
        d.ApplyEditorPlayerSettings(equal);Check(equal.IsApplied&&d.GetHistory().Undo.SequenceEqual(history.Undo),"Generic equal Save history changed.");Reject(()=>d.ApplyEditorPlayerSettings(equal));
    }
    private static void Guards(bool native)
    {
        var d=New(native);var plan=d.PrepareEditorPlayerSettings(Change(d,3,4000));var before=Snapshot(d);var shared=new EditorRequestPreferences();using var policy=new EditorPlayerGoldPolicy(shared);
        Reject(()=>d.PrepareEditorPlayerSettings(Change(d,3,-1)));Reject(()=>d.PrepareEditorPlayerSettings(Change(d,3,0,new string('x',200))));
        Reject(()=>policy.Begin(plan,()=>false));Reject(()=>policy.Begin(plan,()=>{policy.ResetSuppression();return true;}));
        using(var active=policy.Begin(plan,()=>true)!)
        {
            using var big=new EditorLargeMapPolicy(shared);Reject(()=>big.BeginNew(new("GoldBusy","GoldBusy",121,121),()=>true));Check(active.IsCurrent,"Another caller stole the notice lease.");
        }
        using(var big=new EditorLargeMapPolicy(shared))using(var active=big.BeginNew(new("GoldBusy","GoldBusy",121,121),()=>true)!)
        {Reject(()=>policy.Begin(plan,()=>true));Reject(policy.ResetSuppression);Check(active.IsCurrent,"Gold reset stole another caller's lease.");}
        using(var active=policy.Begin(plan,()=>true)!)
        {
            Check(MemoryMarshal.TryGetArray(d.MainFile.Data,out var raw),"Raw source fixture missing.");raw.Array![raw.Offset+raw.Count-1]^=1;Check(!active.IsCurrent&&!plan.IsCurrent,"Raw edit left old notice/data plan current.");Reject(()=>active.Acknowledge(true));Reject(()=>d.ApplyEditorPlayerSettings(plan));raw.Array[raw.Offset+raw.Count-1]^=1;
        }
        using(var active=policy.Begin(plan,()=>true)!){d.Paint([new(2,2)],new(5,2,1));d.Undo();Check(!active.IsCurrent&&!plan.IsCurrent,"Undo revived stale settings.");Reject(()=>d.ApplyEditorPlayerSettings(plan));}
        Exact(before);Check(policy.Flags==0,"Stale notice remembered an answer.");
        using(var pen=d.BeginLivePenStroke(new(5,2,1)))Reject(()=>d.PrepareEditorPlayerSettings(Change(d,3,4000)));
        d.Edit("nested gold prepare",()=>Reject(()=>d.PrepareEditorPlayerSettings(Change(d,3,4000))));
        using(var parent=d.CreateMapPropertiesSession())
        {var privatePlan=parent.WorkingDocument.PrepareEditorPlayerSettings(Change(parent.WorkingDocument,3,4000));parent.WorkingDocument.Paint([new(2,2)],new(5,2,1));parent.WorkingDocument.Undo();Reject(()=>parent.WorkingDocument.ApplyEditorPlayerSettings(privatePlan));Exact(before);}
        var fresh=d.PrepareEditorPlayerSettings(Change(d,3,4000));EventHandler observer=(_,_)=>throw new InvalidOperationException("committed player observer");d.Changed+=observer;
        Reject(()=>d.ApplyEditorPlayerSettings(fresh));d.Changed-=observer;Check(fresh.IsApplied&&!fresh.IsCurrent,"Committed observer failure was misclassified.");d.Undo();Exact(before);
        var foreign=New(native);Reject(()=>foreign.ApplyEditorPlayerSettings(d.PrepareEditorPlayerSettings(Change(d,3,4000))));
    }
    private static void Saved(bool native)
    {
        var d=New(native);Rooms(d,3,2,true);var plan=d.PrepareEditorPlayerSettings(Change(d,3,999999));using var policy=new EditorPlayerGoldPolicy();
        using(var notice=policy.Begin(plan,()=>true)!)notice.Dispose();d.ApplyEditorPlayerSettings(plan);
        var directory=Path.Combine(Path.GetTempPath(),"DK2-GoldNotice-"+Guid.NewGuid().ToString("N"));var saved=d.SaveCopy(Path.Combine(directory,"source"));var opened=NativeMapDocument.Load(saved);
        Check(opened.PlayerSettingsRows().Single(row=>row.ID==3).Gold==999999,"Saved gold was clamped.");
        if(!native)opened=opened.CompileLegacyProject("GoldUnclamped",New(true));
        Check(opened.PlayerSettingsRows().Single(row=>row.ID==3).Gold==999999,"Source compiler clamped initial gold.");
    }
    private static void HostedContext(bool native)
    {
        var live=New(native);Rooms(live,3,2,true);var before=Snapshot(live);
        using var parent=live.CreateMapPropertiesSession();var working=parent.WorkingDocument;
        working.SetMapVariables([new(0x18,1000),new(0xB2,500)]);
        Check(working.CalculateEditorPlayerGoldCapacity(3).Capacity==10000,"Private capacity fixture differs.");
        var plan=working.PrepareEditorPlayerSettings(Change(working,3,4000),live);
        Check(plan.Capacity.Capacity==1000&&plan.NeedsGoldNotice,"Hosted notice read private variables instead of source global map context.");
        using(var policy=new EditorPlayerGoldPolicy())using(var notice=policy.Begin(plan,()=>true)!)notice.Dispose();
        working.ApplyEditorPlayerSettings(plan);Exact(before);Check(working.PlayerSettingsRows().Single(row=>row.ID==3).Gold==4000,"Hosted Save wrote wrong document.");
        var stale=working.PrepareEditorPlayerSettings(Change(working,3,5000),live);
        live.SetMapVariables([new(0x18,101)]);live.Undo();Check(!stale.IsCurrent,"Restored live capacity source revived old private Save.");Reject(()=>working.ApplyEditorPlayerSettings(stale));Exact(before);
        Console.WriteLine($"PLAYER GOLD HOST CONTEXT {native} source global capacity versus private target, separate variables, no live writes and dual-revision guards");
    }
}
