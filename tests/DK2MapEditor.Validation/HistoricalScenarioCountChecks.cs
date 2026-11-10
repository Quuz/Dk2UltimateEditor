using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class HistoricalScenarioCountChecks
{
    private const string GameName="DK2Preview_6a88240eaf814f78";
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static int Number(NativeRecord record,string name)=>int.Parse(record.Read(record.Fields.Single(field=>field.Name==name)),CultureInfo.InvariantCulture);
    private static byte[] Record(string structure,params (string Name,int Value)[] values)
    {
        var bytes=new byte[LegacyProjectSchemas.Size(structure)];
        foreach(var(name,value) in values){var field=LegacyProjectSchemas.Fields(structure).Single(field=>field.Name==name);field.Encode(value.ToString(CultureInfo.InvariantCulture)).CopyTo(bytes,field.Offset);}
        return bytes;
    }
    private sealed class Fixture : IDisposable
    {
        public string DirectoryPath {get;}=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2HistoricalCounts-"+Guid.NewGuid().ToString("N")));
        public string InputPath {get;}
        public byte[] Input {get;}
        public NativeMapDocument Document {get;}
        public Fixture(int[] actionCounts,bool truncate=false,uint scopes=1)
        {
            Require(DirectoryPath.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Unexpected fixture root.");Directory.CreateDirectory(DirectoryPath);
            var basis=TextMapFile.Parse("GUI_GENERATED_MAP\n"+GameName+"\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nAREAS\n1\n8 1 1 1 1\nEOF");
            using var buffer=new MemoryStream();using var writer=new BinaryWriter(buffer);
            writer.Write(scopes);writer.Write(0xC5u);writer.Write((uint)actionCounts.Length);writer.Write(Record("MAPACTIONPOINTV1",("area",8)));
            var condition=Record("MAPTRIGGERV1",("node.type",3),("seconds",600));var action=Record("MAPACTIONV1",("node.type",8),("seconds",600));
            foreach(var count in actionCounts)
            {writer.Write(0xD5u);writer.Write((uint)count);writer.Write(condition);for(var i=0;i<count;i++){writer.Write(0xD6u);writer.Write(action);}}
            var payload=buffer.ToArray();if(truncate)payload=payload[..^1];
            Input=LegacySectionFile.Create(basis.Sections.Where(section=>section.Signature!=NativeMapDocument.LegacyEvents).Append(new(NativeMapDocument.LegacyEvents,1,payload))).ToArray();
            InputPath=Path.Combine(DirectoryPath,GameName+".dkm");File.WriteAllBytes(InputPath,Input);Document=NativeMapDocument.Load(InputPath);
        }
        public void Dispose(){Require(File.ReadAllBytes(InputPath).AsSpan().SequenceEqual(Input),"Historical input changed.");Directory.Delete(DirectoryPath,recursive:true);}
    }
    private static void Counts(NativeMapDocument document,int conditions,int actions)
    {
        var records=document.Records();var triggers=records.Where(record=>record.Category=="Project.TRIGGER").ToArray();var rows=records.Where(record=>record.Category=="Project.ACTION").ToArray();
        Require(!document.HasLegacyUpgrade&&triggers.Length==conditions&&rows.Length==actions,"Upgraded counts/version differ.");
        Require(rows.Select(record=>Number(record,"id")).SequenceEqual(Enumerable.Range(0,actions).Select(index=>65535-index)),"Descending signed action IDs changed.");
        for(var i=0;i<rows.Length;i++)
        {
            Require(Number(rows[i],"next")==((i==rows.Length-1||triggers.Skip(1).Any(trigger=>Number(trigger,"and")==Number(rows[i],"id")-1))?0:Number(rows[i],"id")-1),"Action chain crossed a condition boundary or lost a target.");
            Require(Number(rows[i],"param1")==16&&Number(rows[i],"param5")==600,"Historical action parameters changed.");
        }
        Require(Number(rows[^1],"id")==32768&&Number(rows[^1],"next")==0,"Signed minimum action ID was not retained as terminal 0x8000.");
    }
    private static void Reject(Fixture fixture)
    {
        var document=fixture.Document;var bytes=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var revision=document.EditRevision;var dirty=document.HasChanges;var history=document.GetHistory();
        try{document.UpgradeLegacyProject();throw new InvalidOperationException("Invalid historical counts were accepted.");}catch(InvalidDataException){}
        var after=document.GetHistory();Require(document.HasLegacyUpgrade&&bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&revision==document.EditRevision&&dirty==document.HasChanges
            &&history.ChangeCount==after.ChangeCount&&history.EditorChangeCount==after.EditorChangeCount&&history.SnapshotBytes==after.SnapshotBytes&&history.Undo.SequenceEqual(after.Undo)&&history.Redo.SequenceEqual(after.Redo),"Rejected upgrade left partial bytes/history.");
    }
    public static void Run(Action<string,Action> run)
    {
        run("historical scenario counts: one condition may own all32768 signed action IDs; Undo/Redo, Save/reopen and compile retain0x8000",()=>
        {
            using var fixture=new Fixture([32768]);var document=fixture.Document;var bytes=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            document.UpgradeLegacyProject();Counts(document,1,32768);Require(document.GetHistory().Undo.Count==1,"Upgrade was not one Undo operation.");
            document.Undo();Require(document.HasLegacyUpgrade&&bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Undo changed original historical sections.");document.Redo();Counts(document,1,32768);
            var saved=document.SaveCopy(Path.Combine(fixture.DirectoryPath,"saved"),temporary:true);var reopened=NativeMapDocument.Load(saved);Counts(reopened,1,32768);
            var native=reopened.CompileLegacyProject(GameName,NativeMapDocument.Create(3,3,GameName));var actions=native.Records().Where(record=>record.File.Signature==0xD2&&BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[record.Offset..])==0xD6&&record.File.Data.Span[record.Offset+22]==LegacyEventSchemas.All.Single(type=>type.Name=="ACTION_DISPLAYTIMER").Type).ToArray();
            Require(actions.Length==32768&&actions.Select(record=>Number(record,"ID")).Distinct().Count()==32768&&actions.All(record=>Number(record,"ID")>=32768),"Native compilation lost/collided signed action IDs.");
        });
        run("historical scenario counts:32767 plus1 actions across two conditions retain separate chains",()=>
        {using var fixture=new Fixture([32767,1]);fixture.Document.UpgradeLegacyProject();Counts(fixture.Document,2,32768);});
        run("historical scenario counts: per-condition and cumulative action overflow remain atomic refusals",()=>
        {using(var single=new Fixture([32769]))Reject(single);using(var accumulated=new Fixture([32767,2]))Reject(accumulated);});
        run("historical scenario counts: condition limit, impossible scope count and truncated final action stay guarded",()=>
        {using(var conditions=new Fixture(new int[32768]))Reject(conditions);using(var scopes=new Fixture([0],scopes:uint.MaxValue))Reject(scopes);using(var truncated=new Fixture([32768],truncate:true))Reject(truncated);});
    }
}
