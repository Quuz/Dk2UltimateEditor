using System.Buffers.Binary;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckThumbnail(NativeMapDocument document,string language,string directory)
    {
        var point = 0;
        foreach (var group in new[] { "World","Rooms","Gates","Walls" }) foreach (var entry in LegacyCompilerRules.Elements(group)) for (byte owner = 1; owner <= 7; owner++)
        { document.Paint([new(point % document.Width,point / document.Width)],new((byte)(entry.ID >> 24),owner,(byte)(entry.ID >> 8))); point++; }
        document.Paint([new(20,20)],new(11,2,1)); document.Undo();
        var workspace = new EditorWorkspace { Document = document }; var host = new Window { Content = workspace };
        workspace.SetSelection(new(2,2,4,4)); workspace.Copy(); var clipboard = workspace.BufferedSelection; var selection = Control<MapCanvas>(workspace,"Canvas").Selection;
        var before = document.Files.ToDictionary(file => file,file => file.Data.ToArray()); var undo = document.CanUndo; var redo = document.CanRedo; var dirty = document.HasChanges; var changes = 0;
        document.Changed += (_,_) => changes++;
        var command = Control<Button>(workspace,"ThumbnailButton"); var proportions = Control<CheckBox>(workspace,"OverviewProportions"); var overview = Control<MapOverview>(workspace,"Overview");
        var persisted = new DK2MapEditor.App.Settings.AppSettings { Language = language,Thumbnail = new(0xABCDEF00,0x12345678,0x123456,true,false,96) };
        var restored = System.Text.Json.JsonSerializer.Deserialize<DK2MapEditor.App.Settings.AppSettings>(System.Text.Json.JsonSerializer.Serialize(persisted))!;
        var settingsShell = new DK2MapEditor.App.MainWindow(restored,persistSettings: false);
        var settingsWorkspace = (EditorWorkspace)settingsShell.FindName("Editor");
        Expect(settingsWorkspace.ThumbnailOptions == persisted.Thumbnail && !Control<MapOverview>(settingsWorkspace,"Overview").KeepProportions,"Stored thumbnail options did not survive JSON/startup.");
        settingsWorkspace.ConfigureThumbnailOptions(new(0xABCDEF00,0x12345678,0xFF123456,true,false,7));
        settingsShell.Close();
        Expect(restored.Thumbnail == new MapThumbnailOptions(0xABCDEF00,0x12345678,0x123456,true,false,144),"Settings normalization/closed-shell capture lost valid fields.");
        Expect(command.IsEnabled && command.Content as string == LocalizationService.Get("Thumbnail.Title"),"Thumbnail command not enabled/localized.");
        var window = workspace.CreateThumbnailWindow();
        T Field<T>(string name) => (T)window.FindName(name);
        void Layout()
        {
            ((FrameworkElement)window.Content).Measure(new Size(730,565)); ((FrameworkElement)window.Content).Arrange(new Rect(0,0,730,565));
            ((FrameworkElement)window.Content).UpdateLayout(); window.Dispatcher.Invoke(() => { },DispatcherPriority.Background); ((FrameworkElement)window.Content).UpdateLayout();
        }
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        void Toggle(CheckBox check,bool value) { check.IsChecked = value; check.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent)); }
        void Matches()
        {
            var expected = MapThumbnail.Render(document,window.Options);
            Expect(window.Preview!.Bgra.Span.SequenceEqual(expected.Bgra.Span),"Actual preview differs from current document/options.");
            var actual = (BitmapSource)Field<Image>("PreviewImage").Source; var pixels = new byte[actual.PixelWidth * actual.PixelHeight * 4]; actual.CopyPixels(pixels,actual.PixelWidth * 4,0);
            Expect(actual.IsFrozen && actual.PixelWidth == window.Options.PreviewSize && pixels.AsSpan().SequenceEqual(expected.Bgra.Span),"Actual preview bitmap size/pixels/cache differs.");
        }
        Layout(); Matches();
        Expect(window.VisibilityControls.Count == 37 && window.VisibilityControls.All(check => check.IsChecked == true) && window.Options == new MapThumbnailOptions()
            && Field<TextBox>("HiddenColourInput").Text == "#000000" && !window.ShowInTaskbar,"Thumbnail source defaults/control count differ.");
        foreach (var check in window.VisibilityControls)
        {
            var choice = (ThumbnailVisibility)check.Tag; var options = window.Options; Toggle(check,false); Matches();
            Expect(choice.PlayerScoped ? window.Options.PlayersFlags == (options.PlayersFlags & ~choice.Mask) && window.Options.WorldFlags == options.WorldFlags
                : window.Options.WorldFlags == (options.WorldFlags & ~choice.Mask) && window.Options.PlayersFlags == options.PlayersFlags,"Actual checkbox changed unrelated flag word/bits.");
            Toggle(check,true); Matches(); Expect(window.Options == options,"Checkbox restoration discarded unknown flag bits/options.");
        }
        var colour = Field<TextBox>("HiddenColourInput"); var apply = Field<Button>("ApplyColourButton"); var refresh = Field<Button>("RefreshButton");
        colour.Text = "#336699"; Click(apply); Matches(); Expect(window.Options.HiddenColour == 0x996633,"RGB UI colour encoded as wrong COLORREF.");
        var mask = window.VisibilityControls.First(check => ((ThumbnailVisibility)check.Tag).Key == "Rock"); Toggle(mask,false);
        var key = Field<CheckBox>("UseColourKey"); Toggle(key,true); Matches();
        var keep = Field<CheckBox>("KeepProportions"); Toggle(keep,false); Matches();
        Expect(!overview.KeepProportions && proportions.IsChecked == false,"Thumbnail proportion preference did not reach actual minimap.");
        typeof(EditorWorkspace).GetField("thumbnailWindow",BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(workspace,window);
        Toggle(proportions,true); Matches(); Expect(window.Options.KeepProportions && keep.IsChecked == true,"Minimap proportion change left thumbnail stale.");
        var size = Field<ComboBox>("SizePicker"); size.SelectedIndex = 1; Layout(); Matches(); Expect(window.Preview!.Width == 96 && window.Options.ExportSize == 96,"96 preview/BMP size incorrect.");
        var path = Path.Combine(directory,$"thumbnail-{language}-{(document.IsLegacyProject ? "project" : "native")}-96.bmp");
        Expect(window.SaveTo(path),"Actual SaveTo refused valid thumbnail.");
        var saved = File.ReadAllBytes(path); Expect(BinaryPrimitives.ReadInt32LittleEndian(saved.AsSpan(18)) == 96 && saved.AsSpan().SequenceEqual(MapThumbnail.Bmp(document,window.Options)),"Published UI BMP differs from selected size/options.");
        try { window.SaveTo(path); throw new Exception("UI thumbnail overwrote output."); } catch (IOException) { }
        Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(saved),"Failed UI export changed existing file.");
        var valid = window.Options; var preview = window.Preview; colour.Text = "#notrgb"; Click(apply);
        Expect(window.Options == valid && ReferenceEquals(window.Preview,preview) && Field<TextBlock>("Status").Text == LocalizationService.Get("Thumbnail.InvalidColour"),"Invalid colour changed preview/options or lacks inline error.");
        var badPath = Path.Combine(directory,$"bad-colour-{language}-{document.IsLegacyProject}.bmp"); Expect(!window.SaveTo(badPath) && !File.Exists(badPath),"Invalid pending colour exported a file.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(window.Title == LocalizationService.Get("Thumbnail.Title") && command.Content as string == LocalizationService.Get("Thumbnail.Title")
            && apply.Content as string == LocalizationService.Get("Thumbnail.ApplyColour") && colour.Text == "#notrgb" && window.Options == valid
            && Field<TextBlock>("Status").Text == LocalizationService.Get("Thumbnail.InvalidColour"),"Live translation discarded thumbnail inputs/settings or left stale status/labels.");
        var worldCheck = window.VisibilityControls.First(check => !((ThumbnailVisibility)check.Tag).PlayerScoped);
        Expect(worldCheck.Content as string == LocalizationService.Get("Thumbnail.Filter.Limit"),"Live translated visibility choice stale.");
        var grid = Field<Grid>("PlayerFlagsGrid");
        Expect(grid.Children.OfType<TextBlock>().All(label => !string.IsNullOrWhiteSpace(label.Text) && label.Foreground is SolidColorBrush textBrush && textBrush.Color.R > 150),"Owner/category headings are missing or dark on dark background.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); colour.Text = "336699"; Click(refresh); Matches();
        size.SelectedIndex = 0; Layout(); Matches();
        var output128 = Path.Combine(directory,$"thumbnail-{language}-{document.IsLegacyProject}-128.bmp"); Expect(window.SaveTo(output128),"128 thumbnail export refused.");
        var bmp128 = File.ReadAllBytes(output128); Expect(BinaryPrimitives.ReadInt32LittleEndian(bmp128.AsSpan(18)) == 128 && window.Preview!.Width == 144,"144 preview was exported as 144 instead of original 128.");
        var decoder = new BmpBitmapDecoder(new Uri(output128),BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
        Expect(decoder.Frames[0].PixelWidth == 128 && decoder.Frames[0].Format.BitsPerPixel == 24,"Actual Windows BMP decoder refuses original export format.");
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]),"Thumbnail options/exports changed map bytes.");
        Expect(undo == document.CanUndo && redo == document.CanRedo && dirty == document.HasChanges && changes == 0 && selection == Control<MapCanvas>(workspace,"Canvas").Selection
            && ReferenceEquals(clipboard,workspace.BufferedSelection),"Thumbnail options/exports changed history/selection/copy buffer.");
        document.Redo(); Layout(); Matches(); document.Undo(); Layout(); Matches();
        document.Resize(66,34,1,1,new(4,2,1)); Layout(); Matches(); document.Undo(); Layout(); Matches();
        if (Environment.GetEnvironmentVariable("DK2_THUMBNAIL_UI_ARTIFACTS") is { Length: > 0 } artifactDirectory)
        {
            Directory.CreateDirectory(artifactDirectory); var rendered = new RenderTargetBitmap(730,565,96,96,PixelFormats.Pbgra32); rendered.Render((Visual)window.Content);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(rendered));
            using var output = new FileStream(Path.Combine(artifactDirectory,$"thumbnail-{language}-{(document.IsLegacyProject ? "project" : "native")}.png"),FileMode.CreateNew); encoder.Save(output);
        }
        var retained = window.Options; window.Close(); Expect(window.Document is null && window.Preview is null && Field<Image>("PreviewImage").Source is null && !Field<Button>("SaveButton").IsEnabled,"Closed thumbnail retains source/export authority.");
        try { window.BindDocument(document);throw new Exception("Closed thumbnail was rebound."); }catch(InvalidOperationException){}
        try { window.SaveTo(Path.Combine(directory,"closed.bmp")); throw new Exception("Closed thumbnail exported."); } catch (InvalidOperationException) { }
        var reopenedWindow = workspace.CreateThumbnailWindow(); Expect(reopenedWindow.Options == retained,"Reopening thumbnail discarded session options.");
        typeof(EditorWorkspace).GetField("thumbnailWindow",BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(workspace,reopenedWindow);
        var replacement=NativeMapDocument.CreateNewMap(new("ThumbnailReplacement","Replacement",22,18,NativeFormat:!document.IsLegacyProject));
        workspace.Document=replacement;
        Expect(ReferenceEquals(reopenedWindow.Document,replacement)&&reopenedWindow.Options==retained&&reopenedWindow.Preview!.Bgra.Span.SequenceEqual(MapThumbnail.Render(replacement,retained).Bgra.Span),"Modeless thumbnail did not follow the new map or lost preferences.");
        var replacementPreview=reopenedWindow.Preview;var oldRevision=document.EditRevision;
        try
        {
            var oldTile=document.GetTile(2,2);
            document.Paint([new(2,2)],oldTile with{Owner=(byte)(oldTile.Owner==2?3:2)});
            Expect(document.EditRevision!=oldRevision,"Detached document probe did not produce a change.");
            Expect(ReferenceEquals(replacementPreview,reopenedWindow.Preview),"Old map subscription survived thumbnail rebind.");
        }
        finally{if(document.EditRevision!=oldRevision)document.Undo();}
        var replacementProportions=(CheckBox)reopenedWindow.FindName("KeepProportions");Toggle(replacementProportions,!retained.KeepProportions);
        Expect(workspace.ThumbnailOptions.KeepProportions==reopenedWindow.Options.KeepProportions,"Rebound thumbnail options no longer reached the workspace.");
        workspace.Document = null; Expect(reopenedWindow.Document is null && !command.IsEnabled,"Document close retained thumbnail source/command.");
        try { workspace.CreateThumbnailWindow(); throw new Exception("No-document thumbnail created."); } catch (InvalidOperationException) { }
        host.Content = null; host.Close();
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} all 37 thumbnail flags, RGB/key/proportions, actual BMP sizes, translation and read-only lifetime");
    }
}
