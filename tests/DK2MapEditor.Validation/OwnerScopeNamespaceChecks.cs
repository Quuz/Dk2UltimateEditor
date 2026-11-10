using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class OwnerScopeNamespaceChecks
{
    private const string Name="DK2Preview_6a88240eaf814f78";
    private static readonly (uint Scope,string Kind,string Target)[] Types=[(2,"Creature","creature"),(64,"Door","door"),(128,"Object","object"),(256,"MagicalObject","object")];
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static NativeMapDocument New()=>NativeMapDocument.CreateNewMap(new(Width:14,Height:12,DisplayName:Name,FileName:Name));
    // Import fixture mutation, before the tested operation: repeated IDs cannot
    // be inferred from UI creation, and no installed/input file is written.
    private static void Raw(NativeRecord record,string name,uint value)
    {
        Require(MemoryMarshal.TryGetArray(record.File.Data,out var bytes),"Fixture backing array unavailable.");
        var field=record.Fields.Single(f=>f.Name==name);field.Encode(value.ToString(System.Globalization.CultureInfo.InvariantCulture)).CopyTo(bytes.AsSpan().Slice(record.Offset+field.Offset));
    }
    private static uint Number(NativeRecord r,string name)=>uint.Parse(r.Read(r.Fields.Single(f=>f.Name==name)));
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> before){foreach(var(file,bytes)in before)Require(file.Data.Span.SequenceEqual(bytes),"Owner scope lookup/refusal/Undo changed complete buffers.");}
    private static void Refuses(Action action){try{action();}catch(InvalidOperationException){return;}throw new InvalidOperationException("An actual same-scope duplicate acquired arbitrary binding authority.");}
    private static NativeMapDocument Four(bool linked)
    {
        var d=New();var timer=d.AddLegacyRecord("Trigger",new(0,0));
        Raw(timer,"ctrl",LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMEELAPSED").Type);Raw(timer,"param1",5);Raw(timer,"param5",600);
        var eventID=ScriptGraph.ID(timer); // Adding owner links moves the event rows in KTRG.
        foreach(var(scope,kind,target)in Types)
        {
            var thing=d.AddLegacyRecord(kind,new(2+(int)(scope%3),3));Raw(thing,"uniqueid",77);Raw(thing,"id",scope==256?0x75u:1u);
            if(!linked)continue;
            var link=d.AddLegacyRecord("TriggerLink",new(0,0));Raw(link,"node.type",scope);Raw(link,target,77);Raw(link,"trigger",eventID);
        }
        return d;
    }
    private static void Existing()
    {
        var d=Four(true);var before=Snapshot(d);var revision=d.EditRevision;var history=d.GetHistory();
        foreach(var(scope,_,target)in Types)
        {
            var owner=d.ScriptOwners().Single(o=>o.Scope==scope);var found=d.FindScriptScope(owner)??throw new InvalidOperationException("Existing typed root was not found.");
            Require(found.Scope==scope&&Number(found.Owner,target)==77&&found.Target==1,"Same number in another category hid an exact typed owner binding.");
            var same=d.EnsureScriptScope(owner);Require(same.Owner.Offset==found.Owner.Offset&&same.Owner.File==found.Owner.File,"Ensure replaced an existing typed root.");
        }
        Exact(before);Require(d.EditRevision==revision&&d.GetHistory().Undo.SequenceEqual(history.Undo)&&d.GetHistory().Redo.SequenceEqual(history.Redo),"Read-only typed lookup changed lifetime/history.");
    }
    private static void Create()
    {
        var d=Four(false);
        foreach(var(scope,_,target)in Types)
        {
            var before=Snapshot(d);var owner=d.ScriptOwners().Single(o=>o.Scope==scope);var root=d.BindScriptScope(owner,1);
            Require(root.Scope==scope&&root.Target==1&&Number(root.Owner,target)==77&&Number(owner.Record,"uniqueid")==77,"Binding repaired/import-renumbered an unambiguous typed owner.");
            var after=Snapshot(d);d.Undo();Exact(before);d.Redo();Exact(after);
        }
        var output=d.CompileLegacyProject(Name,NativeMapDocument.Create(14,12,Name));
        var roots=ScriptGraph.Build(output).Roots.Where(r=>Types.Any(t=>t.Scope==r.Scope)&&r.Target!=0).ToArray();
        Require(roots.Length==4&&roots.All(r=>r.Target==1)&&roots.Select(r=>r.Scope).Order().SequenceEqual(Types.Select(t=>t.Scope).Order()),"Native compilation lost or reassigned a typed physical owner: "+string.Join(",",roots.Select(r=>$"{r.Scope}/{r.Target}/{r.Owner.Category}")));
    }
    private static void Band(bool inactiveTail)
    {
        var d=New();var band=d.AddLegacyRecord("Band",new(0,0));d.AddBandMember(band,17,4,3);
        band=d.Records().Single(r=>r.Category=="Project.MTHBAND");Raw(band,"uniqueid",inactiveTail?99u:55u);Raw(band,"heros[0].uniqueid",55);
        if(inactiveTail)Raw(band,"heros[1].uniqueid",55);
        foreach(var scope in new uint[]{8,16})
        {
            var owner=d.ScriptOwners().Single(o=>o.Scope==scope);var before=Snapshot(d);var root=d.EnsureScriptScope(owner);
            var expected=scope==8&&inactiveTail?99u:55u;
            Require(root.Scope==scope&&Number(root.Owner,scope==8?"band":"creature")==expected,"Band/member namespace or inactive tail changed the selected active owner.");
            var after=Snapshot(d);d.Undo();Exact(before);d.Redo();Exact(after);
        }
        if(inactiveTail)Require(Number(band,"heros[1].isvalid")==0&&Number(band,"heros[1].uniqueid")==55,"Binding changed an inactive retained slot.");
    }
    private static void SameScope()
    {
        foreach(var member in new[]{false,true})
        {
            var d=New();
            if(member)
            {
                var band=d.AddLegacyRecord("Band",new(0,0));d.AddBandMember(band,17,4,3);d.AddBandMember(band,19,5,4);
                band=d.Records().Single(r=>r.Category=="Project.MTHBAND");Raw(band,"heros[0].uniqueid",55);Raw(band,"heros[1].uniqueid",55);
            }
            else foreach(var x in new[]{2,3})Raw(d.AddLegacyRecord("Creature",new(x,3)),"uniqueid",55);
            var before=Snapshot(d);var revision=d.EditRevision;var history=d.GetHistory();
            foreach(var owner in d.ScriptOwners().Where(o=>o.Scope==(member?16:2)))
            {Refuses(()=>d.FindScriptScope(owner));Refuses(()=>d.EnsureScriptScope(owner));}
            Exact(before);Require(d.EditRevision==revision&&d.GetHistory().Undo.SequenceEqual(history.Undo)&&d.GetHistory().Redo.SequenceEqual(history.Redo),"Rejected duplicate changed owner IDs/history.");
        }
    }
    public static void Run(Action<string,Action> run)
    {
        RunExisting(run);
        RunCompilation(run);
        run("owner scope namespaces: band parent and active member identities are separate namespaces",()=>Band(false));
        run("owner scope namespaces: inactive retained member identity does not hide an active member binding",()=>Band(true));
        run("owner scope namespaces: actual same-scope creature/member duplicates remain atomically refused",SameScope);
    }
    public static void RunCompilation(Action<string,Action> run)=>run("owner scope namespaces: create/bind/Undo/Redo and actual compilation preserve four category-qualified owners without renumbering",Create);
    public static void RunExisting(Action<string,Action> run)=>run("owner scope namespaces: existing creature/door/object/magic roots sharing a numeric ID remain exactly resolvable",Existing);
}
