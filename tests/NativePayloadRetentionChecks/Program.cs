using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO.Compression;
using DK2MapEditor.Core.Editing;

void Expect(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
ushort ID(NativeRecord record)=>BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[(record.Offset+16)..]);
byte[] Payload(NativeRecord record)=>record.File.Data.Slice(record.Offset+8,8).ToArray();
NativeRecord ProjectEvent(NativeMapDocument map,ushort id)=>map.Records().Single(record=>record.Category is "Project.TRIGGER" or "Project.ACTION" && record.Read(record.Fields.Single(field=>field.Name=="id"))==id.ToString());
var root=Path.GetFullPath(args.Single());Expect(!Directory.Exists(root),"Use fresh evidence directory.");Directory.CreateDirectory(root);
var manual=Path.GetFullPath("tests/Fixtures/NativeRoundtrip/DK2Preview_6a88240eaf814f78.dkm");
var original=NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
var sourceHash=original.Files.ToDictionary(file=>file.SourcePath,file=>Hash(file.SourcePath));
var archivePath=Directory.GetFiles(Path.GetDirectoryName(manual)!,Path.GetFileName(manual)+".native-source-*.zip").Single();
var archive=EditorSourceArchive.Read(archivePath);var manualHash=new[]{manual,archivePath}.ToDictionary(path=>path,Hash);
var project=NativeMapDocument.Load(manual);var projectHash=project.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
var originalEvents=original.Records().Where(record=>record.File.Signature==0xD2).ToDictionary(ID);
var compiled=project.CompileLegacyProject("DK2Preview_6a88240eaf814f78",original);
var compiledEvents=compiled.Records().Where(record=>record.File.Signature==0xD2).ToDictionary(ID);
Expect(compiledEvents.Count==711 && originalEvents.Count==711,"Unexpected level1 event count.");
foreach(var (id,record) in originalEvents)Expect(Payload(record).AsSpan().SequenceEqual(Payload(compiledEvents[archive.NativeEventIDs[id]])),"Unedited native payload differs for event "+id);
for(var y=0;y<original.Height;y++)for(var x=0;x<original.Width;x++)Expect(original.GetTile(x,y)==compiled.GetTile(x,y),"Unedited cell differs at "+x+","+y);
Expect(projectHash.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Compilation mutated project.");
var output=compiled.SaveCopy(Path.Combine(root,"compiled-user-save"));var reload=NativeMapDocument.Load(output);
for(var y=0;y<original.Height;y++)for(var x=0;x<original.Width;x++)Expect(original.GetTile(x,y)==reload.GetTile(x,y),"Export/reload changed preserved tile.");
Console.WriteLine("PASS user's manual DKM+ZIP -> compile/export/reopen: all1800 native cells and all711 event payloads exact; source/project immutable");

// Choose a literal payload difference between ordinary DKM compilation and
// original source. A DKM without its companion provides the ordinary baseline.
var barePath=Path.Combine(root,"bare.dkm");File.Copy(manual,barePath);
var bare=NativeMapDocument.Load(barePath).CompileLegacyProject("DK2Preview_6a88240eaf814f78",original);
var bareEvents=bare.Records().Where(record=>record.File.Signature==0xD2).ToDictionary(ID);
var differences=originalEvents.Where(pair=>!Payload(pair.Value).AsSpan().SequenceEqual(Payload(bareEvents[archive.NativeEventIDs[pair.Key]]))).Select(pair=>archive.NativeEventIDs[pair.Key]).ToArray();
Expect(differences.Length==11,"Expected independently observed eleven event payload differences.");
var selected=differences.Select(id=>ProjectEvent(project,id)).First(record=>LegacyEventSchemas.Find(record.Category=="Project.TRIGGER"?0xD5u:0xD6u,byte.Parse(record.Read(record.Fields.Single(field=>field.Name=="ctrl"))))!.Arguments.Any(field=>field.Name.StartsWith("Param",StringComparison.Ordinal)));
var selectedID=ushort.Parse(selected.Read(selected.Fields.Single(field=>field.Name=="id")));
var selectedSchema=LegacyEventSchemas.Find(selected.Category=="Project.TRIGGER"?0xD5u:0xD6u,byte.Parse(selected.Read(selected.Fields.Single(field=>field.Name=="ctrl"))))!;
var operandName="param"+selectedSchema.Arguments.First(field=>field.Name.StartsWith("Param",StringComparison.Ordinal)).Name[5..];
var operand=selected.Fields.Single(field=>field.Name==operandName);var old=long.Parse(selected.Read(operand));
project.SetField(selected,operand,(old==0?1:0).ToString());
var repeat=selected.Fields.Single(field=>field.Name=="repeat");project.SetField(selected,repeat,"3");
project.Paint([new(10,10)],new MapTile(4,2,1));
using var session=new EditorSaveSession(project,manual);
var editedPath=Path.Combine(root,"edited.dkm");session.Save(session.PrepareSaveAs(editedPath),true);
var edited=NativeMapDocument.Load(editedPath);var editedCompiled=edited.CompileLegacyProject("DK2Preview_6a88240eaf814f78",original);
var editedBarePath=Path.Combine(root,"edited-bare.dkm");File.Copy(editedPath,editedBarePath);
var editedBare=NativeMapDocument.Load(editedBarePath).CompileLegacyProject("DK2Preview_6a88240eaf814f78",original);
var changed=editedCompiled.Records().Single(record=>record.File.Signature==0xD2 && ID(record)==selectedID);
var expectedChanged=editedBare.Records().Single(record=>record.File.Signature==0xD2 && ID(record)==selectedID);
Expect(Payload(changed).AsSpan().SequenceEqual(Payload(expectedChanged)),"Preservation overwrote changed event operands.");
Expect(!Payload(expectedChanged).AsSpan().SequenceEqual(Payload(bareEvents[selectedID])),"Chosen operand edit did not affect the generated native payload.");
Expect(changed.File.Data.Span[changed.Offset+23]==3,"Preservation overwrote repeat flags.");
Expect(editedCompiled.GetTile(10,10)==new MapTile(4,2,1),"Preservation overwrote painted tile.");
for(var y=0;y<original.Height;y++)for(var x=0;x<original.Width;x++)if(x!=10||y!=10)Expect(editedCompiled.GetTile(x,y)==original.GetTile(x,y),"Unedited neighbour changed.");
Console.WriteLine("PASS painted tile, changed event operand and repeat flags survive Save As/reopen/compile; unedited neighbours restored");

var undoProject=NativeMapDocument.Load(manual);
var undoEvent=ProjectEvent(undoProject,selectedID);
undoProject.SetField(undoEvent,undoEvent.Fields.Single(field=>field.Name==operand.Name),(old==0?1:0).ToString());
undoProject.SetField(undoEvent,undoEvent.Fields.Single(field=>field.Name=="repeat"),"3");
undoProject.Paint([new(10,10)],new MapTile(4,2,1));
undoProject.Undo();undoProject.Undo();undoProject.Undo();
var undoCompiled=undoProject.CompileLegacyProject("DK2Preview_6a88240eaf814f78",original);
Expect(undoCompiled.GetTile(10,10)==original.GetTile(10,10),"Undo did not restore original native cell bytes.");
Expect(Payload(undoCompiled.Records().Single(record=>record.File.Signature==0xD2 && ID(record)==selectedID)).AsSpan().SequenceEqual(Payload(originalEvents[archive.NativeEventIDs.Single(pair=>pair.Value==selectedID).Key])),"Undo did not restore native payload.");
Console.WriteLine("PASS Undo returns to exact original native cell/event payload without stale restoration");

var resized=NativeMapDocument.Load(manual);
resized.Resize(resized.Width+2,resized.Height+2,1,1,new MapTile(2,2,1));
using(var resizedSave=new EditorSaveSession(resized,manual))resizedSave.Save(resizedSave.PrepareSaveAs(Path.Combine(root,"resized.dkm")),true);
var resizedCompiled=NativeMapDocument.Load(Path.Combine(root,"resized.dkm")).CompileLegacyProject("DK2Preview_6a88240eaf814f78",original);
File.Copy(Path.Combine(root,"resized.dkm"),Path.Combine(root,"resized-bare.dkm"));
var resizedBare=NativeMapDocument.Load(Path.Combine(root,"resized-bare.dkm")).CompileLegacyProject("DK2Preview_6a88240eaf814f78",original);
for(var y=0;y<resized.Height;y++)for(var x=0;x<resized.Width;x++)Expect(resizedCompiled.GetTile(x,y)==resizedBare.GetTile(x,y),"Original coordinate restoration corrupted resized geometry.");
Console.WriteLine("PASS resized and shifted geometry compiles without restoration at obsolete cell coordinates");

string CopyWithAlteredManifest(string name,Action<JsonObject> alter)
{
    var target=Path.Combine(root,name+".dkm");File.Copy(manual,target);
    var temporary=Path.Combine(root,name+".zip");
    using(var inputZip=ZipFile.OpenRead(archivePath))using(var outputZip=ZipFile.Open(temporary,ZipArchiveMode.Create))
        foreach(var entry in inputZip.Entries)
        {
            var outputEntry=outputZip.CreateEntry(entry.FullName,CompressionLevel.NoCompression);
            using var reader=entry.Open();using var writer=outputEntry.Open();
            if(entry.FullName!="manifest.json") {reader.CopyTo(writer);continue;}
            var manifest=JsonNode.Parse(reader)!.AsObject();alter(manifest);writer.Write(JsonSerializer.SerializeToUtf8Bytes(manifest));
        }
    var companion=EditorSourceArchive.Read(temporary);
    File.Move(temporary,target+".native-source-"+companion.ContentHash+".zip");return target;
}
var relocated=CopyWithAlteredManifest("relocated-labels",manifest=>
{
    var index=0;foreach(var file in manifest["Files"]!.AsArray())file!["SourcePath"]="Z:/not-a-real-source/"+index++;
});
var relocatedCompiled=NativeMapDocument.Load(relocated).CompileLegacyProject("DK2Preview_6a88240eaf814f78",original);
for(var y=0;y<original.Height;y++)for(var x=0;x<original.Width;x++)Expect(relocatedCompiled.GetTile(x,y)==original.GetTile(x,y),"Archive path labels were treated as input files.");
var badMapping=CopyWithAlteredManifest("invalid-event-mapping",manifest=>
{
    var mapping=manifest["NativeEventIDs"]!.AsObject();var first=mapping.First();mapping[first.Key]=(ushort)(first.Value!.GetValue<ushort>()+1);
});
var rejected=false;
try {NativeMapDocument.Load(badMapping).CompileLegacyProject("DK2Preview_6a88240eaf814f78",original);}catch(InvalidDataException){rejected=true;}
Expect(rejected,"Changed archived event identity mapping was accepted.");
Console.WriteLine("PASS archive source paths are inert labels; modified event identity mapping is refused before restoration");

// Derived operands depend on placed entities, even when the event's DKM
// parameters are untouched. Test both of the compiler's coordinate recipes.
var dependent=NativeMapDocument.Create(18,18,"DependencySource",original);
NativeRecord Add(string structure,int x,int y)=>dependent.AddRecord(RecordTemplate.All.Single(template=>template.Structure==structure),new(x,y),3);
void Set(NativeMapDocument document,NativeRecord record,string field,string value)=>document.SetField(record,record.Fields.Single(item=>item.Name==field),value);
var gate=Add("GAMEGATE",4,4);Set(dependent,gate,"bGateType","33");
var gem=Add("GAMEOBJECT",8,8);Set(dependent,gem,"bObjectID","28");
foreach(var (type,x,y) in new (byte,ushort,ushort)[]{(0x20,5,5),(2,8,8)})
{
    var action=Add("Action",0,0);dependent.SetEventType(action,LegacyEventSchemas.Find(0xD6,type)!);
    var fileBytes=action.File.Data.ToArray();fileBytes[action.Offset+8]=0xA7;
    BinaryPrimitives.WriteUInt16LittleEndian(fileBytes.AsSpan(action.Offset+12),x);BinaryPrimitives.WriteUInt16LittleEndian(fileBytes.AsSpan(action.Offset+14),y);
    typeof(NativeFile).GetProperty("Bytes",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(action.File,fileBytes);
}
using var dependencySave=new EditorSaveSession(dependent);
var dependentPath=Path.Combine(root,"dependencies.dkm");var dependencyPlan=dependencySave.PrepareSaveAs(dependentPath);
Expect(!dependencyPlan.HasBlockingIssues,"Synthetic dependency projection blocked.");dependencySave.Save(dependencyPlan,true);
var dependencyProject=NativeMapDocument.Load(dependentPath);
var projectedGate=dependencyProject.Records().Single(record=>record.Category=="Project.MAPRECTEX" && record.Read(record.Fields.Single(field=>field.Name=="node.type"))=="1");
Set(dependencyProject,projectedGate,"rect.x1","6");Set(dependencyProject,projectedGate,"rect.x2","6");
var projectedGem=dependencyProject.Records().Single(record=>record.Category=="Project.MAPTHING" && record.Read(record.Fields.Single(field=>field.Name=="id"))=="28");Set(dependencyProject,projectedGem,"x","9");
var dependencyCompiled=dependencyProject.CompileLegacyProject("DK2Preview_6a88240eaf814f78",original);
var destroy=dependencyCompiled.Records().Single(record=>record.File.Signature==0xD2 && record.File.Data.Span[record.Offset+22]==0x20);
var show=dependencyCompiled.Records().Single(record=>record.File.Signature==0xD2 && record.File.Data.Span[record.Offset+22]==2);
Expect(BinaryPrimitives.ReadUInt16LittleEndian(destroy.File.Data.Span[(destroy.Offset+12)..])==7,"Archived payload moved destroy-gate coordinates back.");
Expect(BinaryPrimitives.ReadUInt16LittleEndian(show.File.Data.Span[(show.Offset+12)..])==9,"Archived payload moved gem coordinates back.");
Console.WriteLine("PASS moved gate and portal gem update derived event coordinates; unchanged event parameters cannot restore obsolete targets");

Expect(sourceHash.All(pair=>Hash(pair.Key)==pair.Value) && manualHash.All(pair=>Hash(pair.Key)==pair.Value),"Original game files or user's saved DKM/ZIP changed.");
var report=new {Passed=true,ManualSave=manual,ManualSaveSHA256=manualHash,OriginalSourceSHA256=sourceHash,
 CellsPreserved=1800,EventPayloadsPreserved=711,PayloadDifferencesPreviouslyLost=differences.Length,
 PaintedTileAndEditedOperandsVerified=true,UndoVerified=true,CompiledDescriptor=output,SourceUnchanged=true,
 UserReportedSave=true,ComputerUseUsed=false,GameLaunched=false};
File.WriteAllText(Path.Combine(root,"actual-payload-retention.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("NATIVE PAYLOAD RETENTION CHECKS PASSED; entity/definition/metadata roundtrip completeness and game verification remain separate.");
