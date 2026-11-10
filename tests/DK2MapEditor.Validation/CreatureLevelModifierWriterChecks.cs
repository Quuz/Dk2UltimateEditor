using System.Buffers.Binary;
using System.Security.Cryptography;
using DK2MapEditor.Core.Editing;

internal static class CreatureLevelModifierWriterChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidDataException or InvalidOperationException){return;}throw new Exception("An invalid level write was accepted.");}
    private static uint N(byte[] bytes,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset,4));
    private static void U(byte[] bytes,int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset,4),value);
    public static void Run(string root)
    {
        // Literal byte expectations, independently specified from x86 MOVSX
        // and the zero->100 branch, including both negative endpoints.
        foreach(var pair in new (int Value,uint Bits)[]{(-10000,0xffffd8f0),(-1,0xffffffff),(0,100),(1,1),(100,100),(10000,10000)})
        {
            Check(CreatureLevelModifierWriter.EncodeEditedValue(pair.Value)==pair.Bits,"Signed DWORD / zero sentinel encoding differs.");
            var normal=CreatureLevelModifierWriter.CreateRecord(new(65,0,2),pair.Value);
            var possession=CreatureLevelModifierWriter.CreateRecord(new(224,31,10),pair.Value);
            Check(normal.Length==16&&N(normal,0)==65&&N(normal,4)==0&&N(normal,8)==pair.Bits&&N(normal,12)==2,"Normal wire tuple differs.");
            Check(possession.Length==16&&N(possession,0)==224&&N(possession,4)==31&&N(possession,8)==pair.Bits&&N(possession,12)==10,"First Person tuple differs.");
        }
        foreach(var value in new[]{int.MinValue,-32768,-10001,10001,32767,int.MaxValue})Reject(()=>CreatureLevelModifierWriter.EncodeEditedValue(value));
        foreach(var key in new CreatureLevelModifierKey[]{new(65,8,2),new(65,32,2),new(65,0,1),new(65,0,11),new(224,0,2),new(1,0,2)})
            Reject(()=>CreatureLevelModifierWriter.CreateRecord(key,1));
        var path=Path.GetFullPath(Path.Combine(root,"..","Dungeon Keeper 2","Data","editor","GlobalVariables.kwd"));
        var source=File.ReadAllBytes(path);var hash=SHA256.HashData(source);
        var rows=CreatureLevelModifiers.ReadCollection(source);
        using var draft=new CreatureLevelModifierDraft(rows);
        foreach(var stat in CreatureLevelModifierControls.Statistics.Select(s=>uint.Parse(s.Value)))
        {
            draft.SelectStatistic(stat,draft.Current);
            var values=Enumerable.Range(0,9).Select(i=>i%3==0?-10000:i%3==1?0:10000).ToArray();
            draft.Store(new(values,-1));
        }
        var changes=draft.Changes;
        var output=CreatureLevelModifierWriter.PatchCollection(source,changes);
        var expected=source.ToArray();
        foreach(var change in changes)
        {
            var row=rows.Last(r=>r.RecordType==change.Key.RecordType&&r.Statistic==change.Key.Statistic&&r.Level==change.Key.Level);
            var bits=unchecked((uint)(change.Value==0?100:(int)change.Value));
            // Preserve exact original bytes when zero emits the existing 100.
            if(unchecked((short)bits)!=unchecked((short)row.PercentBits))U(expected,row.Offset+8,bits);
        }
        Check(output.AsSpan().SequenceEqual(expected)&&source.AsSpan().SequenceEqual(File.ReadAllBytes(path)),"Visible edits changed unrelated bytes or the installation.");
        Check(output.AsSpan(9992).SequenceEqual(source.AsSpan(9992)),"Opaque 3644-byte extension changed.");
        var reread=CreatureLevelModifiers.ReadCollection(output);
        foreach(var change in changes)
        {
            var row=reread.Last(r=>r.RecordType==change.Key.RecordType&&r.Statistic==change.Key.Statistic&&r.Level==change.Key.Level);
            Check(CreatureLevelModifierControls.DisplayValue(row.PercentBits)==(change.Value==0?100:change.Value),"Written values did not reload with official signed semantics.");
        }
        foreach(var hidden in rows.Where(r=>!CreatureLevelModifierControls.HasStatistic(r.Statistic)))
            Check(source.AsSpan(hidden.Offset,16).SequenceEqual(output.AsSpan(hidden.Offset,16)),"A hidden statistic changed.");
        Check(CreatureLevelModifierWriter.PatchCollection(source,[]).AsSpan().SequenceEqual(source),"An empty request normalized the source.");
        // A private buffer with duplicates/noncanonical bits/unknown records
        // must preserve all but the explicitly edited last value DWORD.
        var raw=new byte[56+6*16+11];U(raw,0,230);U(raw,4,4);U(raw,8,56+6*16);U(raw,12,231);U(raw,16,28);U(raw,20,6);U(raw,48,232);U(raw,52,6*16);
        uint[][] tuples=[[65,0,7,2],[999,2,3,4],[65,0,0x0000ffff,2],[224,0,50,10],[65,8,4,2],[65,1,0xabcd0064,3]];
        for(var i=0;i<tuples.Length;i++)for(var j=0;j<4;j++)U(raw,56+i*16+j*4,tuples[i][j]);
        raw.AsSpan(56+6*16).Fill(0xa5);var before=raw.ToArray();
        var edit=new CreatureLevelModifierChange(new(65,0,2),0x0000ffff,-7);
        var patched=CreatureLevelModifierWriter.PatchCollection(raw,[edit]);var wanted=raw.ToArray();U(wanted,56+2*16+8,0xfffffff9);
        Check(patched.AsSpan().SequenceEqual(wanted)&&raw.AsSpan().SequenceEqual(before),"The final duplicate was not the sole write.");
        foreach(var value in new short[]{-1})
            Check(CreatureLevelModifierWriter.PatchCollection(raw,[edit with{Value=value}]).AsSpan().SequenceEqual(raw),"Equal SHORT input normalized upper DWORD bits.");
        Check(CreatureLevelModifierWriter.PatchCollection(raw,[new(new(65,1,3),0xabcd0064,0)]).AsSpan().SequenceEqual(raw),"Zero->100 normalized an existing equivalent DWORD.");
        Reject(()=>CreatureLevelModifierWriter.PatchCollection(raw,[edit,edit]));
        Reject(()=>CreatureLevelModifierWriter.PatchCollection(raw,[edit,edit with{Key=new(65,1,2)}]));
        Reject(()=>CreatureLevelModifierWriter.PatchCollection(raw,[edit with{OriginalBits=uint.MaxValue}]));
        Reject(()=>CreatureLevelModifierWriter.PatchCollection(raw,[edit with{Value=10001}]));
        Reject(()=>CreatureLevelModifierWriter.PatchCollection(raw,[edit with{Key=new(65,8,2),OriginalBits=4}]));
        Reject(()=>CreatureLevelModifierWriter.PatchCollection(raw[..55],[edit]));
        Check(raw.AsSpan().SequenceEqual(before),"A refused batch partially changed caller data.");
        patched[0]=0;Check(raw[0]==230,"Writer output aliases the input array.");
        Check(SHA256.HashData(File.ReadAllBytes(path)).AsSpan().SequenceEqual(hash),"Shared installation data changed.");
        Console.WriteLine("CREATURE LEVEL WRITER DETAIL static official signed DWORD/zero->100 semantics; real shared catalogue draft->write->reload over all visible pages, last duplicate only, byte-exact hidden/unknown/header/opaque retention, complete batch/range/key/stale validation and independent output; no document publication, DKM archive write or GUI/game");
    }
}
