using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckMapObjectDisplay(bool native, string language)
    {
        var doc = NativeMapDocument.CreateNewMap(new("ObjectDisplay", "ObjectDisplay", 16, 12, NativeFormat: native));
        doc.Paint(MapDrawing.Rectangle(new(1, 1, 14, 10), true), new(8, 3, 1));
        void Add(PlacedThingKind kind, int x, int y, byte owner = 3, int data = 1, uint id = 1)
            => doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(kind) with { Owner = owner, TypeID = id, DataAll = data }, new(Position: new(x, y), Flags: (ObjectBatchFlags)127)), [0]);
        Add(PlacedThingKind.Door, 2, 3, data: 1); Add(PlacedThingKind.Door, 4, 3, data: 2); Add(PlacedThingKind.Door, 6, 3, id: 6);
        Add(PlacedThingKind.Door, 8, 3); Add(PlacedThingKind.Trap, 2, 5, data: 0, id: 4); Add(PlacedThingKind.Trap, 4, 5, data: 3, id: 4);
        Add(PlacedThingKind.Trap, 6, 5, data: 0, id: 4); Add(PlacedThingKind.Object, 10, 3); Add(PlacedThingKind.SpecialItem, 10, 5, id: 117);
        Add(PlacedThingKind.Object, 12, 3); Add(PlacedThingKind.SpecialItem, 12, 5, id: 117);
        for (byte owner = 1; owner <= 7; owner++) Add(PlacedThingKind.Creature, 2, 8, owner);
        Add(PlacedThingKind.Creature, 2, 8); Add(PlacedThingKind.Creature, 6, 8); Add(PlacedThingKind.Creature, 6, 8);
        doc.Paint([new(1, 3), new(3, 3), new(5, 3), new(4, 2), new(4, 4), new(6, 5), new(12, 3), new(12, 5), new(6, 8)], new(2, 2, 1));
        var workspace = new EditorWorkspace { Document = doc }; var host = new Window { Content = workspace };
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var menu = Control<MenuItem>(workspace, "ObjectViewMenu"); canvas.CellSize = 48;
        workspace.SetSelection(new(1, 1, 2, 2)); workspace.Copy(); var buffer = workspace.BufferedSelection;
        var original = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        void Exact() => Expect(original.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.GetHistory().Undo.Count == history && ReferenceEquals(buffer, workspace.BufferedSelection), "Map display changed bytes, history or clipboard.");
        Expect(menu.Items.Count == 8 && workspace.ObjectDisplayOptions.Flags == MapObjectDisplay.DefaultFlags, "Actual view controls/defaults missing.");
        foreach (MenuItem item in menu.Items)
        {
            var flag = (MapObjectViewFlags)item.Tag; var before = workspace.ObjectDisplayOptions.Flags;
            item.IsChecked = (before & flag) == 0; item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Expect(workspace.ObjectDisplayOptions.Flags == (before ^ flag), "View command changed unrelated flags."); Exact();
            item.IsChecked = (before & flag) != 0; item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }
        Expect(canvas.ObjectDisplay!.Marks.Count == 11 && canvas.ObjectDisplay.Creatures.Count == 2
            && canvas.ObjectDisplay.Creatures[0].Owners.SequenceEqual(new[] { 1, 1, 2, 1, 1, 1, 1 }), "Actual canvas lacks source physical projection/owner counts.");
        var beforeSnapshot = canvas.ObjectDisplay;
        doc.Paint([new(8, 3)], new(2, 2, 1));
        Expect(!ReferenceEquals(canvas.ObjectDisplay, beforeSnapshot) && canvas.ObjectDisplay!.Marks.Single(mark => mark.Position == new TilePoint(8, 3)).Solid, "Map edits did not invalidate canvas display snapshot.");
        doc.Undo(); Exact();
        workspace.Measure(new Size(1300, 1000)); workspace.Arrange(new Rect(0, 0, 1300, 1000)); workspace.UpdateLayout();
        RenderTargetBitmap Render()
        {
            canvas.InvalidateVisual(); canvas.UpdateLayout(); var bitmap = new RenderTargetBitmap((int)Math.Ceiling(canvas.ActualWidth), (int)Math.Ceiling(canvas.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(canvas); return bitmap;
        }
        uint Pixel(BitmapSource bitmap, int x, int y)
        {
            var pixels = new byte[4]; bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);
            return (uint)(pixels[2] | pixels[1] << 8 | pixels[0] << 16);
        }
        var shown = Render();
        Expect(Pixel(shown, 10 * 48 + 40, 3 * 48 + 8) == 0x5555FF && Pixel(shown, 12 * 48 + 40, 3 * 48 + 8) == 0,
            "Canvas did not render normal/invalid source object corners.");
        workspace.ConfigureObjectDisplay(new(MapObjectDisplay.DefaultFlags | MapObjectViewFlags.ObjectOwnerColours | MapObjectViewFlags.SpecialOwnerColours));
        var coloured = Render(); Expect(Pixel(coloured, 10 * 48 + 40, 3 * 48 + 8) == MapObjectDisplay.OwnerColour(3), "Owner-colour view did not reach actual pixels."); Exact();
        workspace.ConfigureObjectDisplay(new(MapObjectViewFlags.None)); var hidden = Render();
        Expect(Pixel(hidden, 10 * 48 + 40, 3 * 48 + 8) != 0x5555FF && canvas.ObjectDisplay!.Marks.Count == 11, "Hiding objects destroyed projection or kept glyphs.");
        workspace.ConfigureObjectDisplay(new()); canvas.ShowEntities = false; var masterHidden = Render();
        Expect(Pixel(masterHidden, 10 * 48 + 40, 3 * 48 + 8) == Pixel(hidden, 10 * 48 + 40, 3 * 48 + 8), "Existing entity master toggle stopped hiding object glyphs."); canvas.ShowEntities = true;
        var retained = new MapObjectDisplayOptions(MapObjectViewFlags.Objects | MapObjectViewFlags.ObjectOwnerColours | (MapObjectViewFlags)0x80000000);
        workspace.ConfigureObjectDisplay(retained);
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
        Expect(workspace.ObjectDisplayOptions == retained && menu.Items.Cast<MenuItem>().All(item => item.Header is string label && label != "" && !label.StartsWith("MapObjects.", StringComparison.Ordinal)), "Translation reset view state or exposed resource keys.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Exact();
        var settings = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(new AppSettings { MapObjects = retained }))!;
        var shell = new MainWindow(settings, persistSettings: false); var editor = (EditorWorkspace)shell.FindName("Editor");
        Expect(editor.ObjectDisplayOptions == retained, "Persisted view preferences were not restored by real shell.");
        editor.ConfigureObjectDisplay(new(MapObjectViewFlags.SpecialItems)); shell.Close();
        Expect(settings.MapObjects!.Flags == MapObjectViewFlags.SpecialItems, "Real shell did not capture display preferences on close.");
        workspace.ConfigureObjectDisplay(new());
        workspace.UpdateLayout(); Render();
        if (Environment.GetEnvironmentVariable("DK2_MAP_OBJECT_DISPLAY_ARTIFACTS") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
            var bitmap = new RenderTargetBitmap(1300, 1000, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = new FileStream(Path.Combine(directory, $"objects-{language}-{native}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        workspace.Document = null; Expect(canvas.ObjectDisplay is null && workspace.ObjectDisplayOptions.Flags == MapObjectDisplay.DefaultFlags, "Map close retained object snapshot or discarded preferences.");
        workspace.Document = NativeMapDocument.CreateNewMap(new("Empty", "Empty", 8, 8, NativeFormat: native));
        Expect(canvas.ObjectDisplay is { Marks.Count: 0, Creatures.Count: 0 }, "Map replacement retained old objects."); workspace.Document = null; host.Close(); Exact();
        Console.WriteLine($"PASS WPF source canvas object glyphs, owner counts, view controls and saved preferences {language} {(native ? "native" : "project")}");
    }
}
