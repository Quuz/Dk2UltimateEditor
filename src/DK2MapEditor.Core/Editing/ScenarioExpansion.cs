using System.Buffers.Binary;
using System.Security.Cryptography;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    // Native 24-byte events have no expanded member. Keep presentation state
    // in this document; only conversion writes it to the verified DKM member.
    private readonly HashSet<ushort> nativeExpandedConditions=[];
    private bool HasSemanticFileChanges(NativeFile file)
    {
        var current=file.Data.Span;var original=originals[file].AsSpan();
        if(current.SequenceEqual(original))return false;
        if(!IsLegacyProject||file.Signature!=LegacyEvents||current.Length<24||current.Length!=original.Length
            ||!current[..24].SequenceEqual(original[..24])||file.UInt32(4) is not (2 or 3))return true;
        var links=file.UInt32(12);var conditions=file.UInt32(16);var actions=file.UInt32(20);
        var first=24L+links*LegacyProjectSchemas.Size(file.UInt32(4)==2?"TRIGGERLINKV2":"TRIGGERLINK");
        var end=first+(conditions+(long)actions)*56+146*64;
        if(first>current.Length||end!=current.Length)return true;
        var start=0;
        for(var i=0u;i<conditions;i++)
        {
            var expansion=checked((int)(first+i*56L+14));
            if(!current[start..expansion].SequenceEqual(original[start..expansion]))return true;
            start=expansion+2;
        }
        return !current[start..].SequenceEqual(original[start..]);
    }
    public IReadOnlyDictionary<ushort,bool> ReadScenarioExpansion()
    {
        var graph=ScriptGraph.Build(this);
        if(graph.DuplicateIDs.Count!=0)return new Dictionary<ushort,bool>();
        return graph.Events.Values.Where(item=>VerifiedExpansionRecord(item.Record)).ToDictionary(item=>item.ID,
            item=>IsLegacyProject?BinaryPrimitives.ReadInt16LittleEndian(item.Record.File.Data.Span[(item.Record.Offset+14)..])!=0:nativeExpandedConditions.Contains(item.ID));
    }
    private bool VerifiedExpansionRecord(NativeRecord record)=>IsLegacyProject
        ? record.Category=="Project.TRIGGER"&&record.Length==56&&record.File.Signature==LegacyEvents&&record.File.UInt32(4) is 2 or 3
        : record.File.Signature==0xD2&&record.Length==24&&record.File.UInt32(4)==4&&record.File.UInt32(record.Offset)==0xD5;
    /// <summary>Source TVN_ITEMEXPANDED: write exactly one short, without an
    /// editor command, Undo entry, activity event, revision or Changed callback.</summary>
    public bool SetScenarioExpanded(ushort id,bool expanded)
    {
        if(transactionOpen||activePenStroke is not null)throw new InvalidOperationException("Finish the active edit before changing scenario presentation.");
        var graph=ScriptGraph.Build(this);RequireUnambiguousGraph(graph);
        if(id==0||!graph.Events.TryGetValue(id,out var item)||!VerifiedExpansionRecord(item.Record))
            throw new InvalidOperationException("Choose a current condition with a verified expansion layout.");
        if(IsLegacyProject)
        {
            var span=item.Record.File.Bytes.AsSpan(item.Record.Offset+14,2);var value=(short)(expanded?1:0);
            if(BinaryPrimitives.ReadInt16LittleEndian(span)==value)return false;
            BinaryPrimitives.WriteInt16LittleEndian(span,value);return true;
        }
        return expanded?nativeExpandedConditions.Add(id):nativeExpandedConditions.Remove(id);
    }
    private byte[] ScenarioPresentationSaveFingerprint()
    {
        var content=UnusedScriptFingerprint();
        if(IsLegacyProject||nativeExpandedConditions.Count==0)return content;
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);hash.AppendData(content);
        hash.AppendData("DK2 condition expansion"u8);
        Span<byte> id=stackalloc byte[2];
        foreach(var value in nativeExpandedConditions.Order())
        {BinaryPrimitives.WriteUInt16LittleEndian(id,value);hash.AppendData(id);}
        return hash.GetHashAndReset();
    }
}
