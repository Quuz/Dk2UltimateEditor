using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

if(args.Length!=1||Directory.Exists(args[0]))throw new ArgumentException("A new private fixture directory is required.");
var directory=Path.GetFullPath(args[0]);Directory.CreateDirectory(directory);var passed=0;var serial=0;
void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
void Refuses(Action action){try{action();}catch(InvalidDataException){return;}throw new InvalidOperationException("Malformed primary archive was accepted.");}
void Pass(string name){Console.WriteLine("PASS "+name);passed++;}
byte[] Envelope(params (uint Signature,int Version,byte[] Bytes)[] sections)
{
    using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
    writer.Write(LegacySectionFile.AreaFileSignature);writer.Write(1);writer.Write(sections.Sum(section=>12+section.Bytes.Length));
    foreach(var section in sections){writer.Write(section.Signature);writer.Write(section.Version);writer.Write(section.Bytes.Length);writer.Write(section.Bytes);}
    return stream.ToArray();
}
byte[] Append(byte[] original,byte[] tail)
{
    var result=original.Concat(tail).ToArray();BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8),(uint)(result.Length-12));return result;
}
string Write(byte[] bytes)
{
    var path=Path.Combine(directory,$"archive-{serial++:D3}.dkz");File.WriteAllBytes(path,bytes);return path;
}
var doc=NativeMapDocument.CreateNewMap(new("ArchiveImport","ArchiveImport",16,16,NativeFormat:true));
doc.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="GAMEKEEPER"),new(3,3));doc.Paint([new(2,2)],new(4,2,1));
var clipboard=doc.CopySelection(new(2,2,4,4));var complete=SelectionFile.Serialize(clipboard);var parsed=LegacySectionFile.Parse(complete);
var tileSection=parsed.Sections[0];var thingsSection=parsed.Sections[1];var tile=(tileSection.Signature,tileSection.Version,tileSection.Payload.ToArray());var things=(thingsSection.Signature,thingsSection.Version,thingsSection.Payload.ToArray());
var expectedTiles=parsed.ReadTiles();var originalSource=doc.Files.ToDictionary(file=>file.ExportPath,file=>file.Data.ToArray());var revision=doc.EditRevision;var count=doc.EditorChangeCount;var history=doc.GetHistory();
void SourceExact()=>Expect(doc.EditRevision==revision&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history.Undo.Count&&doc.Files.All(file=>file.Data.Span.SequenceEqual(originalSource[file.ExportPath])),"Import changed its source document.");
SelectionFile.ImportCandidate Candidate(byte[] bytes,SelectionFile.ImportRecovery expectedRecovery,int objects)
{
    var path=Write(bytes);var candidate=SelectionFile.ReadForImport(path);
    Expect(candidate.Recovery==expectedRecovery&&candidate.RequiresRecoveryConsent==(expectedRecovery!=SelectionFile.ImportRecovery.None),"Import recovery/mandatory consent differs.");
    Expect(candidate.Selection.ObjectCount==objects&&candidate.Selection.ScriptCount==0,"Import dropped valid objects or retained malformed objects/scripts.");
    Expect(candidate.Selection.Tiles.Width==expectedTiles.Width&&candidate.Selection.Tiles.Height==expectedTiles.Height&&candidate.Selection.Tiles.Tiles.SequenceEqual(expectedTiles.Tiles),"Recovery changed complete primary tile bytes/dimensions.");
    Expect(candidate.SourceIsUnchanged()&&File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes),"Import repaired or changed archive input.");SourceExact();return candidate;
}
foreach(var native in new[]{false,true})
{
    var source=NativeMapDocument.CreateNewMap(new("CompleteArchive","CompleteArchive",16,16,NativeFormat:native));
    if(native)source.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="GAMEKEEPER"),new(3,3));else source.AddLegacyRecord("Creature",new(3,3));
    var bytes=SelectionFile.Serialize(source.CopySelection(new(2,2,4,4)));var path=Write(bytes);var candidate=SelectionFile.ReadForImport(path);
    Expect(!candidate.RequiresRecoveryConsent&&candidate.Recovery==SelectionFile.ImportRecovery.None&&candidate.Selection.IsLegacyProject==!native&&candidate.Selection.ObjectCount==1&&SelectionFile.Serialize(candidate.Selection).AsSpan().SequenceEqual(bytes),"Complete enhanced archive format/objects/metadata changed.");
    Expect(candidate.SourceIsUnchanged()&&File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes),"Complete archive read changed bytes.");
}
Pass("complete native and project archives retain exact data without recovery consent");
Candidate(Envelope(tile),SelectionFile.ImportRecovery.None,0);Pass("original terrain-only archive reaches EOF successfully without recovery");
foreach(var version in Enumerable.Range(1,6))
{
    var structure=version switch{1=>"MAPTHINGV1",2 or 3=>"MAPTHINGV2",4=>"MAPTHINGV4",5=>"MAPTHINGV5",_=>"MAPTHING"};
    Candidate(Envelope(tile,(NativeMapDocument.LegacyThings,version,new byte[LegacyProjectSchemas.Size(structure)])),SelectionFile.ImportRecovery.None,1);
}
Pass("all six supported historical thing versions remain complete imports");
foreach(var version in new[]{0,7,int.MaxValue})Candidate(Envelope(tile,(NativeMapDocument.LegacyThings,version,things.Item3)),SelectionFile.ImportRecovery.TerrainOnly,0);
Pass("unsupported thing versions require explicit terrain-only consent");
foreach(var length in new[]{1,31,63})Candidate(Envelope(tile,(NativeMapDocument.LegacyThings,6,new byte[length])),SelectionFile.ImportRecovery.TerrainOnly,0);
Pass("truncated thing records retain exact terrain and no partial objects");
var truncated=Envelope(tile,(NativeMapDocument.LegacyThings,6,new byte[8]));var thingStart=24+tile.Item3.Length;BinaryPrimitives.WriteUInt32LittleEndian(truncated.AsSpan(thingStart+8),64);Candidate(truncated,SelectionFile.ImportRecovery.TerrainOnly,0);
Pass("declared thing payload beyond EOF leaves primary terrain recoverable");
Candidate(Envelope(tile,(0xBAD0BAD0,1,Array.Empty<byte>())),SelectionFile.ImportRecovery.TerrainOnly,0);Pass("wrong first thing signature requires terrain-only consent");
foreach(var length in new[]{1,7,11})Candidate(Append(Envelope(tile),new byte[length]),SelectionFile.ImportRecovery.TerrainOnly,0);
Pass("truncated post-map headers cannot corrupt valid primary terrain");
Candidate(Envelope(tile,things,(0xBAD0BAD0,1,new byte[]{8,9})),SelectionFile.ImportRecovery.HistoricalData,1);
Pass("unknown later extension retains first compatible objects behind explicit consent");
Candidate(Envelope(tile,things,things),SelectionFile.ImportRecovery.HistoricalData,1);Candidate(Envelope(tile,things,tile),SelectionFile.ImportRecovery.HistoricalData,1);
Pass("duplicate later sections retain original first objects behind explicit consent");
var enhanced=parsed.Sections[2];Candidate(Envelope(tile,things,(enhanced.Signature,99,enhanced.Payload.ToArray())),SelectionFile.ImportRecovery.HistoricalData,1);
Candidate(Envelope(tile,things,(enhanced.Signature,enhanced.Version,new byte[]{0,1})),SelectionFile.ImportRecovery.HistoricalData,1);
Pass("damaged enhanced metadata retains original compatible objects instead of dropping them");
var invalids=new List<byte[]>();
foreach(var offset in new[]{0,4,8,12,16}){var bytes=Envelope(tile,things);BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset),-1);invalids.Add(bytes);}
foreach(var bytes in invalids)Refuses(()=>SelectionFile.ReadForImport(Write(bytes)));
Refuses(()=>SelectionFile.ReadForImport(Write(Envelope(things,tile))));
Pass("invalid AREA signature version size or first KMAP cannot offer recovery");
foreach(var width in new uint[]{0,1025,uint.MaxValue}){var payload=tile.Item3.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(payload,width);Refuses(()=>SelectionFile.ReadForImport(Write(Envelope((tile.Item1,tile.Item2,payload),things))));}
var wrongLength=tile.Item3[..^1];Refuses(()=>SelectionFile.ReadForImport(Write(Envelope((tile.Item1,tile.Item2,wrongLength),things))));
var wrongDeclared=Envelope(tile,things);BinaryPrimitives.WriteUInt32LittleEndian(wrongDeclared.AsSpan(20),uint.MaxValue);Refuses(()=>SelectionFile.ReadForImport(Write(wrongDeclared)));
Pass("invalid dimensions or primary tile payload sizes are refused before recovery");
var strictPath=Write(Envelope(tile,(NativeMapDocument.LegacyThings,7,things.Item3)));Refuses(()=>SelectionFile.Read(strictPath));
Expect(SelectionFile.ReadForImport(strictPath).RequiresRecoveryConsent,"Strict-read refusal became silent recovery.");Pass("trusted strict Read still refuses malformed objects without any silent recovery");
var changedPath=Write(complete);var changed=SelectionFile.ReadForImport(changedPath);var altered=complete.ToArray();altered[^1]^=1;File.WriteAllBytes(changedPath,altered);
Expect(!changed.SourceIsUnchanged()&&File.ReadAllBytes(changedPath).AsSpan().SequenceEqual(altered),"Changed source reused consent or was repaired.");
File.Delete(changedPath);Expect(!changed.SourceIsUnchanged(),"Missing source reused consent.");Pass("changed and removed source files invalidate the immutable import candidate");
var lockedPath=Write(complete);var lockedCandidate=SelectionFile.ReadForImport(lockedPath);using(var stream=new FileStream(lockedPath,FileMode.Open,FileAccess.Read,FileShare.None))Expect(!lockedCandidate.SourceIsUnchanged(),"Locked source reused consent.");
Expect(lockedCandidate.SourceIsUnchanged(),"Unchanged unlocked source was rejected.");Pass("locked source refuses stale checking without changing its archive bytes");
var hugePath=Write(complete);var hugeCandidate=SelectionFile.ReadForImport(hugePath);using(var stream=new FileStream(hugePath,FileMode.Open,FileAccess.Write))stream.SetLength(64L*1024*1024+1);
Expect(!hugeCandidate.SourceIsUnchanged(),"Oversized changed source reused consent.");Refuses(()=>SelectionFile.ReadForImport(hugePath));Pass("oversized inputs and later growth respect the existing 64 MiB limit");
SourceExact();Expect(passed==17,"Incomplete AREA import checks.");Console.WriteLine($"SELECTION ARCHIVE IMPORT CHECKS PASSED: {passed}");return 0;
