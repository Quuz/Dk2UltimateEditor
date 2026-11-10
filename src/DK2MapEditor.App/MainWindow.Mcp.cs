using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using DK2MapEditor.Core.Formats;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Automation;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App;

public partial class MainWindow
{
    private EditorBridgeServer? mcpBridge;
    private EditorAutomation? mcpAutomation;
    /// <summary>Opt-in only. The editor process must be launched by the user.</summary>
    public void StartMcp(string pipeName,string outputRoot)
    {
        if(mcpBridge is not null)throw new InvalidOperationException("MCP is already enabled.");
        var automation=new EditorAutomation(new McpHost(this),new AutomationPaths(outputRoot));
        try
        {
            mcpBridge=new(pipeName,(request,cancellation)=>Dispatcher.InvokeAsync(
                ()=>{cancellation.ThrowIfCancellationRequested();return automation.Execute(request);},
                DispatcherPriority.Background,cancellation).Task,
                message=>System.Diagnostics.Trace.WriteLine("MCP: "+message));
            mcpAutomation=automation;
            McpStatus.Visibility=Visibility.Visible;
            McpStatus.SetResourceReference(TextBlock.TextProperty,"Mcp.Active");
            McpStatus.ToolTip=pipeName+" · "+outputRoot;
            Closed+=(_,_)=>{mcpBridge.Dispose();mcpAutomation.Dispose();};
        }
        catch{automation.Dispose();throw;}
    }
    private sealed class McpHost(MainWindow owner):IEditorAutomationHost
    {
        public NativeMapDocument? Document=>owner.document;
        public long ContextRevision=>owner.saveContextRevision;
        public TileRect? Selection=>owner.Editor.Canvas.Selection;
        public string? BusyReason=>owner.saveOwnerClosed?"The editor is closing."
            :owner.saveCommandActive||owner.scenarioCommandActive||owner.fileProgress is not null||owner.startingFileProgress
                ?"An editor file/compiler command is active."
            :!owner.IsEnabled||owner.document?.HasActivePenStroke==true?"Finish the current modal operation or pen stroke."
            :Application.Current.Windows.OfType<Window>().Any(w=>w!=owner&&w.IsVisible)?"Close other editor dialogs before using MCP."
            :null;
        private object? lastBuffer;
        private string? bufferHash;
        private CameraPathWindow? ActiveCameraLibrary => owner.Editor.AutomationCameraLibrary is {IsVisible:true} window
            &&ReferenceEquals(window.Owner,owner)?window:null;
        public string? CameraLibraryBusyReason => owner.saveOwnerClosed||owner.saveCommandActive||owner.scenarioCommandActive
            ||owner.fileProgress is not null||owner.startingFileProgress||owner.document?.HasActivePenStroke==true
                ?"Finish the current file/compiler command or pen stroke."
            :ActiveCameraLibrary is not {} camera||!camera.IsEnabled?"Open the direct camera-path library first."
            :Application.Current.Windows.OfType<Window>().Any(w=>w!=owner&&w!=camera&&w.IsVisible)?"Close other editor dialogs first."
            :null;
        public object CameraLibrary(string command,string? query,int? pathID)
            =>(ActiveCameraLibrary??throw new InvalidOperationException("The camera library closed.")).AutomationCommand(command,query,pathID);
        public string InteractionToken=>owner.Editor.AutomationInteractionToken+"|"+ActiveCameraLibrary?.AutomationToken;
        public object UiState
        {
            get
            {
                var buffer=owner.Editor.BufferedSelection;
                if(!ReferenceEquals(lastBuffer,buffer))
                {
                    lastBuffer=buffer;bufferHash=buffer is null?null:Convert.ToHexString(SHA256.HashData(SelectionFile.Serialize(buffer))).ToLowerInvariant();
                }
                return new {cameraLibrary=ActiveCameraLibrary?.ReadAutomationState(),normalSavePath=owner.NormalSavePath,language=LocalizationService.CurrentLanguage,tool=owner.Editor.Canvas.Tool.ToString(),
                    paintTile=owner.Editor.Canvas.PaintTile,clipboard=buffer is null?null:new {width=buffer.Tiles.Width,height=buffer.Tiles.Height,objects=buffer.ObjectCount,scripts=buffer.ScriptCount,sha256=bufferHash},
                    window=new {width=owner.ActualWidth,height=owner.ActualHeight,dpiScale=System.Windows.Media.VisualTreeHelper.GetDpi(owner).DpiScaleX},
                    viewport=new {width=owner.Editor.MapScroll.ViewportWidth,height=owner.Editor.MapScroll.ViewportHeight}};
            }
        }
        public object NewMap(NewMapOptions options,bool discardChanges)
        {
            if(owner.document?.HasChanges==true&&!discardChanges)throw new AutomationException("unsaved_changes","The current document is dirty.");
            owner.AdoptNewMap(NativeMapDocument.CreateNewMap(options,owner.document));
            return new {created=true,options.FileName,options.DisplayName,options.Width,options.Height,options.Players,options.Version,options.PlaceHearts,options.NativeFormat};
        }
        public object DrawingTool(string tool)=>new {selected=owner.Editor.RequestDrawingTool(Enum.Parse<DK2MapEditor.App.Editing.DrawingTool>(tool)),tool};
        public object SelectionCommand(string command,TileRect? bounds,bool replaceBuffer)
        {
            var editor=owner.Editor;var doc=owner.document!;
            var buffer=editor.BufferedSelection;
            if(command is "Copy" or "Cut" or "CopyRotated" && buffer is not null&&!replaceBuffer)
                return new {applied=false,needsBufferReplacement=true,command,oldWidth=buffer.Tiles.Width,oldHeight=buffer.Tiles.Height,buffer.ObjectCount,buffer.ScriptCount};
            if(command is not ("Set" or "SelectAll" or "Clear")&&editor.Canvas.Selection is null)
                throw new AutomationException("selection_required","Set an explicit selection first; Paste uses its top-left corner.");
            var previousPrompt=editor.CopyBufferPrompt;var previousError=editor.OperationErrorPresenter;Exception? failure=null;
            var revision=doc.EditRevision;var before=editor.Canvas.Selection;var previousBuffer=buffer;var performed=false;
            try
            {
                editor.CopyBufferPrompt=_=>new CopyBufferDecision(replaceBuffer);
                editor.OperationErrorPresenter=error=>failure=error;
                switch(command)
                {
                    case "Set":editor.SetSelection(bounds!.Value);break;
                    case "SelectAll":editor.SelectAll();break;
                    case "Clear":editor.ClearSelection();break;
                    case "Copy":performed=editor.RequestCopy();break;
                    case "Cut":performed=editor.RequestCopy(cut:true);break;
                    case "CopyRotated":performed=editor.RequestCopy(rotated:true);break;
                    case "Paste":
                        if(buffer is null)throw new AutomationException("clipboard_empty","Copy a selection first.");
                        editor.Paste();break;
                    case "RotateClockwise":editor.Rotate();break;
                    case "RotateCounterclockwise":editor.RotateCounterclockwise();break;
                    case "MirrorHorizontal":editor.MirrorHorizontal();break;
                    case "MirrorVertical":editor.MirrorVertical();break;
                    default:throw new AutomationException("arguments","Unknown selection command.");
                }
                if(failure is not null)throw new AutomationException("selection_failed",failure.Message);
                return new {applied=performed||doc.EditRevision!=revision||before!=editor.Canvas.Selection||!ReferenceEquals(previousBuffer,editor.BufferedSelection),
                    command,transformStatus=editor.TransformStatus.Text};
            }
            finally{editor.CopyBufferPrompt=previousPrompt;editor.OperationErrorPresenter=previousError;}
        }
        public bool CommitFields()=>owner.Editor.CommitFields();
        public object Open(string path,bool discardChanges)
        {
            var previous=owner.document;
            return WithNotices(notices=>
            {
                var saved=owner.DiscardChangesPrompt;
                try
                {
                    owner.DiscardChangesPrompt=()=>discardChanges;
                    owner.OpenMap(path);
                    if(owner.document is null||ReferenceEquals(previous,owner.document))
                        throw new AutomationException("open_failed","The editor did not open this map.",new {notices});
                    return new {path=owner.document.MainFile.SourcePath,notices,diagnostics=Rows(owner.LastDocumentReport)};
                }
                finally{owner.DiscardChangesPrompt=saved;}
            });
        }
        public object Save(string path,bool acceptLosses,bool allowWarnings)
        {
            return WithNotices(notices=>
            {
                var location=owner.SaveLocationPrompt;var confirmation=owner.SaveConfirmationPrompt;
                object? preflight=null;
                try
                {
                    owner.SaveLocationPrompt=_=>path;
                    owner.SaveConfirmationPrompt=plan=>
                    {
                        preflight=new {plan.HasBlockingIssues,plan.NeedsLossConsent,plan.RetainsNativeSource,
                            conversionIssues=plan.ConversionIssues.Take(1000).ToArray(),textLosses=plan.TextLosses.Take(1000).ToArray(),
                            compatibility=Rows(owner.LastSaveCompatibilityReport)};
                        return !plan.HasBlockingIssues&&(!plan.NeedsLossConsent||acceptLosses)
                            &&(owner.LastSaveCompatibilityReport.Count==0||allowWarnings);
                    };
                    var success=owner.RequestSave(saveAs:true);
                    if(!success)throw new AutomationException("save_not_completed",
                        owner.LastSaveError?.Message??"Save requires explicit consent or could not complete. Inspect preflight before retrying.",
                        new {preflight,notices});
                    return new {path=owner.NormalSavePath,sourceArchives=owner.CurrentSaveSession?.LastSave?.SourceArchives,preflight,notices};
                }
                finally{owner.SaveLocationPrompt=location;owner.SaveConfirmationPrompt=confirmation;}
            });
        }
        public object Compile(string basisPath,string outputParent,bool allowWarnings)
        {
            return WithNotices(notices=>
            {
                var unused=owner.UnusedScriptPrompt;var location=owner.CompileLocationPrompt;var warnings=owner.CompileWarningsPrompt;
                try
                {
                    owner.UnusedScriptPrompt=_=>UnusedScriptDecision.Keep;
                    owner.CompileLocationPrompt=()=>new(basisPath,outputParent);
                    owner.CompileWarningsPrompt=_=>allowWarnings;
                    var output=owner.RequestCompileProject();
                    var diagnostics=Rows((owner.LastScenarioCheck?.Diagnostics??[]).Concat(owner.editableDiagnostics??[]));
                    if(output is null)throw new AutomationException("compile_not_completed","Compilation did not complete; inspect diagnostics and notices.",
                        new {diagnostics,notices});
                    return new {path=output,unusedEvents="kept",diagnostics,notices};
                }
                finally{owner.UnusedScriptPrompt=unused;owner.CompileLocationPrompt=location;owner.CompileWarningsPrompt=warnings;}
            });
        }
        private object WithNotices(Func<List<string>,object> action)
        {
            var save=owner.SaveNoticePresenter;var scenario=owner.ScenarioNoticePresenter;var progress=owner.FileProgressPresenter;
            var report=owner.DocumentReportPresenter;
            var notices=new List<string>();
            try
            {
                owner.SaveNoticePresenter=notice=>notices.Add(notice.Message);
                owner.ScenarioNoticePresenter=notice=>notices.Add(notice.Message);
                owner.FileProgressPresenter=_=>{};
                owner.DocumentReportPresenter=null;
                return action(notices);
            }
            finally
            {
                owner.SaveNoticePresenter=save;owner.ScenarioNoticePresenter=scenario;
                owner.FileProgressPresenter=progress;owner.DocumentReportPresenter=report;
                owner.Editor.RefreshActivityStatistics();
            }
        }
        private static object[] Rows(IEnumerable<MapDiagnostic> rows)=>rows.Take(1000).Select(r=>(object)new {
            severity=r.Severity.ToString(),r.Code,r.Message,r.FilePath,r.Region}).ToArray();
    }
}
