using System.IO;
using System.Windows.Controls;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared controls and real Open/Save callers; compile only this stage.
    private static void CheckDiskProgress(bool native,string language,string directory)
    {
        var source=NativeMapDocument.CreateNewMap(new("DiskUI","Disk UI",18,16,PlaceHearts:true,NativeFormat:native));
        var input=source.SaveCopy(Path.Combine(directory,$"disk-input-{language}-{native}"));
        var shell=new MainWindow(new AppSettings(),persistSettings:false){DiscardChangesPrompt=()=>true,SaveNoticePresenter=_=>{},ScenarioNoticePresenter=_=>{}};
        var windows=new List<DiskProgressWindow>();shell.FileProgressPresenter=windows.Add;
        try
        {
            shell.OpenMapFromPath(input);var workspace=(EditorWorkspace)shell.FindName("Editor");var document=workspace.Document!;
            Expect(shell.LastFileProgress[0].Phase==EditorFilePhase.Inspect&&shell.LastFileProgress[^1].Phase==EditorFilePhase.Done
                &&shell.LastFileProgress.Any(p=>p.Phase==EditorFilePhase.Map),"Actual Open omitted load progress/completion.");
            var before=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var bad=Path.Combine(directory,$"disk-bad-{language}-{native}.dkm");
            File.WriteAllBytes(bad,BitConverter.GetBytes(DK2MapEditor.Core.Formats.LegacySectionFile.MapFileSignature));shell.OpenMapFromPath(bad);
            Expect(ReferenceEquals(workspace.Document,document)&&before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))
                &&shell.LastFileProgress[^1].Phase!=EditorFilePhase.Done,"Failed Open adopted a map or reported completion.");
            if(native)
            {
                var project=NativeMapDocument.CreateNewMap(new("DiskProject","Disk project",18,16,PlaceHearts:true));
                shell.OpenMapFromPath(project.SaveCopy(Path.Combine(directory,$"disk-project-{language}")));
            }
            var target=Path.Combine(directory,$"disk-save-{language}-{native}.dkm");shell.SaveLocationPrompt=_=>target;shell.SaveConfirmationPrompt=_=>true;
            Expect(shell.RequestSave(saveAs:true)&&File.Exists(target)&&shell.LastFileProgress.Any(p=>p.Phase==EditorFilePhase.Write)
                &&shell.LastFileProgress.Any(p=>p.Phase==EditorFilePhase.Flush)&&shell.LastFileProgress[^1].Phase==EditorFilePhase.Done,"Actual Save omitted writer/flush/completion or failed.");
            var count=windows.Count;((EditorWorkspace)shell.FindName("Editor")).Document!.Paint([new(3,3)],new(4,2,1));
            shell.RequestAutoSaveTick(locked:true);Expect(windows.Count==count,"Locked autosave opened a disk window.");
        }
        finally{((EditorWorkspace)shell.FindName("Editor")).Document=null;shell.Close();}
        var window=new DiskProgressWindow(false,"Visible target");window.Report(new(EditorFilePhase.Read,Completed:1,Total:3));window.Close();
        window.Report(new(EditorFilePhase.Map,Completed:2,Total:3));
        Expect(((TextBlock)window.FindName("DiskPhase")).Text==LocalizationService.Get("FileProgress.Map"),"Disk indicator closed while an operation was active.");
        LocalizationService.SetLanguage(language=="en"?"ru":"en");
        Expect(window.Title==LocalizationService.Get("FileProgress.Loading")&&((TextBlock)window.FindName("DiskPhase")).Text==LocalizationService.Get("FileProgress.Map")
            &&((ProgressBar)window.FindName("DiskProgress")).Value==2,"Disk live translation lost phase/count.");
        window.CloseForOperation();LocalizationService.SetLanguage(language);
    }
}
