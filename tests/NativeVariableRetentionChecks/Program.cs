using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using DK2MapEditor.Core.Editing;

void Expect(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
void Refuses(Action action) { try { action(); } catch(InvalidDataException) { return; } throw new InvalidOperationException("Expected InvalidDataException."); }
string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
var root=Path.GetFullPath(args.Single());
Expect(!Directory.Exists(root),"Use a fresh evidence directory.");Directory.CreateDirectory(root);
var input="D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd";
var source=NativeMapDocument.Load(input);
var before=source.Files.ToDictionary(file=>file.SourcePath,file=>Hash(file.SourcePath));
var raw=source.Records().Where(record=>record.File.Signature==0xE6 && record.File.ExportPath.EndsWith("Variables.kld",StringComparison.OrdinalIgnoreCase)).ToDictionary(record=>record.Offset,record=>record.File.Data.Slice(record.Offset,16).ToArray());
var expected=new[]{88,168,3576,3704,3752}.Select(offset=>raw[offset]).ToArray();
// These offsets and raw DWORDs come from the independently recorded level1
// sample, not from the new preservation classifier.
Expect(Convert.ToHexString(expected[0])=="02000000010003000300000004000000","Room availability sample changed.");
Expect(Convert.ToHexString(expected[1])=="02000000020003000700000003000000","Creature availability sample changed.");
Expect(BitConverter.ToUInt32(expected[2])==0xE9 && BitConverter.ToUInt32(expected[3])==0x4A && BitConverter.ToUInt32(expected[4])==0xF1,"Unknown variable sample changed.");
var path=Path.Combine(root,"DK2Preview_6a88240eaf814f78.dkm");
using var save=new EditorSaveSession(source);
var plan=save.PrepareSaveAs(path);
Expect(!plan.HasBlockingIssues && plan.NeedsLossConsent && plan.RetainsNativeSource,"Native preservation did not clear only representational blockers.");
Expect(plan.ConversionIssues.Count(issue=>issue.Code is "AvailabilityID" or "AvailabilityType" or "UnknownVariable")==5,"Unexpected preserved variable issues.");
Expect(!save.Save(plan,false) && !File.Exists(path),"Declining projection published DKM.");
Expect(save.Save(plan,true),"Save refused accepted native retention.");
var archivePath=save.LastSave!.SourceArchives.Single();var archive=EditorSourceArchive.Read(archivePath);
Expect(archive.RetainedVariables.AsSpan().SequenceEqual(expected.SelectMany(bytes=>bytes).ToArray()),"Archive lost the exact five native records.");
var loaded=NativeMapDocument.Load(path);
var compiled=loaded.CompileLegacyProject("DK2Preview_6a88240eaf814f78",source);
void CheckPreserved(NativeMapDocument document)
{
    var variables=document.Records().Where(record=>record.File.Signature==0xE6 && record.File.ExportPath.EndsWith("Variables.kld",StringComparison.OrdinalIgnoreCase)).Select(record=>Convert.ToHexString(record.File.Data.Span.Slice(record.Offset,16))).ToArray();
    foreach(var bytes in expected)Expect(variables.Count(value=>value==Convert.ToHexString(bytes))==1,"Preserved variable missing or duplicated.");
}
CheckPreserved(compiled);
var exported=compiled.SaveCopy(Path.Combine(root,"compiled"));CheckPreserved(NativeMapDocument.Load(exported));
Console.WriteLine("PASS real level1 KWD -> accepted DKM+ZIP -> reopen -> compile -> native export/reopen; all five records exact, no duplicates");
var keeper=loaded.Records().Where(record=>record.Category=="Project.MAPOPTIONS").ElementAt(2);
loaded.SetField(keeper,keeper.Fields.Single(field=>field.Name=="PlayerGold"),"4567");
using var reopenedSave=new EditorSaveSession(loaded,path);
reopenedSave.Save(reopenedSave.PrepareSave(),true);
var copy=Path.Combine(root,"edited-copy.dkm");reopenedSave.Save(reopenedSave.PrepareSaveAs(copy),true);
var edited=NativeMapDocument.Load(copy).CompileLegacyProject("DK2Preview_6a88240eaf814f78",source);CheckPreserved(edited);
var nativeKeeper=edited.Records().Single(record=>record.Category=="GAMEPLAYER" && record.Read(record.Fields.Single(field=>field.Name=="bGlobalPlayer"))=="3");
Expect(nativeKeeper.Read(nativeKeeper.Fields.Single(field=>field.Name=="bPlayerGold"))=="4567","Retained records overwrote edited supported player data.");
Console.WriteLine("PASS ordinary Save and renamed Save As retain native records while supported player edits compile");

string AlterArchive(string name,Action<JsonObject> alter)
{
    var destination=Path.Combine(root,name);using var inputZip=ZipFile.OpenRead(archivePath);using var outputZip=ZipFile.Open(destination,ZipArchiveMode.Create);
    foreach(var entry in inputZip.Entries)
    {
        var outputEntry=outputZip.CreateEntry(entry.FullName,CompressionLevel.NoCompression);
        using var reader=entry.Open();using var writer=outputEntry.Open();
        if(entry.FullName!="manifest.json") { reader.CopyTo(writer);continue; }
        var manifest=JsonNode.Parse(reader)!.AsObject();alter(manifest);writer.Write(JsonSerializer.SerializeToUtf8Bytes(manifest));
    }
    return destination;
}
var v1=AlterArchive("version1.zip",manifest=>{manifest["Version"]=1;manifest.Remove("RetainedVariables");});
Expect(EditorSourceArchive.Read(v1).RetainedVariables.IsEmpty,"Version 1 compatibility failed.");
var corrupt=AlterArchive("invented-record.zip",manifest=>manifest["RetainedVariables"]=Convert.ToBase64String(Enumerable.Repeat((byte)0xFF,16).ToArray()));Refuses(()=>EditorSourceArchive.Read(corrupt));
var misaligned=AlterArchive("misaligned.zip",manifest=>manifest["RetainedVariables"]=Convert.ToBase64String(new byte[17]));Refuses(()=>EditorSourceArchive.Read(misaligned));
var duplicate=AlterArchive("duplicate-record.zip",manifest=>manifest["RetainedVariables"]=Convert.ToBase64String(expected[0].Concat(expected[0]).ToArray()));Refuses(()=>EditorSourceArchive.Read(duplicate));
var unsupported=AlterArchive("unsupported-version.zip",manifest=>manifest["Version"]=3);Refuses(()=>EditorSourceArchive.Read(unsupported));
var represented=AlterArchive("represented-record.zip",manifest=>manifest["RetainedVariables"]=Convert.ToBase64String(raw[72]));Refuses(()=>EditorSourceArchive.Read(represented));
var competing=AlterArchive("competing.zip",manifest=>manifest["Statistics"]=Convert.ToBase64String(new byte[0]));
var competingArchive=EditorSourceArchive.Read(competing);
var competingPath=path+".native-source-"+competingArchive.ContentHash+".zip";
File.Copy(competing,competingPath);
try { Refuses(()=>NativeMapDocument.Load(path)); } finally { File.Delete(competingPath); }
var invalid=NativeMapDocument.Load(input);
var availability=invalid.Records().Single(record=>record.Offset==168 && record.File.ExportPath.EndsWith("Variables.kld",StringComparison.OrdinalIgnoreCase));
invalid.SetField(availability,availability.Fields.Single(field=>field.Name=="wGlobalPlayer"),"0");
var invalidConversion=invalid.PrepareProjectConversion();
Expect(invalidConversion.HasBlockingIssues && invalidConversion.Issues.Any(issue=>issue.Code=="VariablePlayer" && issue.Severity==ProjectConversionSeverity.Error),"Invalid player became a permitted passthrough record.");
Console.WriteLine("PASS version 1 archive compatibility and rejection of invented, misaligned, duplicated and unknown-version preserved data");
Console.WriteLine("PASS supported-record injection, ambiguous companions and invalid-player conversion remain blocked");
Expect(before.All(item=>Hash(item.Key)==item.Value),"Original source file changed.");
var report=new {Passed=true,Source=input,Project=path,SourceArchive=archivePath,CompiledDescriptor=exported,PreservedRecords=expected.Select(Convert.ToHexString),OriginalSourceSHA256=before,OriginalSourceUnchanged=true,GuiUsed=false,GameLaunched=false,FixedLevelName="DK2Preview_6a88240eaf814f78",RemainingProjectionLosses=plan.ConversionIssues.Where(issue=>issue.Code is not ("AvailabilityID" or "AvailabilityType" or "UnknownVariable"))};
File.WriteAllText(Path.Combine(root,"actual-variable-retention.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions {WriteIndented=true}));
Console.WriteLine("NATIVE VARIABLE RETENTION CHECKS PASSED; GUI/game and remaining terrain/event fidelity are not claimed.");
