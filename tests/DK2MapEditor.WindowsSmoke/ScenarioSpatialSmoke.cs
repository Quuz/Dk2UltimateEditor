using System.Buffers.Binary;
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
    private static void RenderSpatial(Window window, string state, bool native, string language, string directory)
    {
        var output = Environment.GetEnvironmentVariable("DK2_SPATIAL_UI_ARTIFACTS") ?? directory;
        Directory.CreateDirectory(output);
        foreach (var width in new[] { 540, 700 })
        {
            var height = width == 540 ? 420 : 580;
            var content = (FrameworkElement)window.Content; content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout();
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = new FileStream(Path.Combine(output, $"spatial-{state}-{language}-{native}-{width}.png"), FileMode.CreateNew); encoder.Save(file);
        }
    }

    private static void CheckScenarioSpatial(bool native, string language, string directory)
    {
        foreach (var name in new[] { "ACTION_CHANGESQUARE", "ACTION_CHANGEOWNER", "ACTION_DESTROYGATE" })
        {
            var doc = NativeMapDocument.CreateNewMap(new("SpatialUI", "SpatialUI", 20, 20, NativeFormat: native)); doc.SetHistoryDepth(65535);
            var area = doc.AddMapRegion(MapRegionKind.Area, new(3, 5, 8, 6)); doc.SetField(area, EntityReferences.Identity(area)!.Field, "13");
            var gate = doc.AddMapRegion(MapRegionKind.Gate, new(7, 8, 1, 1));
            if (!native) doc.SetField(gate, EntityReferences.Identity(gate)!.Field, "27");
            var record = doc.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(item => item.Name == name));
            if (name != "ACTION_DESTROYGATE")
            {
                doc.SetField(record, record.Fields.Single(item => item.Name == (native ? "Param5" : "param5")), "0");
                doc.SetField(record, record.Fields.Single(item => item.Name == (native ? "Param7" : "param7")), "1000");
            }
            var workspace = new EditorWorkspace { Document = doc }; Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
            var categories = Control<ComboBox>(workspace, "CategoryPicker"); categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
            var records = Control<ListBox>(workspace, "RecordList"); records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").Offset == record.Offset);
            workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout();
            ScenarioParameterWindow? parent = null; workspace.ScenarioParameterPresenter = window => parent = window;
            var command = Control<Button>(workspace, "ScenarioParameterButton"); Expect(command.IsEnabled, "Spatial common command disabled.");
            command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(parent is not null, "Spatial toolbar did not open its page.");
            T Field<T>(string id) => (T)parent!.FindName(id);
            var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
            void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(bytes.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Spatial UI changed live or unrelated bytes.");
            void Coordinate(Dictionary<NativeFile, byte[]> bytes, int number, int value)
            {
                if (native) BinaryPrimitives.WriteUInt16LittleEndian(bytes[record.File].AsSpan(record.Offset + (number == 5 ? 12 : 14)), (ushort)value);
                else BinaryPrimitives.WriteUInt32LittleEndian(bytes[record.File].AsSpan(record.Offset + 12 + number * 4), (uint)value);
            }
            Expect(parent!.ApplyDraft() && doc.GetHistory().Undo.Count == history, "Opening imported spatial operands repaired bytes/history."); Exact(before);
            RenderSpatial(parent, name + "-imported", native, language, directory);
            var expected = before.ToDictionary(item => item.Key, item => item.Value.ToArray());
            if (name == "ACTION_DESTROYGATE")
            {
                var picker = Field<ComboBox>("ScenarioSpatialGate");
                string? GateKey(object item) => (string?)item.GetType().GetProperty("Key")!.GetValue(item);
                Expect(GateKey(picker.SelectedItem) is null && Get<string>(picker.SelectedItem, "Label").Contains(LocalizationService.Get("ScenarioSpatial.UnknownGate")), "Unknown gate was not retained/explained.");
                picker.SelectedItem = picker.Items.Cast<object>().Single(item => GateKey(item) == doc.ScenarioGateChoices().Single().Key);
                if (native) { Coordinate(expected, 5, 8); Coordinate(expected, 7, 9); }
                else BinaryPrimitives.WriteUInt32LittleEndian(expected[record.File].AsSpan(record.Offset + 32), 27);
                var selectedKey = Get<string>(picker.SelectedItem, "Key"); LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
                Expect(Get<string>(picker.SelectedItem, "Key") == selectedKey && parent.HasPendingInputs, "Translation lost private gate selection."); LocalizationService.SetLanguage(language);
            }
            else
            {
                Expect(Field<TextBox>("ScenarioParam5").Text == "-1" && Field<TextBox>("ScenarioParam7").Text == "999", "Coordinates did not show original stored-minus-one values.");
                Field<TextBox>("ScenarioParam5").Text = "1000"; Expect(!parent.ApplyDraft(), "Spatial display accepted four-digit coordinates."); parent.ResetDraft(); Exact(before);
                var areas = Field<ComboBox>("ScenarioSpatialArea"); areas.SelectedItem = areas.Items.Cast<object>().Single(item => Get<uint>(item, "ID") == 13);
                Expect(Field<TextBox>("ScenarioParam5").Text == "3" && Field<TextBox>("ScenarioParam7").Text == "5", "Area did not choose actual top-left.");
                var canvas = Control<MapCanvas>(workspace, "Canvas"); var selection = canvas.Selection;
                Field<Button>("ScenarioSpatialPoint").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(canvas.IsPickingRegion, "Spatial point did not open actual canvas picker.");
                canvas.CompleteRegionPick(new(6, 7), new(6, 7)); Expect(!canvas.IsPickingRegion && canvas.Selection == selection, "Spatial pick retained authority or changed selection.");
                Expect(Field<TextBox>("ScenarioParam5").Text == "6" && Field<TextBox>("ScenarioParam7").Text == "7", "Point displayed stored rather than canvas coordinates."); Exact(before);
                LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
                Expect(Field<TextBox>("ScenarioParam5").Text == "6" && Field<TextBox>("ScenarioParam7").Text == "7", "Translation lost pending point.");
                LocalizationService.SetLanguage(language); Coordinate(expected, 5, 7); Coordinate(expected, 7, 8);
            }
            RenderSpatial(parent, name + "-selected", native, language, directory); Exact(before);
            Expect(parent.ApplyDraft(), "Spatial composed page could not Apply."); Exact(expected); Expect(doc.GetHistory().Undo.Count == history + 1, "Spatial page did not commit atomically.");
            workspace.Undo(); Exact(before); workspace.Redo(); Exact(expected); workspace.Undo(); parent.ResetDraft(); Exact(before);
            if (name != "ACTION_DESTROYGATE")
            {
                var canvas = Control<MapCanvas>(workspace, "Canvas");
                Field<Button>("ScenarioSpatialPoint").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); parent.ResetDraft(); Expect(!canvas.IsPickingRegion, "Reload retained spatial canvas authority.");
                Field<Button>("ScenarioSpatialPoint").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); parent.Close(); Expect(!canvas.IsPickingRegion && !parent.ApplySpatialPoint(new(1, 1)), "Cancel retained point authority.");
            }
            else parent.Close();
            Expect(!parent.ApplyDraft(), "Closed spatial page could Apply."); Exact(before);
            var late = new ScenarioParameterWindow(doc, record, () => true, () => { doc.Paint([new(1, 1)], new(1, 2, 3, 4)); doc.Undo(); return true; });
            Expect(!late.ApplyDraft(), "Owner changed during generic commit accepted spatial parameters."); late.Close(); Exact(before);
            if (name == "ACTION_DESTROYGATE")
            {
                var type = LegacyEventSchemas.All.Single(item => item.Name == name);
                var context = doc.CreateScenarioCreationContext();
                var creation = new ScenarioCreationWindow(doc, context, 0xD6, () => true, () => true, _ => { }, type);
                Expect(creation.Next(), "Private gate creation did not open.");
                Expect(((ComboBox)creation.ParameterEditor!.FindName("ScenarioSpatialGate")).SelectedIndex == 0 && creation.ParameterEditor.HasPendingInputs, "Original missing temporary gate did not offer first real gate.");
                creation.Close(); Exact(before);
                creation = new(doc, context, 0xD6, () => true, () => true, _ => { }, type);
                Expect(creation.Next() && creation.Finish(), "Private gate creation did not Finish.");
                var created = ScriptGraph.Build(doc).Events[creation.CreatedEventID].Record;
                var gateDraft = doc.CreateScenarioParameterDraft(created);
                Expect(doc.SelectedScenarioGate(gateDraft) == doc.ScenarioGateChoices().Single().Key && doc.GetHistory().Undo.Count == history + 1, "Gate creation wrote a fake ID/coordinates or split history.");
                workspace.Undo(); Exact(before);
                var empty = NativeMapDocument.CreateNewMap(new("NoGates", "NoGates", 20, 20, NativeFormat: native));
                var noGate = empty.AddScenarioEvent(0xD6, new(0, 0), type);
                var blocked = new ScenarioParameterWindow(empty, noGate, () => true, () => true);
                Expect(!blocked.ApplyDraft() && ((TextBlock)blocked.FindName("ScenarioPageStatus")).Text == LocalizationService.Get("ScenarioPage.RequiresGate"), "Missing gate did not disable/explain page.");
                blocked.Close();
            }
            workspace.EndScenarioNamesLifetime();
            Console.WriteLine($"PASS {language} {(native ? "native" : "project")} spatial {name}, actual toolbar, private selection, coordinates, translation, atomic Undo and lifetime");
        }
        CheckScenarioRectangleUI(native, language, directory);
    }

    private static void CheckScenarioRectangleUI(bool native, string language, string directory)
    {
        var doc = NativeMapDocument.CreateNewMap(new("RectangleUI", "RectangleUI", 20, 20, NativeFormat: native)); doc.SetHistoryDepth(65535);
        var workspace = new EditorWorkspace { Document = doc }; Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var templates = Control<ComboBox>(workspace, "TemplatePicker");
        templates.SelectedItem = templates.Items.Cast<object>().Single(item => native ? Get<RecordTemplate>(item, "Template").Structure == "Action" : Get<string>(item, "LegacyKind") == "Action");
        var add = ((WrapPanel)templates.Parent).Children.OfType<Button>().Single(button => button.Content?.ToString() == LocalizationService.Get("Editor.AddRecord"));
        ScenarioCreationWindow? window = null; workspace.ScenarioCreationPresenter = created => window = created;
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact() => Expect(before.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Rectangle edited live bytes before Finish/after Undo.");
        void Open()
        {
            add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(window is not null, "Rectangle ordinary Add did not open wizard.");
            var types = (ComboBox)window!.FindName("ScenarioWizardTypes"); types.SelectedItem = types.Items.Cast<object>().Single(item => Get<LegacyEventSchemas.EventType>(item, "Type").Name == "ACTION_CHANGESQUARE");
            Expect(window.Next(), "Rectangle page could not open.");
            var terrain=(ComboBox)window.ParameterEditor!.FindName("ScenarioParam1");
            terrain.SelectedItem=terrain.Items.Cast<object>().Single(item=>Get<string>(item,"Value")=="8");
        }
        Open();
        ((CheckBox)window!.FindName("ScenarioWizardRectangle")).IsChecked = true;
        Expect(!((TextBox)window.ParameterEditor!.FindName("ScenarioParam5")).IsEnabled && ((ComboBox)window.ParameterEditor.FindName("ScenarioParam2")).IsEnabled, "Rectangle disabled owner or left point inputs enabled.");
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var selection = canvas.Selection;
        ((Button)window.FindName("ScenarioWizardPickRectangle")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(canvas.IsPickingRegion, "Rectangle did not use real map canvas.");
        canvas.CompleteRegionPick(new(4, 7), new(3, 5));
        Expect(!canvas.IsPickingRegion && canvas.Selection == selection && ((TextBox)window.ParameterEditor.FindName("ScenarioParam5")).Text == "3", "Rectangle normalization/canvas cleanup failed."); Exact();
        RenderSpatial(window, "rectangle-selected", native, language, directory);
        Expect(window.Back() && window.Next() && ((CheckBox)window.FindName("ScenarioWizardRectangle")).IsChecked == true, "Rectangle lost Back/Next context.");
        window.Close(); Exact(); Open(); Expect(((CheckBox)window!.FindName("ScenarioWizardRectangle")).IsChecked == true, "Cancel lost submitted rectangle context.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(((CheckBox)window.FindName("ScenarioWizardRectangle")).IsChecked == true, "Translation lost rectangle."); LocalizationService.SetLanguage(language);
        var history = doc.GetHistory().Undo.Count; Expect(window.Finish(), "Rectangle Finish failed."); var graph = ScriptGraph.Build(doc);
        Expect(graph.Events.Count == 6 && doc.GetHistory().Undo.Count == history + 1, "Rectangle Finish did not make one six-event transaction.");
        var ordered = graph.Events.Values.Single(item => !graph.Events.Values.Any(other => other.Edges.Any(edge => edge.Target == item.ID)));
        for (var index = 0; index < 6; index++)
        {
            var record = ordered.Record;
            Expect(record.Read(record.Fields.Single(field => field.Name == (native ? "Param5" : "param5"))) == (4 + index % 2).ToString()
                && record.Read(record.Fields.Single(field => field.Name == (native ? "Param7" : "param7"))) == (6 + index / 2).ToString(), "Actual wizard rectangle order differs from source.");
            if (index != 5) ordered = graph.Events[ordered.Edges.Single().Target];
            else Expect(ordered.ID == window.CreatedEventID, "Wizard selected first instead of last expanded action.");
        }
        var after = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); workspace.Undo(); Exact(); workspace.Redo(); Expect(after.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Rectangle Redo lost bytes."); workspace.Undo(); Exact();
        Open(); Expect(((CheckBox)window!.FindName("ScenarioWizardRectangle")).IsChecked != true && ((TextBox)window.ParameterEditor!.FindName("ScenarioParam5")).Text == "3", "Finish retained rectangle mode or last cell."); window.Close();
        var type = LegacyEventSchemas.All.Single(item => item.Name == "ACTION_CHANGESQUARE");
        ScenarioCreationWindow? late = null;
        late = new(doc, doc.CreateScenarioCreationContext(), 0xD6, () => true, () => { ((CheckBox)late!.FindName("ScenarioWizardRectangle")).IsChecked = false; return true; }, _ => throw new InvalidOperationException("Late rectangle change committed."), type);
        Expect(late.Next(), "Late rectangle setup failed."); ((CheckBox)late.FindName("ScenarioWizardRectangle")).IsChecked = true;
        Expect(late.AcceptRectangle(new(2, 3, 2, 2)) && !late.Finish(), "Rectangle changed during generic commit was accepted."); late.Close(); Exact();
        workspace.EndScenarioNamesLifetime();
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} creation rectangle, actual Add/canvas/Back/Next/Cancel/Finish, x-first chain, reset, translation and atomic Undo");
    }
}
