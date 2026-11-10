using System.Text.Json;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.Core.Automation;

/// <summary>The live WPF owner implements this on its Dispatcher. Tests use a headless owner.</summary>
public interface IEditorAutomationHost
{
    NativeMapDocument? Document { get; }
    long ContextRevision { get; }
    TileRect? Selection { get; }
    string? BusyReason { get; }
    string InteractionToken => "";
    object? UiState => null;
    object NewMap(NewMapOptions options,bool discardChanges) => throw new InvalidOperationException("New-map host is unavailable.");
    object SelectionCommand(string command,TileRect? bounds,bool replaceBuffer) => throw new InvalidOperationException("Selection host is unavailable.");
    object DrawingTool(string tool) => throw new InvalidOperationException("Drawing-tool host is unavailable.");
    string? CameraLibraryBusyReason => "The direct camera library is not available.";
    object CameraLibrary(string command,string? query,int? pathID) => throw new InvalidOperationException("Camera-library host is unavailable.");
    bool CommitFields();
    object Open(string path, bool discardChanges);
    object Save(string path, bool acceptLosses, bool allowWarnings);
    object Compile(string basisPath, string outputParent, bool allowWarnings);
}

public sealed class AutomationException(string code, string message, object? details = null) : Exception(message)
{
    public string Code { get; } = code;
    public object? Details { get; } = details;
}
public sealed record AutomationRequest(string Tool, JsonElement Arguments);
public sealed record AutomationReply(bool Ok, object? Result, object? State, string? Code = null, string? Message = null, object? Details = null);
public static class AutomationJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);
}
