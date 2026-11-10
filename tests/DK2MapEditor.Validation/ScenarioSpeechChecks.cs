using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    public static void RunSpeechPage()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioSpeechSource.json")!;
        using var fixture = JsonDocument.Parse(stream); var source = fixture.RootElement;
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Source"))) root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Original speech source missing.");
        foreach (var input in source.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original speech source changed.");
        Expect(source.GetProperty("PageID").GetInt32() == 7211 && source.GetProperty("NumericInput").GetProperty("DisplayMaximum").GetInt32() == 9999
            && source.GetProperty("SpeechIdentifiersStartAt").GetInt32() == 1 && source.GetProperty("Checkbox").GetProperty("CheckedValue").GetInt32() == 1, "Independent SAY controls are not captured.");
        Expect(source.GetProperty("OriginalPhases").GetProperty("Callbacks").GetString()!.Contains("LBN_DBLCLK")
            && source.GetProperty("OriginalHelpers").GetProperty("SoundListAndPlayback").GetString()!.Contains("Table[(Identifier-1)]"), "Speech selection/audio index contract differs from original.");
        Expect(pages.Count(page => page.PageID != -1 && ScenarioControlPages.Supports(page.Type)) == 69, "Speech did not complete all69 source parameter pages.");
        var page = pages.Single(page => page.Type == "ACTION_SAY");
        Expect(ScenarioControlPages.Controls(page.Type).Select(control => (control.Parameter, control.Maximum, control.Unsigned))
            .SequenceEqual(new[] { (1, 9999L, true), (5, 1L, true) }), "Speech operands differ from original.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2Say-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false, true })
            {
                var doc = NativeMapDocument.CreateNewMap(new("Say", "Say", 20, 20, NativeFormat: native)); doc.SetHistoryDepth(65535);
                var record = Add(doc, native, page); string Name(int n) => (native ? "Param" : "param") + n;
                var before = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
                Expect(ScenarioControlPages.MissingRequirements(page.Type, doc).Count == 0, "Speech invented an audio-bank prerequisite.");
                doc.ApplyScenarioEditorParameterDraft(doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [])); Exact(before);
                Expect(doc.GetHistory().Undo.Count == history, "Opening/empty Save normalized imported speech bytes.");
                foreach (var id in new[] { 0u, 1u, 2u, 9999u })
                foreach (var flag in new[] { 0u, 1u })
                {
                    var expected = before.ToDictionary(item => item.Key, item => item.Value.ToArray());
                    if (native)
                    { BinaryPrimitives.WriteUInt16LittleEndian(expected[record.File].AsSpan(record.Offset + 8), (ushort)id); expected[record.File][record.Offset + 12] = (byte)flag; }
                    else
                    { BinaryPrimitives.WriteUInt32LittleEndian(expected[record.File].AsSpan(record.Offset + 16), id); BinaryPrimitives.WriteUInt32LittleEndian(expected[record.File].AsSpan(record.Offset + 32), flag); }
                    var draft = doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(1), id.ToString(CultureInfo.InvariantCulture)), new(Name(5), flag.ToString(CultureInfo.InvariantCulture))]);
                    Exact(before); doc.ApplyScenarioEditorParameterDraft(draft); Exact(expected);
                    Expect(doc.GetHistory().Undo.Count == history + 1, "Speech ID/checkbox did not create one edit.");
                    doc.ApplyScenarioEditorParameterDraft(doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(1), id.ToString()), new(Name(5), flag.ToString())]));
                    Expect(doc.GetHistory().Undo.Count == history + 1, "Identical speech controls added history.");
                    var saved = doc.SaveCopy(Path.Combine(directory, $"say-{native}-{id}-{flag}")); var reopened = NativeMapDocument.Load(saved);
                    var row = reopened.Records().Single(item => EntityReferences.EventType(item)?.Name == page.Type);
                    Expect(reopened.CreateScenarioParameterDraft(row).Values.SequenceEqual(doc.CreateScenarioParameterDraft(record).Values), "Speech operands changed on reopen.");
                    if (!native)
                    {
                        var compiled = doc.CompileLegacyProject("SayCompiled", NativeMapDocument.Create(3, 3, "Basis"));
                        var action = compiled.Records().Single(item => EntityReferences.EventType(item)?.Name == page.Type);
                        Expect(action.File.Data.Span.Slice(action.Offset + 8, 8).SequenceEqual(new byte[] { (byte)id, (byte)(id >> 8), 0, 0, (byte)flag, 0, 0, 0 }), "Speech compiler differs from original WORD/zero/zero/BYTE/zeros.");
                    }
                    doc.Undo(); Exact(before); doc.Redo(); Exact(expected); doc.Undo(); Exact(before);
                }
                foreach (var invalid in new[] { "-1", "10000", "4294967295", "x" }) Reject(() => doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(1), invalid)]));
                foreach (var invalid in new[] { "-1", "2", "256" }) Reject(() => doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(5), invalid)]));
                var stale = doc.CreateScenarioParameterDraft(record); doc.SetField(record, record.Fields.Single(field => field.Name == Name(1)), "1"); doc.Undo();
                Reject(() => doc.UpdateScenarioControlPage(stale, [new(Name(1), "2")])); Exact(before);
                var raw = doc.CreateScenarioParameterDraft(record); Mutable(record.File)[record.Offset + 15] ^= 1;
                Reject(() => doc.UpdateScenarioControlPage(raw, [new(Name(1), "2")])); Mutable(record.File)[record.Offset + 15] ^= 1; Exact(before);
                var other = NativeMapDocument.CreateNewMap(new("ForeignSay", "ForeignSay", 20, 20, NativeFormat: native));
                Reject(() => other.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(1), "2")]));
                Console.WriteLine($"SPEECH DETAIL native={native} pages=69 ID=0,1,2,9999 flags=0,1 opaque repeat missing-bank no-op atomic-undo reopen compiler stale raw foreign");
            }
        }
        finally { Directory.Delete(directory, true); }
    }
}
