using System.Diagnostics;
using System.Text.Json;
using DK2MapEditor.Core.Automation;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Mcp;

var root=Path.GetFullPath(args.Single());
Directory.CreateDirectory(root);
void Expect(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
JsonElement Json(object? value)=>JsonSerializer.SerializeToElement(value,AutomationJson.Options);
string Token(EditorAutomation engine)=>Json(engine.State()).GetProperty("stateToken").GetString()!;
AutomationReply Call(EditorAutomation engine,string tool,object a)=>engine.Execute(new(tool,Json(a)));
string Code(AutomationReply reply)=>reply.Code??"success";
var host=new TestHost(NativeMapDocument.CreateNewMap(new("McpFixture","MCP",24,24)));
using var engine=new EditorAutomation(host,new(root));
var baseline=host.Document!.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
bool Exact()=>baseline.All(r=>r.Key.Data.Span.SequenceEqual(r.Value));
var fingerprint=Call(engine,"dk2_map_fingerprint",new {});
Expect(fingerprint.Ok,"Fingerprint read failed.");
var originalFingerprint=Json(fingerprint.Result).GetRawText();
Expect(Json(fingerprint.Result).GetProperty("files").GetArrayLength()==host.Document.Files.Count,"Fingerprint omitted loaded buffers.");
// A typed DKM level override lives only in editor metadata, outside Files.
var modifierDefaults=Enumerable.Range(0,32).SelectMany(stat=>Enumerable.Range(2,9)
    .Select(level=>new CreatureLevelModifierRecord(65,(uint)stat,100,(uint)level,0))
    .Append(new CreatureLevelModifierRecord(224,(uint)stat,100,10,0))).ToArray();
using(var metadata=host.Document.CreateCreatureLevelModifierSession(modifierDefaults))
{
    var levels=metadata.Draft.Current.Levels.ToArray();levels[0]=175;
    metadata.Draft.Store(new(levels,metadata.Draft.Current.FirstPerson));
    host.Document.ApplyCreatureLevelModifiers(metadata);
}
Expect(Exact(),"Metadata fixture unexpectedly changed loaded map files.");
var metadataFingerprint=Json(Call(engine,"dk2_map_fingerprint",new {}).Result);
Expect(metadataFingerprint.GetProperty("orderedSha256").GetString()==Json(fingerprint.Result).GetProperty("orderedSha256").GetString()
    &&metadataFingerprint.GetProperty("documentSha256").GetString()!=Json(fingerprint.Result).GetProperty("documentSha256").GetString(),
    "Document fingerprint missed an edit confined to editor metadata.");
Expect(Call(engine,"dk2_history_undo",new {expectedStateToken=Token(engine)}).Ok
    &&Json(Call(engine,"dk2_map_fingerprint",new {}).Result).GetRawText()==originalFingerprint,"Metadata Undo fingerprint differs.");
Console.WriteLine("PASS read-only ordered file fingerprints, metadata-only change detection and exact Undo");
var first=Token(engine);
Expect(Call(engine,"dk2_map_fingerprint",new {}).Ok&&Token(engine)==first,"Fingerprint changed the live state token.");
var paint=new {expectedStateToken=first,startX=5,startY=5,endX=5,endY=5,terrain=4,owner=3,variant=1,shape="Brush"};
Expect(Call(engine,"dk2_terrain_paint",paint).Ok&&!Exact(),"MCP terrain edit did not change the live fixture.");
Expect(Code(Call(engine,"dk2_terrain_paint",paint))=="stale_state","Stale mutation was accepted.");
Expect(Json(Call(engine,"dk2_map_fingerprint",new {}).Result).GetRawText()!=originalFingerprint,"Fingerprint did not observe changed bytes.");
var painted=host.Document.GetTile(5,5);
Expect(Call(engine,"dk2_history_undo",new {expectedStateToken=Token(engine)}).Ok&&Exact(),"MCP Undo failed exact file restoration.");
Expect(Json(Call(engine,"dk2_map_fingerprint",new {}).Result).GetRawText()==originalFingerprint,"Undo did not restore ordered buffer fingerprints.");
Expect(Call(engine,"dk2_history_redo",new {expectedStateToken=Token(engine)}).Ok&&host.Document.GetTile(5,5)==painted,"MCP Redo failed.");
Expect(Code(Call(engine,"dk2_tiles_read",new {x=0,y=0,width=500,height=500}))=="tile_bounds","Oversized tile read was accepted.");
Expect(Code(Call(engine,"dk2_terrain_paint",new {expectedStateToken=Token(engine),startX=5,startY=5,endX=5,endY=5,terrain=255,owner=3,variant=1,shape="Brush"}))=="terrain_palette","Terrain command bypassed the editor palette.");
Console.WriteLine("PASS live Core terrain command, stale rejection, exact Undo/Redo and bounded tile reads");

host.BusyReason="Human dialog";
Expect(Code(Call(engine,"dk2_history_undo",new {expectedStateToken=Token(engine)}))=="editor_busy","Busy editor was mutated.");
Expect(Call(engine,"dk2_editor_status",new {}).Ok,"Busy status must remain readable.");
host.BusyReason=null;
host.CommitAction=()=>host.Document.PaintEditorGesture(new(7,7),new(7,7),new(5,3,1),EditorPaintShape.Brush);
Expect(Code(Call(engine,"dk2_history_undo",new {expectedStateToken=Token(engine)}))=="stale_state","Pending human field commit bypassed revision guard.");
host.CommitAction=null;
var selectionToken=Token(engine);host.Selection=new(1,1,2,2);
Expect(Code(Call(engine,"dk2_history_undo",new {expectedStateToken=selectionToken}))=="stale_state","Changed selection bypassed state guard.");
var before=host.Document.EditRevision;
Expect(Call(engine,"dk2_terrain_paint",new {expectedStateToken=Token(engine),startX=8,startY=8,endX=8,endY=8,terrain=4,owner=3,variant=1,shape="Brush"}).Ok
    &&host.Document.EditRevision==before,"Terrain paint ignored the current selection.");
host.Selection=null;
Console.WriteLine("PASS busy/dialog, pending human commit and selection guards; outside-selection gesture is a no-op");

var prepared=Call(engine,"dk2_properties_prepare",new {expectedStateToken=Token(engine),name="DraftName",width=26,height=25,anchor=4});
Expect(prepared.Ok&&host.Document.Width==24,"Properties prepare changed the live map.");
var plan=Json(prepared.Result).GetProperty("planToken").GetString();
Expect(Call(engine,"dk2_properties_cancel",new {planToken=plan}).Ok&&host.Document.Width==24,"Draft Cancel changed live dimensions.");
Expect(Code(Call(engine,"dk2_properties_apply",new {expectedStateToken=Token(engine),planToken=plan}))=="stale_plan","Cancelled draft was applied.");
prepared=Call(engine,"dk2_properties_prepare",new {expectedStateToken=Token(engine),name="DraftName",width=26,height=25,anchor=4});
Expect(prepared.Ok,"Second properties prepare failed: "+prepared.Message);
plan=Json(prepared.Result).GetProperty("planToken").GetString();
Expect(Call(engine,"dk2_properties_apply",new {expectedStateToken=Token(engine),planToken=plan}).Ok&&host.Document.Width==26&&host.Document.Height==25,"Properties Apply failed.");
Expect(Call(engine,"dk2_history_undo",new {expectedStateToken=Token(engine)}).Ok&&host.Document.Width==24&&host.Document.Height==24,"Properties plan was not one atomic Undo.");
prepared=Call(engine,"dk2_properties_prepare",new {expectedStateToken=Token(engine),width=121,height=121});
Expect(prepared.Ok,"Large preview failed.");
plan=Json(prepared.Result).GetProperty("planToken").GetString();
Expect(!Call(engine,"dk2_properties_apply",new {expectedStateToken=Token(engine),planToken=plan}).Ok&&host.Document.Width==24,"Large preview bypassed consent.");
Expect(Call(engine,"dk2_properties_cancel",new {planToken=plan}).Ok,"Large plan cancellation failed.");
Console.WriteLine("PASS independent properties draft, Cancel, atomic Apply/Undo, explicit large-map consent");

var rows=Call(engine,"dk2_records_list",new {limit=200});
Expect(rows.Ok,"Record listing failed.");
var list=Json(rows.Result).GetProperty("records");
var editable=list.EnumerateArray().First(r=>r.GetProperty("fieldCount").GetInt32()>0);
var handle=editable.GetProperty("recordHandle").GetString();
var record=Call(engine,"dk2_record_read",new {expectedStateToken=Token(engine),recordHandle=handle});
Expect(record.Ok,"Record field read failed.");
Expect(Code(Call(engine,"dk2_record_set",new {expectedStateToken=Token(engine),recordHandle=handle,field="not-a-field",value="1"}))=="unknown_field","Unknown field was accepted.");
// General name is a verified editable field. The next list handle is resolved from the same document, never arbitrary offsets.
var general=host.Document.CreateMapGeneralDraft().Record!;
var generalHandle=list.EnumerateArray().Single(r=>r.GetProperty("offset").GetInt32()==general.Offset&&r.GetProperty("category").GetString()==general.Category)
    .GetProperty("recordHandle").GetString();
Expect(Call(engine,"dk2_record_set",new {expectedStateToken=Token(engine),recordHandle=generalHandle,field="Name",value="ViaMcp"}).Ok
    &&host.Document.CreateMapGeneralDraft().Values["Name"]=="ViaMcp","Verified field edit did not reach document.");
Expect(Call(engine,"dk2_history_undo",new {expectedStateToken=Token(engine)}).Ok&&host.Document.CreateMapGeneralDraft().Values["Name"]!="ViaMcp","Field Undo failed.");
Console.WriteLine("PASS paginated handles, verified fields, record edit and Undo");

var token=Token(engine);
Expect(Code(Call(engine,"dk2_map_open",new {expectedStateToken=token,path=Path.Combine(root,"missing.dkm")}))=="input_path","Missing input was accepted.");
var save=Path.Combine(root,"FromMcp.dkm");
Expect(Call(engine,"dk2_map_save",new {expectedStateToken=Token(engine),path=save}).Ok&&File.Exists(save),"Core host Save route failed.");
Expect(Code(Call(engine,"dk2_map_save",new {expectedStateToken=Token(engine),path=save}))=="output_exists","Implicit overwrite was allowed.");
Expect(Code(Call(engine,"dk2_map_save",new {expectedStateToken=Token(engine),path=Path.Combine(root,"x.kwd")}))=="save_format","KWD Save As was incorrectly introduced.");
Expect(Code(Call(engine,"dk2_map_export",new {expectedStateToken=Token(engine),outputDirectory=root+"-sibling"}))=="output_path","Sibling prefix escaped output root.");
Expect(Code(Call(engine,"dk2_map_export",new {expectedStateToken=Token(engine),outputDirectory=Path.Combine(root,"..","escape")}))=="output_path","Traversal escaped output root.");
var exported=Path.Combine(root,"Copy");
Expect(Call(engine,"dk2_map_export",new {expectedStateToken=Token(engine),outputDirectory=exported}).Ok&&Directory.EnumerateFiles(exported,"*.dkm",SearchOption.AllDirectories).Any(),"Copy was not exported.");
Expect(Code(Call(engine,"dk2_map_export",new {expectedStateToken=Token(engine),outputDirectory=exported}))=="output_exists","Export reused an existing directory.");
host.Document.PaintEditorGesture(new(9,9),new(9,9),new(4,3,1),EditorPaintShape.Brush);
Expect(Code(Call(engine,"dk2_map_open",new {expectedStateToken=Token(engine),path=save}))=="unsaved_changes","Open silently discarded edits.");
Expect(Call(engine,"dk2_map_open",new {expectedStateToken=Token(engine),path=save,discardChanges=true}).Ok,"Explicit discard/open failed.");
var oldToken=Token(engine);host.ContextRevision++;
Expect(Code(Call(engine,"dk2_history_undo",new {expectedStateToken=oldToken}))=="stale_state","Owner context did not invalidate writes.");
Expect(Call(engine,"dk2_map_validate",new {}).Ok,"Validator bridge failed.");
Console.WriteLine("PASS real Core save/reopen/export, no implicit overwrite/discard, output containment and validator");

// A real reparse-point escape, when the OS permits creating one.
var target=Path.Combine(root,"Actual");Directory.CreateDirectory(target);var link=Path.Combine(root,"Redirect");
try{Directory.CreateSymbolicLink(link,target);}
catch(IOException) when(OperatingSystem.IsWindows())
{
    // Windows directory junctions need no symbolic-link privilege. This helper is test setup only.
    var setup=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
    setup.ArgumentList.Add("-NoProfile");setup.ArgumentList.Add("-NonInteractive");setup.ArgumentList.Add("-Command");
    setup.ArgumentList.Add("$ErrorActionPreference='Stop'; New-Item -ItemType Junction -Path '"+link.Replace("'","''")+"' -Target '"+target.Replace("'","''")+"' | Out-Null");
    using var junction=Process.Start(setup)!;await junction.WaitForExitAsync();
    Expect(junction.ExitCode==0,"Could not prepare the junction test: "+await junction.StandardError.ReadToEndAsync());
}
Expect(Code(Call(engine,"dk2_map_export",new {expectedStateToken=Token(engine),outputDirectory=Path.Combine(link,"Copy")}))=="output_path","Junction/link output was accepted.");
Console.WriteLine("PASS reparse-point output rejection");

// In-memory malformed JSON/lifecycle plus newer protocol discovery fallback.
var wire=string.Join("\n",new[]{
    "{bad}",
    "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"server/discover\"}",
    "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}",
    "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-11-25\",\"capabilities\":{},\"clientInfo\":{\"name\":\"checks\",\"version\":\"1\"}}}",
    "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}",
    "{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"tools/call\",\"params\":{\"name\":\"dk2_tiles_read\",\"arguments\":{\"x\":0,\"y\":0,\"width\":1,\"height\":1,\"unknown\":true}}}"
})+"\n";
var protocolOutput=new StringWriter();var protocolLog=new StringWriter();
await new McpStdioServer(new StringReader(wire),protocolOutput,protocolLog,(_,_)=>throw new InvalidOperationException("Invalid requests must not dispatch.")).RunAsync();
var responses=protocolOutput.ToString().Split('\n',StringSplitOptions.RemoveEmptyEntries).Select(s=>JsonDocument.Parse(s).RootElement.Clone()).ToArray();
Expect(responses.Length==5&&responses[0].GetProperty("error").GetProperty("code").GetInt32()==-32700
    &&responses[1].GetProperty("error").GetProperty("code").GetInt32()==-32601
    &&responses[2].GetProperty("error").GetProperty("code").GetInt32()==-32000
    &&responses[3].GetProperty("result").GetProperty("protocolVersion").GetString()=="2025-11-25"
    &&responses[4].GetProperty("error").GetProperty("code").GetInt32()==-32602,"Protocol rejection/lifecycle failure.");
Console.WriteLine("PASS malformed JSON, discovery fallback, initialize lifecycle and strict argument schemas");

// Actual MCP executable -> stdio -> current-user named pipe -> serialized live Core owner.
// This does not launch WPF, the renderer or the game.
var pipe="dk2-checks-"+Guid.NewGuid().ToString("N");
using var bridge=new EditorBridgeServer(pipe,(request,_)=>Task.FromResult(engine.Execute(request)));
var mcp=Path.GetFullPath("src/DK2MapEditor.Mcp/bin/Release/net10.0/DK2MapEditor.Mcp.dll");
var start=new ProcessStartInfo("dotnet"){RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true};
start.ArgumentList.Add(mcp);start.ArgumentList.Add("--pipe="+pipe);
using var process=Process.Start(start)!;
var stderr=process.StandardError.ReadToEndAsync();
async Task<JsonElement> Rpc(object message)
{
    await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(message,AutomationJson.Options));
    await process.StandardInput.FlushAsync();
    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
    var line=await process.StandardOutput.ReadLineAsync(timeout.Token)??throw new IOException("MCP exited early.");
    using var document=JsonDocument.Parse(line);return document.RootElement.Clone();
}
var initialized=await Rpc(new {jsonrpc="2.0",id=1,method="initialize",@params=new {protocolVersion="2025-11-25",capabilities=new {},clientInfo=new {name="checks",version="1"}}});
Expect(initialized.GetProperty("result").GetProperty("serverInfo").GetProperty("name").GetString()=="dk2-map-editor","Actual MCP handshake failed.");
await process.StandardInput.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");
var tools=await Rpc(new {jsonrpc="2.0",id=2,method="tools/list"});
Expect(tools.GetProperty("result").GetProperty("tools").GetArrayLength()==25,"Actual tool discovery failed.");
var status=await Rpc(new {jsonrpc="2.0",id=3,method="tools/call",@params=new {name="dk2_editor_status",arguments=new {}}});
var remoteToken=status.GetProperty("result").GetProperty("structuredContent").GetProperty("state").GetProperty("stateToken").GetString();
Expect(remoteToken==Token(engine),"The bridge reached a different document.");
var remoteEdit=await Rpc(new {jsonrpc="2.0",id=4,method="tools/call",@params=new {name="dk2_terrain_paint",
    arguments=new {expectedStateToken=remoteToken,startX=10,startY=10,endX=10,endY=10,terrain=4,owner=3,variant=1,shape="Brush"}}});
