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

internal static partial class Program
{
    private static readonly uint[] Scopes=[32,1,2,64,128,256,4,8,16];
    private static string directory="";private static int count;
    [STAThread]
    private static int Main(string[] args)
    {
        if(args.Length!=1 || Directory.Exists(args[0]))throw new ArgumentException("Require fresh private renders directory.");
        directory=args[0];Directory.CreateDirectory(directory);var app=new DK2MapEditor.App.App();app.InitializeComponent();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
        try
        {
            foreach(var language in new[]{"en","ru"})foreach(var native in new[]{false,true})
            {
                foreach(var scope in Scopes)New(language,native,scope);
                foreach(var scope in Scopes.Where(scope=>scope!=32))Modify(language,native,scope);
                CancelAndStale(language,native);ExistingLevel(language,native);
                DefaultPlayer(language,native);
                WorkspaceOperations(language,native);
            }
            Check(count==84,"Expected84 loaded cases");Console.WriteLine("EDITOR SCENARIO POINT UI CHECKS PASSED: 84");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
        finally{app.Shutdown();}
    }
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Pass(string name){count++;Console.WriteLine("PASS "+name);}
    private static string Name(string language,bool native,string action,uint scope)=>$"{language}-{(native?"native":"project")}-{action}-{scope}";
    private static NativeMapDocument Fixture(bool native)
    {
        var d=NativeMapDocument.CreateNewMap(new("PointUI","PointUI",16,16,NativeFormat:native));
        if(native)d.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="GAMEPLAYER"),new(0,0),4);
        foreach(var copy in new[]{0,1})foreach(var (kind,structure) in new[]{("Creature","GAMEKEEPER"),("Door","GAMEDOOR"),("Object","GAMEOBJECT"),("MagicalObject","GAMEOBJECT"),("Area","GAMEAREA"),("Band","GAMEBAND")})
        {
            var record=native?d.AddRecord(RecordTemplate.All.Single(template=>template.Structure==structure),new(2+copy,4)):d.AddEditorLegacyRecord(kind,new(2+copy,4));
            if(kind=="MagicalObject")d.SetField(record,record.Fields.Single(field=>field.Name==(native?"bObjectID":"id")),ValueCatalogues.ForTemplate(kind,ValueCatalogues.GameVersion(d)).First().Value);
            if(kind=="Band")d.AddEditorBandMember(record,6);
        }
        Check(Scopes.All(scope=>d.ScriptOwners().Any(owner=>owner.Scope==scope)),"Nine scope UI fixture");return d;
    }
    private static ScriptOwner Owner(NativeMapDocument d,uint scope,bool last=false)=>last?d.ScriptOwners().Last(owner=>owner.Scope==scope):d.ScriptOwners().First(owner=>owner.Scope==scope);
    private static bool Same(ScriptOwner a,ScriptOwner b)=>a.Scope==b.Scope && a.MemberIndex==b.MemberIndex && a.PlayerID==b.PlayerID && a.Record.File==b.Record.File && a.Record.Offset==b.Record.Offset;
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument d)=>d.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Check(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"UI preview/cancel changed source bytes");
    private static string Label(ScriptOwner owner)=>LocalizationService.Get("ScriptScope."+owner.Scope)+$" · #{owner.Record.Offset} · {owner.MemberIndex+1}";
    private static void Show(Window window)
    {window.ShowInTaskbar=false;window.WindowStartupLocation=WindowStartupLocation.Manual;window.Left=-10000;window.Top=-10000;window.Show();Flush();Check(window.IsLoaded,"Actual WPF window not loaded");}
    private static void Flush(){var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);}
    private static Button Button(ScenarioPointWindow window,string name)=>(Button)window.FindName(name);
    private static void Click(ScenarioPointWindow window,string name){var button=Button(window,name);Check(button.IsEnabled,"Disabled command "+name);button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));Flush();}
    private static void Pick(ScenarioPointWindow window,ScriptOwner owner)
    {var list=(ListBox)window.FindName("ScenarioPointOwners");list.SelectedItem=list.Items.Cast<ListBoxItem>().Single(row=>row.Tag is ScenarioPointOwnerChoice choice && Same(choice.Owner,owner));Flush();Check(window.SelectedOwner is { } selected && Same(selected,owner),"Owner row did not select private owner");}
    private static IEnumerable<DependencyObject> Visual(DependencyObject parent)
    {yield return parent;for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)foreach(var child in Visual(VisualTreeHelper.GetChild(parent,i)))yield return child;}
    private static void Render(ScenarioPointWindow window,string name)
    {
        window.UpdateLayout();Flush();var bitmap=new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),(int)Math.Ceiling(window.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(window);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(Path.Combine(directory,name+".png"));encoder.Save(stream);
    }
    private static void New(string language,bool native,uint scope)
    {
        LocalizationService.SetLanguage(language);var name=Name(language,native,"new",scope);var d=Fixture(native);var bytes=Snapshot(d);var history=d.GetHistory();var revision=d.EditRevision;
        var shell=new MainWindow(new AppSettings(),persistSettings:false);var workspace=(EditorWorkspace)shell.FindName("Editor");workspace.Document=d;workspace.Visibility=Visibility.Visible;Show(shell);ScenarioPointWindow? window=null;
        workspace.ScenarioPointPresenter=shown=>{window=shown;Show(shown);};
        try
        {
            ((TabControl)workspace.FindName("WorkspaceTabs")).SelectedIndex=2;
            ((ComboBox)workspace.FindName("ScriptScopePicker")).SelectedValue=(int)scope;((ComboBox)workspace.FindName("ScriptOwnerPicker")).SelectedIndex=0;Flush();
            var command=Visual(workspace).OfType<Button>().Single(button=>button.Content?.ToString()==LocalizationService.Get("Editor.ScriptOpenScope"));command.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));Flush();
            Check(window is {IsLoaded:true},"Actual workspace New did not load wizard");var w=window!;Check(w.SelectedScope==scope && !w.IsOwnerPage,"Original type page missing");Exact(bytes);
            if(scope==32){Check(!Button(w,"ScenarioPointNext").IsEnabled,"Level has no owner page");}
            else{Check(!Button(w,"ScenarioPointFinish").IsEnabled,"Nonlevel Finish allowed before owner page");Click(w,"ScenarioPointNext");Pick(w,Owner(d,scope));Click(w,"ScenarioPointBack");Exact(bytes);Click(w,"ScenarioPointNext");}
            Render(w,name);Exact(bytes);Click(w,"ScenarioPointFinish");Check(w.SavedPoint is {Scope:var actual} && actual==scope,"Workspace Finish lost point");
            Check(d.EditRevision==revision+1 && d.GetHistory().ChangeCount==history.ChangeCount+1 && d.EditorChangeCount==unchecked(history.EditorChangeCount+1),"Workspace source Finish count");
            if(native)Exact(bytes);else{d.Undo();Exact(bytes);d.Redo();Check(d.FindScriptScope(Owner(d,scope)) is not null,"UI point redo missing");}
            Pass(name);
        }
        finally{window?.Close();shell.Close();Flush();}
    }
    private static ScriptEdge Branch(NativeMapDocument d,ScriptOwner owner)
    {using var s=d.CreateEditorScenarioPointSession(initialScope:owner.Scope);s.SelectOwner(owner);var root=d.ApplyEditorScenarioPoint(d.PrepareEditorScenarioPoint(s));d.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));return d.FindScriptScope(owner)!;}
    private static void Modify(string language,bool native,uint scope)
    {
        LocalizationService.SetLanguage(language);var name=Name(language,native,"modify",scope);var d=Fixture(native);var first=Owner(d,scope);var destination=Owner(d,scope,true);var root=Branch(d,first);var bytes=Snapshot(d);var history=d.GetHistory();var revision=d.EditRevision;ScriptEdge? result=null;
        var window=new ScenarioPointWindow(d,()=>true,()=>true,Label,saved=>result=saved,existing:root);Show(window);
        try
        {
            foreach(var type in Scopes)Check(!((RadioButton)window.FindName("ScenarioPointScope"+type)).IsEnabled,"Modify type not locked");Click(window,"ScenarioPointNext");Pick(window,destination);Render(window,name);Exact(bytes);
            Click(window,"ScenarioPointFinish");Check(result?.Target==root.Target && d.FindScriptScope(destination)?.Target==root.Target,"Modify lost event chain");Check(d.GetHistory().ChangeCount==history.ChangeCount+1 && d.EditRevision==revision+1,"Modify accepted count");d.Undo();Exact(bytes);Pass(name);
        }
        finally{window.Close();Flush();}
    }
    private static void CancelAndStale(string language,bool native)
    {
        LocalizationService.SetLanguage(language);var name=Name(language,native,"cancel-stale",2);var d=Fixture(native);var bytes=Snapshot(d);var history=d.GetHistory();
        var window=new ScenarioPointWindow(d,()=>true,()=>true,Label,_=>throw new InvalidOperationException("Cancelled wizard completed"));Show(window);
        ((RadioButton)window.FindName("ScenarioPointScope2")).IsChecked=true;Click(window,"ScenarioPointNext");Pick(window,Owner(d,2));Render(window,name);Click(window,"ScenarioPointCancel");Exact(bytes);Check(d.GetHistory().ChangeCount==history.ChangeCount,"Cancel counted");
        var current=true;window=new ScenarioPointWindow(d,()=>current,()=>true,Label,_=>throw new InvalidOperationException("Stale wizard completed"),initialScope:2);Show(window);Click(window,"ScenarioPointNext");Pick(window,Owner(d,2));current=false;window.InvalidateOwner();Check(!Button(window,"ScenarioPointFinish").IsEnabled && !window.Finish(),"Stale Finish accepted");Exact(bytes);window.Close();Flush();Pass(name);
    }
    private static void ExistingLevel(string language,bool native)
    {
        LocalizationService.SetLanguage(language);var name=Name(language,native,"existing-level",32);var d=Fixture(native);var root=Branch(d,Owner(d,32));var bytes=Snapshot(d);var window=new ScenarioPointWindow(d,()=>true,()=>true,Label,_=>throw new InvalidOperationException("Existing Level finished"),existing:root);Show(window);
        try{Check(!Button(window,"ScenarioPointNext").IsEnabled && !Button(window,"ScenarioPointFinish").IsEnabled && !window.Finish(),"Source existing Level Finish enabled");Render(window,name);Exact(bytes);Pass(name);}finally{window.Close();Flush();}
    }
    private static void DefaultPlayer(string language,bool native)
    {
        LocalizationService.SetLanguage(language);var name=Name(language,native,"default-player",1);var d=Fixture(native);var bytes=Snapshot(d);var window=new ScenarioPointWindow(d,()=>true,()=>true,Label,_=>{},initialScope:1);Show(window);
        try
        {
            Click(window,"ScenarioPointNext");var owner=window.SelectedOwner;Check(owner is not null && (owner.PlayerID==3 || owner.Record.Fields.FirstOrDefault(field=>field.Name=="bGlobalPlayer") is { } id && owner.Record.Read(id)=="3"),"Original default player3 missing");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Check(window.Title==LocalizationService.Get("ScenarioPoint.Title") && window.SelectedOwner is { } current && Same(current,owner!),"Language switch lost physical selection/title");LocalizationService.SetLanguage(language);
            Render(window,name);Exact(bytes);Click(window,"ScenarioPointCancel");Exact(bytes);Pass(name);
        }
        finally{window.Close();Flush();}
    }
}
