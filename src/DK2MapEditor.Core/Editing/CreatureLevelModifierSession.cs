using System.Buffers.Binary;
using System.Security.Cryptography;

namespace DK2MapEditor.Core.Editing;

/// <summary>A private editor of existing map tuples, never the installation's
/// shared defaults. A complete source is required for the ten-field UI draft.</summary>
public sealed class CreatureLevelModifierSession : IDisposable
{
    internal CreatureLevelModifierSession(NativeMapDocument owner,long revision,byte[] fingerprint,CreatureLevelModifierDraft draft,IReadOnlyList<CreatureLevelModifierRecord> defaults)
    {Owner=owner;Revision=revision;Fingerprint=fingerprint;Draft=draft;Defaults=defaults;}
    internal NativeMapDocument Owner {get;}
    internal long Revision {get;}
    internal byte[] Fingerprint {get;}
    internal bool Applying {get;set;}
    internal IReadOnlyList<CreatureLevelModifierRecord> Defaults {get;}
    public CreatureLevelModifierDraft Draft {get;}
    public bool IsClosed=>Draft.IsClosed;
    public CreatureLevelModifierValues? DefaultValues
    {
        get
        {
            if(IsClosed)throw new ObjectDisposedException(nameof(CreatureLevelModifierSession));
            var rows=Defaults.Where(row=>row.Statistic==Draft.SelectedStatistic&&(row.IsDocumentedLevelKey||row.IsObservedPossessionKey))
                .GroupBy(row=>(row.RecordType,row.Level)).ToDictionary(group=>group.Key,group=>group.Last());
            if(Enumerable.Range(2,9).Any(level=>!rows.ContainsKey((65u,(uint)level)))||!rows.ContainsKey((224u,10u)))return null;
            return new(Array.AsReadOnly(Enumerable.Range(2,9).Select(level=>unchecked((int)(short)rows[(65u,(uint)level)].PercentBits)).ToArray()),unchecked((short)rows[(224u,10u)].PercentBits));
        }
    }
    public void ResetSelectedToDefaults()
    {
        if(!Owner.IsCreatureLevelModifierSessionCurrent(this))throw new InvalidOperationException("Refresh level modifiers before reset.");
        Draft.Store(DefaultValues??throw new InvalidOperationException("No complete shared default page is available."));
    }
    /// <summary>Project the complete private view to a new shared catalogue
    /// buffer. This does not apply map edits or publish a file.</summary>
    public byte[] CreateSharedCatalogueCopy(CreatureLevelModifierCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        if(!Owner.IsCreatureLevelModifierSessionCurrent(this))throw new InvalidOperationException("Refresh level modifiers before exporting a catalogue.");
        if(!catalogue.HasCompleteVisibleTable)throw new InvalidDataException("A complete shared variable catalogue is required.");
        var source=catalogue.Records.GroupBy(row=>new CreatureLevelModifierKey(row.RecordType,row.Statistic,row.Level)).ToDictionary(group=>group.Key,group=>group.Last());
        var captured=Defaults.GroupBy(row=>new CreatureLevelModifierKey(row.RecordType,row.Statistic,row.Level)).ToDictionary(group=>group.Key,group=>group.Last());
        var changes=new List<CreatureLevelModifierChange>();
        foreach(var value in Draft.SnapshotValues())
        {
            if(!source.TryGetValue(value.Key,out var row)||!captured.TryGetValue(value.Key,out var original)||original.PercentBits!=row.PercentBits)
                throw new InvalidOperationException("Export from the catalogue snapshot used to open this context.");
            if(value.Value!=unchecked((short)row.PercentBits))changes.Add(new(value.Key,row.PercentBits,value.Value));
        }
        return CreatureLevelModifierWriter.PatchCollection(catalogue.CopyBytes(),changes);
    }
    public void Dispose()=>Draft.Dispose();
}

public sealed partial class NativeMapDocument
{
    // D2LM v1 is editor-only DKM metadata, not an official game section.
    // SHA256(original retained bytes), count, reserved; then 24-byte deltas:
    // type/statistic/level/retained offset/original DWORD/edited DWORD.
    internal const uint LevelModifierDeltaSignature=0x4D4C3244;
    private const int LevelModifierDeltaHeader=52;
    private NativeFile? nativeLevelModifierDelta;
    private byte[]? originalLevelModifierDelta;
    private NativeFile LevelModifierDeltaFile
    {
        get
        {
            if(nativeLevelModifierDelta is null)
            {
                var bytes=new byte[LevelModifierDeltaHeader];WriteUInt32(bytes,0,LevelModifierDeltaSignature);
                WriteUInt32(bytes,4,1);WriteUInt32(bytes,8,LevelModifierDeltaHeader-12);
                nativeLevelModifierDelta=new(MainFile.SourcePath+"#CreatureLevelModifierDelta",MainFile.ExportPath+"#CreatureLevelModifierDelta",bytes);
                originalLevelModifierDelta=bytes.ToArray();
            }
            return nativeLevelModifierDelta;
        }
    }
    private bool LevelModifierDeltaChanged=>IsLegacyProject&&!LevelModifierDeltaFile.Data.Span.SequenceEqual(originalLevelModifierDelta);
    public bool HasCreatureLevelModifierOverrides=>IsLegacyProject&&(ReadLevelModifierDelta().Count!=0||ReadOwnedLevelModifiers().Count!=0);
    private readonly record struct LevelDelta(int Offset,CreatureLevelModifierKey Key,uint OriginalBits,uint Bits);

