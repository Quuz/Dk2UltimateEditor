using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared WPF checks only. Running this harness opens/constructs editor UI
    // and requires the user's separate authorization for physical UI execution.
    private static void CheckRegionRemovalConfirmation(bool native, string language)
    {
        NativeMapDocument New() => NativeMapDocument.CreateNewMap(new("RegionRemovalUI", "RegionRemovalUI", 24, 24, NativeFormat: native));
        var doc = New(); doc.AddMapRegion(MapRegionKind.Gate, new(3, 4, 1, 1)); doc.AddMapRegion(MapRegionKind.Gate, new(8, 9, 1, 1));
        if (native)
        {
            var heart = doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEGATE"), new(15, 16));
            doc.SetField(heart, heart.Fields.Single(field => field.Name == "bGateType"), "14");
        }
        var workspace = new EditorWorkspace { Document = doc };
        Expect(((ListBox)workspace.FindName("RegionPicker")).Items.Count == 2, "Keeper heart entered the logical region picker.");
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        void Command(string name) => typeof(EditorWorkspace).GetMethod(name, flags)!.Invoke(workspace, [workspace, new RoutedEventArgs()]);
        void SelectRegions(MapRegionKind kind)
        {
            var categories = (ComboBox)workspace.FindName("CategoryPicker");
            var category = native ? "GAME" + kind.ToString().ToUpperInvariant() : "Project.MAPRECTEX";
            categories.SelectedItem = categories.Items.Cast<object>().Single(item => (string)item.GetType().GetProperty("ID")!.GetValue(item)! == category);
            var list = (ListBox)workspace.FindName("RecordList"); Expect(list.SelectionMode == SelectionMode.Extended, "Region inspector did not support batch selection.");
            list.SelectedItems.Clear();
            foreach (var item in list.Items.Cast<object>())
            {
                var record = (NativeRecord)item.GetType().GetProperty("Record")!.GetValue(item)!;
                if (workspace.Document!.IsEditorRegionRemovalRecord(record) && workspace.Document.GetMapRegion(record)?.Kind == kind) list.SelectedItems.Add(item);
            }
        }
        var count = doc.EditorChangeCount; var history = doc.GetHistory().Undo.Count; var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var prompts = 0;
        workspace.RegionRemovalPresenter = window =>
        {
            prompts++; var yes = (Button)window.FindName("RegionRemovalYes"); var no = (Button)window.FindName("RegionRemovalNo"); var remember = (CheckBox)window.FindName("RegionRemovalRemember");
            Expect(yes.IsDefault && yes.IsEnabled && no.IsEnabled, "Default Yes/No controls were not initialized.");
            Expect(((TextBlock)window.FindName("RegionRemovalCount")).Text == "(2)", "The batch count was not displayed.");
            Expect(((TextBlock)window.FindName("RegionRemovalMessage")).Text == LocalizationService.Get("RegionRemoval.ConfirmGate"), "Gate question was not localized.");
            remember.IsChecked = true; Expect(!no.IsEnabled, "Remember left the nondefault No button enabled."); window.Close();
        };
        SelectRegions(MapRegionKind.Gate); Command("DeleteRecord_Click");
        Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.EditorChangeCount == count, "Closing a checked request removed regions or remembered acceptance.");
        workspace.RegionRemovalPresenter = window =>
        {
            prompts++; ((CheckBox)window.FindName("RegionRemovalRemember")).IsChecked = true;
            var alternate = language == "en" ? "ru" : "en"; LocalizationService.SetLanguage(alternate);
            Expect(window.Title == LocalizationService.Get("RegionRemoval.Title"), "Open removal title did not update with localization.");
            LocalizationService.SetLanguage(language); Expect(window.Accept(), "Actual confirmation controls rejected the current batch.");
        };
        Command("DeleteRecord_Click");
        Expect(prompts == 2 && doc.EditorRegionRemovalRecords().Count == 0 && doc.EditorChangeCount == unchecked(count + 1) && doc.GetHistory().Undo.Count == history + 1, "Controller did not make one accepted batch operation.");
        doc.Undo(); Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Actual confirmation Undo did not restore whole buffers.");
        var other = New(); other.AddMapRegion(MapRegionKind.Effect, new(3, 4, 1, 1)); workspace.Document = other; SelectRegions(MapRegionKind.Effect); Command("DeleteRecord_Click");
        Expect(prompts == 2 && other.EditorRegionRemovalRecords().Count == 0, "Suppressed default Yes opened another prompt or did not cross region kinds/documents.");
        Command("RegionRemovalReset_Click");
        other.AddMapRegion(MapRegionKind.Gate, new(3, 4, 1, 1)); other.AddMapRegion(MapRegionKind.Gate, new(8, 9, 1, 1)); SelectRegions(MapRegionKind.Gate); var unchanged = other.Files.ToDictionary(file => file, file => file.Data.ToArray());
        workspace.RegionRemovalPresenter = window =>
        {
            prompts++; var list = (ListBox)workspace.FindName("RecordList"); list.SelectedItems.Clear();
            Expect(!window.Accept(), "Changing selection allowed an old confirmation to delete it."); window.Close();
        };
        Command("DeleteRecord_Click"); Expect(prompts == 3 && unchanged.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Reset/stale-selection cancellation changed map buffers.");
        Command("RegionRemovalReset_Click");
        var usedDoc = New(); var usedArea = usedDoc.AddMapRegion(MapRegionKind.Area, new(3, 4, 1, 1));
        usedDoc.AddMapRegion(MapRegionKind.Area, new(8, 9, 1, 1));
        usedDoc.EnsureScriptScope(usedDoc.ScriptOwners().Single(owner => owner.Scope == 4 && owner.Record.Offset == usedArea.Offset));
        workspace.Document = usedDoc; SelectRegions(MapRegionKind.Area);
        var usedBefore = usedDoc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var stages = 0;
        workspace.RegionRemovalPresenter = window =>
        {
            stages++;
            if (stages == 1) { Expect(window.Accept(), "First window did not advance to used-area question."); return; }
            Expect(((TextBlock)window.FindName("RegionRemovalMessage")).Text == LocalizationService.Get("RegionRemoval.UsedAreas"), "Second question text is not localized.");
            var cancelButton = (Button)window.FindName("RegionRemovalCancel"); var noButton = (Button)window.FindName("RegionRemovalNo");
            Expect(cancelButton.Visibility == Visibility.Visible && cancelButton.IsEnabled && noButton.IsEnabled, "Used-area question lacks Yes/No/Cancel.");
            ((CheckBox)window.FindName("RegionRemovalRemember")).IsChecked = true;
            Expect(!cancelButton.IsEnabled && !noButton.IsEnabled, "Remembered default did not disable No/Cancel.");
            ((CheckBox)window.FindName("RegionRemovalRemember")).IsChecked = false;
            noButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        };
        Command("DeleteRecord_Click"); Expect(stages == 2 && usedDoc.EditorRegionRemovalRecords().Count == 1, "Two-window controller did not keep the used area on No.");
        usedDoc.Undo(); Expect(usedBefore.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Used-area No Undo lost map bytes.");
        SelectRegions(MapRegionKind.Area); stages = 0;
        workspace.RegionRemovalPresenter = window => { stages++; if (stages == 1) window.Accept(); else ((Button)window.FindName("RegionRemovalCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); };
        Command("DeleteRecord_Click"); Expect(stages == 2 && usedBefore.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Second Cancel committed a removal.");
        workspace.Document = null;
        Console.WriteLine($"REGION REMOVAL CONFIRMATION UI native={native} language={language}: actual batch controls, checked close, live localization, common Undo/count, cross-document default reply, reset and changed-selection refusal; physical keyboard/mouse/DPI still required");
    }
}
