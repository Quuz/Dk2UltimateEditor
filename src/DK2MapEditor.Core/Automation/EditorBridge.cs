using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace DK2MapEditor.Core.Automation;

public static class BridgeLines
{
    public const int RequestLimit=1024*1024,ResponseLimit=8*1024*1024;
    // StreamReader.ReadLineAsync has no length bound. Read incrementally to bound untrusted input.
    public static async Task<string?> ReadAsync(TextReader reader,int limit,CancellationToken cancellation=default)
    {
        var line=new StringBuilder();var one=new char[1];
        while(true)
        {
            var read=await reader.ReadAsync(one.AsMemory(),cancellation);
            if(read==0)return line.Length==0?null:throw new InvalidDataException("Incomplete JSON line.");
            if(one[0]=='\n')return line.ToString();
            if(one[0]!='\r')line.Append(one[0]);
            if(line.Length>limit)throw new InvalidDataException("JSON line exceeds the transport limit.");
        }
    }
    public static void ValidatePipeName(string name)
    {
        if(name.Length is <1 or >64||name.Any(c=>!(char.IsAsciiLetterOrDigit(c)||c is '-' or '_')))
            throw new ArgumentException("Use a pipe name of 1..64 ASCII letters, digits, '-' or '_'.");
    }
}

/// <summary>Same-user Windows named pipe, one request at a time. No socket, shell or game launch.</summary>
public sealed class EditorBridgeServer : IDisposable
{
    private readonly NamedPipeServerStream pipe;
    private readonly CancellationTokenSource stop=new();
    private readonly Task lifetime;
    public Task Completion=>lifetime;
    public EditorBridgeServer(string name,Func<AutomationRequest,CancellationToken,Task<AutomationReply>> execute,Action<string>? log=null)
    {
        BridgeLines.ValidatePipeName(name);
        // Bind synchronously: a second editor with the same pipe fails at startup, never silently shares it.
        pipe=new(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
        lifetime=Run(execute,log);
    }
    private async Task Run(Func<AutomationRequest,CancellationToken,Task<AutomationReply>> execute,Action<string>? log)
    {
        try
        {
            while(!stop.IsCancellationRequested)
            {
                await pipe.WaitForConnectionAsync(stop.Token).ConfigureAwait(false);
                try
                {
                    using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
                    timeout.CancelAfter(TimeSpan.FromMinutes(2));
                    using var reader=new StreamReader(pipe,new UTF8Encoding(false,true),false,4096,leaveOpen:true);
                    using var writer=new StreamWriter(pipe,new UTF8Encoding(false),4096,leaveOpen:true){AutoFlush=true,NewLine="\n"};
                    var line=await BridgeLines.ReadAsync(reader,BridgeLines.RequestLimit,timeout.Token).ConfigureAwait(false);
                    if(line is not null)
                    {
                        var request=JsonSerializer.Deserialize<AutomationRequest>(line,AutomationJson.Options)
                            ??throw new InvalidDataException("Missing request.");
                        var reply=await execute(request,timeout.Token).ConfigureAwait(false);
                        var output=JsonSerializer.Serialize(reply,AutomationJson.Options);
                        if(output.Length>BridgeLines.ResponseLimit)
                            output=JsonSerializer.Serialize(new AutomationReply(false,null,reply.State,"response_limit",
                                "The command completed but its report exceeded the response limit. Inspect state before retrying."),AutomationJson.Options);
                        await writer.WriteLineAsync(output.AsMemory(),timeout.Token).ConfigureAwait(false);
                    }
                }
                catch(Exception e) when(e is IOException or JsonException or OperationCanceledException or ArgumentException or UnauthorizedAccessException)
                {log?.Invoke(e.Message);}
                finally{if(pipe.IsConnected)pipe.Disconnect();}
            }
        }
        catch(Exception e) when(e is IOException or OperationCanceledException or ObjectDisposedException)
        {if(!stop.IsCancellationRequested)log?.Invoke(e.Message);}
    }
    public void Dispose(){stop.Cancel();pipe.Dispose();}
}
public sealed class EditorBridgeClient(string pipeName)
{
    public async Task<JsonElement> ExecuteAsync(AutomationRequest request,CancellationToken cancellation=default)
    {
        BridgeLines.ValidatePipeName(pipeName);
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        using var pipe=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
        try{await pipe.ConnectAsync(3000,timeout.Token);}
        catch(TimeoutException e){throw new AutomationException("editor_unavailable","Start the editor with the matching --mcp-pipe option. "+e.Message);}
        using var reader=new StreamReader(pipe,new UTF8Encoding(false,true),false,4096,leaveOpen:true);
        using var writer=new StreamWriter(pipe,new UTF8Encoding(false),4096,leaveOpen:true){AutoFlush=true,NewLine="\n"};
        var text=JsonSerializer.Serialize(request,AutomationJson.Options);
        if(text.Length>BridgeLines.RequestLimit)throw new AutomationException("request_limit","Request is too large.");
        await writer.WriteLineAsync(text.AsMemory(),timeout.Token);
        var line=await BridgeLines.ReadAsync(reader,BridgeLines.ResponseLimit,timeout.Token)
            ??throw new IOException("Editor disconnected. Command outcome may be unknown; inspect state before retrying.");
        using var parsed=JsonDocument.Parse(line);
        return parsed.RootElement.Clone();
    }
}