    private void LoadLevelModifierDelta(byte[] bytes)
    {
        if(MainFile.Signature!=Formats.LegacySectionFile.MapFileSignature)throw new InvalidDataException("Level modifier extensions require a DKM map project.");
        if(nativeLevelModifierDelta is not null)throw new InvalidDataException("Duplicate DKM level modifier extension.");
        nativeLevelModifierDelta=new(MainFile.SourcePath+"#CreatureLevelModifierDelta",MainFile.ExportPath+"#CreatureLevelModifierDelta",bytes);
        originalLevelModifierDelta=bytes.ToArray();_ = ReadLevelModifierDelta();
    }
    private IReadOnlyList<LevelDelta> ReadLevelModifierDelta()
    {
        var file=LevelModifierDeltaFile;var bytes=file.Data.Span;
        if(bytes.Length<LevelModifierDeltaHeader||file.UInt32(0)!=LevelModifierDeltaSignature||file.UInt32(4)!=1
            ||file.UInt32(8)!=bytes.Length-12||file.UInt32(48)!=0)
            throw new InvalidDataException("Invalid DKM level modifier extension.");
        var count=file.UInt32(44);
        if(count>230||LevelModifierDeltaHeader+count*24L!=bytes.Length)
            throw new InvalidDataException("Invalid DKM level modifier delta count.");
        if(count==0&&bytes.Slice(12,32).IndexOfAnyExcept((byte)0)>=0)
            throw new InvalidDataException("An empty level modifier extension must not claim a source hash.");
        var rows=new List<LevelDelta>();var keys=new HashSet<CreatureLevelModifierKey>();var offsets=new HashSet<int>();
        for(var offset=LevelModifierDeltaHeader;offset<bytes.Length;offset+=24)
        {
            var key=new CreatureLevelModifierKey(file.UInt32(offset),file.UInt32(offset+4),file.UInt32(offset+8));
            var sourceOffset=file.UInt32(offset+12);var original=file.UInt32(offset+16);var bits=file.UInt32(offset+20);
            // Reuse the actual writer's key/range/signed encoding guards.
            byte[] record;
            try{record=CreatureLevelModifierWriter.CreateRecord(key,unchecked((short)bits));}
            catch(ArgumentException error){throw new InvalidDataException("Invalid DKM level modifier key or value.",error);}
            if(sourceOffset>160000-16||sourceOffset%16!=0||BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(8))!=bits
                ||unchecked((short)original)==unchecked((short)bits)||!keys.Add(key)||!offsets.Add((int)sourceOffset))
                throw new InvalidDataException("Invalid DKM level modifier delta entry.");
            rows.Add(new((int)sourceOffset,key,original,bits));
        }
        return rows.AsReadOnly();
    }
    private byte[] EffectiveRetainedLevelVariables()
    {
        var rows=ReadLevelModifierDelta();var output=nativeRetainedVariables.ToArray();
        if(rows.Count==0)return output;
        if(!SHA256.HashData(nativeRetainedVariables).AsSpan().SequenceEqual(LevelModifierDeltaFile.Data.Span.Slice(12,32)))
            throw new InvalidDataException("The level modifier extension requires its exact original native source companion.");
        var source=CreatureLevelModifiers.ReadRetained(nativeRetainedVariables)
            .GroupBy(row=>new CreatureLevelModifierKey(row.RecordType,row.Statistic,row.Level)).ToDictionary(group=>group.Key,group=>group.Last());
        foreach(var row in rows)
        {
            if(!source.TryGetValue(row.Key,out var original)||original.Offset!=row.Offset||original.PercentBits!=row.OriginalBits)
                throw new InvalidDataException("A level modifier delta does not match the last original source occurrence.");
            WriteUInt32(output,row.Offset+8,row.Bits);
        }
        return output;
    }

    /// <summary>Existing editable map tuples with this project's explicit
    /// deltas. Shared defaults are not merged and missing values are not guessed.</summary>
    public IReadOnlyList<CreatureLevelModifierRecord> ReadExistingCreatureLevelModifiers()
        =>IsLegacyProject?Array.AsReadOnly(CreatureLevelModifiers.ReadRetained(EffectiveRetainedLevelVariables()).Concat(ReadOwnedLevelModifiers()).ToArray())
            :MapVariableFile is { } file?CreatureLevelModifiers.ReadCollection(file.Data.Span):[];

    public CreatureLevelModifierSession CreateCreatureLevelModifierSession()
    {
        return CreateCreatureLevelModifierSession(ReadCreatureLevelModifierDefaults());
    }
    public bool IsCreatureLevelModifierSessionCurrent(CreatureLevelModifierSession session)
        =>session is not null&&!session.IsClosed&&!session.Applying&&ReferenceEquals(session.Owner,this)&&!IsSaveLocked
            &&session.Revision==EditRevision&&UnusedScriptFingerprint().AsSpan().SequenceEqual(session.Fingerprint);
    public void ApplyCreatureLevelModifiers(CreatureLevelModifierSession session)
    {
        if(!IsCreatureLevelModifierSessionCurrent(session))throw new InvalidOperationException("Refresh level modifiers after source changes or Cancel.");
        var changes=session.Draft.Changes;session.Applying=true;
        try{ApplyLevelModifiersWithDefaults(changes,session.Defaults);session.Dispose();}
        finally{session.Applying=false;}
    }

    /// <summary>One atomic change of existing map values. Shared installation
    /// collections and immutable archived originals are never targets.</summary>
    public void SetExistingCreatureLevelModifiers(IEnumerable<CreatureLevelModifierChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        if(activePenStroke is not null)throw new InvalidOperationException("Finish the active pen gesture before changing level modifiers.");
        var selected=changes.ToArray();
        if(!IsLegacyProject)
        {
            var file=MapVariableFile??throw new InvalidOperationException("The map variable collection is missing.");
            var bytes=CreatureLevelModifierWriter.PatchCollection(file.Data.Span,selected);
            Edit("Change creature level modifiers",()=>file.Bytes=bytes);return;
        }
        if(MainFile.Signature!=Formats.LegacySectionFile.MapFileSignature)throw new InvalidOperationException("Level modifiers require a DKM map project.");
        var current=EffectiveRetainedLevelVariables();var owned=LevelModifierOwnedFile;_=ReadOwnedLevelModifiers();
        // The bounded ordinary E6 codec validates every request before Edit.
        var dataLength=current.Length+owned.Length-16;
        var collection=new byte[56+dataLength];WriteUInt32(collection,0,0xE6);WriteUInt32(collection,4,4);WriteUInt32(collection,8,(uint)collection.Length);
        WriteUInt32(collection,12,0xE7);WriteUInt32(collection,16,28);WriteUInt32(collection,20,(uint)(dataLength/16));
        WriteUInt32(collection,48,0xE8);WriteUInt32(collection,52,(uint)dataLength);current.CopyTo(collection,56);owned.Data.Span[16..].CopyTo(collection.AsSpan(56+current.Length));
        var edited=CreatureLevelModifierWriter.PatchCollection(collection,selected);
        var deltas=new List<LevelDelta>();
        var last=CreatureLevelModifiers.ReadRetained(nativeRetainedVariables)
            .GroupBy(row=>new CreatureLevelModifierKey(row.RecordType,row.Statistic,row.Level)).Select(group=>group.Last());
        foreach(var original in last)
        {
            var bits=BinaryPrimitives.ReadUInt32LittleEndian(edited.AsSpan(56+original.Offset+8));
            if(unchecked((short)bits)==unchecked((short)original.PercentBits))continue;
            deltas.Add(new(original.Offset,new(original.RecordType,original.Statistic,original.Level),original.PercentBits,bits));
        }
        var output=new byte[LevelModifierDeltaHeader+deltas.Count*24];WriteUInt32(output,0,LevelModifierDeltaSignature);
        WriteUInt32(output,4,1);WriteUInt32(output,8,(uint)output.Length-12);WriteUInt32(output,44,(uint)deltas.Count);
        if(deltas.Count!=0)SHA256.HashData(nativeRetainedVariables).CopyTo(output,12);
        var position=LevelModifierDeltaHeader;
        foreach(var row in deltas.OrderBy(row=>row.Offset))
        {
            WriteUInt32(output,position,row.Key.RecordType);WriteUInt32(output,position+4,row.Key.Statistic);WriteUInt32(output,position+8,row.Key.Level);
            WriteUInt32(output,position+12,(uint)row.Offset);WriteUInt32(output,position+16,row.OriginalBits);WriteUInt32(output,position+20,row.Bits);position+=24;
        }
        var target=LevelModifierDeltaFile;
        var ownedOutput=owned.Data.ToArray();edited.AsSpan(56+current.Length).CopyTo(ownedOutput.AsSpan(16));
        Edit("Change creature level modifiers",()=>{target.Bytes=output;owned.Bytes=ownedOutput;});
    }
    internal void PublishCreatureLevelModifierSource(string projectPath)
    {
        if(!HasCreatureLevelModifierOverrides)return;
        _=EffectiveRetainedLevelVariables();
        if(nativePayloadSource is { } archive){archive.Publish(projectPath,null);return;}
        if(ReadLevelModifierDelta().Count!=0||nativeRetainedVariables.Length!=0)
            throw new InvalidDataException("Save level modifier edits with their original native source companion.");
    }
}
