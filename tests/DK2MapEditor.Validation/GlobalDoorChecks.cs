using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class GlobalDoorChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid door input accepted."); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument doc, Dictionary<NativeFile, byte[]> bytes)
    { Expect(doc.Files.Count == bytes.Count && doc.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Door operation changed unrelated/no-op/undo bytes."); }
    private static GlobalDefinitionRow Row(NativeMapDocument doc, int index = 0) => doc.GlobalDefinitionRows().Single(row => row.Family == "Doors" && row.Index == index);
    private static GlobalDoorDraft Read(NativeMapDocument doc, int index = 0) => doc.CreateGlobalDoorDraft(Row(doc, index));
    private static GlobalDoorInput[] Inputs(GlobalDoorDraft draft, params (string Field, string Value)[] edits) => GlobalDoorControls.Fields.Select(field => new GlobalDoorInput(field,
        edits.LastOrDefault(edit => edit.Field == field) is var changed && changed.Field is not null ? changed.Value : draft.Display(field))).ToArray();
    public static void Run()
    {
        using var source = JsonDocument.Parse(typeof(GlobalDoorChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.GlobalDoorSource.json")!);
        var oracle = source.RootElement;
        var fields = oracle.GetProperty("Fields").EnumerateArray().Select(item => item.GetString()!).ToArray();
        Expect(fields.Length == 9 && fields.SequenceEqual(GlobalDoorControls.Fields), "Incomplete original door field set.");
        var sourceRoot = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(sourceRoot, "Source"))) sourceRoot = Path.GetDirectoryName(sourceRoot) ?? throw new Exception("Original source missing.");
        foreach (var entry in oracle.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(sourceRoot, entry.Name)))).Equals(entry.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original door input changed.");
        foreach (var property in oracle.GetProperty("Choices").EnumerateObject())
            Expect(GlobalDoorControls.Choices(property.Name).Select(choice => choice.Value).SequenceEqual(property.Value.EnumerateArray().Select(item => item.GetProperty("ID").GetUInt32().ToString(CultureInfo.InvariantCulture))), "Original complete door catalogue differs: " + property.Name);
        foreach (var property in oracle.GetProperty("NumericControls").EnumerateObject())
        {
            Expect(GlobalDoorControls.TextEntryEnabled(property.Name) == property.Value.GetProperty("TextEntryEnabled").GetBoolean() && property.Value.GetProperty("SpinnerEnabled").GetBoolean(), "Original edit/spinner HWND policy differs.");
            var acceptsInput = property.Value.GetProperty("SpinnerInputEnabled").GetBoolean();
            Expect(GlobalDoorControls.SpinnerInputEnabled(property.Name) == acceptsInput, "Measured native buddy input policy differs.");
            if (!acceptsInput)
            {
                foreach (var value in new[] { "0", "123", "65535", "100000" }) foreach (var direction in new[] { -1, 1 })
                    Reject(() => GlobalDoorControls.Step(property.Name, value, direction));
            }
            else Expect(GlobalDoorControls.Step(property.Name, "0", -1) == "0" && GlobalDoorControls.Step(property.Name, "65535", 1) == "65535" && GlobalDoorControls.Step(property.Name, "123", 1) == "124" && GlobalDoorControls.Step(property.Name, "123", -1) == "122", "Door spinner bounds/order differ.");
        }
        var directory = Path.Combine(Path.GetTempPath(), "DK2Doors", Guid.NewGuid().ToString("N"));
        var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory, "basis"));
        foreach (var doc in new[] { NativeMapDocument.CreateNewMap(new("Doors", "Doors", 20, 20)), native })
        {
            doc.SetHistoryDepth(65535); var initial = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
            foreach (var door in oracle.GetProperty("Doors").EnumerateArray())
            {
                var index = door.GetProperty("Index").GetInt32(); var row = Row(doc, index);
                Expect(row.ID == door.GetProperty("ID").GetUInt32(), "Original door order differs.");
                var draft = doc.CreateGlobalDoorDraft(row); Exact(doc, initial);
                Expect(draft.Editable, "Original door definition is not editable.");
                doc.ApplyGlobalDoorDraft(doc.UpdateGlobalDoorDraft(draft, Inputs(draft))); Exact(doc, initial);
                Expect(doc.GetHistory().Undo.Count == history, "Door re-entry materialized defaults/history.");
            }
            var current = Read(doc);
            var changes = new[] { ("Cost", "65535"), ("BuildPoints", "1234"), ("Health", "5678"), ("HealthGain", "999"), ("ManaCost", "456"),
                ("Material", "7"), ("DeathEffect", "370"), ("Crate", "57"), ("Key", "137") };
            var expected = changes.ToDictionary(item => item.Item1, item => item.Item2);
            var prepared = doc.UpdateGlobalDoorDraft(current, Inputs(current, changes)); Exact(doc, initial);
            doc.ApplyGlobalDoorDraft(prepared);
            Expect(expected.All(item => Read(doc).Raw(item.Key) == item.Value) && doc.GetHistory().Undo.Count == history + 1, "Door Apply lost a field/split history.");
            void Packed(NativeMapDocument packed, Dictionary<NativeFile, byte[]>? prior)
            {
                var record = Row(packed).Record!; var all = prior?.ToDictionary(item => item.Key, item => item.Value.ToArray());
                foreach (var (field, value) in expected)
                {
                    var storage = oracle.GetProperty("NativeStorage").GetProperty(field);
                    var offset = record.Offset + storage.GetProperty("Offset").GetInt32(); var size = storage.GetProperty("Size").GetInt32(); var number = ushort.Parse(value, CultureInfo.InvariantCulture);
                    var actual = size == 1 ? record.File.Data.Span[offset] : BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[offset..]);
                    Expect(actual == number, "Independent door native offset differs: " + field);
                    if (all is null) continue;
                    if (size == 1) all[record.File][offset] = (byte)number;
                    else BinaryPrimitives.WriteUInt16LittleEndian(all[record.File].AsSpan(offset), number);
                }
                if (all is not null) Exact(packed, all);
            }
            if (!doc.IsLegacyProject) Packed(doc, initial);
            var applied = Snapshot(doc);
            var reopened = NativeMapDocument.Load(doc.SaveCopy(Path.Combine(directory, doc.IsLegacyProject ? "project" : "native")));
            Expect(expected.All(item => Read(reopened).Raw(item.Key) == item.Value), "Door save/reopen differs.");
            if (doc.IsLegacyProject) Packed(doc.CompileLegacyProject("DoorCompiled", native), null);
            doc.ApplyGlobalDoorDraft(doc.UpdateGlobalDoorDraft(Read(doc), Inputs(Read(doc)))); Exact(doc, applied);
            Expect(doc.GetHistory().Undo.Count == history + 1, "Door no-op consumed history.");
            doc.Undo(); Exact(doc, initial); doc.Redo(); Exact(doc, applied); doc.Undo(); Exact(doc, initial);
            current = Read(doc);
            foreach (var property in oracle.GetProperty("NumericControls").EnumerateObject())
            {
                var format = GlobalDefinitionNumbers.Format("Doors", property.Name, !doc.IsLegacyProject);
                Expect(format.Minimum == 0 && format.Maximum == 65535, "Generic table omitted source door range.");
                foreach (var endpoint in new[] { "0", "65535" })
                {
                    var edit = doc.UpdateGlobalDoorDraft(current, Inputs(current, (property.Name, endpoint)));
                    doc.ApplyGlobalDoorDraft(edit); Expect(Read(doc).Raw(property.Name) == endpoint, "Door numeric endpoint differs.");
                    if (edit.Changed) doc.Undo(); Exact(doc, initial);
                }
                foreach (var invalid in new[] { "-1", "65536", "1.5", "1e2", "bad" })
                {
                    Reject(() => doc.UpdateGlobalDoorDraft(current, Inputs(current, ("Material", "7"), (property.Name, invalid))));
                    Reject(() => doc.SetGlobalDefinitionDisplayValues([new("Doors", 0, property.Name, invalid)])); Exact(doc, initial);
                }
            }
            foreach (var property in oracle.GetProperty("Choices").EnumerateObject())
            {
                foreach (var choice in property.Value.EnumerateArray())
                {
                    var id = choice.GetProperty("ID").GetUInt32().ToString(CultureInfo.InvariantCulture);
                    var edit = doc.UpdateGlobalDoorDraft(current, Inputs(current, (property.Name, id)));
                    doc.ApplyGlobalDoorDraft(edit); Expect(Read(doc).Raw(property.Name) == id, "Door source catalogue value not accepted.");
                    if (edit.Changed) doc.Undo(); Exact(doc, initial);
                }
                Reject(() => doc.UpdateGlobalDoorDraft(current, Inputs(current, ("Health", "100"), (property.Name, property.Name == "DeathEffect" ? "371" : "254")))); Exact(doc, initial);
            }
            Reject(() => doc.UpdateGlobalDoorDraft(current, Inputs(current).Skip(1)));
            Reject(() => doc.UpdateGlobalDoorDraft(current, Inputs(current).Append(new("Cost", "222"))));
            Reject(() => NativeMapDocument.CreateNewMap(new("Foreign", "Foreign", 12, 12)).ApplyGlobalDoorDraft(current));
            Reject(() => doc.CreateGlobalDoorDraft(doc.GlobalDefinitionRows().First(row => row.Family == "Rooms")));
            doc.SetGlobalDefinitionValues([new("Doors", 0, "Cost", "333")]); var stale = Snapshot(doc);
            Reject(() => doc.ApplyGlobalDoorDraft(current)); Exact(doc, stale); doc.Undo(); Exact(doc, initial);
            doc.SetGlobalDefinitionValues([new("Doors", 0, "Material", "254"), new("Doors", 0, "DeathEffect", "63000"), new("Doors", 0, "Key", "253"),
                new("Doors", 0, "ManaCost", doc.IsLegacyProject ? "100000" : "65535")]);
            var imported = Snapshot(doc); var importedDraft = Read(doc);
            doc.ApplyGlobalDoorDraft(doc.UpdateGlobalDoorDraft(importedDraft, Inputs(importedDraft))); Exact(doc, imported);
            var repair = doc.UpdateGlobalDoorDraft(importedDraft, Inputs(importedDraft, ("Material", "0"))); doc.ApplyGlobalDoorDraft(repair);
            Expect(Read(doc).Raw("DeathEffect") == "63000" && Read(doc).Raw("Key") == "253" && Read(doc).Raw("ManaCost") == importedDraft.Raw("ManaCost"), "Door repair normalized another import.");
            doc.Undo(); Exact(doc, imported); doc.Undo(); Exact(doc, initial);
            Console.WriteLine($"DOOR DETAIL native={!doc.IsLegacyProject} fields=9 rows=8 choices=393 source-ranges ManaCost-disabled-edit-active-spinner packed undo redo compiler reopen unknown wide stale foreign");
        }
        var missing = NativeMapDocument.Create(12, 12, "Missing"); Reject(() => missing.ApplyGlobalDoorDraft(Read(missing)));
    }
}
