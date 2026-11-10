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
    private static void CheckScenarioDescriptions(bool native, string language, string directory)
    {
        var document = NativeMapDocument.CreateNewMap(new("DescriptionUI", "DescriptionUI", 20, 20, NativeFormat: native));
        var ids = new Dictionary<string, ushort>();
        foreach (var type in LegacyEventSchemas.All)
        {
            var structure = type.RecordSignature == 0xD5 ? "Trigger" : "Action";
            var record = native ? document.AddRecord(RecordTemplate.All.Single(template => template.Structure == structure), new(0, 0))
                : document.AddLegacyRecord(structure, new(0, 0));
            document.SetEventType(record, type); ids.Add(type.Name, ScriptGraph.ID(record));
        }
        var variable = ScriptGraph.Build(document).Events[ids["TRIGGER_VARIABLE"]].Record;
        foreach (var (number, value) in new[] { (1, "3"), (2, "7"), (3, "1"), (5, "42") })
            document.SetField(variable, variable.Fields.Single(field => field.Name == (native ? "Param" : "param") + number), value);
        var alias = new string('A', 63); document.SetScenarioName(ScenarioNameKind.Variable, 7, alias);
        var bytes = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var revision = document.EditRevision; var history = document.GetHistory().Undo.Count; var dirty = document.HasChanges;
        var workspace = new EditorWorkspace { Document = document };
        var clipboard = document.CopySelection(new(0, 0, 1, 1)); workspace.LoadSelectionIntoBuffer(clipboard);
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 2;
        workspace.Measure(new Size(1200, 760)); workspace.Arrange(new Rect(0, 0, 1200, 760)); workspace.UpdateLayout();
        var tree = Control<TreeView>(workspace, "ScriptTree");
        IEnumerable<object> Nodes(System.Collections.IEnumerable items)
        {
            foreach (var item in items)
            {
                yield return item;
                foreach (var child in Nodes((System.Collections.IEnumerable)item.GetType().GetProperty("Children")!.GetValue(item)!)) yield return child;
            }
        }
        void AllLabels()
        {
            var descriptions = ScenarioDescriptions.Build(document, LocalizationService.CurrentLanguage);
            var graph = ScriptGraph.Build(document); var nodes = Nodes(tree.Items).ToArray();
            foreach (var item in graph.Events.Values)
            {
                var text = descriptions.Describe(item.Record).Text;
                Expect(nodes.Any(node => Get<ushort>(node, "EventID") == item.ID && Get<string>(node, "Label").Contains(text, StringComparison.Ordinal)), "Actual tree lacks semantic description for " + item.Type?.Name);
            }
        }
        AllLabels();
        Expect(Select(tree, node => Get<ushort>(node, "EventID") == ids["TRIGGER_VARIABLE"]), "Described variable is not selectable.");
        var selected = Get<string>(tree.SelectedItem, "Label");
        Expect(selected.Contains(alias, StringComparison.Ordinal) && selected.Contains("42", StringComparison.Ordinal), "Actual tree lost alias/operand.");
        var targets = Control<ComboBox>(workspace, "ScriptTargetPicker");
        Expect(targets.Items.Cast<object>().Any(item => Get<ushort>(item, "ID") == ids["TRIGGER_VARIABLE"] && Get<string>(item, "Label").Contains(alias, StringComparison.Ordinal)), "Link target choices lack event descriptions.");
        var destinations = Control<ComboBox>(workspace, "ScriptMovePicker");
        Expect(destinations.Items.Cast<object>().Any(item => Get<string>(item, "Label").Contains(alias, StringComparison.Ordinal)), "Move/incoming choices lack event descriptions.");
        Expect(ReferenceEquals(workspace.BufferedSelection, clipboard) && document.EditRevision == revision && document.GetHistory().Undo.Count == history && document.HasChanges == dirty
            && bytes.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Browsing descriptions changed bytes/history/dirty/copy buffer.");
        var imageDirectory = Environment.GetEnvironmentVariable("DK2_SCENARIO_DESCRIPTION_RENDER_DIR") ?? Path.Combine(directory, "scenario-descriptions-render"); Directory.CreateDirectory(imageDirectory);
        IEnumerable<TextBlock> TextBlocks(DependencyObject parent)
        {
            if (parent is TextBlock text) yield return text;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
                foreach (var child in TextBlocks(VisualTreeHelper.GetChild(parent, index))) yield return child;
        }
        foreach (var bounds in new[] { new Size(1200, 760), new Size(900, 600) })
        {
            workspace.Measure(bounds); workspace.Arrange(new Rect(new Point(), bounds)); workspace.UpdateLayout();
            var label = TextBlocks(tree).Single(text => text.Text == Get<string>(tree.SelectedItem, "Label"));
            var labelBounds = label.TransformToAncestor(tree).TransformBounds(new Rect(new Point(), label.RenderSize));
            Expect(labelBounds.Right < tree.ActualWidth - 5 && (bounds.Width > 900 || label.ActualHeight > label.FontSize * 1.5),
                $"Long description geometry failed: viewport={tree.ActualWidth}, right={labelBounds.Right}, height={label.ActualHeight}, font={label.FontSize}, window={bounds.Width}.");
            var bitmap = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = File.Create(Path.Combine(imageDirectory, $"{language}-{(native ? "native" : "project")}-{bounds.Width}.png")); encoder.Save(output);
        }
        document.SetScenarioName(ScenarioNameKind.Variable, 7, "Changed"); workspace.UpdateLayout();
        Expect(Get<string>(tree.SelectedItem, "Label").Contains("Changed", StringComparison.Ordinal), "Alias edit failed to refresh selected tree label.");
        document.Undo(); workspace.UpdateLayout(); Expect(Get<string>(tree.SelectedItem, "Label").Contains(alias, StringComparison.Ordinal), "Undo failed to refresh description.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); AllLabels();
        Expect(Get<string>(tree.SelectedItem, "Label").Contains(language == "en" ? "Если" : "If", StringComparison.Ordinal), "Live language switch failed to refresh full sentence.");
        Expect(ReferenceEquals(workspace.BufferedSelection, clipboard), "Description refresh replaced copy buffer.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); workspace.Document = null;
        Expect(tree.Items.Count == 0 && targets.Items.Count == 0 && destinations.Items.Count == 0, "Closed document retained description choices.");
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} all 103 event tree descriptions, linked choices, alias/Undo/language refresh and read-only copy buffer");
    }
}
