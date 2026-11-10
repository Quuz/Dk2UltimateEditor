using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class CreatureLevelModifierReadChecks
{
    public static void Run(string root)
    {
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Reject(byte[] bytes)
        {try{CreatureLevelModifiers.ReadCollection(bytes);}catch(InvalidDataException){return;}throw new Exception("Unverified/truncated level table accepted.");}
        var raw=new byte[56+6*16+16];
        void U(int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(offset),value);
        U(0,0xE6);U(4,4);U(8,56+6*16);U(12,0xE7);U(16,28);U(20,6);U(48,0xE8);U(52,6*16);
        var tuples=new (uint Type,uint Stat,uint Value,uint Level)[]{(65,0,100,2),(65,31,0xffffffff,10),(65,0,250,2),(224,4,60,10),(65,99,7,0),(999,1,2,3),(65,1,777,3)};
        for(var i=0;i<tuples.Length;i++){var row=tuples[i];U(56+i*16,row.Type);U(60+i*16,row.Stat);U(64+i*16,row.Value);U(68+i*16,row.Level);}
        var before=raw.ToArray();var rows=CreatureLevelModifiers.ReadCollection(raw);
        Check(rows.Count==5&&rows[0]==new CreatureLevelModifierRecord(65,0,100,2,56)&&rows[1].PercentBits==uint.MaxValue
            &&rows[2].Statistic==0&&rows[2].Level==2&&rows[2].PercentBits==250,"Literal layout/order/duplicate/unsigned value changed.");
        Check(rows[3].IsObservedPossessionKey&&!rows[3].IsDocumentedLevelKey&&!rows[4].IsDocumentedLevelKey
            &&rows[4].Statistic==99&&rows[4].Level==0,"Possession or unknown key was guessed into normal levels.");
        Check(rows.All(r=>r.PercentBits!=777)&&raw.AsSpan().SequenceEqual(before),"Trailing data was read as records or source bytes normalized.");
        var retained=CreatureLevelModifiers.ReadRetained(raw.AsSpan(56,6*16));Check(retained.Count==5&&retained[0].Offset==0,"Retained source offsets/layout changed.");
        foreach(var offset in new[]{0,4,8,12,16,20,48,52}){var bad=raw.ToArray();bad[offset]^=1;Reject(bad);}
        Reject(raw[..55]);Reject(raw[..(56+6*16-1)]);
        try{CreatureLevelModifiers.ReadRetained(new byte[17]);throw new Exception("Partial retained tuple accepted.");}catch(InvalidDataException){}
        var shared=Path.GetFullPath(Path.Combine(root,"..","Dungeon Keeper 2","Data","editor","GlobalVariables.kwd"));
        var sharedBytes=File.ReadAllBytes(shared);var originalHash=SHA256.HashData(sharedBytes);var actual=CreatureLevelModifiers.ReadCollection(sharedBytes);
        var normal=actual.Where(r=>r.RecordType==65).ToArray();var possession=actual.Where(r=>r.RecordType==224).ToArray();
        Check(normal.Length==288&&possession.Length==32&&normal.All(r=>r.IsDocumentedLevelKey)&&possession.All(r=>r.IsObservedPossessionKey),"Official default data no longer matches the inspected source fixture.");
        Check(normal.Select(r=>(r.Statistic,r.Level)).Distinct().Count()==288&&Enumerable.Range(0,32).All(stat=>Enumerable.Range(2,9).All(level=>normal.Any(r=>r.Statistic==stat&&r.Level==level))),"Official 32×9 key coverage differs.");
        Check(SHA256.HashData(File.ReadAllBytes(shared)).AsSpan().SequenceEqual(originalHash),"Shared installation data changed.");
        var d=NativeMapDocument.CreateNewMap(new("LevelRead","Level Read",20,20,NativeFormat:true));
        foreach(var t in tuples.Take(5))
        {
            var record=d.AddRecord(new("GAMEGLOBALVARIABLE",0xE6,t.Type,16),new(0,0));
            // Generic schemas intentionally omit unused fields. Manufacture
            // literal imported tuples, not a semantic writer for this feature.
            Check(MemoryMarshal.TryGetArray(record.File.Data,out var buffer),"No fixture buffer.");
            var target=buffer.Array!.AsSpan(buffer.Offset+record.Offset,16);
            BinaryPrimitives.WriteUInt32LittleEndian(target[4..],t.Stat);
            BinaryPrimitives.WriteUInt32LittleEndian(target[8..],t.Value);
            BinaryPrimitives.WriteUInt32LittleEndian(target[12..],t.Level);
        }
        var snapshot=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var history=d.GetHistory();var revision=d.EditRevision;var events=0;
        d.Changed+=(_,_)=>events++;d.HistoryChanged+=(_,_)=>events++;
        var sources=d.ReadCreatureLevelModifiers();var map=sources.Single(s=>s.Storage==CreatureLevelModifierStorage.MapVariables);
        Check(map.Records.Count==5&&map.Records[2].PercentBits==250,"Map occurrence source was merged with defaults or duplicate keys.");
        Check(snapshot.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&d.EditRevision==revision&&d.EditorChangeCount==history.EditorChangeCount
            &&d.GetHistory().Undo.SequenceEqual(history.Undo)&&events==0,"Read-only level inventory mutated document/history/events.");
        var conversion=d.PrepareProjectConversion();Check(!conversion.HasBlockingIssues,"Existing retained-variable conversion failed.");
        var projected=conversion.Project.ReadCreatureLevelModifiers().Single(s=>s.Storage==CreatureLevelModifierStorage.RetainedNativeSource);
        Check(projected.Records.Count==5&&projected.Records.Select(r=>(r.RecordType,r.Statistic,r.PercentBits,r.Level)).SequenceEqual(map.Records.Select(r=>(r.RecordType,r.Statistic,r.PercentBits,r.Level))),"DKM retained occurrences were lost, renumbered or merged.");
        Check(snapshot.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Read/conversion wrote the source map.");
        Console.WriteLine("CREATURE LEVEL READ DETAIL bounded 16-byte official tuples, exact unsigned/duplicate/unknown/possession keys, declared section versus opaque trailer, malformed headers; real shared32×9+32 data, map and DKM retained sources without merge/write/events. I12 editor/writer/ranges remain open; no GUI/game");
    }
}
