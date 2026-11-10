using System.Reflection;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class Program
{
    private static int groups;
    private static string directory="";
    private static readonly EditorHeroMemberValues Hero=new(17);
    private static readonly EditorHeroMemberValues Leader=Hero with {Options2=43};
    private static int Main(string[] args)
    {
        if(args.Length!=1 || Directory.Exists(args[0]))throw new ArgumentException("Require a fresh private artifact directory.");
        directory=args[0];Directory.CreateDirectory(directory);
        try
        {
            foreach(var native in new[]{false,true})
            {
                Preview(native);Limits(native);Nested(native);ReplaceRemove(native);Equal(native);
                Guards(native);Overflow(native);NoHistory(native);Save(native);BandCapacity(native);DeleteBand(native);
            }
            Orphan();Layouts();Check(groups==24,"Expected twenty-four model groups.");
            Console.WriteLine("EDITOR HERO BAND MODEL CHECKS PASSED: 24");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
    }
    private static NativeMapDocument Fixture(bool native)=>NativeMapDocument.CreateNewMap(new("HeroBand","HeroBand",16,16,NativeFormat:native));
    private static string Format(bool native)=>native?"native":"project";
    private static NativeRecord Band(NativeMapDocument d)
    {
        using var s=d.CreateEditorHeroBandSession();s.Add(Leader);s.Add(Hero);
        if(s.HasStoredName)s.SetName("Bande de héros n°0");
        return d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(s,"Band "+s.ID));
    }
    private static Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)
    {foreach(var pair in bytes)Check(pair.Key.Data.Span.SequenceEqual(pair.Value),"Unexpected live byte change: "+pair.Key.ExportPath);}
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {
        Exception? error=null;try{action();}catch(Exception e)when(e is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or OverflowException){error=e;}
        Check(error is not null,"Expected rejection.");
    }
    private static void Pass(bool native,string message){groups++;Console.WriteLine("PASS "+Format(native)+" "+message);}
    private static void Accepted(NativeMapDocument d,EditHistorySnapshot before,long revision)
    {
        var after=d.GetHistory();Check(after.ChangeCount==before.ChangeCount+1 && after.EditorChangeCount==unchecked(before.EditorChangeCount+1)
            && after.Undo.Count==before.Undo.Count+1 && after.Redo.Count==0 && d.EditRevision==revision+1,"Parent Save must own one Undo and one count.");
    }
    private static void Preview(bool native)
    {
        var d=Fixture(native);var bytes=Bytes(d);var h=d.GetHistory();var rev=d.EditRevision;
        using(var s=d.CreateEditorHeroBandSession())
        {
            Check(s.IsNew && s.ID==0,"First source band ID is zero.");Reject(()=>d.PrepareEditorHeroBand(s,"Band 0"));
            s.Add(Hero);Reject(()=>d.PrepareEditorHeroBand(s,"Band 0"));
            s.Replace([0],Leader);s.Add(Leader);Reject(()=>d.PrepareEditorHeroBand(s,"Band 0"));
            s.Remove([1]);var p=d.PrepareEditorHeroBand(s,"Band 0");Exact(bytes);
        }
        Exact(bytes);Check(d.GetHistory().ChangeCount==h.ChangeCount && d.EditRevision==rev,"Preview/Cancel leaked history.");
        Pass(native,"private New/Cancel, ID0, empty/no-leader/two-leader Save refusals");
    }
    private static void Limits(bool native)
    {
        var d=Fixture(native);using var s=d.CreateEditorHeroBandSession();s.Add(Leader);
        for(var i=1;i<16;i++)s.Add(Hero);
        Check(s.Members.Count==16 && s.Members.All(m=>m.ScenarioIdentity==0) && s.Members.Skip(1).All(m=>m.Values==Hero),"Source defaults/identity deferral.");
        Reject(()=>s.Add(Hero));Reject(()=>s.Replace([0],Leader with{Level=11}));
        Reject(()=>s.Replace([0],Leader with{GoldHeld=101}));Reject(()=>s.Replace([0],Leader with{Health=101}));
        Check(s.Members[0].Values==Leader && s.Members.Count==16,"Refused member change leaked partial fields.");
        _=d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(s,"Band 0"));using var next=d.CreateEditorHeroBandSession();Check(next.ID==1,"Next source free band ID.");
        Pass(native,"sixteen-slot limit, member defaults/UID0, atomic ranges and next free ID");
    }
    private static void Nested(bool native)
    {
        var d=Fixture(native);var band=Band(d);var before=Bytes(d);var h=d.GetHistory();var revision=d.EditRevision;
        using var s=d.CreateEditorHeroBandSession(band);
        using(var child=s.CreateMemberDraft(1)){child.Values=Hero with{Level=7,Enemy=0,GoldHeld=25};}
        Check(s.Members[1].Values==Hero,"Nested Cancel changed parent.");
        var accepted=s.CreateMemberDraft(1);accepted.Values=Hero with{Level=8,Enemy=0,GoldHeld=40};s.ApplyMemberDraft(accepted);
        Check(accepted.Closed && s.Members[1].Values.Level==8,"Nested Apply lost private values.");Exact(before);
        var result=d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(s,"Band 0"));Accepted(d,h,revision);var after=Bytes(d);
        d.Undo();Exact(before);Check(d.EditorChangeCount==h.EditorChangeCount,"Undo failed source count.");
        d.Redo();Exact(after);Check(d.BandMembers(result)[1].Level==8,"Redo lost member payload.");
        Pass(native,"nested member Cancel/Apply remains private, whole-parent Save/Undo/Redo");
    }
    private static void ReplaceRemove(bool native)
    {
        var d=Fixture(native);var band=Band(d);var bytes=Bytes(d);
        using var s=d.CreateEditorHeroBandSession(band);s.Add(Hero with{Level=4});
        s.Replace([1,2],Hero with{Level=6,Enemy=7,Health=90,GoldHeld=70});
        s.Remove([1]);Check(s.Members.Count==2 && s.Members[1].Values.Level==6 && s.Members[0].IsLeader,"Compaction/replace order.");
        Reject(()=>s.Remove([16]));Reject(()=>s.Replace([],Hero));Exact(bytes);
        _=d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(s,"Band 0"));d.Undo();Exact(bytes);
        Pass(native,"multi-member Replace/Remove, stable order, safe compaction, atomic Undo");
    }
    private static void Equal(bool native)
    {
        var d=Fixture(native);var band=Band(d);
        Check(MemoryMarshal.TryGetArray(band.File.Data,out var padding),"Fixture raw band payload.");
        var opaque=native?band.Offset+12:band.Offset+band.Fields.Single(f=>f.Name=="name").Offset+63;
        padding.Array![padding.Offset+opaque]=0xA7;
        d.Paint([new(5,5)],new(5,2,1));d.Undo();Check(d.CanRedo,"Fixture needs pending redo.");
        var bytes=Bytes(d);var h=d.GetHistory();var revision=d.EditRevision;
        using var s=d.CreateEditorHeroBandSession(band);if(s.HasStoredName)s.SetName(s.Name);var p=d.PrepareEditorHeroBand(s,"Band 0");
        _=d.ApplyEditorHeroBand(p);Exact(bytes);Accepted(d,h,revision);Check(!d.CanRedo,"Equal source parent Save must clear obsolete redo.");
        d.Undo();Exact(bytes);Check(d.EditorChangeCount==h.EditorChangeCount,"Equal own Undo must restore source counter.");
        d.Redo();Exact(bytes);Check(d.EditorChangeCount==unchecked(h.EditorChangeCount+1),"Equal Redo count.");
        var unchanged=d.GetHistory();d.Edit("Generic no-op",()=>{});Check(d.GetHistory().Undo.Count==unchanged.Undo.Count && d.GetHistory().ChangeCount==unchanged.ChangeCount,"Generic no-op contract changed.");
        Pass(native,"equal accepted Save owns Undo, clears redo, reversible count; generic no-op retained");
    }
    private static void Guards(bool native)
    {
        var d=Fixture(native);var band=Band(d);using var s=d.CreateEditorHeroBandSession(band);
        var child=s.CreateMemberDraft(1);s.Add(Hero);Reject(()=>s.ApplyMemberDraft(child));Check(!child.Closed,"Refused stale child consumed.");
        var p=d.PrepareEditorHeroBand(s,"Band 0");s.Replace([1],Hero with{Level=5});Reject(()=>d.ApplyEditorHeroBand(p));Check(!s.Closed,"Refused stale plan consumed.");
        p=d.PrepareEditorHeroBand(s,"Band 0");Reject(()=>Fixture(native).ApplyEditorHeroBand(p));
        var before=Bytes(d);d.Paint([new(5,5)],new(5,2,1));var changed=Bytes(d);Reject(()=>d.ApplyEditorHeroBand(p));Exact(changed);
        using var closed=d.CreateEditorHeroBandSession(band);closed.Dispose();Reject(()=>closed.Add(Hero));
        d.Undo();Exact(before);Pass(native,"stale nested child, stale/foreign parent plan, live mutation and closed session guards");
    }
    private static void Overflow(bool native)
    {
        foreach(var field in new[]{"changeCount","<EditRevision>k__BackingField"})
        {
            var d=Fixture(native);using var s=d.CreateEditorHeroBandSession();s.Add(Leader);var p=d.PrepareEditorHeroBand(s,"Band 0");var bytes=Bytes(d);
            var f=typeof(NativeMapDocument).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)!;var old=f.GetValue(d);f.SetValue(d,long.MaxValue);
            if(field=="<EditRevision>k__BackingField")typeof(EditorHeroBandSession).GetField("<Revision>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(s,long.MaxValue);
            Reject(()=>d.ApplyEditorHeroBand(p));Exact(bytes);Check(!s.Closed,"Overflow consumed private session.");f.SetValue(d,old);
            if(field=="<EditRevision>k__BackingField")typeof(EditorHeroBandSession).GetField("<Revision>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(s,old);
            _=d.ApplyEditorHeroBand(p);
        }
        var wrap=Fixture(native);typeof(NativeMapDocument).GetField("editorChangeCount",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(wrap,uint.MaxValue);
        using var s2=wrap.CreateEditorHeroBandSession();s2.Add(Leader);_=wrap.ApplyEditorHeroBand(wrap.PrepareEditorHeroBand(s2,"Band 0"));
        Check(wrap.EditorChangeCount==0,"Source uint counter must wrap.");wrap.Undo();Check(wrap.EditorChangeCount==uint.MaxValue,"Undo restores wrapped source counter.");
        Pass(native,"modern overflow atomic/refused session retained, source32-bit wrap and Undo");
    }
    private static void NoHistory(bool native)
    {
        var d=Fixture(native);var band=Band(d);d.SetHistoryDepth(0);var h=d.GetHistory();
        using var s=d.CreateEditorHeroBandSession(band);_=d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(s,"Band 0"));
        Check(!d.CanUndo && d.GetHistory().ChangeCount==h.ChangeCount+1 && d.EditorChangeCount==unchecked(h.EditorChangeCount+1),"History0 accepted Save must still count.");
        Pass(native,"history disabled retains accepted Save counts without allocating history");
    }
    private static void Save(bool native)
    {
        var d=Fixture(native);var band=Band(d);using var s=d.CreateEditorHeroBandSession(band);
        s.Replace([1],Hero with{Level=10,Enemy=0,GoldHeld=100,Objective=23,Area=0});
        band=d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(s,"Band 0"));var expected=band.File.Data.Slice(band.Offset,band.Length).ToArray();
        var path=d.SaveCopy(Path.Combine(directory,"saved-"+Format(native)));var reopened=NativeMapDocument.Load(path);
        var copy=reopened.Records().Single(NativeMapDocument.IsBand);Check(copy.File.Data.Span.Slice(copy.Offset,copy.Length).SequenceEqual(expected),"Actual saved/reopened complete band payload differs.");
        Check(!reopened.HasChanges,"Saved reopened map is dirty.");Pass(native,"actual private Save/reopen preserves complete band payload and clean state");
    }
    private static void Orphan()
    {
        var d=Fixture(false);var band=Band(d);var owner=d.ScriptOwners().Single(o=>o.Scope==16 && o.Record.File==band.File && o.Record.Offset==band.Offset && o.MemberIndex==1);
        using(var point=d.CreateEditorScenarioPointSession(initialScope:16)){point.SelectOwner(owner);_=d.ApplyEditorScenarioPoint(d.PrepareEditorScenarioPoint(point));}
        var script=d.Files.Single(f=>f.Signature==NativeMapDocument.LegacyEvents);var scriptBytes=script.Data.ToArray();var before=Bytes(d);
        using var s=d.CreateEditorHeroBandSession(band);var identity=s.Members[1].ScenarioIdentity;Check(identity!=0 && s.Members[1].HasScenario,"Fixture member scope.");
        s.Remove([1]);_=d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(s,"Band 0"));Check(script.Data.Span.SequenceEqual(scriptBytes),"Source parent removal must not silently delete scenario roots.");
        using(var point=d.CreateEditorScenarioPointSession(initialScope:16))Check(point.Choices.All(c=>c.Owner.MemberIndex!=1),"Removed owner still exposed.");
        d.Undo();Exact(before);Pass(false,"bound-member removal preserves literal scenario roots; parent Undo restores identity/binding");
    }
    private static void BandCapacity(bool native)
    {
        var d=Fixture(native);var first=Band(d);
        for(var index=1;index<23;index++)
        {
            if(native)d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEBAND"),new(0,0));
            else d.AddLegacyRecord("Band",new(0,0));
        }
        using(var last=d.CreateEditorHeroBandSession()){last.Add(Leader);_=d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(last,"Band "+last.ID));}
        Check(d.Records().Count(NativeMapDocument.IsBand)==24,"Fixture band count.");var bytes=Bytes(d);var h=d.GetHistory();
        Reject(()=>d.CreateEditorHeroBandSession());Exact(bytes);Check(d.GetHistory().ChangeCount==h.ChangeCount,"Refused twenty-fifth New counted.");
        using(var modify=d.CreateEditorHeroBandSession(first))_=d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(modify,"Band 0"));
        Check(d.Records().Count(NativeMapDocument.IsBand)==24,"Modify at cap changed collection size.");
        if(native)d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEBAND"),new(0,0));else d.AddLegacyRecord("Band",new(0,0));
        Check(d.Records().Count(NativeMapDocument.IsBand)==25,"Generic collection API was incorrectly capped.");
        Reject(()=>d.CreateEditorHeroBandSession());Pass(native,"source twenty-four-band New limit, atomic refusal, existing Modify and generic APIs retained");
    }
    private static void DeleteBand(bool native)
    {
        var d=Fixture(native);var band=Band(d);_=Band(d);
        foreach(var scope in new uint[]{8,16})
        {
            var owner=d.ScriptOwners().First(o=>o.Scope==scope && (o.BandRecord??o.Record).File==band.File && (o.BandRecord??o.Record).Offset==band.Offset);
            using var point=d.CreateEditorScenarioPointSession(initialScope:scope);point.SelectOwner(owner);
            var root=d.ApplyEditorScenarioPoint(d.PrepareEditorScenarioPoint(point));
            d.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMEELAPSED"));
        }
        var script=d.Files.Single(f=>f.Signature==(native?0xD2u:NativeMapDocument.LegacyEvents));var scriptBefore=script.Data.ToArray();var before=Bytes(d);var h=d.GetHistory();var revision=d.EditRevision;
        Reject(()=>Fixture(native).DeleteEditorHeroBand(band));Exact(before);
        d.Edit("Outer transaction",()=>Reject(()=>d.DeleteEditorHeroBand(band)));Exact(before);
        var countField=typeof(NativeMapDocument).GetField("changeCount",BindingFlags.Instance|BindingFlags.NonPublic)!;var priorCount=countField.GetValue(d);countField.SetValue(d,long.MaxValue);
        Reject(()=>d.DeleteEditorHeroBand(band));Exact(before);countField.SetValue(d,priorCount);
        d.DeleteEditorHeroBand(band);Accepted(d,h,revision);
        Check(d.Records().Count(NativeMapDocument.IsBand)==1 && script.Data.Span.SequenceEqual(scriptBefore),"Source Delete must remove only the band and preserve literal scenario bytes.");
        var after=Bytes(d);d.Undo();Exact(before);d.Redo();Exact(after);Check(script.Data.Span.SequenceEqual(scriptBefore),"Delete Redo changed scenarios.");
        d.Undo();var restored=d.Records().First(NativeMapDocument.IsBand);d.SetHistoryDepth(0);var count=d.EditorChangeCount;d.DeleteEditorHeroBand(restored);
        Check(!d.CanUndo && d.EditorChangeCount==unchecked(count+1),"History0 Delete source count.");
        Pass(native,"source selector Delete bound band only, literal script retention, single Undo/Redo, foreign/nested/overflow guards and history0");
    }
    private static void Layouts()
    {
        var d=Fixture(false);var file=d.Files.Single(f=>f.Signature==NativeMapDocument.LegacyBands);
        Check(MemoryMarshal.TryGetArray(file.Data,out var buffer),"Fixture raw buffer.");buffer.Array![buffer.Offset+4]=4;
        var bytes=Bytes(d);
        using(var draft=d.CreateEditorHeroBandSession())
        {draft.Add(Leader);Exact(bytes);var saved=d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(draft,"Historical New"));Check(saved.Category=="Project.MTHBAND","Private v4 New was not converted to current layout.");d.Undo();Exact(bytes);}
        Check(MemoryMarshal.TryGetArray(file.Data,out buffer),"Restored fixture raw buffer.");
        buffer.Array![buffer.Offset+4]=6;var unsupported=Bytes(d);Reject(()=>d.CreateEditorHeroBandSession());Exact(unsupported);
        Pass(false,"historical empty v4 private conversion and exact Undo; unknown version still refused atomically");
    }
}
