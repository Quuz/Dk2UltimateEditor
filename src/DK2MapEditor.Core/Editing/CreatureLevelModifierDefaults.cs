using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    // D2LO stores typed editor-created map records, independently of D2LM's
    // immutable original occurrences. It never claims these are source bytes.
    internal const uint LevelModifierOwnedSignature=0x4F4C3244;
    private NativeFile? nativeLevelModifierOwned;
    private byte[]? originalLevelModifierOwned;
    private NativeFile LevelModifierOwnedFile
    {
        get
        {
            if(nativeLevelModifierOwned is null)
            {
                var bytes=new byte[16];WriteUInt32(bytes,0,LevelModifierOwnedSignature);WriteUInt32(bytes,4,1);WriteUInt32(bytes,8,4);
                nativeLevelModifierOwned=new(MainFile.SourcePath+"#CreatureLevelModifierOwned",MainFile.ExportPath+"#CreatureLevelModifierOwned",bytes);
                originalLevelModifierOwned=bytes.ToArray();
            }
            return nativeLevelModifierOwned;
        }
    }
    private bool LevelModifierOwnedChanged=>IsLegacyProject&&!LevelModifierOwnedFile.Data.Span.SequenceEqual(originalLevelModifierOwned);
    private void LoadLevelModifierOwned(byte[] bytes)
    {
        if(MainFile.Signature!=Formats.LegacySectionFile.MapFileSignature||nativeLevelModifierOwned is not null)
            throw new InvalidDataException("Duplicate or misplaced DKM level override section.");
        nativeLevelModifierOwned=new(MainFile.SourcePath+"#CreatureLevelModifierOwned",MainFile.ExportPath+"#CreatureLevelModifierOwned",bytes);
        originalLevelModifierOwned=bytes.ToArray();_=ReadOwnedLevelModifiers();
    }
    private IReadOnlyList<CreatureLevelModifierRecord> ReadOwnedLevelModifiers()
    {
        var file=LevelModifierOwnedFile;
        if(file.Length<16||file.Signature!=LevelModifierOwnedSignature||file.UInt32(4)!=1||file.UInt32(8)!=file.Length-12
            ||file.UInt32(12)>230||16+file.UInt32(12)*16L!=file.Length)
            throw new InvalidDataException("Invalid DKM level override section.");
        var sourceKeys=CreatureLevelModifiers.ReadRetained(nativeRetainedVariables).Select(Key).ToHashSet();
        var keys=new HashSet<CreatureLevelModifierKey>();var rows=CreatureLevelModifiers.ReadRetained(file.Data.Span[16..]);
        if(rows.Count!=file.UInt32(12))throw new InvalidDataException("A level override section contains unrelated records.");
        foreach(var row in rows)
        {
            byte[] encoded;
            try{encoded=CreatureLevelModifierWriter.CreateRecord(Key(row),unchecked((short)row.PercentBits));}
            catch(ArgumentException error){throw new InvalidDataException("Invalid owned level override key/value.",error);}
            if(!keys.Add(Key(row))||sourceKeys.Contains(Key(row))||BinaryPrimitives.ReadUInt32LittleEndian(encoded.AsSpan(8))!=row.PercentBits)
                throw new InvalidDataException("An owned level override is duplicate, noncanonical or collides with its original source.");
        }
        return Array.AsReadOnly(rows.Select(row=>row with{Offset=row.Offset+16}).ToArray());
    }
    private static CreatureLevelModifierKey Key(CreatureLevelModifierRecord row)=>new(row.RecordType,row.Statistic,row.Level);

    /// <summary>Read the explicit shared catalogue already loaded with this
    /// document, or preserved in its original native archive. No file I/O and
    /// no invented defaults; a caller may explicitly supply another snapshot.</summary>
    public IReadOnlyList<CreatureLevelModifierRecord> ReadCreatureLevelModifierDefaults()
    {
        if(!IsLegacyProject)
        {
            var matches=files.Where(file=>file.Signature==0xE6&&Path.GetFileName(file.ExportPath).Equals("GlobalVariables.kwd",StringComparison.OrdinalIgnoreCase)).ToArray();
            if(matches.Length>1)throw new InvalidDataException("Ambiguous shared level modifier catalogues.");
            return matches.Length==0?[]:CreatureLevelModifiers.ReadCollection(matches[0].Data.Span);
        }
        var archived=nativePayloadSource?.SourceFiles.Where(file=>Path.GetFileName(file.ExportPath).Equals("GlobalVariables.kwd",StringComparison.OrdinalIgnoreCase)).ToArray()??[];
        if(archived.Length>1)throw new InvalidDataException("Ambiguous archived shared level modifier catalogues.");
        return archived.Length==0?[]:CreatureLevelModifiers.ReadCollection(archived[0].Bytes.AsSpan());
    }
    public CreatureLevelModifierSession CreateCreatureLevelModifierSession(IEnumerable<CreatureLevelModifierRecord> defaults)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        if(IsSaveLocked)throw new InvalidOperationException("Finish the current edit before opening level modifiers.");
        var snapshot=Array.AsReadOnly(defaults.ToArray());
        var draft=new CreatureLevelModifierDraft(snapshot.Concat(ReadExistingCreatureLevelModifiers()));
        return new(this,EditRevision,UnusedScriptFingerprint(),draft,snapshot);
    }
    private void ApplyLevelModifiersWithDefaults(IReadOnlyList<CreatureLevelModifierChange> changes,IReadOnlyList<CreatureLevelModifierRecord> defaults)
    {
        var existing=ReadExistingCreatureLevelModifiers().Select(Key).ToHashSet();
        var inherited=defaults.Where(row=>CreatureLevelModifierControls.HasStatistic(row.Statistic)&&(row.IsDocumentedLevelKey||row.IsObservedPossessionKey))
            .GroupBy(Key).ToDictionary(group=>group.Key,group=>group.Last());
        var additions=new List<byte[]>();var updates=new List<CreatureLevelModifierChange>();
        foreach(var change in changes)
        {
            if(existing.Contains(change.Key)){updates.Add(change);continue;}
            var record=CreatureLevelModifierWriter.CreateRecord(change.Key,change.Value);
            if(!inherited.TryGetValue(change.Key,out var value)||value.PercentBits!=change.OriginalBits)
                throw new InvalidOperationException("The new level override requires its captured shared value.");
            if(unchecked((short)BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(8)))!=unchecked((short)value.PercentBits))additions.Add(record);
        }
        // Validate updates, source topology and D2LO capacity before mutation.
        if(updates.Count!=0){var replay=CreateCompilationSnapshot();replay.SetExistingCreatureLevelModifiers(updates);}
        if(IsLegacyProject&&ReadOwnedLevelModifiers().Count+additions.Count>230)throw new InvalidDataException("Too many owned level overrides.");
        if(!IsLegacyProject&&MapVariableFile is null)throw new InvalidOperationException("The map variable collection is missing.");
        if(IsLegacyProject&&MainFile.Signature!=Formats.LegacySectionFile.MapFileSignature)throw new InvalidOperationException("Level overrides require a DKM map project.");
        Edit("Change creature level modifiers",()=>
        {
            if(updates.Count!=0)SetExistingCreatureLevelModifiers(updates);
            if(additions.Count==0)return;
            if(!IsLegacyProject){foreach(var record in additions)InsertRecord(MapVariableFile!,record);return;}
            var target=LevelModifierOwnedFile;var bytes=new byte[target.Length+additions.Count*16];target.Data.Span.CopyTo(bytes);
            var offset=target.Length;foreach(var record in additions){record.CopyTo(bytes,offset);offset+=16;}
            WriteUInt32(bytes,8,(uint)bytes.Length-12);WriteUInt32(bytes,12,target.UInt32(12)+(uint)additions.Count);target.Bytes=bytes;
        });
    }
    private static void VerifyPropertiesLevelOccurrences(IReadOnlyList<NativeRecord> before,IReadOnlyList<NativeRecord> after)
    {
        CreatureLevelModifierKey RecordKey(NativeRecord row)=>new(row.File.UInt32(row.Offset),row.File.UInt32(row.Offset+4),row.File.UInt32(row.Offset+12));
        bool Visible(NativeRecord row)
        {
            if(row.Length!=16)return false;var key=RecordKey(row);
            return CreatureLevelModifierControls.HasStatistic(key.Statistic)&&(key.RecordType==65&&key.Level is >=2 and <=10||key.RecordType==224&&key.Level==10);
        }
        var last=before.Where(Visible).GroupBy(RecordKey).ToDictionary(group=>group.Key,group=>group.Last());
        if(after.Count<before.Count)throw new InvalidOperationException("Map properties must retain original variable occurrences.");
        for(var i=0;i<before.Count;i++)
        {
            var old=before[i];var next=after[i];var a=old.File.Data.Span.Slice(old.Offset,old.Length);var b=next.File.Data.Span.Slice(next.Offset,next.Length);
            if(a.SequenceEqual(b))continue;
            if(!Visible(old)||old.Length!=next.Length||!a[..8].SequenceEqual(b[..8])||!a[12..].SequenceEqual(b[12..])||!ReferenceEquals(last[RecordKey(old)],old))
                throw new InvalidOperationException("Map properties must retain unknown variables, early duplicates and level keys.");
            var bits=next.File.UInt32(next.Offset+8);var original=old.File.UInt32(old.Offset+8);
            var encoded=CreatureLevelModifierWriter.CreateRecord(RecordKey(next),unchecked((short)bits));
            if(BinaryPrimitives.ReadUInt32LittleEndian(encoded.AsSpan(8))!=bits||unchecked((short)bits)==unchecked((short)original))
                throw new InvalidOperationException("Map properties contain an invalid or normalized level value.");
        }
        var added=new HashSet<CreatureLevelModifierKey>();
        foreach(var row in after.Skip(before.Count))
        {
            if(!Visible(row)||last.ContainsKey(RecordKey(row))||!added.Add(RecordKey(row)))throw new InvalidOperationException("Only new visible level overrides may be appended.");
            var encoded=CreatureLevelModifierWriter.CreateRecord(RecordKey(row),unchecked((short)row.File.UInt32(row.Offset+8)));
            if(!encoded.AsSpan().SequenceEqual(row.File.Data.Span.Slice(row.Offset,16)))throw new InvalidOperationException("A new level override is not canonical.");
        }
    }
}
