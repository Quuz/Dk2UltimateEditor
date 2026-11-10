using System.Text.Json;
using DK2MapEditor.Core.Automation;

namespace DK2MapEditor.Mcp;

/// <summary>MCP 2025-11-25 stdio subset: lifecycle, ping, tools/list and tools/call.
/// stdout is exclusively NDJSON. There are no external package/SDK dependencies.</summary>
public sealed class McpStdioServer(TextReader input,TextWriter output,TextWriter log,
    Func<AutomationRequest,CancellationToken,Task<JsonElement>> execute)
{
    private bool initialized,ready;
    private const string Version="2025-11-25";
    public async Task RunAsync(CancellationToken cancellation=default)
    {
        while(!cancellation.IsCancellationRequested)
        {
            string? line;
            try{line=await BridgeLines.ReadAsync(input,BridgeLines.RequestLimit,cancellation);}
            catch(InvalidDataException e){await Error(null,-32700,e.Message);return;}
            if(line is null)return;
            JsonDocument json;
            try{json=JsonDocument.Parse(line);}
            catch(JsonException){await Error(null,-32700,"Invalid JSON.");continue;}
            using(json)
            {
                var root=json.RootElement;
                if(root.ValueKind!=JsonValueKind.Object){await Error(null,-32600,"Expected one JSON-RPC object.");continue;}
                var hasId=root.TryGetProperty("id",out var id);
                if(!root.TryGetProperty("jsonrpc",out var rpc)||rpc.ValueKind!=JsonValueKind.String||rpc.GetString()!="2.0"
                    ||!root.TryGetProperty("method",out var method)||method.ValueKind!=JsonValueKind.String
                    ||hasId&&id.ValueKind is not (JsonValueKind.String or JsonValueKind.Number))
                {await Error(null,-32600,"Invalid JSON-RPC request.");continue;}
                var name=method.GetString();
                if(!hasId)
                {
                    if(name=="notifications/initialized"&&initialized)ready=true;
                    // No progress messages are produced; cancellation notifications are accepted.
                    // Calls are serialized. A command already committed cannot be cancelled/retried automatically.
                    continue;
                }
                try
                {
                    var parameters=root.TryGetProperty("params",out var p)?p:default;
                    switch(name)
                    {
                        case "initialize":
                            if(initialized){await Error(id,-32600,"Already initialized.");break;}
                            if(parameters.ValueKind!=JsonValueKind.Object
                                ||!parameters.TryGetProperty("protocolVersion",out var offered)||offered.ValueKind!=JsonValueKind.String
                                ||!parameters.TryGetProperty("capabilities",out var cap)||cap.ValueKind!=JsonValueKind.Object
                                ||!parameters.TryGetProperty("clientInfo",out var info)||info.ValueKind!=JsonValueKind.Object)
                            {await Error(id,-32602,"initialize requires protocolVersion, capabilities and clientInfo.");break;}
                            initialized=true;
                            var supported=new[]{"2024-11-05","2025-03-26","2025-06-18",Version};
                            await Result(id,new {protocolVersion=supported.Contains(offered.GetString())?offered.GetString():Version,
                                capabilities=new {tools=new {listChanged=false}},
                                serverInfo=new {name="dk2-map-editor",version="1.0.0"},
                                instructions="Local DK2 editor bridge. Start with dk2_editor_status. All coordinates are zero based. Mutations require its current stateToken; inspect each result. Saving currently uses DKM/MAP plus source ZIP when needed; compile produces the official seven game files. Output is restricted to the editor's explicit MCP workspace. No GUI or game is launched by this server."});
                            break;
                        case "ping":await Result(id,new {});break;
                        case "tools/list":
                            if(!ready){await Error(id,-32000,"Initialize and send notifications/initialized first.");break;}
                            await Result(id,new {tools=AutomationTools.All.Select(t=>new {name=t.Name,description=t.Description,inputSchema=t.InputSchema,
                                annotations=new {readOnlyHint=t.ReadOnly,destructiveHint=!t.ReadOnly,idempotentHint=t.ReadOnly,openWorldHint=false}})});
                            break;
                        case "tools/call":
                            if(!ready){await Error(id,-32000,"Initialize and send notifications/initialized first.");break;}
                            if(parameters.ValueKind!=JsonValueKind.Object||!parameters.TryGetProperty("name",out var toolName)||toolName.ValueKind!=JsonValueKind.String)
                            {await Error(id,-32602,"tools/call requires name and arguments.");break;}
                            var tool=AutomationTools.Find(toolName.GetString()!);
                            var arguments=parameters.TryGetProperty("arguments",out var a)?a:JsonSerializer.SerializeToElement(new {});
                            AutomationTools.Validate(tool,arguments);
                            JsonElement result;
                            try{result=await execute(new(tool.Name,arguments),cancellation);}
                            catch(Exception e) when(e is IOException or OperationCanceledException or AutomationException or UnauthorizedAccessException)
                            {
                                result=JsonSerializer.SerializeToElement(new AutomationReply(false,null,null,
                                    e is AutomationException ae?ae.Code:"bridge_failed",
                                    e.Message+" If a request was sent, its outcome may be unknown; inspect editor state before retrying."),AutomationJson.Options);
                            }
                            await Result(id,new {content=new[]{new {type="text",text=result.GetRawText()}},
                                structuredContent=result,isError=!result.TryGetProperty("ok",out var ok)||ok.ValueKind!=JsonValueKind.True});
                            break;
                        default:await Error(id,-32601,"Method not found.");break;
                    }
                }
                catch(AutomationException e){await Error(id,-32602,e.Message);}
                catch(Exception e) when(e is InvalidOperationException or JsonException or ArgumentException)
                {await Error(id,-32602,"Invalid request parameters: "+e.Message);}
            }
        }
    }
    private async Task Result(JsonElement id,object value)
    {
        await output.WriteLineAsync(JsonSerializer.Serialize(new {jsonrpc="2.0",id,result=value},AutomationJson.Options));
        await output.FlushAsync();
    }
    private async Task Error(JsonElement? id,int code,string message)
    {
        await output.WriteLineAsync(JsonSerializer.Serialize(new {jsonrpc="2.0",id,error=new {code,message}},AutomationJson.Options));
        await output.FlushAsync();
        await log.WriteLineAsync("MCP: "+message);
    }
}
