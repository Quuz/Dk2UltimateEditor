using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    public static void RunCameraParent()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioCameraPage.json")!;
        using var fixture = JsonDocument.Parse(stream);
        var source = fixture.RootElement.GetProperty("SourcePage");
        Expect(source.GetProperty("Type").GetString() == "ACTION_CAMERAPATH" && source.GetProperty("PageID").GetInt32() == 7224,
            "Camera parent source page identity differs.");
        var numeric = source.GetProperty("NumericInputs")[0];
        Expect(numeric.GetProperty("Parameter").GetInt32() == 1 && !numeric.GetProperty("Signed").GetBoolean()
            && numeric.GetProperty("SourceDisplayMaximum").GetInt32() == 9999 && fixture.RootElement.GetProperty("RequiresArea").GetBoolean(),
            "Camera source unsigned range/prerequisite differs.");
        var controls = fixture.RootElement.GetProperty("Controls").EnumerateArray().Select(item =>
            (item.GetProperty("Parameter").GetInt32(), item.GetProperty("Kind").GetString(), item.GetProperty("Maximum").GetInt64(), item.GetProperty("Unsigned").GetBoolean()));
        Expect(ScenarioControlPages.Controls("ACTION_CAMERAPATH").Select(item => (item.Parameter, (string?)item.Kind.ToString(), item.Maximum, item.Unsigned)).SequenceEqual(controls),
            "Camera controls differ from source fixture.");
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Source"))) root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Original source missing.");
        foreach (var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase),
                "Original input changed: " + input.Name);
        var expectedChoices = Regex.Matches(File.ReadAllText(Path.Combine(root, "Source/Sources/Datas/Datas_CameraPaths.c")),
            @"\{\s*(\d+),\s*(\d+),\s*szCameraPath\d+,(TRUE|FALSE)\}")
            .Select(match => (ID: int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), Ceiling: match.Groups[3].Value == "TRUE" ? 0 : 1)).ToArray();
        Expect(expectedChoices.Length == 52 && expectedChoices.Count(item => item.Ceiling == 0) == 24, "Original camera defaults incomplete.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2CameraParent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false, true })
            {
                var page = pages.Single(item => item.Type == "ACTION_CAMERAPATH");
                var doc = NativeMapDocument.CreateNewMap(new("CameraParent", "CameraParent", 20, 20, NativeFormat: native));
                doc.SetHistoryDepth(65535);
                foreach (var id in new[] { 13, 39 })
                {
                    var area = doc.AddMapRegion(MapRegionKind.Area, new(id == 13 ? 2 : 6, 2, 1, 1));
                    doc.SetField(area, EntityReferences.Identity(area)!.Field, id.ToString(CultureInfo.InvariantCulture));
                }
                var record = Add(doc, native, page);
                var operands = Fields(native, page);
                string Name(int parameter) => (native ? "Param" : "param") + parameter;
                var before = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
                var draft = doc.CreateScenarioParameterDraft(record);
                Expect(ScenarioControlPages.MissingRequirements(page.Type, doc).Count == 0, "Existing camera areas missing.");
                Expect(ScenarioControlPages.Choices(draft, ScenarioControlPages.Controls(page.Type)[1], doc).Select(item => item.Value).SequenceEqual(new[] { "13", "39" }), "Camera area identities differ.");
                doc.ApplyScenarioParameterDraft(doc.UpdateScenarioControlPage(draft, [])); Exact(before);
                if (!native)
                {
                    var imported = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(before[record.File].AsSpan(record.Offset + 16));
                    doc.ApplyScenarioParameterDraft(doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(1), imported.ToString(CultureInfo.InvariantCulture))])); Exact(before);
                }
                Expect(doc.GetHistory().Undo.Count == history, "Camera opening/no-op repaired imports.");
                foreach (var choice in expectedChoices)
                {
                    draft = doc.CreateScenarioParameterDraft(record);
                    var prepared = doc.UpdateScenarioControlPage(draft, [new(Name(2), "39")]);
                    prepared = doc.UpdateScenarioCameraLibrary(prepared, choice.ID); Exact(before);
                    Expect(draft.Values[Name(2)] != "39", "Private parent/library mutated original draft.");
                    var expected = before.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
                    foreach (var (parameter, value) in new[] { (1, choice.ID), (2, 39), (3, choice.Ceiling) })
                    {
                        var operand = operands.Single(item => item.Name == Name(parameter));
                        Expect(operand.Offset == (native ? 7 + parameter : 12 + parameter * 4) && operand.Size == (native ? 1 : 4), "Camera operand storage differs from literal compiler offsets.");
                        Write(expected[record.File], record.Offset + operand.Offset, operand, value);
                    }
                    doc.ApplyScenarioParameterDraft(prepared); Exact(expected);
                    Expect(doc.GetHistory().Undo.Count == history + 1, "Camera area/profile/ceiling not one Undo.");
                    doc.Undo(); Exact(before); doc.Redo(); Exact(expected);
                    var noOpRevision = doc.EditRevision;
                    doc.ApplyScenarioParameterDraft(doc.UpdateScenarioCameraLibrary(doc.CreateScenarioParameterDraft(record), choice.ID)); Exact(expected);
                    Expect(doc.EditRevision == noOpRevision, "Same camera library choice made an edit.");
                    var saved = doc.SaveCopy(Path.Combine(directory, $"{native}-{choice.ID}")); var reopened = NativeMapDocument.Load(saved);
                    var loaded = reopened.Records().Single(CameraPaths.IsCameraAction);
                    Expect(reopened.CreateScenarioParameterDraft(loaded).Values.SequenceEqual(doc.CreateScenarioParameterDraft(record).Values), "Camera parent operands lost on reopen.");
                    if (!native)
                    {
                        var compiled = doc.CompileLegacyProject("CameraParentCompiled", NativeMapDocument.Create(3, 3, "Basis"));
                        var action = compiled.Records().Single(CameraPaths.IsCameraAction);
                        Expect(action.File.Data.Span[action.Offset + 8] == choice.ID && action.File.Data.Span[action.Offset + 9] == 39
                            && action.File.Data.Span[action.Offset + 10] == choice.Ceiling, "Camera parent compiler bytes differ.");
                    }
                    doc.Undo(); Exact(before);
                }
                foreach (var value in new[] { 0, native ? 255 : 9999 })
                {
                    var operand = operands.Single(item => item.Name == Name(1));
                    var expected = before.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
                    Write(expected[record.File], record.Offset + operand.Offset, operand, value);
                    doc.ApplyScenarioParameterDraft(doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(1), value.ToString(CultureInfo.InvariantCulture))])); Exact(expected);
                    doc.Undo(); Exact(before);
                }
                foreach (var input in new[] { new ScenarioParameterValue(Name(1), "-1"), new(Name(1), native ? "256" : "10000"), new(Name(2), "14"), new(Name(3), "2") })
                    Reject(() => doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [input]));
                Reject(() => doc.UpdateScenarioCameraLibrary(doc.CreateScenarioParameterDraft(record), 999));
                var foreign = NativeMapDocument.CreateNewMap(new("Foreign", "Foreign", 20, 20, NativeFormat: native));
                Reject(() => foreign.UpdateScenarioCameraLibrary(doc.CreateScenarioParameterDraft(record), 50));
                var stale = doc.CreateScenarioParameterDraft(record);
                doc.SetField(record, record.Fields.Single(item => item.Name == Name(2)), "13"); doc.Undo();
                Reject(() => doc.UpdateScenarioCameraLibrary(stale, 50)); Exact(before);
                var rawDraft = doc.CreateScenarioParameterDraft(record); var raw = Mutable(record.File); var position = record.Offset + (native ? 15 : 47);
                raw[position] ^= 1; Reject(() => doc.UpdateScenarioCameraLibrary(rawDraft, 50)); raw[position] ^= 1; Exact(before);
                var empty = NativeMapDocument.CreateNewMap(new("EmptyCamera", "EmptyCamera", 20, 20, NativeFormat: native));
                var emptyRecord = Add(empty, native, page); var untouched = Snapshot(empty);
                Expect(ScenarioControlPages.MissingRequirements(page.Type, empty).SequenceEqual(new[] { EntityKind.Area }), "Camera original area prerequisite missing.");
                Reject(() => empty.UpdateScenarioControlPage(empty.CreateScenarioParameterDraft(emptyRecord), []));
                Reject(() => empty.UpdateScenarioCameraLibrary(empty.CreateScenarioParameterDraft(emptyRecord), 50)); Exact(untouched);
                Expect(doc.GetHistory().Undo.Count == history, "Rejected camera inputs changed history.");
            }
        }
        finally { Directory.Delete(directory, true); }
    }
}
