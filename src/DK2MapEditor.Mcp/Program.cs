using DK2MapEditor.Core.Automation;
using DK2MapEditor.Mcp;
using System.Text;

Console.InputEncoding=new UTF8Encoding(false);
Console.OutputEncoding=new UTF8Encoding(false);

if(args.Contains("--help"))
{
    Console.Error.WriteLine("DK2MapEditor.Mcp --pipe=dk2-editor\nMCP stdio bridge; launch the editor separately with --mcp-pipe=dk2-editor --mcp-root=<absolute workspace>.");
    return 0;
}
try
{
    var pipe="dk2-editor";
    foreach(var argument in args)
        if(argument.StartsWith("--pipe=",StringComparison.Ordinal))pipe=argument[7..];
        else throw new ArgumentException("Unknown option: "+argument);
    BridgeLines.ValidatePipeName(pipe);
    var client=new EditorBridgeClient(pipe);
    var server=new McpStdioServer(Console.In,Console.Out,Console.Error,(request,ct)=>client.ExecuteAsync(request,ct));
    await server.RunAsync();
    return 0;
}
catch(Exception e) when(e is IOException or ArgumentException or UnauthorizedAccessException)
{Console.Error.WriteLine(e.Message);return 1;}
