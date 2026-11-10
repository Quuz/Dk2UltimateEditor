using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class GlobalTerrainChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action) { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid terrain input accepted."); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> expected) => Expect(expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Terrain edit changed unrelated/undo bytes.");
    private static GlobalDefinitionRow Row(NativeMapDocument doc, int index) => doc.GlobalDefinitionRows().Single(row => row.Family == "Terrains" && row.Index == index);
    private static GlobalTerrainDraft Draft(NativeMapDocument doc, int index) => doc.CreateGlobalTerrainDraft(Row(doc, index));
    public static void Run()
    {
        using var source = JsonDocument.Parse(typeof(GlobalTerrainChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.GlobalTerrainSource.json")!);
        var oracle = source.RootElement; var fields = oracle.GetProperty("Fields").EnumerateArray().Select(item => item.GetString()!).ToArray();
        Expect(fields.Length == 22 && fields.Order().SequenceEqual(GlobalTerrainControls.Fields.Order()), "Original complete terrain controls differ.");
        var root = AppContext.BaseDirectory; while (!Directory.Exists(Path.Combine(root, "Source"))) root = Path.GetDirectoryName(root) ?? throw new Exception("Original source missing.");
        foreach (var entry in oracle.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, entry.Name)))).Equals(entry.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original terrain input changed.");
        var enabled = oracle.GetProperty("TransformationEnabledIDs").EnumerateArray().Select(item => item.GetUInt32()).ToHashSet();
        var invisible = oracle.GetProperty("VisibilityDisabledIDs").EnumerateArray().Select(item => item.GetUInt32()).ToHashSet();
        var choices = oracle.GetProperty("EnabledTransformationChoices").EnumerateArray().Select(item => item.GetUInt32().ToString(CultureInfo.InvariantCulture)).ToArray();
        var claimScanSource = oracle.GetProperty("ClaimScanNameSource");
        var claimScan = GlobalTerrainControls.Choices(0, "MaxHealthType").Single(choice => choice.Value == "38");
        Expect(claimScan.English == claimScanSource.GetProperty("English").GetString() && claimScan.Russian == claimScanSource.GetProperty("Russian").GetString(), "Known original perception field was labelled unknown.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2Terrain", Guid.NewGuid().ToString("N"));
        var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory, "basis"));
        foreach (var doc in new[] { NativeMapDocument.CreateNewMap(new("Terrain", "Terrain", 20, 20)), native })
        {
            doc.SetHistoryDepth(65535); var initial = Snapshot(doc);
            GlobalTerrainInput[] Inputs(GlobalTerrainDraft draft, params (string Field, string Value)[] edits) => fields.Select(field => new GlobalTerrainInput(field, edits.LastOrDefault(edit => edit.Field == field) is var changed && changed.Field is not null ? changed.Value : draft.Display(field))).ToArray();
            // All38 rows: opening is read-only; explicit Save matches only
            // the source six RGB-zero branches and retains all opaque bytes.
            foreach (var row in doc.GlobalDefinitionRows().Where(row => row.Family == "Terrains"))
            {
                Expect(GlobalTerrainControls.Enabled(row.Index, "MaxHealthType") == enabled.Contains(row.ID) && GlobalTerrainControls.Enabled(row.Index, "MaxHealthEffect") == enabled.Contains(row.ID), "Source transformation/effect availability differs.");
                foreach (var effect in new[] { "MaxHealthEffect", "DestroyedEffect" }) Expect(GlobalDefinitionDecorations.Enabled("Terrains", row.Index, effect) == enabled.Contains(row.ID), "Existing named-effect API differs from independent terrain availability oracle.");
                Expect(GlobalTerrainControls.Enabled(row.Index, "Visible") == !invisible.Contains(row.ID), "Source visibility availability differs.");
                var actual = GlobalTerrainControls.Choices(row.Index, "MaxHealthType");
                Expect(actual.Count == (enabled.Contains(row.ID) ? 13 : 38) && actual.All(choice => choice.English.Length > 0 && choice.Russian.Length > 0), "Incomplete source bilingual terrain choices.");
                if (enabled.Contains(row.ID)) Expect(actual.Select(choice => choice.Value).SequenceEqual(choices), "Filtered source terrain prefix differs.");
                var before = Snapshot(doc); var history = doc.GetHistory().Undo.Count; var draft = doc.CreateGlobalTerrainDraft(row); Exact(before);
                if (!draft.Editable) { Reject(() => doc.ApplyGlobalTerrainDraft(draft)); Exact(before); continue; }
                var expectedValues = fields.ToDictionary(field => field, draft.Raw);
                foreach (var light in new[] { "TerrainLight", "AmbientLight" }) if (expectedValues[light] == "0") foreach (var colour in new[] { "R", "G", "B" }) expectedValues[light + colour] = "0";
                var saved = doc.UpdateGlobalTerrainDraft(draft, Inputs(draft)); Exact(before); doc.ApplyGlobalTerrainDraft(saved);
                Expect(fields.All(field => Draft(doc, row.Index).Raw(field) == expectedValues[field]), "Terrain Save/default values differ.");
                var changed = fields.Any(field => draft.Raw(field) != expectedValues[field]);
                Expect(doc.GetHistory().Undo.Count == history + (changed ? 1 : 0), "Terrain no-op/explicit Save consumed wrong history.");
                if (changed) { var after = Snapshot(doc); doc.Undo(); Exact(before); doc.Redo(); Exact(after); doc.Undo(); Exact(before); }
            }
            Exact(initial);
            // Source Rock is index2; all controls active, literal mixed edit.
            var current = Draft(doc, 2); var beforeEdit = Snapshot(doc); var historyEdit = doc.GetHistory().Undo.Count;
            var mixed = doc.UpdateGlobalTerrainDraft(current, Inputs(current,
                ("MaxHealthType", "8"), ("DestroyedType", "3"), ("MaxHealthEffect", "370"), ("DestroyedEffect", "369"),
                ("Damage", "-32768"), ("GoldValue", "65535"), ("StartingHealth", "12345"), ("MaxHealth", "54321"), ("ManaGain", "321"), ("MaxManaGain", "654"),
                ("WibbleH", "255"), ("WibbleV", "254"), ("Visible", "1"), ("TerrainLight", "1"), ("AmbientLight", "0"),
                ("TerrainLightR", "17"), ("TerrainLightG", "34"), ("TerrainLightB", "51"), ("AmbientLightR", "85"), ("AmbientLightG", "102"), ("AmbientLightB", "119"), ("TerrainLightH", "16000.999755859375")));
            Exact(beforeEdit); doc.ApplyGlobalTerrainDraft(mixed);
            var expected = new Dictionary<string, string> {
                ["MaxHealthType"]="8", ["DestroyedType"]="3", ["MaxHealthEffect"]="370", ["DestroyedEffect"]="369",
                ["Damage"]="-32768", ["GoldValue"]="65535", ["StartingHealth"]="12345", ["MaxHealth"]="54321", ["ManaGain"]="321", ["MaxManaGain"]="654", ["WibbleH"]="255", ["WibbleV"]="254",
                ["Visible"]="1", ["TerrainLight"]="1", ["AmbientLight"]="0", ["TerrainLightR"]="17", ["TerrainLightG"]="34", ["TerrainLightB"]="51", ["AmbientLightR"]="0", ["AmbientLightG"]="0", ["AmbientLightB"]="0", ["TerrainLightH"]="65540095" };
            Expect(expected.All(item => Draft(doc, 2).Raw(item.Key) == item.Value) && doc.GetHistory().Undo.Count == historyEdit + 1, "Whole terrain page lost signed/fractional/RGB fields or split history.");
            void Packed(NativeMapDocument packed, Dictionary<NativeFile, byte[]>? before)
            {
                var record = Row(packed, 2).Record!; var bytes = record.File.Data.Span;
                var all = before?.ToDictionary(item => item.Key, item => item.Value.ToArray());
                foreach (var item in expected)
                {
                    var storage = oracle.GetProperty("NativeStorage").GetProperty(item.Key); var offset = record.Offset + storage.GetProperty("Offset").GetInt32(); var size = storage.GetProperty("Size").GetInt32();
                    var actual = storage.GetProperty("Kind").GetString() == "BOOL" ? (bytes[offset] & storage.GetProperty("Mask").GetByte()) != 0 ? 1L : 0L
                        : storage.GetProperty("Kind").GetString() == "short" ? BinaryPrimitives.ReadInt16LittleEndian(bytes[offset..]) : size == 1 ? bytes[offset] : size == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]) : BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]);
                    Expect(actual.ToString(CultureInfo.InvariantCulture) == item.Value, "Independent native terrain offset differs: " + item.Key);
                    if (all is null) continue;
                    var target = all[record.File].AsSpan(offset); var number = long.Parse(item.Value, CultureInfo.InvariantCulture);
                    if (storage.GetProperty("Kind").GetString() == "BOOL") { var mask = storage.GetProperty("Mask").GetByte(); target[0] = (byte)((target[0] & ~mask) | (number == 0 ? 0 : mask)); }
                    else if (size == 1) target[0] = (byte)number;
                    else if (size == 2) BinaryPrimitives.WriteUInt16LittleEndian(target, unchecked((ushort)number));
                    else BinaryPrimitives.WriteInt32LittleEndian(target, (int)number);
                }
                if (all is not null) Exact(all);
            }
            if (!doc.IsLegacyProject) Packed(doc, beforeEdit);
            var edited = Snapshot(doc); var reopened = NativeMapDocument.Load(doc.SaveCopy(Path.Combine(directory, doc.IsLegacyProject ? "project" : "native")));
            Expect(expected.All(item => Draft(reopened, 2).Raw(item.Key) == item.Value), "Terrain save/reopen changed controls.");
            if (doc.IsLegacyProject) Packed(doc.CompileLegacyProject("TerrainCompiled", native), null);
            doc.Undo(); Exact(beforeEdit); doc.Redo(); Exact(edited); doc.Undo(); Exact(beforeEdit);
            current = Draft(doc, 2);
            foreach (var number in oracle.GetProperty("NumericControls").EnumerateObject())
            {
                var minimum = number.Value.GetProperty("Minimum").GetInt64(); var maximum = number.Value.GetProperty("Maximum").GetInt64();
                var format = GlobalDefinitionNumbers.Format("Terrains", number.Name, !doc.IsLegacyProject);
                Expect(format.Minimum == minimum && format.Maximum == maximum, "Ordinary table omitted the source terrain range: " + number.Name);
                foreach (var endpoint in new[] { minimum, maximum })
                {
                    var prepared = doc.UpdateGlobalTerrainDraft(current, Inputs(current, (number.Name, endpoint.ToString(CultureInfo.InvariantCulture)))); doc.ApplyGlobalTerrainDraft(prepared);
                    Expect(Draft(doc, 2).Raw(number.Name) == endpoint.ToString(CultureInfo.InvariantCulture), "Source terrain numeric endpoint failed.");
                    if (doc.GetHistory().Undo.Count > historyEdit) doc.Undo(); Exact(beforeEdit);
                }
                foreach (var invalid in new[] { minimum - 1, maximum + 1 })
                {
                    Reject(() => doc.UpdateGlobalTerrainDraft(current, Inputs(current, (number.Name, invalid.ToString(CultureInfo.InvariantCulture)))));
                    Reject(() => doc.SetGlobalDefinitionDisplayValues([new("Terrains", 2, number.Name, invalid.ToString(CultureInfo.InvariantCulture))])); Exact(beforeEdit);
                }
            }
            var lightsOff = doc.UpdateGlobalTerrainDraft(current, Inputs(current, ("TerrainLight", "0"), ("AmbientLight", "0"), ("TerrainLightR", "17"), ("TerrainLightG", "34"), ("TerrainLightB", "51"), ("AmbientLightR", "85"), ("AmbientLightG", "102"), ("AmbientLightB", "119")));
            doc.ApplyGlobalTerrainDraft(lightsOff);
            Expect(new[] { "TerrainLightR", "TerrainLightG", "TerrainLightB", "AmbientLightR", "AmbientLightG", "AmbientLightB" }.All(field => Draft(doc, 2).Raw(field) == "0") && Draft(doc, 2).Raw("TerrainLightH") == current.Raw("TerrainLightH"), "Explicit Save lost six RGB cleanup or cleared retained light height.");
            if (doc.GetHistory().Undo.Count > historyEdit) doc.Undo(); Exact(beforeEdit);
            foreach (var choice in choices)
            { var prepared = doc.UpdateGlobalTerrainDraft(current, Inputs(current, ("MaxHealthType", choice), ("DestroyedType", choice))); doc.ApplyGlobalTerrainDraft(prepared); if (doc.GetHistory().Undo.Count > historyEdit) doc.Undo(); Exact(beforeEdit); }
            foreach (var invalid in new[] { ("Damage", "-32769"), ("Damage", "32768"), ("GoldValue", "65536"), ("WibbleV", "256"), ("TerrainLightR", "256"), ("TerrainLightH", "16001"), ("MaxHealthType", "38"), ("MaxHealthEffect", "371"), ("Visible", "2"), ("Damage", "1.5") })
            { Reject(() => doc.UpdateGlobalTerrainDraft(current, Inputs(current, ("GoldValue", "222"), invalid))); Exact(beforeEdit); }
            var border = Draft(doc, 0); Reject(() => doc.UpdateGlobalTerrainDraft(border, Inputs(border, ("MaxHealthType", "8"))));
            var room = Draft(doc, 14); Reject(() => doc.UpdateGlobalTerrainDraft(room, Inputs(room, ("Visible", room.Raw("Visible") == "0" ? "1" : "0")))); Exact(beforeEdit);
            var stale = current; doc.SetGlobalDefinitionValues([new("Terrains", 2, "GoldValue", "222")]); var changedBefore = Snapshot(doc); Reject(() => doc.ApplyGlobalTerrainDraft(stale)); Exact(changedBefore); doc.Undo(); Exact(beforeEdit);
            Reject(() => NativeMapDocument.CreateNewMap(new("Foreign", "Foreign", 12, 12)).ApplyGlobalTerrainDraft(current));
            Reject(() => doc.UpdateGlobalTerrainDraft(current, Inputs(current).Skip(1))); Reject(() => doc.UpdateGlobalTerrainDraft(current, Inputs(current).Concat([new("Damage", "0")]))); Exact(beforeEdit);
            // Wider project imports and inactive values remain exact; explicit
            // Save cleanup is tested separately from opening/Cancel.
            doc.SetGlobalDefinitionValues([new("Terrains", 0, "MaxHealthType", doc.IsLegacyProject ? "65000" : "255"), new("Terrains", 2, "GoldValue", doc.IsLegacyProject ? "100000" : "65535")]);
            var imported = Snapshot(doc); var importedDraft = Draft(doc, 0); var preserved = doc.UpdateGlobalTerrainDraft(importedDraft, Inputs(importedDraft));
            Expect(preserved.Raw("MaxHealthType") == importedDraft.Raw("MaxHealthType"), "Imported inactive terrain was normalized."); Exact(imported);
            Expect(doc.UpdateGlobalTerrainDraft(Draft(doc, 2), Inputs(Draft(doc, 2))).Raw("GoldValue") == (doc.IsLegacyProject ? "100000" : "65535"), "Imported numeric value normalized."); Exact(imported); doc.Undo(); Exact(beforeEdit);
            Console.WriteLine($"TERRAIN DETAIL native={!doc.IsLegacyProject} fields=22 rows=38 transforms=8 choices=13 ranges=8 RGB-save-zero=6 exact-packed whole-page undo redo reopen compiler unknown inactive stale foreign");
        }
        var missing = NativeMapDocument.Create(12, 12, "Missing"); Reject(() => missing.ApplyGlobalTerrainDraft(Draft(missing, 0)));
    }
}
