using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only. This uses real owned modal windows and may play the
    // original warning sound; execution requires explicit UI authorization.
    private static void CheckCreatureJobsRequest(NativeMapDocument document,string language)
    {
        var workspace=new EditorWorkspace{Document=document};CreatureGlobalsWindow? parent=null;
        workspace.CreatureGlobalsPresenter=window=>{parent=window;window.Show();};
        CreatureGlobalsWindow Open()
        {
            parent?.Close();parent=null;
            workspace.Measure(new Size(1200,850));workspace.Arrange(new Rect(0,0,1200,850));workspace.UpdateLayout();
            Control<Button>(workspace,"DefinitionCreatureGlobals").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            return parent??throw new InvalidOperationException("Creature globals command did not open current parent.");
        }
        T Field<T>(string name)where T:Control=>(T)((CreatureWorkspacePageWindow)parent!.ActiveRowEditor!).FindControl("DefinitionJobs"+name)!;
        void Choose(string name,string value)
        {var list=Field<ComboBox>(name);list.SelectedItem=list.Items.Cast<object>().Single(item=>Get<string>(item,"ID")==value);}
        void MakeNonstandard()
        {
            Expect(parent!.Select("Jobs",0),"Source Jobs page unavailable.");
            foreach(var group in new[]{"Happy","Unhappy","Angry"})
            {Choose("Group",group);var list=Field<DataGrid>("Table");list.SelectAll();Field<Button>("Remove").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
            Choose("Group","Happy");Choose("Type",CreatureJobs.Allowed("Happy")[0].Name.Value);
            Field<TextBox>("Chance").Text="37";Field<TextBox>("Mood").Text="-9";Field<TextBox>("Mana").Text="11";
            Field<Button>("Add").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(Field<CheckBox>("Acknowledged").Visibility==Visibility.Collapsed,"Hosted Jobs leaked the standalone acknowledgement checkbox after editing.");
            foreach(var column in Field<DataGrid>("Table").Columns.Cast<DataGridTextColumn>())
            {
                var property=((Binding)column.Binding).Path.Path;
                Expect(column.Header is TextBlock {TextWrapping:TextWrapping.Wrap} header&&header.MaxWidth<150&&header.Text.Length>0&&!header.Text.Contains("System.Windows"),"Hosted Jobs lost readable wrapped headers after editing.");
                if(property is "Chance" or "Mood" or "Mana")Expect(((TextBlock)column.Header).Text==LocalizationService.Get("CreatureGlobals.Job"+property),"Hosted Jobs parameter header is not localized.");
            }
            Expect(((CreatureWorkspacePageWindow)parent.ActiveRowEditor!).JobWarnings.First() is{Group:"Happy",Total:37},"Pending job warning tuple mismatch.");
        }
        var bytes=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var history=document.GetHistory();
        var count=document.EditorChangeCount;var activity=document.ReadActivityStatistics()!.Encode();
        void Exact()=>Expect(bytes.All(row=>row.Key.Data.Span.SequenceEqual(row.Value))&&document.EditorChangeCount==count
            &&document.GetHistory().Undo.SequenceEqual(history.Undo)&&document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity),"Job warning/navigation/close/reset wrote live map/history/activity.");
        try
        {
            Console.WriteLine($"Jobs request {language} {(document.IsLegacyProject?"project":"native")}: edited page");
            Open();MakeNonstandard();
            Console.WriteLine("Jobs request: cancel");
            PatrolModal(()=>Expect(!parent!.Select("Light",0),"Cancel accepted Jobs navigation."),PatrolStep<CreatureJobsWarningWindow>(window=>
            {Expect(Equals(PatrolControl<Button>(window,"CreatureJobsWarningCancel").Content,language=="ru"?"Отмена":"Cancel"),"Job warning Cancel caption is missing or not localized.");Expect(PatrolControl<Button>(window,"CreatureJobsWarningContinue").IsDefault,"Continue is not the source default.");PatrolControl<Button>(window,"CreatureJobsWarningCancel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}));
            Expect(parent!.SelectedSection=="Jobs"&&!parent.JobWarningsSuppressed,"Cancel changed page/suppression.");Exact();
            Console.WriteLine("Jobs request: close with pending remember");
            PatrolModal(()=>Expect(!parent.Select("Light",0),"Close accepted Jobs navigation."),PatrolStep<CreatureJobsWarningWindow>(window=>
            {PatrolControl<CheckBox>(window,"CreatureJobsWarningRemember").IsChecked=true;Expect(!PatrolControl<Button>(window,"CreatureJobsWarningCancel").IsEnabled,"Checked remember retained Cancel.");window.Close();}));
            Expect(!parent.JobWarningsSuppressed,"Close remembered pending checkbox.");Exact();
            Console.WriteLine("Jobs request: remember and continue");
            PatrolModal(()=>Expect(parent.Select("Light",0),"Accepted Jobs request did not permit private navigation."),PatrolStep<CreatureJobsWarningWindow>(window=>
            {Expect(PatrolControl<TextBlock>(window,"CreatureJobsWarningMessage").Text.Contains("37"),"Warning lost original sum.");PatrolControl<CheckBox>(window,"CreatureJobsWarningRemember").IsChecked=true;PatrolControl<Button>(window,"CreatureJobsWarningContinue").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(window.Accepted,"Continue failed source consent.");}));
            Expect(parent.JobWarningsSuppressed,"Continue did not remember source session choice.");Exact();
            Console.WriteLine("Jobs request: reopen with suppression");
            Open();MakeNonstandard();Expect(parent!.Select("Light",0),"Reopened parent lost auto-Continue preference.");Exact();
            Console.WriteLine("Jobs request: replace map and reset suppression");
            parent.Close();workspace.Document=null;workspace.Document=document;Open();
            Expect(parent!.JobWarningsSuppressed,"Map replacement lost process preference.");
            ((Button)parent.FindName("CreatureGlobalsResetJobWarnings")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(!parent.JobWarningsSuppressed,"Reset failed or required map mutation.");MakeNonstandard();Exact();
            // Keep this test-owned parent alive while invalidating its document context.
            // The workspace command normally closes its registered parent (and its
            // modal HWND) immediately; that path cannot test a still-open stale request.
            parent.Close();parent=workspace.CreateCreatureGlobalsWindow();Expect(parent is not null,"Missing stale-owner parent.");parent!.Show();MakeNonstandard();
            Console.WriteLine("Jobs request: stale owner");
            PatrolModal(()=>Expect(!parent.Select("Light",0),"Closed owner accepted pending job warning."),PatrolStep<CreatureJobsWarningWindow>(window=>
            {PatrolControl<CheckBox>(window,"CreatureJobsWarningRemember").IsChecked=true;workspace.Document=null;Expect(!window.Continue()&&!window.Accepted,"Closed owner kept live consent.");window.Close();}));Exact();
            var independent=new EditorWorkspace{Document=document};var fresh=independent.CreateCreatureGlobalsWindow();
            Expect(fresh is not null&&!fresh.JobWarningsSuppressed,"Independent workspace inherited session suppression.");fresh!.Close();independent.Document=null;
            Console.WriteLine($"PASS {language} {(document.IsLegacyProject?"project":"native")} owned source job Continue/Cancel, remember/no-close, reopening/map reset and stale owner; physical original GUI/audio/DPI remain open");
        }
        finally{parent?.Close();workspace.Document=null;LocalizationService.SetLanguage(language);}
    }
}
