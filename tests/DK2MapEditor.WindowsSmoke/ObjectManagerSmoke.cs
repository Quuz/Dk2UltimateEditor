using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckObjectManager(bool native, string language)
    {
        var doc = NativeMapDocument.CreateNewMap(new("ManagerUI", "ManagerUI", 12, 10, NativeFormat: native));
        foreach (var kind in Enum.GetValues<PlacedThingKind>()) foreach (var y in new[] { 2, 3 })
        {
            var template = ObjectBatchTemplate.Default(kind) with { Owner = 3 };
            doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(template, new(Position: new(1 + (int)kind, y), Flags: (ObjectBatchFlags)127)), [0]);
        }
        var sourceOwner = doc.ScriptOwners().First(owner => owner.Scope == 2);
        var root = doc.EnsureScriptScope(sourceOwner); doc.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER"));
        var workspace = new EditorWorkspace { Document = doc }; var host = new Window { Content = workspace };
        workspace.SetSelection(new(1, 2, 5, 1)); workspace.Copy(); var buffer = workspace.BufferedSelection;
        var button = Control<Button>(workspace, "ObjectManagerButton"); Expect(button.IsEnabled && button.Content as string == LocalizationService.Get("Manager.Title"), "Manager command unavailable/untranslated.");
        var window = workspace.CreateObjectManagerWindow()!;
        var removeUsed=false;var translatedInRequest=false;
        window.ConfirmationPresenter=requestWindow=>
        {
            if(!translatedInRequest){translatedInRequest=true;LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(((DataGrid)window.FindName("ManagerObjects")).SelectedItems.Count==10,"Request translation lost selection");}
            if(requestWindow.Request.Prompt==EditorConfirmationPrompt.ObjectRemoval || removeUsed)Expect(requestWindow.Accept(),"Source manager Yes refused");
            else ((Button)requestWindow.FindName("RegionRemovalNo")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        };
        T Field<T>(string name) => (T)window.FindName(name);
        void Click(string name) => Field<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        ShowManagerCallerWindow(window);
        var table = Field<DataGrid>("ManagerObjects");
        ObjectManagerWindow.ManagerRow[] Rows() => table.Items.Cast<ObjectManagerWindow.ManagerRow>().ToArray();
        void Layout()
        {
            window.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);window.UpdateLayout();
        }
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var changes = 0; doc.Changed += (_, _) => changes++;
        void Exact() => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Manager preview/filter/undo changed bytes.");
        Expect(Field<CheckBox>("ManagerSelectionOnly").IsChecked==true&&Rows().Length == 1 && Rows().All(row => row.Entry.Thing.Kind == PlacedThingKind.Creature), "Initial source selected-area creature list wrong.");
        Click("ManagerPreview"); Expect(window.RemovalPreview is null && Field<TextBlock>("ManagerStatus").Text == LocalizationService.Get("Manager.NoSelection"), "Empty manager selection not validated/localized.");
        Field<ComboBox>("ManagerFamily").SelectedValue = 1; Expect(Rows().Length == 2 && Rows()[0].Entry.Thing.Kind == PlacedThingKind.Door, "Source selected-area door/trap family order wrong.");
        Field<ComboBox>("ManagerFamily").SelectedValue = 2; Expect(Rows().Length == 2 && Rows()[0].Entry.Thing.Kind == PlacedThingKind.SpecialItem, "Source selected-area special/object family order wrong.");
        Field<ComboBox>("ManagerFamily").SelectedValue = 3;
        Expect(table.Columns[0] is DataGridTemplateColumn && Rows().All(row => row.Icon.IsFrozen && row.Icon.PixelWidth == 32
            && ReferenceEquals(row.Icon, OriginalObjectImages.For(row.Entry.Thing.Kind, row.ID, ValueCatalogues.GameVersion(doc)))), "Actual manager row images lost source identities/sizes.");
        Field<CheckBox>("ManagerSelectionOnly").IsChecked=false;
        foreach (var method in Enum.GetValues<ObjectManagerSort>())
        {
            Field<ComboBox>("ManagerSort").SelectedValue = (int)method; Expect(Rows().Length == 10, "Sort lost physical entries.");
            Expect(Rows().Select(row => (row.Entry.Record.File, row.Entry.Record.Offset)).SequenceEqual(ObjectManagerOrdering.Sort(doc.ObjectManagerEntries(), method, language, ValueCatalogues.GameVersion(doc))
                .Select(entry => (entry.Record.File, entry.Record.Offset))), "Actual manager sorting not applied.");
        }
        Field<CheckBox>("ManagerSelectionOnly").IsChecked = true; Expect(Rows().Length == 5 && Rows().All(row => row.Entry.Thing.Position.Y == 2), "Map rectangle filter wrong.");
        Field<TextBox>("ManagerSearch").Text = Rows().First(row => row.Entry.Thing.Kind == PlacedThingKind.Creature).Entry.Name("ru", ValueCatalogues.GameVersion(doc));
        Expect(Rows().Length == 1 && Rows()[0].Entry.Thing.Kind == PlacedThingKind.Creature, "Cross-language catalogue search wrong.");
        Field<TextBox>("ManagerSearch").Text = ""; Field<CheckBox>("ManagerSelectionOnly").IsChecked = false;
        Click("ManagerSelectAll"); Expect(table.SelectedItems.Count == 10, "Select all visible not functional."); Click("ManagerPreview");
        Expect(window.RemovalPreview?.Entries.Count == 10 && window.RemovalPreview.ScriptedCount == 1 && Field<TextBlock>("ManagerSummary").Text.Contains("10"), "Removal preview count wrong.");
        Expect(Field<Button>("ManagerApply").IsEnabled && window.FindName("ManagerConfirmed") is null && window.FindName("ManagerIncludeScripted") is null,"Preview must open source questions without obsolete consent checkboxes");
        var retained = window.RemovalPreview; LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Layout();
        Expect(window.RemovalPreview == retained && table.SelectedItems.Count == 10 && Field<Button>("ManagerApply").IsEnabled
            && window.Title == LocalizationService.Get("Manager.Title") && Rows().All(row => !row.Kind.StartsWith("ProjectKind.", StringComparison.Ordinal)), "Language change lost preview/selection or untranslated rows.");
        LocalizationService.SetLanguage(language); Layout(); Exact(); Expect(changes == 0, "Read-only manager emitted map changes.");
        if (Environment.GetEnvironmentVariable("DK2_OBJECT_MANAGER_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            CaptureManagerCallerWindow(window,artifacts,$"manager-{language}-{native}.png",
                "ManagerObjects","ManagerPreview","ManagerApply","ManagerResetRequests");
        }
        var noRemoved=doc.ObjectManagerEntries().Count(entry=>entry.Identity==0);
        var removalSourceCount=doc.EditorChangeCount;var removalModernCount=doc.GetHistory().ChangeCount;
        Click("ManagerApply"); Expect(doc.EditorChangeCount==removalSourceCount+noRemoved&&doc.GetHistory().ChangeCount==removalModernCount+1&&changes == 1 && doc.ObjectManagerEntries().Count == 10-noRemoved && doc.ObjectManagerEntries().Count(entry=>entry.Scripted)==1
            && window.RemovalPreview is null && Field<TextBlock>("ManagerStatus").Text == string.Format(LocalizationService.Get("Manager.Applied"), noRemoved), "Source No must protect every assigned identity and retain a parent Undo even when all skipped");
        doc.Undo();Exact();Expect(doc.EditorChangeCount==removalSourceCount,"Removal Undo did not restore source count.");
        doc.Redo();Expect(doc.EditorChangeCount==removalSourceCount+noRemoved,"Removal Redo did not restore source count.");doc.Undo();Exact();
        removeUsed=true;Click("ManagerSelectAll"); Click("ManagerPreview");
        Click("ManagerApply"); Expect(doc.ObjectManagerEntries().Count == 0 && ScriptGraph.Build(doc).Events.Count == 1
            &&doc.EditorChangeCount==removalSourceCount+10,"Included script-bound removal lost graph or per-object count.");doc.Undo();Exact();
        Expect(doc.EditorChangeCount==removalSourceCount,"Included removal Undo count wrong.");
        LocalizationService.SetLanguage(language);
        Click("ManagerSelectAll"); Click("ManagerPreview");
        Click("ManagerSelectNone"); Expect(window.RemovalPreview is null && !Field<Button>("ManagerApply").IsEnabled, "Selection change kept destructive stale preview.");
        Click("ManagerSelectAll"); Click("ManagerPreview");
        doc.Paint([new(7, 7)], new(8, 3, 1)); Expect(window.RemovalPreview is null && table.SelectedItems.Count == 0 && !Field<Button>("ManagerApply").IsEnabled, "External edit retained stale selection/confirmation."); doc.Undo(); Exact();
        // Pending generic data commits must invalidate removal before its old preview can apply.
        Click("ManagerSelectAll"); Click("ManagerPreview");
        var metadata = doc.Records().First(record => record.Category == (native ? "Map" : "Project.MAPPROPERTIES"));
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == metadata.Category);
        var records = Control<ListBox>(workspace, "RecordList"); records.SelectedItem = records.Items.Cast<object>().First(item => Get<NativeRecord>(item, "Record").File == metadata.File);
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        workspace.Measure(new Size(1080, 760)); workspace.Arrange(new Rect(0, 0, 1080, 760)); workspace.UpdateLayout();
        var grid = Control<DataGrid>(workspace, "PropertyGrid"); grid.SelectedItem = grid.Items.Cast<object>().First(item => Get<NativeField>(item, "Field").Name == (native ? "wstrMainName" : "Name"));
        grid.CurrentCell = new(grid.SelectedItem, grid.Columns[1]); grid.BeginEdit(); workspace.UpdateLayout();
        var editing = grid.Columns[1].GetCellContent(grid.SelectedItem) as TextBox; Expect(editing is not null, "Manager pending-property fixture did not enter actual DataGrid."); editing!.Text = "PendingManager";
        Expect(!window.ApplyRemoval() && doc.ObjectManagerEntries().Count == 10 && window.RemovalPreview is null, "Pending generic edit allowed stale removal.");
        doc.Undo(); Exact();
        Expect(ReferenceEquals(buffer, workspace.BufferedSelection), "Manager altered copy buffer.");
        Click("ManagerSelectAll"); Click("ManagerPreview");
        var newer = workspace.CreateObjectManagerWindow()!; Expect(window.RemovalPreview is null && !window.ApplyRemoval(), "Replaced manager callback still active.");
        newer.Cancel(); Exact();
        var closing = workspace.CreateObjectManagerWindow()!; var closingTable = (DataGrid)closing.FindName("ManagerObjects"); closingTable.SelectAll(); closing.BuildRemovalPreview();
        workspace.Document = null; Expect(closing.RemovalPreview is null && !closing.ApplyRemoval() && !button.IsEnabled && workspace.CreateObjectManagerWindow() is null, "Closed document retained live deletion callback.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); LocalizationService.SetLanguage(language); Exact(); host.Close();
        Console.WriteLine($"PASS WPF object manager removal/sorting {language} {(native ? "native" : "project")}");
        CheckUnderCursorRemoval(native,language);
        CheckManagerCategoryScope(native,language);
        CheckInspectorObjectRemoval(native,language);
    }
}
