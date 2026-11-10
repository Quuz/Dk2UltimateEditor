using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class SelectionUnrelatedScriptIdentityChecks
{
    private const string Name="DK2Preview_6a88240eaf814f78";
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new(Name,Name,12,12,NativeFormat:native));
    private static void Raw(NativeRecord row,string name,uint value)
    {Require(MemoryMarshal.TryGetArray(row.File.Data,out var bytes),"Fixture backing bytes unavailable.");var f=row.Fields.Single(f=>f.Name==name);f.Encode(value.ToString(System.Globalization.CultureInfo.InvariantCulture)).CopyTo(bytes.AsSpan().Slice(row.Offset+f.Offset));}
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> before){foreach(var(f,bytes)in before)Require(f.Data.Span.SequenceEqual(bytes),"Selection changed malformed input or history bytes.");}
    private static NativeRecord Event(NativeMapDocument d)=>d.IsLegacyProject?d.AddLegacyRecord("Trigger",new(0,0)):d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="Trigger"),new(0,0));
    private static void Bind(NativeMapDocument d,NativeRecord thing,ushort target)
    {
        if(!d.IsLegacyProject){Raw(thing,thing.Fields.Single(f=>f.Name.EndsWith("Trigger",StringComparison.Ordinal)).Name,target);return;}
        Raw(thing,"uniqueid",77);
        var link=d.Records().SingleOrDefault(r=>r.Category=="Project.TRIGGERLINK"&&r.Read(r.Fields.Single(f=>f.Name=="node.type"))=="2")??d.AddLegacyRecord("TriggerLink",new(0,0));
        Raw(link,"node.type",2);Raw(link,"creature",77);Raw(link,"trigger",target);
    }
    private static (NativeMapDocument Document,NativeRecord Thing) Fixture(bool native,bool reachable)
    {
        var d=New(native);var thing=native?d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEKEEPER"),new(2,2)):d.AddLegacyRecord("Creature",new(2,2));
        if(reachable){var first=Event(d);Bind(d,thing,ScriptGraph.ID(first));}
        var second=Event(d);var third=Event(d);Raw(second,native?"ID":"id",reachable?7u:0u);Raw(third,native?"ID":"id",reachable?7u:0u);
        if(reachable)Raw(second,native?"NextOr":"or",65000);
        return(d,thing);
    }
    private static void Copy(bool reachable)
    {
        foreach(var native in new[]{false,true})
        {
            var(d,_)=Fixture(native,reachable);var before=Snapshot(d);var revision=d.EditRevision;var history=d.GetHistory();
            var clipboard=d.CopySelection(new(2,2,1,1),protectBoundary:true);var expected=reachable?(native?1:2):0;
            Require(clipboard.ObjectCount==1&&clipboard.ScriptCount==expected,"Copy included an unrelated malformed graph or dropped a selected unambiguous root.");
            clipboard=SelectionFile.Parse(SelectionFile.Serialize(clipboard));Require(clipboard.ScriptCount==expected,"DKZ changed the reachable graph boundary.");
            Exact(before);Require(d.EditRevision==revision&&d.GetHistory().Undo.SequenceEqual(history.Undo)&&d.GetHistory().Redo.SequenceEqual(history.Redo),"Read-only Copy/DKZ changed the source lifetime.");
            var destination=New(native);var targetBefore=Snapshot(destination);destination.PasteEditorSelection(new(5,5),clipboard);
            var copied=destination.CopySelection(new(5,5,1,1));Require(copied.ObjectCount==1&&copied.ScriptCount==expected,"Pasted clipboard included malformed source-only IDs or lost its selected graph.");
            Require(destination.Records().Count(ScriptGraph.IsEvent)==(reachable?1:0),"Unrelated zero/duplicate/dangling events appeared in destination.");
            var after=Snapshot(destination);destination.Undo();Exact(targetBefore);destination.Redo();Exact(after);Exact(before);
        }
    }
    private static void RejectReachable()
    {
        foreach(var native in new[]{false,true})foreach(var target in new ushort[]{7,65000})
        {
            var(d,thing)=Fixture(native,true);Bind(d,thing,target);var before=Snapshot(d);var revision=d.EditRevision;var history=d.GetHistory();
            try{d.CopySelection(new(2,2,1,1));throw new InvalidOperationException("An ambiguous/dangling selected graph was copied by guess.");}
            catch(InvalidDataException error){Require(error.Message.Contains(target==7?"ambiguous":"dangling",StringComparison.OrdinalIgnoreCase),"Selected malformed root lacks an explicit graph diagnostic.");}
            Exact(before);Require(d.EditRevision==revision&&d.GetHistory().Undo.SequenceEqual(history.Undo)&&d.GetHistory().Redo.SequenceEqual(history.Redo),"Rejected selected graph changed data/history.");
        }
    }
    public static void Run(Action<string,Action> run)
    {
        run("selection malformed IDs: unreferenced zero-ID duplicates cannot prevent unscripted physical Copy/DKZ/Paste",()=>Copy(false));
        run("selection malformed IDs: unrelated duplicate/dangling events stay outside an unambiguous selected graph",()=>Copy(true));
        run("selection malformed IDs: actually selected duplicate or missing targets are explicitly refused without input repair",RejectReachable);
    }
}
