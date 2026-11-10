using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private EditorSourceArchive? nativePayloadSource;

    private static bool HasNativePayloadSource(EditorSourceArchive archive)
        => archive.SourceFiles.Any(file=>file.Bytes.Length>=36 && BinaryPrimitives.ReadUInt32LittleEndian(file.Bytes.AsSpan())==0x64);

    // Build solely from validated immutable ZIP entries. Archived paths are
    // labels, never filesystem instructions or input to NativeMapDocument.Load.
    private static NativeMapDocument ArchivedNativeDocument(EditorSourceArchive archive)
    {
        var document=new NativeMapDocument();
        foreach(var source in archive.SourceFiles)
        {
            if(source.ExportPath.EndsWith(OwnerScopesSuffix,StringComparison.Ordinal))
            {
                var state=new NativeFile(source.SourcePath,source.ExportPath,source.Bytes.ToArray());
                _=DecodeNativeOwnerScopes(state);document.nativeOwnerScopes=state;
                document.originalNativeOwnerScopes=state.Data.ToArray();continue;
            }
            if(source.ExportPath.EndsWith(AreaScopesSuffix,StringComparison.Ordinal))
            {
                var state=new NativeFile(source.SourcePath,source.ExportPath,source.Bytes.ToArray());
                _=DecodeNativeAreaScopes(state);document.nativeAreaScopes=state;
                document.originalNativeAreaScopes=state.Data.ToArray();continue;
            }
            if(source.ExportPath.EndsWith(GateBindingsSuffix,StringComparison.Ordinal))
            {
                var state=new NativeFile(source.SourcePath,source.ExportPath,source.Bytes.ToArray());
                _=DecodeNativeGateLinks(state);document.nativeGateBindings=state;
                document.originalNativeGateBindings=state.Data.ToArray();continue;
            }
            if(source.ExportPath.EndsWith("#GameTarget",StringComparison.Ordinal))
            {
                if(source.Bytes.Length!=4)throw new InvalidDataException("Invalid archived native game target.");
                document.nativeGameTarget=new(source.SourcePath,source.ExportPath,source.Bytes.ToArray());
                document.originalNativeGameTarget=document.nativeGameTarget.UInt32(0);continue;
            }
            if(source.ExportPath.EndsWith("#ResizeFill",StringComparison.Ordinal))
            {
                if(source.Bytes.Length!=4)throw new InvalidDataException("Invalid archived resize background.");
                document.nativeResizeFill=new(source.SourcePath,source.ExportPath,source.Bytes.ToArray());
                document.originalNativeResizeFill=document.nativeResizeFill.UInt32(0);continue;
            }
            if(source.Bytes.Length<12)throw new InvalidDataException("Truncated archived native file.");
            var file=new NativeFile(source.SourcePath,source.ExportPath,source.Bytes.ToArray());
            if(source.ExportPath.EndsWith(ScenarioNamesExtension,StringComparison.OrdinalIgnoreCase))
            {
                document.nativeScenarioNames=file;document.originalNativeScenarioNames=file.Data.ToArray();
                document.nativeScenarioNamesLoaded=true;continue;
            }
            document.Add(file);
        }
        var descriptors=document.files.Where(file=>file.Signature==0xDC).ToArray();
        var tiles=document.files.Where(file=>file.Signature==0x64).ToArray();
        if(descriptors.Length>1 || tiles.Length!=1)throw new InvalidDataException("Ambiguous archived native map.");
        document.MainFile=descriptors.SingleOrDefault() ?? tiles[0];document.TileFile=tiles[0];
        document.CheckTiles();_ = document.Records();
        return document;
    }

    private void RestoreUneditedNativePayloads(NativeMapDocument output,NativeMapDocument basis,bool includeDeclaredKeepers)
    {
        if(nativePayloadSource is not { } archive)return;
        var original=ArchivedNativeDocument(archive);
        var conversion=original.PrepareProjectConversion("NativePayloadBaseline.dkm");
        var baseline=conversion.Project;
        try
        {
            if(conversion.HasBlockingIssues)throw new InvalidDataException("Archived source cannot establish a safe native payload baseline.");
            if(archive.NativeEventIDs.Count!=conversion.NativeEventIDs.Count || archive.NativeEventIDs.Any(pair=>!conversion.NativeEventIDs.TryGetValue(pair.Key,out var id) || id!=pair.Value))
                throw new InvalidDataException("Archived native event identity mapping does not match its source.");
            // This baseline has no source archive, so compilation cannot recurse.
            var generatedBaseline=baseline.CompileLegacyProject("PayloadBaseline",basis,includeDeclaredKeepers);
            try
            {
                RestoreUneditedNativeGates(output,original,baseline,generatedBaseline);
                RestoreUneditedNativeRecords(output,original,baseline,generatedBaseline,archive.NativeEventIDs);
                if(Width==baseline.Width && Height==baseline.Height)
                    for(var y=0;y<Height;y++)for(var x=0;x<Width;x++)
                        if(GetTile(x,y)==baseline.GetTile(x,y))output.PutTile(x,y,original.GetTile(x,y));
                var raw=original.Records().Where(record=>record.File.Signature==0xD2).ToDictionary(record=>BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[(record.Offset+16)..]));
                var generated=generatedBaseline.Records().Where(record=>record.File.Signature==0xD2).ToDictionary(record=>BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[(record.Offset+16)..]));
                var identities=archive.NativeEventIDs.ToDictionary(pair=>pair.Value,pair=>pair.Key);
                var currentEvents=Records().Where(record=>record.Category is "Project.TRIGGER" or "Project.ACTION").ToDictionary(record=>ushort.Parse(record.Read(record.Fields.Single(field=>field.Name=="id"))));
                var priorEvents=baseline.Records().Where(record=>record.Category is "Project.TRIGGER" or "Project.ACTION").ToDictionary(record=>ushort.Parse(record.Read(record.Fields.Single(field=>field.Name=="id"))));
                var outputGateOrigins=output.NativeGateBindingRecords().Where(NativeSourceGate).Select(gate=>NativeDerivedPosition(gate,true)).ToHashSet();
                foreach(var record in output.Records().Where(record=>record.File.Signature==0xD2))
                {
                    var id=BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[(record.Offset+16)..]);
                    if(!identities.TryGetValue(id,out var originalID) || !raw.TryGetValue(originalID,out var native) || !generated.TryGetValue(id,out var prior))continue;
                    var bytes=record.File.Bytes.AsSpan(record.Offset,record.Length);
                    var previous=prior.File.Data.Span.Slice(prior.Offset,prior.Length);
                    var source=native.File.Data.Span.Slice(native.Offset,native.Length);
                    if(!currentEvents.TryGetValue(id,out var currentEvent) || !priorEvents.TryGetValue(id,out var priorEvent))continue;
                    if(currentEvent.Fields.Where(field=>field.Name.StartsWith("param",StringComparison.Ordinal)).Any(field=>currentEvent.Read(field)!=priorEvent.Read(priorEvent.Fields.Single(candidate=>candidate.Name==field.Name))))continue;
                    // A formerly missing raw coordinate must not acquire an
                    // unrelated gate inserted after native-to-DKM conversion.
                    if(IsNativeDestroyAction(native) && original.NativeGateTarget(native) is null
                        && NativeDerivedPayload(native) is var missingPoint && outputGateOrigins.Contains(new(missingPoint.X-1,missingPoint.Y-1)))continue;
                    // Keep freshly generated IDs, graph links, repeat flags and
                    // changed operands. Derived gate/gem coordinates participate
                    // in the comparison, so moved dependencies cannot be undone.
                    if(bytes.Length==24 && source.Length==24 && bytes[..4].SequenceEqual(source[..4]) && bytes[22]==source[22]
                        && bytes.Slice(8,8).SequenceEqual(previous.Slice(8,8)))source.Slice(8,8).CopyTo(bytes[8..]);
                }
            }
            finally { generatedBaseline.activityStatistics?.Dispose(); }
        }
        finally { baseline.activityStatistics?.Dispose(); }
    }
}
