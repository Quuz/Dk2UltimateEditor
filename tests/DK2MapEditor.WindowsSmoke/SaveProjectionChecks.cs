using System.IO;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

// Independent wire and source-save assertions. No section is ignored: even
// KSTS is checked in full against exact count/date/elapsed/Undo invariants.
internal static class SaveProjectionChecks
{
    internal sealed record CaptureState(byte[]? Project, Dictionary<string,byte[]> Files,
        ActivityStatisticsPayload? Statistics, uint? MinElapsed, DateTime MinDate);
    private sealed record Written(CaptureState Before,byte[] Project,byte[]? PreviousInput,byte[]? PreviousProject);
    private static readonly ConditionalWeakTable<NativeMapDocument,Written> written=new();
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    internal static CaptureState Capture(NativeMapDocument document)
    {
        var project=document.IsLegacyProject?WireBefore(document):null;
        var files=document.Files.ToDictionary(file=>file.ExportPath,file=>file.Data.ToArray());
        var statistics=document.ReadActivityStatistics();var elapsed=document.ActivityElapsedMilliseconds;
        return new(project,files,statistics,elapsed,DateTime.Now);
    }
    internal static string SaveCopy(NativeMapDocument document,string path,bool temporary=false)
    {
        var before=Capture(document);var saved=document.SaveCopy(path,temporary);
        Complete(document,before,saved,temporary);return saved;
    }
    internal static TextMapExportResult ExportText(string path,NativeMapDocument document,TextMapScriptMode mode=TextMapScriptMode.SupportedScenarios)
    {
        var before=Capture(document);var result=TextMapFile.SaveCopy(path,document,mode);
        Complete(document,before,result.EditableCopyPath);return result;
    }
    internal static void Complete(NativeMapDocument document,CaptureState before,string saved,bool temporary=false)
    {
        var maxDate=DateTime.Now;var maxElapsed=document.ActivityElapsedMilliseconds;
        if(before.Project is null)return; // Native maps keep their existing exact native assertions.
        var after=document.ReadActivityStatistics();var expected=before.Project;
        if(before.Statistics is null)Require(after is null,"Opaque statistics acquired a writable session.");
        else
        {
            Require(after is not null,"Supported save statistics disappeared.");var current=after!;
            Require(current.SavedTimes==unchecked(before.Statistics.SavedTimes+(temporary?0u:1u)),"Original save count boundary differs.");
            var date=current.LastSaved.CalendarValue();Require(date.HasValue&&date.Value>=before.MinDate.AddMilliseconds(-1)&&date.Value<=maxDate.AddMilliseconds(1),"Save date is outside the actual local write interval.");
            Require(before.MinElapsed.HasValue&&maxElapsed.HasValue&&unchecked(current.RealTicks-before.MinElapsed.Value)<=unchecked(maxElapsed.Value-before.MinElapsed.Value),"Save elapsed counter is outside the actual write interval.");
            var permitted=before.Statistics with {SavedTimes=current.SavedTimes,LastSaved=current.LastSaved,RealTicks=current.RealTicks};
            Require(permitted.Encode().AsSpan().SequenceEqual(current.Encode()),"Save changed another statistics field, extension, opaque tail or Undo count.");
            // Temporary source writer restores only the live count; output kept +1.
            var output=current with {SavedTimes=unchecked(before.Statistics.SavedTimes+1)};
            var parsed=LegacySectionFile.Parse(before.Project);var first=false;
            var sections=parsed.Sections.Select(section=>
            {
                if(first||section.Signature!=ActivityStatisticsPayload.Signature)return section;
                first=true;return new LegacySection(section.Signature,ActivityStatisticsPayload.Version,output.Encode());
            }).ToList();
            if(!first)sections.Add(new(ActivityStatisticsPayload.Signature,ActivityStatisticsPayload.Version,output.Encode()));
            expected=Envelope(parsed.Signature,parsed.Version,sections);
        }
        Require(File.ReadAllBytes(saved).AsSpan().SequenceEqual(expected),"Saved project differs from exact ordered source-statistics projection; another byte/header/section changed.");
        Require(before.Files.Count==document.Files.Count&&document.Files.All(file=>before.Files[file.ExportPath].AsSpan().SequenceEqual(file.Data.Span)),"SaveCopy changed editable input buffers.");
        written.TryGetValue(document,out var previous);
        written.Remove(document);written.Add(document,new(before,expected,previous?.Before.Project,previous?.Project));
    }
    internal static bool PreviousEquals(NativeMapDocument document,byte[] previousBytes)
    {
        Require(written.TryGetValue(document,out var saved),"Sequential comparison requires a complete validated Save.");var state=saved!;
        // Both complete outputs were independently validated, including every
        // KSTS byte. Raw editable input must be unchanged between the writes.
        return state.PreviousInput is not null&&state.PreviousProject is not null
            &&state.Before.Project!.AsSpan().SequenceEqual(state.PreviousInput)
            &&previousBytes.AsSpan().SequenceEqual(state.PreviousProject);
    }
    internal static bool BeforeEquals(NativeMapDocument document,byte[] expected)
    {
        Require(written.TryGetValue(document,out var saved),"Exact project comparison requires a captured, validated SaveCopy.");
        return saved!.Before.Project!.AsSpan().SequenceEqual(expected);
    }
    internal static void RoundTrip(NativeMapDocument document,NativeMapDocument reopened,IReadOnlyDictionary<string,byte[]>? expectedSource=null)
    {
        Require(document.IsLegacyProject&&reopened.IsLegacyProject,"Project roundtrip changed format.");
        Require(written.TryGetValue(document,out var saved),"Roundtrip requires full validated save projection.");var state=saved!;
        if(expectedSource is not null)
            Require(expectedSource.Count==state.Before.Files.Count&&expectedSource.All(pair=>state.Before.Files.TryGetValue(pair.Key,out var bytes)&&bytes.AsSpan().SequenceEqual(pair.Value)),"Roundtrip source changed from caller's exact snapshot.");
        Require(reopened.MainFile.Data.Span.SequenceEqual(state.Project),"Reopened full envelope differs from exact expected output.");
        Require(reopened.TileFile.Data.Span.SequenceEqual(state.Before.Files[document.TileFile.ExportPath]),"Reopened authoritative tile buffer changed.");
        var sections=LegacySectionFile.Parse(state.Project).Sections;
        var optional=new[]{NativeMapDocument.LegacyThings,NativeMapDocument.LegacyAreas,NativeMapDocument.LegacyBands,NativeMapDocument.LegacyEvents,NativeMapDocument.LegacyGlobals}
            .Where(signature=>!sections.Any(section=>section.Signature==signature)).ToArray();
        Require(reopened.Files.Count==2+sections.Count+optional.Length,"Reopen added/dropped a section or buffer.");
        var raw=reopened.Files.Where(file=>file!=reopened.MainFile&&file!=reopened.TileFile).ToArray();
        for(var index=0;index<raw.Length;index++)
        {
            var file=raw.Single(file=>file.ExportPath=="section-"+index);
            if(index<sections.Count)Require(file.Data.Span.SequenceEqual(SectionBytes(sections[index])),"Reopened section/header/version/opaque bytes differ at "+index);
            else Require(file.Signature==optional[index-sections.Count]&&EmptyOptional(file.Signature,BinaryPrimitives.ReadInt32LittleEndian(file.Data.Span[4..]),file.Data.Span),"Reopen optional default is not the exact literal source layout.");
        }
    }
    internal static bool FileEquals(NativeMapDocument document,NativeFile actual,byte[] expectedBefore)
    {
        if(!document.IsLegacyProject)return actual.Data.Span.SequenceEqual(expectedBefore);
        Require(written.TryGetValue(document,out var saved),"Roundtrip file comparison requires a captured, validated SaveCopy.");
        var state=saved!;
        // Retain the caller's pre-edit/Undo assertion; the expected wire payload
        // cannot hide a changed original snapshot.
        if(state.Before.Files.TryGetValue(actual.ExportPath,out var original))
            Require(original.AsSpan().SequenceEqual(expectedBefore),"Caller snapshot differs from source bytes before Save: "+actual.ExportPath);
        var sections=LegacySectionFile.Parse(state.Project).Sections;
        if(actual.ExportPath.StartsWith("section-",StringComparison.Ordinal)&&int.TryParse(actual.ExportPath.AsSpan(8),out var index))
        {
            if(index<sections.Count)return actual.Data.Span.SequenceEqual(SectionBytes(sections[index]));
            // The loader appends only literal unchanged optional source defaults.
            return actual.Data.Span.SequenceEqual(expectedBefore);
        }
        if(actual.ExportPath==document.TileFile.ExportPath)return actual.Data.Span.SequenceEqual(state.Before.Files[document.TileFile.ExportPath]);
        return actual.Data.Span.SequenceEqual(state.Project);
    }
    private static byte[] WireBefore(NativeMapDocument document)
    {
        var original=LegacySectionFile.Parse(document.MainFile.Data.Span);var sections=new List<LegacySection>();
        foreach(var file in document.Files.Where(file=>file!=document.MainFile&&file!=document.TileFile&&file.ExportPath.StartsWith("section-",StringComparison.Ordinal)).OrderBy(file=>int.Parse(file.ExportPath.AsSpan(8))))
        {
            var index=int.Parse(file.ExportPath.AsSpan(8));var bytes=file.Data.Span;
            Require(bytes.Length>=12&&BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..])==bytes.Length-12,"Section snapshot has invalid source size.");
            var version=BinaryPrimitives.ReadInt32LittleEndian(bytes[4..]);
            // Original section occurrences/order are always retained. The five
            // loader-created optional placeholders are omitted only if exactly
            // equal to their documented literal empty source layout.
            if(index>=original.Sections.Count&&EmptyOptional(file.Signature,version,bytes))continue;
            if(file.Signature==LegacySectionFile.TileSectionSignature)
            {
                var payload=new byte[checked(8+document.Width*document.Height*4)];
                BinaryPrimitives.WriteInt32LittleEndian(payload,document.Width);BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4),document.Height);
                for(var y=0;y<document.Height;y++)for(var x=0;x<document.Width;x++)BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8+(y*document.Width+x)*4),document.GetTile(x,y).Packed);
                sections.Add(new(file.Signature,version,payload));
            }
            else sections.Add(new(file.Signature,version,bytes[12..].ToArray()));
        }
        return Envelope(original.Signature,original.Version,sections);
    }
    private static bool EmptyOptional(uint signature,int version,ReadOnlySpan<byte> bytes)
    {
        var layout=signature switch
        {
            NativeMapDocument.LegacyThings=>(6,12),NativeMapDocument.LegacyAreas=>(1,12),
            NativeMapDocument.LegacyBands=>(5,12),NativeMapDocument.LegacyEvents=>(3,24+146*64),
            NativeMapDocument.LegacyGlobals=>(1,16),_=>(0,0)
        };
        return version==layout.Item1&&bytes.Length==layout.Item2&&bytes[12..].IndexOfAnyExcept((byte)0)<0;
    }
    private static byte[] SectionBytes(LegacySection section)
    {
        var bytes=new byte[12+section.Payload.Length];BinaryPrimitives.WriteUInt32LittleEndian(bytes,section.Signature);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4),section.Version);BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8),section.Payload.Length);section.Payload.Span.CopyTo(bytes.AsSpan(12));return bytes;
    }
    private static byte[] Envelope(uint signature,int version,IEnumerable<LegacySection> sections)
    {
        // Preserve MAPF/AREA, complete headers, versions, duplicates and order.
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        writer.Write(signature);writer.Write(version);writer.Write(0);
        foreach(var section in sections)writer.Write(SectionBytes(section));
        var bytes=stream.ToArray();BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8),bytes.Length-12);return bytes;
    }
}