Expect(!remoteEdit.GetProperty("result").GetProperty("isError").GetBoolean()&&host.Document.GetTile(10,10).Terrain==4,"Actual transport edit failed.");
var stale=await Rpc(new {jsonrpc="2.0",id=5,method="tools/call",@params=new {name="dk2_history_undo",arguments=new {expectedStateToken=remoteToken}}});
Expect(stale.GetProperty("result").GetProperty("isError").GetBoolean()
    &&stale.GetProperty("result").GetProperty("structuredContent").GetProperty("code").GetString()=="stale_state","Transport lost structured errors.");
process.StandardInput.Close();await process.WaitForExitAsync();
Expect(process.ExitCode==0&&string.IsNullOrWhiteSpace(await stderr),"MCP stdout/stderr or clean shutdown failed.");
Console.WriteLine("PASS actual MCP process -> stdio -> named pipe -> live Core document; tools, mutation, structured stale errors, clean EOF");

// New MCP surface: same Core factories/painting; selection UI forwarding is checked on the actual WPF owner separately.
var newToken=Token(engine);
Expect(Code(Call(engine,"dk2_map_new",new {expectedStateToken=newToken,fileName="McpNew",name="McpNew",width=121,height=121,players=1,version=17,discardChanges=true}))=="large_map_consent","New-map size consent was bypassed.");
Expect(Call(engine,"dk2_map_new",new {expectedStateToken=Token(engine),fileName="McpNew",name="McpNew",width=24,height=24,players=1,version=17,discardChanges=true}).Ok,"New-map host route failed.");
Expect(host.Document.Width==24&&host.Document.CreateMapGeneralDraft().Values["Name"]=="McpNew","New-map adoption failed.");
Expect(Call(engine,"dk2_terrain_paint",new {expectedStateToken=Token(engine),startX=10,startY=10,endX=10,endY=10,terrain=8,owner=3,variant=1,shape="Brush"}).Ok,"Object-test terrain preparation failed.");
var objectsBefore=host.Document.ObjectManagerEntries().Count;
var objectPreview=Call(engine,"dk2_object_paint",new {expectedStateToken=Token(engine),kind="Creature",typeID=1,x=10,y=10,owner=3,level=2});
Expect(objectPreview.Ok&&host.Document.ObjectManagerEntries().Count==objectsBefore,"Object preview mutated the map.");
var placement=Call(engine,"dk2_object_paint",new {expectedStateToken=Token(engine),kind="Creature",typeID=1,x=10,y=10,owner=3,level=2,apply=true,allowWarnings=true});
Expect(placement.Ok&&host.Document.ObjectManagerEntries().Count==objectsBefore+1,"Object painting failed: "+placement.Message);
Expect(Call(engine,"dk2_history_undo",new {expectedStateToken=Token(engine)}).Ok&&host.Document.ObjectManagerEntries().Count==objectsBefore,"Object painting Undo failed.");
Expect(Call(engine,"dk2_object_catalogue",new {kind="Creature"}).Ok&&Call(engine,"dk2_history_read",new {}).Ok,"Read-only new tools failed.");
var contextToken=Token(engine);host.InteractionToken="changed human clipboard";
Expect(Code(Call(engine,"dk2_history_redo",new {expectedStateToken=contextToken}))=="stale_state","Changed clipboard context bypassed the state guard.");
Console.WriteLine("PASS new-map host, large-map consent, object catalogue/preview/painting/Undo, history metadata and clipboard-context guard");

