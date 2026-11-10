using System.IO;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckObjectBatch(bool native, string language)
    {
        var document = NativeMapDocument.CreateNewMap(new("BatchUI", "BatchUI", 12, 10, NativeFormat: native));
        document.Paint(MapDrawing.Rectangle(new(1, 1, 10, 8), true), new(8, 3, 1, 231));
        var workspace = new EditorWorkspace { Document = document }; var host = new Window { Content = workspace };
        workspace.SetSelection(new(0, 0, 5, 4)); workspace.Copy(); var clipboard = workspace.BufferedSelection;
        var button = Control<Button>(workspace, "ObjectBatchButton");
        Expect(button.IsEnabled && button.Content as string == LocalizationService.Get("Batch.Title"), "Batch command unavailable/untranslated.");
        var window = workspace.CreateObjectBatchWindow()!;ShowManagerCallerWindow(window);
        T Field<T>(string name) => (T)window.FindName(name);
        void Click(string name) => Field<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        void Kind(PlacedThingKind kind) => Field<ComboBox>("BatchFamily").SelectedValue = ((int)kind).ToString();
        ObjectBatchWindow.BatchRow[] Rows() => Field<DataGrid>("BatchCandidates").Items.Cast<ObjectBatchWindow.BatchRow>().ToArray();
        void Layout()
        {
            window.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);window.UpdateLayout();
        }
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); var changes = 0; document.Changed += (_, _) => changes++;
        void Exact() => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Batch UI preview/cancel/undo changed bytes.");
        Kind(PlacedThingKind.Creature); Expect(Field<TextBox>("BatchTypeID").Text == "1" && Field<TextBox>("BatchHealth").Text == "100"
            && Field<TextBox>("BatchHeroOptions").Text == "0" && Field<TextBox>("BatchObjective").Text == "19", "Source creature defaults wrong.");
        Field<TextBox>("BatchHeroOptions").Text = "2147483649"; Field<TextBox>("BatchHealth").Text = "bad input";
        Field<CheckBox>("BatchBit5_2").IsChecked = true; Expect(Field<TextBox>("BatchHeroOptions").Text == "2147483651", "Named hero flag lost unknown bits.");
        Field<CheckBox>("BatchBit5_2").IsChecked = false;
        Field<TextBox>("BatchCreatureOptions").Text = "2147483657";
        foreach (uint bit in new uint[] { 1, 4, 8, 16, 32, 64, 128 })
        {
            var box = Field<CheckBox>("BatchBit6_" + bit); var original = uint.Parse(Field<TextBox>("BatchCreatureOptions").Text);
            box.IsChecked = !box.IsChecked; Expect((uint.Parse(Field<TextBox>("BatchCreatureOptions").Text) & 0x80000000) != 0, "Named creature flag lost unknown bit.");
            box.IsChecked = !box.IsChecked; Expect(uint.Parse(Field<TextBox>("BatchCreatureOptions").Text) == original, "Named creature bit mapping wrong.");
        }
        Field<ComboBox>("BatchSuggestionObjective").SelectedValue = "23"; Expect(Field<TextBox>("BatchObjective").Text == "23", "Named objective did not update field.");
        Field<ComboBox>("BatchSuggestionObjective").SelectedValue = "19";
        Field<ComboBox>("BatchSuggestionOwner").SelectedValue = "7"; Expect(Field<TextBox>("BatchOwner").Text == "7", "Named owner did not update field.");
        Kind(PlacedThingKind.Trap); Expect(Field<TextBox>("BatchTypeID").Text == "4" && Field<TextBox>("BatchDataAll").Text == "4", "Source trap template wrong.");
        Field<ComboBox>("BatchSuggestionDataAll").SelectedValue = "0"; Expect(Field<TextBox>("BatchDataAll").Text == "0", "Trap blueprint inaccessible.");
        Field<ComboBox>("BatchSuggestionDataAll").SelectedValue = "4";
        Kind(PlacedThingKind.Door); Expect(Field<TextBox>("BatchTypeID").Text == "6" && Field<TextBox>("BatchDataAll").Text == "0", "Source door template wrong.");
        Field<ComboBox>("BatchSuggestionDataAll").SelectedValue = "2"; Expect(Field<TextBox>("BatchDataAll").Text == "2", "Door blueprint inaccessible.");
        Field<ComboBox>("BatchSuggestionDataAll").SelectedValue = "0";
        Expect(Field<ComboBox>("BatchSuggestionEnemy").Visibility == Visibility.Collapsed && Field<ComboBox>("BatchSuggestionObjective").Visibility == Visibility.Collapsed,
            "Switching category left unrelated creature suggestions visible.");
        Kind(PlacedThingKind.SpecialItem); Expect(Field<TextBox>("BatchTypeID").Text == "117" && Field<TextBox>("BatchOwner").Text == "2", "Source special template wrong.");
        Kind(PlacedThingKind.Creature); Expect(Field<TextBox>("BatchHealth").Text == "bad input" && Field<TextBox>("BatchHeroOptions").Text == "2147483649", "Independent invalid/unknown drafts lost.");
        Click("BatchPreview"); Expect(window.PreviewPlan is null && Field<TextBlock>("BatchStatus").Text == LocalizationService.Get("Batch.Failed"), "Invalid draft accepted/not localized."); Exact();
        Field<TextBox>("BatchHealth").Text = "93"; Field<TextBox>("BatchOwner").Text = "3"; Field<TextBox>("BatchX").Text = "2"; Field<TextBox>("BatchY").Text = "2";
        Field<ComboBox>("BatchMode").SelectedValue = "0"; Click("BatchPreview");
        Expect(window.PreviewPlan?.Template.HeroOptions == 2147483649 && Rows().Length == 1 && Rows()[0].Add, "Actual preview/template binding wrong.");
        Field<CheckBox>("BatchConfirmed").IsChecked = true; Expect(Field<Button>("BatchApply").IsEnabled, "Explicit confirmation did not enable Apply.");
        Rows()[0].Add = false; Expect(!Field<CheckBox>("BatchConfirmed").IsChecked!.Value && !Field<Button>("BatchApply").IsEnabled, "Candidate decision retained confirmation.");
        Click("BatchSelectAll"); Field<CheckBox>("BatchConfirmed").IsChecked = true;
        var retained = window.PreviewPlan;
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Layout();
        Expect(ReferenceEquals(window.PreviewPlan, retained) && Field<TextBox>("BatchHeroOptions").Text == "2147483649" && Rows()[0].Add
            && Field<Button>("BatchApply").IsEnabled && window.Title == LocalizationService.Get("Batch.Title"), "Live translation erased draft/preview/decisions.");
        LocalizationService.SetLanguage(language); Exact(); Expect(changes == 0, "Preview/language editing emitted changes.");
        var firstSourceCount=document.EditorChangeCount;
        Click("BatchApply"); Expect(document.EditorChangeCount==firstSourceCount+1&&changes == 1 && window.PreviewPlan is null && Field<TextBlock>("BatchStatus").Text == string.Format(LocalizationService.Get("Batch.Applied"), 1), "Batch Apply failed/not atomic.");
        document.Undo();Exact();Expect(document.EditorChangeCount==firstSourceCount,"Insertion Undo source count wrong.");
        document.Redo();Expect(document.EditorChangeCount==firstSourceCount+1,"Insertion Redo source count wrong.");document.Undo();Exact();
        foreach (var kind in Enum.GetValues<PlacedThingKind>())
        {
            Kind(kind); Field<TextBox>("BatchOwner").Text = "3"; Field<TextBox>("BatchX").Text = "2"; Field<TextBox>("BatchY").Text = "2";
            Click("BatchPreview"); Expect(Rows().Length == 1 && Rows()[0].Add, "Source family UI cannot insert on owned path.");
            Field<CheckBox>("BatchConfirmed").IsChecked = true; Click("BatchApply"); document.Undo(); Exact();
        }
        Kind(PlacedThingKind.Object); Field<TextBox>("BatchTypeID").Text = "999"; Expect(Field<ComboBox>("BatchNamedType").SelectedItem is null, "Unknown type silently replaced with named suggestion.");
        Field<ComboBox>("BatchMode").SelectedValue = "1"; Click("BatchUseSelection"); Field<CheckBox>("BatchFlagIgnoreTerrain").IsChecked = true;
        Click("BatchPreview"); Expect(Rows().Length == 20 && Rows().Count(row => !row.CanAdd) == 8, "Selection/boundary preview differs.");
        Click("BatchSelectAll"); Expect(Rows().Count(row => row.Add) == 12 && Rows().Where(row => !row.CanAdd).All(row => !row.Add), "Select all enabled locked edge.");
        var locked = Rows().First(row => !row.CanAdd); locked.Add = true; Expect(!locked.Add, "Locked row setter enabled boundary.");
        Layout(); var dataGrid = Field<DataGrid>("BatchCandidates"); var lockedCell = dataGrid.Columns[0].GetCellContent(locked);
        CheckBox? FindCheck(DependencyObject? control)
        {
            if (control is CheckBox check) return check; if (control is null) return null;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(control); index++) if (FindCheck(VisualTreeHelper.GetChild(control, index)) is { } found) return found;
            return null;
        }
        Expect(FindCheck(lockedCell) is { IsEnabled: false }, "Actual locked candidate checkbox is editable.");
        if (Environment.GetEnvironmentVariable("DK2_OBJECT_BATCH_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            CaptureManagerCallerWindow(window,artifacts,$"batch-{language}-{native}.png",
                "BatchCandidates","BatchPreview","BatchApply","BatchConfirmed","BatchSelectAll");
        }
        var rectangleSourceCount=document.EditorChangeCount;var rectangleHistory=document.GetHistory();
        Field<CheckBox>("BatchConfirmed").IsChecked=true;Click("BatchApply");
        Expect(document.EditorChangeCount==rectangleSourceCount+12&&document.GetHistory().Undo.Count==rectangleHistory.Undo.Count+1
            &&document.ObjectManagerEntries().Count==12&&Field<TextBlock>("BatchStatus").Text==string.Format(LocalizationService.Get("Batch.Applied"),12),
            "Actual twelve-object batch not counted per source object in one Undo.");
        var insertedBytes=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());Click("BatchPreview");Click("BatchSelectAll");
        Expect(Rows().Count(row=>row.Add)==12&&Rows().Where(row=>row.Add).All(row=>row.Candidate.Replacement is not null),"Actual twelve-replacement preview missing.");
        Field<CheckBox>("BatchConfirmed").IsChecked=true;Click("BatchApply");
        Expect(document.EditorChangeCount==rectangleSourceCount+36&&document.ObjectManagerEntries().Count==12
            &&document.GetHistory().Undo.Count==rectangleHistory.Undo.Count+2,"Actual replacements did not count twelve removals plus twelve insertions.");
        document.Undo();Expect(document.EditorChangeCount==rectangleSourceCount+12&&insertedBytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Replacement Undo not exact.");
        document.Undo();Exact();Expect(document.EditorChangeCount==rectangleSourceCount,"Rectangle Undo did not restore source count.");
        Click("BatchPreview");Click("BatchSelectAll");
        Field<CheckBox>("BatchConfirmed").IsChecked = true; Field<TextBox>("BatchWidth").Text = "6";
        Expect(window.PreviewPlan is null && !Field<Button>("BatchApply").IsEnabled, "Input change kept stale preview.");
        Field<ComboBox>("BatchMode").SelectedValue = "2"; Field<Slider>("BatchFrequencySlider").Value = 32;
        Expect(Field<TextBox>("BatchFrequency").Text == "32", "Slider frequency not saved to effective control.");
        Field<TextBox>("BatchFrequency").Text = "3"; Expect(Field<Slider>("BatchFrequencySlider").Value == 3, "Numeric frequency not reflected.");
        var samples = 0; Expect(window.BuildPreview(_ => samples++ % 3) && samples == 24 && Rows().Length == 8, "UI random rectangle frequency/order wrong.");
        var sampled = window.PreviewPlan; LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(window.PreviewPlan == sampled && samples == 24, "Translation resampled random positions."); LocalizationService.SetLanguage(language);
        var sampledCount=document.EditorChangeCount;var sampledHistory=document.GetHistory();
        Click("BatchSelectAll");var sampledSelected=Rows().Count(row=>row.Add);Field<CheckBox>("BatchConfirmed").IsChecked=true;Click("BatchApply");
        Expect(samples==24&&document.EditorChangeCount==sampledCount+(uint)sampledSelected
            &&document.GetHistory().Undo.Count==sampledHistory.Undo.Count+1,"Actual random Apply resampled or counted the batch as one.");
        document.Undo();Exact();Expect(document.EditorChangeCount==sampledCount,"Random insertion Undo count wrong.");
        Click("BatchPreview");Click("BatchKeepAll"); Expect(Rows().All(row => !row.Add), "Keep all treated as destructive deletion."); Exact();
        document.Paint([new(7, 7)], new(4, 2, 1)); Expect(window.PreviewPlan is null && !Field<Button>("BatchApply").IsEnabled, "External edit did not invalidate preview."); document.Undo(); Exact();
        // Actual pending generic edits must commit first and invalidate an older batch.
        Field<ComboBox>("BatchMode").SelectedValue = "0"; Field<TextBox>("BatchX").Text = "2"; Field<TextBox>("BatchY").Text = "2"; Click("BatchPreview");
        var metadata = document.Records().First(record => record.Category == (native ? "Map" : "Project.MAPPROPERTIES"));
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == metadata.Category);
        var records = Control<ListBox>(workspace, "RecordList"); records.SelectedItem = records.Items.Cast<object>().First(item => Get<NativeRecord>(item, "Record").File == metadata.File);
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        workspace.Measure(new Size(1300, 900)); workspace.Arrange(new Rect(0, 0, 1300, 900)); workspace.UpdateLayout();
        var properties = Control<DataGrid>(workspace, "PropertyGrid"); properties.SelectedItem = properties.Items.Cast<object>().First(item => Get<NativeField>(item, "Field").Name == (native ? "wstrMainName" : "Name"));
        properties.CurrentCell = new DataGridCellInfo(properties.SelectedItem, properties.Columns[1]); properties.BeginEdit(); properties.UpdateLayout();
        var editing = properties.Columns[1].GetCellContent(properties.SelectedItem) as TextBox; Expect(editing is not null, "Batch pending-property fixture did not enter actual DataGrid."); editing!.Text = "PendingBatch";
        Field<CheckBox>("BatchConfirmed").IsChecked = true; Expect(!window.ApplyPreview() && metadata.Read(metadata.Fields.Single(field => field.Name == (native ? "wstrMainName" : "Name"))) == "PendingBatch",
            "Batch overwrote or failed to commit a pending generic edit.");
        document.Undo(); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Background); Exact();
        Click("BatchCancel"); Expect(!window.BuildPreview() && !window.ApplyPreview(), "Canceled batch retained apply authority."); Exact();
        window = workspace.CreateObjectBatchWindow()!; Kind(PlacedThingKind.Creature);
        Expect(Field<TextBox>("BatchHeroOptions").Text == "2147483649" && Field<TextBox>("BatchFrequency").Text == "3", "Session drafts/frequency lost on reopen.");
        workspace.Document = null; Expect(!window.BuildPreview() && !window.ApplyPreview() && !button.IsEnabled && workspace.CreateObjectBatchWindow() is null, "Closed workspace retained batch window/command.");
        Expect(workspace.BufferedSelection == clipboard, "Batch workflow replaced selection clipboard."); host.Close(); Exact();
        if (!native) CheckHistoricalBatchWindow(document);
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} object batch: five templates, actual preview/checkboxes/apply, selection, random slider, live language, one undo, cancellation and stale/lifetime guards");
    }
    private static void CheckHistoricalBatchWindow(NativeMapDocument source)
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2MapEditor.HistoricalBatchUI", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = source.SaveCopy(Path.Combine(directory, "basis")); var container = LegacySectionFile.Read(path);
            var historicalPath = Path.Combine(directory, "old.dkm"); File.WriteAllBytes(historicalPath, LegacySectionFile.Create(container.Sections.Select(section => section.Signature == NativeMapDocument.LegacyThings
                ? new(section.Signature, 3, ReadOnlyMemory<byte>.Empty) : section)).ToArray());
            var historical = NativeMapDocument.Load(historicalPath); var before = historical.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var workspace = new EditorWorkspace { Document = historical }; var window = workspace.CreateObjectBatchWindow()!;
            T Field<T>(string name) => (T)window.FindName(name);
            Field<ComboBox>("BatchFamily").SelectedValue = "0"; Field<TextBox>("BatchOwner").Text = "3"; Field<TextBox>("BatchX").Text = "2"; Field<TextBox>("BatchY").Text = "2";
            Expect(Field<CheckBox>("BatchUpgrade").Visibility == Visibility.Visible, "Historical section upgrade is not exposed.");
            Expect(window.BuildPreview(), "Historical placement preview failed."); Field<CheckBox>("BatchConfirmed").IsChecked = true;
            Expect(!window.ApplyPreview() && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Historical batch silently discarded template fields or upgraded without consent.");
            Field<CheckBox>("BatchUpgrade").IsChecked = true; Expect(window.BuildPreview(), "Confirmed historical preview failed.");
            Field<CheckBox>("BatchConfirmed").IsChecked = true; Expect(window.ApplyPreview(), "Confirmed historical template could not be applied.");
            Expect(historical.Records().Single(record => record.File.Signature == NativeMapDocument.LegacyThings).Category == "Project.MAPTHING", "Historical batch did not retain complete template.");
            historical.Undo(); Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Historical upgrade/insertion did not undo together.");
            workspace.Document = null;
        }
        finally { Directory.Delete(directory, true); }
    }
}
