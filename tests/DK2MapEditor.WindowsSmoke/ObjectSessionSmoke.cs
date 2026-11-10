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
    private static void CheckObjectSession(bool native, string language)
    {
        NativeMapDocument Fixture(int width = 16, int height = 14)
        {
            var result = NativeMapDocument.CreateNewMap(new("SessionUI", "SessionUI", width, height, NativeFormat: native));
            result.Paint(MapDrawing.Rectangle(new(1, 1, width - 2, height - 2), true), new(8, 3, 1));
            foreach (var kind in Enum.GetValues<PlacedThingKind>()) foreach (var index in new[] { 0, 1 })
            {
                if (width <= 7 && kind != PlacedThingKind.Creature) continue;
                var template = ObjectBatchTemplate.Default(kind) with { Owner = 3, Health = (uint)(11 + index * 11),
                    TypeID = kind == PlacedThingKind.Creature ? (uint)(5 + index) : ObjectBatchTemplate.Default(kind).TypeID };
                result.ApplyObjectBatchInsertion(result.PlanObjectBatchInsertion(template, new(Position: new(2 + (int)kind, 2 + index), Flags: (ObjectBatchFlags)127)), [0]);
            }
            return result;
        }
        var doc = Fixture(); var owner = doc.ScriptOwners().First(owner => owner.Scope == 2); var binding = doc.EnsureScriptScope(owner);
        doc.InsertScriptEvent(binding.Owner, binding.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER"));
        var workspace = new EditorWorkspace { Document = doc }; var host = new Window { Content = workspace };
        workspace.SetSelection(new(1, 1, 7, 6)); workspace.Copy(); var buffer = workspace.BufferedSelection;
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        void Exact() => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Session preference changed map bytes.");
        var manager = workspace.CreateObjectManagerWindow()!;
        T Manager<T>(string name) => (T)manager.FindName(name);
        ObjectBatchWindow OpenMultiple(int family)
        {
            manager.ConfirmationPresenter=dialog=>Expect(dialog.Accept(),"Source session modification Yes refused");
            Manager<TextBox>("ManagerSearch").Text = ""; Manager<CheckBox>("ManagerSelectionOnly").IsChecked = false;
            Manager<ComboBox>("ManagerFamily").SelectedValue = family; Manager<DataGrid>("ManagerObjects").SelectAll();
            return manager.CreateModificationWindow()!;
        }
        var window = OpenMultiple(0);
        T Field<T>(string name) => (T)window.FindName(name);
        void Click(string name) => Field<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(Field<TextBox>("BatchTypeID").Text == "1" && Field<TextBox>("BatchHealth").Text == "100", "Multi-edit copied first selected object rather than original shared template.");
        Expect(Field<CheckBox>("ModifyMaskType").IsChecked == true && Field<CheckBox>("ModifyMaskHealth").IsChecked == true
            && Field<CheckBox>("ModifyMaskCoordinates").IsChecked == false && Field<CheckBox>("BatchFlagIgnoreIncompatible").IsChecked == true
            && Field<ComboBox>("ModifyCoordinates").SelectedValue as string == "1", "Source first-run modification defaults wrong.");
        foreach (var mask in Enum.GetValues<ObjectModificationMask>().Where(mask => mask is not (ObjectModificationMask.None or ObjectModificationMask.All)))
            if (window.FindName("ModifyMask" + mask) is CheckBox check) check.IsChecked = false;
        Field<CheckBox>("ModifyMaskHealth").IsChecked = true; Field<TextBox>("BatchHealth").Text = "73";
        Field<TextBox>("BatchX").Text = "7"; Field<TextBox>("BatchY").Text = "6";
        Field<ComboBox>("ModifyCoordinates").SelectedValue = "0";
        Field<ComboBox>("ModifyCreatureBits").SelectedValue = "1"; Field<ComboBox>("ModifyHeroBits").SelectedValue = "1";
        Field<TextBox>("ModifyCreatureMask").Text = "bad mask"; Field<TextBox>("ModifyHeroMask").Text = "2147483648";
        Field<CheckBox>("BatchFlagAskOptions").IsChecked = false; Field<CheckBox>("BatchFlagPreview").IsChecked = false;
        Field<CheckBox>("BatchFlagIgnoreEnemies").IsChecked = true; Expect(!window.BuildPreview(), "Invalid retained mask silently normalized.");
        window.Cancel(); window = OpenMultiple(0);
        Expect(Field<TextBox>("BatchHealth").Text == "73" && Field<TextBox>("ModifyCreatureMask").Text == "bad mask"
            && Field<ComboBox>("ModifyCoordinates").SelectedValue as string == "0" && Field<ComboBox>("ModifyCreatureBits").SelectedValue as string == "1"
            && Field<TextBox>("ModifyHeroMask").Text == "2147483648" && Field<CheckBox>("BatchFlagAskOptions").IsChecked == false
            && Field<CheckBox>("BatchFlagPreview").IsChecked == false, "Close/reopen discarded raw templates, bit modes or warning flags.");
        Field<TextBox>("ModifyCreatureMask").Text = "4294967295"; Click("BatchPreview"); Expect(window.ModificationPreview?.Candidates.Count == 2, "Fresh source Yes selection was not applied.");
        Field<CheckBox>("ModifyIncludeScripted").IsChecked = true; Click("BatchPreview"); Field<CheckBox>("ModifyDetachScripts").IsChecked = true; Field<CheckBox>("BatchConfirmed").IsChecked = true;
        window.Cancel(); Exact();
        Manager<ComboBox>("ManagerSort").SelectedValue = (int)ObjectManagerSort.Owner; Manager<TextBox>("ManagerSearch").Text = "5";
        Manager<CheckBox>("ManagerSelectionOnly").IsChecked = true; Manager<DataGrid>("ManagerObjects").SelectAll(); manager.BuildRemovalPreview();
        manager.Cancel(); manager = workspace.CreateObjectManagerWindow()!;
        Expect(Manager<ComboBox>("ManagerSort").SelectedValue as int? == (int)ObjectManagerSort.Owner && Manager<TextBox>("ManagerSearch").Text == "5"
            && Manager<CheckBox>("ManagerSelectionOnly").IsChecked == true && Manager<DataGrid>("ManagerObjects").SelectedItems.Count == 0
            && manager.RemovalPreview is null && manager.FindName("ManagerIncludeScripted") is null && manager.FindName("ManagerConfirmed") is null,
            "Manager retained destructive state or lost display preferences.");
        window = OpenMultiple(0);
        Expect(Field<CheckBox>("ModifyIncludeScripted").IsChecked == true && Field<CheckBox>("ModifyIncludeScripted").Visibility==Visibility.Collapsed && Field<CheckBox>("ModifyDetachScripts").IsChecked != true
            && Field<CheckBox>("BatchConfirmed").IsChecked != true && window.ModificationPreview is null && Field<TextBox>("BatchX").Text == "7", "Reopened multi-edit carried old consent or lost destination.");
        var initial = Field<TextBox>("BatchHealth").Text; LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
        Expect(Field<TextBox>("BatchHealth").Text == initial && Field<ComboBox>("ModifyCoordinates").SelectedValue as string == "0", "Translation overwrote session inputs."); LocalizationService.SetLanguage(language); window.Cancel();
        window = OpenMultiple(1); Field<ComboBox>("BatchFamily").SelectedValue = "2"; Field<TextBox>("BatchDataAll").Text = "99";
        Expect(!Field<ComboBox>("ModifyCoordinates").IsEnabled && Field<ComboBox>("ModifyCoordinates").SelectedValue as string == "1", "Noncreature relative coordinates not forced."); window.Cancel();
        window = OpenMultiple(2); Expect(Field<ComboBox>("BatchFamily").SelectedValue as string == "4", "Shared source subtype did not map trap to special-item template.");
        Field<TextBox>("BatchTypeID").Text = "118"; Field<ComboBox>("BatchFamily").SelectedValue = "3"; Field<TextBox>("BatchTypeID").Text = "28"; window.Cancel();
        window = workspace.CreateObjectBatchWindow()!;
        Field<ComboBox>("BatchFamily").SelectedValue = "2"; Expect(Field<TextBox>("BatchDataAll").Text == "99", "Add and multi-edit do not share the five template drafts.");
        Expect(Field<CheckBox>("BatchFlagAskOptions").IsChecked == true && Field<CheckBox>("BatchFlagPreview").IsChecked == true
            && Field<CheckBox>("BatchFlagIgnoreEnemies").IsChecked == false, "Modification options leaked into addition options.");
        Field<ComboBox>("BatchFamily").SelectedValue = "0"; Field<TextBox>("BatchHealth").Text = "88";
        Field<ComboBox>("BatchMode").SelectedValue = "2"; Field<ComboBox>("BatchReplacement").SelectedValue = "3";
        Field<TextBox>("BatchFrequency").Text = "7"; Field<CheckBox>("BatchWholeMap").IsChecked = false;
        Field<TextBox>("BatchX").Text = "9"; Field<TextBox>("BatchY").Text = "8";
        Field<CheckBox>("BatchFlagAskOptions").IsChecked = false; Field<CheckBox>("BatchFlagIgnoreTerrain").IsChecked = true; window.Cancel();
        // Individual property editing uses that actual object's values, not global defaults,
        // and closing it cannot corrupt the shared template or global mask preferences.
        Manager<ComboBox>("ManagerFamily").SelectedValue = 0; var table = Manager<DataGrid>("ManagerObjects"); table.UnselectAll(); table.SelectedItem = table.Items[0];
        window = manager.CreateModificationWindow()!; Expect(Field<TextBox>("BatchHealth").Text == "11", "Single-record editor used shared multi template."); Field<TextBox>("BatchHealth").Text = "9999"; window.Cancel();
        window = OpenMultiple(0); Expect(Field<TextBox>("BatchHealth").Text == "88" && Field<ComboBox>("ModifyCoordinates").SelectedValue as string == "0"
            && Field<CheckBox>("ModifyMaskHealth").IsChecked == true && Field<CheckBox>("ModifyMaskType").IsChecked != true && Field<TextBox>("BatchX").IsEnabled,
            "Single record/noncreature edits overwrote shared template, hidden mask/coordinate preference, or leaked random addition mode.");
        Field<CheckBox>("ModifyIncludeScripted").IsChecked = true; Click("BatchPreview"); Field<CheckBox>("BatchConfirmed").IsChecked = true;
        Click("BatchApply"); Expect(doc.ObjectManagerEntries().Where(entry => entry.Thing.Kind == PlacedThingKind.Creature)
            .All(entry => doc.ObjectTemplateForModification(entry.Record).Health == 88), "Retained shared template not used in actual masked application."); doc.Undo(); Exact(); window.Cancel();
        // A normal language-independent full-template snapshot survives reopening.
        window = workspace.CreateObjectBatchWindow()!; Expect(Field<ComboBox>("BatchMode").SelectedValue as string == "2" && Field<TextBox>("BatchFrequency").Text == "7"
            && Field<ComboBox>("BatchReplacement").SelectedValue as string == "3" && Field<CheckBox>("BatchFlagIgnoreTerrain").IsChecked == true, "Addition mode/frequency/policy not retained."); window.Cancel();
        Manager<TextBox>("ManagerSearch").Text = "5"; Manager<CheckBox>("ManagerSelectionOnly").IsChecked = true;
        var second = Fixture(7, 7); var secondBytes = second.Files.ToDictionary(file => file, file => file.Data.ToArray()); workspace.Document = second;
        Expect(!manager.ApplyRemoval() && ReferenceEquals(buffer, workspace.BufferedSelection), "Document replacement kept destructive manager or lost clipboard.");
        manager = workspace.CreateObjectManagerWindow()!;
        Expect(Manager<TextBox>("ManagerSearch").Text == "5" && Manager<DataGrid>("ManagerObjects").SelectedItems.Count == 0 && !manager.BuildRemovalPreview(), "Map switch lost session search or reused old records.");
        window = OpenMultiple(0); Expect(Field<TextBox>("BatchHealth").Text == "88" && Field<TextBox>("BatchX").Text == "6" && Field<TextBox>("BatchY").Text == "6", "Smaller-map source coordinate clamp or template retention failed.");
        Click("BatchPreview"); Expect(window.ModificationPreview?.Candidates.Count == 2, "Retained group preferences did not build a fresh preview on the new map.");
        if (Environment.GetEnvironmentVariable("DK2_OBJECT_SESSION_UI_ARTIFACTS") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); var content = (FrameworkElement)window.Content; content.Measure(new Size(1056, 708)); content.Arrange(new Rect(0, 0, 1056, 708)); content.UpdateLayout();
            var bitmap = new RenderTargetBitmap(1056, 708, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = new FileStream(Path.Combine(directory, $"session-{language}-{native}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        window.Cancel(); manager.Cancel(); Expect(secondBytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Session reopening modified second map.");
        var isolatedWorkspace = new EditorWorkspace { Document = second }; var isolatedHost = new Window { Content = isolatedWorkspace }; var isolated = isolatedWorkspace.CreateObjectBatchWindow()!;
        ((ComboBox)isolated.FindName("BatchFamily")).SelectedValue = "0";
        Expect(((TextBox)isolated.FindName("BatchHealth")).Text == "100" && ((TextBox)isolated.FindName("BatchFrequency")).Text == "2", "Independent editor leaked another workspace session."); isolated.Cancel(); isolatedHost.Close();
        workspace.Document = null; host.Close(); Exact(); Expect(doc.GetHistory().Undo.Count == history, "Preference changes created undo entries.");
        Console.WriteLine($"PASS WPF source object-manager session retention {language} {(native ? "native" : "project")}");
    }
}
