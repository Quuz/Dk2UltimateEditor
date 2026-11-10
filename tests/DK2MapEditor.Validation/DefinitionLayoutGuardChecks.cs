using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class DefinitionLayoutGuardChecks
{
    private const string GameName="DK2Preview_6a88240eaf814f78";
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Word(byte[] bytes,int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset),value);
    private static byte[] Section(LegacyGlobalSchemas.Definition family,int? rows=null,int? stride=null)
    {
        var count=rows??LegacyCompilerRules.DefinitionOrder(family.Group).Count;var size=stride??family.RecordSize;var bytes=new byte[56+count*size];
        Word(bytes,0,family.Signature);Word(bytes,4,4);Word(bytes,8,(uint)bytes.Length);Word(bytes,12,family.Signature+1);Word(bytes,16,28);Word(bytes,20,(uint)count);Word(bytes,24,1);Word(bytes,48,family.Signature+2);Word(bytes,52,(uint)(count*size));
        if(family.Signature==0xAA)for(var i=0;i<count;i++)bytes[56+i*size+0xF4B]=(byte)(i+1);
        return bytes;
    }
    private sealed class Fixture : IDisposable
    {
        public string DirectoryPath {get;}=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2DefinitionHeaders-"+Guid.NewGuid().ToString("N")));
        private int next;
        public Fixture(){Require(DirectoryPath.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Unexpected fixture root.");Directory.CreateDirectory(DirectoryPath);}
        public (NativeMapDocument Document,string DefinitionPath,byte[] Bytes) Create(LegacyGlobalSchemas.Definition family,Action<byte[]>? mutate=null,int? count=null,int? size=null,int tail=0)
        {
            var source=NativeMapDocument.Create(8,8,GameName);var path=source.SaveCopy(Path.Combine(DirectoryPath,"case"+next++),temporary:true);
            var header=File.ReadAllBytes(path);var reference=56+(int)BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(52))+8;
            Word(header,reference,0);new NativeField("Reference",0,64,NativeValueKind.WesternAnsi).Encode("Data\\Editor\\Maps\\"+GameName+"Globals.kld").CopyTo(header,reference+8);File.WriteAllBytes(path,header);
            var prefix=Section(LegacyGlobalSchemas.All.First(row=>row.Signature!=family.Signature&&row.Signature!=0xAA));var probe=Section(family,count,size);mutate?.Invoke(probe);
            var bytes=prefix.Concat(probe).Concat(Enumerable.Range(0,tail).Select(i=>(byte)(0x80+i%127))).ToArray();var definition=Path.Combine(Path.GetDirectoryName(path)!,GameName+"Globals.kld");File.WriteAllBytes(definition,bytes);
            return(NativeMapDocument.Load(path),definition,bytes);
        }
        public void Dispose()=>Directory.Delete(DirectoryPath,recursive:true);
    }
    private static NativeRecord[] Rows(NativeMapDocument document,LegacyGlobalSchemas.Definition family)=>document.Records().Where(row=>row.Category=="Definition."+family.Group).ToArray();
    private static void Opaque(NativeMapDocument document,LegacyGlobalSchemas.Definition family)
    {
        var rows=Rows(document,family);Require(rows.Length!=0&&rows.All(row=>row.Fields.Count==0),"Unverified "+family.Group+" header exposed typed fields.");
        Require(document.GlobalDefinitionRows().Where(row=>row.Family==family.Group).All(row=>!row.Editable),"Named family editor accepted an unverified header.");
        var before=rows[0].File.Data.ToArray();var history=document.GetHistory();
        try{document.SetField(rows[0],family.Fields[0],"1");throw new InvalidOperationException("Opaque fields were editable.");}catch(ArgumentException){}
        Require(rows[0].File.Data.Span.SequenceEqual(before)&&history.Undo.SequenceEqual(document.GetHistory().Undo),"Refused raw edit changed bytes/history.");
    }
    public static void ZeroStride()
    {
        using var fixture=new Fixture();var family=LegacyGlobalSchemas.All.First(row=>row.Signature!=0xAA);
        try{fixture.Create(family,count:1,size:0);throw new InvalidOperationException("A positive definition count with zero record bytes was accepted.");}catch(InvalidDataException){}
    }
    public static void FutureVersions()
    {
        using var fixture=new Fixture();foreach(var family in LegacyGlobalSchemas.All)
        {
            var(doc,path,original)=fixture.Create(family,bytes=>Word(bytes,4,5));Opaque(doc,family);
            var saved=doc.SaveCopy(Path.Combine(fixture.DirectoryPath,"opaque-"+family.Group),temporary:true);var reopened=NativeMapDocument.Load(saved);Opaque(reopened,family);
            Require(Rows(reopened,family)[0].File.Data.Span.SequenceEqual(original)&&File.ReadAllBytes(path).AsSpan().SequenceEqual(original),"Raw Save/reopen changed the unknown inner version or original data.");
        }
    }
    public static void Run(Action<string,Action> run)
    {
        run("definition layout guards: future inner version is opaque across all seven native families",FutureVersions);
        run("definition layout guards: wrong info signature/length and definition marker cannot expose typed fields",()=>
        {
            using var fixture=new Fixture();foreach(var family in LegacyGlobalSchemas.All)foreach(var(offset,value) in new[]{(12,0xDEADu),(16,27u),(48,0xBEEFu)})
            {var(doc,_,_)=fixture.Create(family,bytes=>Word(bytes,offset,value));Opaque(doc,family);}
        });
        run("definition layout guards: verified families and all44/46 by5449/5537 creature prefixes stay editable with exact tails",()=>
        {
            using var fixture=new Fixture();foreach(var family in LegacyGlobalSchemas.All)
            foreach(var shape in family.Signature==0xAA?new (int Count,int Size)[]{(44,5449),(44,5537),(46,5449),(46,5537)}:new (int Count,int Size)[]{(LegacyCompilerRules.DefinitionOrder(family.Group).Count,family.RecordSize)})
            foreach(var tail in new[]{0,13,55})
            {
                var(doc,path,original)=fixture.Create(family,count:shape.Count,size:shape.Size,tail:tail);var rows=Rows(doc,family);
                Require(rows.Length==shape.Count&&rows.All(row=>row.Fields.Count==family.Fields.Count),"Verified common-prefix fields were disabled.");
                Require(doc.GlobalDefinitionRows().Where(row=>row.Family==family.Group&&row.Record is not null).All(row=>row.Editable),"Verified named family was disabled.");
                var field=rows[0].Fields.First(field=>field.BitMask==0&&field.Kind is NativeValueKind.Byte or NativeValueKind.UInt16 or NativeValueKind.Int32);var bytes=rows[0].File.Data.ToArray();
                doc.SetField(rows[0],field,"1");Require(rows[0].Read(field)=="1","Verified raw edit failed.");doc.Undo();Require(rows[0].File.Data.Span.SequenceEqual(bytes),"Undo changed opaque creature extension/tail.");
                var saved=doc.SaveCopy(Path.Combine(fixture.DirectoryPath,"saved-"+Guid.NewGuid().ToString("N")),temporary:true);var copy=NativeMapDocument.Load(saved);Require(Rows(copy,family)[0].File.Data.Span.SequenceEqual(original),"Save/reopen changed opaque native bytes.");Require(File.ReadAllBytes(path).AsSpan().SequenceEqual(original),"Original synthetic definition changed.");
            }
        });
        run("definition layout guards: malformed payload/count remains a refusal before guessed parsing",()=>
        {
            using var fixture=new Fixture();foreach(var family in LegacyGlobalSchemas.All)foreach(var offset in new[]{20,52})
            {try{fixture.Create(family,bytes=>Word(bytes,offset,uint.MaxValue));throw new InvalidOperationException("Malformed native definition was loaded.");}catch(InvalidDataException){}}
        });
        run("definition layout guards: positive count with zero stride is refused before record allocation",ZeroStride);
        run("definition layout guards: unsupported definition blocks native-to-DKM and retains exact source snapshot",()=>
        {
            using var fixture=new Fixture();foreach(var family in LegacyGlobalSchemas.All)
            {
                var(doc,path,original)=fixture.Create(family,bytes=>Word(bytes,16,27));var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var conversion=doc.PrepareProjectConversion();
                Require(conversion.HasBlockingIssues&&conversion.Issues.Any(issue=>issue.Code=="UnverifiedDefinitions"),"Conversion silently projected an unverified definition header.");
                Require(conversion.SourceFiles.Single(file=>file.ExportPath.EndsWith(GameName+"Globals.kld",StringComparison.Ordinal)).Bytes.AsSpan().SequenceEqual(original),"Conversion omitted original unverified section bytes.");
                Require(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&File.ReadAllBytes(path).AsSpan().SequenceEqual(original),"Conversion altered original opaque bytes.");
            }
        });
    }
}
