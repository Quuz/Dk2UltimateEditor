using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class GlobalTrapChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid trap input accepted."); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument doc, Dictionary<NativeFile, byte[]> bytes) => Expect(doc.Files.Count == bytes.Count && doc.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Trap operation changed unrelated/retained/undo bytes.");
    private static GlobalDefinitionRow Row(NativeMapDocument doc, int index = 0) => doc.GlobalDefinitionRows().Single(row => row.Family == "Traps" && row.Index == index);
    private static GlobalTrapDraft Read(NativeMapDocument doc, int index = 0) => doc.CreateGlobalTrapDraft(Row(doc, index));
    private static GlobalTrapInput[] Inputs(GlobalTrapDraft draft, params (string Field, string Value)[] edits) => GlobalTrapControls.MainFields.Select(field => new GlobalTrapInput(field,
        edits.LastOrDefault(edit => edit.Field == field) is var changed && changed.Field is not null ? changed.Value : draft.Display(field))).ToArray();
    private static string Invariant(int value) => value.ToString(CultureInfo.InvariantCulture);
    public static void Run()
    {
        using var source = JsonDocument.Parse(typeof(GlobalTrapChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.GlobalTrapSource.json")!);
        var oracle = source.RootElement;
        string[] Names(string key) => oracle.GetProperty(key).EnumerateArray().Select(item => item.GetString()!).ToArray();
        Expect(Names("PageFields").Length == 24 && Names("PageFields").SequenceEqual(GlobalTrapControls.MainFields), "Incomplete main trap page/source Save order.");
        Expect(Names("Fields").Length == 28 && Names("Fields").Order().SequenceEqual(GlobalTrapControls.StoredFields.Order()) && Names("NestedFields").Length == 3 && Names("HiddenPreservedFields").SequenceEqual(new[] { "ShotDelay" }), "Incomplete parent/nested/hidden storage.");
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "Source"))) root = root.Parent;
        foreach (var entry in oracle.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName, entry.Name)))).Equals(entry.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original trap input changed.");
        foreach (var entry in oracle.GetProperty("Controls").EnumerateObject())
            if (entry.Value.TryGetProperty("Catalogue", out var catalogue))
                Expect(GlobalTrapControls.Choices(entry.Name).Select(item => item.Value).SequenceEqual(oracle.GetProperty("Catalogues").GetProperty(catalogue.GetString()!).EnumerateArray().Select(item => Invariant(item.GetProperty("ID").GetInt32()))), "Original trap catalogue differs: " + entry.Name);
        foreach (var shot in oracle.GetProperty("Catalogues").GetProperty("GShots").EnumerateArray())
        {
            var option = GlobalProjectiles.Find(Invariant(shot.GetProperty("ID").GetInt32()))!;
            Expect(option.Form == shot.GetProperty("Variable").GetInt32() && option.Signed1 == (shot.GetProperty("Signed1").GetInt32() == 1) && option.Signed2 == (shot.GetProperty("Signed2").GetInt32() == 1) && option.Label1 == shot.GetProperty("Text1ID").GetInt32() && option.Label2 == shot.GetProperty("Text2ID").GetInt32(), "Original shot form/signedness/labels differ.");
            if (option.Form is 2 or 3 or 4)
                Expect(GlobalProjectiles.Targets(option).Select(item => item.Value).SequenceEqual(oracle.GetProperty("Catalogues").GetProperty(option.Form == 4 ? "Objects" : "GCreatures").EnumerateArray().Select(item => Invariant(item.GetProperty("ID").GetInt32()))), "Original child target catalogue differs.");
        }
        var directory = Path.Combine(Path.GetTempPath(), "DK2Traps", Guid.NewGuid().ToString("N"));
        var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory, "basis"));
        foreach (var doc in new[] { NativeMapDocument.CreateNewMap(new("Traps", "Traps", 20, 20)), native })
        {
            doc.SetHistoryDepth(65535); var initial = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
            foreach (var trap in oracle.GetProperty("Traps").EnumerateArray())
            {
                var index = trap.GetProperty("Index").GetInt32(); var current = Read(doc, index);
                Expect(Row(doc, index).ID == trap.GetProperty("ID").GetUInt32() && current.Editable, "Original trap order/layout differs.");
                doc.ApplyGlobalTrapDraft(doc.UpdateGlobalTrapDraft(current, Inputs(current))); Exact(doc, initial);
                var inactive = NamesFrom(oracle.GetProperty("MagicDoorPolicy").GetProperty("DisabledFields"));
                foreach (var field in GlobalTrapControls.MainFields)
                {
                    var enabled = index != 14 || !inactive.Contains(field);
                    Expect(GlobalTrapControls.Enabled(index, field) == enabled, "Source Magic Door availability differs: " + field);
                    if (!enabled)
                    {
                        var changed = GlobalTrapControls.Named(field) ? GlobalTrapControls.Choices(field).First(choice => choice.Value != current.Raw(field)).Value : current.Raw(field) == "0" ? "1" : "0";
                        Reject(() => doc.UpdateGlobalTrapDraft(current, Inputs(current, (field, changed)))); Exact(doc, initial);
                    }
                }
                Expect(!GlobalTrapControls.Enabled(index, "ShotDelay"), "Hidden ShotDelay exposed as an editing control.");
            }
            Expect(doc.GetHistory().Undo.Count == history, "Opening/no-op materialized trap defaults.");
            var baseline = Read(doc);
            var edits = GlobalTrapControls.MainFields.Select(field => (Field: field, Value: GlobalTrapControls.Named(field) ? GlobalTrapControls.Choices(field).Last().Value : GlobalTrapControls.Boolean(field) ? baseline.Raw(field) == "0" ? "1" : "0" : GlobalDefinitionNumbers.Format("Traps", field).Fractional ? "1.5" : "1234")).ToArray();
            // A known two-number projectile lets the parent and both nested pages
            // participate in one transaction, without exercising the live APIs.
            var two = GlobalProjectiles.Options.First(option => option.Form == 0);
            edits = edits.Select(edit => edit.Field == "ShotType" ? (edit.Field, Invariant((int)two.ID)) : edit).ToArray();
            var prepared = doc.UpdateGlobalTrapDraft(baseline, Inputs(baseline, edits));
            prepared = doc.UpdateGlobalTrapTriggerDraft(prepared, "321");
            prepared = doc.UpdateGlobalTrapShotDraft(prepared, two.Signed1 ? "-9" : "111", two.Signed2 ? "-7" : "222"); Exact(doc, initial);
            var expected = GlobalTrapControls.StoredFields.ToDictionary(field => field, prepared.Raw);
            doc.ApplyGlobalTrapDraft(prepared);
            Expect(GlobalTrapControls.StoredFields.All(field => Read(doc).Raw(field) == expected[field]) && doc.GetHistory().Undo.Count == history + 1, "Complete parent/child Apply split history or lost fields.");
            void Packed(NativeMapDocument packed, Dictionary<NativeFile, byte[]>? before)
            {
                var record = Row(packed).Record!; var all = before?.ToDictionary(item => item.Key, item => item.Value.ToArray());
                foreach (var field in GlobalTrapControls.StoredFields)
                {
                    var storage = oracle.GetProperty("NativeStorage").GetProperty(field); var offset = record.Offset + storage.GetProperty("Offset").GetInt32(); var size = storage.GetProperty("Size").GetInt32();
                    var number = long.Parse(expected[field], CultureInfo.InvariantCulture);
                    var mask = storage.TryGetProperty("Mask", out var flag) ? flag.GetByte() : (byte)0;
                    var actual = mask != 0 ? (record.File.Data.Span[offset] & mask) == 0 ? 0L : 1L : size == 1 ? record.File.Data.Span[offset] : size == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[offset..]) : BinaryPrimitives.ReadInt32LittleEndian(record.File.Data.Span[offset..]);
                    var stored = size == 2 && number < 0 ? unchecked((ushort)(short)number) : number;
                    Expect(actual == stored, "Independent trap native offset/mask differs: " + field);
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
            Expect(expected.All(item => Read(reopened).Raw(item.Key) == item.Value), "Trap save/reopen lost composed values.");
            if (doc.IsLegacyProject) Packed(doc.CompileLegacyProject("TrapCompiled", native), null);
            doc.ApplyGlobalTrapDraft(doc.UpdateGlobalTrapDraft(Read(doc), Inputs(Read(doc)))); Exact(doc, applied);
            Expect(doc.GetHistory().Undo.Count == history + 1, "Trap no-op consumed history.");
            doc.Undo(); Exact(doc, initial); doc.Redo(); Exact(doc, applied); doc.Undo(); Exact(doc, initial);
            baseline = Read(doc);
            foreach (var entry in oracle.GetProperty("Controls").EnumerateObject())
            {
                if (entry.Value.TryGetProperty("Catalogue", out _))
                {
                    foreach (var choice in GlobalTrapControls.Choices(entry.Name))
                    {
                        var edit = doc.UpdateGlobalTrapDraft(baseline, Inputs(baseline, (entry.Name, choice.Value)));
                        doc.ApplyGlobalTrapDraft(edit); Expect(Read(doc).Raw(entry.Name) == choice.Value, "Original trap choice refused.");
                        if (edit.Changed) doc.Undo(); Exact(doc, initial);
                    }
                    Reject(() => doc.UpdateGlobalTrapDraft(baseline, Inputs(baseline, (entry.Name, entry.Name.EndsWith("Effect", StringComparison.Ordinal) ? "371" : "254"))));
                    continue;
                }
                var fractional = entry.Value.TryGetProperty("Fractional", out var fraction) && fraction.GetBoolean();
                var maximum = fractional ? 16000m + 4095m / 4096m : entry.Value.GetProperty("Maximum").GetDecimal();
                foreach (var value in new[] { "0", maximum.ToString(CultureInfo.InvariantCulture) })
                {
                    var edit = doc.UpdateGlobalTrapDraft(baseline, Inputs(baseline, (entry.Name, value))); doc.ApplyGlobalTrapDraft(edit);
                    Expect(decimal.Parse(Read(doc).Display(entry.Name), CultureInfo.InvariantCulture) == decimal.Parse(value, CultureInfo.InvariantCulture), "Trap endpoint differs.");
                    if (edit.Changed) doc.Undo(); Exact(doc, initial);
                }
                foreach (var value in new[] { "-1", (maximum + 1).ToString(CultureInfo.InvariantCulture), "1e2", "bad" })
                { Reject(() => doc.UpdateGlobalTrapDraft(baseline, Inputs(baseline, ("Material", "7"), (entry.Name, value)))); Exact(doc, initial); }
            }
            CheckNested(doc, initial, baseline, native);
            Reject(() => doc.UpdateGlobalTrapDraft(baseline, Inputs(baseline).Skip(1)));
            Reject(() => doc.UpdateGlobalTrapDraft(baseline, Inputs(baseline).Append(new("ShotDelay", "0"))));
            Reject(() => doc.UpdateGlobalTrapDraft(baseline, Inputs(baseline).Append(new("Health", "1"))));
            Reject(() => NativeMapDocument.CreateNewMap(new("Foreign", "Foreign", 12, 12)).ApplyGlobalTrapDraft(baseline));
            Reject(() => doc.CreateGlobalTrapDraft(doc.GlobalDefinitionRows().First(row => row.Family == "Rooms")));
            doc.SetGlobalDefinitionValues([new("Traps", 0, "ShotDelay", baseline.Raw("ShotDelay") == "0" ? "4096" : "0")]); var stale = Snapshot(doc);
            Reject(() => doc.ApplyGlobalTrapDraft(baseline)); Exact(doc, stale); doc.Undo(); Exact(doc, initial);
            doc.SetGlobalDefinitionValues([new("Traps", 0, "Material", "254"), new("Traps", 0, "DeathEffect", "63000"), new("Traps", 0, "TriggerType", "253"), new("Traps", 0, "ShotType", "252"), new("Traps", 0, "Health", doc.IsLegacyProject ? "100000" : "65535")]);
            var imported = Snapshot(doc); var importDraft = Read(doc);
            doc.ApplyGlobalTrapDraft(doc.UpdateGlobalTrapDraft(importDraft, Inputs(importDraft))); Exact(doc, imported);
            Reject(() => doc.UpdateGlobalTrapTriggerDraft(importDraft, "1")); Reject(() => doc.UpdateGlobalTrapShotDraft(importDraft, "1", "1"));
            doc.ApplyGlobalTrapDraft(doc.UpdateGlobalTrapDraft(importDraft, Inputs(importDraft, ("Material", "0"))));
            Expect(Read(doc).Raw("DeathEffect") == "63000" && Read(doc).Raw("TriggerType") == "253" && Read(doc).Raw("Health") == importDraft.Raw("Health"), "Trap repair normalized unrelated imports.");
            doc.Undo(); Exact(doc, imported); doc.Undo(); Exact(doc, initial);
            Console.WriteLine($"TRAP DETAIL native={!doc.IsLegacyProject} main=24 nested=3 hidden=1 rows=15 shot-types=66 MagicDoor-disabled=14 full-catalogues packed-bit-preservation private-compose atomic-history compiler reopen stale foreign imports");
        }
        var missing = NativeMapDocument.Create(12, 12, "Missing"); Reject(() => missing.ApplyGlobalTrapDraft(Read(missing)));
    }
    private static string[] NamesFrom(JsonElement list) => list.EnumerateArray().Select(item => item.GetString()!).ToArray();
    private static void CheckNested(NativeMapDocument doc, Dictionary<NativeFile, byte[]> initial, GlobalTrapDraft baseline, NativeMapDocument compilerBasis)
    {
        foreach (var trigger in new[] { "1", "2", "3" })
        {
            var parent = doc.UpdateGlobalTrapDraft(baseline, Inputs(baseline, ("TriggerType", trigger)));
            foreach (var parameter in new[] { "0", "32767" }) Expect(doc.UpdateGlobalTrapTriggerDraft(parent, parameter).Raw("TriggerData") == parameter, "Trigger endpoint differs.");
            foreach (var parameter in new[] { "-1", "32768", "1.5", "bad" }) Reject(() => doc.UpdateGlobalTrapTriggerDraft(parent, parameter));
        }
        foreach (var option in GlobalProjectiles.Options)
        {
            var parent = doc.UpdateGlobalTrapDraft(baseline, Inputs(baseline, ("ShotType", Invariant((int)option.ID))));
            Expect(parent.Raw("ShotData1") == baseline.Raw("ShotData1") && parent.Raw("ShotData2") == baseline.Raw("ShotData2"), "Changing parent type modified unsubmitted child options.");
            if (option.Form == 255) { Reject(() => doc.UpdateGlobalTrapShotDraft(parent, "0", "0")); continue; }
            if (option.Form is 0 or 1)
            {
                foreach (var first in new[] { option.Signed1 ? "-32768" : "0", "32767" })
                {
                    var child = doc.UpdateGlobalTrapShotDraft(parent, first, option.Signed2 ? "-32768" : "0");
                    Expect(child.Display("ShotData1") == first && (option.Form == 0 || child.Raw("ShotData2") == baseline.Raw("ShotData2")), "Source signed child/form1 retention differs.");
                }
                Reject(() => doc.UpdateGlobalTrapShotDraft(parent, "32768", "0"));
                if (option.Form == 0) Reject(() => doc.UpdateGlobalTrapShotDraft(parent, "0", "32768"));
            }
            else
            {
                foreach (var target in GlobalProjectiles.Targets(option))
                {
                    var child = doc.UpdateGlobalTrapShotDraft(parent, target.Value, option.Form == 3 ? "10" : option.Form == 2 ? "100" : "65535");
                    Expect(child.Raw("ShotData1") == target.Value && (option.Form != 4 || child.Raw("ShotData2") == (target.Value == "1" ? "65535" : "0")), "Complete child target/source object zero differs.");
                }
                Reject(() => doc.UpdateGlobalTrapShotDraft(parent, "65534", "1"));
                if (option.Form is 2 or 3) Reject(() => doc.UpdateGlobalTrapShotDraft(parent, GlobalProjectiles.Targets(option)[0].Value, option.Form == 2 ? "101" : "11"));
                if (option.Form == 4)
                {
                    if (doc.IsLegacyProject) Expect(doc.UpdateGlobalTrapShotDraft(parent, "1", "80000").Raw("ShotData2") == "80000", "Source project gold maximum refused.");
                    else Reject(() => doc.UpdateGlobalTrapShotDraft(parent, "1", "80000"));
                    Reject(() => doc.UpdateGlobalTrapShotDraft(parent, "1", "80001"));
                }
            }
            Exact(doc, initial);
        }
        // Imported arguments may be retained on the parent, but explicit object
        // options OK must zero gold for a non-gold object even if it is unchanged.
        var objectType = GlobalProjectiles.Options.First(option => option.Form == 4);
        var objectID = GlobalProjectiles.Targets(objectType).First(choice => choice.Value != "1").Value;
        doc.SetGlobalDefinitionValues([new("Traps", 0, "ShotType", Invariant((int)objectType.ID)), new("Traps", 0, "ShotData1", objectID), new("Traps", 0, "ShotData2", "1234")]);
        var imported = Snapshot(doc); var draft = Read(doc);
        doc.ApplyGlobalTrapDraft(doc.UpdateGlobalTrapDraft(draft, Inputs(draft))); Exact(doc, imported);
        var childOK = doc.UpdateGlobalTrapShotDraft(draft, objectID, "bad ignored disabled gold"); Exact(doc, imported);
        Expect(childOK.Raw("ShotData2") == "0", "Explicit child Save retained non-gold argument.");
        doc.ApplyGlobalTrapDraft(childOK); doc.Undo(); Exact(doc, imported); doc.Undo(); Exact(doc, initial);
        var signedType = GlobalProjectiles.Options.First(option => option.Form is 0 or 1 && option.Signed1);
        doc.SetGlobalDefinitionValues([new("Traps", 0, "ShotType", Invariant((int)signedType.ID)), new("Traps", 0, "ShotData1", "65527")]);
        var signedImport = Snapshot(doc); var signedDraft = Read(doc);
        Expect(signedDraft.Display("ShotData1") == "-9", "Imported signed WORD alias display differs.");
        var signedNoop = doc.UpdateGlobalTrapShotDraft(signedDraft, "-9", signedDraft.Display("ShotData2"));
        Expect(signedNoop.Raw("ShotData1") == "65527", "Unchanged signed child normalized WORD alias.");
        doc.ApplyGlobalTrapDraft(signedNoop); Exact(doc, signedImport);
        doc.Undo(); Exact(doc, initial);
        if (doc.IsLegacyProject)
        {
            foreach (var type in GlobalProjectiles.Options.Where(option => option.Form is 1 or 255))
            {
                doc.SetGlobalDefinitionValues([new("Traps", 0, "ShotType", Invariant((int)type.ID)), new("Traps", 0, "ShotData1", "123"), new("Traps", 0, "ShotData2", "456")]);
                var retained = Snapshot(doc); var parent = Read(doc);
                if (type.Form == 1) parent = doc.UpdateGlobalTrapShotDraft(parent, "123", "ignored second");
                doc.ApplyGlobalTrapDraft(parent); Exact(doc, retained);
                var compiled = doc.CompileLegacyProject("TrapInactive", compilerBasis);
                Expect(Read(compiled).Raw("ShotData2") == "0" && Read(doc).Raw("ShotData2") == "456", "Compiler-only zero changed retained parent argument.");
                doc.Undo(); Exact(doc, initial);
            }
        }
    }
}
