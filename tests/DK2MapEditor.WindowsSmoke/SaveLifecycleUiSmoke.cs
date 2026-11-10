using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static MainWindow SaveShell(AppSettings settings)
        =>new(settings,persistSettings:false)
        {
            WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Width=980,Height=600,
            ShowActivated=false,ShowInTaskbar=false,Left=-10000,Top=-10000,WindowStartupLocation=WindowStartupLocation.Manual,
            DiscardChangesPrompt=()=>true,SaveNoticePresenter=_=>{}
        };
    private static void SaveLayout(Window window)
    {
        window.Left=-10000;window.Top=-10000;window.WindowStartupLocation=WindowStartupLocation.Manual;
        window.ShowActivated=false;window.ShowInTaskbar=false;window.Show();
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
        Expect(window.IsLoaded&&window.Left==-10000&&window.Top==-10000,"Save UI must be a loaded owned offscreen window.");
    }
    private static void SaveRender(Window window,string name)
    {
        if(Environment.GetEnvironmentVariable("DK2_SAVE_UI_ARTIFACTS") is not {Length:>0} directory)return;
        Directory.CreateDirectory(directory);var content=(FrameworkElement)window.Content;
        content.UpdateLayout();
        foreach(var controlName in new[]{"SaveAccept","SaveCancel","AutoSaveApply","RecoveryAccept","SaveSummary","SaveArchiveNotice"})
        {
            if(window.FindName(controlName) is not FrameworkElement element||element.Visibility!=Visibility.Visible)continue;
            var bounds=element.TransformToAncestor(content).TransformBounds(new Rect(element.RenderSize));
            Expect(bounds.Left>=-0.1&&bounds.Top>=-0.1&&bounds.Right<=content.ActualWidth+0.1&&bounds.Bottom<=content.ActualHeight+0.1,
                "Actual save dialog control clipped inside its content: "+controlName);
        }
        // Loaded root carries its margin/visual offset. Its ActualWidth/Height
        // omit that outer space; allocate the full window before rendering it.
        var width=(int)Math.Ceiling(window.ActualWidth);var height=(int)Math.Ceiling(window.ActualHeight);
        Expect(width>300&&height>100,"Save dialog window has no bounds.");
        var background=new DrawingVisual();using(var draw=background.RenderOpen())draw.DrawRectangle((Brush)window.FindResource("Brush.Background"),null,new Rect(0,0,width,height));
        var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(background);bitmap.Render(content);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream=new FileStream(Path.Combine(directory,name),FileMode.CreateNew);encoder.Save(stream);
    }
    private static void CheckSaveLifecycle(string directory,string language,bool native)
    {
        var fixture=NativeMapDocument.CreateNewMap(new("SaveUI","SaveUI",18,18,NativeFormat:native));
        if(native)fixture.SetScenarioName(ScenarioNameKind.Variable,0,"Source alias");
        var entry=fixture.SaveCopy(Path.Combine(directory,$"save-ui-source-{language}-{native}"),temporary:true);
        var input=Directory.GetFiles(Path.GetDirectoryName(entry)!,"*",SearchOption.AllDirectories).ToDictionary(path=>path,File.ReadAllBytes);
        var settings=new AppSettings {Language=language,AutoSaveMinutes=1,RecentFiles=[null!,"",entry]};
        var shell=SaveShell(settings);EditorWorkspace workspace=(EditorWorkspace)shell.FindName("Editor");
        T Field<T>(string name)=>(T)shell.FindName(name);
        void Menu(string name)=>Field<MenuItem>(name).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        bool Consent(EditorSavePlan plan)
        {
            var dialog=shell.CreateSaveConfirmationWindow(plan);dialog.Owner=shell;SaveLayout(dialog);
            Expect(dialog.Title==LocalizationService.Get("Save.Confirm.Title"),"Save consent title is untranslated.");
            var summary=(TextBlock)dialog.FindName("SaveSummary");var archive=(TextBlock)dialog.FindName("SaveArchiveNotice");
            Expect(summary.Text.Contains(plan.Path)&&archive.Visibility==(plan.RetainsNativeSource?Visibility.Visible:Visibility.Collapsed),"Consent omits path/source companion.");
            var before=summary.Text;LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(summary.Text!=before&&dialog.Title==LocalizationService.Get("Save.Confirm.Title"),"Live save-consent translation is stale.");
            LocalizationService.SetLanguage(language);dialog.UpdateLayout();
            SaveRender(dialog,$"save-consent-{language}-{native}.png");
            ((Button)dialog.FindName("SaveAccept")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(dialog.Accepted&&!dialog.Accept(),"Closed accepted consent retained authority.");return dialog.Accepted;
        }
        try
        {
            shell.OpenMapFromPath(entry);SaveLayout(shell);var document=workspace.Document ?? throw new InvalidOperationException("Loaded Save fixture must have an editable document.");
            Expect(!document.HasChanges&&!Field<MenuItem>("SaveMenuItem").IsEnabled&&Field<MenuItem>("SaveAsMenuItem").IsEnabled,
                "Loaded clean map has incorrect Save commands.");
            Expect(shell.NormalSavePath==(native?null:entry),"Ordinary DKM/native target adoption differs.");
            document.Paint([new(3,3)],new(4,2,1));Expect(Field<MenuItem>("SaveMenuItem").IsEnabled&&shell.Title.EndsWith(" *"),"Dirty title/menu failed.");
            var path=Path.Combine(directory,$"save-ui-{language}-{native}.dkm");var revision=document.EditRevision;var count=document.EditorChangeCount;
            shell.SaveLocationPrompt=_=>null;Menu("SaveAsMenuItem");
            Expect(!File.Exists(path)&&ReferenceEquals(workspace.Document,document)&&document.EditRevision==revision&&document.EditorChangeCount==count&&document.CanUndo,"Save As cancellation changed map/history.");
            shell.SaveLocationPrompt=_=>path;shell.SaveConfirmationPrompt=Consent;
            Menu("SaveAsMenuItem");var adopted=workspace.Document!;
            Expect(File.Exists(path)&&shell.NormalSavePath==path&&!adopted.HasChanges&&!adopted.CanUndo&&!adopted.CanRedo&&adopted.EditorChangeCount==0
                &&!Field<MenuItem>("SaveMenuItem").IsEnabled&&!shell.Title.EndsWith(" *")&&shell.Title.Contains(document.CreateMapGeneralDraft().Values["Name"]),"Real Save As did not publish/adopt clean baseline or retain the internal map title.");
            Expect(ReferenceEquals(adopted,document)==!native&&settings.RecentFiles![0]==path&&settings.LastOpenedFile==path,"Save As replacement/recents failed.");
            Expect(shell.CurrentSaveSession!.LastSave!.SourceArchives.Count==(native?1:0),"Native source archive was lost.");
            adopted.Paint([new(5,4)],new(5,1,0));var savedBefore=adopted.ReadActivityStatistics()!.SavedTimes;
            var prompts=0;shell.SaveLocationPrompt=_=>{prompts++;return null;};Menu("SaveMenuItem");
            Expect(prompts==0&&NativeMapDocument.Load(path).GetTile(5,4)==adopted.GetTile(5,4)&&!adopted.HasChanges
                &&adopted.ReadActivityStatistics()!.SavedTimes==savedBefore+1,"Ordinary actual Save did not reuse target.");
            adopted.Paint([new(6,4)],new(4,2,1));Expect(shell.RequestSaveShortcut(Key.S,ModifierKeys.Control)&&!adopted.HasChanges,"Ctrl+S shared command failed.");
            var shortcutPath=Path.Combine(directory,$"save-shortcut-{language}-{native}.dkm");shell.SaveLocationPrompt=_=>shortcutPath;
            Expect(shell.RequestSaveShortcut(Key.S,ModifierKeys.Control|ModifierKeys.Shift)&&File.Exists(shortcutPath)&&shell.NormalSavePath==shortcutPath
                &&!shell.RequestSaveShortcut(Key.S,ModifierKeys.None),"Ctrl+Shift+S shared command failed.");
            Console.WriteLine($"PASS {language} {(native?"native":"project")} loaded Save menus, consent/language/cancel, clean target/title/history, repeat Save, shared shortcuts and recents");

            var setting=shell.CreateAutoSaveSettingsWindow();setting.Owner=shell;SaveLayout(setting);
            var minutes=(TextBox)setting.FindName("AutoSaveMinutes");var status=(TextBlock)setting.FindName("AutoSaveStatus");
            foreach(var invalid in new[]{"","-1","4294967296","1.5","+1"}){minutes.Text=invalid;Expect(!setting.Apply()&&settings.AutoSaveMinutes==1&&status.Text==LocalizationService.Get("Save.AutoSettings.Invalid"),"Invalid autosave setting applied.");}
            minutes.Text="0";Expect(setting.Apply()&&shell.CurrentSaveSession!.AutoSave.IntervalMinutes==0,"Autosave disable failed.");
            minutes.Text="1";Expect(setting.Apply()&&settings.AutoSaveMinutes==1,"Autosave setting was not applied.");SaveRender(setting,$"autosave-settings-{language}-{native}.png");
            var applyButton=(Button)setting.FindName("AutoSaveApply");
            string ApplyCaption(string lang)=>lang=="en"?"Apply":"Применить";
            Expect(applyButton.Content is string label&&label==ApplyCaption(language),"Autosave Apply caption missing/untranslated.");
            var inputBeforeLanguage=minutes.Text;var otherLanguage=language=="en"?"ru":"en";
            LocalizationService.SetLanguage(otherLanguage);setting.UpdateLayout();
            Expect(applyButton.Content is string otherLabel&&otherLabel==ApplyCaption(otherLanguage)
                &&setting.Title==LocalizationService.Get("Save.AutoSettings")&&minutes.Text==inputBeforeLanguage,
                "Autosave live language change lost caption/title/input.");
            LocalizationService.SetLanguage(language);setting.UpdateLayout();
            Expect(applyButton.Content is string restoredLabel&&restoredLabel==ApplyCaption(language),"Autosave caption did not switch back.");
            applyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(!setting.IsVisible&&!setting.Apply()&&settings.AutoSaveMinutes==1,"Actual autosave Apply button did not close/save or retained authority.");
            shell.ConfigureRecoveryDirectory(Path.Combine(directory,$"save-ui-recovery-{language}-{native}"),startTimer:false);
            var pen=adopted.BeginLivePenStroke(new(4,2,1));pen.Start(new(7,5));var penCount=adopted.EditorChangeCount;
            for(var i=0;i<60;i++)Expect(!shell.RequestAutoSaveTick(),"UI timer fired before original boundary.");
            Expect(shell.RequestAutoSaveTick()&&adopted.HasActivePenStroke&&!pen.IsClosed&&adopted.EditorChangeCount==penCount
                &&shell.NormalSavePath==shortcutPath&&NativeMapDocument.Load(shell.RecoveryPath!).GetTile(7,5)==adopted.GetTile(7,5),"UI autosave ended live pen/adopted temporary target.");
            pen.Move(new(8,5));pen.Dispose();Expect(adopted.GetHistory().Undo.Count==1,"UI autosave split gesture history.");
            var old=File.ReadAllBytes(shortcutPath);shell.SaveOutputFactory=_=>throw new IOException("Owned UI write failure.");
            Expect(!shell.RequestSave()&&shell.LastSaveError is IOException&&adopted.CanUndo&&adopted.HasChanges
                &&File.ReadAllBytes(shortcutPath).AsSpan().SequenceEqual(old)&&shell.NormalSavePath==shortcutPath,"Failed actual Save changed target/history.");
            shell.SaveOutputFactory=null;var stale=shell.CurrentSaveSession!.PrepareSaveAs(Path.Combine(directory,$"stale-ui-{language}-{native}.dkm"));
            var staleWindow=shell.CreateSaveConfirmationWindow(stale);staleWindow.Owner=shell;SaveLayout(staleWindow);
            adopted.Paint([new(9,5)],new(5,1,0));Expect(!staleWindow.Accept(),"Edited document retained stale consent.");staleWindow.Close();
            var beforeContext=shell.CurrentSaveSession;var beforeBuffers=adopted.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            var contextPath=Path.Combine(directory,$"context-ui-{language}-{native}.dkm");
            shell.SaveLocationPrompt=_=>{shell.OpenMapFromPath(entry);return contextPath;};
            Expect(!shell.RequestSave(saveAs:true)&&!File.Exists(contextPath)&&!ReferenceEquals(shell.CurrentSaveSession,beforeContext)
                &&beforeBuffers.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Document switch during location prompt published stale map.");
            var current=workspace.Document;var corrupt=Path.Combine(directory,$"bad-open-{language}-{native}.dkm");File.Copy(shortcutPath,corrupt);
            File.WriteAllBytes(corrupt+".native-source-"+new string('0',64)+".zip",[1,2,3]);shell.OpenMapFromPath(corrupt);
            Expect(ReferenceEquals(workspace.Document,current)&&shell.LastSaveError is not null,"Corrupt source companion replaced live document.");
            Expect(input.All(pair=>File.ReadAllBytes(pair.Key).AsSpan().SequenceEqual(pair.Value)),"Save UI changed source inputs.");
            Console.WriteLine($"PASS {language} {(native?"native":"project")} loaded autosave settings/live-pen timing, write failure, stale consent, document switch and failed Open preservation");
        }
        finally { shell.Close(); }
        Expect(!shell.RequestSave()&&!shell.RequestRecovery()&&!shell.RequestAutoSaveTick(),"Closed save owner retained commands.");
    }
    private static void CheckSaveRecovery(string directory,string language)
    {
        var root=Path.Combine(directory,"recovery-ui-"+language);string path;
        using(var lifetime=new EditorRecoveryLifetime(root))
        {
            path=lifetime.RecoveryPath;var native=NativeMapDocument.Create(18,18,"RecoveredUI");
            native.SetScenarioName(ScenarioNameKind.Variable,0,"Recovered alias");native.Paint([new(3,3)],new(4,2,1));
            using var session=new EditorSaveSession(native);session.SaveTemporary(path);
        }
        var flag=Path.Combine(Path.GetDirectoryName(path)!,"open.flag");File.Move(flag+".closed",flag);
        var bytes=Directory.GetFiles(Path.GetDirectoryName(path)!,"*").ToDictionary(file=>file,File.ReadAllBytes);
        var settings=new AppSettings {Language=language};var shell=SaveShell(settings);
        try
        {
            shell.ConfigureRecoveryDirectory(root,startTimer:false);SaveLayout(shell);
            shell.RecoveryPrompt=candidates=>
            {
                var dialog=shell.CreateRecoverySelectionWindow(candidates);dialog.Owner=shell;SaveLayout(dialog);
                var list=(ListBox)dialog.FindName("RecoveryCandidates");var selection=list.SelectedIndex;
                LocalizationService.SetLanguage(language=="en"?"ru":"en");
                Expect(dialog.Title==LocalizationService.Get("Save.Recover")&&list.SelectedIndex==selection,"Recovery language switch lost selection.");
                LocalizationService.SetLanguage(language);dialog.UpdateLayout();SaveRender(dialog,$"recovery-selection-{language}.png");
                ((Button)dialog.FindName("RecoveryAccept")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Expect(dialog.SelectedPath==path&&!dialog.Accept(),"Recovery accept/closed lifetime failed.");return dialog.SelectedPath;
            };
            Expect(shell.RequestRecovery(automatic:true)&&shell.IsRecoveredDocument&&shell.NormalSavePath is null
                &&((EditorWorkspace)shell.FindName("Editor")).Document is {HasChanges:false}
                &&!((MenuItem)shell.FindName("SaveMenuItem")).IsEnabled&&((MenuItem)shell.FindName("SaveAsMenuItem")).IsEnabled
                &&settings.LastOpenedFile is null&&(settings.RecentFiles?.Count??0)==0,"Recovery adopted normal target or recent/dirty state.");
            var saved=Path.Combine(directory,"recovery-ui-saved-"+language+".dkm");shell.SaveLocationPrompt=_=>saved;shell.SaveConfirmationPrompt=_=>true;
            Expect(shell.RequestSave(saveAs:true)&&!shell.IsRecoveredDocument&&shell.NormalSavePath==saved
                &&shell.CurrentSaveSession!.LastSave!.SourceArchives.Count==1,"Recovered Save As lost target/source archive.");
        }
        finally { shell.Close(); }
        Expect(EditorRecoveryLifetime.Find(root).Count==0&&EditorRecoveryLifetime.Find(root,includeCompleted:true).Any(item=>item.Path==path)
            &&bytes.Where(pair=>pair.Key!=flag).All(pair=>File.ReadAllBytes(pair.Key).AsSpan().SequenceEqual(pair.Value))
            &&File.ReadAllBytes(flag+".dismissed").AsSpan().SequenceEqual(bytes[flag]),"Normal close deleted recovery or left automatic abandoned offer.");
        File.Move(flag+".dismissed",flag);var canceled=SaveShell(new AppSettings());
        try
        {
            canceled.ConfigureRecoveryDirectory(root,startTimer:false);SaveLayout(canceled);canceled.RecoveryPrompt=_=>null;
            Expect(!canceled.RequestRecovery(automatic:true)&&EditorRecoveryLifetime.Find(root).Count==1,"Cancel modified recovery before normal close.");
        }
        finally { canceled.Close(); }
        Expect(EditorRecoveryLifetime.Find(root).Count==0&&File.Exists(path),"Normal close after cancel lost retained recovery or repeated crash offer.");
        Console.WriteLine($"PASS {language} loaded recovery chooser/language/clean no-target state, archive Save As, normal-close acknowledgement and cancel retention");
    }
}
