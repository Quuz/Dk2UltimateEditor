using System.IO;
using System.Buffers.Binary;
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
    private static string directory="",fixtures="";
    private static int groups;
    [STAThread]
    private static int Main(string[] args)
    {
        if(args.Length!=2 || Directory.Exists(args[0]) || !Directory.Exists(args[1]))throw new ArgumentException("Require fresh artifacts and accepted private historical fixture root.");
        directory=args[0];fixtures=args[1];Directory.CreateDirectory(directory);
        var app=new DK2MapEditor.App.App();app.InitializeComponent();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
        try
        {
            foreach(var language in new[]{"en","ru"})for(var version=1;version<=5;version++)
            {NewWorkspace(language,version);ModifyWorkspace(language,version);CancelDeleteWorkspace(language,version);}
            Check(groups==30,"Expected thirty historical loaded UI groups.");Console.WriteLine("HISTORICAL HERO BAND UI CHECKS PASSED: 30");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
        finally{app.Shutdown();}
    }
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static T At<T>(FrameworkElement owner,string name)where T:FrameworkElement=>owner.FindName(name) as T??throw new InvalidOperationException("Missing actual control "+name);
    private static void Flush(){var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);}
    private static void Show(Window window){window.Left=-10000;window.Top=-10000;window.WindowStartupLocation=WindowStartupLocation.Manual;window.ShowInTaskbar=false;window.Show();Flush();Check(window.IsLoaded,"Actual window not loaded.");}
    private static Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Check(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Historical preview/Cancel/Undo changed live map bytes.");
    private static uint Version(NativeFile file)=>BinaryPrimitives.ReadUInt32LittleEndian(file.Data.Span[4..]);
    private static void Click(FrameworkElement owner,string name){var b=At<Button>(owner,name);Check(b.IsEnabled,"Disabled actual command "+name);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Flush();}
    private static IEnumerable<DependencyObject> Visual(DependencyObject root){yield return root;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var c in Visual(VisualTreeHelper.GetChild(root,i)))yield return c;}
    private static NativeRecord[] Bands(NativeMapDocument d)=>d.Records().Where(NativeMapDocument.IsBand).ToArray();
    private static NativeMapDocument Fixture(string tag,int version)
    {
        var source=Path.Combine(fixtures,$"v{version}-preview/fixture/historical.dkm");var target=Path.Combine(directory,tag+".dkm");File.Copy(source,target);
        var d=NativeMapDocument.Load(target);Check(Version(Bands(d)[0].File)==version,"Fixture historical version changed before actual window.");return d;
    }
    private static string Tag(string language,int version,string action)=>language+"-v"+version+"-"+action;
    private static void Pass(string tag){groups++;Console.WriteLine("PASS "+tag);}
    private static void Select(EditorWorkspace workspace,NativeRecord band)
    {
        At<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=1;Flush();var categories=At<ComboBox>(workspace,"CategoryPicker");
        categories.SelectedItem=categories.Items.Cast<object>().Single(i=>i.GetType().GetProperty("ID")?.GetValue(i)?.ToString()==band.Category);Flush();
        var list=At<ListBox>(workspace,"RecordList");list.SelectedItem=list.Items.Cast<object>().Single(i=>i.GetType().GetProperty("Record")?.GetValue(i) is NativeRecord r && r.File==band.File && r.Offset==band.Offset);Flush();
    }
    private static void Render(Window window,string tag)
    {
        Flush();var content=(FrameworkElement)window.Content;var width=(int)Math.Ceiling(content.ActualWidth);var height=(int)Math.Ceiling(content.ActualHeight);Check(width>400 && height>300,"Actual render dimensions.");
        var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);var backdrop=new DrawingVisual();using(var dc=backdrop.RenderOpen())dc.DrawRectangle(window.Background,null,new Rect(0,0,width,height));bitmap.Render(backdrop);bitmap.Render(content);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(directory,tag+".png"));encoder.Save(file);
    }
    private static void NewWorkspace(string language,int version)
    {
        LocalizationService.SetLanguage(language);var tag=Tag(language,version,"new");var d=Fixture(tag,version);var before=Bytes(d);var h=d.GetHistory();
        var workspace=new EditorWorkspace{Document=d};var shell=new Window{Content=workspace,Width=1100,Height=820};HeroBandWindow? parent=null;Show(shell);workspace.HeroBandPresenter=w=>{parent=w;Show(w);};
        try
        {
            At<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=1;Flush();var picker=At<ComboBox>(workspace,"TemplatePicker");picker.SelectedItem=picker.Items.Cast<object>().Single(i=>i.GetType().GetProperty("LegacyKind")?.GetValue(i)?.ToString()=="Band");Flush();
            var add=Visual(workspace).OfType<Button>().Single(b=>b.Content?.ToString()==LocalizationService.Get("Editor.AddRecord"));add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Flush();Check(parent is{IsLoaded:true},"Actual historical New route failed.");
            var w=parent!;var fields=At<HeroBandValuePanel>(w,"HeroBandTemplate");At<ComboBox>(fields,"HeroCreature").SelectedValue=17u;At<RadioButton>(fields,"HeroRole2").IsChecked=true;Click(w,"HeroBandAdd");Exact(before);Render(w,tag);Click(w,"HeroBandSave");
            Check(w.SavedRecord is{} && Bands(d).Length==3 && Version(w.SavedRecord.File)==5 && d.GetHistory().Undo.Count==h.Undo.Count+1,"Historical New lost band/current layout/one Undo.");d.Undo();Exact(before);Pass(tag);
        }
        finally{parent?.Close();shell.Close();Flush();}
    }
    private static void ModifyWorkspace(string language,int version)
    {
        LocalizationService.SetLanguage(language);var tag=Tag(language,version,"modify");var d=Fixture(tag,version);var band=Bands(d)[1];var before=Bytes(d);var h=d.GetHistory();
        var workspace=new EditorWorkspace{Document=d};var shell=new Window{Content=workspace,Width=1100,Height=820};HeroBandWindow? parent=null;Show(shell);workspace.HeroBandPresenter=w=>{parent=w;Show(w);};
        try
        {
            Select(workspace,band);var command=Visual(workspace).OfType<Button>().Single(b=>b.Content?.ToString()==LocalizationService.Get("HeroBand.Modify"));command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Flush();Check(parent is{IsLoaded:true},"Actual historical Modify route failed.");
            var w=parent!;Check(At<TextBox>(w,"HeroBandName").Text=="Band 1","Historical selected name/ordinal lost in window.");At<ListBox>(w,"HeroBandMembers").SelectedIndex=1;
            w.MemberPresenter=child=>{Show(child);At<ComboBox>(At<HeroBandValuePanel>(child,"HeroMemberFields"),"HeroLevel").SelectedValue=9u;child.Close();return false;};Click(w,"HeroBandModify");Exact(before);
            w.MemberPresenter=child=>{Show(child);var fields=At<HeroBandValuePanel>(child,"HeroMemberFields");At<ComboBox>(fields,"HeroLevel").SelectedValue=9u;var values=fields.ReadValues();LocalizationService.SetLanguage(language=="en"?"ru":"en");Flush();Check(fields.ReadValues()==values,"Historical member language reset draft.");LocalizationService.SetLanguage(language);Flush();Render(child,tag+"-member");Check(child.ApplyMember(),"Historical nested Apply refused.");return true;};
            Click(w,"HeroBandModify");Exact(before);Render(w,tag);Click(w,"HeroBandSave");var saved=w.SavedRecord??throw new InvalidOperationException("Historical parent Save failed.");
            Check(saved.Offset==12+LegacyProjectSchemas.Size("MTHBAND") && d.BandMembers(saved)[1].Level==9 && d.GetHistory().Undo.Count==h.Undo.Count+1,"Historical actual selected payload/offset/one Undo lost.");
            Check(before.Where(p=>p.Key!=saved.File).All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Historical window upgraded unrelated sections/scripts.");d.Undo();Exact(before);Pass(tag);
        }
        finally{parent?.Close();shell.Close();Flush();}
    }
    private static void CancelDeleteWorkspace(string language,int version)
    {
        LocalizationService.SetLanguage(language);var tag=Tag(language,version,"cancel-delete");var d=Fixture(tag,version);var band=Bands(d)[1];var before=Bytes(d);var h=d.GetHistory();
        var workspace=new EditorWorkspace{Document=d};var shell=new Window{Content=workspace,Width=1100,Height=820};HeroBandWindow? parent=null;Show(shell);workspace.HeroBandPresenter=w=>{parent=w;Show(w);};
        try
        {
            Select(workspace,band);var modify=Visual(workspace).OfType<Button>().Single(b=>b.Content?.ToString()==LocalizationService.Get("HeroBand.Modify"));modify.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Flush();Check(parent is{IsLoaded:true},"Historical Cancel parent not loaded.");
            At<TextBox>(parent!,"HeroBandName").Text="Canceled name";Click(parent!,"HeroBandCancel");Exact(before);Check(d.GetHistory().ChangeCount==h.ChangeCount,"Historical Cancel counted/converted main data.");
            Select(workspace,band);var remove=Visual(workspace).OfType<Button>().Single(b=>b.Content?.ToString()==LocalizationService.Get("Editor.Delete") && b.Parent is WrapPanel);workspace.HeroBandDeletionConfirmation=_=>false;remove.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Flush();Exact(before);Render(shell,tag);
            workspace.HeroBandDeletionConfirmation=_=>true;remove.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Flush();Check(Bands(d).Length==1 && Version(band.File)==version && d.GetHistory().Undo.Count==h.Undo.Count+1,"Actual historical Delete altered format/count/history.");
            Check(before.Where(p=>p.Key!=band.File).All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Actual historical Delete altered scripts/other sections.");d.Undo();Exact(before);Pass(tag);
        }
        finally{parent?.Close();shell.Close();Flush();}
    }
}
