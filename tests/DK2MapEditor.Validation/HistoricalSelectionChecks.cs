using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class HistoricalSelectionChecks
{
    private static void Expect(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
    private static void U(byte[] bytes,int offset,uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset),value);
    private static uint U(ReadOnlySpan<byte> bytes,int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    private static byte[] Envelope(IEnumerable<LegacySection> sections,uint signature=LegacySectionFile.MapFileSignature)
    { var data=LegacySectionFile.Create(sections).ToArray();U(data,0,signature);return data; }
    public static void Run()
    {
        var directory=Path.Combine(Path.GetTempPath(),"HistoricalSelection-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var tiles=new byte[8+10*10*4];U(tiles,0,10);U(tiles,4,10);
            for(var index=0;index<100;index++)U(tiles,8+index*4,0x01010202);
            for(var version=1;version<=6;version++)
            foreach(var type in Enumerable.Range(0,5))
            foreach(var owner in type==0?new uint[]{1,3}:new uint[]{3})
            {
                var size=version==1?44:version<=3?56:version<=5?60:64;
                var raw=new byte[size];U(raw,0,0x11111111);U(raw,4,0x22222222);
                U(raw,8,(uint)type);U(raw,12,3);U(raw,16,4);U(raw,20,type==0?17u:1u);
                U(raw,24,owner);U(raw,28,345);
                var data=type==0?6u:uint.MaxValue;
                if(version==6) {U(raw,32,72);U(raw,36,data);U(raw,40,0x89ABCDEF);U(raw,44,13);U(raw,48,55);U(raw,52,9);U(raw,56,0xF1234567);U(raw,60,7);}
                else if(version<=3)
                {
                    U(raw,32,data);U(raw,36,0x89ABCDEF);U(raw,40,0x77777777);
                    if(version>=2){U(raw,44,55);U(raw,48,9);U(raw,52,0xF1234567);}
                }
                else {U(raw,32,data);U(raw,36,0x89ABCDEF);U(raw,40,13);U(raw,44,55);U(raw,48,9);U(raw,52,0xF1234567);U(raw,56,7);}
                var expected=new byte[64];U(expected,8,(uint)type);U(expected,12,1);U(expected,16,1);
                U(expected,20,type==0?17u:1u);U(expected,24,owner);U(expected,28,345);
                U(expected,32,version==6?72u:0);
                U(expected,36,type==2&&version<=4?4u:data);U(expected,40,0x89ABCDEF);
                U(expected,44,version<=3?(type==0?9u:0):13u);
                U(expected,48,version<=3?(type==0&&owner==1?19u:0):55u);
                U(expected,52,version>=2?9u:0);U(expected,56,version>=2?0xF1234567:0);U(expected,60,version>=4?7u:0);
                var path=Path.Combine(directory,$"v{version}-t{type}-o{owner}.dkm");
                File.WriteAllBytes(path,Envelope([new(LegacySectionFile.TileSectionSignature,1,tiles),new(NativeMapDocument.LegacyThings,version,raw)]));
                var document=NativeMapDocument.Load(path);var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
                var revision=document.EditRevision;var dirty=document.HasChanges;var history=document.GetHistory();
                var selection=document.CopySelection(new(2,3,4,4));var bytes=SelectionFile.Serialize(selection);
                var parsed=LegacySectionFile.Parse(bytes);var compatible=parsed.Sections.Single(section=>section.Signature==NativeMapDocument.LegacyThings);
                Expect(compatible.Version==6&&compatible.Payload.Span.SequenceEqual(expected),$"Historical v{version} type{type} owner{owner} compatible record differs from literal original-reader mapping.");
                var reopened=SelectionFile.Parse(bytes);
                Expect(SelectionFile.Serialize(reopened).AsSpan().SequenceEqual(bytes),"Historical selection did not preserve its raw enhanced snapshot.");
                // Previous DKCP3 stored common fields without applying old-reader
                // defaults. Its exact representation remains readable; a new Save
                // writes DKCP4 with a corrected base, retaining the original payload.
                var oldCompatible=expected.ToArray();
                if(version<=3){U(oldCompatible,40,0);U(oldCompatible,44,0);U(oldCompatible,48,version>=2?55u:0);}
                if(type==2&&version<=4)U(oldCompatible,36,data);
                var oldBytes=Envelope(parsed.Sections.Select(section=>section.Signature==SelectionFile.ClipboardSection
                    ?new LegacySection(section.Signature,3,section.Payload[..^(4*16)]):section.Signature==NativeMapDocument.LegacyThings
                    ?new LegacySection(section.Signature,6,oldCompatible):section),LegacySectionFile.AreaFileSignature);
                Expect(SelectionFile.Serialize(SelectionFile.Parse(oldBytes)).AsSpan().SequenceEqual(bytes),"Previous DKCP3 historical projection cannot upgrade losslessly.");
                var converted=SelectionFile.ConvertToFormat(selection,false);var target=NativeMapDocument.Create(14,14,"Target");
                target.PasteSelection(new(2,3),converted);
                var native=target.Records().Single(record=>record.File.Signature==0xBE);
                string Value(string name)=>native.Read(native.Fields.Single(field=>field.Name==name));
                if(type==0)
                {
                    Expect(Value("bGlobalBehavior")==U(expected,44).ToString(),"Historical native conversion lost source creature behavior default.");
                    if(owner==1)Expect(Value("bHeroOptions")==((byte)U(expected,40)).ToString()&&Value("bHeroObjective")==U(expected,48).ToString(),"Historical native conversion lost hero options/objective.");
                }
                if(type==2)Expect(Value("bTrapShots")==((byte)U(expected,36)).ToString(),"Historical native conversion lost trap shot upgrade.");
                Expect(document.EditRevision==revision&&document.HasChanges==dirty&&document.GetHistory().Undo.Count==history.Undo.Count
                    &&document.GetHistory().Redo.Count==history.Redo.Count&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Selection projection modified the historical document.");
            }
        }
        finally {Directory.Delete(directory,true);}
    }
}
