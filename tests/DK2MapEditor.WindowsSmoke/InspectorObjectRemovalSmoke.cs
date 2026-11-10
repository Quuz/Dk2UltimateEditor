using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only: running this constructs real WPF windows. Compile it, but
    // keep physical execution in the user's manual acceptance workflow.
    private static void CheckInspectorObjectRemoval(bool native, string language)
    {
        NativeMapDocument New() => NativeMapDocument.CreateNewMap(new("InspectorRemoval", "InspectorRemoval", 18, 18, NativeFormat: native));
        void Add(NativeMapDocument target, PlacedThingKind kind, int x, int y)
            => target.ApplyEditorObjectBatchInsertion(target.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(kind) with { Owner = 3 },
                new(Position: new(x, y), Flags: (ObjectBatchFlags)127)), [0]);
        var doc = New();
        foreach (var kind in Enum.GetValues<PlacedThingKind>()) for (var i = 0; i < 2; i++) Add(doc, kind, 2 + (int)kind, 3 + i);
        var workspace = new EditorWorkspace { Document = doc }; var host = new Window { Content = workspace }; ShowManagerCallerWindow(host);
        var list = Control<ListBox>(workspace, "RecordList"); var categories = Control<ComboBox>(workspace, "CategoryPicker");
        NativeRecord Record(object item) => (NativeRecord)item.GetType().GetProperty("Record")!.GetValue(item)!;
        void Command() => typeof(EditorWorkspace).GetMethod("DeleteRecord_Click", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(workspace, [workspace, new RoutedEventArgs()]);
        void Select(PlacedThingKind kind)
        {
            var selected = workspace.Document!.ObjectManagerEntries().Where(entry => entry.Thing.Kind == kind).Select(entry => entry.Record).ToArray();
            categories.SelectedItem = categories.Items.Cast<object>().Single(item => (string)item.GetType().GetProperty("ID")!.GetValue(item)! == selected[0].Category);
            Expect(list.SelectionMode == SelectionMode.Extended, "Physical inspector category cannot select a batch.");
            list.SelectedItems.Clear();
            foreach (var item in list.Items.Cast<object>())
                if (selected.Any(record => record.File == Record(item).File && record.Offset == Record(item).Offset)) list.SelectedItems.Add(item);
        }
        Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument target) => target.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes)
            => Expect(bytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Inspector changed bytes on cancellation or lost full Undo.");
        workspace.SetSelection(new(10, 10, 2, 2)); workspace.Copy();
        var buffer = workspace.BufferedSelection; var rectangle = Control<MapCanvas>(workspace, "Canvas").Selection;
        var prompts = 0;
        foreach (var kind in Enum.GetValues<PlacedThingKind>())
        {
            Select(kind); var before = Snapshot(doc); var history = doc.GetHistory(); var selected = list.SelectedItems.Count;
            workspace.InspectorObjectRemovalPresenter = window =>
            {
                prompts++; Expect(window.Request.Prompt == EditorConfirmationPrompt.ObjectRemoval && window.Request.Count == selected, "Inspector did not use manager confirmation for the complete selection.");
                Expect(((TextBlock)window.FindName("RegionRemovalMessage")).Text == LocalizationService.Get("ThingRequest.Confirm"), "Inspector question is not localized.");
                ((CheckBox)window.FindName("RegionRemovalRemember")).IsChecked = true; window.Close();
            };
            Command(); Exact(before); Expect(doc.GetHistory().Undo.Count == history.Undo.Count && doc.EditorChangeCount == history.EditorChangeCount, "Checked close committed/remembered removal.");
            workspace.InspectorObjectRemovalPresenter = window =>
            {
                prompts++; LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
                Expect(list.SelectedItems.Count == selected && window.Accept(), "Language refresh discarded the selected objects or refused current Yes.");
            };
            Command();
            Expect(doc.ObjectManagerEntries().All(entry => entry.Thing.Kind != kind) && doc.GetHistory().Undo.Count == history.Undo.Count + 1
                && doc.EditorChangeCount == history.EditorChangeCount + (uint)selected, "Inspector removal lost parent Undo or per-object counts.");
            doc.Undo(); Exact(before); LocalizationService.SetLanguage(language);
        }
        Expect(prompts == 10, "Closing checked confirmation unexpectedly remembered Yes.");
        Expect(ReferenceEquals(buffer, workspace.BufferedSelection) && Control<MapCanvas>(workspace, "Canvas").Selection == rectangle, "Inspector deletion changed copy buffer or map selection.");

        Select(PlacedThingKind.Creature); var pendingBefore = Snapshot(doc); var pendingRefused = 0;
        var grid = Control<DataGrid>(workspace, "PropertyGrid");
        grid.SelectedItem = grid.Items.Cast<object>().Single(item => ((NativeField)item.GetType().GetProperty("Field")!.GetValue(item)!).Name == (native ? "bGlobalPlayer" : "owner"));
        grid.CurrentCell = new(grid.SelectedItem, grid.Columns[1]); Expect(grid.BeginEdit(), "Pending Inspector fixture could not begin editing."); workspace.UpdateLayout();
        var input = grid.Columns[1].GetCellContent(grid.SelectedItem) as TextBox; Expect(input is not null, "Pending Inspector field has no text editor."); input!.Text = "4";
        workspace.RecordRemovalRefusalPresenter = message => { pendingRefused++; Expect(message == LocalizationService.Get("Editor.RemovalChanged"), "Pending edit refusal is not localized."); };
        workspace.InspectorObjectRemovalPresenter = _ => throw new Exception("A pending field edit authorized deletion of old row addresses.");
        Command(); workspace.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        Expect(pendingRefused == 1 && doc.ObjectManagerEntries().Count == 10, "Pending field commit deleted an old or narrowed selection."); doc.Undo(); Exact(pendingBefore);

        // Used-object No is the source assigned-ID filter, not generic detach.
        doc.EnsureScriptScope(doc.ScriptOwners().First(owner => owner.Scope == 2)); Select(PlacedThingKind.Creature);
        var usedBefore = Snapshot(doc); var assigned = doc.ObjectManagerEntries().Where(entry => entry.Thing.Kind == PlacedThingKind.Creature && entry.Identity != 0).Count();
        var stages = 0; var usedHistory = doc.GetHistory();
        workspace.InspectorObjectRemovalPresenter = window =>
        {
            stages++;
            if (window.Request.Prompt == EditorConfirmationPrompt.ObjectRemoval) Expect(window.Accept(), "First Inspector question refused Yes.");
            else { Expect(window.Request.Prompt == EditorConfirmationPrompt.UsedThingRemoval, "Used-creature question missing."); ((Button)window.FindName("RegionRemovalNo")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); }
        };
        Command(); Expect(stages == 2 && doc.ObjectManagerEntries().Count(entry => entry.Thing.Kind == PlacedThingKind.Creature) == assigned, "Used No did not preserve every assigned creature.");
        Expect(doc.GetHistory().Undo.Count == usedHistory.Undo.Count + 1, "Accepted No did not retain its source parent Undo."); doc.Undo(); Exact(usedBefore);

        Select(PlacedThingKind.Creature);
        workspace.InspectorObjectRemovalPresenter = window =>
        {
            list.SelectedItems.Clear(); Expect(!window.Accept(), "Changed Inspector selection authorized an old request."); window.Close();
        };
        Command(); Exact(usedBefore);
        Select(PlacedThingKind.Creature);
        var replacement = New(); Add(replacement, PlacedThingKind.Creature, 4, 5); var replacementBefore = Snapshot(replacement);
        workspace.InspectorObjectRemovalPresenter = window => { workspace.Document = replacement; Expect(!window.Accept(), "Old document request survived replacement."); window.Close(); };
        Command(); Exact(usedBefore); Exact(replacementBefore);

        // A mixed set must never fall through to generic deletion of its first row.
        var mixed = New(); Add(mixed, PlacedThingKind.Creature, 4, 5); mixed.AddMapRegion(MapRegionKind.Area, new(6, 7, 1, 1));
        workspace.Document = mixed; Select(PlacedThingKind.Creature); var physicalView = list.SelectedItem;
        var areaCategory = mixed.EditorRegionRemovalRecords().Single().Category;
        categories.SelectedItem = categories.Items.Cast<object>().Single(item => (string)item.GetType().GetProperty("ID")!.GetValue(item)! == areaCategory);
        var regionView = list.SelectedItem; var mixedBefore = Snapshot(mixed); var refused = 0;
        list.ItemsSource = new[] { physicalView, regionView }; list.SelectionMode = SelectionMode.Extended; list.SelectAll();
        workspace.RecordRemovalRefusalPresenter = message => { refused++; Expect(message == LocalizationService.Get("Editor.RemovalMixed"), "Mixed selection refusal is not localized."); };
        workspace.InspectorObjectRemovalPresenter = _ => throw new Exception("Mixed records entered physical confirmation.");
        Command(); Exact(mixedBefore); Expect(refused == 1, "Mixed records were silently narrowed to the primary row.");
        workspace.Document = replacement;

        // Remembered object Yes is shared with the manager and crosses documents.
        Select(PlacedThingKind.Creature);
        workspace.InspectorObjectRemovalPresenter = window => { ((CheckBox)window.FindName("RegionRemovalRemember")).IsChecked = true; Expect(window.Accept(), "Remember Yes refused."); };
        Command(); replacement.Undo();
        var manager = workspace.CreateObjectManagerWindow()!;
        manager.ConfirmationPresenter = _ => throw new Exception("Inspector and manager do not share the removal bit.");
        ((DataGrid)manager.FindName("ManagerObjects")).SelectAll(); Expect(manager.BuildRemovalPreview() && manager.ApplyRemoval(), "Remembered Inspector reply did not reach the manager."); manager.Cancel();
        var next = New(); Add(next, PlacedThingKind.Door, 4, 5); workspace.Document = next; Select(PlacedThingKind.Door);
        workspace.InspectorObjectRemovalPresenter = _ => throw new Exception("Remembered Inspector reply did not cross documents."); Command(); Expect(next.ObjectManagerEntries().Count == 0, "Cross-document default Yes did not remove the door.");
        workspace.Document = null; LocalizationService.SetLanguage(language); host.Close();
        Console.WriteLine($"PASS WPF Inspector object routing: five families, group Undo/counts, checked close, language/selection/document lifetime, used No and shared preferences {native} {language}; physical acceptance still required");
    }
}
