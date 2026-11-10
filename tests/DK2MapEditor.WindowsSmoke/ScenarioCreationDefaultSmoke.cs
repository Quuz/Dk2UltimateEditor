using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioCreationDefaults(bool native, string language, string directory)
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioCreationDefaults.json")!;
        using var fixture = JsonDocument.Parse(stream); var oracle = fixture.RootElement;
        using var pagesStream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioParameterPages.json")!;
        using var pageFixture = JsonDocument.Parse(pagesStream);
        var pages = pageFixture.RootElement.EnumerateArray().ToDictionary(item => item.GetProperty("Type").GetString()!, item => item);
        var defaults = oracle.GetProperty("Pages").EnumerateArray().ToDictionary(item => item.GetProperty("Type").GetString()!,
            item => Enumerable.Range(1, 8).Select(number => unchecked((uint)item.GetProperty("Values").GetProperty(number.ToString()).GetInt64())).ToArray());
        var zero = oracle.GetProperty("CreationAndSaveFlow").EnumerateObject().SelectMany(item => item.Value.GetProperty("ZeroOnSaveTypes").EnumerateArray().Select(value => value.GetString()!)).ToHashSet();
        var document = NativeMapDocument.CreateNewMap(new("DefaultsUI", "DefaultsUI", 20, 20, NativeFormat: native)); document.SetHistoryDepth(65535);
        var workspace = new EditorWorkspace { Document = document };
        workspace.ScenarioCreationPresenter = CompleteDefaultCreation;
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var templates = Control<ComboBox>(workspace, "TemplatePicker");
        var addButton = ((WrapPanel)templates.Parent).Children.OfType<Button>().Single(button => button.Content?.ToString() == DK2MapEditor.App.Localization.LocalizationService.Get("Editor.AddRecord"));
        var picker = Control<ComboBox>(workspace, "EventTypePicker"); var records = Control<ListBox>(workspace, "RecordList");
        NativeRecord Current() => Get<NativeRecord>(records.SelectedItem, "Record");
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(bytes.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "UI event type command wrote unexpected parameters, links, names or other data.");
        var choices = 0;
        foreach (var (kind, signature) in new[] { ("Trigger", 0xD5u), ("Action", 0xD6u) })
        {
            templates.SelectedItem = templates.Items.Cast<object>().Single(item => native
                ? Get<RecordTemplate>(item, "Template").Structure == kind : Get<string>(item, "LegacyKind") == kind);
            var count = document.GetHistory().Undo.Count; var before = Snapshot();
            addButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var record = Current(); var initial = oracle.GetProperty("CreationAndSaveFlow").GetProperty(kind);
            var initialName = initial.GetProperty("InitialType").GetString()!;
            Expect(EntityReferences.EventType(record)?.Name == initialName && document.GetHistory().Undo.Count == count + 1, "Ordinary Add did not create the source initial type in one edit.");
            var expected = Snapshot(); Write(expected, record, initialName, initial.GetProperty("InitialValues").EnumerateArray().Select(value => value.GetUInt32()).ToArray()); Exact(expected);
            Expect(record.File.Data.Span[record.Offset + (native ? 23 : 55)] == 1, "Ordinary Add did not set source once-only repeat.");
            document.Undo(); Exact(before); document.Redo(); Exact(expected);
            var categories = Control<ComboBox>(workspace, "CategoryPicker");
            categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
            records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").File == record.File && Get<NativeRecord>(item, "Record").Offset == record.Offset);
            foreach (var type in LegacyEventSchemas.All.Where(item => item.RecordSignature == signature).OrderBy(item => item.Type))
            {
                record = Current(); before = Snapshot(); count = document.GetHistory().Undo.Count;
                var oldName = EntityReferences.EventType(record)!.Name; var values = Logical(record, oldName);
                if (oldName != type.Name)
                {
                    if (defaults.TryGetValue(type.Name, out var defined)) values = defined;
                    if (zero.Contains(type.Name)) values = new uint[8];
                }
                expected = before.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone());
                if (oldName != type.Name) Write(expected, record, type.Name, values);
                var confirmations = 0; workspace.ScenarioEventTypeConfirmer = () => { confirmations++; return true; };
                picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<LegacyEventSchemas.EventType>(item, "Type").Name == type.Name);
                Exact(expected); Expect(EntityReferences.EventType(Current())?.Name == type.Name, "Actual event type picker did not apply the requested type.");
                Expect(confirmations == (oldName == type.Name ? 0 : 1) && document.GetHistory().Undo.Count == count + (oldName == type.Name ? 0 : 1), "Type picker confirmation/history semantics differ.");
                var after = Snapshot(); var revision = document.EditRevision;
                Expect(workspace.ChangeSelectedScenarioEventType(type, () => throw new InvalidOperationException("Same type asked for confirmation.")), "Same type was rejected.");
                Exact(after); Expect(document.EditRevision == revision, "Same type changed the document."); choices++;
            }
        }
        Expect(choices == 103, "All 103 ordinary type choices were not exercised.");
        var addType = LegacyEventSchemas.All.Single(item => item.Name == "ACTION_ADDCREATURE");
        workspace.ScenarioEventTypeConfirmer = () => false; var baseline = Snapshot(); var history = document.GetHistory().Undo.Count;
        picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<LegacyEventSchemas.EventType>(item, "Type").Name == addType.Name);
        Exact(baseline); Expect(document.GetHistory().Undo.Count == history, "Declined actual type choice changed history.");
        workspace.ScenarioEventTypeConfirmer = () => true;
        picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<LegacyEventSchemas.EventType>(item, "Type").Name == addType.Name);
        var parent = workspace.CreateScenarioParameterWindow(); Expect(parent is not null, "Freshly selected spawn type has no parameter window.");
        var flag = (TextBox)parent!.FindName("ScenarioParam4"); Expect(flag.Text == "41", "Fresh spawn flags show compiler zero instead of source default 41.");
        foreach (var number in new[] { 5, 7 }) Expect(((TextBox)parent.FindName("ScenarioParam" + number)).Text == "1", "Fresh spawn coordinates differ from source defaults.");
        foreach (var width in new[] { 700, 540 }) Render((FrameworkElement)parent.Content, "spawn", width, width == 700 ? 580 : 420);
        parent.Close();
        var timer = LegacyEventSchemas.All.Single(item => item.Name == "ACTION_SETTIMER");
        baseline = Snapshot(); history = document.GetHistory().Undo.Count;
        Expect(!workspace.ChangeSelectedScenarioEventType(timer, () => false), "Declined direct confirmation applied."); Exact(baseline);
        try
        {
            workspace.ChangeSelectedScenarioEventType(timer, () =>
            {
                Expect(!workspace.ChangeSelectedScenarioEventType(timer, () => true), "Reentrant type choice was accepted.");
                var record = Current(); var repeat = record.Fields.Single(field => field.Name == (native ? "Repeat" : "one"));
                document.SetField(record, repeat, "254"); document.Undo(); return true;
            });
            throw new InvalidOperationException("Edit+Undo during type confirmation was accepted.");
        }
        catch (InvalidOperationException error) when (error.Message == "The document changed after selecting an event type.") { }
        Exact(baseline); Expect(document.GetHistory().Undo.Count == history, "Stale confirmation wrote a type change.");
        foreach (var width in new[] { 1100, 800 }) Render(workspace, "workspace", width, 760);
        Expect(!workspace.ChangeSelectedScenarioEventType(timer, () => { workspace.EndScenarioNamesLifetime(); return true; }), "Closed owner accepted the type reply."); Exact(baseline);
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} source initial Add, all 103 actual type choices, independent default bytes, same-type retention, Cancel, private spawn display and stale/reentrant/closed reply guards");

        uint[] Logical(NativeRecord record, string name)
        {
            var result = new uint[8];
            if (!native) for (var number = 0; number < 8; number++) result[number] = BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[(record.Offset + 16 + number * 4)..]);
            else foreach (var field in pages[name].GetProperty("Native").EnumerateArray())
            {
                var fieldName = field.GetProperty("Name").GetString()!; if (!fieldName.StartsWith("Param", StringComparison.Ordinal)) continue;
                var value = record.File.Data.Span[(record.Offset + field.GetProperty("Offset").GetInt32())..]; var size = field.GetProperty("Size").GetInt32();
                result[int.Parse(fieldName[5..], CultureInfo.InvariantCulture) - 1] = size == 1 ? value[0] : size == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(value) : BinaryPrimitives.ReadUInt32LittleEndian(value);
            }
            return result;
        }
        void Write(Dictionary<NativeFile, byte[]> expected, NativeRecord record, string name, uint[] values)
        {
            var bytes = expected[record.File]; var page = pages[name];
            if (!native) for (var number = 0; number < 8; number++) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(record.Offset + 16 + number * 4), values[number]);
            else
            {
                var payload = oracle.GetProperty("CompilerPadding").GetProperty(name).EnumerateArray().Select(value => value.GetByte()).ToArray();
                foreach (var field in page.GetProperty("Native").EnumerateArray())
                {
                    var fieldName = field.GetProperty("Name").GetString()!; if (!fieldName.StartsWith("Param", StringComparison.Ordinal)) continue;
                    var value = values[int.Parse(fieldName[5..], CultureInfo.InvariantCulture) - 1]; var offset = field.GetProperty("Offset").GetInt32() - 8; var size = field.GetProperty("Size").GetInt32();
                    if (size == 1) payload[offset] = unchecked((byte)value); else if (size == 2) BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(offset), unchecked((ushort)value)); else BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(offset), value);
                }
                if (name == "ACTION_SHOWCRYSTAL") { BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4), 65535); BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6), 65535); }
                payload.CopyTo(bytes, record.Offset + 8);
            }
            bytes[record.Offset + (native ? 22 : 54)] = page.GetProperty("ID").GetByte();
        }
        void Render(FrameworkElement content, string state, int width, int height)
        {
            var target = Environment.GetEnvironmentVariable("DK2_SCENARIO_DEFAULTS_RENDER_DIR") ?? directory; Directory.CreateDirectory(target);
            content.Measure(new(width, height)); content.Arrange(new(0, 0, width, height)); content.UpdateLayout();
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = new FileStream(Path.Combine(target, $"defaults-{state}-{language}-{native}-{width}.png"), FileMode.CreateNew); encoder.Save(output);
        }
    }
}
