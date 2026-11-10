using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Human-only presentation mode. Build does not run it; no screenshots,
    // automated clicks, real memory exhaustion or installed maps are involved.
    private static int RunManualHistoryMemoryNotice(string language)
    {
        LocalizationService.SetLanguage(language);
        var root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2M10Manual-"+Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        MainWindow? shell=null;Window? controls=null;
        var application=(DK2MapEditor.App.App)Application.Current;
        var handler=typeof(DK2MapEditor.App.App).GetMethod("HistoryMemoryFailure",BindingFlags.Instance|BindingFlags.NonPublic)!
            .CreateDelegate<DispatcherUnhandledExceptionEventHandler>(application);
        try
        {
            const string level="DK2Preview_6a88240eaf814f78";
            var initial=NativeMapDocument.CreateNewMap(new(level,level,14,14));
            var path=initial.SaveCopy(Path.Combine(root,level));
            shell=new MainWindow(new AppSettings{Language=language,HistoryDepth=200,AutoSaveMinutes=0,Thumbnail=new()},persistSettings:false);
            application.MainWindow=shell;
            // Ignore only the synthetic map load report; it is not part of M10.
            shell.DocumentReportPresenter=_=>{};
            application.DispatcherUnhandledException+=handler;
            typeof(MainWindow).GetMethod("OpenMap",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(shell,[path]);
            var workspace=(EditorWorkspace)shell.FindName("Editor");
            var document=workspace.Document??throw new InvalidOperationException("Temporary map was not loaded.");
            document.Paint([new(3,3)],new(4,2,1));document.Undo(); // One retained Redo, clean map.
            var panel=new StackPanel{Margin=new(16)};
            controls=new Window{Title="M10 · ручная проверка / manual check",Width=570,Height=455,Content=panel,WindowStartupLocation=WindowStartupLocation.CenterScreen};
            panel.Children.Add(new TextBlock{TextWrapping=TextWrapping.Wrap,Text=$"Ручная проверка M10 ({language}). Открыта временная DKM, автосохранение и запись настроек выключены. Не открывайте свои карты в этом режиме. Кнопка ниже имитирует отказ подготовки Undo без расходования памяти. Это настоящий HistoryMemoryNoticeWindow приложения."});
            var status=new TextBlock{Margin=new(0,12,0,8),TextWrapping=TextWrapping.Wrap};panel.Children.Add(status);
            void ShowState(string prefix)=>status.Text=$"{prefix}\nUndo: {document.CanUndo}; Redo: {document.CanRedo}; Changed: {document.HasChanges}; counter: {document.EditorChangeCount}.";
            void Button(string caption,Action action)
            {
                var button=new Button{Content=caption,Margin=new(0,4,0,0),Padding=new(8)};
                button.Click+=(_,_)=>{try{action();}catch(Exception error){status.Text=error.ToString();Console.Error.WriteLine(error);}};panel.Children.Add(button);
            }
            Button("Показать безопасную ошибку Undo / Simulate Undo failure",()=>
            {
                var buffers=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var history=document.GetHistory();var count=document.EditorChangeCount;
                var factory=typeof(NativeMapDocument).GetField("historyBufferCopy",BindingFlags.Instance|BindingFlags.NonPublic)!;
                var prior=factory.GetValue(document);EditorHistoryMemoryException? failure=null;
                try
                {
                    factory.SetValue(document,new Func<byte[],byte[]>(_=>throw new OutOfMemoryException("Controlled M10 presentation; no actual allocation pressure.")));
                    document.Edit("Manual M10 injection",()=>throw new InvalidOperationException("Refused edit action must not execute."));
                }
                catch(EditorHistoryMemoryException error){failure=error;}
                finally{factory.SetValue(document,prior);}
                if(failure is not {IsSafeToNotify:true} || !buffers.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))
                    ||document.EditorChangeCount!=count||!document.GetHistory().Undo.SequenceEqual(history.Undo)||!document.GetHistory().Redo.SequenceEqual(history.Redo))
                    throw new InvalidOperationException("Safe failure/rollback preparation did not preserve data/history; do not accept M10.");
                shell.Activate();
                // Route through the product's actual dispatcher handler while
                // keeping its normal settings-loading startup suppressed.
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(()=>throw failure));
                ShowState("Ошибка передана штатному dispatcher handler. После «Запомнить» повторный показ может быть подавлен. Оценку вида, фокуса и клавиатуры сообщает пользователь.");
            });
            Button("Настройки истории / сброс уведомления",()=>
            {var settings=shell.CreateHistorySettingsWindow()??throw new InvalidOperationException("History settings owner is closed.");settings.Owner=shell;settings.ShowDialog();ShowState("Настройки закрыты. Сброс проверяется повторным показом ошибки.");});
            Button("Undo",()=>{if(document.CanUndo)document.Undo();ShowState("Undo.");});
            Button("Redo",()=>{if(document.CanRedo)document.Redo();ShowState("Redo.");});
            Button("Закрыть проверку / Close",()=>shell.Close());
            ShowState("Проверьте OK, Escape, крестик, «Запомнить», повтор и сброс; затем Undo/Redo.");
            var frame=new DispatcherFrame();var shellClosed=false;
            shell.Closed+=(_,_)=>{shellClosed=true;frame.Continue=false;controls.Close();};
            controls.Closed+=(_,_)=>{if(!shellClosed)shell.Close();};
            shell.Show();controls.Show();
            // The exact App instance suppresses normal startup explicitly;
            // this frame serves only the two human-controlled fixture windows.
            Dispatcher.PushFrame(frame);
            Console.WriteLine("Manual M10 presentation ended. No UI acceptance is inferred; report observations explicitly.");
            return 0;
        }
        finally
        {
            application.DispatcherUnhandledException-=handler;
            var tempBase=Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()))+Path.DirectorySeparatorChar;
            if(!root.StartsWith(tempBase,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unexpected manual fixture cleanup target.");
            if(Directory.Exists(root))Directory.Delete(root,recursive:true);
        }
    }
}
