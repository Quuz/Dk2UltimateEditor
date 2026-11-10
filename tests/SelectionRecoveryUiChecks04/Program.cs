using System.Buffers.Binary;
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
using DK2MapEditor.Core.Formats;

internal static class Program
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    [STAThread]
    private static int Main(string[] args)
    {
        if(args.Length!=1||Directory.Exists(args[0]))throw new ArgumentException("A new private fixture directory is required.");
        var directory=Path.GetFullPath(args[0]);Directory.CreateDirectory(directory);
        var app=new DK2MapEditor.App.App();app.InitializeComponent();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
        try
        {
            foreach(var language in new[]{"en","ru"})foreach(var native in new[]{false,true})Check(directory,language,native);
            Console.WriteLine("SELECTION RECOVERY UI CHECKS PASSED: 4");return 0;
        }
        catch(Exception exception){Console.WriteLine(exception);return 1;}
    }
    private static void Check(string directory,string language,bool native)
    {
        LocalizationService.SetLanguage(language);var format=native?"native":"project";
        var doc=NativeMapDocument.CreateNewMap(new("RecoveryUI","RecoveryUI",16,16,NativeFormat:native));
        if(native)doc.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="GAMEKEEPER"),new(3,3));else doc.AddLegacyRecord("Creature",new(3,3));
        doc.Paint([new(2,2)],new(4,2,1));var candidate=doc.CopySelection(new(2,2,4,4));var original=SelectionFile.Serialize(candidate);
        var firstEnd=24+(int)BinaryPrimitives.ReadUInt32LittleEndian(original.AsSpan(20));var damaged=original.ToArray();BinaryPrimitives.WriteInt32LittleEndian(damaged.AsSpan(firstEnd+4),7);
        var path=Path.Combine(directory,$"recovery-{language}-{format}.dkz");File.WriteAllBytes(path,damaged);
        var historical=original.ToArray();var extension=firstEnd+12+(int)BinaryPrimitives.ReadUInt32LittleEndian(original.AsSpan(firstEnd+8));BinaryPrimitives.WriteInt32LittleEndian(historical.AsSpan(extension+4),99);
        var historicalPath=Path.Combine(directory,$"historical-{language}-{format}.dkz");File.WriteAllBytes(historicalPath,historical);
        var shell=new MainWindow(new AppSettings(),persistSettings:false);var editor=(EditorWorkspace)shell.FindName("Editor");editor.Document=doc;editor.SetSelection(new(8,8,4,4));
        var old=doc.CopySelection(new(5,5,2,2));editor.LoadSelectionIntoBuffer(old);editor.ConfirmCopyBufferReplacement=true;
        shell.WindowStartupLocation=WindowStartupLocation.Manual;shell.Left=-10000;shell.Top=-10000;shell.Width=1500;shell.Height=1000;shell.ShowInTaskbar=false;shell.Show();
        Dictionary<string,byte[]> MapBytes()=>doc.Files.ToDictionary(file=>file.ExportPath,file=>file.Data.ToArray());
        var bytes=MapBytes();var oldBytes=SelectionFile.Serialize(old);var revision=doc.EditRevision;var undo=doc.GetHistory().Undo.Count;var counter=doc.EditorChangeCount;var dirty=doc.HasChanges;
        void ExactMap()=>Expect(doc.Files.Count==bytes.Count&&doc.Files.All(file=>file.Data.Span.SequenceEqual(bytes[file.ExportPath]))&&doc.EditRevision==revision&&doc.EditorChangeCount==counter&&doc.GetHistory().Undo.Count==undo&&doc.HasChanges==dirty,"Recovery/decline/stale guard changed map/history/counters.");
        void ExactBuffer()=>Expect(ReferenceEquals(old,editor.BufferedSelection)&&SelectionFile.Serialize(old).AsSpan().SequenceEqual(oldBytes)&&editor.ConfirmCopyBufferReplacement,"Recovery replaced old buffer or suppressed future questions without complete consent.");
        bool Dialog(SelectionRecoveryRequest request,bool accept)
        {
            Expect(request.Width==4&&request.Height==4&&request.FileName==Path.GetFileName(request.Recovery==SelectionFile.ImportRecovery.TerrainOnly?path:historicalPath),"Actual request did not identify source geometry/file.");
            var window=editor.CreateSelectionRecoveryConfirmation(request);window.WindowStartupLocation=WindowStartupLocation.Manual;window.Left=-10000;window.Top=-10000;window.ShowInTaskbar=false;window.Show();
            try
            {
                window.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
                var question=(TextBlock)window.FindName("SelectionRecoveryQuestion");var summary=(TextBlock)window.FindName("SelectionRecoverySummary");
                var yes=(Button)window.FindName("SelectionRecoveryLoad");var no=(Button)window.FindName("SelectionRecoveryCancel");
                var first=question.Text;Expect(window.IsLoaded&&yes.IsDefault&&no.IsCancel&&summary.Text.Contains(request.FileName)&&summary.Text.Contains("4 × 4"),"Loaded recovery controls/default/cancel/summary are incomplete.");
                LocalizationService.SetLanguage(language=="en"?"ru":"en");window.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
                Expect(question.Text!=first&&yes.Content as string==LocalizationService.Get("Editor.SelectionRecovery.Load"),"Open recovery dialog lost live EN/RU translation.");
                LocalizationService.SetLanguage(language);window.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
                Render(window,Path.Combine(directory,$"recovery-{request.Recovery}-{language}-{format}-{accept}.png"));
                (accept?yes:no).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Expect(window.Accepted==accept&&!window.Respond(!accept),"Actual recovery button/closed reply differs.");return window.Accepted;
            }
            finally{window.Close();}
        }
        try
        {
            var replacementCalls=0;editor.CopyBufferPrompt=_=>{replacementCalls++;return new(true);};
            editor.SelectionRecoveryPrompt=request=>Dialog(request,false);
            Expect(!editor.RequestSelectionFile(path)&&replacementCalls==0,"Declined recovery reached buffer replacement.");ExactMap();ExactBuffer();
            editor.SelectionRecoveryPrompt=request=>Dialog(request,true);editor.CopyBufferPrompt=_=>new(false,true);
            Expect(!editor.RequestSelectionFile(path),"Declined second buffer confirmation was ignored.");ExactMap();ExactBuffer();
            editor.CopyBufferPrompt=_=>new(true);
            Expect(editor.RequestSelectionFile(path)&&editor.BufferedSelection is {ObjectCount:0,ScriptCount:0}&&editor.BufferedSelection.Tiles.Tiles.SequenceEqual(candidate.Tiles.Tiles),"Accepted terrain recovery not loaded exactly.");ExactMap();editor.LoadSelectionIntoBuffer(old);
            Expect(editor.RequestSelectionFile(historicalPath)&&editor.BufferedSelection is {ObjectCount:1,ScriptCount:0},"Recovery dropped valid original compatible objects.");ExactMap();editor.LoadSelectionIntoBuffer(old);
            editor.SelectionRecoveryPrompt=_=>{var altered=damaged.ToArray();altered[^1]^=1;File.WriteAllBytes(path,altered);return true;};
            Expect(!editor.RequestSelectionFile(path),"Changed archive reused recovery consent.");ExactMap();ExactBuffer();File.WriteAllBytes(path,damaged);
            editor.SelectionRecoveryPrompt=_=>{editor.SetSelection(new(7,7,4,4));return true;};Expect(!editor.RequestSelectionFile(path),"Changed target selection reused recovery consent.");ExactMap();ExactBuffer();editor.SetSelection(new(8,8,4,4));
            var firstOld=old.Tiles.Tiles[0];editor.SelectionRecoveryPrompt=_=>{((MapTile[])old.Tiles.Tiles)[0]=new((byte)(firstOld.Terrain==6?4:6),2,1);return true;};
            Expect(!editor.RequestSelectionFile(path)&&ReferenceEquals(old,editor.BufferedSelection),"Changed retained buffer was replaced by stale consent.");((MapTile[])old.Tiles.Tiles)[0]=firstOld;ExactMap();ExactBuffer();
            editor.SelectionRecoveryPrompt=_=>
            {
                Expect(!editor.RequestSelectionFile(path)&&!editor.RequestCopy(cut:true)&&!editor.RequestLoadSelectionIntoBuffer(candidate),"Recovery allowed reentrant import/cut/buffer replacement.");return false;
            };
            Expect(!editor.RequestSelectionFile(path),"Reentrant recovery changed state.");ExactMap();ExactBuffer();
            editor.SelectionRecoveryPrompt=_=>true;editor.CopyBufferPrompt=_=>{var altered=damaged.ToArray();altered[^1]^=1;File.WriteAllBytes(path,altered);return new(true,true);};
            Expect(!editor.RequestSelectionFile(path),"Archive changed during second confirmation reused consent/suppression.");ExactMap();ExactBuffer();File.WriteAllBytes(path,damaged);
            var promptCalls=0;editor.SelectionRecoveryPrompt=_=>{promptCalls++;return true;};editor.ClearSelection();
            Expect(!editor.RequestSelectionFile(path,pasteIntoSelection:true)&&promptCalls==0,"Direct insertion without selection opened recovery or modified map.");ExactMap();ExactBuffer();editor.SetSelection(new(8,8,4,4));
            var wrong=damaged.ToArray();BinaryPrimitives.WriteInt32LittleEndian(wrong.AsSpan(4),2);var invalidPath=Path.Combine(directory,$"invalid-{language}-{format}.dkz");File.WriteAllBytes(invalidPath,wrong);
            try{editor.RequestSelectionFile(invalidPath);throw new InvalidOperationException("Invalid AREA primary header opened recovery.");}catch(InvalidDataException){}
            Expect(promptCalls==0,"Invalid primary data asked for partial recovery.");ExactMap();ExactBuffer();
            editor.SelectionRecoveryPrompt=null;editor.CopyBufferPrompt=_=>new(true);var modalObserved=false;
            shell.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,new Action(()=>
            {
                var field=typeof(EditorWorkspace).GetField("selectionRecoveryWindow",BindingFlags.Instance|BindingFlags.NonPublic)!;
                var window=(SelectionRecoveryConfirmationWindow?)field.GetValue(editor);Expect(window is {IsLoaded:true,IsVisible:true},"Actual default modal recovery route never opened.");
                modalObserved=true;window!.Left=-10000;window.Top=-10000;Render(window,Path.Combine(directory,$"recovery-default-modal-{language}-{format}.png"));
                ((Button)window.FindName("SelectionRecoveryCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }));
            Expect(!editor.RequestSelectionFile(path)&&modalObserved,"Actual default modal Cancel imported data.");ExactMap();ExactBuffer();
            var beforeTarget=doc.GetTile(8,8);doc.Paint([new(8,8)],new((byte)(candidate.Tiles.Tiles[0].Terrain==6?4:6),2,1));bytes=MapBytes();revision=doc.EditRevision;undo=doc.GetHistory().Undo.Count;counter=doc.EditorChangeCount;dirty=doc.HasChanges;
            editor.SelectionRecoveryPrompt=request=>{Expect(request.DirectInsertion,"Direct recovery reported buffer mode.");return Dialog(request,true);};editor.CopyBufferPrompt=_=>throw new InvalidOperationException("Direct insertion asked to replace retained buffer.");
            Expect(editor.RequestSelectionFile(path,pasteIntoSelection:true)&&ReferenceEquals(old,editor.BufferedSelection)&&doc.GetHistory().Undo.Count==undo+1&&doc.GetTile(8,8)==candidate.Tiles.Tiles[0],"Direct recovery did not paste terrain in exactly one Undo while retaining buffer.");
            doc.Undo();revision=doc.EditRevision;ExactMap();ExactBuffer();
            editor.SelectionRecoveryPrompt=_=>{editor.Document=null;return true;};Expect(!editor.RequestSelectionFile(path),"Closed document reused source recovery consent.");ExactMap();ExactBuffer();editor.Document=doc;editor.SetSelection(new(8,8,4,4));
            editor.SelectionRecoveryPrompt=_=>{shell.Close();return true;};Expect(!editor.RequestSelectionFile(path)&&!editor.RequestSelectionFile(path)&&!editor.RequestSelectionFile(path,pasteIntoSelection:true),"Actual closed shell retained recovery command authority.");ExactMap();ExactBuffer();
            Console.WriteLine($"PASS {language} {format} actual AREA recovery, EN/RU loaded controls and modal Cancel, exact buffer and one-Undo direct insertion, compatible objects, second consent and stale/reentrant/closed guards");
        }
        finally{editor.Document=null;shell.Close();}
    }
    private static void Render(Window window,string path)
    {
        window.UpdateLayout();var width=(int)Math.Ceiling(window.ActualWidth);var height=(int)Math.Ceiling(window.ActualHeight);
        Expect(width>300&&height>80,"Recovery render collapsed.");
        foreach(var name in new[]{"SelectionRecoveryQuestion","SelectionRecoverySummary","SelectionRecoveryLoad","SelectionRecoveryCancel"})
        {
            var control=(FrameworkElement)window.FindName(name);var origin=control.TranslatePoint(new Point(),window);
            Expect(control.IsVisible&&control.ActualWidth>0&&control.ActualHeight>0&&origin.X>=0&&origin.Y>=0&&origin.X+control.ActualWidth<=window.ActualWidth+0.5&&origin.Y+control.ActualHeight<=window.ActualHeight+0.5,$"Actual loaded recovery control is clipped: {name}.");
        }
        Console.WriteLine($"RECOVERY WINDOW LAYOUT width={width} height={height}; all actual controls wholly contained");
        var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(window);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(path);encoder.Save(output);
    }
}
