using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckObjectImages(bool native, string language)
    {
        var repo = AppContext.BaseDirectory; while (!Directory.Exists(Path.Combine(repo, "Source"))) repo = Path.GetDirectoryName(repo)!;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var rc = Encoding.GetEncoding(1252).GetString(File.ReadAllBytes(Path.Combine(repo, "Source/Resources/Editor.rc")));
        var paths = Regex.Matches(rc, "(?m)^(\\d+)\\s+BITMAP\\s+\"([^\"]+)\"")
            .ToDictionary(match => int.Parse(match.Groups[1].Value), match => match.Groups[2].Value.Replace("\\\\", "/"));
        var source = File.ReadAllText(Path.Combine(repo, "Source/Sources/Datas/Datas_Things.c"));
        var kinds = new[] { (PlacedThingKind.Creature, "BadCreatures", 16), (PlacedThingKind.Creature, "BadElites", 13),
            (PlacedThingKind.Creature, "Heros", 16), (PlacedThingKind.Door, "Doors", 8), (PlacedThingKind.Trap, "Traps", 14),
            (PlacedThingKind.Object, "Objects", 116), (PlacedThingKind.SpecialItem, "Magics", 13), (PlacedThingKind.SpecialItem, "MagicsV17", 13) };
        foreach (var (kind, group, count) in kinds)
        {
            var body = Regex.Match(source, @"ELEMENT\s+" + group + @"\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value;
            var independent = Regex.Matches(body, @"\{\s*(0x[0-9A-Fa-f]+)\s*,\s*0x[0-9A-Fa-f]+\s*,\s*(\d+)\s*,")
                .Select(match => (ID: Convert.ToUInt32(match.Groups[1].Value[2..], 16), Resource: int.Parse(match.Groups[2].Value))).Where(item => item.ID != 0).ToArray();
            Expect(independent.Length == count, "Independent source image catalogue count wrong: " + group);
            foreach (var item in independent)
            {
                Expect(OriginalObjectImages.ResourceID(kind, item.ID, group == "Magics" ? 13 : 17) == item.Resource, "Image ID differs from original source: " + group + " / " + item.ID);
                var bitmap = OriginalObjectImages.Get(item.Resource, 64);
                Expect(bitmap.IsFrozen && bitmap.PixelWidth == 64 && bitmap.PixelHeight == 64 && ReferenceEquals(bitmap, OriginalObjectImages.Get(item.Resource, 64)), "Image cache/size/freezing wrong.");
                using var stream = File.OpenRead(Path.Combine(repo, "Source", paths[item.Resource]));
                BitmapSource original = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                if (original.PixelWidth != 64 || original.PixelHeight != 64) original = new TransformedBitmap(original, new ScaleTransform(64d / original.PixelWidth, 64d / original.PixelHeight));
                byte[] Pixels(BitmapSource input)
                {
                    var converted = new FormatConvertedBitmap(input, PixelFormats.Bgra32, null, 0); var bytes = new byte[64 * 64 * 4]; converted.CopyPixels(bytes, 64 * 4, 0); return bytes;
                }
                Expect(Pixels(original).SequenceEqual(Pixels(bitmap)), "Embedded bitmap pixels differ from preserved source: " + item.Resource);
            }
        }
        Expect(OriginalObjectImages.ResourceID(PlacedThingKind.Creature, uint.MaxValue) is null
            && ReferenceEquals(OriginalObjectImages.For(PlacedThingKind.Creature, uint.MaxValue), OriginalObjectImages.Get(1115)), "Unknown ID was coerced to a known item.");
        var doc = NativeMapDocument.CreateNewMap(new("ImageUI", "ImageUI", 12, 12, NativeFormat: native));
        var workspace = new EditorWorkspace { Document = doc }; var host = new Window { Content = workspace };
        workspace.SetSelection(new(1, 1, 4, 4)); workspace.Copy(); var buffer = workspace.BufferedSelection;
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        var window = workspace.CreateObjectBatchWindow()!;
        var family = (ComboBox)window.FindName("BatchFamily"); var idField = (TextBox)window.FindName("BatchTypeID");
        foreach (var kind in Enum.GetValues<PlacedThingKind>())
        {
            family.SelectedValue = ((int)kind).ToString(); idField.Text = "999999";
            var combo = (ComboBox)window.FindName("BatchNamedType"); Expect(combo.ItemTemplate is not null && combo.SelectedItem is null, "Image combo dropped unknown ID or lacks images.");
            var picker = window.CreateImagePicker()!; var tree = (TreeView)picker.FindName("ObjectImageTree");
            var expected = kind switch { PlacedThingKind.Creature => 45, PlacedThingKind.Door => 8, PlacedThingKind.Trap => 14, PlacedThingKind.Object => 116, _ => 13 };
            Expect(picker.Items.Count == expected && picker.SelectedID is null && !picker.ApplyChoice(), "Chooser silently picked a default for an unknown ID.");
            Expect(tree.Items.Count == (kind == PlacedThingKind.Creature ? 3 : 1), "Original creature groups missing.");
            var chosen = kind == PlacedThingKind.Creature ? picker.Items.Single(item => item.ID == 0x22) : picker.Items.Last();
            foreach (TreeViewItem root in tree.Items) foreach (TreeViewItem leaf in root.Items) if (leaf.Tag is ObjectImagePickerWindow.Item item && item.ID == chosen.ID) leaf.IsSelected = true;
            Expect(picker.SelectedID == chosen.ID, "Actual tree selection failed.");
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(picker.SelectedID == chosen.ID && idField.Text == "999999", "Translation applied or lost image choice."); LocalizationService.SetLanguage(language);
            Expect(picker.ApplyChoice() && idField.Text == chosen.ID.ToString() && combo.SelectedItem is not null, "Explicit image choice did not update actual numeric draft.");
            Expect(!picker.ApplyChoice(), "Closed picker can still apply.");
            picker = window.CreateImagePicker()!; ((TextBox)picker.FindName("ObjectImageSearch")).Text = chosen.ID.ToString();
            Expect(picker.SelectedID == chosen.ID, "Search dropped retained choice."); picker.Cancel(); Expect(idField.Text == chosen.ID.ToString(), "Cancel changed draft.");
        }
        family.SelectedValue = "3";
        var rendered = window.CreateImagePicker()!;
        if (Environment.GetEnvironmentVariable("DK2_OBJECT_IMAGES_UI_ARTIFACTS") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); var content = (FrameworkElement)rendered.Content; content.Measure(new Size(550, 620)); content.Arrange(new Rect(0, 0, 550, 620)); content.UpdateLayout();
            var bitmap = new RenderTargetBitmap(550, 620, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = new FileStream(Path.Combine(directory, $"objects-{language}-{native}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        window.Cancel(); Expect(!rendered.ApplyChoice() && rendered.Items.Count == 0, "Parent closure did not revoke chooser.");
        foreach (var version in new[] { 13, 17 })
        {
            var special = new ObjectImagePickerWindow(PlacedThingKind.SpecialItem, version, 127, () => true, _ => { });
            Expect(special.Items.Single(item => item.ID == 127).Label.Contains(version == 13 ? (language == "ru" ? "Убить вражеских бесов" : "Kill Enemy Imps") : (language == "ru" ? "Убить существо" : "Kill a Creature"), StringComparison.OrdinalIgnoreCase), "Version-specific special label lost."); special.Cancel();
        }
        window = workspace.CreateObjectBatchWindow()!; var stale = window.CreateImagePicker()!; workspace.Document = null;
        Expect(!stale.ApplyChoice() && stale.Items.Count == 0, "Map closure retained stale image chooser."); window.Cancel(); host.Close();
        Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.GetHistory().Undo.Count == history && ReferenceEquals(buffer, workspace.BufferedSelection), "Image selection changed map/history/clipboard.");
        Console.WriteLine($"PASS WPF original object images, source mappings/pixels, grouped choice and draft lifetime {language} {(native ? "native" : "project")}");
    }
}