var disconnected=new EditorBridgeClient("dk2-unavailable-"+Guid.NewGuid().ToString("N"));
try{await disconnected.ExecuteAsync(new("dk2_editor_status",Json(new {})));throw new InvalidOperationException("Disconnected bridge succeeded.");}
catch(AutomationException error){Expect(error.Code=="editor_unavailable","Disconnected error code lost.");}
Console.WriteLine("PASS clear disconnected-editor response; NO WPF, GUI, renderer or game executed");

sealed class TestHost(NativeMapDocument document):IEditorAutomationHost
{
    public NativeMapDocument Document {get;private set;}=document;
    public long ContextRevision {get;set;}
    public string InteractionToken {get;set;}="";
    public object NewMap(NewMapOptions options,bool discardChanges){Document=NativeMapDocument.CreateNewMap(options,Document);ContextRevision++;return new {created=true};}
    public TileRect? Selection {get;set;}
    public string? BusyReason {get;set;}
    public Action? CommitAction {get;set;}
    public bool CommitFields(){CommitAction?.Invoke();return true;}
    public object Open(string path,bool discardChanges){Document=NativeMapDocument.Load(path);ContextRevision++;return new {path};}
    public object Save(string path,bool acceptLosses,bool allowWarnings)
    {
        using var session=new EditorSaveSession(Document);
        var plan=session.PrepareSaveAs(path);
        if(!session.Save(plan,acceptLosses))throw new AutomationException("save_not_completed","Save declined.");
        Document=session.Document;ContextRevision++;return new {path};
    }
    public object Compile(string basisPath,string outputParent,bool allowWarnings)
        =>throw new AutomationException("headless_host","WPF caller compilation is build-checked only; no GUI is executed.");
}
