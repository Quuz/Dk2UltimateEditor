using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class GlobalRoomChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid room input accepted."); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument doc, Dictionary<NativeFile, byte[]> bytes)
    { Expect(doc.Files.Count == bytes.Count && doc.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Room operation changed unrelated/no-op/undo bytes."); }
    private static GlobalDefinitionRow Row(NativeMapDocument doc, int index) => doc.GlobalDefinitionRows().Single(row => row.Family == "Rooms" && row.Index == index);
    private static GlobalRoomDraft Read(NativeMapDocument doc, int index = 6) => doc.CreateGlobalRoomDraft(Row(doc, index));
    private static GlobalRoomInput[] Inputs(GlobalRoomDraft draft, params (string Field, string Value)[] edits) => GlobalRoomControls.Fields.Select(field => new GlobalRoomInput(field,
        edits.LastOrDefault(edit => edit.Field == field) is var changed && changed.Field is not null ? changed.Value : draft.Display(field))).ToArray();
    public static void Run()
    {
        using var source = JsonDocument.Parse(typeof(GlobalRoomChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.GlobalRoomSource.json")!);
        var oracle = source.RootElement;
        var fields = oracle.GetProperty("Fields").EnumerateArray().Select(item => item.GetString()!).ToArray();
        Expect(fields.Length == 20 && fields.Order().SequenceEqual(GlobalRoomControls.Fields.Order()), "Incomplete original room field set.");
        var sourceRoot = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(sourceRoot, "Source"))) sourceRoot = Path.GetDirectoryName(sourceRoot) ?? throw new Exception("Original source missing.");
        foreach (var entry in oracle.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(sourceRoot, entry.Name)))).Equals(entry.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original room input changed.");
        var rooms = oracle.GetProperty("Rooms").EnumerateArray().ToArray();
        Expect(rooms.Length == 24 && oracle.GetProperty("BattleEffectAlwaysDisabledInOriginalUI").GetBoolean(), "Incomplete source room policy.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2Rooms", Guid.NewGuid().ToString("N"));
        var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory, "basis"));
        foreach (var doc in new[] { NativeMapDocument.CreateNewMap(new("Rooms", "Rooms", 20, 20)), native })
        {
            doc.SetHistoryDepth(65535); var initial = Snapshot(doc); var initialHistory = doc.GetHistory().Undo.Count;
            foreach (var room in rooms)
            {
                var index = room.GetProperty("Index").GetInt32(); var row = Row(doc, index);
                Expect(row.ID == room.GetProperty("ID").GetUInt32(), "Original room order differs.");
                Expect(GlobalRoomControls.Enabled(index, "Cost") == room.GetProperty("CostEditable").GetBoolean(), "Source CantBuild cost differs.");
                Expect(!GlobalRoomControls.Enabled(index, "BattleEffect") && !GlobalDefinitionDecorations.Enabled("Rooms", index, "BattleEffect"), "Original permanently inactive BattleEffect is editable.");
                foreach (var prefix in new[] { "Effects", "Objects" })
                {
                    var enabled = room.GetProperty(prefix == "Effects" ? "EnabledEffectSlots" : "EnabledObjectSlots").EnumerateArray().Select(value => value.GetInt32()).ToHashSet();
                    for (var slot = 0; slot < 6; slot++)
                    {
                        var name = $"{prefix}[{slot}]";
                        Expect(GlobalRoomControls.Enabled(index, name) == enabled.Contains(slot) && GlobalDefinitionDecorations.Enabled("Rooms", index, name) == enabled.Contains(slot), "Existing/full room mask differs from independent source oracle.");
                    }
                }
                var draft = doc.CreateGlobalRoomDraft(row); Exact(doc, initial);
                if (!draft.Editable) { Reject(() => doc.ApplyGlobalRoomDraft(draft)); continue; }
                doc.ApplyGlobalRoomDraft(doc.UpdateGlobalRoomDraft(draft, Inputs(draft)));
                Exact(doc, initial); Expect(doc.GetHistory().Undo.Count == initialHistory, "Whole room re-entry materialized defaults/history.");
            }
            var current = Read(doc); var before = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
            var changes = new[] { ("Cost", "65535"), ("Effects[0]", "370"), ("Effects[1]", "369"),
                ("Objects[0]", "1"), ("Objects[1]", "2"), ("Objects[2]", "3"), ("Objects[3]", "4"), ("Objects[4]", "5"), ("Objects[5]", "6"),
                ("TorchIntensity", "255"), ("TorchLightR", "17"), ("TorchLightG", "34"), ("TorchLightB", "51"), ("TorchLightH", "8.999755859375"), ("TorchLightRad", "3.999755859375") };
            var expected = fields.ToDictionary(field => field, current.Raw);
            foreach (var (field, value) in changes) expected[field] = value;
            expected["TorchLightH"] = "36863"; expected["TorchLightRad"] = "16383";
            var prepared = doc.UpdateGlobalRoomDraft(current, Inputs(current, changes)); Exact(doc, before);
            doc.ApplyGlobalRoomDraft(prepared);
            Expect(expected.All(item => Read(doc).Raw(item.Key) == item.Value) && doc.GetHistory().Undo.Count == history + 1, "Complete room Apply lost fields/split history.");
            void Packed(NativeMapDocument packed, Dictionary<NativeFile, byte[]>? prior)
            {
                var record = Row(packed, 6).Record!; var all = prior?.ToDictionary(item => item.Key, item => item.Value.ToArray());
                foreach (var (field, value) in expected)
                {
                    var storage = oracle.GetProperty("NativeStorage").GetProperty(field);
                    var offset = record.Offset + storage.GetProperty("Offset").GetInt32(); var size = storage.GetProperty("Size").GetInt32();
                    var number = long.Parse(value, CultureInfo.InvariantCulture);
                    var actual = size == 1 ? record.File.Data.Span[offset] : BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[offset..]);
                    Expect(actual == number, "Independent room native offset differs: " + field);
                    if (all is null) continue;
                    if (size == 1) all[record.File][offset] = (byte)number;
                    else BinaryPrimitives.WriteUInt16LittleEndian(all[record.File].AsSpan(offset), (ushort)number);
                }
                if (all is not null) Exact(packed, all);
            }
            if (!doc.IsLegacyProject) Packed(doc, before);
            var applied = Snapshot(doc);
            var reopened = NativeMapDocument.Load(doc.SaveCopy(Path.Combine(directory, doc.IsLegacyProject ? "project" : "native")));
            Expect(expected.All(item => Read(reopened).Raw(item.Key) == item.Value), "Room save/reopen differs.");
            if (doc.IsLegacyProject) Packed(doc.CompileLegacyProject("RoomCompiled", native), null);
            doc.ApplyGlobalRoomDraft(doc.UpdateGlobalRoomDraft(Read(doc), Inputs(Read(doc)))); Exact(doc, applied);
            Expect(doc.GetHistory().Undo.Count == history + 1, "Room no-op consumed another Undo.");
            doc.Undo(); Exact(doc, before); doc.Redo(); Exact(doc, applied); doc.Undo(); Exact(doc, before);
            current = Read(doc);
            foreach (var property in oracle.GetProperty("NumericControls").EnumerateObject())
            {
                var low = property.Value.GetProperty("Minimum").GetInt64(); var high = property.Value.GetProperty("Maximum").GetInt64();
                var format = GlobalDefinitionNumbers.Format("Rooms", property.Name, !doc.IsLegacyProject);
                Expect(format.Minimum == low && format.Maximum == high, "Generic number table omitted source room range.");
                foreach (var endpoint in new[] { low, high })
                {
                    var edit = doc.UpdateGlobalRoomDraft(current, Inputs(current, (property.Name, endpoint.ToString(CultureInfo.InvariantCulture))));
                    doc.ApplyGlobalRoomDraft(edit); Expect(Read(doc).Raw(property.Name) == endpoint.ToString(CultureInfo.InvariantCulture), "Room numeric endpoint differs.");
                    if (edit.Changed) doc.Undo(); Exact(doc, before);
                }
                foreach (var invalid in new[] { low - 1, high + 1 })
                {
                    Reject(() => doc.UpdateGlobalRoomDraft(current, Inputs(current, (property.Name, invalid.ToString(CultureInfo.InvariantCulture)))));
                    Reject(() => doc.SetGlobalDefinitionDisplayValues([new("Rooms", 6, property.Name, invalid.ToString(CultureInfo.InvariantCulture))])); Exact(doc, before);
                }
            }
            foreach (var invalid in new[] { ("Cost", "65536"), ("TorchIntensity", "256"), ("TorchLightR", "-1"), ("TorchLightB", "256"),
                ("TorchLightH", "9"), ("TorchLightRad", "4"), ("TorchLightRad", "-0.00001"), ("Cost", "1.5"), ("Effects[0]", "371"), ("Objects[0]", "255"), ("BattleEffect", current.Raw("BattleEffect") == "0" ? "1" : "0") })
            { Reject(() => doc.UpdateGlobalRoomDraft(current, Inputs(current, ("Cost", "222"), invalid))); Exact(doc, before); }
            var heart = Read(doc, 0); Reject(() => doc.UpdateGlobalRoomDraft(heart, Inputs(heart, ("Cost", heart.Raw("Cost") == "0" ? "1" : "0"))));
            foreach (var field in fields.Where(field => !GlobalRoomControls.Enabled(6, field)))
                Reject(() => doc.UpdateGlobalRoomDraft(current, Inputs(current, (field, current.Raw(field) == "0" ? "1" : "0"))));
            Reject(() => doc.UpdateGlobalRoomDraft(current, Inputs(current).Skip(1)));
            Reject(() => doc.UpdateGlobalRoomDraft(current, Inputs(current).Append(new("Cost", "222"))));
            Reject(() => NativeMapDocument.CreateNewMap(new("Foreign", "Foreign", 12, 12)).ApplyGlobalRoomDraft(current));
            doc.SetGlobalDefinitionValues([new("Rooms", 6, "Cost", "333")]); var stale = Snapshot(doc);
            Reject(() => doc.ApplyGlobalRoomDraft(current)); Exact(doc, stale); doc.Undo(); Exact(doc, before);
            doc.SetGlobalDefinitionValues([new("Rooms", 6, "BattleEffect", "65000"), new("Rooms", 6, "Effects[5]", "64000"), new("Rooms", 6, "Effects[0]", "63000"),
                new("Rooms", 0, "Cost", doc.IsLegacyProject ? "100000" : "65535"), new("Rooms", 6, "TorchIntensity", doc.IsLegacyProject ? "100000" : "255")]);
            var imported = Snapshot(doc); var importedDraft = Read(doc);
            doc.ApplyGlobalRoomDraft(doc.UpdateGlobalRoomDraft(importedDraft, Inputs(importedDraft))); Exact(doc, imported);
            var importedHeart = Read(doc, 0); doc.ApplyGlobalRoomDraft(doc.UpdateGlobalRoomDraft(importedHeart, Inputs(importedHeart))); Exact(doc, imported);
            var repair = doc.UpdateGlobalRoomDraft(importedDraft, Inputs(importedDraft, ("Effects[0]", "0"))); doc.ApplyGlobalRoomDraft(repair);
            Expect(Read(doc).Raw("BattleEffect") == "65000" && Read(doc).Raw("Effects[5]") == "64000" && Read(doc).Raw("TorchIntensity") == importedDraft.Raw("TorchIntensity"), "Explicit room repair normalized another inactive/imported value.");
            doc.Undo(); Exact(doc, imported); doc.Undo(); Exact(doc, before);
            Console.WriteLine($"ROOM DETAIL native={!doc.IsLegacyProject} fields=20 rows=24 CantBuild BattleEffect-inactive masks source-ranges RGB fixed-height-radius packed undo redo compiler reopen inactive unknown stale foreign");
        }
        var missing = NativeMapDocument.Create(12, 12, "Missing"); Reject(() => missing.ApplyGlobalRoomDraft(Read(missing, 0)));
    }
}
