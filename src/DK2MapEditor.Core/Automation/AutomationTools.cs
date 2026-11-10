using System.Text.Json;

namespace DK2MapEditor.Core.Automation;

public sealed record AutomationTool(string Name,string Description,JsonElement InputSchema,bool ReadOnly);
public static class AutomationTools
{
    private const string Token="\"expectedStateToken\":{\"type\":\"string\",\"description\":\"Exact stateToken from the latest result. Refresh after every edit or human action.\"}";
    private static AutomationTool Tool(string name,string description,string properties,string required="",bool readOnly=false)
    {
        using var json=JsonDocument.Parse("{\"type\":\"object\",\"properties\":{"+properties+"},\"additionalProperties\":false,\"required\":["+
            string.Join(",",required.Split(',',StringSplitOptions.RemoveEmptyEntries).Select(value=>JsonSerializer.Serialize(value)))+"]}");
        return new(name,description,json.RootElement.Clone(),readOnly);
    }
    private static string S(string name)=>"\""+name+"\":{\"type\":\"string\"}";
    private static string I(string name)=>"\""+name+"\":{\"type\":\"integer\"}";
    private static string B(string name)=>"\""+name+"\":{\"type\":\"boolean\"}";
    public static IReadOnlyList<AutomationTool> All { get; } = [
        Tool("dk2_editor_status","Read the live editor state, output root, busy reason, selection and stateToken. Coordinates are zero based.","",readOnly:true),
        Tool("dk2_map_fingerprint","Read ordered SHA256/length/signature/export name for loaded map files, plus a document hash covering all transaction buffers and editor metadata. No raw bytes, clipboard or undo buffers are exposed.","",readOnly:true),
        Tool("dk2_map_open","Open an existing map in the live editor. Unsaved edits require explicit discardChanges=true.",Token+","+S("path")+","+B("discardChanges"),"expectedStateToken,path"),
        Tool("dk2_tiles_read","Read a rectangle of at most 4096 tiles; zero-based x/y. Returns raw terrain/owner/variant/reserved values.",I("x")+","+I("y")+","+I("width")+","+I("height"),"x,y,width,height",true),
        Tool("dk2_terrain_paint","One undoable editor terrain gesture. Honors the current selection, protected boundary and terrain footprints. Brush paints a single tile. Use Line/Rectangle/FilledRectangle/Ellipse/FilledEllipse for drags.",
            Token+","+I("startX")+","+I("startY")+","+I("endX")+","+I("endY")+","+I("terrain")+","+I("owner")+","+I("variant")+",\"shape\":{\"type\":\"string\",\"enum\":[\"Brush\",\"Line\",\"Rectangle\",\"FilledRectangle\",\"Ellipse\",\"FilledEllipse\"]}",
            "expectedStateToken,startX,startY,endX,endY,terrain,owner,variant,shape"),
        Tool("dk2_records_list","Page through records. Handles are valid only with this response's stateToken. Category is an exact optional filter.",S("category")+","+I("offset")+","+I("limit"),readOnly:true),
        Tool("dk2_record_read","Read verified fields of a record handle from dk2_records_list.",Token+","+S("recordHandle"),"expectedStateToken,recordHandle",true),
        Tool("dk2_record_set","Change one verified field using the same SetField command as the property grid. value is a string, including numeric values. One Undo restores the edit.",Token+","+S("recordHandle")+","+S("field")+","+S("value"),"expectedStateToken,recordHandle,field,value"),
        Tool("dk2_history_undo","Undo one operation on the live document.",Token,"expectedStateToken"),
        Tool("dk2_history_redo","Redo one operation on the live document.",Token,"expectedStateToken"),
        Tool("dk2_map_validate","Run the existing read-only validator; no script deletion or automatic repair.",B("includeGameplay"),readOnly:true),
        Tool("dk2_map_save","Normal Save As through the editor's existing DKM/MAP route. May retain native-source ZIP. Warnings/losses require explicit flags. Writes only inside outputRoot; never installs into the game.",
            Token+","+S("path")+","+B("acceptLosses")+","+B("allowWarnings")+","+B("overwrite"),"expectedStateToken,path"),
        Tool("dk2_map_export","Export a copy in its CURRENT format into a NEW directory inside outputRoot. Does not change the open document's normal Save target.",Token+","+S("outputDirectory"),"expectedStateToken,outputDirectory"),
        Tool("dk2_map_compile","Use the live editor Compile route, keeping unused events. basisPath is an existing game KWD. Writes a new subdirectory under outputParent inside outputRoot. Does not launch or install the level.",
            Token+","+S("basisPath")+","+S("outputParent")+","+B("allowWarnings"),"expectedStateToken,basisPath,outputParent"),
        Tool("dk2_properties_prepare","Prepare an independent map-properties draft. Optional name/players/version and optional resize width+height/anchor(0..8)/removeOutside. Returns a planToken and crop/large-map preview; live map is unchanged.",
            Token+","+S("name")+","+I("players")+","+I("version")+","+I("width")+","+I("height")+","+I("anchor")+","+B("removeOutside"),"expectedStateToken"),
        Tool("dk2_properties_apply","Apply the prepared editor-properties plan as one undoable operation. Explicit consent is required for large maps/cropping.",Token+","+S("planToken")+","+B("largeMapAccepted")+","+B("croppingAccepted"),"expectedStateToken,planToken"),
        Tool("dk2_properties_cancel","Discard the prepared draft without changing the live map.",S("planToken"),"planToken"),
        Tool("dk2_map_new","Create an in-memory map with the same factory/adoption route as File > New. Does not save it. Large maps and discarding dirty data require explicit flags.",
            Token+","+S("fileName")+","+S("name")+","+I("width")+","+I("height")+","+I("players")+","+I("version")+","+S("fillPreset")+","+B("placeHearts")+","+B("nativeFormat")+","+B("acceptLargeMap")+","+B("discardChanges"),
            "expectedStateToken,fileName,name,width,height,players,version"),
        Tool("dk2_selection_command","Use the actual editor selection/clipboard commands. Set requires x,y,width,height. Paste requires a selection as its explicit target. Replacing a nonempty buffer requires replaceBuffer=true; the default returns the existing consent request without changing anything.",
            Token+",\"command\":{\"type\":\"string\",\"enum\":[\"Set\",\"SelectAll\",\"Clear\",\"Copy\",\"Cut\",\"CopyRotated\",\"Paste\",\"RotateClockwise\",\"RotateCounterclockwise\",\"MirrorHorizontal\",\"MirrorVertical\"]},"+I("x")+","+I("y")+","+I("width")+","+I("height")+","+B("replaceBuffer"),"expectedStateToken,command"),
        Tool("dk2_drawing_tool","Select the actual editor drawing tool; does not paint. Subsequent mouse gestures use this tool.",Token+",\"tool\":{\"type\":\"string\",\"enum\":[\"Brush\",\"Line\",\"Rectangle\",\"FilledRectangle\",\"Ellipse\",\"FilledEllipse\",\"Fill\",\"Select\",\"Sample\"]}","expectedStateToken,tool"),
        Tool("dk2_object_catalogue","Read verified object/creature/door/trap/special IDs for the current game version, with EN/RU names.","\"kind\":{\"type\":\"string\",\"enum\":[\"Creature\",\"Door\",\"Trap\",\"Object\",\"SpecialItem\"]}","kind",true),
        Tool("dk2_object_paint","Preview (default) or apply the existing single-object painting command. Creatures stack; other families may update/replace entries. apply=true is required; warnings additionally require allowWarnings=true. Script-bound replacements require manual review. typeID comes from object_catalogue; level maps to the source DataAll field for creatures.",
            Token+",\"kind\":{\"type\":\"string\",\"enum\":[\"Creature\",\"Door\",\"Trap\",\"Object\",\"SpecialItem\"]},"+I("typeID")+","+I("x")+","+I("y")+","+I("owner")+","+I("health")+","+I("gold")+","+I("level")+","+B("apply")+","+B("allowWarnings"),"expectedStateToken,kind,typeID,x,y"),
        Tool("dk2_record_duplicate","Duplicate a verified current record using the existing DuplicateRecord command, with its identity/link rules. Unsupported categories are refused.",Token+","+S("recordHandle"),"expectedStateToken,recordHandle"),
        Tool("dk2_camera_library","Inspect or operate the already-open direct camera-path library. Does not open windows. Search requires query; Select requires pathID from the filtered choices. Apply uses the existing atomic editor command; Cancel closes without applying. Other map commands remain blocked while the dialog is open.",
            Token+",\"command\":{\"type\":\"string\",\"enum\":[\"Inspect\",\"Search\",\"Select\",\"Apply\",\"Cancel\"]},"+S("query")+","+I("pathID"),"expectedStateToken,command"),
        Tool("dk2_history_read","Read history descriptions and counts; never exposes undo byte buffers.","",readOnly:true)
    ];
    public static AutomationTool Find(string name)=>All.SingleOrDefault(t=>t.Name==name)??throw new AutomationException("unknown_tool","Unknown editor tool.");
    public static void Validate(AutomationTool tool,JsonElement args)
    {
        if(args.ValueKind!=JsonValueKind.Object)throw new AutomationException("arguments","Arguments must be an object.");
        var schema=tool.InputSchema;var properties=schema.GetProperty("properties");
        var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var row in args.EnumerateObject())
        {
            if(!seen.Add(row.Name)||!properties.TryGetProperty(row.Name,out var field))throw new AutomationException("arguments","Unknown or duplicate argument: "+row.Name);
            var valid=field.GetProperty("type").GetString() switch {
                "string"=>row.Value.ValueKind==JsonValueKind.String&&row.Value.GetString()!.Length<=32768,
                "integer"=>row.Value.ValueKind==JsonValueKind.Number&&row.Value.TryGetInt32(out _),
                "boolean"=>row.Value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                _=>false };
            if(!valid)throw new AutomationException("arguments","Invalid argument type: "+row.Name);
            if(field.TryGetProperty("enum",out var choices)&&!choices.EnumerateArray().Any(c=>c.GetString()==row.Value.GetString()))
                throw new AutomationException("arguments","Invalid choice: "+row.Name);
        }
        foreach(var required in schema.GetProperty("required").EnumerateArray())
            if(!seen.Contains(required.GetString()!))throw new AutomationException("arguments","Missing argument: "+required.GetString());
    }
}
