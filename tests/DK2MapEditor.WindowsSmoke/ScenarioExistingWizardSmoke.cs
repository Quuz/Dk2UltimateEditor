using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckExistingWizard(bool native, string language, string directory)
    {
        var doc = NativeMapDocument.CreateNewMap(new("ExistingUI", "ExistingUI", 20, 20, NativeFormat: native)); doc.SetHistoryDepth(65535);
        var condition = doc.AddScenarioEvent(0xD5, new(0, 0)); var conditionID = ScriptGraph.ID(condition);
        var say = doc.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(type => type.Name == "ACTION_SAY")); var sayID = ScriptGraph.ID(say);
        var cell = doc.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(type => type.Name == "ACTION_CHANGESQUARE")); var cellID = ScriptGraph.ID(cell);
        var destroy = doc.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(type => type.Name == "ACTION_DESTROYGATE")); var destroyID = ScriptGraph.ID(destroy);
        var spawn = doc.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(type => type.Name == "ACTION_ADDCREATURE")); var spawnID = ScriptGraph.ID(spawn);
        doc.AddMapRegion(MapRegionKind.Gate, new(3, 4, 1, 1)); doc.AddMapRegion(MapRegionKind.Area, new(2, 2, 3, 3));
        NativeRecord Record(ushort id) => ScriptGraph.Build(doc).Events[id].Record;
        void Set(NativeRecord record, string name, string value) => doc.SetField(record, record.Fields.Single(field => field.Name == name), value);
        Set(say, native ? "Param1" : "param1", "65000"); Set(say, native ? "Param5" : "param5", native ? "3" : "-2147418045");
        Set(destroy, native ? "NativeArgument5" : "param5", "65000"); if (native) Set(destroy, "NativeArgument7", "64000");
        var resources = Path.Combine(directory, $"existing-resources-{language}-{native}");
        var soundFolder = Path.Combine(resources, "Data", "Sound", "sfx", "SPEECH_LEVEL1"); Directory.CreateDirectory(soundFolder);
        var textFolder = Path.Combine(resources, "Data", "Text", "Default"); Directory.CreateDirectory(textFolder);
        // Independent literal original SDT/STR layouts, with distinct payloads.
        var payloads = new[] { new byte[] { 0xFF, 0x11, 0xFD, 0x41 }, new byte[] { 0xFF, 0x12, 0xFD, 0x42 } };
        var bankBytes = new byte[12 + 2 * 44]; BinaryPrimitives.WriteUInt32LittleEndian(bankBytes, 2);
        var textBytes = new byte[30]; "BFST"u8.CopyTo(textBytes); BinaryPrimitives.WriteUInt32LittleEndian(textBytes.AsSpan(4), 0x420); BinaryPrimitives.WriteUInt32LittleEndian(textBytes.AsSpan(8), 2);
        for (var index = 0; index < 2; index++)
        {
            var offset = 12 + index * 44; BinaryPrimitives.WriteUInt32LittleEndian(bankBytes.AsSpan(4 + index * 4), (uint)offset);
            BinaryPrimitives.WriteUInt32LittleEndian(bankBytes.AsSpan(offset), 40); BinaryPrimitives.WriteUInt32LittleEndian(bankBytes.AsSpan(offset + 4), 4);
            Encoding.ASCII.GetBytes(index == 0 ? "Zulu" : "Alpha").CopyTo(bankBytes, offset + 8); payloads[index].CopyTo(bankBytes, offset + 40);
            var textOffset = 20 + index * 5; BinaryPrimitives.WriteUInt32LittleEndian(textBytes.AsSpan(12 + index * 4), (uint)(textOffset - 12));
            textBytes[textOffset] = 1; textBytes[textOffset + 1] = 1; textBytes[textOffset + 4] = (byte)(0xD4 - index);
        }
        var bankPath = Path.Combine(soundFolder, "SPEECH_LEVEL1HD.SDT"); var textPath = Path.Combine(textFolder, "level1.STR");
        File.WriteAllBytes(bankPath, bankBytes); File.WriteAllBytes(textPath, textBytes);
        var speechMetadata = doc.CreateMapSpeechTextDraft(); doc.SetField(speechMetadata.Record!, speechMetadata.Record!.Fields.Single(field => field.Name == MapSpeechText.StorageField(doc.IsLegacyProject, "SpeechFile")), "SPEECH_LEVEL1");
        var workspace = new EditorWorkspace { Document = doc }; Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        workspace.ConfigureGameResources(new(resources, "existing-player.exe"));
        workspace.SetSelection(new(1, 1, 3, 3)); workspace.Copy(); var buffer = workspace.BufferedSelection;
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var selection = canvas.Selection;
        Dictionary<NativeFile, byte[]> Snapshot() => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var before = Snapshot(); var history = doc.GetHistory().Undo.Count;
        void Exact() { Expect(before.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Existing wizard changed live/unrelated bytes before Finish."); Expect(ReferenceEquals(buffer, workspace.BufferedSelection) && canvas.Selection == selection, "Existing wizard changed ordinary selection/copy buffer."); }
        ScenarioCreationWindow? active = null; workspace.ScenarioEditingPresenter = window => active = window;
        void SelectRecord(ushort id)
        {
            Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
            var record = Record(id); var categories = Control<ComboBox>(workspace, "CategoryPicker"); categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
            var records = Control<ListBox>(workspace, "RecordList"); records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").File == record.File && Get<NativeRecord>(item, "Record").Offset == record.Offset);
            workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout();
        }
        void Open(ushort id)
        {
            SelectRecord(id); active = null; var command = Control<Button>(workspace, "ScenarioEditEventButton");
            Expect(command.IsEnabled && command.Visibility == Visibility.Visible, "Ordinary existing-event command unavailable."); command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(active is { IsEditingExisting: true } && active.Title == LocalizationService.Get("ScenarioWizard.EditTitle") && active.Step == ScenarioCreationStep.Type, "Actual Edit did not open the original Type-first existing mode."); Exact();
        }
        void Type(string name)
        { var picker = (ComboBox)active!.FindName("ScenarioWizardTypes"); picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<LegacyEventSchemas.EventType>(item, "Type").Name == name); Exact(); }
        string TypeName() => Get<LegacyEventSchemas.EventType>(((ComboBox)active!.FindName("ScenarioWizardTypes")).SelectedItem, "Type").Name;
        void Click(string name)
        {
            ((Button)active!.FindName(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (active.Content is FrameworkElement content)
            { content.Measure(new Size(700, 580)); content.Arrange(new Rect(0, 0, 700, 580)); content.UpdateLayout(); }
        }
        // Every real type choice opens its original page (or Repeat/no-page Finish)
        // without changing the live event. Required choices are not fabricated.
        var paths = 0;
        foreach (var type in LegacyEventSchemas.All)
        {
            Open(type.RecordSignature == 0xD5 ? conditionID : sayID); Type(type.Name);
            if (LegacyEventSchemas.ParameterPageID(type.Name) != -1)
                Expect(active!.Next() && active.Step == ScenarioCreationStep.Parameters && active.ParameterEditor is not null, "Original existing parameter page did not open: " + type.Name);
            else if (type.RecordSignature == 0xD5) Expect(active!.Next() && active.Step == ScenarioCreationStep.Repeat, "No-page condition skipped Repeat.");
            else Expect(((Button)active!.FindName("ScenarioWizardFinish")).Visibility == Visibility.Visible, "No-page action cannot Finish from Type.");
            active!.Close(); Exact(); paths++;
        }
        Expect(paths == 103, "Actual existing type-page paths incomplete.");
        Open(conditionID); RenderExistingWizard(active!, "type", native, language); Type("TRIGGER_TIMEELAPSED"); Click("ScenarioWizardNext");
        ((TextBox)active!.ParameterEditor!.FindName("ScenarioSeconds")).Text = "17"; Click("ScenarioWizardBack"); Click("ScenarioWizardNext");
        Expect(((TextBox)active.ParameterEditor!.FindName("ScenarioSeconds")).Text == "17", "Existing Back/Next lost submitted duration.");
        ((TextBox)active.ParameterEditor.FindName("ScenarioSeconds")).Text = "23"; RenderExistingWizard(active, "parameters", native, language); Click("ScenarioWizardCancel"); Exact();
        ScenarioCreationWindow? addWindow = null; workspace.ScenarioCreationPresenter = window => addWindow = window;
        var templates = Control<ComboBox>(workspace, "TemplatePicker"); templates.SelectedItem = templates.Items.Cast<object>().Single(item => native ? Get<RecordTemplate>(item, "Template").Structure == "Trigger" : Get<string>(item, "LegacyKind") == "Trigger");
        var add = ((WrapPanel)templates.Parent).Children.OfType<Button>().Single(button => button.Content?.ToString() == LocalizationService.Get("Editor.AddRecord")); add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(addWindow is { IsEditingExisting: false } && Get<LegacyEventSchemas.EventType>(((ComboBox)addWindow.FindName("ScenarioWizardTypes")).SelectedItem, "Type").Name == "TRIGGER_TIMEELAPSED" && addWindow.Next() && ((TextBox)addWindow.ParameterEditor!.FindName("ScenarioSeconds")).Text == "17", "Actual Add lost submitted cancelled Modify context or retained pending23."); addWindow!.Close(); Exact();
        Open(conditionID); Expect(TypeName() == "TRIGGER_VARIABLE", "Reopened actual Modify used remembered type instead of live event."); Type("TRIGGER_TIMEELAPSED"); Click("ScenarioWizardNext");
        ((TextBox)active!.ParameterEditor!.FindName("ScenarioSeconds")).Text = "17"; Click("ScenarioWizardNext");
        var repeat = (ComboBox)active.FindName("ScenarioWizardRepeat"); repeat.SelectedItem = repeat.Items.Cast<object>().Single(item => Get<ScenarioRepeatMode>(item, "Mode") == ScenarioRepeatMode.Count);
        ((TextBox)active.FindName("ScenarioWizardCount")).Text = "254"; Click("ScenarioWizardBack"); Click("ScenarioWizardNext");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(((TextBox)active.FindName("ScenarioWizardCount")).Text == "254" && active.Title == LocalizationService.Get("ScenarioWizard.EditTitle"), "Existing translation discarded repeat/title."); LocalizationService.SetLanguage(language);
        RenderExistingWizard(active, "repeat", native, language); Exact(); Click("ScenarioWizardFinish");
        var expected = before.ToDictionary(item => item.Key, item => item.Value.ToArray()); var changed = Record(conditionID);
        Array.Clear(expected[changed.File], changed.Offset + (native ? 8 : 16), native ? 8 : 32);
        if (native) { expected[changed.File][changed.Offset + 8] = 5; BinaryPrimitives.WriteUInt32LittleEndian(expected[changed.File].AsSpan(changed.Offset + 12), 17); }
        else { BinaryPrimitives.WriteUInt32LittleEndian(expected[changed.File].AsSpan(changed.Offset + 16), 5); BinaryPrimitives.WriteUInt32LittleEndian(expected[changed.File].AsSpan(changed.Offset + 32), 17); }
        expected[changed.File][changed.Offset + (native ? 22 : 54)] = (byte)LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED").Type; expected[changed.File][changed.Offset + (native ? 23 : 55)] = 254;
        Expect(active.CreatedEventID == conditionID && expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)) && doc.GetHistory().Undo.Count == history + 1, "Existing Type/Parameters/Repeat Finish changed ID or split history/unrelated bytes.");
        var after = Snapshot(); workspace.Undo(); Exact(); workspace.Redo(); Expect(after.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Existing Redo differs."); workspace.Undo(); Exact();
        Open(conditionID); Type("TRIGGER_TIMER"); Click("ScenarioWizardNext"); ((ComboBox)active!.ParameterEditor!.FindName("AliasEventPrimary")).Text = "Edited timer";
        Click("ScenarioWizardNext"); Exact(); Click("ScenarioWizardFinish");
        Expect(active.CreatedEventID == conditionID && doc.GetHistory().Undo.Count == history + 1 && doc.ReadScenarioNames().Slots.Any(slot => slot.StoredName == "Edited timer"), "Existing nested alias did not commit atomically with same event."); workspace.Undo(); Exact();
        Open(cellID); Click("ScenarioWizardNext"); var rectangle = (CheckBox)active!.FindName("ScenarioWizardRectangle");
        Expect(!rectangle.IsEnabled && !((Button)active.FindName("ScenarioWizardPickRectangle")).IsEnabled && !active.AcceptRectangle(new(1, 1, 2, 2)), "Existing cell action allowed rectangle expansion.");
        rectangle.IsChecked = true; Expect(rectangle.IsChecked == false, "Forced rectangle changed existing metadata."); ((Button)active.FindName("ScenarioWizardPickRectangle")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(!canvas.IsPickingRegion, "Disabled existing rectangle started canvas picking.");
        RenderExistingWizard(active, "cell", native, language); Click("ScenarioWizardFinish"); Expect(active.CreatedEventID == cellID && doc.GetHistory().Undo.Count == history, "Unchanged existing cell consumed Undo or expanded records."); Exact();
        Open(destroyID); Click("ScenarioWizardNext"); Expect(!active!.ParameterEditor!.HasPendingInputs, "Unknown imported gate silently selected a new gate."); RenderExistingWizard(active, "gate", native, language);
        Click("ScenarioWizardFinish"); Expect(active.CreatedEventID == destroyID && doc.GetHistory().Undo.Count == history, "Unknown existing gate was normalized by no-op Finish."); Exact();
        Open(sayID); Click("ScenarioWizardNext"); Expect(((TextBox)active!.ParameterEditor!.FindName("ScenarioParam1")).Text == "65000", "Existing speech import lost.");
        RenderExistingWizard(active, "speech", native, language); Click("ScenarioWizardFinish"); Expect(active.CreatedEventID == sayID && doc.GetHistory().Undo.Count == history, "Existing speech no-op normalized unknown operands."); Exact();
        Open(sayID); Click("ScenarioWizardNext"); var speech = active!.ParameterEditor!; var sounds = (ListBox)speech.FindName("ScenarioSpeechList");
        Expect(sounds.Items.Count == 2, "Existing Modify lost current map speech bank."); sounds.SelectedIndex = 1;
        Expect(((TextBox)speech.FindName("ScenarioParam1")).Text == "2" && ((TextBox)speech.FindName("ScenarioSpeechCaption")).Text == "B", "Existing speech list changed original one-based sound/caption correspondence.");
        var previews = new List<string>();
        void Preview(ScenarioParameterWindow page)
        {
            page.SpeechPlaybackLauncher = request => { previews.Add(request.Mp2Path); Expect(request.PlayerPath == "existing-player.exe" && File.ReadAllBytes(request.Mp2Path).SequenceEqual(payloads[1]), "Existing Modify preview used wrong player/payload."); Exact(); };
        }
        Preview(speech); Expect(speech.PlaySpeech(), "Existing Modify preview failed.");
        sounds.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = System.Windows.Controls.Control.MouseDoubleClickEvent });
        Expect(previews.Count == 2 && doc.GetHistory().Undo.Count == history, "Existing actual double-click preview missing or changed live map.");
        RenderExistingWizard(active, "speech-selected", native, language);
        Click("ScenarioWizardBack"); Expect(previews.All(path => !File.Exists(path)) && !speech.PlaySpeech(), "Existing Back retained MP2 or disposed audio authority.");
        Click("ScenarioWizardNext"); speech = active.ParameterEditor!; Expect(((TextBox)speech.FindName("ScenarioParam1")).Text == "2", "Existing Back/Next lost private speech Store.");
        Preview(speech); Expect(speech.PlaySpeech(), "Existing restored speech preview failed."); Click("ScenarioWizardCancel"); Expect(previews.All(path => !File.Exists(path)) && !speech.PlaySpeech(), "Existing Cancel retained audio/temporary MP2."); Exact();
        Open(sayID); Click("ScenarioWizardNext"); speech = active!.ParameterEditor!; Expect(((TextBox)speech.FindName("ScenarioParam1")).Text == "65000", "Existing speech reopen used cancelled private ID.");
        ((ListBox)speech.FindName("ScenarioSpeechList")).SelectedIndex = 1; ((CheckBox)speech.FindName("ScenarioSpeechSuppressText")).IsChecked = false;
        Preview(speech); Expect(speech.PlaySpeech(), "Existing Finish preview missing."); Click("ScenarioWizardFinish");
        var speechExpected = before.ToDictionary(item => item.Key, item => item.Value.ToArray()); var speechRecord = Record(sayID);
        if (native) { BinaryPrimitives.WriteUInt16LittleEndian(speechExpected[speechRecord.File].AsSpan(speechRecord.Offset + 8), 2); speechExpected[speechRecord.File][speechRecord.Offset + 12] = 0; }
        else { BinaryPrimitives.WriteUInt32LittleEndian(speechExpected[speechRecord.File].AsSpan(speechRecord.Offset + 16), 2); BinaryPrimitives.WriteUInt32LittleEndian(speechExpected[speechRecord.File].AsSpan(speechRecord.Offset + 32), 0); }
        Expect(active.CreatedEventID == sayID && doc.GetHistory().Undo.Count == history + 1 && speechExpected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)) && previews.All(path => !File.Exists(path)) && !speech.PlaySpeech(), "Existing speech Finish changed identity/unrelated bytes/history or retained audio."); workspace.Undo(); Exact();
        Open(spawnID); Click("ScenarioWizardNext"); var parameter = active!.ParameterEditor!; var flags = parameter.CreateSpawnFlagsWindow(); Expect(flags is not null, "Existing spawn flag child missing.");
        ((CheckBox)flags!.FindName("SpawnFlag4")).IsChecked = true; Expect(flags.ApplyDraft(), "Existing private flags failed."); flags.Close();
        ((Button)parameter.FindName("ScenarioSpawnPoint")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(canvas.IsPickingRegion, "Existing point did not use actual canvas."); canvas.CompleteRegionPick(new(6, 7), new(6, 7));
        RenderExistingWizard(active, "spawn", native, language); Exact(); Click("ScenarioWizardFinish"); var result = Record(spawnID);
        Expect(active.CreatedEventID == spawnID && doc.GetHistory().Undo.Count == history + 1, "Existing spawn created a new record or split history.");
        if (native) Expect(result.File.Data.Span.Slice(result.Offset + 8, 8).SequenceEqual(new byte[] { 4, 3, 1, 45, 6, 0, 7, 0 }), "Existing spawn flags/point literal differs.");
        else Expect(BinaryPrimitives.ReadUInt32LittleEndian(result.File.Data.Span[(result.Offset + 28)..]) == 45 && BinaryPrimitives.ReadUInt32LittleEndian(result.File.Data.Span[(result.Offset + 32)..]) == 6 && BinaryPrimitives.ReadUInt32LittleEndian(result.File.Data.Span[(result.Offset + 40)..]) == 7, "Existing project spawn flags/point literal differs."); workspace.Undo(); Exact();
        // Actual tree command chooses its event, even while another record is
        // selected in ordinary properties; current ID/links survive Finish.
        SelectRecord(sayID); Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 2;
        var tree = Control<TreeView>(workspace, "ScriptTree"); Expect(SelectExistingTreeEvent(tree, conditionID), "Existing tree event could not be selected.");
        active = null; var treeEdit = Control<Button>(workspace, "ScriptEditEventButton"); Expect(treeEdit.IsEnabled, "Actual tree Edit command disabled."); treeEdit.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(active is { IsEditingExisting: true } && TypeName() == "TRIGGER_VARIABLE", "Tree Edit used the ordinary selected action."); Type("TRIGGER_TIMEELAPSED"); Click("ScenarioWizardNext"); ((TextBox)active!.ParameterEditor!.FindName("ScenarioSeconds")).Text = "17"; Click("ScenarioWizardNext"); Click("ScenarioWizardFinish");
        Expect(active.CreatedEventID == conditionID && doc.GetHistory().Undo.Count == history + 1, "Actual tree Edit changed identity/history."); workspace.Undo(); Exact();
        // Real pending generic field commit invalidates the modeless wizard.
        Open(sayID); Click("ScenarioWizardNext"); SelectRecord(sayID); var properties = Control<DataGrid>(workspace, "PropertyGrid");
        properties.SelectedItem = properties.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == (native ? "Param1" : "param1")); properties.ScrollIntoView(properties.SelectedItem, properties.Columns[1]); properties.UpdateLayout(); properties.CurrentCell = new DataGridCellInfo(properties.SelectedItem, properties.Columns[1]); Expect(properties.BeginEdit(), "Existing pending generic edit missing."); properties.UpdateLayout();
        var pending = properties.Columns[1].GetCellContent(properties.SelectedItem) as TextBox; Expect(pending is not null, "Existing pending generic editor absent."); pending!.Text = "1";
        Expect(!active!.Finish() && Record(sayID).Read(Record(sayID).Fields.Single(field => field.Name == (native ? "Param1" : "param1"))) == "1", "Existing Finish ignored actual pending generic commit."); active.Close(); workspace.Undo(); Exact();
        // Late unsubmitted parameter changes, reentrant Finish and owner Undo
        // cannot revive an older prepared result.
        ScenarioCreationWindow? late = null;
        late = new(doc, doc.CreateScenarioCreationContext(), 0xD6, () => true, () => { ((TextBox)late!.ParameterEditor!.FindName("ScenarioParam1")).Text = "2"; Expect(!late.Finish(), "Reentrant existing Finish accepted."); return true; }, _ => throw new InvalidOperationException("Late existing input committed."), existingRecord: Record(sayID));
        Expect(late.Next() && !late.Finish(), "Late unsubmitted existing input reused old plan."); late.Close(); Exact();
        Open(sayID); doc.Paint([new(1, 1)], new(1, 2, 3, 4)); doc.Undo(); Expect(!active!.Next() && !active.Finish(), "Owner Undo restored old existing authority."); active.Close(); Exact();
        Open(sayID); workspace.ConfigureGameResources(new(Path.Combine(directory, "reconfigured"))); Expect(!active!.Next() && !active.Finish(), "Resource reconfiguration reused existing wizard."); Exact();
        Open(sayID); workspace.EndScenarioNamesLifetime(); Expect(!active!.Next() && !active.Finish() && workspace.CreateScenarioEditingWindow(Record(sayID)) is null, "Closed owner reused existing edit."); Exact();
        Expect(File.ReadAllBytes(bankPath).SequenceEqual(bankBytes) && File.ReadAllBytes(textPath).SequenceEqual(textBytes), "Existing wizard changed resource fixtures.");
        CheckExistingWizardScopes(native);
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} existing full wizard, actual ordinary/tree commands, 103 pages, private Back/Next/Cancel context, same-ID atomic Finish, imports/gates, aliases/flags/canvas, translation and lifetime");
    }
    private static void CheckExistingWizardScopes(bool native)
    {
        var doc = NativeMapDocument.CreateNewMap(new("ExistingScopes", "ExistingScopes", 12, 12, NativeFormat: native));
        var condition = doc.AddScenarioEvent(0xD5, new(0, 0)); var id = ScriptGraph.ID(condition);
        if (!native) foreach (var scope in new[] { 1, 32 }) { var link = doc.AddLegacyRecord("TriggerLink", new(0, 0)); doc.SetField(link, link.Fields.Single(field => field.Name == "node.type"), scope.ToString()); }
        var roots = ScriptGraph.Build(doc).Roots; var player = roots.First(edge => edge.Scope == 1); var level = roots.First(edge => edge.Scope == 32);
        doc.SetScriptLink(player.Owner, player.Field, id); var workspace = new EditorWorkspace { Document = doc };
        bool HasPlayerOnly(ScenarioCreationWindow wizard) => ((ComboBox)wizard.FindName("ScenarioWizardTypes")).Items.Cast<object>().Any(item => Get<LegacyEventSchemas.EventType>(item, "Type").Name == "TRIGGER_CREATURES");
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var only = workspace.CreateScenarioEditingWindow(ScriptGraph.Build(doc).Events[id].Record); Expect(only is not null && HasPlayerOnly(only), "Actual player-root Modify excluded original player-only type."); only!.Close();
        Expect(before.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Opening player-root Modify changed links.");
        doc.SetScriptLink(level.Owner, level.Field, id); before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var shared = workspace.CreateScenarioEditingWindow(ScriptGraph.Build(doc).Events[id].Record); Expect(shared is not null && !HasPlayerOnly(shared), "Actual shared-root Modify ignored one linked scope."); shared!.Close(); workspace.EndScenarioNamesLifetime();
        Expect(before.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Shared-root Modify changed identity/links on Cancel.");
    }
    private static bool SelectExistingTreeEvent(ItemsControl parent, ushort id)
    {
        bool Contains(object node) => Get<ushort>(node, "EventID") == id || Get<IEnumerable<object>>(node, "Children").Any(Contains);
        parent.UpdateLayout();
        foreach (var node in parent.Items.Cast<object>().Where(Contains))
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(node) is not TreeViewItem container) continue;
            if (Get<ushort>(node, "EventID") == id) { container.IsSelected = true; container.BringIntoView(); return true; }
            container.IsExpanded = true; container.UpdateLayout(); if (SelectExistingTreeEvent(container, id)) return true;
        }
        return false;
    }
    private static void RenderExistingWizard(Window window, string state, bool native, string language)
    {
        foreach (var width in new[] { 540, 700 }) foreach (var bottom in new[] { false, true })
        {
            var size = new Size(width, width == 540 ? 420 : 580); var content = (FrameworkElement)window.Content;
            content.Measure(size); content.Arrange(new Rect(new Point(0, 0), size)); content.UpdateLayout();
            if (state == "type")
            {
                var picker = (ComboBox)window.FindName("ScenarioWizardTypes");
                Expect(picker.ActualHeight > 0 && picker.ActualHeight <= 40, "Type selector stretched over the wizard content area.");
            }
            IEnumerable<DependencyObject> Descendants(DependencyObject value)
            { for (var index = 0; index < VisualTreeHelper.GetChildrenCount(value); index++) { var child = VisualTreeHelper.GetChild(value, index); yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
            var scroll = Descendants(content).OfType<ScrollViewer>().FirstOrDefault(item => item.Content is StackPanel);
            if (bottom) scroll?.ScrollToEnd(); else scroll?.ScrollToTop(); content.UpdateLayout();
            foreach (var key in new[] { "ScenarioWizardBack", "ScenarioWizardNext", "ScenarioWizardFinish", "ScenarioWizardCancel" })
                if (window.FindName(key) is Button { Visibility: Visibility.Visible } button)
                { var bounds = button.TransformToAncestor(content).TransformBounds(new Rect(button.RenderSize)); Expect(bounds.Left >= 0 && bounds.Right <= size.Width && bounds.Bottom <= size.Height, "Existing wizard buttons outside viewport."); }
            var target = Environment.GetEnvironmentVariable("DK2_EXISTING_WIZARD_ARTIFACTS"); if (string.IsNullOrEmpty(target)) continue;
            Directory.CreateDirectory(target); var bitmap = new RenderTargetBitmap(width, (int)size.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = new FileStream(Path.Combine(target, $"existing-{state}-{language}-{native}-{width}-{bottom}.png"), FileMode.CreateNew); encoder.Save(output);
        }
    }
}
