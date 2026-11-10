using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class NativeDefinitionTailChecks
{
    private const string GameName="DK2Preview_6a88240eaf814f78";
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Word(byte[] bytes,int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset),value);
    private static byte[] Section(uint signature,int? rows=null,int? stride=null)
    {
        var family=LegacyGlobalSchemas.Find(signature);var count=rows??(family is null?1:LegacyCompilerRules.DefinitionOrder(family.Group).Count);var size=stride??family?.RecordSize??16;var bytes=new byte[56+count*size];
        Word(bytes,0,signature);Word(bytes,4,4);Word(bytes,8,(uint)bytes.Length);Word(bytes,12,signature+1);Word(bytes,16,28);Word(bytes,20,(uint)count);Word(bytes,24,1);Word(bytes,48,signature+2);Word(bytes,52,(uint)(count*size));
        if(signature==0xAA)for(var i=0;i<count;i++)bytes[56+i*size+0xF4B]=(byte)(i+1);return bytes;
    }
    private static byte[] Tail(int length)=>Enumerable.Range(0,length).Select(i=>(byte)(0x80+i%127)).ToArray();
    private static byte[] Join(params byte[][] data)=>data.SelectMany(bytes=>bytes).ToArray();
    private static NativeRecord[] Rows(NativeMapDocument document,string family)=>document.Records().Where(row=>row.Category=="Definition."+family).ToArray();
    private static void Reject(Action action)
    {try{action();throw new InvalidOperationException("Expected a refusal for unsupported/corrupt native definition data.");}catch(InvalidDataException){}}
    private sealed class Fixture : IDisposable
    {
        public string Root {get;}=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2DefinitionTails-"+Guid.NewGuid().ToString("N")));
        private readonly List<(string Path,byte[] Bytes)> inputs=[];private int next;
        public Fixture(){Require(Root.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Unexpected fixture root.");Directory.CreateDirectory(Root);}
        public NativeMapDocument Load(byte[] bytes)
        {
            var path=NativeMapDocument.Create(8,8,GameName).SaveCopy(Path.Combine(Root,"input"+next++),temporary:true);var descriptor=File.ReadAllBytes(path);
            var reference=56+(int)BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(52))+8;Word(descriptor,reference,0);new NativeField("Reference",0,64,NativeValueKind.WesternAnsi).Encode("Data\\Editor\\Maps\\"+GameName+"Globals.kld").CopyTo(descriptor,reference+8);File.WriteAllBytes(path,descriptor);
            var definitions=Path.Combine(Path.GetDirectoryName(path)!,GameName+"Globals.kld");File.WriteAllBytes(definitions,bytes);inputs.Add((definitions,bytes));return NativeMapDocument.Load(path);
        }
        public string Fresh(string name)=>Path.Combine(Root,name+Guid.NewGuid().ToString("N"));
        public void Dispose(){foreach(var(path,bytes) in inputs)Require(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes),"Original definition input changed.");Directory.Delete(Root,recursive:true);}
    }
    public static void OfficialPacking()
    {
        using var fixture=new Fixture();var definitions=new uint[]{0x6E,0x78,0xAA,0x8C,0x82,0xF0,0xA0,0x96,0x104}.Select(signature=>Section(signature)).Append(Tail(96)).ToArray();
        var basis=fixture.Load(Join(definitions));var project=NativeMapDocument.CreateNewMap(new(Width:8,Height:8,DisplayName:GameName,FileName:GameName));
        var compiled=project.CompileLegacyProject(GameName,basis);var output=fixture.Fresh("official");Reject(()=>compiled.SaveCopy(output));Require(!Directory.Exists(output),"Rejected official packing left a published partial directory.");
    }
    public static void Run(Action<string,Action> run)
    {
        run("native definition tails: standalone arbitrary suffix1/55/56/96/4096 survives edit Undo and raw Save/reopen",()=>
        {
            using var fixture=new Fixture();foreach(var length in new[]{1,55,56,96,4096})
            {
                var bytes=Join(Section(0x6E),Tail(length));var source=fixture.Load(bytes);var row=Rows(source,"Terrains")[0];var field=row.Fields.First(field=>field.BitMask==0&&field.Kind==NativeValueKind.Byte);
                source.SetField(row,field,"1");source.Undo();Require(row.File.Data.Span.SequenceEqual(bytes),"Undo changed opaque suffix.");
                var copy=NativeMapDocument.Load(source.SaveCopy(fixture.Fresh("raw"),temporary:true));Require(Rows(copy,"Terrains")[0].File.Data.Span.SequenceEqual(bytes),"Raw Save/reopen changed a standalone suffix.");
            }
        });
        run("native definition tails: packed known and framed unknown sections plus suffix retain all raw bytes",()=>
        {
            using var fixture=new Fixture();var bytes=Join(Section(0x6E),Section(0x7000),Section(0x78),Tail(96));var source=fixture.Load(bytes);
            Require(Rows(source,"Terrains").Length==41&&Rows(source,"Rooms").Length==26,"Known families around unknown data were lost.");
            var copy=NativeMapDocument.Load(source.SaveCopy(fixture.Fresh("packed"),temporary:true));Require(Rows(copy,"Rooms")[0].File.Data.Span.SequenceEqual(bytes),"Packed raw copy lost unknown frame/tail.");
        });
        run("native definition tails: new-map basis extracts real families and keeps terminal suffix with its contiguous chunk",()=>
        {
            using var fixture=new Fixture();var terrain=Section(0x6E);var rooms=Section(0x78);var tail=Tail(96);var source=fixture.Load(Join(terrain,rooms,tail));
            var target=NativeMapDocument.Create(8,8,GameName,source);Require(target.Files.Single(file=>file.Signature==0x6E).Data.Span.SequenceEqual(terrain),"Terrain extraction included other known families.");
            Require(target.Files.Single(file=>file.Signature==0x78).Data.Span.SequenceEqual(Join(rooms,tail)),"Terminal suffix disappeared while extracting its physical preceding chunk.");
            var reopened=NativeMapDocument.Load(target.SaveCopy(fixture.Fresh("basis"),temporary:true));Require(Rows(reopened,"Rooms")[0].File.Data.Span.SequenceEqual(Join(rooms,tail)),"Basis raw copy changed retained suffix.");
        });
        run("native definition tails: unknown frame between known families does not hide the following family from copy",()=>
        {
            using var fixture=new Fixture();var terrain=Section(0x6E);var unknown=Section(0x7000);var rooms=Section(0x78);var source=fixture.Load(Join(terrain,unknown,rooms));
            var target=NativeMapDocument.Create(8,8,GameName,source);Require(target.Files.Single(file=>file.Signature==0x6E).Data.Span.SequenceEqual(Join(terrain,unknown)),"Contiguous unknown frame was discarded or moved past another known family.");
            Require(target.Files.SingleOrDefault(file=>file.Signature==0x78) is { } room&&room.Data.Span.SequenceEqual(rooms),"Unknown second signature hid the following requested family.");
        });
        run("native definition tails: unsupported inner version stays opaque and cannot become an unreadable standalone basis",()=>
        {
            using var fixture=new Fixture();var future=Section(0x78);Word(future,4,5);var source=fixture.Load(Join(Section(0x6E),future));
            Require(Rows(source,"Rooms").Length!=0&&Rows(source,"Rooms").All(row=>row.Fields.Count==0),"Future section exposed guessed fields.");Reject(()=>NativeMapDocument.Create(8,8,GameName,source));
        });
        run("native definition tails: magic-only junk and embedded valid headers are not resynchronisation points",()=>
        {
            using var fixture=new Fixture();var magic=new byte[96];Word(magic,0,0x78);var bytes=Join(Section(0x6E),magic,Section(0x82));var source=fixture.Load(bytes);
            Require(Rows(source,"Rooms").Length==0&&Rows(source,"Traps").Length==0,"Opaque bytes were searched for embedded family signatures.");Require(Rows(source,"Terrains")[0].File.Data.Span.SequenceEqual(bytes),"Opaque magic-only data changed.");
        });
        run("native definition tails: a coherent known truncated section remains a hard bounds refusal",()=>
        {using var fixture=new Fixture();var truncated=Section(0x78)[..^1];Reject(()=>fixture.Load(Join(Section(0x6E),truncated)));});
        run("native definition tails: DKM requires explicit opaque-data loss consent and keeps exact source archive",()=>
        {
            using var fixture=new Fixture();foreach(var bytes in new[]{Join(Section(0x6E),Section(0x7000),Section(0x78)),Join(Section(0x6E),Tail(96))})
            {
                var source=fixture.Load(bytes);using var session=new EditorSaveSession(source);var path=fixture.Fresh("project")+".dkm";var plan=session.PrepareSaveAs(path);
                Require(!plan.HasBlockingIssues&&plan.NeedsLossConsent&&plan.ConversionIssues.Any(issue=>issue.Code=="OpaqueDefinitionData"&&issue.Severity==ProjectConversionSeverity.Loss),"Unmodelled definition bytes were not explicitly reported.");
                Require(!session.Save(plan,false)&&!File.Exists(path),"Declined loss consent wrote DKM.");Require(session.Save(plan,true),"Accepted source-preserving conversion failed.");var archive=EditorSourceArchive.Read(session.LastSave!.SourceArchives.Single());
                Require(archive.SourceFiles.Single(file=>file.ExportPath.EndsWith(GameName+"Globals.kld",StringComparison.Ordinal)).Bytes.AsSpan().SequenceEqual(bytes),"Source companion lost unmodelled bytes.");
            }
        });
        run("native definition tails: official packing refuses opaque bytes instead of relocating or dropping them",OfficialPacking);
    }
}
