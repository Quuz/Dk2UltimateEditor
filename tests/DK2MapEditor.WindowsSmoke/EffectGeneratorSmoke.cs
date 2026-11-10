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
    private static void CheckEffectGenerators(NativeMapDocument document, string language)
    {
        var first = document.AddMapRegion(MapRegionKind.Effect, new(2,3,4,5));
        var second = document.AddMapRegion(MapRegionKind.Effect, new(8,9,1,1));
        var area = document.AddMapRegion(MapRegionKind.Area, new(1,1,2,2));
        var gate = document.AddMapRegion(MapRegionKind.Gate, new(12,12,1,1));
        var workspace = new EditorWorkspace { Document = document };
        var tabs = Control<TabControl>(workspace,"WorkspaceTabs");
        var categories = Control<ComboBox>(workspace,"CategoryPicker"); var records = Control<ListBox>(workspace,"RecordList");
        var grid = Control<DataGrid>(workspace,"PropertyGrid"); var panel = Control<Expander>(workspace,"EffectGeneratorPanel");
        var apply = Control<Button>(workspace,"EffectGeneratorApply"); var revert = Control<Button>(workspace,"EffectGeneratorRevert"); var status = Control<TextBlock>(workspace,"EffectGeneratorStatus");
        void Layout(double width = 1500, double height = 1000) { workspace.Measure(new Size(width,height)); workspace.Arrange(new Rect(0,0,width,height)); workspace.UpdateLayout(); }
        void Select(NativeRecord record)
        {
            categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item,"ID") == record.Category);
            records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item,"Record").File == record.File && Get<NativeRecord>(item,"Record").Offset == record.Offset);
            tabs.SelectedIndex = 1; panel.IsExpanded = true; Layout();
        }
        ComboBox Choice(string field) => Control<ComboBox>(workspace,"EffectGenerator" + field);
        TextBox Frequency() => Control<TextBox>(workspace,"EffectGeneratorFrequency");
        Slider Slider() => Control<WrapPanel>(workspace,"EffectGeneratorFields").Children.OfType<StackPanel>().SelectMany(cell => cell.Children.OfType<Slider>()).Single();
        void Choose(string field,string id) { var combo = Choice(field); combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item,"ID") == id); }
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        EffectGeneratorDraft Read(NativeRecord? record = null) => document.CreateEffectGeneratorDraft(record ?? first);
        Dictionary<NativeFile,byte[]> Snapshot() => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]),"Generator UI changed unexpected bytes."); }
        void Field(string name) { grid.SelectedItem = grid.Items.Cast<object>().Single(item => Get<NativeField>(item,"Field").Name == name); }
        var initial = Snapshot(); var changes = 0; document.Changed += (_,_) => changes++;
        // Navigate through the actual map manager, not only a programmatic category selection.
        Control<ComboBox>(workspace,"RegionKindPicker").SelectedValue = (int)MapRegionKind.Effect;
        var regions = Control<ListBox>(workspace,"RegionPicker"); regions.SelectedItem = regions.Items.Cast<object>().Single(item => Get<MapRegion>(item,"Region").Record.Offset == first.Offset);
        Program.Click(Control<StackPanel>(workspace,"RegionPanel"),"Editor.RegionProperties"); panel.IsExpanded = true; Layout();
        Expect(tabs.SelectedIndex == 1 && Get<NativeRecord>(records.SelectedItem,"Record").Offset == first.Offset && panel.Visibility == Visibility.Visible && apply.IsEnabled,"Effect manager properties did not expose generator controls.");
        Expect(Control<WrapPanel>(workspace,"EffectGeneratorFields").Children.Count == 5 && Frequency().Text == "1" && Slider().Minimum == 1 && Slider().Maximum == 255,"Generator/source defaults or controls differ.");
        foreach (var field in EffectGenerators.Fields.Take(4))
        {
            var combo = Choice(field);
            Expect(combo.Items.Count == 371 && Get<string>(combo.SelectedItem,"ID") == "0" && combo.IsEnabled,"Generator slot list missing source IDs/None.");
            foreach (var choice in EffectCatalogue.Choices) { Choose(field,choice.Value); Expect(Get<string>(combo.SelectedItem,"Label").EndsWith(choice.Name(language),StringComparison.Ordinal),"Generator slot missing translated source name."); }
            Expect(combo.Items.Cast<object>().All(item => !Get<string>(item,"Label").Contains("szEffect")),"Internal effect symbols leaked into labels.");
        }
        Exact(initial); Expect(changes == 0,"Browsing generator slots wrote data."); Click(revert);
        Frequency().Text = "127"; Expect(Slider().Value == 127,"Frequency input did not synchronize slider."); Slider().Value = 255; Expect(Frequency().Text == "255","Slider did not synchronize integer input.");
        Choose("Effect1","370"); Choose("Effect2","55"); Choose("Effect3","256"); Choose("Effect4","1");
        Frequency().Text = "0"; Click(apply); Exact(initial); Expect(status.Text == LocalizationService.Get("Editor.EffectGenerator.Failed") && changes == 0,"Invalid frequency partly applied four slots.");
        Frequency().Text = "255"; var label = Get<string>(Choice("Effect1").SelectedItem,"Label");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(Get<string>(Choice("Effect1").SelectedItem,"ID") == "370" && Get<string>(Choice("Effect1").SelectedItem,"Label") != label && Frequency().Text == "255","Live translation lost generator draft.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout(); Exact(initial);
        Click(apply); Expect(changes == 1 && EffectGenerators.Fields.Select(field => Read().Raw(field)).SequenceEqual(new[] { "370","55","256","1","255" }),"Whole generator draft did not apply as one edit.");
        Expect(document.GetMapRegion(first)!.Bounds == new TileRect(2,3,4,5) && Read(second).Raw("Frequency") == "1","Generator controls changed geometry/another generator.");
        var full = Snapshot(); Click(apply); Exact(full); Expect(changes == 1 && !revert.IsEnabled,"Generator no-op created history/draft.");
        Choose("Effect1","0"); Frequency().Text = "2"; Click(revert); Exact(full); Expect(Get<string>(Choice("Effect1").SelectedItem,"ID") == "370" && Frequency().Text == "255" && status.Text == LocalizationService.Get("Editor.EffectGenerator.Reverted"),"Revert wrote/retained generator draft or reported an edit.");
        workspace.Undo(); Exact(initial); workspace.Redo(); Exact(full);
        // The generic named picker must map effect types, never generator identity.
        foreach (var field in EffectGenerators.Fields.Take(4))
        {
            Field(EffectGenerators.StorageField(first,field)); Expect(Control<StackPanel>(workspace,"NamedValuePanel").Visibility == Visibility.Visible && Control<ComboBox>(workspace,"NamedValuePicker").Items.Count == 371,"Generic generator field missing named choices.");
        }
        Field(document.IsLegacyProject ? "id" : "bEffectID"); Expect(Control<StackPanel>(workspace,"NamedValuePanel").Visibility == Visibility.Collapsed,"Generator identity confused with effect type.");
        Field(EffectGenerators.StorageField(first,"Effect1")); var search = Control<TextBox>(workspace,"NamedSearch"); search.Text = "370";
        Expect(Control<ComboBox>(workspace,"NamedValuePicker").Items.Count == 1 && Get<ValueChoice>(Control<ComboBox>(workspace,"NamedValuePicker").SelectedItem,"Choice").Value == "370","Effect named search lost numeric ID."); search.Text = "";
        var named = Control<ComboBox>(workspace,"NamedValuePicker"); named.SelectedItem = named.Items.Cast<object>().Single(item => Get<ValueChoice>(item,"Choice").Value == "2");
        Program.Click(Control<StackPanel>(workspace,"NamedValuePanel"),"Editor.ApplyNamedValue"); Expect(Read().Raw("Effect1") == "2" && Get<string>(Choice("Effect1").SelectedItem,"ID") == "2","Generic named apply did not refresh generator slot."); workspace.Undo(); Exact(full);
        // A pending raw identity edit invalidates a compound draft after committing it.
        Field(document.IsLegacyProject ? "id" : "bEffectID"); var property = grid.SelectedItem; grid.ScrollIntoView(property,grid.Columns[1]); Layout(); grid.CurrentCell = new(property,grid.Columns[1]);
        Expect(grid.BeginEdit(),"Cannot begin raw generator identity edit."); Layout(); ((TextBox)grid.Columns[1].GetCellContent(property)).Text = "7";
        Choose("Effect1","2"); Click(apply); workspace.Dispatcher.Invoke(() => {},System.Windows.Threading.DispatcherPriority.Background);
        Expect(Read().Raw("Effect1") == "370" && first.Read(first.Fields.Single(field => field.Name == (document.IsLegacyProject ? "id" : "bEffectID"))) == "7" && status.Text == LocalizationService.Get("Editor.EffectGenerator.Failed"),"Pending raw commit saved stale generator draft."); workspace.Undo(); Exact(full);
        var unknown = document.IsLegacyProject ? new[] { "-1","-2147483648","2147483647","65536","-2147483648" } : new[] { "65535","65534","65533","65532","0" };
        document.Edit("Imported generator fixture",() => { for (var index = 0; index < 5; index++) document.SetField(first,first.Fields.Single(field => field.Name == EffectGenerators.StorageField(first,EffectGenerators.Fields[index])),unknown[index]); });
        Layout(); var imported = Snapshot(); var count = changes;
        for (var index = 0; index < 4; index++) Expect(Get<string>(Choice(EffectGenerators.Fields[index]).SelectedItem,"ID") == unknown[index] && Choice(EffectGenerators.Fields[index]).Items.Count == 372,"Imported unknown effect disappeared/guessed.");
        Expect(Frequency().Text == unknown[4] && Slider().Value == 1,"Slider normalized imported frequency."); Click(apply); Exact(imported); Expect(changes == count,"Unknown generator no-op normalized bytes.");
        Choose("Effect1","0"); Click(apply); Expect(Read().Raw("Effect1") == "0" && EffectGenerators.Fields.Skip(1).Select(field => Read().Raw(field)).SequenceEqual(unknown.Skip(1)),"Repairing one effect normalized other imported values.");
        workspace.Undo(); Exact(imported); workspace.Undo(); Exact(full);
        Choose("Effect1","2"); Select(second); Expect(Get<string>(Choice("Effect1").SelectedItem,"ID") == "0" && Frequency().Text == "1","Switching generators leaked another draft."); Select(first); Expect(Get<string>(Choice("Effect1").SelectedItem,"ID") == "370","Returning to generator resurrected discarded draft.");
        Select(area); Expect(panel.Visibility == Visibility.Collapsed && workspace.FindName("EffectGeneratorEffect1") is null,"Area exposed generator controls."); Select(gate); Expect(panel.Visibility == Visibility.Collapsed,"Gate exposed generator controls."); Select(first);
        Layout(980,500); var scroll = Control<ScrollViewer>(workspace,"PropertiesTopScroll"); Expect(grid.ActualHeight >= 100 && scroll.ScrollableHeight > 0,"Generator panel displaced property grid in small window.");
        scroll.ScrollToEnd(); workspace.UpdateLayout(); var bounds = Frequency().TransformToAncestor(scroll).TransformBounds(new Rect(new Point(),Frequency().RenderSize));
        Expect(bounds.Top >= 0 && bounds.Bottom <= scroll.ActualHeight + 1,"Generator frequency cannot be reached by scrolling.");
        if (Environment.GetEnvironmentVariable("DK2_EFFECT_GENERATOR_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts); Render(980,500,"small"); scroll.ScrollToHome(); Layout(); Render(1500,1000,"full");
            void Render(int width,int height,string size)
            {
                var bitmap = new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32); bitmap.Render(workspace); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts,$"generator-{language}-{(document.IsLegacyProject ? "project" : "native")}-{size}.png")); encoder.Save(output);
            }
        }
        Exact(full);
        if (!document.IsLegacyProject)
        {
            var root = Path.Combine(Path.GetTempPath(),"DK2GeneratorUnknownUI-" + Guid.NewGuid().ToString("N"));
            try
            {
                var entry = document.SaveCopy(root); var path = Path.Combine(root,first.File.ExportPath); var bytes = File.ReadAllBytes(path); var end = first.Offset + first.Length;
                var extended = bytes.AsSpan(0,end).ToArray().Concat(new byte[] { 0xF1,0xF2,0xF3,0xF4 }).Concat(bytes.AsSpan(end).ToArray()).ToArray();
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(extended.AsSpan(first.Offset + 4),40); File.WriteAllBytes(path,extended);
                var unknownDocument = NativeMapDocument.Load(entry); workspace.Document = unknownDocument; var unknownRecord = unknownDocument.Records().Single(record => record.Category == "GAMEEFFECT" && record.Offset == first.Offset); Select(unknownRecord);
                Expect(panel.Visibility == Visibility.Visible && !apply.IsEnabled && !revert.IsEnabled && !Frequency().IsEnabled && Frequency().Text == "" && EffectGenerators.Fields.Take(4).All(field => !Choice(field).IsEnabled && Choice(field).SelectedItem is null),"Unknown generator variant inferred editable prefix controls.");
                Click(apply); Expect(unknownDocument.Files.Single(file => file.ExportPath == first.File.ExportPath).Data.Span.SequenceEqual(extended),"Disabled unknown generator apply rewrote opaque data.");
            }
            finally { workspace.Document = null; if (Directory.Exists(root)) Directory.Delete(root,true); }
        }
        workspace.Document = NativeMapDocument.Create(20,20,"Replacement"); Layout(); Expect(panel.Visibility == Visibility.Collapsed && workspace.FindName("EffectGeneratorFrequency") is null,"Document replacement leaked generator draft/controls.");
        workspace.Document = null; Layout(); Expect(panel.Visibility == Visibility.Collapsed && !apply.IsEnabled && !revert.IsEnabled && Control<WrapPanel>(workspace,"EffectGeneratorFields").Children.Count == 0,"Closing document retained generator controls.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} generator UI: four 371-choice slots, integer frequency/slider, atomic apply/undo, unknown retention, draft translation, pending raw commit, manager navigation and scrolling");
    }
}
