using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class GlobalSpellChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid spell input accepted."); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument doc, Dictionary<NativeFile, byte[]> bytes) => Expect(doc.Files.Count == bytes.Count && doc.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Spell operation changed unrelated/private/no-op/undo bytes.");
    private static GlobalDefinitionRow Row(NativeMapDocument doc, int index = 0) => doc.GlobalDefinitionRows().Single(row => row.Family == "Spells" && row.Index == index);
    private static GlobalSpellDraft Read(NativeMapDocument doc, int index = 0) => doc.CreateGlobalSpellDraft(Row(doc, index));
    private static GlobalSpellInput[] Inputs(GlobalSpellDraft draft, params (string Field, string Value)[] edits) => GlobalSpellControls.MainFields.Select(field => new GlobalSpellInput(field,
        edits.LastOrDefault(edit => edit.Field == field) is var changed && changed.Field is not null ? changed.Value : draft.Display(field))).ToArray();
    private static string Invariant(int value) => value.ToString(CultureInfo.InvariantCulture);
    public static void Run()
    {
        using var source = JsonDocument.Parse(typeof(GlobalSpellChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.GlobalSpellSource.json")!);
        var oracle = source.RootElement;
        string[] Names(string key) => oracle.GetProperty(key).EnumerateArray().Select(item => item.GetString()!).ToArray();
        Expect(Names("PageFields").Length == 12 && Names("PageFields").SequenceEqual(GlobalSpellControls.MainFields), "Incomplete main spell page/source Save order.");
        Expect(Names("Fields").Length == 16 && Names("Fields").Order().SequenceEqual(GlobalSpellControls.StoredFields.Order()) && Names("NestedFields").SequenceEqual(new[] { "ShotData1", "ShotData2", "UpgShotData1", "UpgShotData2" }) && Names("HiddenPreservedFields").Length == 0, "Incomplete spell parent/two child slots.");
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "Source"))) root = root.Parent;
        foreach (var entry in oracle.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName, entry.Name)))).Equals(entry.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original spell input changed.");
        foreach (var entry in oracle.GetProperty("Controls").EnumerateObject())
            if (entry.Value.TryGetProperty("Catalogue", out var catalogue))
                Expect(GlobalSpellControls.Choices(entry.Name).Select(item => item.Value).SequenceEqual(oracle.GetProperty("Catalogues").GetProperty(catalogue.GetString()!).EnumerateArray().Select(item => Invariant(item.GetProperty("ID").GetInt32()))), "Original spell catalogue differs: " + entry.Name);
        foreach (var shot in oracle.GetProperty("Catalogues").GetProperty("GShots").EnumerateArray())
        {
            var option = GlobalProjectiles.Find(Invariant(shot.GetProperty("ID").GetInt32()))!;
            Expect(option.Form == shot.GetProperty("Variable").GetInt32() && option.Signed1 == (shot.GetProperty("Signed1").GetInt32() == 1) && option.Signed2 == (shot.GetProperty("Signed2").GetInt32() == 1) && option.Label1 == shot.GetProperty("Text1ID").GetInt32() && option.Label2 == shot.GetProperty("Text2ID").GetInt32(), "Original shot form/signedness/labels differ.");
            if (option.Form is 2 or 3 or 4)
                Expect(GlobalProjectiles.Targets(option).Select(item => item.Value).SequenceEqual(oracle.GetProperty("Catalogues").GetProperty(option.Form == 4 ? "Objects" : "GCreatures").EnumerateArray().Select(item => Invariant(item.GetProperty("ID").GetInt32()))), "Original child target catalogue differs.");
        }
        var directory = Path.Combine(Path.GetTempPath(), "DK2Spells", Guid.NewGuid().ToString("N"));
        var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory, "basis"));
        foreach (var doc in new[] { NativeMapDocument.CreateNewMap(new("Spells", "Spells", 20, 20)), native })
        {
            doc.SetHistoryDepth(65535); var initial = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
            foreach (var spell in oracle.GetProperty("Spells").EnumerateArray())
            {
                var index = spell.GetProperty("Index").GetInt32(); var current = Read(doc, index);
                Expect(Row(doc, index).ID == spell.GetProperty("ID").GetUInt32() && current.Editable, "Original spell order/layout differs.");
                doc.ApplyGlobalSpellDraft(doc.UpdateGlobalSpellDraft(current, Inputs(current))); Exact(doc, initial);
            }
            Expect(doc.GetHistory().Undo.Count == history, "Opening/no-op materialized spell defaults.");
            var baseline = Read(doc); var two = GlobalProjectiles.Options.First(option => option.Form == 0);
            var edits = GlobalSpellControls.MainFields.Select(field => (Field: field, Value: field is "ShotType" or "UpgShotType" ? Invariant((int)two.ID) : GlobalSpellControls.Named(field) ? GlobalSpellControls.Choices(field).Last().Value : GlobalSpellControls.Boolean(field) ? baseline.Raw(field) == "0" ? "1" : "0" : field == "ManaCost" ? "999999" : field == "RechargeTime" ? "1.5" : "1234")).ToArray();
            var prepared = doc.UpdateGlobalSpellDraft(baseline, Inputs(baseline, edits));
            prepared = doc.UpdateGlobalSpellShotDraft(prepared, "Shot", two.Signed1 ? "-9" : "111", two.Signed2 ? "-7" : "222");
            prepared = doc.UpdateGlobalSpellShotDraft(prepared, "Upgrade", two.Signed1 ? "-8" : "333", two.Signed2 ? "-6" : "444"); Exact(doc, initial);
            var expected = GlobalSpellControls.StoredFields.ToDictionary(field => field, prepared.Raw); doc.ApplyGlobalSpellDraft(prepared);
            Expect(expected.All(item => Read(doc).Raw(item.Key) == item.Value) && doc.GetHistory().Undo.Count == history + 1, "Complete parent/two children Apply split history or lost fields.");
            void Packed(NativeMapDocument packed, Dictionary<NativeFile, byte[]>? before)
            {
                var record = Row(packed).Record!; var all = before?.ToDictionary(item => item.Key, item => item.Value.ToArray());
                foreach (var field in GlobalSpellControls.StoredFields)
                {
                    var storage = oracle.GetProperty("NativeStorage").GetProperty(field); var offset = record.Offset + storage.GetProperty("Offset").GetInt32(); var size = storage.GetProperty("Size").GetInt32(); var number = long.Parse(expected[field], CultureInfo.InvariantCulture);
                    var mask = storage.TryGetProperty("Mask", out var flag) ? flag.GetByte() : (byte)0;
                    var actual = mask != 0 ? (record.File.Data.Span[offset] & mask) == 0 ? 0L : 1L : size == 1 ? record.File.Data.Span[offset] : size == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[offset..]) : BinaryPrimitives.ReadInt32LittleEndian(record.File.Data.Span[offset..]);
                    var stored = size == 2 && number < 0 ? unchecked((ushort)(short)number) : number;
                    Expect(actual == stored, "Independent spell native offset/flag differs: " + field);
                    if (all is null) continue;
                    if (mask != 0) all[record.File][offset] = (byte)((all[record.File][offset] & ~mask) | (number == 0 ? 0 : mask));
                    else if (size == 1) all[record.File][offset] = (byte)number;
                    else if (size == 2) BinaryPrimitives.WriteUInt16LittleEndian(all[record.File].AsSpan(offset), (ushort)stored);
                    else BinaryPrimitives.WriteInt32LittleEndian(all[record.File].AsSpan(offset), (int)number);
                }
                if (all is not null) Exact(packed, all);
            }
            if (!doc.IsLegacyProject) Packed(doc, initial);
            var applied = Snapshot(doc); var reopened = NativeMapDocument.Load(doc.SaveCopy(Path.Combine(directory, doc.IsLegacyProject ? "project" : "native")));
            Expect(expected.All(item => Read(reopened).Raw(item.Key) == item.Value), "Spell save/reopen lost composed values.");
            if (doc.IsLegacyProject) Packed(doc.CompileLegacyProject("SpellCompiled", native), null);
            doc.ApplyGlobalSpellDraft(doc.UpdateGlobalSpellDraft(Read(doc), Inputs(Read(doc)))); Exact(doc, applied);
            Expect(doc.GetHistory().Undo.Count == history + 1, "Spell no-op consumed history.");
            doc.Undo(); Exact(doc, initial); doc.Redo(); Exact(doc, applied); doc.Undo(); Exact(doc, initial); baseline = Read(doc);
            foreach (var entry in oracle.GetProperty("Controls").EnumerateObject())
            {
                if (entry.Value.TryGetProperty("Catalogue", out _))
                {
                    foreach (var choice in GlobalSpellControls.Choices(entry.Name))
                    {
                        var edit = doc.UpdateGlobalSpellDraft(baseline, Inputs(baseline, (entry.Name, choice.Value))); doc.ApplyGlobalSpellDraft(edit);
                        Expect(Read(doc).Raw(entry.Name) == choice.Value, "Original spell choice refused."); if (edit.Changed) doc.Undo(); Exact(doc, initial);
                    }
                    Reject(() => doc.UpdateGlobalSpellDraft(baseline, Inputs(baseline, (entry.Name, "254")))); continue;
                }
                var fractional = entry.Value.TryGetProperty("Fractional", out var fraction) && fraction.GetBoolean();
                var maximum = fractional ? 32767m + 4095m / 4096m : entry.Value.GetProperty("Maximum").GetDecimal();
                foreach (var value in new[] { "0", maximum.ToString(CultureInfo.InvariantCulture) })
                {
                    var edit = doc.UpdateGlobalSpellDraft(baseline, Inputs(baseline, (entry.Name, value))); doc.ApplyGlobalSpellDraft(edit);
                    Expect(decimal.Parse(Read(doc).Display(entry.Name), CultureInfo.InvariantCulture) == decimal.Parse(value, CultureInfo.InvariantCulture), "Spell endpoint differs.");
                    if (edit.Changed) doc.Undo(); Exact(doc, initial);
                }
                foreach (var value in new[] { "-1", (maximum + 1).ToString(CultureInfo.InvariantCulture), "1e2", "bad" })
                { Reject(() => doc.UpdateGlobalSpellDraft(baseline, Inputs(baseline, ("ManaCost", "222"), (entry.Name, value)))); Exact(doc, initial); }
                if (entry.Name is "ManaCost" or "ManaDrain" or "UpgRechargeTime" or "RechargeTime")
                {
                    var format = GlobalDefinitionNumbers.Format("Spells", entry.Name, !doc.IsLegacyProject);
                    Expect(format.Minimum == 0 && format.Maximum == maximum && format.Fractional == fractional, "Generic numeric table lost source spell range/units.");
                }
            }
            Reject(() => doc.UpdateGlobalSpellDraft(baseline, Inputs(baseline, ("UpgRechargeTime", "1.5"))));
            CheckNested(doc, initial, baseline, native);
            Reject(() => doc.UpdateGlobalSpellDraft(baseline, Inputs(baseline).Skip(1)));
            Reject(() => doc.UpdateGlobalSpellDraft(baseline, Inputs(baseline).Append(new("ShotData1", "0"))));
            Reject(() => doc.UpdateGlobalSpellDraft(baseline, Inputs(baseline).Append(new("ManaCost", "1"))));
            Reject(() => NativeMapDocument.CreateNewMap(new("Foreign", "Foreign", 12, 12)).ApplyGlobalSpellDraft(baseline));
            Reject(() => doc.CreateGlobalSpellDraft(doc.GlobalDefinitionRows().First(row => row.Family == "Rooms")));
            doc.SetGlobalDefinitionValues([new("Spells", 0, "UpgShotData2", baseline.Raw("UpgShotData2") == "0" ? "1" : "0")]); var stale = Snapshot(doc);
            Reject(() => doc.ApplyGlobalSpellDraft(baseline)); Exact(doc, stale); doc.Undo(); Exact(doc, initial);
            doc.SetGlobalDefinitionValues([new("Spells", 0, "HandAnim", "254"), new("Spells", 0, "CastRule", "253"), new("Spells", 0, "ShotType", "252"), new("Spells", 0, "UpgShotType", "251"), new("Spells", 0, "ManaCost", "2000000"), new("Spells", 0, "ManaDrain", "65535")]);
            var imported = Snapshot(doc); var importDraft = Read(doc);
            doc.ApplyGlobalSpellDraft(doc.UpdateGlobalSpellDraft(importDraft, Inputs(importDraft))); Exact(doc, imported);
            Reject(() => doc.UpdateGlobalSpellShotDraft(importDraft, "Shot", "1", "1")); Reject(() => doc.UpdateGlobalSpellShotDraft(importDraft, "Upgrade", "1", "1"));
            doc.ApplyGlobalSpellDraft(doc.UpdateGlobalSpellDraft(importDraft, Inputs(importDraft, ("HandAnim", "0"))));
            Expect(Read(doc).Raw("CastRule") == "253" && Read(doc).Raw("UpgShotType") == "251" && Read(doc).Raw("ManaCost") == "2000000" && Read(doc).Raw("ManaDrain") == "65535", "Spell repair normalized unrelated imports.");
            doc.Undo(); Exact(doc, imported); doc.Undo(); Exact(doc, initial);
            Console.WriteLine($"SPELL DETAIL native={!doc.IsLegacyProject} main=12 nested=4 rows=13 slots=2 shot-types=66 upgrade-recharge-integer full-catalogues packed-bit-preservation private-compose atomic-history compiler reopen stale foreign imports");
        }
        var missing = NativeMapDocument.Create(12, 12, "Missing"); Reject(() => missing.ApplyGlobalSpellDraft(Read(missing)));
    }
    private static void CheckNested(NativeMapDocument doc, Dictionary<NativeFile, byte[]> initial, GlobalSpellDraft baseline, NativeMapDocument compilerBasis)
    {
        Reject(() => doc.UpdateGlobalSpellShotDraft(baseline, "Invalid", "0", "0"));
        foreach (var slot in new[] { "Shot", "Upgrade" })
        {
            // Test literals are independent of the production slot field mapper.
            var type = slot == "Shot" ? "ShotType" : "UpgShotType"; var first = slot == "Shot" ? "ShotData1" : "UpgShotData1"; var second = slot == "Shot" ? "ShotData2" : "UpgShotData2";
            var otherFirst = slot == "Shot" ? "UpgShotData1" : "ShotData1"; var otherSecond = slot == "Shot" ? "UpgShotData2" : "ShotData2";
            foreach (var option in GlobalProjectiles.Options)
            {
                var parent = doc.UpdateGlobalSpellDraft(baseline, Inputs(baseline, (type, Invariant((int)option.ID))));
                Expect(parent.Raw(first) == baseline.Raw(first) && parent.Raw(second) == baseline.Raw(second), "Parent type change modified unsubmitted child arguments.");
                if (option.Form == 255) { Reject(() => doc.UpdateGlobalSpellShotDraft(parent, slot, "0", "0")); continue; }
                GlobalSpellDraft submitted;
                if (option.Form is 0 or 1)
                {
                    submitted = doc.UpdateGlobalSpellShotDraft(parent, slot, option.Signed1 ? "-32768" : "0", option.Signed2 ? "-32768" : "0");
                    Expect(submitted.Display(first) == (option.Signed1 ? "-32768" : "0") && (option.Form == 0 || submitted.Raw(second) == baseline.Raw(second)), "Signed child/form1 second retention differs.");
                    Reject(() => doc.UpdateGlobalSpellShotDraft(parent, slot, "32768", "0"));
                    if (option.Form == 0) Reject(() => doc.UpdateGlobalSpellShotDraft(parent, slot, "0", "32768"));
                }
                else
                {
                    foreach (var target in GlobalProjectiles.Targets(option))
                    {
                        var child = doc.UpdateGlobalSpellShotDraft(parent, slot, target.Value, option.Form == 3 ? "10" : option.Form == 2 ? "100" : "65535");
                        Expect(child.Raw(first) == target.Value && (option.Form != 4 || child.Raw(second) == (target.Value == "1" ? "65535" : "0")), "Complete child target/source object zero differs.");
                    }
                    submitted = doc.UpdateGlobalSpellShotDraft(parent, slot, GlobalProjectiles.Targets(option)[0].Value, "1");
                    Reject(() => doc.UpdateGlobalSpellShotDraft(parent, slot, "65534", "1"));
                    if (option.Form is 2 or 3) Reject(() => doc.UpdateGlobalSpellShotDraft(parent, slot, GlobalProjectiles.Targets(option)[0].Value, option.Form == 2 ? "101" : "11"));
                    if (option.Form == 4)
                    {
                        if (doc.IsLegacyProject) Expect(doc.UpdateGlobalSpellShotDraft(parent, slot, "1", "80000").Raw(second) == "80000", "Source project gold maximum refused.");
                        else Reject(() => doc.UpdateGlobalSpellShotDraft(parent, slot, "1", "80000"));
                        Reject(() => doc.UpdateGlobalSpellShotDraft(parent, slot, "1", "80001"));
                    }
                }
                Expect(submitted.Raw(otherFirst) == baseline.Raw(otherFirst) && submitted.Raw(otherSecond) == baseline.Raw(otherSecond), "Submitting one child slot changed the other slot."); Exact(doc, initial);
            }
            var objectType = GlobalProjectiles.Options.First(option => option.Form == 4); var objectID = GlobalProjectiles.Targets(objectType).First(choice => choice.Value != "1").Value;
            doc.SetGlobalDefinitionValues([new("Spells", 0, type, Invariant((int)objectType.ID)), new("Spells", 0, first, objectID), new("Spells", 0, second, "1234")]);
            var imported = Snapshot(doc); var draft = Read(doc); doc.ApplyGlobalSpellDraft(doc.UpdateGlobalSpellDraft(draft, Inputs(draft))); Exact(doc, imported);
            var childOK = doc.UpdateGlobalSpellShotDraft(draft, slot, objectID, "ignored disabled gold"); Exact(doc, imported);
            Expect(childOK.Raw(second) == "0", "Explicit child non-gold Save retained argument."); doc.ApplyGlobalSpellDraft(childOK); doc.Undo(); Exact(doc, imported); doc.Undo(); Exact(doc, initial);
            var signedType = GlobalProjectiles.Options.First(option => option.Form is 0 or 1 && option.Signed1);
            doc.SetGlobalDefinitionValues([new("Spells", 0, type, Invariant((int)signedType.ID)), new("Spells", 0, first, "65527")]);
            var signedImport = Snapshot(doc); var signedDraft = Read(doc);
            Expect(signedDraft.Display(first) == "-9", "Imported signed WORD alias display differs.");
            var signedNoop = doc.UpdateGlobalSpellShotDraft(signedDraft, slot, "-9", signedDraft.Display(second));
            Expect(signedNoop.Raw(first) == "65527", "Unchanged signed child normalized WORD alias."); doc.ApplyGlobalSpellDraft(signedNoop); Exact(doc, signedImport); doc.Undo(); Exact(doc, initial);
            if (doc.IsLegacyProject)
                foreach (var noSecond in GlobalProjectiles.Options.Where(option => option.Form is 1 or 255))
                {
                    doc.SetGlobalDefinitionValues([new("Spells", 0, type, Invariant((int)noSecond.ID)), new("Spells", 0, first, "123"), new("Spells", 0, second, "456")]);
                    var retained = Snapshot(doc); var parent = Read(doc); if (noSecond.Form == 1) parent = doc.UpdateGlobalSpellShotDraft(parent, slot, "123", "ignored second");
                    doc.ApplyGlobalSpellDraft(parent); Exact(doc, retained); var compiled = doc.CompileLegacyProject("SpellInactive", compilerBasis);
                    Expect(Read(compiled).Raw(second) == "0" && Read(doc).Raw(second) == "456", "Compiler-only zero changed retained parent argument."); doc.Undo(); Exact(doc, initial);
                }
        }
    }
}
