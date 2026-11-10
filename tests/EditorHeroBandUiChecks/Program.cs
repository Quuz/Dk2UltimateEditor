using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static class Program
{
    private static int groups;
    private static string directory="";
    [STAThread]
    private static int Main(string[] args)
    {
        if(args.Length!=1 || Directory.Exists(args[0]))throw new ArgumentException("Require fresh private UI artifacts.");
        directory=args[0];Directory.CreateDirectory(directory);
        var app=new DK2MapEditor.App.App();app.InitializeComponent();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
        try
        {
            foreach(var language in new[]{"en","ru"})foreach(var native in new[]{false,true})
            {NewWorkspace(language,native);ModifyWorkspace(language,native);CancelStale(language,native);Validation(language,native);TranslateEqual(language,native);DeleteWorkspace(language,native);}
            Check(groups==24,"Expected twenty-four loaded UI groups.");Console.WriteLine("EDITOR HERO BAND UI CHECKS PASSED: 24");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
        finally{app.Shutdown();}
    }
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Pass(string name){groups++;Console.WriteLine("PASS "+name);}
    private static string Name(string language,bool native,string action)=>language+"-"+(native?"native":"project")+"-"+action;
    private static NativeMapDocument Fixture(bool native)=>NativeMapDocument.CreateNewMap(new("HeroBandUI","HeroBandUI",16,16,NativeFormat:native));
    private static Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Check(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Preview/Cancel/Undo changed unrelated map bytes.");
    private static void Show(Window window)
    {window.ShowInTaskbar=false;window.WindowStartupLocation=WindowStartupLocation.Manual;window.Left=-10000;window.Top=-10000;window.Show();Flush();Check(window.IsLoaded,"Window did not load.");}
    private static void Flush(){var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);}
    private static T At<T>(FrameworkElement owner,string name)where T:FrameworkElement=>owner.FindName(name) as T??throw new InvalidOperationException("Missing control "+name);
    private static void Click(FrameworkElement owner,string name){var button=At<Button>(owner,name);Check(button.IsEnabled,"Disabled command "+name);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Flush();}
    private static void CheckBandHistory(NativeMapDocument document,string description)
    {
        Check(document.GetHistory().Undo.Last().Description==description,"Wrong actual band history operation.");
        var window=new HistoryWindow(document);Show(window);
        try
        {
            var undo=At<TreeView>(window,"UndoTree");var item=(TreeViewItem)undo.Items[^1];
            Check(item.Header?.ToString()==HistoryLabels.Description(description),"Actual band history label differs.");
            if(LocalizationService.CurrentLanguage=="ru")Check(item.Header?.ToString()!=description,"Russian band history remains English.");
            var language=LocalizationService.CurrentLanguage;var before=item.Header?.ToString();
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Flush();
            Check(((TreeViewItem)undo.Items[^1]).Header?.ToString()!=before,"Band history live translation did not refresh.");
            LocalizationService.SetLanguage(language);Flush();
        }
        finally{window.Close();Flush();}
    }
    private static HeroBandValuePanel Fields(HeroBandWindow window)=>At<HeroBandValuePanel>(window,"HeroBandTemplate");
    private static void Set(FrameworkElement panel,string name,uint value)=>At<ComboBox>(panel,name).SelectedValue=value;
    private static void Leader(HeroBandValuePanel panel,bool leader)
    {At<RadioButton>(panel,leader?"HeroRole2":"HeroRole0").IsChecked=true;}
    private static void Render(Window window,string name)
    {
        Flush();var content=(FrameworkElement)window.Content;var width=(int)Math.Ceiling(content.ActualWidth);var height=(int)Math.Ceiling(content.ActualHeight);
        Check(width>400 && height>300,"Invalid actual loaded render geometry.");
        var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);var backdrop=new DrawingVisual();using(var drawing=backdrop.RenderOpen())drawing.DrawRectangle(window.Background,null,new Rect(0,0,width,height));bitmap.Render(backdrop);bitmap.Render(content);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(directory,name+".png"));encoder.Save(file);
    }
    private static IEnumerable<DependencyObject> Visual(DependencyObject root)
    {yield return root;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Visual(VisualTreeHelper.GetChild(root,i)))yield return child;}
    private static NativeRecord Existing(NativeMapDocument d)
    {
        using var s=d.CreateEditorHeroBandSession();s.Add(new(17,Options2:43));s.Add(new(6));if(s.HasStoredName)s.SetName("Bande de héros n°0");
        return d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(s,"Band 0"));
    }
    private static void SelectWorkspaceBand(EditorWorkspace workspace,NativeMapDocument d,NativeRecord? band=null)
    {
        At<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=1;var categories=At<ComboBox>(workspace,"CategoryPicker");
        var category=d.IsLegacyProject?"Project.MTHBAND":"GAMEBAND";
        categories.SelectedItem=categories.Items.Cast<object>().Single(item=>item.GetType().GetProperty("ID")?.GetValue(item)?.ToString()==category);
        Flush();
        if(band is not null)
        {
            var list=At<ListBox>(workspace,"RecordList");
            list.SelectedItem=list.Items.Cast<object>().Single(item=>item.GetType().GetProperty("Record")?.GetValue(item) is NativeRecord r && r.File==band.File && r.Offset==band.Offset);Flush();
        }
    }
    private static void NewWorkspace(string language,bool native)
    {
        LocalizationService.SetLanguage(language);var tag=Name(language,native,"workspace-new");var d=Fixture(native);var bytes=Bytes(d);var h=d.GetHistory();
        var workspace=new EditorWorkspace{Document=d};var shell=new Window{Content=workspace,Width=1100,Height=820};HeroBandWindow? window=null;
        Show(shell);workspace.HeroBandPresenter=w=>{window=w;Show(w);};
        try
        {
            At<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=1;Flush();
            var picker=At<ComboBox>(workspace,"TemplatePicker");
            picker.SelectedItem=picker.Items.Cast<object>().Single(item=>item.GetType().GetProperty("LegacyKind")?.GetValue(item)?.ToString()=="Band"
                || item.GetType().GetProperty("Template")?.GetValue(item) is RecordTemplate t && t.Structure=="GAMEBAND");
            Flush();
            var command=Visual(workspace).OfType<Button>().Single(button=>button.Content?.ToString()==LocalizationService.Get("Editor.AddRecord"));
            command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Flush();Check(window is{IsLoaded:true},"Actual workspace New did not open parent.");var w=window!;
            Check(At<Button>(w,"HeroBandAdd").Content?.ToString()==LocalizationService.Get("Editor.AddBandMember"),"Actual Add member label missing.");
            var controls=Fields(w);Set(controls,"HeroCreature",17);Leader(controls,true);Click(w,"HeroBandAdd");Leader(controls,false);Set(controls,"HeroCreature",6);Click(w,"HeroBandAdd");
            if(!native)At<TextBox>(w,"HeroBandName").Text="Bande de héros n°0";
            Exact(bytes);Check(d.GetHistory().ChangeCount==h.ChangeCount,"Member preview counted main edits.");Render(w,tag);Click(w,"HeroBandSave");
            Check(w.SavedRecord is{} && d.GetHistory().Undo.Count==h.Undo.Count+1 && d.EditorChangeCount==unchecked(h.EditorChangeCount+1),"Actual workspace parent Save/Undo count.");
            CheckBandHistory(d,"Add hero band");var band=w.SavedRecord!;Check(d.BandMembers(band).Count(m=>m.Active)==2,"New saved band members.");var after=Bytes(d);d.Undo();Exact(bytes);d.Redo();Exact(after);
            Pass(tag);
        }
        finally{window?.Close();shell.Close();Flush();}
    }
    private static void ModifyWorkspace(string language,bool native)
    {
        LocalizationService.SetLanguage(language);var tag=Name(language,native,"workspace-modify");var d=Fixture(native);var band=Existing(d);var bytes=Bytes(d);var h=d.GetHistory();
        var workspace=new EditorWorkspace{Document=d};var shell=new Window{Content=workspace,Width=1100,Height=820};HeroBandWindow? window=null;Show(shell);workspace.HeroBandPresenter=w=>{window=w;Show(w);};
        try
        {
            SelectWorkspaceBand(workspace,d,band);
            var button=Visual(workspace).OfType<Button>().Single(b=>b.Content?.ToString()==LocalizationService.Get("HeroBand.Modify"));button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Flush();
            Check(window is{IsLoaded:true},"Actual workspace Modify did not open parent.");var w=window!;At<ListBox>(w,"HeroBandMembers").SelectedIndex=1;
            w.MemberPresenter=child=>
            {
                Show(child);var fields=At<HeroBandValuePanel>(child,"HeroMemberFields");Set(fields,"HeroLevel",8);Set(fields,"HeroEnemy",0);
                At<TextBox>(fields,"HeroGold").Text="40";Render(child,tag+"-member");Check(child.ApplyMember(),"Nested member failed Apply.");return true;
            };
            Click(w,"HeroBandModify");Exact(bytes);Render(w,tag);Click(w,"HeroBandSave");
            Check(d.BandMembers(band)[1].Level==8 && d.BandMembers(band)[1].Enemy==0 && d.GetHistory().Undo.Count==h.Undo.Count+1,"Nested payload/whole Undo incorrect.");CheckBandHistory(d,"Save hero band");d.Undo();Exact(bytes);Pass(tag);
        }
        finally{window?.Close();shell.Close();Flush();}
    }
    private static void CancelStale(string language,bool native)
    {
        LocalizationService.SetLanguage(language);var tag=Name(language,native,"cancel-stale");var d=Fixture(native);var band=Existing(d);var bytes=Bytes(d);var h=d.GetHistory();
        var w=new HeroBandWindow(d,band,()=>true,()=>true);Show(w);At<ListBox>(w,"HeroBandMembers").SelectedIndex=1;
        w.MemberPresenter=child=>{Show(child);Set(At<HeroBandValuePanel>(child,"HeroMemberFields"),"HeroLevel",9);child.Close();return false;};Click(w,"HeroBandModify");
        Set(Fields(w),"HeroCreature",17);Leader(Fields(w),false);Click(w,"HeroBandReplace");Render(w,tag);Click(w,"HeroBandCancel");Exact(bytes);Check(d.GetHistory().ChangeCount==h.ChangeCount,"Cancel counted.");
        w=new HeroBandWindow(d,band,()=>true,()=>false);Show(w);Check(!w.SaveBand(),"Pending parent commit accepted.");Exact(bytes);w.Close();
        w=new HeroBandWindow(d,band,()=>true,()=>true);Show(w);d.Paint([new(5,5)],new(5,2,1));var changed=Bytes(d);
        Check(!At<Button>(w,"HeroBandSave").IsEnabled && !w.SaveBand(),"Stale parent Save remained active.");Exact(changed);w.Close();Flush();Pass(tag);
    }
    private static void Validation(string language,bool native)
    {
        LocalizationService.SetLanguage(language);var tag=Name(language,native,"leader-capacity");var d=Fixture(native);var before=Bytes(d);var w=new HeroBandWindow(d,null,()=>true,()=>true);Show(w);
        try
        {
            Check(!w.SaveBand(),"Empty band saved.");Set(Fields(w),"HeroCreature",17);Leader(Fields(w),false);Click(w,"HeroBandAdd");Check(!w.SaveBand(),"No-leader band saved.");
            Leader(Fields(w),true);Click(w,"HeroBandAdd");Click(w,"HeroBandAdd");Check(!w.SaveBand(),"Two-leader band saved.");
            var list=At<ListBox>(w,"HeroBandMembers");list.SelectedIndex=2;Click(w,"HeroBandRemove");
            Leader(Fields(w),false);for(var i=2;i<16;i++)Click(w,"HeroBandAdd");Check(!At<Button>(w,"HeroBandAdd").IsEnabled,"Sixteenth capacity guard absent.");
            list.SelectedItems.Add(list.Items[2]);list.SelectedItems.Add(list.Items[3]);Set(Fields(w),"HeroLevel",7);Click(w,"HeroBandReplace");
            Render(w,tag);Exact(before);Check(w.SaveBand(),"Valid one-leader band rejected.");Check(d.BandMembers(w.SavedRecord!).Count(m=>m.Active)==16,"Capacity payload lost.");d.Undo();Exact(before);Pass(tag);
        }
        finally{w.Close();Flush();}
    }
    private static void TranslateEqual(string language,bool native)
    {
        LocalizationService.SetLanguage(language);var tag=Name(language,native,"translation-equal");var d=Fixture(native);var band=Existing(d);d.Paint([new(5,5)],new(5,2,1));d.Undo();var bytes=Bytes(d);var h=d.GetHistory();
        var w=new HeroBandWindow(d,band,()=>true,()=>true);Show(w);
        try
        {
            var fields=Fields(w);Set(fields,"HeroCreature",6);At<TextBox>(fields,"HeroHealth").Text="75";
            var values=fields.ReadValues();LocalizationService.SetLanguage(language=="en"?"ru":"en");Flush();Check(fields.ReadValues()==values,"Translation reset private inputs.");
            LocalizationService.SetLanguage(language);Flush();Check(fields.ReadValues()==values,"Back translation reset private inputs.");Render(w,tag);Exact(bytes);
            Check(w.SaveBand(),"Equal parent Save refused.");Exact(bytes);Check(d.GetHistory().Undo.Count==h.Undo.Count+1 && !d.CanRedo,"Equal Save failed own Undo/clear redo.");
            d.Undo();Exact(bytes);Check(d.EditorChangeCount==h.EditorChangeCount,"Equal Save Undo count.");Pass(tag);
        }
        finally{w.Close();Flush();}
    }
    private static void DeleteWorkspace(string language,bool native)
    {
        LocalizationService.SetLanguage(language);var tag=Name(language,native,"workspace-delete");var d=Fixture(native);var band=Existing(d);
        var owner=d.ScriptOwners().Single(o=>o.Scope==8 && o.Record.File==band.File && o.Record.Offset==band.Offset);
        using(var point=d.CreateEditorScenarioPointSession(initialScope:8))
        {point.SelectOwner(owner);var root=d.ApplyEditorScenarioPoint(d.PrepareEditorScenarioPoint(point));d.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMEELAPSED"));}
        var script=d.Files.Single(f=>f.Signature==(native?0xD2u:NativeMapDocument.LegacyEvents));var scriptBefore=script.Data.ToArray();var bytes=Bytes(d);var h=d.GetHistory();
        var workspace=new EditorWorkspace{Document=d};var shell=new Window{Content=workspace,Width=1100,Height=820};Show(shell);
        try
        {
            SelectWorkspaceBand(workspace,d,band);var command=Visual(workspace).OfType<Button>().Single(b=>b.Content?.ToString()==LocalizationService.Get("Editor.Delete") && b.Parent is WrapPanel);
            workspace.HeroBandDeletionConfirmation=_=>false;command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Flush();Exact(bytes);Check(d.GetHistory().ChangeCount==h.ChangeCount,"Refused Delete counted.");
            Render(shell,tag);workspace.HeroBandDeletionConfirmation=_=>true;command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Flush();
            Check(!d.Records().Any(NativeMapDocument.IsBand) && script.Data.Span.SequenceEqual(scriptBefore) && d.GetHistory().Undo.Count==h.Undo.Count+1,"Actual source Delete lost script bytes or band/Undo count.");
            CheckBandHistory(d,"Delete hero band");d.Undo();Exact(bytes);Pass(tag);
        }
        finally{shell.Close();Flush();}
    }
}
