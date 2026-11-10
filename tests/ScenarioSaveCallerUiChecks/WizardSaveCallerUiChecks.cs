using System.IO;
using System.Reflection;
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

internal static class WizardSaveCallerUiChecks
{
    internal static void Run(string directory)
    {
        foreach(var language in new[]{"en","ru"})foreach(var native in new[]{false,true})
            foreach(var type in new[]{"TRIGGER_DEATH","TRIGGER_TIMER","ACTION_ADDELEMENT","ACTION_SETTIMER","ACTION_SHOWCRYSTAL","ACTION_VICTORY","ACTION_DEFEAT","ACTION_HASCRYSTAL","ACTION_POSSESSCREATURE","ACTION_MAKEHUNGRY","ACTION_REMOVE","ACTION_SHOW"})Check(directory,language,native,type);
    }
    private static void Check(string directory,string language,bool native,string typeName)
    {
        LocalizationService.SetLanguage(language);var format=native?"native":"project";
        var document=NativeMapDocument.CreateNewMap(new("WizardUI","WizardUI",16,16,NativeFormat:native));
        var type=LegacyEventSchemas.All.Single(item=>item.Name==typeName);var record=document.AddScenarioEvent(type.RecordSignature,new(0,0),type);var id=ScriptGraph.ID(record);
        document.Paint([new(2,2)],new(5,2,1));var painted=Snapshot(document);var paintedHistory=document.GetHistory();document.Undo();
        var baseline=Snapshot(document);var baselineNames=Names(document);
        var shell=new MainWindow(new AppSettings(),persistSettings:false);var workspace=(EditorWorkspace)shell.FindName("Editor");workspace.Document=document;
        shell.ShowInTaskbar=false;shell.Left=-10000;shell.Top=-10000;shell.Width=1400;shell.Height=950;shell.Show();
        ScenarioCreationWindow? active=null;
        workspace.ScenarioEditingPresenter=window=>{active=window;window.ShowInTaskbar=false;window.Left=-10000;window.Top=-10000;window.Show();};
        void Open()
        {
            active=null;var current=ScriptGraph.Build(document).Events[id].Record;
            ((TabControl)workspace.FindName("WorkspaceTabs")).SelectedIndex=1;
            var categories=(ComboBox)workspace.FindName("CategoryPicker");categories.SelectedItem=categories.Items.Cast<object>().Single(item=>Read<string>(item,"ID")==current.Category);
            var records=(ListBox)workspace.FindName("RecordList");records.SelectedItem=records.Items.Cast<object>().Single(item=>Read<NativeRecord>(item,"Record").File==current.File && Read<NativeRecord>(item,"Record").Offset==current.Offset);
            var command=(Button)workspace.FindName("ScenarioEditEventButton");Expect(command.IsEnabled && command.Visibility==Visibility.Visible,"Actual Edit event command unavailable");command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(active is{IsLoaded:true,IsEditingExisting:true} && active.Step==ScenarioCreationStep.Type,"Actual existing wizard did not load Type-first");
            Exact(baseline);Expect(Names(document)==baselineNames,"Opening wizard changed names");
        }
        void ReadyToFinish()
        {
            if(LegacyEventSchemas.ParameterPageID(typeName)!=-1 || type.RecordSignature==213)
                Expect(active!.Next(),"Existing wizard Next refused");
            if(active!.Step==ScenarioCreationStep.Parameters && type.RecordSignature==213)
                Expect(active.Next(),"Existing condition repeat step refused");
            active!.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);active.UpdateLayout();
            var finish=(Button)active.FindName("ScenarioWizardFinish");Expect(finish.IsEnabled && finish.Visibility==Visibility.Visible,"Existing wizard Finish unavailable");
        }
        try
        {
            Open();ReadyToFinish();
            var window=active!;var history=document.GetHistory();var revision=document.EditRevision;
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(window.Title==LocalizationService.Get("ScenarioWizard.EditTitle"),"Existing wizard live title translation");LocalizationService.SetLanguage(language);
            window.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);window.UpdateLayout();
            foreach(var name in new[]{"ScenarioWizardFinish","ScenarioWizardCancel","ScenarioWizardBack"})
            {
                var button=(FrameworkElement)window.FindName(name);var origin=button.TranslatePoint(new Point(),window);
                Expect(button.IsVisible && button.ActualHeight>0 && origin.X>=0 && origin.Y>=0 && origin.X+button.ActualWidth<=window.ActualWidth+0.5 && origin.Y+button.ActualHeight<=window.ActualHeight+0.5,"Wizard footer clipped: "+name);
            }
            var bitmap=new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),(int)Math.Ceiling(window.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using(var stream=new FileStream(Path.Combine(directory,$"finish-{typeName}-{language}-{format}.png"),FileMode.CreateNew))encoder.Save(stream);
            ((Button)window.FindName("ScenarioWizardFinish")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Exact(baseline);
            window.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
            var accepted=document.GetHistory();Expect(window.CreatedEventID==id && !window.IsVisible && !window.Finish(),"Actual Finish did not accept/close exactly once");
            Expect(accepted.ChangeCount==history.ChangeCount+1 && accepted.EditorChangeCount==unchecked(history.EditorChangeCount+1) && document.EditRevision==revision+1 && accepted.Undo.SequenceEqual(history.Undo) && accepted.Redo.SequenceEqual(history.Redo) && accepted.SnapshotBytes==history.SnapshotBytes && Names(document)==baselineNames,"Loaded equal Finish counter/history/metadata");
            document.Redo();Exact(painted);Expect(document.GetHistory().ChangeCount==paintedHistory.ChangeCount+1 && document.EditorChangeCount==unchecked(paintedHistory.EditorChangeCount+1),"Loaded wizard redo carries accepted count");document.Undo();Exact(baseline);
            Open();ReadyToFinish();var repeated=document.GetHistory();var repeatedRevision=document.EditRevision;
            Expect(active!.Finish(),"Repeated actual equal Finish refused");Exact(baseline);var afterRepeated=document.GetHistory();
            Expect(afterRepeated.ChangeCount==repeated.ChangeCount+1 && afterRepeated.EditorChangeCount==unchecked(repeated.EditorChangeCount+1) && document.EditRevision==repeatedRevision+1 && afterRepeated.Undo.SequenceEqual(repeated.Undo) && afterRepeated.Redo.SequenceEqual(repeated.Redo) && Names(document)==baselineNames,"Repeated equal wizard acceptance");
            Open();ReadyToFinish();var cancelled=document.GetHistory();var cancelledRevision=document.EditRevision;var cancelledWindow=active!;cancelledWindow.Close();
            Exact(baseline);SameHistory(cancelled,document.GetHistory());Expect(document.EditRevision==cancelledRevision && !cancelledWindow.Finish() && Names(document)==baselineNames,"Wizard Cancel counted Save");
            Console.WriteLine($"PASS {language} {format} {typeName} loaded Edit/Finish command: equal/repeated source count, no new Undo, redo/identity/names retained, Cancel, translation and footer");
        }
        finally{active?.Close();workspace.Document=null;shell.Close();}
    }
    private static T Read<T>(object source,string name)=>(T)source.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.Instance)!.GetValue(source)!;
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document)=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static string Names(NativeMapDocument document)=>string.Join("|",document.ReadScenarioNames().Slots.Select(slot=>$"{slot.Kind}:{slot.Index}:{slot.StoredName}"));
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Expect(bytes.All(item=>item.Key.Data.Span.SequenceEqual(item.Value)),"Unexpected live/graph/opaque bytes");
    private static void SameHistory(EditHistorySnapshot a,EditHistorySnapshot b)=>Expect(a.ChangeCount==b.ChangeCount && a.EditorChangeCount==b.EditorChangeCount && a.SnapshotBytes==b.SnapshotBytes && a.Undo.SequenceEqual(b.Undo) && a.Redo.SequenceEqual(b.Redo),"Unexpected history change");
    private static void Expect(bool accepted,string message){if(!accepted)throw new InvalidOperationException(message);}
}
