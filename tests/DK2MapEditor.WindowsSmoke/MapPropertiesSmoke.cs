using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckEditorDerivedResizeWindow(bool native,string language)
    {
        if(!native)return;
        var doc=NativeMapDocument.CreateNewMap(new("DerivedUI","DerivedUI",32,30,NativeFormat:true));
        var gem=doc.AddRecord(RecordTemplate.All.Single(item=>item.Structure=="GAMEOBJECT"),new(18,17));doc.SetField(gem,gem.Fields.Single(field=>field.Name=="bObjectID"),"28");
        doc.AddMapRegion(MapRegionKind.Gate,new(9,10,1,1));
        var crystal=doc.AddScenarioEvent(0xD6,new(0,0),LegacyEventSchemas.All.Single(item=>item.RecordSignature==0xD6&&item.Type==2));
        var gate=doc.AddScenarioEvent(0xD6,new(0,0),LegacyEventSchemas.All.Single(item=>item.RecordSignature==0xD6&&item.Type==32));
        doc.ApplyScenarioEditorParameterDraft(doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(gate),doc.ScenarioGateChoices().Single().Key));
        var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var history=doc.GetHistory().Undo.Count;var count=doc.EditorChangeCount;
        var window=new MapPropertiesWindow(doc,()=>true,()=>true,new());
        int Coordinate(NativeRecord action,int offset)=>System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(action.File.Data.Span.Slice(action.Offset+offset,2));
        try
        {
            ((TextBox)window.FindName("MapPropertiesWidth")).Text="36";((TextBox)window.FindName("MapPropertiesHeight")).Text="34";((RadioButton)window.FindName("MapPropertiesAnchor8")).IsChecked=true;
            Expect(window.ApplyProperties()&&Coordinate(crystal,12)==22&&Coordinate(crystal,14)==21&&Coordinate(gate,12)==14&&Coordinate(gate,14)==15,"Actual properties window left computed targets behind.");
            Expect(doc.GetHistory().Undo.Count==history+1&&doc.EditorChangeCount==unchecked(count+4),"Window derived projection invented extra source counts/history.");
            Expect(LocalizationService.Get("Diagnostic.DK2N032")!="Diagnostic.DK2N032","Missing computed-target localization: "+language);
            doc.Undo();Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Window computed target Undo lost exact buffers.");
        }
        finally{window.Close();}
    }
    private static void CheckEditorPlayerResizeWindow(bool native, string language)
    {
        var doc=NativeMapDocument.CreateNewMap(new("PlayerResizeUI","PlayerResizeUI",32,30,Players:2,NativeFormat:native));
        var player=doc.PlayerSettingsRows().Single(row=>row.ID==4);
        doc.SetPlayerSettings([new(player.ID,player.Name,(int)player.Gold,true,player.Health is null?null:(int)player.Health)]);
        foreach(var id in new uint[]{3,4}){doc.SetPlayerCamera(id,new(8,9));doc.SetPlayerBuildArea(id,new(4,5,8,8));}
        var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
        var window=new MapPropertiesWindow(doc,()=>true,()=>true,new());
        try
        {
            ((TextBox)window.FindName("MapPropertiesWidth")).Text="36";((TextBox)window.FindName("MapPropertiesHeight")).Text="34";
            ((RadioButton)window.FindName("MapPropertiesAnchor8")).IsChecked=true;
            Expect(window.ApplyProperties(),"Actual map properties refused CPU keeper relocation.");
            var human=doc.PlayerGeometries().Single(row=>row.ID==3);var cpu=doc.PlayerGeometries().Single(row=>row.ID==4);
            Expect(human.Camera==new TilePoint(8,9)&&human.BuildStart==new TilePoint(4,5)&&cpu.Camera==new TilePoint(12,13)&&cpu.BuildStart==new TilePoint(8,9),"Actual window shifted human fields or failed to shift CPU fields.");
            Expect(doc.EditorChangeCount==unchecked(count+2)&&doc.GetHistory().Undo.Count==history+1,"Window player relocation added per-player history/counts.");
            Expect(LocalizationService.Get("Diagnostic.DK2N030")!="Diagnostic.DK2N030"&&LocalizationService.Get("Diagnostic.DK2N031")!="Diagnostic.DK2N031","Player warnings lack translation: "+language);
            doc.Undo();Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Window player resize Undo lost full buffers.");
        }
        finally{window.Close();}
    }
    private static void CheckEditorScriptResizeWindow(bool native, string language)
    {
        var doc = NativeMapDocument.CreateNewMap(new("ResizeScriptUI", "ResizeScriptUI", 32, 30, NativeFormat: native));
        var action = doc.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(type => type.RecordSignature == 0xD6 && type.Type == 22));
        doc.SetField(action, action.Fields.Single(field => field.Name == (native ? "Param5" : "param5")), "29");
        doc.SetField(action, action.Fields.Single(field => field.Name == (native ? "Param7" : "param7")), "27");
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count; var counter = doc.EditorChangeCount;
        using var canceled = doc.CreateMapPropertiesSession();
        var window = new MapPropertiesWindow(doc, () => true, () => true, new());
        try
        {
            ((TextBox)window.FindName("MapPropertiesWidth")).Text = "24"; ((TextBox)window.FindName("MapPropertiesHeight")).Text = "22";
            Expect(window.ApplyProperties() && doc.Width == 24 && doc.Height == 22, "Actual properties window refused a retained out-of-map script target.");
            var current = doc.Records().Single(ScriptGraph.IsEvent);
            Expect(current.Read(current.Fields.Single(field => field.Name == (native ? "Param5" : "param5"))) == "29"
                && current.Read(current.Fields.Single(field => field.Name == (native ? "Param7" : "param7"))) == "27", "Window normalized the retained target.");
            Expect(doc.EditorChangeCount == unchecked(counter + 2) && doc.GetHistory().Undo.Count == history + 1, "Window target resize lost source count/parent history.");
            Expect(!string.IsNullOrWhiteSpace(LocalizationService.Get("Diagnostic.DK2N029")), "Localized target diagnostic missing: " + language);
            doc.Undo(); Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Window target resize Undo lost bytes.");
        }
        finally { window.Close(); }
    }

    private static void CheckMapProperties(bool native, string language, string directory)
    {
        CheckMapNameWarning(native,language);
        CheckEditorScriptResizeWindow(native, language);
        CheckEditorPlayerResizeWindow(native, language);
        CheckEditorDerivedResizeWindow(native, language);
        var seed = NativeMapDocument.CreateNewMap(new("ParentUI", "ParentUI", 24, 24, Players: 5, PlaceHearts: true, NativeFormat: native));
        seed.Paint([new(8, 9)], new(5, 3, 1), "Marker");
        if (native) seed.AddRecord(RecordTemplate.All.Single(row => row.Structure == "GAMEOBJECT"), new(20, 20)); else seed.AddLegacyRecord("Object", new(20, 20));
        var input = seed.SaveCopy(Path.Combine(directory, $"parent-input-{language}-{native}"));
        var disk = Directory.GetFiles(Path.GetDirectoryName(input)!, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
        var shell = new MainWindow(new AppSettings(), persistSettings: false); shell.OpenMapFromPath(input);
        var workspace = (EditorWorkspace)shell.FindName("Editor"); var source = workspace.Document!;
        var before = source.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = source.GetHistory().Undo.Count;
        workspace.SetSelection(new(2, 2, 3, 3)); workspace.Copy(); var buffer = workspace.BufferedSelection; var selection = Control<MapCanvas>(workspace, "Canvas").Selection;
        void Exact()
        {
            Expect(before.All(row => row.Key.Data.Span.SequenceEqual(row.Value)) && source.GetHistory().Undo.Count == history && source.TargetGameVersion == 17, "Outer Cancel/Undo did not retain exact map/history/target.");
            Expect(ReferenceEquals(buffer, workspace.BufferedSelection) && selection == Control<MapCanvas>(workspace, "Canvas").Selection, "Map properties changed live selection/copy buffer.");
        }
        MapPropertiesWindow? window = null;
        workspace.MapPropertiesPresenter = shown => window = shown;
        T Named<T>(string key) => (T)window!.FindName("MapProperties" + key);
        void Layout(Size? size = null)
        { var content = (FrameworkElement)window!.Content; var bounds = size ?? new Size(1080, 800); content.Measure(bounds); content.Arrange(new Rect(new Point(), bounds)); content.UpdateLayout(); }
        void Open(bool menu = false)
        {
            window = null;
            if (menu) ((MenuItem)shell.FindName("MapPropertiesMenuItem")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            else Control<Button>(workspace, "MapPropertiesButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(window is not null && !ReferenceEquals(source, window.WorkingDocument), "Actual map properties command lacks independent draft."); Layout();
        }
        void Choose(string key, string value)
        { var combo = Named<ComboBox>(key); combo.SelectedItem = combo.Items.Cast<object>().Single(row => Get<string>(row, "Value") == value); }
        void Compose()
        {
            var child = window!.ChildWorkspace; var tabs = Control<TabControl>(child, "WorkspaceTabs");
            Expect(tabs.Items.Count == 10 && tabs.Items.Cast<TabItem>().Where(row => row.Visibility == Visibility.Visible).Count() == 4, "Parent child workspace scope/indexes differ.");
            var canvas = Control<MapCanvas>(child, "Canvas"); var tile = window.WorkingDocument.GetTile(8, 9);
            canvas.Tool = DrawingTool.Brush; canvas.PaintTile = new(2, 7, 1); canvas.PaintStroke([new(8, 9)]); canvas.DrawGesture(new(8, 9), new(8, 9)); canvas.FillAt(new(8, 9)); canvas.Tool = DrawingTool.PlaceObject; canvas.PaintObjectAt(new(8, 9));
            Expect(window.WorkingDocument.GetTile(8, 9) == tile, "Properties geometry canvas painted terrain/objects."); canvas.Tool = DrawingTool.Select;
            tabs.SelectedIndex = 7; var player = window.WorkingDocument.PlayerSettingsRows()[0];
            child.PlayerGoldPresenter=shown=>shown.Acknowledge();
            Control<TextBox>(child, "PlayerGold").Text = (player.Gold + 123).ToString();
            Expect(!window.ApplyProperties() && Named<TextBlock>("Status").Text == LocalizationService.Get("MapProperties.Pending"), "Parent silently discarded unaccepted player inputs."); Exact();
            Click(Control<StackPanel>(child, "PlayerGeneral"), "Editor.PlayerApply");
            Expect(window.WorkingDocument.PlayerSettingsRows()[0].Gold == player.Gold + 123, "Player child Apply failed.");
            var camera = window.WorkingDocument.PlayerGeometries().Single(row => row.ID == player.ID);
            if (camera.CameraEditable)
            {
                Click(Control<StackPanel>(child, "PlayerCameraControls"), "Editor.PlayerCameraPick");
                Expect(canvas.IsPickingRegion && tabs.SelectedIndex == 0, "Parent player geometry navigation failed."); canvas.CompleteRegionPick(new(6, 7), new(6, 7));
                Expect(tabs.SelectedIndex == 7 && window.WorkingDocument.PlayerGeometries().Single(row => row.ID == player.ID).EffectiveCamera == new TilePoint(6, 7), "Parent camera pick failed.");
            }
            tabs.SelectedIndex = 5; Layout(); var table = Control<DataGrid>(child, "RuleTable"); table.SelectedCells.Clear(); table.SelectedCells.Add(new(table.Items[0], table.Columns[3]));
            var ruleValue = Control<ComboBox>(child, "RuleValuePicker"); ruleValue.SelectedItem = ruleValue.Items.Cast<object>().Single(row => Get<ValueChoice>(row, "Choice").Value == "4");
            Click(Control<StackPanel>(child, "RulePanel"), "Editor.RuleApply");
            tabs.SelectedIndex = 6; Layout(); var variables = Control<DataGrid>(child, "VariableTable");
            var variable = window.WorkingDocument.MapVariableCells().First(row => row.Editable && row.Definition.Minimum <= 1 && row.Definition.Maximum >= 1 && !row.Definition.FixedPoint);
            variables.SelectedItem = variables.Items.Cast<object>().Single(row => Get<MapVariableCell>(row, "Cell").Definition.ID == variable.Definition.ID);
            Control<TextBox>(child, "VariableNumber").Text = "1"; Click(Control<StackPanel>(child, "VariablePanel"), "Editor.VariableApply");
            MapLevelOptionsWindow? level = null; window.LevelOptionsPresenter = shown => level = shown;
            Named<Button>("LevelOptions").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ((TextBox)level!.FindName("LevelOptionGameTicks")).Text = "49";
            Expect(!window.ApplyProperties() && level.HasPendingInputs, "Parent accepted unapplied level child.");
            Expect(level.ApplyOptions() && window.WorkingDocument.CreateMapLevelOptionsDraft().Values["GameTicks"] == "49", "Level child did not edit independent working document.");
            MapSpeechTextWindow? speech = null; window.SpeechTextPresenter = shown => speech = shown;
            Named<Button>("SpeechText").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var speechDraft = window.WorkingDocument.CreateMapSpeechTextDraft([], 0);
            // Both formats expose the named text-table selector without game resources.
            var tableChoice = (ComboBox)speech!.FindName("SpeechTextFieldTextTableID");
            tableChoice.SelectedItem = tableChoice.Items.Cast<object>().First(row => Get<string>(row, "ID") != speechDraft.Values["TextTableID"]);
            Expect(!window.ApplyProperties() && speech.HasPendingInputs, "Parent accepted unapplied speech/text child."); Expect(speech.ApplyOptions(), "Speech child Apply failed.");
            Choose("Players", "1"); Choose("Version", "13"); Named<TextBox>("Name").Text = "Parent accepted";
            tabs.SelectedIndex = 7; Exact();
        }
        try
        {
            Open(true); Expect(Named<RadioButton>("Anchor0").IsChecked == true && Named<ComboBox>("Players").Items.Count == 5 && Named<ComboBox>("Version").Items.Count == 3, "Parent source anchors/catalogues differ.");
            foreach (var row in MapGeneralSettings.Keepers) Choose("Players", row.Value); foreach (var row in MapGeneralSettings.Versions) Choose("Version", row.Value);
            Named<TextBox>("Name").Text = "pending"; LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(Named<TextBox>("Name").Text == "pending", "Language discarded parent input."); LocalizationService.SetLanguage(language);
            Named<Button>("Reset").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Compose(); Named<Button>("Cancel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact();
            Open(); Compose();
            foreach (var size in new[] { new Size(1080, 800), new Size(850, 650) })
            {
                Layout(size); var content = (FrameworkElement)window!.Content; var bounds = Named<Button>("Apply").TransformToAncestor(content).TransformBounds(new Rect(Named<Button>("Apply").RenderSize));
                Expect(bounds.Right <= size.Width && bounds.Bottom <= size.Height && bounds.Top >= 0 && Named<TextBox>("Name").ActualWidth >= 300, "Parent buttons or fields clipped.");
                if (Environment.GetEnvironmentVariable("DK2_MAP_PROPERTIES_ARTIFACTS") is { Length: > 0 } artifacts)
                {
                    Directory.CreateDirectory(artifacts); var visual = new DrawingVisual(); using (var context = visual.RenderOpen()) context.DrawRectangle((Brush)Application.Current.FindResource("Brush.Background"), null, new Rect(new Point(), size));
                    var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); bitmap.Render(content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = new FileStream(Path.Combine(artifacts, $"parent-{language}-{native}-{size.Width}.png"), FileMode.CreateNew); encoder.Save(stream);
                }
            }
            var applied = window!.ApplyProperties();
            Expect(applied && source.GetHistory().Undo.Count == history + 1 && source.TargetGameVersion == 13 && source.PlayerSettingsRows().Count == 7
                && source.CreateMapGeneralDraft().Values["Players"] == "1" && source.CreateMapLevelOptionsDraft().Values["GameTicks"] == "49", "Parent composition failed: applied=" + applied + ", players=" + source.PlayerSettingsRows().Count + ", status=" + Named<TextBlock>("Status").Text + ", detail=" + Named<TextBlock>("Status").ToolTip);
            var accepted = source.Files.ToDictionary(file => file, file => file.Data.ToArray()); var output = source.SaveCopy(Path.Combine(directory, $"parent-output-{language}-{native}")); var reload = NativeMapDocument.Load(output);
            Expect(reload.CreateMapGeneralDraft().Values["Name"] == "Parent accepted" && reload.CreateMapLevelOptionsDraft().Values["GameTicks"] == "49" && reload.TargetGameVersion == (native ? 17 : 13), "Parent save/reopen lost metadata or invented native target persistence.");
            source.Undo(); Exact(); source.Redo(); Expect(accepted.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Parent Redo lost composite bytes."); source.Undo(); Exact();
            var equalCount=source.EditorChangeCount;
            Open(); Expect(window!.ApplyProperties() && source.GetHistory().Undo.Count == history+1 && source.EditorChangeCount==unchecked(equalCount+1), "Equal original parent Apply lost preallocated Undo/change increment.");
            Expect(before.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Equal source Apply changed map buffers.");
            source.Undo();Expect(source.EditorChangeCount==equalCount,"Equal parent Undo lost original count.");Exact();
            Open(); Named<TextBox>("Name").Text = new string('A', 64); Expect(!window!.ApplyProperties(), "Parent display-name overflow accepted."); Exact(); window.Close();
            for (var anchor = 0; anchor < 9; anchor++)
            {
                Open(); Named<TextBox>("Width").Text = "26"; Named<TextBox>("Height").Text = "26"; Named<RadioButton>("Anchor" + anchor).IsChecked = true;
                Expect(window!.ApplyProperties() && source.GetTile(8 + anchor % 3, 9 + anchor / 3) == new MapTile(5, 3, 1) && source.GetHistory().Undo.Count == history + 1, "Parent nine-anchor resize translation differs."); source.Undo(); Exact();
            }
            Open(); Named<TextBox>("Width").Text = "0"; Named<TextBox>("Height").Text = "1"; Named<Button>("ResetSize").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(Named<TextBox>("Width").Text == "24" && Named<TextBox>("Height").Text == "24", "Reset dimensions failed."); window!.Close();
            Open(); Named<TextBox>("Width").Text = "130"; Named<TextBox>("Height").Text = "130";
            var prompts = 0; window!.LargeMapPrompt = _ => { prompts++; Expect(!window.ApplyProperties(), "Parent consent allowed reentrant Apply."); Named<TextBox>("Name").Text = "changed during consent"; return true; };
            Expect(!window.ApplyProperties() && prompts == 1, "Changed parent input reused consent."); Exact();
            window.LargeMapPrompt = _ => false; Expect(!window.ApplyProperties(), "Large-map denial accepted."); Exact();
            window.LargeMapPrompt = plan => { Expect(plan.Resize.Anchor == 0 && plan.NeedsLargeMapConsent, "Large preflight source semantics differ."); return true; };
            Expect(window.ApplyProperties() && source.Width == 130 && source.Height == 130, "Large-map accepted resize failed.");
            var largeCount=source.EditorChangeCount;
            Open(); window!.LargeMapPrompt = _ => throw new InvalidOperationException("Unchanged large dimensions requested consent."); Expect(window.ApplyProperties() && source.GetHistory().Undo.Count == history + 2 && source.EditorChangeCount==unchecked(largeCount+1), "Unchanged large parent lost original equal Apply history/count.");
            source.Undo();Expect(source.Width==130&&source.EditorChangeCount==largeCount,"Equal large Apply Undo resized map.");source.Undo();Exact();
            foreach (var change in new[] { "working", "sourceUndo", "locale", "close", "levelInputUndo" })
            {
                Open(); MapLevelOptionsWindow? level = null;
                if (change == "levelInputUndo") { window!.LevelOptionsPresenter = shown => level = shown; Named<Button>("LevelOptions").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); }
                Named<TextBox>("Width").Text = "130"; Named<TextBox>("Height").Text = "130";
                window!.LargeMapPrompt = _ =>
                {
                    if (change == "working") window.WorkingDocument.Paint([new(1, 1)], new(3, 2, 1), "Wrong child scope");
                    if (change == "sourceUndo") { source.Paint([new(1, 1)], new(3, 2, 1), "Reentrant source"); source.Undo(); }
                    if (change == "locale") LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
                    if (change == "close") window.Close();
                    if (change == "levelInputUndo") { var text = (TextBox)level!.FindName("LevelOptionGameTicks"); var previous = text.Text; text.Text = "49"; text.Text = previous; }
                    return true;
                };
                Expect(!window.ApplyProperties(), "Consent survived " + change); window.Close(); LocalizationService.SetLanguage(language); Exact();
            }
            Open(); Named<TextBox>("Width").Text = "0"; Named<TextBox>("Height").Text = "1"; var cropPrompts = 0;
            window!.CropPrompt = plan => { cropPrompts++; Expect(plan.Crops.Count != 0, "Crop prompt lacks records."); return false; }; Expect(!window.ApplyProperties() && cropPrompts == 1, "Crop denial accepted or omitted prompt: " + Named<TextBlock>("Status").ToolTip); Exact();
            window.CropPrompt = _ => true; Expect(window.ApplyProperties() && source.Width == 7 && source.Height == 7, "Accepted crop failed."); source.Undo(); Exact();
            Open(); Named<TextBox>("Name").Text = "pending ordinary field";
            var pendingDraft = source.CreateMapLevelOptionsDraft(); var pendingRecord = pendingDraft.Record!;
            var pendingField = pendingRecord.Fields.Single(row => row.Name == MapLevelOptions.StorageField(source.IsLegacyProject, "Soundtrack"));
            var categories = Control<ComboBox>(workspace, "CategoryPicker"); var records = Control<ListBox>(workspace, "RecordList");
            categories.SelectedItem = categories.Items.Cast<object>().Single(row => Get<string>(row, "ID") == pendingRecord.Category); records.SelectedItem = records.Items.Cast<object>().Single(row => Get<NativeRecord>(row, "Record").Offset == pendingRecord.Offset);
            Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1; workspace.Measure(new Size(1300, 900)); workspace.Arrange(new Rect(0, 0, 1300, 900)); workspace.UpdateLayout();
            var grid = Control<DataGrid>(workspace, "PropertyGrid"); grid.SelectedItem = grid.Items.Cast<object>().Single(row => Get<NativeField>(row, "Field").Name == pendingField.Name);
            grid.ScrollIntoView(grid.SelectedItem, grid.Columns[1]); grid.UpdateLayout(); grid.CurrentCell = new(grid.SelectedItem, grid.Columns[1]); Expect(grid.BeginEdit(), "Parent pending ordinary field did not enter edit."); grid.UpdateLayout();
            var pending = grid.Columns[1].GetCellContent(grid.SelectedItem) as TextBox; Expect(pending is not null, "Parent pending ordinary field has no text box."); pending!.Text = "3";
            Expect(!window!.ApplyProperties() && source.CreateMapLevelOptionsDraft().Values["Soundtrack"] == "3" && source.CreateMapGeneralDraft().Values["Name"] == "ParentUI", "Ordinary-field commit reused parent authority."); window.Close(); source.Undo(); Exact();
            Open(); Named<TextBox>("Name").Text = "stale"; source.Paint([new(1, 1)], new(3, 2, 1), "External edit"); source.Undo(); Expect(!window!.ApplyProperties(), "Edit+Undo reused parent authority."); window.Close(); Exact();
            Open(); var general = source.CreateMapGeneralDraft(); var field = general.Record!.Fields.First(row => row.Size > 0);
            Expect(MemoryMarshal.TryGetArray(general.Record.File.Data, out var bytes), "Raw parent fixture unavailable."); var offset = bytes.Offset + general.Record.Offset + field.Offset; var original = bytes.Array![offset]; bytes.Array[offset] ^= 1;
            Expect(!window!.ApplyProperties(), "Raw byte change reused parent authority."); bytes.Array[offset] = original; window.Close(); Exact();
            Open(); workspace.EndMapPropertiesLifetime(); Expect(!window!.ApplyProperties() && workspace.CreateMapPropertiesWindow() is null, "Closed owner retained parent authority."); Exact();
            Expect(disk.All(row => File.ReadAllBytes(row.Key).AsSpan().SequenceEqual(row.Value)), "Properties tests changed source files.");
        }
        finally { window?.Close(); while (source.CanUndo) source.Undo(); workspace.Document = null; shell.Close(); }
        Console.WriteLine($"PASS WPF map properties parent composition, Cancel/atomic Undo, nine anchors, large/crop/stale guards {language} {(native ? "native" : "project")}");
        CheckMapCreatureModifiers(native,language);
    }

    private static void CheckMapPropertiesLayouts(string language, string directory)
    {
        var basis = NativeMapDocument.CreateNewMap(new("Layouts", "Layouts", 20, 20));
        var project = LegacySectionFile.Read(basis.SaveCopy(Path.Combine(directory, "parent-layout-basis-" + language))); var info = project.Sections.Single(row => row.Signature == NativeMapDocument.LegacyInfo);
        foreach (var version in Enumerable.Range(1, 8))
        {
            var structure = version switch { 1 => "MAPPROPERTIESV1", 2 or 3 => "MAPPROPERTIESV2", 4 => "MAPPROPERTIESV4", 5 => "MAPPROPERTIESV5", 6 => "MAPPROPERTIESV6", _ => "MAPPROPERTIES" };
            var bytes = LegacySectionFile.Create(project.Sections.Select(row => row == info ? new LegacySection(info.Signature, version, info.Payload[..LegacyProjectSchemas.Size(structure)]) : row)).ToArray();
            var path = Path.Combine(directory, $"parent-ui-layout-{language}-{version}.dkm"); File.WriteAllBytes(path, bytes); var doc = NativeMapDocument.Load(path);
            if (version == 8) { var rejected = false; try { new MapPropertiesWindow(doc, () => true, () => true, new()).Close(); } catch (InvalidOperationException) { rejected = true; } Expect(rejected, "Future layout inferred writable properties."); continue; }
            var window = new MapPropertiesWindow(doc, () => true, () => true, new());
            try
            {
                Expect(((ComboBox)window.FindName("MapPropertiesVersion")).IsEnabled == (version >= 6), "Historical absent Version became editable.");
                ((TextBox)window.FindName("MapPropertiesName")).Text = "Historical"; Expect(window.ApplyProperties(), "Historical parent Apply failed."); doc.Undo();
                var statisticsBeforeSave=doc.ReadActivityStatistics()!;
                var savedPath=doc.SaveCopy(Path.Combine(directory,$"parent-ui-layout-saved-{language}-{version}"));
                var statisticsAfterSave=doc.ReadActivityStatistics()!;
                Expect(statisticsAfterSave.SavedTimes==unchecked(statisticsBeforeSave.SavedTimes+1)
                    &&statisticsAfterSave.UsageOfUndo==statisticsBeforeSave.UsageOfUndo&&statisticsAfterSave.LastSaved.Year!=0,
                    "Historical SaveCopy lost original statistics effects.");
                var originalSections=LegacySectionFile.Parse(bytes).Sections;var projected=false;
                var expectedSections=originalSections.Select(section=>
                {
                    if(projected||section.Signature!=ActivityStatisticsPayload.Signature)return section;
                    projected=true;return new LegacySection(section.Signature,ActivityStatisticsPayload.Version,statisticsAfterSave.Encode());
                }).ToArray();
                Expect(projected,"Historical fixture needs its original KSTS section.");
                Expect(File.ReadAllBytes(savedPath).AsSpan().SequenceEqual(LegacySectionFile.Create(expectedSections).ToArray()),
                    "Historical parent upgraded or changed bytes beyond its exact captured SaveCopy statistics projection.");
            }
            finally { window.Close(); }
        }
        foreach (var native in new[] { false, true })
        {
            var doc = NativeMapDocument.CreateNewMap(new("Unknown", "Unknown", 20, 20, NativeFormat: native)); var general = doc.CreateMapGeneralDraft();
            doc.SetField(general.Record!, general.Record!.Fields.Single(row => row.Name == MapGeneralSettings.StorageField(doc.IsLegacyProject, "Players")), "255");
            if (!native) doc.SetField(general.Record!, general.Record.Fields.Single(row => row.Name == "Version"), "254");
            var nameField = general.Record.Fields.Single(row => row.Name == MapGeneralSettings.StorageField(doc.IsLegacyProject, "Name"));
            Expect(MemoryMarshal.TryGetArray(general.Record.File.Data, out var backing), "Imported name backing unavailable.");
            for (var index = 0; index < nameField.Size; index++) backing.Array![backing.Offset + general.Record.Offset + nameField.Offset + index] = native && index % 2 == 1 ? (byte)0 : (byte)'A';
            var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
            var window = new MapPropertiesWindow(doc, () => true, () => true, new());
            Expect(((TextBox)window.FindName("MapPropertiesName")).Text.Length == 64 && ((ComboBox)window.FindName("MapPropertiesPlayers")).Items.Count == 6, "Imported full-width name/unknown keeper truncated.");
            var importedCount=doc.EditorChangeCount;
            Expect(window.ApplyProperties() && doc.GetHistory().Undo.Count == history+1 && doc.EditorChangeCount==unchecked(importedCount+1)
                && before.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Imported equal source Apply lost count/history or coerced unknown/padding bytes.");
            window.Close();doc.Undo();Expect(doc.EditorChangeCount==importedCount&&before.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Imported equal Apply Undo lost original bytes/count.");
            window = new(doc, () => true, () => true, new());
            var choice = (ComboBox)window.FindName("MapPropertiesPlayers"); choice.SelectedItem = choice.Items.Cast<object>().Single(row => Get<string>(row, "Value") == "2");
            Expect(window.ApplyProperties() && doc.CreateMapGeneralDraft().Values["Name"].Length == 64 && (native || doc.TargetGameVersion == 254), "Unrelated parent edit coerced imported name/target."); doc.Undo(); Expect(before.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Imported parent Undo changed opaque bytes."); window.Close();
            if (language == "en") foreach (var preset in LegacyCompilerRules.Elements("NewMap").Select(row => row.ID))
            {
                doc = NativeMapDocument.CreateNewMap(new("Background", "Background", 20, 20, FillID: preset, NativeFormat: native));
                window = new(doc, () => true, () => true, new()); ((TextBox)window.FindName("MapPropertiesWidth")).Text = "23"; ((TextBox)window.FindName("MapPropertiesHeight")).Text = "23";
                Expect(window.ApplyProperties() && doc.GetTile(21, 21) == EditorTerrainRules.DecodeFillPreset(preset), "Parent used paint/default fill instead of stored background."); window.Close();
            }
        }
        Console.WriteLine($"PASS WPF map properties historical V1–V7/future refusal, unknown/full-width imports and all source backgrounds {language}");
    }
}
