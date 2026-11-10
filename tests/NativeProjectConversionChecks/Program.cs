using System.Buffers.Binary;
using System.Reflection;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

var directory=Path.Combine(Path.GetTempPath(),"NativeProjectConversionChecks",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
void Expect(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
void Refuses<T>(Action action) where T:Exception {try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
long Number(NativeRecord record,string name)=>long.Parse(record.Read(record.Fields.Single(field=>field.Name==name)));
void Set(NativeMapDocument document,NativeRecord record,string field,string value)=>document.SetField(record,record.Fields.Single(item=>item.Name==field),value);
NativeRecord Add(NativeMapDocument document,string type,int x=4,int y=4,byte owner=3)=>document.AddRecord(RecordTemplate.All.Single(template=>template.Structure==type),new(x,y),owner);
Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument doc)=>doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
bool Same(NativeMapDocument doc,Dictionary<NativeFile,byte[]> snapshot)=>doc.Files.Count==snapshot.Count && snapshot.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value));
NativeRecord Info(NativeMapDocument doc)=>doc.Records().Single(record=>record.Category=="Project.MAPPROPERTIES");
string Text(NativeRecord record,string field)=>record.Read(record.Fields.Single(item=>item.Name==field));
void SaveReopen(PreparedProjectConversion conversion,string name,Action<NativeMapDocument> check)
{
 var path=Path.Combine(directory,name+".dkm");using var save=new EditorProjectSaveSession(conversion.Project);save.SaveAs(path);
 var loaded=NativeMapDocument.Load(path);Expect(loaded.IsLegacyProject&&!loaded.HasChanges&&!loaded.CanUndo,"Saved conversion did not reopen clean.");check(loaded);
}
try
{
 var native=NativeMapDocument.Create(18,18,"NativeProjection");var before=Bytes(native);var revision=native.EditRevision;var history=native.GetHistory();var statistics=native.ReadActivityStatistics();
 var clean=native.PrepareProjectConversion(Path.Combine(directory,"prepared.dkm"));
 Expect(clean.IsCurrent&&clean.Project.HasChanges&&!File.Exists(Path.Combine(directory,"prepared.dkm"))&&Same(native,before)&&native.EditRevision==revision&&native.GetHistory().SnapshotBytes==history.SnapshotBytes&&ReferenceEquals(native.ReadActivityStatistics(),statistics),"Preparation changed source or wrote a target.");
 var options=clean.Project.Records().Where(record=>record.Category=="Project.MAPOPTIONS").ToArray();
 Expect(options.Length==7&&options.All(record=>Number(record,"CreaturesCount[1]")==-1&&Number(record,"Rooms[0]")==255&&Number(record,"Spells[0]")==255),"Import used new-map availability/count defaults.");
 Expect(clean.RequiresOriginalBundleRetention&&clean.SourceFiles.Count>=native.Files.Count&&clean.SourceFiles.All(file=>!file.Bytes.IsDefault),"Complete immutable original bundle missing.");
 SaveReopen(clean,"basic",loaded=>Expect(loaded.Width==18&&loaded.Height==18&&loaded.Records().Count(record=>record.Category=="Project.MAPOPTIONS")==7,"Basic DKM lost geometry/players."));
 Expect(Same(native,before)&&native.EditRevision==revision,"Saving converted project modified original.");
 native.Paint([new(3,3)],new(4,2,1));Expect(!clean.IsCurrent,"Edited source did not invalidate conversion.");native.Undo();
 var staleStatistics=native.PrepareProjectConversion();native.RecordActivity(ActivityCounterEvent.LeftMapClick);Expect(!staleStatistics.IsCurrent,"Counter changes did not invalidate conversion.");
 Refuses<ArgumentException>(()=>native.PrepareProjectConversion("bad.map"));Refuses<InvalidOperationException>(()=>clean.Project.PrepareProjectConversion());
 Console.WriteLine("PASS native independent preparation, seven original import defaults, immutable input, real DKM Save/reopen and stale data/statistics guards");

 var full=NativeMapDocument.Create(18,18,"FullProjection");
 var metadata=full.Records().Single(record=>record.Category=="Map");Set(full,metadata,"wstrMainName","Dungeon Ω");Set(full,metadata,"wstrMainDescription","Full entities Café");Set(full,metadata,"bMainObjectiveTextID","54321");
 var player=full.Records().Single(record=>record.Category=="GAMEPLAYER");Set(full,player,"bPlayerGold","-1234");Set(full,player,"bPlayerBattleRemove","258");Set(full,player,"szPlayerName","Keeper Café");
 var trigger=Add(full,"Trigger");var root=Text(trigger,"ID");
 foreach(var type in new[]{"GAMEOBJECT","GAMETRAP","GAMEDOOR","GAMENEUTRAL","GAMEHERO","GAMEKEEPER","GAMEAREA","GAMEEFFECT","GAMEBAND"})
 {
  var record=Add(full,type);var rootField=record.Fields.FirstOrDefault(field=>field.Name.EndsWith("Trigger",StringComparison.Ordinal));if(rootField is not null)Set(full,record,rootField.Name,root);
  if(type=="GAMEOBJECT")Set(full,record,"dwObjectGoldValue","4294967295");
  if(type=="GAMEHERO") {Set(full,record,"bHeroObjective","7");Set(full,record,"bHeroTargetArea","9");}
  if(type=="GAMEBAND") {var member=full.Records().First(item=>item.File==record.File&&item.Offset==record.Offset+51);Set(full,member,"bGlobalCreatureID","1");Set(full,member,"bGlobalBehavior","9");Set(full,member,"bGlobalLevel","4");Set(full,member,"wHeroTrigger",root);}
 }
 var magical=Add(full,"GAMEOBJECT",6,6);Set(full,magical,"bObjectID","117");Set(full,magical,"wObjectTrigger",root);
 var gate=Add(full,"GAMEGATE",7,7);Set(full,gate,"bGateType","33");var heart=Add(full,"GAMEGATE",8,8);Set(full,heart,"bGateType","14");Set(full,heart,"wGateHealth","87");
 full.SetScenarioName(ScenarioNameKind.Variable,0,"Gold Café");full.SetScenarioName(ScenarioNameKind.Timer,0,"Clock");
 var count=Add(full,"GAMEVARCREATURECOUNT");Set(full,count,"dwGlobalPlayer","3");Set(full,count,"dwGlobalCreatureID",LegacyCompilerRules.Elements("BadCreatures").First().Variable.ToString());Set(full,count,"dwCreatureCount","4294967295");
 var alliance=Add(full,"GAMEVARALLIANCE");Set(full,alliance,"dwGlobalPlayer","3");Set(full,alliance,"dwAllieWith","4");
 var available=new List<(string Array,int Index)>();
 foreach(var (group,kind,array) in new[]{("VarRooms",1,"Rooms"),("Traps",3,"Traps"),("Doors",4,"Doors"),("Spells",5,"Spells")})
 {
  var catalogue=LegacyCompilerRules.Elements(group);var index=catalogue.Select((item,index)=>(item,index)).First(pair=>pair.item.Variable>0&&pair.item.Variable<uint.MaxValue).index;
  var record=Add(full,"GAMEVARTHINGSTATUS");Set(full,record,"wThingType",kind.ToString());Set(full,record,"wGlobalPlayer","3");Set(full,record,"dwThingID",catalogue[index].Variable.ToString());Set(full,record,"dwThingState","258");available.Add((array,index));
 }
 var creatureParameter=Add(full,"GAMEVARCREATURE");Set(full,creatureParameter,"dwGlobalCreatureID",LegacyCompilerRules.Elements("BadCreatures").First().Variable.ToString());Set(full,creatureParameter,"dwCreatureParamType","4");Set(full,creatureParameter,"dwCreatureNewValue","4294967295");
 var speechField=metadata.Fields.Single(field=>field.Name=="szMainSpeech");Set(full,metadata,"szMainSpeech","voice");var descriptor=full.MainFile.Data.ToArray();descriptor[metadata.Offset+speechField.Offset+17]=0xE9;
 typeof(NativeFile).GetProperty("Bytes",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(full.MainFile,descriptor);
 var speech=full.MainFile.Data.Span.Slice(metadata.Offset+speechField.Offset,32).ToArray();
 full.RecordActivity(ActivityCounterEvent.LeftMapClick);full.CompilationTaskFinished();
 var fullBytes=Bytes(full);var fullStats=full.ReadActivityStatistics();var conversion=full.PrepareProjectConversion();
 Expect(Same(full,fullBytes)&&conversion.IsCurrent&&conversion.Issues.Any(issue=>issue.Code=="TextProjection")&&!conversion.HasBlockingIssues,"Full conversion changed original or has unexpected errors: "+string.Join(";",conversion.Issues));
 var projected=conversion.Project;var entities=projected.Records();
 Expect(Text(Info(projected),"Name")=="Dungeon ?"&&Text(Info(projected),"Description")=="Full entities Café"&&Number(Info(projected),"TextObjectiveID")==54321,"Metadata text/WORD projection wrong.");
 var keeper=entities.Where(record=>record.Category=="Project.MAPOPTIONS").ElementAt(2);
 foreach(var slot in available)Expect(Number(keeper,$"{slot.Array}[{slot.Index}]")==2,"Availability category/index/BYTE cast wrong.");
 var parameterField=Info(projected).Fields.Single(field=>field.Name=="CreaturesParams[0][3]");Expect(Text(Info(projected),parameterField.Name)=="4294967295","Creature parameter catalogue/value bits lost.");
 var storedSpeech=Info(projected).Fields.Single(field=>field.Name=="SpeechFile");Expect(Info(projected).File.Data.Span.Slice(Info(projected).Offset+storedSpeech.Offset,32).SequenceEqual(speech),"Full-width speech bytes after NUL lost.");
 Expect(Number(keeper,"PlayerGold")==-1234&&Number(keeper,"PlayerBattleRemove")==2&&Number(keeper,"PlayerHealth")==87&&Text(keeper,"PlayerName")=="Keeper Café","Player signed/BYTE cast/name/heart health wrong.");
 Expect(entities.Count(record=>record.Category=="Project.MAPTHING")==7&&entities.Count(record=>record.Category=="Project.MAPRECTEX")==3&&entities.Count(record=>record.Category=="Project.MTHBAND")==1,"Entity family counts wrong.");
 var objectRecord=entities.Single(record=>record.Category=="Project.MAPTHING"&&Number(record,"node.type")==3);Expect(Number(objectRecord,"dataAll")==-1,"Object unsigned gold bits lost.");
 var band=entities.Single(record=>record.Category=="Project.MTHBAND");Expect(Number(band,"heros[0].isvalid")==1&&Number(band,"heros[0].level")==4&&Number(band,"heros[1].isvalid")==0,"Band member/terminator wrong.");
 var links=entities.Where(record=>record.Category=="Project.TRIGGERLINK").ToArray();Expect(links.Any(record=>Number(record,"node.type")==0x40)&&links.Any(record=>Number(record,"node.type")==0x80)&&links.Any(record=>Number(record,"node.type")==0x100)&&links.Any(record=>Number(record,"node.type")==0x10),"Door/object/magic/member semantic roots missing.");
 Expect(Number(Info(projected),"Allies[2][3]")==1&&Number(Info(projected),"Allies[3][2]")==1&&projected.ReadScenarioNames().Slots.Single(slot=>slot.Kind==ScenarioNameKind.Variable&&slot.Index==0).StoredName=="Gold Café","Alliance/alias projection wrong.");
 Expect(projected.ReadStoredActivityStatistics()!.CreatedTime==fullStats!.CreatedTime&&projected.ReadStoredActivityStatistics()!.CompileTime==fullStats.CompileTime&&projected.ReadStoredActivityStatistics()!.LMBClicks==fullStats.LMBClicks,"Literal native/runtime statistics lost.");
 SaveReopen(conversion,"full",loaded=>Expect(loaded.Records().Count(record=>record.Category=="Project.MAPTHING")==7&&loaded.ReadScenarioNames().Slots.Any(slot=>slot.StoredName=="Gold Café"),"Saved full project lost entities/names."));
 Expect(Same(full,fullBytes),"Full Save altered source bytes.");
 Console.WriteLine("PASS native all entity families, signed metadata/player casts, heart health, raw speech, availability/creature parameters, aliases, alliances and semantic scope roots through real DKM Save/reopen");

 var allEvents=NativeMapDocument.Create(18,18,"AllEvents");var eventGate=Add(allEvents,"GAMEGATE",4,4);Set(allEvents,eventGate,"bGateType","33");
 foreach(var type in LegacyEventSchemas.All)
 {
  var record=Add(allEvents,type.RecordSignature==0xD5?"Trigger":"Action");allEvents.SetEventType(record,type);
  record=allEvents.Records().Single(item=>item.File==record.File&&item.Offset==record.Offset);
  foreach(var field in record.Fields.Where(field=>field.Name.StartsWith("Param",StringComparison.Ordinal)))Set(allEvents,record,field.Name,field.Kind==NativeValueKind.Int32?"-17":field.Kind==NativeValueKind.UInt16?"519":"7");
  if(type.RecordSignature==0xD6&&type.Type==0x20)
  {
   var raw=record.File.Data.ToArray();BinaryPrimitives.WriteUInt16LittleEndian(raw.AsSpan(record.Offset+12),5);BinaryPrimitives.WriteUInt16LittleEndian(raw.AsSpan(record.Offset+14),5);typeof(NativeFile).GetProperty("Bytes",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(record.File,raw);
  }
 }
 var eventsBefore=Bytes(allEvents);var allConverted=allEvents.PrepareProjectConversion();
 Expect(!allConverted.HasBlockingIssues&&Same(allEvents,eventsBefore),"Supported event projection has unexpected diagnostics: "+string.Join(";",allConverted.Issues));
 Expect(allConverted.Issues.Any(issue=>issue.Code=="EventPayloadProjection"),"Ignored source payload bits were silently lost without reporting.");
 var projectedEvents=allConverted.Project.Records().Where(record=>record.Category is "Project.TRIGGER" or "Project.ACTION").ToArray();
 Expect(projectedEvents.Length==LegacyEventSchemas.All.Count,"Supported event type dropped.");
 foreach(var record in allEvents.Records().Where(record=>record.File.Signature==0xD2))
 {
  var signature=BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[record.Offset..]);var type=record.File.Data.Span[record.Offset+22];var id=Number(record,"ID");
  var imported=projectedEvents.Single(item=>Number(item,"id")==allConverted.NativeEventIDs[(ushort)id]);Expect(Number(imported,"ctrl")==type&&(signature==0xD5)==(unchecked((short)Number(imported,"id"))>0),"Event type/signed ID changed incorrectly.");
  foreach(var field in LegacyEventSchemas.Find(signature,type)!.Arguments.Where(field=>field.Name.StartsWith("Param",StringComparison.Ordinal)&&LegacyEventSchemas.ParameterPageID(LegacyEventSchemas.Find(signature,type)!.Name)!=-1))
   Expect(Number(imported,"param"+field.Name[5..])==long.Parse(record.Read(field)),"Compiler/read recipe disagreement for "+signature.ToString("X")+"/"+type+"/"+field.Name);
  if(signature==0xD6&&type==0x20)Expect(Number(imported,"param5")==1,"Destroy-gate coordinates not resolved to project ID.");
 }
 SaveReopen(allConverted,"events",loaded=>Expect(loaded.Records().Count(record=>record.Category is "Project.TRIGGER" or "Project.ACTION")==LegacyEventSchemas.All.Count,"Event types lost on Save/reopen."));
 Console.WriteLine("PASS every supported native event type, independent compiler payload recipes, signed IDs and destroy-gate coordinate translation through DKM Save/reopen");

 var malformed=NativeMapDocument.Create(18,18,"Malformed");var cycle=Add(malformed,"Trigger");Set(malformed,cycle,"NextOr",Text(cycle,"ID"));var malformedMetadata=malformed.Records().Single(record=>record.Category=="Map");Set(malformed,malformedMetadata,"wMainTrigger",Text(cycle,"ID"));
 var malformedBytes=Bytes(malformed);var bad=malformed.PrepareProjectConversion();Expect(bad.HasBlockingIssues&&bad.Issues.Any(issue=>issue.Code=="CyclicEventGraph")&&Same(malformed,malformedBytes),"Cyclic graph was erased, changed or recursively accepted.");
 Set(malformed,cycle,"NextOr","32000");var dangling=malformed.PrepareProjectConversion();Expect(dangling.HasBlockingIssues&&dangling.Issues.Any(issue=>issue.Code=="MissingEventLink")&&Number(dangling.Project.Records().Single(record=>record.Category=="Project.TRIGGER"),"or")==32000,"Missing target was silently cleared.");
 var linked=NativeMapDocument.Create(18,18,"Linked");var linkedTrigger=Add(linked,"Trigger");var linkedAction=Add(linked,"Action");
 var nativeActionID=ushort.Parse(Text(linkedAction,"ID"));Set(linked,linkedTrigger,"ChildAnd",nativeActionID.ToString());Set(linked,linkedAction,"NextOr","65535");
 Set(linked,linked.Records().Single(record=>record.Category=="Map"),"wMainTrigger",Text(linkedTrigger,"ID"));
 var linkedBefore=Bytes(linked);var linkedConversion=linked.PrepareProjectConversion();var signedAction=linkedConversion.NativeEventIDs[nativeActionID];
 Expect(signedAction>32767&&signedAction!=65535&&Same(linked,linkedBefore),"Action remap reused an absent target or modified input.");
 var importedTrigger=linkedConversion.Project.Records().Single(record=>record.Category=="Project.TRIGGER");var importedAction=linkedConversion.Project.Records().Single(record=>record.Category=="Project.ACTION");
 Expect(Number(importedTrigger,"and")==signedAction&&Number(importedAction,"id")==signedAction&&Number(importedAction,"next")==65535&&linkedConversion.Issues.Any(issue=>issue.Code=="MissingEventLink"),"Signed-ID conversion did not preserve references/absent target.");
 var predecessor=BinaryPrimitives.ReadUInt16LittleEndian(importedAction.File.Data.Span.Slice(importedAction.Offset+LegacyProjectSchemas.Size("NODE"),2));
 Expect(predecessor==Number(importedTrigger,"id"),"Action predecessor lost after ID remap.");
 Console.WriteLine("PASS cyclic and absent native event links produce blocking diagnostics while preserving original graph bytes");

 var definitionProject=NativeMapDocument.CreateNewMap(new("Definitions","Definitions",18,18,PlaceHearts:true));var terrain=definitionProject.GlobalDefinitionRows().First(row=>row.Family=="Terrains"&&row.Editable);var value=definitionProject.GlobalDefinitionValues(terrain).First(item=>item.Name=="MaxHealth");
 definitionProject.SetGlobalDefinitionValues([new("Terrains",terrain.Index,value.Name,(long.Parse(value.Value)+1).ToString())]);
 var definitionNative=definitionProject.CompileLegacyProject("DefinitionNative",NativeMapDocument.Create(18,18,"Basis",NativeMapDocument.Load(@"D:\Games\DK2\Dungeon Keeper 2\Data\editor\maps\Standart\Alcatraz.kwd")));var definitionBytes=Bytes(definitionNative);var convertedDefinitions=definitionNative.PrepareProjectConversion();
 Expect(!convertedDefinitions.HasBlockingIssues&&Same(definitionNative,definitionBytes),"Definition native projection failed: "+string.Join(";",convertedDefinitions.Issues));
 var projectedTerrain=convertedDefinitions.Project.GlobalDefinitionRows().Single(row=>row.Family=="Terrains"&&row.Index==terrain.Index);
 Expect(convertedDefinitions.Project.GlobalDefinitionValues(projectedTerrain).Single(item=>item.Name=="MaxHealth").Value==(long.Parse(value.Value)+1).ToString(),"Native edited global field reverted to default.");
 SaveReopen(convertedDefinitions,"definitions",loaded=>Expect(loaded.GlobalDefinitionValues(loaded.GlobalDefinitionRows().Single(row=>row.Family=="Terrains"&&row.Index==terrain.Index)).Single(item=>item.Name=="MaxHealth").Value==(long.Parse(value.Value)+1).ToString(),"Global override lost on Save/reopen."));
 var areaTiles=new byte[8+7*7*4];BinaryPrimitives.WriteInt32LittleEndian(areaTiles,7);BinaryPrimitives.WriteInt32LittleEndian(areaTiles.AsSpan(4),7);for(var index=0;index<49;index++)BinaryPrimitives.WriteUInt32LittleEndian(areaTiles.AsSpan(8+index*4),new MapTile(2,2,1).Packed);
 var opaque=new byte[]{17,0,254,19};var areaBytesFile=LegacySectionFile.Create([new(LegacySectionFile.TileSectionSignature,1,areaTiles),new(0x12345678,9,opaque)]).ToArray();BinaryPrimitives.WriteUInt32LittleEndian(areaBytesFile,LegacySectionFile.AreaFileSignature);var area=LegacySectionFile.Parse(areaBytesFile);var areaPath=Path.Combine(directory,"area.dkz");File.WriteAllBytes(areaPath,area.ToArray());
 var areaDocument=NativeMapDocument.Load(areaPath);var areaBytes=Bytes(areaDocument);var areaConversion=areaDocument.PrepareProjectConversion();Expect(Same(areaDocument,areaBytes)&&areaConversion.Project.Records().Count(record=>record.Category=="Project.MAPOPTIONS")==7,"AREA modified input or omitted map defaults.");
 SaveReopen(areaConversion,"area",loaded=>Expect(loaded.Files.Any(file=>file.Signature==0x12345678&&file.Data.Span[12..].SequenceEqual(opaque)),"AREA opaque section lost."));
 Console.WriteLine("PASS native edited global definitions and AREA opaque sections retain working values through real DKM Save/reopen");
 Console.WriteLine("NATIVE PROJECT CONVERSION CORE CHECKS PASSED: companion publication/unified Save/windows/full regression remain required");
}
finally {Directory.Delete(directory,true);}
