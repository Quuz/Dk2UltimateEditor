using DK2MapEditor.Core.Editing;
var directory=Path.Combine(Path.GetTempPath(),"EditorPropertiesCallerChecks",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
void Expect(bool value,string text){if(!value)throw new InvalidOperationException(text);}
void Refuses(Action action){try{action();}catch(Exception e) when(e is InvalidOperationException or ArgumentException){return;}throw new InvalidOperationException("Expected caller refusal.");}
Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument doc)=>doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
bool Exact(Dictionary<NativeFile,byte[]> bytes)=>bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value));
NativeMapDocument Clean(bool native,Action<NativeMapDocument>? configure=null)
{
 var created=NativeMapDocument.CreateNewMap(new("Caller","Caller",24,24,NativeFormat:native));configure?.Invoke(created);
 var path=created.SaveCopy(Path.Combine(directory,Guid.NewGuid().ToString("N")),temporary:true);return NativeMapDocument.Load(path);
}
NativeRecord Object(NativeMapDocument doc,TilePoint point)=>doc.IsLegacyProject?doc.AddLegacyRecord("Object",point):doc.AddRecord(RecordTemplate.All.First(row=>row.Structure=="GAMEOBJECT"),point);
try
{
 foreach(var native in new[]{false,true})
 {
  var format=native?"native":"project";var doc=Clean(native);var before=Snapshot(doc);var revision=doc.EditRevision;
  using(var generic=doc.CreateMapPropertiesSession())doc.ApplyMapProperties(doc.PrepareMapProperties(generic));
  Expect(Exact(before)&&doc.EditorChangeCount==0&&!doc.HasChanges&&doc.EditRevision==revision,"Generic no-op contract changed.");
  using(var source=doc.CreateMapPropertiesSession())
  {
   var plan=doc.PrepareEditorMapProperties(source);Expect(!plan.Changed&&doc.EditorMapPropertiesChangeIncrements(plan)==1,"Equal source preview lost original counter.");
   doc.ApplyEditorMapProperties(plan);Refuses(()=>doc.ApplyEditorMapProperties(plan));
  }
  Expect(Exact(before)&&doc.EditorChangeCount==1&&doc.HasChanges&&doc.GetHistory().Undo.Count==1,"Equal source Apply did not retain one Undo/dirty/counter.");
  doc.Undo();Expect(Exact(before)&&doc.EditorChangeCount==0&&!doc.HasChanges&&doc.CanRedo,"Equal source Undo failed.");doc.Redo();Expect(Exact(before)&&doc.EditorChangeCount==1&&doc.HasChanges,"Equal source Redo failed.");
  Console.WriteLine($"PASS {format} equal original properties Apply retains Undo/dirty/count, exact Undo/Redo and unchanged generic no-op contract");

  foreach(var anchor in Enumerable.Range(0,9))foreach(var dimensions in new[]{(Width:27,Height:29),(Width:21,Height:19)})
  {
   doc=Clean(native,map=>{Object(map,new(8,8));Object(map,new(12,11));});before=Snapshot(doc);
   using var source=doc.CreateMapPropertiesSession();var plan=doc.PrepareEditorMapProperties(source,new(dimensions.Width,dimensions.Height,anchor,RemoveOutside:true));
   var shifted=(dimensions.Width-24)*(anchor%3)/2!=0||(dimensions.Height-24)*(anchor/3)/2!=0;
   var count=shifted?4u:2u;Expect(!plan.NeedsCropConsent&&doc.EditorMapPropertiesChangeIncrements(plan)==count,"Original options+resize+per-thing shift count mismatch.");
   doc.ApplyEditorMapProperties(plan);Expect(doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==1,"Resize counter confused atomic history steps.");
   var after=Snapshot(doc);doc.Undo();Expect(Exact(before)&&doc.EditorChangeCount==0,"Resize source Undo failed.");doc.Redo();Expect(Exact(after)&&doc.EditorChangeCount==count,"Resize source Redo failed.");
  }
  Console.WriteLine($"PASS {format} all nine anchors, odd grow/shrink, separate source resize/shift increments and exact one-step replay");

  foreach(var point in new[]{new TilePoint(0,5),new(20,5),new(5,0),new(5,20),new(19,19)})
  {
   doc=Clean(native,map=>Object(map,point));before=Snapshot(doc);using var source=doc.CreateMapPropertiesSession();
   var plan=doc.PrepareEditorMapProperties(source,new(21,21,RemoveOutside:true));var outside=point.X<=0||point.X>=20||point.Y<=0||point.Y>=20;
   Expect(plan.Crops.Sum(row=>row.Count)==(outside?1:0)&&doc.EditorMapPropertiesChangeIncrements(plan)==(outside?3:2),"Original fixed-boundary crop/count mismatch.");
   if(outside){Refuses(()=>doc.ApplyEditorMapProperties(plan));Expect(Exact(before)&&doc.EditorChangeCount==0,"Declined crop changed owner.");}
   doc.ApplyEditorMapProperties(plan,croppingAccepted:outside);Expect(doc.EditorChangeCount==(outside?3:2),"Accepted source crop count mismatch.");doc.Undo();Expect(Exact(before)&&doc.EditorChangeCount==0,"Boundary crop Undo failed.");
  }
  doc=Clean(native,map=>Object(map,new(23,8)));before=Snapshot(doc);
  using(var source=doc.CreateMapPropertiesSession())
  {
   var plan=doc.PrepareEditorMapProperties(source,new(21,24,8,RemoveOutside:true));Expect(plan.Crops.Sum(row=>row.Count)==1&&doc.EditorMapPropertiesChangeIncrements(plan)==4,"Moved then removed thing must count both calls.");
   doc.ApplyEditorMapProperties(plan,croppingAccepted:true);Expect(doc.EditorChangeCount==4,"Move+crop source result wrong.");doc.Undo();Expect(Exact(before)&&doc.EditorChangeCount==0,"Move+crop source Undo wrong.");
  }
  Console.WriteLine($"PASS {format} fixed outer boundaries, interior retention, declined crop and moved-then-removed counter with exact Undo");

  doc=Clean(native);before=Snapshot(doc);using(var source=doc.CreateMapPropertiesSession())
  {
   var plan=doc.PrepareEditorMapProperties(source);doc.Paint([new(4,4)],new(4,2,1));doc.Undo();Refuses(()=>doc.ApplyEditorMapProperties(plan));Expect(Exact(before)&&doc.EditorChangeCount==0,"Stale editor properties plan applied.");
  }
  using(var generic=doc.CreateMapPropertiesSession())Refuses(()=>doc.ApplyEditorMapProperties(doc.PrepareMapProperties(generic)));
  using(var canceled=doc.CreateMapPropertiesSession())
  {
   var plan=doc.PrepareEditorMapProperties(canceled);canceled.Dispose();Refuses(()=>doc.ApplyEditorMapProperties(plan));
  }
  doc.SetHistoryDepth(0);using(var source=doc.CreateMapPropertiesSession())doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(source));
  Expect(Exact(before)&&doc.HasChanges&&doc.EditorChangeCount==1&&!doc.CanUndo&&!doc.CanRedo,"Zero depth suppressed source counter or changed data.");
  using var save=new EditorSaveSession(doc);var target=Path.Combine(directory,"normal-"+native+".dkm");save.Save(save.PrepareSaveAs(target),true);
  Expect(save.Document.EditorChangeCount==0&&!save.Document.HasChanges&&!save.Document.CanUndo,"Normal Save did not reset source no-op state.");
  Console.WriteLine($"PASS {format} stale/generic/canceled caller refusal, zero-depth equal source Apply and normal Save reset");

  doc=Clean(native,map=>
  {
   if(!native)
   {
    foreach(var kind in new[]{"Creature","Door","Trap","Object","MagicalObject","Area","Gate","Effect"})map.AddLegacyRecord(kind,new(6,7));
   }
   else
   {
    foreach(var kind in new[]{"GAMENEUTRAL","GAMEHERO","GAMEKEEPER","GAMEDOOR","GAMETRAP","GAMEOBJECT","GAMEAREA","GAMEGATE","GAMEEFFECT"})
    {
     var row=map.AddRecord(RecordTemplate.All.First(template=>template.Structure==kind),new(6,7));
     if(kind=="GAMEGATE")map.SetField(row,row.Fields.Single(field=>field.Name=="bGateType"),"33");
    }
    var magical=Object(map,new(6,7));map.SetField(magical,magical.Fields.Single(field=>field.Name=="bObjectID"),"117");
    var heart=map.AddRecord(RecordTemplate.All.First(template=>template.Structure=="GAMEGATE"),new(8,8));
    map.SetField(heart,heart.Fields.Single(field=>field.Name=="bGateType"),"14");
   }
  });
  before=Snapshot(doc);using(var source=doc.CreateMapPropertiesSession())
  {
   var plan=doc.PrepareEditorMapProperties(source,new(27,29,8,RemoveOutside:true));var count=native?12u:10u;
   Expect(!plan.NeedsCropConsent&&doc.EditorMapPropertiesChangeIncrements(plan)==count,"Five thing families/three rectangle families count mismatch or native heart counted as gate.");
   doc.ApplyEditorMapProperties(plan);Expect(doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==1,"All-family source Apply did not retain one history step.");
   doc.Undo();Expect(Exact(before)&&doc.EditorChangeCount==0,"All-family source Undo lost bytes/count.");
  }
  Console.WriteLine($"PASS {format} all five source thing families and three rectangle families count individually on shift, native heart excluded, exact one-step Undo");

  foreach(var effect in new[]{false,true})
  {
   doc=Clean(native,map=>
   {
    var kind=effect?"GAMEEFFECT":"GAMEAREA";
    var row=native?map.AddRecord(RecordTemplate.All.First(template=>template.Structure==kind),new(5,5)):map.AddLegacyRecord(effect?"Effect":"Area",new(5,5));
    var field=native?(effect?"wEffectWidth":"dwAreaLocationEndX"):"rect.x2";
    map.SetField(row,row.Fields.Single(item=>item.Name==field),native&&effect?"15":"20");
   });
   before=Snapshot(doc);using var source=doc.CreateMapPropertiesSession();
   var plan=doc.PrepareEditorMapProperties(source,new(21,24,RemoveOutside:true));
   Expect(plan.Crops.Sum(row=>row.Count)==1&&doc.EditorMapPropertiesChangeIncrements(plan)==3,"Partial rectangle/effect touching fixed border must be fully removed.");
   Refuses(()=>doc.ApplyEditorMapProperties(plan));Expect(Exact(before)&&doc.EditorChangeCount==0,"Rectangle crop decline changed owner.");
   doc.ApplyEditorMapProperties(plan,croppingAccepted:true);Expect(doc.EditorChangeCount==3,"Rectangle removal count incorrect.");
   doc.Undo();Expect(Exact(before)&&doc.EditorChangeCount==0,"Rectangle crop Undo lost bytes/count.");
  }
  Console.WriteLine($"PASS {format} partially boundary-crossing area/effect whole-record crop, independent source increments, declined consent and exact Undo");

 }
 Console.WriteLine("EDITOR PROPERTIES CALLER CORE CHECKS PASSED: actual windows/full regression/other source callers remain required");
}
finally
{
 var root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"EditorPropertiesCallerChecks"))+Path.DirectorySeparatorChar;var resolved=Path.GetFullPath(directory);
 if(!resolved.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Private cleanup escaped root.");Directory.Delete(resolved,recursive:true);
}
