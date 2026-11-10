using System.Buffers.Binary;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckInspectorReferences(string directory, string language, bool custom)
    {
        var basis = NativeMapDocument.Create(8, 7, "InspectorUI");
        var root = Path.Combine(directory, $"references-{language}-{custom}"); var entry = basis.SaveCopy(root);
        string tilePath;
        if (custom)
        {
            var descriptor = File.ReadAllBytes(entry);
            var start = 56 + checked((int)BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(52))) + 8;
            var count = (int)(BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(20)) & 0xFFFF);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var codec = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            tilePath = "";
            for (var index = 0; index < count; index++)
            {
                var offset = start + index * 72;
                var old = codec.GetString(descriptor, offset + 8, 64).Split('\0')[0].Replace('\\', Path.DirectorySeparatorChar);
                if (!Path.HasExtension(old)) old += ".kwd";
                var file = basis.Files.SingleOrDefault(file => file.ExportPath.Equals(old, StringComparison.OrdinalIgnoreCase));
                if (file is null) continue;
                var relative = $"références/¡éßœ€_{index}.kld";
                var target = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Move(Path.Combine(root, file.ExportPath), target);
                descriptor.AsSpan(offset + 8, 64).Clear(); codec.GetBytes(relative).CopyTo(descriptor, offset + 8);
                if (file.Signature == 0x64) tilePath = target;
            }
            File.WriteAllBytes(entry, descriptor);
        }
        else
        {
            var copied = Path.Combine(root, "copied"); Directory.CreateDirectory(copied);
            var target = Path.Combine(copied, Path.GetFileName(entry)); File.Copy(entry, target); entry = target;
            foreach (var file in basis.Files.Where(file => file != basis.MainFile))
                File.WriteAllBytes(Path.Combine(copied, Path.GetFileName(file.ExportPath)), new byte[12]);
            tilePath = Path.Combine(root, basis.TileFile.ExportPath);
        }
        var tileBytes = File.ReadAllBytes(tilePath);
        BinaryPrimitives.WriteUInt32LittleEndian(tileBytes.AsSpan(32), 0); File.WriteAllBytes(tilePath, tileBytes);
        var inputs = Directory.GetFiles(root, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
        var window = new MainWindow(new AppSettings(), persistSettings: false);
        try
        {
            window.OpenMapFromPath(entry);
            var content = (FrameworkElement)window.Content; content.Measure(new Size(1400, 900)); content.Arrange(new Rect(0, 0, 1400, 900)); content.UpdateLayout();
            var editor = (EditorWorkspace)window.FindName("Editor"); var document = editor.Document!;
            var status = (TextBlock)window.FindName("StatusText"); var dimensions = (TextBlock)window.FindName("DimensionsValue");
            var list = (ItemsControl)window.FindName("DiagnosticsList");
            Expect(document is not null && document.Width == 8 && document.Height == 7 && dimensions.Text == "8 × 7"
                && status.Text == LocalizationService.Get("Status.Valid"), "Actual shell lost explicitly referenced map dimensions/editing.");
            var warning = list.Items.Cast<object>().Single(item => Get<string>(item, "Code") == "DK2W124");
            Expect(Get<string>(warning, "FileName") == Path.GetFileName(tilePath)
                && Get<string>(warning, "Message") == LocalizationService.Get("Diagnostic.DK2W124"), "Shell warning names a stale companion or is untranslated.");
            Expect(!document!.HasChanges && !document.CanUndo, "Shell reference inspection changed source/history.");

            // Malformed inputs cannot be opened without a native modal warning.
            // Inject read-only inspection results into the actual rendering path;
            // this does not claim automation of failed-open dialogs.
            var original = File.ReadAllBytes(entry); var bad = original.ToArray();
            BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(52), uint.MaxValue); File.WriteAllBytes(entry, bad);
            var invalid = new Dk2FileInspector().Inspect(entry);
            typeof(MainWindow).GetField("inspection", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, invalid);
            typeof(MainWindow).GetMethod("RenderInspection", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
            object Error() => list.Items.Cast<object>().Single(item => Get<string>(item, "Code") == "DK2E113");
            Expect(status.Text == LocalizationService.Get("Status.Invalid") && Get<string>(Error(), "Message") == LocalizationService.Get("Diagnostic.DK2E113")
                && Get<Brush>(Error(), "Color") == (Brush)window.FindResource("Brush.Error"), "Reference-table error is not translated/marked fatal in actual shell.");
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
            Expect(status.Text == LocalizationService.Get("Status.Invalid") && Get<string>(Error(), "Message") == LocalizationService.Get("Diagnostic.DK2E113"), "Live translation retained stale reference error text.");
            LocalizationService.SetLanguage(language); File.WriteAllBytes(entry, original);
            Expect(ReferenceEquals(editor.Document, document) && !document.HasChanges && !document.CanUndo, "Read-only diagnostic rendering changed loaded document.");
            foreach (var (path, bytes) in inputs) Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes), "Shell inspection changed input bytes.");
            Console.WriteLine($"PASS {language} KWD references custom={custom}: actual shell dimensions, resolved warnings, read-only errors and live translation");
        }
        finally { window.Close(); }
    }
}
