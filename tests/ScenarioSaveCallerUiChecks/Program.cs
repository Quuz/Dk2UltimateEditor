using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
    [STAThread]
    private static int Main(string[] args)
    {
        if(args.Length!=1 || Directory.Exists(args[0]))throw new ArgumentException("Require a fresh private artifacts directory.");
        Directory.CreateDirectory(args[0]);var app=new DK2MapEditor.App.App();app.InitializeComponent();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
        try
        {
            foreach(var language in new[]{"en","ru"})foreach(var native in new[]{false,true})
                foreach(var type in new[]{"TRIGGER_DEATH","ACTION_ADDELEMENT","TRIGGER_VARIABLE","TRIGGER_TIMER","ACTION_SETVARIABLE","ACTION_SETTIMER"})Check(args[0],language,native,type);
            WizardSaveCallerUiChecks.Run(args[0]);
            Console.WriteLine("SCENARIO SAVE CALLER LOADED UI CHECKS PASSED: 72");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
    }
    private static void Check(string directory,string language,bool native,string typeName)
    {
        LocalizationService.SetLanguage(language);var format=native?"native":"project";
        var d=NativeMapDocument.CreateNewMap(new("CallerUI","CallerUI",16,16,NativeFormat:native));
        var type=LegacyEventSchemas.All.Single(item=>item.Name==typeName);var kind=type.RecordSignature==213?"Trigger":"Action";
        var row=native?d.AddRecord(RecordTemplate.All.Single(item=>item.Structure==kind),new(0,0)):d.AddLegacyRecord(kind,new(0,0));
        d.SetEventType(row,type);row=d.Records().Single(item=>item.File==row.File && item.Offset==row.Offset && item.Length==row.Length);
        var alias=NativeMapDocument.HasScenarioAliasPage(typeName);var clear=typeName=="TRIGGER_DEATH";
        if(clear)
        {
            Check(MemoryMarshal.TryGetArray(row.File.Data,out var bytes),"Raw source fixture bytes");
            for(var index=0;index<(native?8:32);index++)bytes.Array![bytes.Offset+row.Offset+(native?8:16)+index]=(byte)(0x93^index);
        }
        if(alias)
        {
            void Set(int number,string value)=>d.SetField(row,row.Fields.Single(field=>field.Name==(native?"Param":"param")+number),value);
            Set(typeName.StartsWith("TRIGGER_",StringComparison.Ordinal)?2:1,"7");
            if(kind=="Trigger")Set(1,"3");
            if(typeName=="TRIGGER_VARIABLE"){Set(3,"1");Set(4,"0");Set(5,"42");}
            if(typeName=="TRIGGER_TIMER")Set(5,"3661");
            if(typeName=="ACTION_SETVARIABLE"){Set(2,"10");Set(5,"42");}
            d.SetScenarioName(typeName is "TRIGGER_VARIABLE" or "ACTION_SETVARIABLE"?ScenarioNameKind.Variable:ScenarioNameKind.Timer,7,"Keep");
        }
        var shell=new MainWindow(new AppSettings(),persistSettings:false);var workspace=(EditorWorkspace)shell.FindName("Editor");workspace.Document=d;
        shell.ShowInTaskbar=false;shell.Left=-10000;shell.Top=-10000;shell.Width=1400;shell.Height=950;shell.Show();
        ScenarioParameterWindow? window=null;
        workspace.ScenarioParameterPresenter=shown=>{window=shown;shown.ShowInTaskbar=false;shown.Left=-10000;shown.Top=-10000;shown.Show();};
        try
        {
            ((TabControl)workspace.FindName("WorkspaceTabs")).SelectedIndex=1;
            var categories=(ComboBox)workspace.FindName("CategoryPicker");categories.SelectedItem=categories.Items.Cast<object>().Single(item=>Read<string>(item,"ID")==row.Category);
            var records=(ListBox)workspace.FindName("RecordList");records.SelectedItem=records.Items.Cast<object>().Single(item=>Read<NativeRecord>(item,"Record").Offset==row.Offset);
            var command=(Button)workspace.FindName("ScenarioParameterButton");Check(command.IsEnabled,"Actual parameter command unavailable");command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(window is{IsLoaded:true},"Actual workspace command did not load parent");var w=window!;
            T Named<T>(string name)=>(T)w.FindName(name);
            var original=Snapshot(d);var before=d.GetHistory();var revision=d.EditRevision;
            var expected=original.ToDictionary(item=>item.Key,item=>(byte[])item.Value.Clone());if(clear)Array.Clear(expected[row.File],row.Offset+(native?8:16),native?8:32);
            Named<Button>("ScenarioPageApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Exact(expected);
            Check(d.EditorChangeCount==unchecked(before.EditorChangeCount+1) && d.GetHistory().ChangeCount==before.ChangeCount+1 && d.EditRevision==revision+1,"Actual parent first Apply count/revision");
            Check(d.GetHistory().Undo.Count==before.Undo.Count+(clear?1:0),"Actual parent first Apply history");
            d.Paint([new(2,2)],new(5,2,1));var painted=Snapshot(d);var paintedHistory=d.GetHistory();d.Undo();w.ResetDraft();
            var equal=Snapshot(d);var equalNames=Names(d);var equalHistory=d.GetHistory();var equalRevision=d.EditRevision;
            Check(w.ApplyDraft(),"Loaded equal parent Apply refused");Exact(equal);
            var accepted=d.GetHistory();Check(d.EditorChangeCount==unchecked(equalHistory.EditorChangeCount+1) && accepted.ChangeCount==equalHistory.ChangeCount+1 && d.EditRevision==equalRevision+1,"Actual equal parent accepted count");
            Check(accepted.Undo.SequenceEqual(equalHistory.Undo) && accepted.Redo.SequenceEqual(equalHistory.Redo) && accepted.SnapshotBytes==equalHistory.SnapshotBytes && Names(d)==equalNames,"Actual equal parent preserves history/metadata");
            d.Redo();Exact(painted);Check(d.GetHistory().ChangeCount==paintedHistory.ChangeCount+1,"Loaded parent redo carries count");d.Undo();Exact(equal);w.ResetDraft();
            var count=d.EditorChangeCount;var rev=d.EditRevision;var history=d.GetHistory();
            Named<Button>("ScenarioPageDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Exact(equal);
            Check(d.EditorChangeCount==count && d.EditRevision==rev && d.GetHistory().Undo.SequenceEqual(history.Undo) && d.GetHistory().Redo.SequenceEqual(history.Redo),"Parent Discard must not accept Save");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Check(w.Title==LocalizationService.Get("ScenarioPage.Title"),"Live translated parent title");LocalizationService.SetLanguage(language);
            // The same real Window on a private wizard document must use the
            // generic no-op route, including all four embedded alias pages.
            var privateDocument=NativeMapDocument.CreateNewMap(new("Private","Private",16,16,NativeFormat:native));
            var privateRow=native?privateDocument.AddRecord(RecordTemplate.All.Single(item=>item.Structure==kind),new(0,0)):privateDocument.AddLegacyRecord(kind,new(0,0));
            privateDocument.SetEventType(privateRow,type);privateRow=privateDocument.Records().Single(item=>item.File==privateRow.File && item.Offset==privateRow.Offset && item.Length==privateRow.Length);
            foreach(var editingExisting in new[]{false,true})
            {
                var privateWindow=new ScenarioParameterWindow(privateDocument,privateRow,()=>true,()=>true,privateCreation:true,privateEditing:editingExisting){ShowInTaskbar=false,Left=-10000,Top=-10000};
                try
                {
                    privateWindow.Show();Check(privateWindow.IsLoaded,"Private wizard parameter Window not loaded");
                    var privateBytes=Snapshot(privateDocument);var privateNames=Names(privateDocument);var privateHistory=privateDocument.GetHistory();var privateRevision=privateDocument.EditRevision;
                    Check(privateWindow.ApplyDraft(),"Private wizard no-op Apply refused");Exact(privateBytes);
                    var afterPrivate=privateDocument.GetHistory();Check(privateDocument.EditRevision==privateRevision && privateDocument.EditorChangeCount==privateHistory.EditorChangeCount && afterPrivate.ChangeCount==privateHistory.ChangeCount && afterPrivate.Undo.SequenceEqual(privateHistory.Undo) && afterPrivate.Redo.SequenceEqual(privateHistory.Redo) && Names(privateDocument)==privateNames,"Private wizard no-op invoked live Save counter");
                }
                finally{privateWindow.Close();}
            }
            var repeatBefore=d.GetHistory();var repeatRevision=d.EditRevision;
            Check(w.ApplyDraft(),"Repeated loaded equal parent Apply refused");Exact(equal);
            var repeatAfter=d.GetHistory();Check(repeatAfter.ChangeCount==repeatBefore.ChangeCount+1 && repeatAfter.EditorChangeCount==unchecked(repeatBefore.EditorChangeCount+1) && d.EditRevision==repeatRevision+1 && repeatAfter.Undo.SequenceEqual(repeatBefore.Undo) && repeatAfter.Redo.SequenceEqual(repeatBefore.Redo) && repeatAfter.SnapshotBytes==repeatBefore.SnapshotBytes && Names(d)==equalNames,"Repeated loaded equal parent did not count once with retained history");
            w.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);w.UpdateLayout();
            foreach(var name in new[]{"ScenarioPageApply","ScenarioPageDiscard","ScenarioPageStatus"})
            {
                var control=Named<FrameworkElement>(name);var position=control.TranslatePoint(new Point(),w);
                Check(control.IsVisible && control.ActualHeight>0 && position.X>=0 && position.Y>=0 && position.X+control.ActualWidth<=w.ActualWidth+0.5 && position.Y+control.ActualHeight<=w.ActualHeight+0.5,"Actual parent footer clipped: "+name);
            }
            var bitmap=new RenderTargetBitmap((int)Math.Ceiling(w.ActualWidth),(int)Math.Ceiling(w.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(w);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using(var stream=new FileStream(Path.Combine(directory,$"parent-{typeName}-{language}-{format}.png"),FileMode.CreateNew))encoder.Save(stream);
            workspace.Document=null;workspace.Document=d;Check(!w.ApplyDraft(),"Replaced workspace revived old parent");w.Close();Check(!w.ApplyDraft(),"Closed parent accepted Save");
            Console.WriteLine($"PASS {language} {format} {typeName} loaded workspace Save: source count, normalization/equal history, redo, private wizard no-op, discard, translation, footer and closed/owner guards");
        }
        finally{window?.Close();workspace.Document=null;shell.Close();}
    }
    private static T Read<T>(object source,string name)=>(T)source.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.Instance)!.GetValue(source)!;
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document)=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Check(bytes.All(item=>item.Key.Data.Span.SequenceEqual(item.Value)),"Unexpected event/graph/opaque byte mutation");
    private static string Names(NativeMapDocument document)=>string.Join("|",document.ReadScenarioNames().Slots.Select(slot=>$"{slot.Kind}:{slot.Index}:{slot.StoredName}"));
    private static void Check(bool accepted,string message){if(!accepted)throw new InvalidOperationException(message);}
}
