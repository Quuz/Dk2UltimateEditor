using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static class Program
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    [STAThread]
    private static int Main(string[] args)
    {
        if(args.Length!=1||Directory.Exists(args[0]))throw new ArgumentException("A new private fixture directory is required.");
        Directory.CreateDirectory(args[0]);var app=new DK2MapEditor.App.App();app.InitializeComponent();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
        try{foreach(var language in new[]{"en","ru"})foreach(var native in new[]{false,true})Check(args[0],language,native);Console.WriteLine("EDITOR SCENARIO NAME UI CHECKS PASSED: 4");return 0;}
        catch(Exception ex){Console.WriteLine(ex);return 1;}
    }
    private static void Check(string directory,string language,bool native)
    {
        LocalizationService.SetLanguage(language);var format=native?"native":"project";
        var d=NativeMapDocument.CreateNewMap(new("AliasUI","AliasUI",16,16,NativeFormat:native));
        var added=native?d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="Trigger"),new(0,0)):d.AddLegacyRecord("Trigger",new(0,0));
        var id=ScriptGraph.ID(added);
        var shell=new MainWindow(new AppSettings(),persistSettings:false);var workspace=(EditorWorkspace)shell.FindName("Editor");workspace.Document=d;
        shell.ShowInTaskbar=false;shell.Left=-10000;shell.Top=-10000;shell.Width=1400;shell.Height=950;shell.Show();
        ScenarioNamesWindow? window=null;workspace.ScenarioNamesPresenter=shown=>{window=shown;shown.ShowInTaskbar=false;shown.Left=-10000;shown.Top=-10000;shown.Show();};
        try
        {
            ((TabControl)workspace.FindName("WorkspaceTabs")).SelectedIndex=2;
            ((Button)workspace.FindName("ScenarioNamesButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(window is {IsLoaded:true},"Actual naming command did not load a window.");
            var w=window!;T Named<T>(string name)=>(T)w.FindName(name);
            var variables=Named<DataGrid>("ScenarioVariables");var timers=Named<DataGrid>("ScenarioTimers");var input=Named<TextBox>("ScenarioNameInput");var apply=Named<Button>("ScenarioNameApply");var discard=Named<Button>("ScenarioNameDiscard");
            Expect(variables.Items.Count==129&&timers.Items.Count==17&&variables.IsReadOnly&&timers.IsReadOnly&&input.MaxLength==63,"Original lists/bounds missing.");
            var before=d.Files.ToDictionary(f=>f.ExportPath,f=>f.Data.ToArray());var h=d.GetHistory();var count=d.EditorChangeCount;var revision=d.EditRevision;
            input.Text="Canceled";discard.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(input.Text==""&&d.EditRevision==revision&&d.EditorChangeCount==count,"Discard modified state.");
            input.Text="Имя";Expect(!w.ApplyName()&&Named<TextBlock>("ScenarioNamesStatus").Text==LocalizationService.Get("ScenarioNames.Invalid")&&d.EditRevision==revision,"Invalid encoding accepted.");
            input.Text="Manager";LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(input.Text=="Manager"&&apply.Content as string==LocalizationService.Get("ScenarioNames.Apply"),"Live translation lost draft/button.");LocalizationService.SetLanguage(language);
            apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            string Current()=>d.ReadScenarioNames().Slots.Single(r=>r.Kind==ScenarioNameKind.Variable&&r.Index==0).StoredName;
            Expect(Current()=="Manager"&&d.EditorChangeCount==count+1&&d.EditRevision==revision+1&&d.GetHistory().Undo.Count==h.Undo.Count&&d.GetHistory().SnapshotBytes==h.SnapshotBytes,"Actual changed Apply did not count once without Undo.");
            apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(Current()=="Manager"&&d.EditorChangeCount==count+2&&d.GetHistory().Undo.Count==h.Undo.Count,"Actual accepted equal name did not count without Undo.");
            d.Undo();Expect(Current()=="Manager"&&!ScriptGraph.Build(d).Events.ContainsKey(id)&&!w.ApplyName()&&Named<TextBlock>("ScenarioNamesStatus").Text==LocalizationService.Get("ScenarioNames.Stale"),"Undo event erased alias or retained stale manager authority.");
            d.Redo();Expect(Current()=="Manager"&&ScriptGraph.Build(d).Events.ContainsKey(id),"Redo event erased accepted alias.");
            if(native)Expect(d.Files.All(f=>f.Data.Span.SequenceEqual(before[f.ExportPath])),"Native aliases changed game bytes.");
            discard.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            timers.SelectedIndex=16;input.Text="Hidden";Expect(w.ApplyName()&&d.ReadScenarioNames().Slots.Single(r=>r.Kind==ScenarioNameKind.Timer&&r.Index==16).StoredName=="Hidden","Special timer alias lost.");
            var saved=d.SaveCopy(Path.Combine(directory,$"saved-{language}-{format}"));var reopened=NativeMapDocument.Load(saved);
            Expect(reopened.ReadScenarioNames().Slots.Single(r=>r.Kind==ScenarioNameKind.Variable&&r.Index==0).StoredName=="Manager"&&reopened.ReadScenarioNames().Slots.Single(r=>r.Kind==ScenarioNameKind.Timer&&r.Index==16).StoredName=="Hidden","Save/reopen lost accepted metadata.");
            w.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);w.UpdateLayout();
            foreach(var name in new[]{"ScenarioNameInput","ScenarioNameApply","ScenarioNameDiscard","ScenarioVariables","ScenarioTimers"})
            {
                var control=Named<FrameworkElement>(name);var origin=control.TranslatePoint(new Point(),w);
                Expect(control.IsVisible&&control.ActualHeight>0&&origin.X>=0&&origin.Y>=0&&origin.X+control.ActualWidth<=w.ActualWidth+0.5&&origin.Y+control.ActualHeight<=w.ActualHeight+0.5,$"Actual naming control clipped: {name}");
            }
            var bitmap=new RenderTargetBitmap((int)Math.Ceiling(w.ActualWidth),(int)Math.Ceiling(w.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(w);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(Path.Combine(directory,$"names-{language}-{format}.png")))encoder.Save(stream);
            workspace.Document=null;workspace.Document=d;Expect(!w.ApplyName(),"Replaced document revived old name authority.");w.Close();Expect(!w.ApplyName(),"Closed window accepted a name.");
            Console.WriteLine($"PASS {language} {format} actual loaded manager command, equal and changed no-Undo acceptance, event Undo/Redo persistence, live translation, save/reopen, discard/invalid/stale/closed guards");
        }
        finally{window?.Close();workspace.Document=null;shell.Close();}
    }
}
