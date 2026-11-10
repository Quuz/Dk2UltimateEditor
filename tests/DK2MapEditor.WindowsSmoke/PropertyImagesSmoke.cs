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
    private static void CheckPropertyImages(bool native, string language)
    {
        var doc = NativeMapDocument.CreateNewMap(new("PropertyImages", "PropertyImages", 12, 12, NativeFormat: native));
        doc.Paint(MapDrawing.Rectangle(new(1, 1, 10, 10), true), new(8, 3, 1));
        var band = native ? doc.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(2, 2)) : doc.AddLegacyRecord("Band", new(2, 2));
        doc.AddBandMember(band, 14); doc.AddBandMember(band, 1);
        foreach (var kind in Enum.GetValues<PlacedThingKind>())
            doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(kind) with { Owner = 3 }, new(Position: new(2 + (int)kind, 4), Flags: (ObjectBatchFlags)127)), [0]);
        var scope = doc.EnsureScriptScope(doc.ScriptOwners().First(owner => owner.MemberIndex == 0));
        doc.InsertScriptEvent(scope.Owner, scope.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER"));
        var workspace = new EditorWorkspace { Document = doc }; var host = new Window { Content = workspace };
        workspace.SetSelection(new(1, 1, 7, 6)); workspace.Copy(); var buffer = workspace.BufferedSelection;
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); var list = Control<ListBox>(workspace, "RecordList");
        var grid = Control<DataGrid>(workspace, "PropertyGrid"); var named = Control<ComboBox>(workspace, "NamedValuePicker");
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        void Exact() => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.GetHistory().Undo.Count == history, "Image workflow changed map or history outside explicit Apply/undo.");
        void SelectRecord(NativeRecord record)
        {
            categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
            list.SelectedItem = list.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").File == record.File && Get<NativeRecord>(item, "Record").Offset == record.Offset);
        }
        void Field(string name) => grid.SelectedItem = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == name);
        void Choose(ObjectImagePickerWindow picker, uint id)
        {
            foreach (TreeViewItem root in ((TreeView)picker.FindName("ObjectImageTree")).Items)
                foreach (TreeViewItem leaf in root.Items) if (leaf.Tag is ObjectImagePickerWindow.Item item && item.ID == id) leaf.IsSelected = true;
            Expect(picker.SelectedID == id && picker.ApplyChoice(), "Source image choice unavailable or stale.");
        }
        SelectRecord(band); Control<ComboBox>(workspace, "BandSlotPicker").SelectedIndex = 1;
        Field(native ? "bGlobalCreatureID" : "heros[0].id");
        Expect(named.Items.Count == 32 && named.ItemTemplate is not null && Control<Button>(workspace, "NamedImagePickerButton").Visibility == Visibility.Visible, "Band member's actual named field lacks source images/32-entry catalogue.");
        var picker = workspace.CreateNamedImagePicker()!;
        Expect(picker.Items.Count == 32 && picker.Items.Select(item => item.Group).Distinct().Count() == 2 && !picker.Items.Any(item => item.ID == 34), "Band image chooser permits elites.");
        Choose(picker, 7); Exact();
        Expect(Get<ValueChoice>(named.SelectedItem, "Choice").Value == "7" && doc.BandMembers(band)[0].CreatureID == 14, "Image choice applied directly instead of choosing a named draft.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
        Expect(Get<ValueChoice>(named.SelectedItem, "Choice").Value == "7", "Translation lost pending named image draft."); LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        Click(Control<StackPanel>(workspace, "NamedValuePanel"), "Editor.ApplyNamedValue");
        Expect(doc.BandMembers(band)[0].CreatureID == 7 && doc.BandMembers(band)[1].CreatureID == 1 && doc.GetHistory().Undo.Count == history + 1, "Actual named image Apply changed adjacent band or did not make one undo.");
        doc.Undo(); Exact();
        var adding = workspace.CreateBandCreatureImagePicker()!; Expect(adding.Items.Count == 32, "Band Add image choice uses ordinary 45-entry list."); Choose(adding, 16);
        Control<ComboBox>(workspace, "BandLevelPicker").SelectedValue = 5; Control<ComboBox>(workspace, "BandEnemyPicker").SelectedValue = 7; Exact();
        Click(Control<StackPanel>(workspace, "BandPanel"), "Editor.AddBandMember");
        Expect(doc.BandMembers(band)[2] is { CreatureID: 16, Active: true, Level: 5, Enemy: 7 } && doc.GetHistory().Undo.Count == history + 1, "Band Add did not use the chosen source type/settings."); doc.Undo(); Exact();

        foreach (var entry in doc.ObjectManagerEntries())
        {
            SelectRecord(entry.Record); var field = ValueCatalogues.PrimaryField(entry.Record)!; Field(field.Name);
            Expect(named.ItemTemplate is not null && named.Items.Cast<object>().All(item => Get<BitmapSource>(item, "Icon").IsFrozen), "Placed type choices lack exact cached image sources.");
            picker = workspace.CreateNamedImagePicker()!;
            var combined = native && entry.Thing.Kind is PlacedThingKind.Object or PlacedThingKind.SpecialItem;
            var expected = combined ? 129 : entry.Thing.Kind switch { PlacedThingKind.Creature => 45, PlacedThingKind.Door => 8, PlacedThingKind.Trap => 14, PlacedThingKind.Object => 116, _ => 13 };
            Expect(picker.Items.Count == expected, "Placed field chooser lost source catalogue or native combined object/special layout.");
            if (combined) Expect(picker.Items.Where(item => item.Group == "Objects").All(item => item.Icon.PixelWidth == 64)
                && picker.Items.Where(item => item.Group.StartsWith("Magics")).All(item => item.Icon.PixelWidth == 32), "Combined chooser mixed image sizes/categories.");
            var target = combined ? 127u : picker.Items.Last().ID; Choose(picker, target); Exact();
            Expect(Get<ValueChoice>(named.SelectedItem, "Choice").Value == target.ToString(), "Image choice did not update numeric named variant.");
            Click(Control<StackPanel>(workspace, "NamedValuePanel"), "Editor.ApplyNamedValue");
            Expect(entry.Record.Read(field) == target.ToString(), "Actual property Apply ignored chosen image ID."); doc.Undo(); Exact();
            SelectRecord(entry.Record); Field(native ? "bGlobalPlayer" : "owner");
            Expect(Control<Button>(workspace, "NamedImagePickerButton").Visibility == Visibility.Collapsed && workspace.CreateNamedImagePicker() is null, "Owner numbers were confused with sprite IDs.");
        }
        // Imported band elites retain a raw/custom choice and icon without being offered as a valid new source member.
        SelectRecord(band); Control<ComboBox>(workspace, "BandSlotPicker").SelectedIndex = 1; Field(native ? "bGlobalCreatureID" : "heros[0].id");
        var member = Get<NativeRecord>(grid.SelectedItem, "Record"); var memberID = Get<NativeField>(grid.SelectedItem, "Field"); doc.SetField(member, memberID, "34");
        var imported = member.File.Data.ToArray();
        Expect(Get<ValueChoice>(named.SelectedItem, "Choice").Value == "34" && named.Items.Count == 33 && Get<BitmapSource>(named.SelectedItem, "Icon").IsFrozen, "Imported elite was dropped/coerced rather than retained as custom.");
        picker = workspace.CreateNamedImagePicker()!; Expect(picker.SelectedID is null && !picker.Items.Any(item => item.ID == 34) && !picker.ApplyChoice(), "Band chooser silently normalized imported elite."); picker.Cancel();
        Expect(member.File.Data.Span.SequenceEqual(imported), "Inspecting a custom band ID changed bytes."); doc.Undo(); Exact();

        var templates = Control<ComboBox>(workspace, "TemplatePicker");
        templates.SelectedItem = templates.Items.Cast<object>().Single(item => native ? Get<RecordTemplate>(item, "Template").Structure == "GAMEOBJECT" : Get<string>(item, "LegacyKind") == "MagicalObject");
        var elements = Control<ComboBox>(workspace, "ElementPicker");
        Expect(elements.ItemTemplate is not null && elements.Items.Count == (native ? 129 : 13), "Add template named-image dropdown lost catalogue.");
        var elementPicker = workspace.CreateElementImagePicker()!; Choose(elementPicker, 127); Exact();
        var painting = workspace.CreateObjectPaintingWindow()!;
        Expect(((ComboBox)painting.FindName("BatchFamily")).SelectedValue as string == "4" && ((TextBox)painting.FindName("BatchTypeID")).Text == "127", "Native combined Add image choice did not reach special-item painting template."); painting.Cancel(); Exact();

        SelectRecord(band); Control<ComboBox>(workspace, "BandSlotPicker").SelectedIndex = 1; Field(native ? "bGlobalCreatureID" : "heros[0].id");
        var stale = workspace.CreateNamedImagePicker()!; Field(native ? "bGlobalLevel" : "heros[0].level"); Expect(!stale.ApplyChoice(), "A changed selected field accepted a stale image choice."); stale.Cancel();
        Field(native ? "bGlobalCreatureID" : "heros[0].id");
        if (Environment.GetEnvironmentVariable("DK2_PROPERTY_IMAGES_UI_ARTIFACTS") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); workspace.Measure(new Size(1400, 900)); workspace.Arrange(new Rect(0, 0, 1400, 900)); workspace.UpdateLayout();
            var bitmap = new RenderTargetBitmap(1400, 900, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = new FileStream(Path.Combine(directory, $"properties-{language}-{native}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        stale = workspace.CreateBandCreatureImagePicker()!; workspace.Document = null; Expect(!stale.ApplyChoice() && stale.Items.Count == 0, "Map close retained a band image callback."); host.Close(); Exact();
        Expect(ReferenceEquals(buffer, workspace.BufferedSelection), "Property image workflows changed copy buffer.");
        Console.WriteLine($"PASS WPF image-backed property/template/band choices, source contexts and explicit edits {language} {(native ? "native" : "project")}");
    }
}
