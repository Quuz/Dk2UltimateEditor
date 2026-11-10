using System.Buffers.Binary;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckNativeTileLayout(string directory,string language,uint storedSize,bool undersizedFile = false)
    {
        var basis = NativeMapDocument.Create(8,7,"TileLayoutUI"); var root = Path.Combine(directory,$"tiles-{language}-{storedSize}-{undersizedFile}"); var entry = basis.SaveCopy(root);
        byte[] tail = [0xA1,0,0xFF,0x52,0x37]; var path = Path.Combine(root,basis.TileFile.ExportPath); var bytes = basis.TileFile.Data.ToArray().Concat(tail).ToArray();
        var fileSize = undersizedFile ? 36u : (uint)bytes.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8),fileSize); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32),storedSize); File.WriteAllBytes(path,bytes);
        var inspection = new Dk2FileInspector().Inspect(entry);
        Expect(inspection.IsValid && inspection.Diagnostics.Count(item => item.Code == "DK2W141") == (undersizedFile ? 1 : 0),"Native tile-size inspector emitted a false error or duplicate file-size warnings.");
        if (undersizedFile)
        {
            var window = new MainWindow(new AppSettings(),persistSettings: false);
            try
            {
                window.OpenMapFromPath(entry); var content = (FrameworkElement)window.Content; content.Measure(new Size(1400,900)); content.Arrange(new Rect(0,0,1400,900)); content.UpdateLayout();
                var shellEditor = (EditorWorkspace)window.FindName("Editor"); var status = (TextBlock)window.FindName("StatusText"); var diagnostics = (ItemsControl)window.FindName("DiagnosticsList");
                Expect(shellEditor.Document is not null && shellEditor.Visibility == Visibility.Visible && status.Text == LocalizationService.Get("Status.Valid"),"Shell marked a complete source-compatible tile file invalid or failed to enable editing.");
                foreach (var code in new[] { "DK2W124","DK2W141" })
                    Expect(diagnostics.Items.Cast<object>().Single(item => Get<string>(item,"Code") == code) is { } warning && Get<string>(warning,"Message") == LocalizationService.Get("Diagnostic." + code),"Native size warning not translated in actual shell.");
                LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
                Expect(status.Text == LocalizationService.Get("Status.Valid") && diagnostics.Items.Cast<object>().All(item => Get<string>(item,"Message") == LocalizationService.Get("Diagnostic." + Get<string>(item,"Code"))),"Live shell translation lost native size warnings.");
                LocalizationService.SetLanguage(language);
            }
            finally { window.Close(); }
        }
        var document = NativeMapDocument.Load(entry); var workspace = new EditorWorkspace { Document = document };
        void Layout() { workspace.Measure(new Size(1200,850)); workspace.Arrange(new Rect(0,0,1200,850)); workspace.UpdateLayout(); }
        Layout(); var canvas = Control<MapCanvas>(workspace,"Canvas"); var initial = document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        void Exact() { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(initial[file]),"Native tile UI lost header/tail/unrelated bytes."); }
        Control<ComboBox>(workspace,"TerrainPicker").SelectedValue = 4; canvas.PaintStroke([new(2,2)]);
        Expect(document.GetTile(2,2).Terrain == 4 && BinaryPrimitives.ReadUInt32LittleEndian(document.TileFile.Data.Span[8..]) == fileSize && BinaryPrimitives.ReadUInt32LittleEndian(document.TileFile.Data.Span[32..]) == storedSize && document.TileFile.Data.Span[(36 + 8 * 7 * 4)..].SequenceEqual(tail),"Actual canvas normalized stored size or painted opaque tail.");
        workspace.Undo(); Exact(); LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout(); Exact(); LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        document.Resize(9,8,1,1,new(2,2,1)); Layout(); Expect(document.Width == 9 && document.Height == 8 && document.TileFile.Data.Span[(36 + 9 * 8 * 4)..].SequenceEqual(tail) && canvas.RenderSize.Width > 0,"Resizing did not refresh canvas/preserve opaque tile tail.");
        var resized = document.TileFile.Data.ToArray(); workspace.Undo(); Exact(); workspace.Redo(); Expect(document.TileFile.Data.Span.SequenceEqual(resized),"Actual workspace redo lost tile metadata/tail.");
        var reopened = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,$"tile-copy-{language}-{storedSize}-{undersizedFile}"))); Expect(reopened.TileFile.Data.Span.SequenceEqual(resized),"Edited UI tile layout did not save/reopen exactly.");
        workspace.Undo(); Exact(); workspace.Document = null; Layout(); Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes),"Canvas load/edit/export changed input file.");
        Console.WriteLine($"PASS {language} native tile stored-size={storedSize} undersized-file={undersizedFile}: shell/inspector, real canvas painting, opaque tail, live translation, resize/undo/redo and save/reopen");
    }
}
