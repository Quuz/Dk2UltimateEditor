using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckHistory(string directory, string language, bool native)
    {
        var original=NativeMapDocument.CreateNewMap(new("HistoryUI","HistoryUI",20,20,NativeFormat:native));
        var entry=original.SaveCopy(Path.Combine(directory,$"history-{language}-{native}"));
        var sourceBytes=Directory.GetFiles(Path.GetDirectoryName(entry)!,"*",SearchOption.AllDirectories).ToDictionary(path=>path,File.ReadAllBytes);
        var settings=new AppSettings { HistoryDepth=200,Language=language,Thumbnail=new() };
        var shell=new MainWindow(settings,persistSettings:false);
        T Shell<T>(string name)=>(T)shell.FindName(name);
        void Open()=>typeof(MainWindow).GetMethod("OpenMap",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(shell,[entry]);
        var emptySettings=shell.CreateHistorySettingsWindow()!; Expect(shell.CreateHistoryWindow() is null && !Shell<MenuItem>("HistoryMenuItem").IsEnabled,"No-map history command retained.");
        emptySettings.Close(); Open();
        var workspace=Shell<EditorWorkspace>("Editor"); var document=workspace.Document!;
        var copyField=typeof(NativeMapDocument).GetField("historyBufferCopy",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var copyFactory=copyField.GetValue(document);EditorHistoryMemoryException? memoryFailure=null;
        copyField.SetValue(document,new Func<byte[],byte[]>(_=>throw new OutOfMemoryException("Injected UI history failure")));
        try{document.Edit("Memory notice",()=>throw new Exception("Refused action ran."));}
        catch(EditorHistoryMemoryException failure){memoryFailure=failure;}
        finally{copyField.SetValue(document,copyFactory);}
        Expect(memoryFailure is {IsSafeToNotify:true},"UI history failure was not recovered.");
        var noticeCalls=0;
        shell.HistoryMemoryPresenter=notice=>{noticeCalls++;notice.Close();};
        Expect(shell.PresentHistoryMemoryFailure(memoryFailure!)&&noticeCalls==1,"Close did not present the informational error.");
        shell.HistoryMemoryPresenter=notice=>{noticeCalls++;((CheckBox)notice.FindName("HistoryMemoryRemember")).IsChecked=true;((Button)notice.FindName("HistoryMemoryOK")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));};
        Expect(shell.PresentHistoryMemoryFailure(memoryFailure!)&&noticeCalls==2,"OK did not acknowledge the error.");
        Expect(shell.PresentHistoryMemoryFailure(memoryFailure!)&&noticeCalls==2,"Remembered memory notice appeared again.");
        var resetNotice=shell.CreateHistorySettingsWindow()!;((Button)resetNotice.FindName("ResetMemoryButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));resetNotice.Close();
        shell.HistoryMemoryPresenter=notice=>{noticeCalls++;notice.Close();};
        Expect(shell.PresentHistoryMemoryFailure(memoryFailure!)&&noticeCalls==3,"History settings did not restore the notice.");
        shell.HistoryMemoryPresenter=null;
        var loadedBuffers=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        var changes=0; document.Changed+=(_,_)=>changes++;
        var tile2=document.GetTile(2,2); var tile3=document.GetTile(3,3);
        document.Edit("Paste tiles, things and scripts",()=>
        {
            document.Paint([new(2,2)],new(4,1,0));
            document.Edit("Change global definitions",()=>document.Paint([new(3,3)],new(5,2,0)));
        });
        var window=shell.CreateHistoryWindow()!;
        T Field<T>(string name)=>(T)window.FindName(name);
        void Layout()
        {
            window.Measure(new Size(740,580)); window.Arrange(new Rect(0,0,740,580)); window.UpdateLayout();
        }
        Layout();
        var undo=Field<TreeView>("UndoTree"); var redo=Field<TreeView>("RedoTree"); var summary=Field<TextBlock>("SummaryText");
        Expect(window.Document==document && undo.Items.Count==1 && redo.Items.Count==0 && changes==1 && Shell<MenuItem>("UndoMenuItem").IsEnabled,"Actual history window/commands changed map.");
        var root=(TreeViewItem)undo.Items[0];
        Expect(root.Foreground == (Brush)window.FindResource("Brush.Text") && ((TreeViewItem)root.Items[0]).Foreground == root.Foreground,"Dark history tree has unreadable text.");
        Expect((string)root.Header==HistoryLabels.Description("Paste tiles, things and scripts") && root.Items.Count==2
            && ((TreeViewItem)root.Items[1]).Items.Count==1,"Actual history tree lost changed substeps.");
        var snapshot=document.GetHistory();
        Expect(summary.Text==string.Format(LocalizationService.Get("History.Summary"),1,0,1,snapshot.SnapshotBytes,200),"Actual counters mismatch.");
        // Read production edit identifiers independently, including project
        // operations, rather than asserting a duplicated hard-coded list.
        var repo=AppContext.BaseDirectory; while(!Directory.Exists(Path.Combine(repo,"src"))) repo=Path.GetDirectoryName(repo)!;
        foreach(var path in Directory.GetFiles(Path.Combine(repo,"src/DK2MapEditor.Core/Editing"),"*.cs"))
            foreach(Match match in Regex.Matches(File.ReadAllText(path),"\\bEdit\\(\\s*\"([^\"]+)\""))
            {
                var description=match.Groups[1].Value;
                if(description.EndsWith(' ')) continue;
                Expect(!HistoryLabels.Description(description).StartsWith("History.",StringComparison.Ordinal),"Missing operation label: "+description);
                if(language=="ru") Expect(HistoryLabels.Description(description)!=description,"Untranslated Russian operation: "+description);
            }
        foreach(var description in new[]{"Brush","Rectangle","FilledRectangle","Line","Ellipse","FilledEllipse","Add GAMEHERO","Delete Project.TRIGGER","Duplicate GAMEBAND","Add project Creature","Name","ACTION_CAMERAPATH"})
            if(language=="ru") Expect(HistoryLabels.Description(description)!=description,"Dynamic operation untranslated: "+description);
        var retained=summary.Text;
        LocalizationService.SetLanguage(language=="en"?"ru":"en"); Layout();
        Expect(window.Title==LocalizationService.Get("History.Title") && summary.Text!=retained
            && (string)((TreeViewItem)undo.Items[0]).Header==HistoryLabels.Description("Paste tiles, things and scripts") && changes==1,"History live translation changed data/left stale tree.");
        LocalizationService.SetLanguage(language); document.Undo(); Layout();
        Expect(undo.Items.Count==0 && redo.Items.Count==1 && !Shell<MenuItem>("UndoMenuItem").IsEnabled && Shell<MenuItem>("RedoMenuItem").IsEnabled,"History/commands did not refresh on Undo.");
        document.Redo(); Layout();
        Expect(undo.Items.Count==1 && redo.Items.Count==0 && changes==3,"History/commands did not refresh on Redo.");
        if(Environment.GetEnvironmentVariable("DK2_HISTORY_UI_ARTIFACTS") is { Length:>0 } artifacts)
        {
            Directory.CreateDirectory(artifacts); Render(window,$"history-{language}-{native}.png",artifacts,740,580);
        }
        var setting=shell.CreateHistorySettingsWindow()!;
        T Input<T>(string name)=>(T)setting.FindName(name);
        var depth=Input<TextBox>("DepthInput"); var confirmation=Input<CheckBox>("ConfirmClear"); var status=Input<TextBlock>("StatusText");
        Expect(depth.Text=="200","Initial preference mismatch.");
        foreach(var invalid in new[]{"","-1","65536","4294967295","1.5","2,0","+1","０"})
        {
            depth.Text=invalid; Expect(!setting.ApplyDepth() && status.Text==LocalizationService.Get("History.InvalidDepth") && settings.HistoryDepth==200 && document.HistoryDepth==200,"Invalid depth applied.");
        }
        depth.Text="0"; Expect(!setting.ApplyDepth() && document.CanUndo && status.Text==LocalizationService.Get("History.NeedClearConfirmation"),"Zero cleared history without confirmation.");
        LocalizationService.SetLanguage(language=="en"?"ru":"en");
        Expect(depth.Text=="0" && status.Text==LocalizationService.Get("History.NeedClearConfirmation") && setting.Title==LocalizationService.Get("History.Settings"),"Settings translation discarded input/error.");
        LocalizationService.SetLanguage(language); var calls=changes;
        confirmation.IsChecked=true; Expect(setting.ApplyDepth() && settings.HistoryDepth==0 && document.HistoryDepth==0 && !document.CanUndo && !document.CanRedo
            && !Shell<MenuItem>("UndoMenuItem").IsEnabled && !Shell<MenuItem>("RedoMenuItem").IsEnabled && changes==calls && undo.Items.Count==0,"Confirmed zero changed map or left history commands enabled.");
        depth.Text="65535"; Expect(confirmation.IsChecked==false && setting.ApplyDepth() && settings.HistoryDepth==65535,"Max depth / confirmation invalidation failed.");
        Input<Button>("DefaultButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(depth.Text=="200" && settings.HistoryDepth==65535,"Default button applied without confirmation.");
        Expect(setting.ApplyDepth() && settings.HistoryDepth==200,"Default restore failed.");
        depth.Text="2"; Expect(setting.ApplyDepth() && document.HistoryDepth==2,"Session preference not applied.");
        var json=JsonSerializer.Serialize(settings); var deserialized=JsonSerializer.Deserialize<AppSettings>(json)!;
        Expect(deserialized.HistoryDepth==2 && deserialized.Language==language && deserialized.Thumbnail==settings.Thumbnail
            && JsonSerializer.Deserialize<AppSettings>("{}")!.HistoryDepth==200,"Settings JSON failed compatibility/preservation.");
        if(Environment.GetEnvironmentVariable("DK2_HISTORY_UI_ARTIFACTS") is { Length:>0 } output)
        {
            setting.Measure(new Size(620,360)); setting.Arrange(new Rect(0,0,620,360)); setting.UpdateLayout();
            Render(setting,$"history-settings-{language}-{native}.png",output,620,360);
        }
        depth.Text="0"; confirmation.IsChecked=true; setting.Close(); Expect(!setting.ApplyDepth() && settings.HistoryDepth==2,"Closed/canceled settings retained authority.");
        window.Close(); Expect(window.Document is null && undo.Items.Count==0 && redo.Items.Count==0,"Closed history retained source/tree.");
        // Restore bytes after history was intentionally cleared, avoiding any
        // automation of the unrelated native discard confirmation dialog.
        document.SetHistoryDepth(0); document.Paint([new(2,2)],tile2); document.Paint([new(3,3)],tile3);
        Expect(loadedBuffers.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)) && document.HasChanges && document.EditorChangeCount!=0,
            "Direct fixture restoration must keep original change state while restoring every loaded file.");
        // Source MapChanges remains dirty after direct inverse writes, even at depth zero.
        // Detach this private test fixture before Open so the independent lifetime test
        // does not automate the unrelated native discard confirmation.
        document.Changed-=(EventHandler)Delegate.CreateDelegate(typeof(EventHandler),shell,
            typeof(MainWindow).GetMethod("Document_Changed",BindingFlags.Instance|BindingFlags.NonPublic)!);
        document.HistoryChanged-=(EventHandler)Delegate.CreateDelegate(typeof(EventHandler),shell,
            typeof(MainWindow).GetMethod("Document_HistoryChanged",BindingFlags.Instance|BindingFlags.NonPublic)!);
        // Capture the old document window before detaching the private dirty
        // fixture. CreateHistoryWindow correctly returns null without a document.
        window=shell.CreateHistoryWindow() ?? throw new InvalidOperationException("Old document history window must exist before detaching the owner fixture.");
        typeof(MainWindow).GetField("document",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(shell,null);
        typeof(MainWindow).GetField("historyWindow",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(shell,window);
        Open(); Expect(window.Document is null && workspace.Document!=document && workspace.Document!.HistoryDepth==2,"Map replacement retained old history or lost preference.");
        window=shell.CreateHistoryWindow()!; var finalSetting=shell.CreateHistorySettingsWindow()!;
        typeof(MainWindow).GetField("historyWindow",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(shell,window);
        typeof(MainWindow).GetField("historySettingsWindow",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(shell,finalSetting);
        shell.Close(); Expect(window.Document is null && !finalSetting.ApplyDepth() && shell.CreateHistoryWindow() is null
            && shell.CreateHistorySettingsWindow() is null,"Closed owner retained history/settings authority.");
        var invalidShell=new MainWindow(new AppSettings{HistoryDepth=-1},persistSettings:false);
        var normalized=invalidShell.CreateHistorySettingsWindow()!; Expect(((TextBox)normalized.FindName("DepthInput")).Text=="200","Corrupt persisted depth not normalized."); normalized.Close(); invalidShell.Close();
        Expect(sourceBytes.All(pair=>File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)),"History/settings wrote source inputs.");
        Console.WriteLine($"PASS {language} {(native?"native":"project")} original depth, actual nested history/counters, live undo/redo, zero confirmation, JSON, localization and lifetime");
        static void Render(Window source,string name,string path,int width,int height)
        {
            var content=(FrameworkElement)source.Content; content.Measure(new Size(width,height)); content.Arrange(new Rect(0,0,width,height)); content.UpdateLayout();
            var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32); bitmap.Render(content);
            var pixels=new byte[width*height*4]; bitmap.CopyPixels(pixels,width*4,0);
            Expect(pixels.Where((_,index)=>index%4==3).Count(alpha=>alpha!=0)>10000,"Offscreen history render is blank.");
            var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream=new FileStream(Path.Combine(path,name),FileMode.CreateNew); encoder.Save(stream);
        }
    }
}
