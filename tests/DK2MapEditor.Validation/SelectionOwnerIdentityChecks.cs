using System.Globalization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class SelectionOwnerIdentityChecks
{
    private const string GameName="DK2Preview_6a88240eaf814f78";
    private static readonly (uint Scope,uint Type,string Field)[] owners=[(2,0,"creature"),(64,1,"door"),(128,3,"object"),(256,4,"object")];
    private static readonly TileRect selected=new(1,1,6,3);
    private static readonly TilePoint destination=new(1,4);
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static uint Number(NativeRecord record,string name)=>uint.Parse(record.Read(record.Fields.Single(field=>field.Name==name)),CultureInfo.InvariantCulture);
    private static byte[] Record(string structure,params (string Name,uint Value)[] values)
    {
        var bytes=new byte[LegacyProjectSchemas.Size(structure)];
        foreach(var(name,value) in values){var field=LegacyProjectSchemas.Fields(structure).Single(field=>field.Name==name);field.Encode(value.ToString(CultureInfo.InvariantCulture)).CopyTo(bytes,field.Offset);}
        return bytes;
    }
    private sealed class Fixture : IDisposable
    {
        public string DirectoryPath {get;}=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2SelectionOwners-"+Guid.NewGuid().ToString("N")));
        private readonly Dictionary<string,byte[]> inputs=[];
        public Fixture(){Require(DirectoryPath.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Unexpected fixture root.");Directory.CreateDirectory(DirectoryPath);}
        public NativeMapDocument Create(bool sameID=true,bool ambiguous=false,bool occupied=false)
        {
            var map="GUI_GENERATED_MAP\n"+GameName+"\nPLAYERS 1\nMAP\n8 X 8\n"+string.Join('\n',Enumerable.Range(0,8).Select(y=>string.Concat(Enumerable.Range(0,8).Select(x=>x==0||y==0||x==7||y==7?"Oa":"Oc"))))+"\nEOF";
            var basis=TextMapFile.Parse(map);var things=new List<byte[]>();var links=new List<byte[]>();
            var count=occupied?1:ambiguous?2:4;
            for(var i=0;i<count;i++)
            {
                var type=occupied?3u:ambiguous?0u:owners[i].Type;var id=occupied?1u:77u+(sameID?0u:(uint)i);
                things.Add(Record("MAPTHING",("node.type",type),("uniqueid",id),("id",type==4?0x75u:1u),("owner",3),("x",occupied?1u:2u+(uint)i),("y",occupied?1u:2u),("dataAll",type==0?1u:0u),("health",100)));
                if(occupied){links.Add(Record("TRIGGERLINK",("node.type",2),("creature",2),("trigger",1)));continue;}
                if(ambiguous&&i!=0)continue;
                var scope=ambiguous?2u:owners[i].Scope;var target=ambiguous?"creature":owners[i].Field;
                var values=new List<(string,uint)>{("node.type",scope),("trigger",1),(target,id)};
                if(!sameID&&!ambiguous)
                    foreach(var field in new[]{"creature","door","object"}.Where(field=>field!=target))values.Add((field,field=="creature"?77u:field=="door"?78u:79u));
                links.Add(Record("TRIGGERLINK",values.ToArray()));
            }
            var trigger=Record("TRIGGER",("node.type",0xD5),("id",1),("ctrl",LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED").Type),("param1",5),("param5",600),("repeat",1));
            using var events=new MemoryStream();using(var writer=new BinaryWriter(events,System.Text.Encoding.UTF8,leaveOpen:true))
            {writer.Write((uint)links.Count);writer.Write(1u);writer.Write(0u);foreach(var link in links)writer.Write(link);writer.Write(trigger);var aliases=new byte[146*64];System.Text.Encoding.ASCII.GetBytes(occupied?"DestinationAlias":"SourceAlias").CopyTo(aliases,0);writer.Write(aliases);}
            var sections=basis.Sections.Where(section=>section.Signature is not(NativeMapDocument.LegacyThings or NativeMapDocument.LegacyEvents)).Append(new(NativeMapDocument.LegacyThings,6,things.SelectMany(row=>row).ToArray())).Append(new(NativeMapDocument.LegacyEvents,3,events.ToArray()));
            var bytes=LegacySectionFile.Create(sections).ToArray();var path=Path.Combine(DirectoryPath,"input"+inputs.Count+".dkm");File.WriteAllBytes(path,bytes);inputs.Add(path,bytes);return NativeMapDocument.Load(path);
        }
        public void Dispose(){foreach(var(path,bytes) in inputs)Require(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes),"Input project changed.");Directory.Delete(DirectoryPath,recursive:true);}
    }
    private static Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument document)=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static bool Same(Dictionary<NativeFile,byte[]> bytes)=>bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value));
    private static void Owners(NativeMapDocument document,uint eventID=1)
    {
        var rows=document.Records();var things=rows.Where(row=>row.Category=="Project.MAPTHING"&&Number(row,"y")==5).ToArray();var links=rows.Where(row=>row.Category=="Project.TRIGGERLINK"&&Number(row,"trigger")==eventID).ToArray();
        Require(things.Length==4&&links.Length==4,"Pasted owner/link count changed.");Require(things.Select(row=>Number(row,"uniqueid")).Distinct().Count()==4,"Pasted unique IDs collided.");
        foreach(var(scope,type,field) in owners)
        {var thing=things.Single(row=>Number(row,"node.type")==type);var link=links.Single(row=>Number(row,"node.type")==scope);Require(Number(link,field)==Number(thing,"uniqueid"),"Pasted scope"+scope+" acquired another category's unique ID.");}
    }
    public static void Run(Action<string,Action> run)
    {
        run("selection owner identity: direct paste retains all four owner categories sharing one numeric UID",()=>
        {using var fixture=new Fixture();var source=fixture.Create();var before=Bytes(source);var clipboard=source.CopySelection(selected);var target=fixture.Create(occupied:true);target.PasteSelection(destination,clipboard);Owners(target,2);Require(Same(before),"Copy/paste changed source.");});
        run("selection owner identity: actual buffer and direct DKZ read/import retain category-scoped owners",()=>
        {
            using var fixture=new Fixture();var source=fixture.Create();var before=Bytes(source);var clipboard=source.CopySelection(selected);
            foreach(var direct in new[]{false,true})
            {var path=Path.Combine(fixture.DirectoryPath,direct?"direct.dkz":"buffer.dkz");if(direct)SelectionFile.SaveDirectSelectionConfirmed(path,clipboard);else SelectionFile.SaveNew(path,clipboard);var candidate=SelectionFile.ReadForImport(path);Require(candidate.SourceIsUnchanged(),"Written archive changed.");var target=fixture.Create(occupied:true);target.PasteSelection(destination,candidate.Selection);Owners(target,2);}
            Require(Same(before),"DKZ export changed source.");
        });
        run("selection owner identity: paste remaps only the active owner field and preserves inactive target bytes",()=>
        {
            using var fixture=new Fixture();var source=fixture.Create(sameID:false);var original=source.Records().Where(row=>row.Category=="Project.TRIGGERLINK").ToArray();var target=fixture.Create(occupied:true);target.PasteSelection(destination,source.CopySelection(selected));Owners(target,2);
            foreach(var(scope,_,active) in owners)
            {var before=original.Single(row=>Number(row,"node.type")==scope);var after=target.Records().Single(row=>row.Category=="Project.TRIGGERLINK"&&Number(row,"trigger")==2&&Number(row,"node.type")==scope);foreach(var name in new[]{"creature","door","object"}.Where(name=>name!=active))Require(Number(before,name)==Number(after,name),"Inactive scope target field was rewritten: "+scope+"/"+name);}
        });
        run("selection owner identity: same-category ambiguous source still refuses without changing bytes/history",()=>
        {
            using var fixture=new Fixture();var source=fixture.Create(ambiguous:true);var before=Bytes(source);var revision=source.EditRevision;var history=source.GetHistory();
            try{source.CopySelection(selected);throw new InvalidOperationException("Ambiguous same-category owner was accepted.");}catch(InvalidDataException){}
            var after=source.GetHistory();Require(Same(before)&&revision==source.EditRevision&&history.ChangeCount==after.ChangeCount&&history.EditorChangeCount==after.EditorChangeCount&&history.Undo.SequenceEqual(after.Undo)&&history.Redo.SequenceEqual(after.Redo),"Rejected copy changed source/history.");
        });
        run("selection owner identity: reserved UID/event collision, Undo/Redo and actual DKM Save/reopen preserve owners",()=>
        {
            using var fixture=new Fixture();var source=fixture.Create();var clipboard=source.CopySelection(selected);var snapshot=SelectionFile.Serialize(clipboard);var target=fixture.Create(occupied:true);var before=Bytes(target);target.PasteSelection(destination,clipboard);Owners(target,2);
            Require(target.Records().Where(row=>row.Category=="Project.MAPTHING"&&Number(row,"y")==5).All(row=>Number(row,"uniqueid")>=3),"Paste acquired the pre-existing/orphan reserved UID.");
            Require(target.GetHistory().Undo.Count==1,"Paste was not one Undo operation.");target.Undo();Require(Same(before),"Undo changed original target bytes.");target.Redo();Owners(target,2);
            var reopened=NativeMapDocument.Load(target.SaveCopy(Path.Combine(fixture.DirectoryPath,"saved"),temporary:true));Owners(reopened,2);
            Require(reopened.ReadScenarioNames().Slots.Any(slot=>slot.StoredName=="DestinationAlias"),"Paste overwrote destination global aliases.");Require(SelectionFile.Serialize(clipboard).AsSpan().SequenceEqual(snapshot),"Paste changed the independent clipboard.");
        });
    }
}
