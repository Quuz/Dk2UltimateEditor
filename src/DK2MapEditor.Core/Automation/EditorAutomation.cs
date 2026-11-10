using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.Core.Automation;

/// <summary>Serialized on the owner's UI thread. Never owns a second live map or bypasses history.</summary>
public sealed class EditorAutomation(IEditorAutomationHost host,AutomationPaths paths) : IDisposable
{
    private readonly string instance=Guid.NewGuid().ToString("N");
    private readonly ConditionalWeakTable<NativeMapDocument,Identity> identities=new();
    private sealed class Identity { public string Value {get;}=Guid.NewGuid().ToString("N"); }
    private MapPropertiesSession? draft;
    private MapPropertiesPlan? plan;
    private string? planToken,planState;
    private string Token()
    {
        var doc=host.Document;var identity=doc is null?"empty":identities.GetValue(doc,_=>new()).Value;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{instance}|{identity}|{doc?.EditRevision}|{host.ContextRevision}|{host.InteractionToken}|{host.Selection}"))).ToLowerInvariant();
    }
    public object State()=>new {
        instanceId=instance,stateToken=Token(),document=host.Document is {} doc?new {
            path=doc.MainFile.SourcePath,width=doc.Width,height=doc.Height,revision=doc.EditRevision,
            dirty=doc.HasChanges,canUndo=doc.CanUndo,canRedo=doc.CanRedo,format=doc.IsLegacyProject?"DKM":"Native"
        }:null,selection=host.Selection,busyReason=host.BusyReason,outputRoot=paths.Root,ui=host.UiState
    };
    public AutomationReply Execute(AutomationRequest request)
    {
        try
        {
            var tool=AutomationTools.Find(request.Tool);AutomationTools.Validate(tool,request.Arguments);
            var a=request.Arguments;
            if(request.Tool=="dk2_editor_status")return new(true,State(),State());
            if(request.Tool=="dk2_properties_cancel")
            {
                RequirePlan(S(a,"planToken"));ClearPlan();return new(true,new{cancelled=true},State());
            }
            // Only this dialog-specific command may run inside its own modal loop.
            // Do not commit the disabled owner's grid or bypass another modal operation.
            if(request.Tool=="dk2_camera_library")
            {
                RequireToken(S(a,"expectedStateToken"));
                var command=S(a,"command");
                var hasQuery=a.TryGetProperty("query",out var query);var hasID=a.TryGetProperty("pathID",out var pathID);
                if((command=="Search")!=hasQuery ||(command=="Select")!=hasID)
                    throw new AutomationException("arguments","Search requires only query; Select requires only pathID; other commands accept neither.");
                if(host.CameraLibraryBusyReason is {} reason)throw new AutomationException("camera_library_unavailable",reason);
                var view=host.CameraLibrary(command,query.ValueKind==JsonValueKind.Undefined?null:query.GetString(),pathID.ValueKind==JsonValueKind.Undefined?null:pathID.GetInt32());
                return new(true,view,State());
            }
            if(host.BusyReason is {} busy)throw new AutomationException("editor_busy",busy);
            if(a.TryGetProperty("expectedStateToken",out var expected))RequireToken(expected.GetString()!);
            if(!tool.ReadOnly)
            {
                if(!host.CommitFields())throw new AutomationException("pending_fields","Finish or cancel the current property-grid edit.");
                // Committing a human edit may have invalidated the expected map revision.
                RequireToken(S(a,"expectedStateToken"));
            }
            var result=ExecuteCore(request.Tool,a);
            return new(true,result,State());
        }
        catch(EditorHistoryMemoryException e) when(e.IsSafeToNotify)
        {return new(false,null,State(),"history_memory",e.Message);}
        catch(AutomationException e){return new(false,null,State(),e.Code,e.Message,e.Details);}
        catch(Exception e) when(e is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or OverflowException or FormatException)
        {return new(false,null,State(),"command_failed",e.Message);}
    }
    private void RequireToken(string expected)
    {
        if(!string.Equals(expected,Token(),StringComparison.Ordinal))
            throw new AutomationException("stale_state","The map or editor context changed. Read fresh state and inspect before retrying.");
    }
    private NativeMapDocument Document()=>host.Document??throw new AutomationException("no_document","Open a map first.");
    private object ExecuteCore(string name,JsonElement a)
    {
        if(name=="dk2_map_open")
        {
            var path=S(a,"path");
            if(Path.GetExtension(path).ToLowerInvariant() is not (".kwd" or ".kld" or ".dkm" or ".map" or ".dkz" or ".area"))
                throw new AutomationException("input_path","Choose a supported map file.");
            if(!Path.IsPathFullyQualified(path)||!File.Exists(path))throw new AutomationException("input_path","Use an existing absolute map path.");
            if(host.Document?.HasChanges==true&&!B(a,"discardChanges"))
                throw new AutomationException("unsaved_changes","Opening another map would discard unsaved changes.");
            ClearPlan();return host.Open(Path.GetFullPath(path),B(a,"discardChanges"));
        }
        if(name=="dk2_map_new")
        {
            if(host.Document?.HasChanges==true&&!B(a,"discardChanges"))throw new AutomationException("unsaved_changes","Creating a map would discard unsaved changes.");
            var fill=a.TryGetProperty("fillPreset",out var requestedFill)?requestedFill.GetString()!:"0x02020101";
            var fillID=fill.StartsWith("0x",StringComparison.OrdinalIgnoreCase)?uint.Parse(fill[2..],System.Globalization.NumberStyles.HexNumber):uint.Parse(fill,System.Globalization.CultureInfo.InvariantCulture);
            var options=new NewMapOptions(S(a,"fileName"),S(a,"name"),I(a,"width"),I(a,"height"),I(a,"players"),Byte(a,"version"),fillID,B(a,"placeHearts"),B(a,"nativeFormat"));
            if(options.Validate() is not NewMapProblem.None)throw new AutomationException("new_map_options","Invalid new-map options: "+options.Validate());
            if(options.NeedsLargeMapWarning&&!B(a,"acceptLargeMap"))throw new AutomationException("large_map_consent","Explicit acceptLargeMap=true is required.");
            ClearPlan();return host.NewMap(options,B(a,"discardChanges"));
        }
        var doc=Document();
        switch(name)
        {
            case "dk2_map_fingerprint":
            {
                var files=doc.Files.Select((file,index)=>new {index,signature=file.Signature,exportPath=file.ExportPath,
                    length=file.Data.Length,sha256=Convert.ToHexString(SHA256.HashData(file.Data.Span)).ToLowerInvariant()}).ToArray();
                return new {files,orderedSha256=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(files,AutomationJson.Options))).ToLowerInvariant(),documentSha256=doc.ReadEditingFingerprint()};
            }
            case "dk2_drawing_tool":return host.DrawingTool(S(a,"tool"));
            case "dk2_selection_command":
            {
                TileRect? bounds=null;
                if(S(a,"command")=="Set")
                {
                    var x=I(a,"x",-1);var y=I(a,"y",-1);var w=I(a,"width",0);var h=I(a,"height",0);
                    if(x<0||y<0||w<1||h<1||(long)x+w>doc.Width||(long)y+h>doc.Height)
                        throw new AutomationException("selection_bounds","Choose a valid selection rectangle.");
                    bounds=new(x,y,w,h);
                }
                else if(new[]{"x","y","width","height"}.Any(key=>a.TryGetProperty(key,out _)))
                    throw new AutomationException("arguments","Bounds are only valid for the Set command.");
                return host.SelectionCommand(S(a,"command"),bounds,B(a,"replaceBuffer"));
            }
            case "dk2_object_catalogue":
                return new {kind=S(a,"kind"),version=ValueCatalogues.GameVersion(doc),choices=ValueCatalogues.ForTemplate(S(a,"kind")=="SpecialItem"?"MagicalObject":S(a,"kind"),ValueCatalogues.GameVersion(doc))};
            case "dk2_object_paint":
            {
                var kind=Enum.Parse<PlacedThingKind>(S(a,"kind"));var defaults=ObjectBatchTemplate.Default(kind);var id=I(a,"typeID");
                if(id<0||!ValueCatalogues.ForTemplate(kind==PlacedThingKind.SpecialItem?"MagicalObject":kind.ToString(),ValueCatalogues.GameVersion(doc))
                    .Any(choice=>choice.Value==id.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                    throw new AutomationException("object_type","Choose a typeID from the current object catalogue.");
                var owner=I(a,"owner",defaults.Owner);var health=I(a,"health",checked((int)defaults.Health));var gold=I(a,"gold",checked((int)defaults.GoldHeld));
                var level=I(a,"level",defaults.DataAll);
                if(owner is <0 or >7||health<0||gold<0||a.TryGetProperty("level",out _)&&kind!=PlacedThingKind.Creature)
                    throw new AutomationException("object_template","Choose owner 0..7 and nonnegative health/gold; level is for creatures.");
                var template=defaults with{TypeID=(uint)id,Owner=(byte)owner,Health=(uint)health,GoldHeld=(uint)gold,DataAll=level};
                var point=Point(a,"",doc);var preview=doc.PlanObjectPainting(template,point);
                var summary=new {kind=kind.ToString(),preview.Position,preview.Locked,permission=preview.Permission.ToString(),warnings=preview.Warnings.ToString(),
                    preview.Creates,preview.TogglesDoor,removals=preview.Removals.Count,scriptedRemovals=preview.Removals.Count(r=>r.Scripted),effective=preview.EffectiveTemplate};
                if(!B(a,"apply"))return new {applied=false,preview=summary};
                if(preview.Locked)throw new AutomationException("object_locked","The terrain or record layout blocks this placement.",summary);
                if(preview.Removals.Any(r=>r.Scripted))throw new AutomationException("script_review","Review script-bound replacements manually.",summary);
                if(preview.Warnings!=ObjectBatchWarning.None&&!B(a,"allowWarnings"))throw new AutomationException("object_warnings","Review the preview and explicitly allow its warnings.",summary);
                return new {applied=true,count=doc.ApplyEditorObjectPainting(preview),preview=summary};
            }
            case "dk2_record_duplicate":
            {
                var copy=doc.DuplicateRecord(Record(doc,S(a,"recordHandle")));
                return new {recordHandle=Handle(doc,copy),copy.Category};
            }
            case "dk2_history_read":
            {
                var history=doc.GetHistory();
                return new {history.ChangeCount,history.EditorChangeCount,history.SnapshotBytes,history.Depth,
                    undoCount=history.Undo.Count,redoCount=history.Redo.Count,undo=history.Undo.Take(200),redo=history.Redo.Take(200)};
            }
            case "dk2_tiles_read":
            {
                var x=I(a,"x");var y=I(a,"y");var w=I(a,"width");var h=I(a,"height");
                if(w<=0||h<=0||(long)w*h>4096||x<0||y<0||(long)x+w>doc.Width||(long)y+h>doc.Height)
                    throw new AutomationException("tile_bounds","Read a valid rectangle of at most 4096 tiles.");
                return new {x,y,width=w,height=h,tiles=Enumerable.Range(y,h).Select(yy=>Enumerable.Range(x,w).Select(xx=>doc.GetTile(xx,yy)).ToArray()).ToArray()};
            }
            case "dk2_terrain_paint":
            {
                var start=Point(a,"start",doc);var end=Point(a,"end",doc);
                var tile=new MapTile(Byte(a,"terrain"),Byte(a,"owner"),Byte(a,"variant"));
                if(tile.Terrain is <1 or >41||tile.Owner is <1 or >7||tile.Variant is not (1 or 2))
                    throw new AutomationException("terrain_palette","Choose terrain 1..41, owner 1..7 and variant 1 or 2, as in the editor palette.");
                var shape=Enum.Parse<EditorPaintShape>(S(a,"shape"));
                if(shape==EditorPaintShape.Brush&&start!=end)throw new AutomationException("arguments","Brush accepts one tile; use Line for a drag.");
                var before=doc.EditRevision;
                var placement=doc.PaintEditorGesture(start,end,tile,shape,host.Selection);
                return new {placement=placement.ToString(),changed=before!=doc.EditRevision};
            }
            case "dk2_records_list":
            {
                var offset=I(a,"offset",0);var limit=I(a,"limit",100);
                if(offset<0||limit is <1 or >200)throw new AutomationException("arguments","offset must be nonnegative; limit must be 1..200.");
                var records=doc.Records().Select(record=>(Record:record,Handle:Handle(doc,record)))
                    .Where(row=>!a.TryGetProperty("category",out var category)||row.Record.Category==category.GetString()).ToArray();
                return new {total=records.Length,offset,records=records.Skip(offset).Take(limit).Select(row=>new {
                    recordHandle=row.Handle,category=row.Record.Category,file=row.Record.File.ExportPath,offset=row.Record.Offset,
                    length=row.Record.Length,fieldCount=row.Record.Fields.Count})};
            }
            case "dk2_record_read":
            {
                var record=Record(doc,S(a,"recordHandle"));
                if(record.Fields.Count>4000)throw new AutomationException("record_limit","This record exceeds the bridge field limit.");
                return new {recordHandle=S(a,"recordHandle"),record.Category,fields=record.Fields.Select(f=>new {
                    name=f.Name,offset=f.Offset,size=f.Size,kind=f.Kind.ToString(),f.BitMask,value=record.Read(f)})};
            }
            case "dk2_record_set":
            {
                var record=Record(doc,S(a,"recordHandle"));var field=record.Fields.SingleOrDefault(f=>f.Name==S(a,"field"))
                    ??throw new AutomationException("unknown_field","Choose a verified field returned by dk2_record_read.");
                var before=doc.EditRevision;doc.SetField(record,field,S(a,"value"));
                return new {changed=before!=doc.EditRevision,value=record.Read(field)};
            }
            case "dk2_history_undo":
            {var available=doc.CanUndo;if(available)doc.Undo();return new {applied=available};}
            case "dk2_history_redo":
            {var available=doc.CanRedo;if(available)doc.Redo();return new {applied=available};}
            case "dk2_map_validate":
            {
                var rows=NativeMapValidator.Validate(doc,includeGameplay:B(a,"includeGameplay",true));
                return new {count=rows.Count,errors=rows.Count(r=>r.Severity==DiagnosticSeverity.Error),
                    warnings=rows.Count(r=>r.Severity==DiagnosticSeverity.Warning),truncated=rows.Count>1000,
                    diagnostics=rows.Take(1000).Select(r=>new {severity=r.Severity.ToString(),r.Code,r.Message,r.FilePath,r.Region})};
            }
            case "dk2_map_save":
                return host.Save(paths.CheckSave(S(a,"path"),B(a,"overwrite")),B(a,"acceptLosses"),B(a,"allowWarnings"));
            case "dk2_map_export":
            {
                var output=paths.Check(S(a,"outputDirectory"));
                if(File.Exists(output)||Directory.Exists(output))throw new AutomationException("output_exists","Export requires a new directory.");
                return new {path=doc.SaveCopy(output),format=doc.IsLegacyProject?"DKM":"Native"};
            }
            case "dk2_map_compile":
            {
                var basis=S(a,"basisPath");var parent=paths.Check(S(a,"outputParent"));
                if(!Path.IsPathFullyQualified(basis)||!File.Exists(basis)||!Path.GetExtension(basis).Equals(".kwd",StringComparison.OrdinalIgnoreCase))
                    throw new AutomationException("input_path","Select an existing game KWD basis by absolute path.");
                if(!Directory.Exists(parent))throw new AutomationException("output_path","Create the output parent directory first.");
                return host.Compile(basis,parent,B(a,"allowWarnings"));
            }
            case "dk2_properties_prepare":return Prepare(doc,a);
            case "dk2_properties_apply":
            {
                RequirePlan(S(a,"planToken"));RequireToken(planState!);
                doc.ApplyEditorMapProperties(plan!,B(a,"largeMapAccepted"),B(a,"croppingAccepted"));
                ClearPlan();return new {applied=true};
            }
            default:throw new AutomationException("unknown_tool","Unknown editor tool.");
        }
    }
    private object Prepare(NativeMapDocument doc,JsonElement a)
    {
        ClearPlan();draft=doc.CreateMapPropertiesSession();
        try
        {
            var work=draft.WorkingDocument;var general=work.CreateMapGeneralDraft();
            var values=general.Values.ToDictionary(r=>r.Key,r=>r.Value);
            foreach(var key in MapGeneralSettings.Fields)
            {
                var argument=key.ToLowerInvariant();
                if(a.TryGetProperty(argument,out var v))
                {
                    if(!values.ContainsKey(key))throw new AutomationException("properties_layout","This field is unavailable for the current map.");
                    values[key]=v.ValueKind==JsonValueKind.String?v.GetString()!:v.GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            if(general.Editable)work.ApplyMapGeneralSettings(work.UpdateMapGeneralSettings(general,values.Select(r=>new MapGeneralInput(r.Key,r.Value))));
            MapPropertiesResize? resize=null;
            if(a.TryGetProperty("width",out _)||a.TryGetProperty("height",out _))
            {
                var w=I(a,"width",-1);var h=I(a,"height",-1);var anchor=I(a,"anchor",0);
                if(w is <7 or >1024||h is <7 or >1024||anchor is <0 or >8)throw new AutomationException("arguments","Resize requires width/height 7..1024 and anchor 0..8.");
                resize=new(w,h,anchor,B(a,"removeOutside"));
            }
            else if(a.TryGetProperty("anchor",out _)||a.TryGetProperty("removeOutside",out _))
                throw new AutomationException("arguments","anchor/removeOutside require width and height.");
            plan=doc.PrepareEditorMapProperties(draft,resize);planToken=Guid.NewGuid().ToString("N");planState=Token();
            return new {planToken,plan.Changed,plan.Resized,plan.NeedsLargeMapConsent,plan.NeedsCropConsent,plan.Crops,
                width=plan.Resize.Width,height=plan.Resize.Height,general=values};
        }
        catch{ClearPlan();throw;}
    }
    private void RequirePlan(string token)
    {
        if(plan is null||token!=planToken)throw new AutomationException("stale_plan","Prepare a new map-properties plan.");
    }
    private void ClearPlan(){draft?.Dispose();draft=null;plan=null;planToken=null;planState=null;}
    private static string Handle(NativeMapDocument doc,NativeRecord record)=>$"{doc.Files.ToList().IndexOf(record.File)}:{record.Offset}:{record.Length}:{record.Category}";
    private static NativeRecord Record(NativeMapDocument doc,string handle)=>doc.Records().SingleOrDefault(r=>Handle(doc,r)==handle)
        ??throw new AutomationException("record_handle","Record handle is unavailable. Read a fresh record list.");
    private static TilePoint Point(JsonElement a,string prefix,NativeMapDocument doc)
    {
        var x=I(a,prefix.Length==0?"x":prefix+"X");var y=I(a,prefix.Length==0?"y":prefix+"Y");
        if(x<0||y<0||x>=doc.Width||y>=doc.Height)throw new AutomationException("tile_bounds","Terrain coordinates must be inside the map.");
        return new(x,y);
    }
    private static byte Byte(JsonElement a,string name)=>checked((byte)I(a,name));
    private static int I(JsonElement a,string name,int fallback=0)=>a.TryGetProperty(name,out var v)?v.GetInt32():fallback;
    private static string S(JsonElement a,string name)=>a.GetProperty(name).GetString()!;
    private static bool B(JsonElement a,string name,bool fallback=false)=>a.TryGetProperty(name,out var v)?v.GetBoolean():fallback;
    public void Dispose()=>ClearPlan();
}
