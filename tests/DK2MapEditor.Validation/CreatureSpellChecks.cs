using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static class CreatureSpellChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid creature spell action accepted."); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument doc, Dictionary<NativeFile, byte[]> bytes) => Expect(doc.Files.Count == bytes.Count && doc.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Creature spell changed unrelated/private/canceled/undo bytes.");
    private static GlobalDefinitionRow Row(NativeMapDocument doc, int index = 0) => doc.GlobalDefinitionRows().Single(row => row.Family == "CrSpells" && row.Index == index);
    private static CreatureSpellDraft Read(NativeMapDocument doc, int index = 0) => doc.CreateCreatureSpellDraft(Row(doc, index));
    private static CreatureSpellInput[] Inputs(CreatureSpellDraft draft, params (string Field, string Value)[] edits) => CreatureSpellControls.MainFields.Select(field => new CreatureSpellInput(field,
        edits.LastOrDefault(edit => edit.Field == field) is var changed && changed.Field is not null ? changed.Value : draft.Display(field))).ToArray();
    private static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    public static void Run()
    {
        using var source = JsonDocument.Parse(typeof(CreatureSpellChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.CreatureGlobalsSessionSource.json")!);
        using var projectileSource = JsonDocument.Parse(typeof(CreatureSpellChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.GlobalSpellSource.json")!);
        var oracle = source.RootElement; var projectiles = projectileSource.RootElement;
        var save = oracle.GetProperty("Functions").GetProperty("Properties_GlobalSaveChangesCr").GetString()!;
        var sourceMain = Regex.Matches(save, @"Context->Variables\.CrSpells\[Context->CurrentSpell\]\.(\w+)\s*=").Select(match => match.Groups[1].Value).ToArray();
        Expect(sourceMain.Length == 9 && sourceMain.SequenceEqual(CreatureSpellControls.MainFields), "Creature spell main controls/source Save order differ.");
        Expect(oracle.GetProperty("Pages")[8].GetProperty("Fields").EnumerateArray().Select(field => field.GetString()).Order().SequenceEqual(CreatureSpellControls.StoredFields.Order()) && CreatureSpellControls.StoredFields.Count == 11, "Incomplete creature spell stored/child fields.");
        var proc = oracle.GetProperty("Functions").GetProperty("Properties_GlobalCrSpellsProc").GetString()!;
        Expect(proc.Contains("case 104:") && proc.Contains("case 106:") && proc.Contains("if (lData > 1024)") && proc.Contains("1024<<12"), "Original creature spell numeric controls changed.");
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "Source"))) root = root.Parent;
        foreach (var scope in new[] { oracle, projectiles })
            foreach (var entry in scope.GetProperty("InputHashes").EnumerateObject())
                Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName, entry.Name)))).Equals(entry.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original creature spell/projectile input changed.");
        var compiler = File.ReadAllText(Path.Combine(root!.FullName, "Source", "Sources", "Compiler", "Compiler_Generate.c"));
        var compilerStart = compiler.IndexOf("int Compiler_GenerateCreatureSpellsFile(", StringComparison.Ordinal);
        var compilerEnd = compiler.IndexOf("void Compiler_SetDate(", compilerStart, StringComparison.Ordinal);
        var spellCompiler = compiler[compilerStart..compilerEnd];
        Expect(spellCompiler.Contains("*((WORD *)(CrSpell+0x00CC)) = (WORD)MapGlobalVariables.CrSpells[Offset].ShotData2;") && !spellCompiler.Contains("GShots"), "Creature spell compiler must directly cast both operands, without keeper-spell form-dependent zeroing.");
        var catalogues = projectiles.GetProperty("Catalogues");
        Expect(CreatureSpellControls.Choices("Shot").Select(choice => choice.Value).SequenceEqual(catalogues.GetProperty("GShots").EnumerateArray().Select(item => Number(item.GetProperty("ID").GetInt32()))), "Creature spell projectile catalogue differs.");
        foreach (var shot in catalogues.GetProperty("GShots").EnumerateArray())
        {
            var option = GlobalProjectiles.Find(Number(shot.GetProperty("ID").GetInt32()))!;
            Expect(option.Form == shot.GetProperty("Variable").GetInt32() && option.Signed1 == (shot.GetProperty("Signed1").GetInt32() == 1) && option.Signed2 == (shot.GetProperty("Signed2").GetInt32() == 1), "Source child form/signedness differs.");
            if (option.Form is 2 or 3 or 4)
                Expect(GlobalProjectiles.Targets(option).Select(choice => choice.Value).SequenceEqual(catalogues.GetProperty(option.Form == 4 ? "Objects" : "GCreatures").EnumerateArray().Select(item => Number(item.GetProperty("ID").GetInt32()))), "Source creature/object targets differ.");
        }
        var directory = Path.Combine(Path.GetTempPath(), "DK2CreatureSpells", Guid.NewGuid().ToString("N"));
        var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory, "basis"));
        foreach (var doc in new[] { NativeMapDocument.CreateNewMap(new("CreatureSpells", "CreatureSpells", 20, 20)), native })
        {
            doc.SetHistoryDepth(65535); var initial = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
            foreach (var spell in oracle.GetProperty("CrSpells").EnumerateArray())
            {
                var index = spell.GetProperty("Index").GetInt32(); var current = Read(doc, index);
                Expect(Row(doc, index).ID == spell.GetProperty("ID").GetUInt32() && current.Editable, "Original creature spell row unavailable/reordered.");
                doc.ApplyCreatureSpellDraft(doc.UpdateCreatureSpellDraft(current, Inputs(current))); Exact(doc, initial);
            }
            Expect(doc.GetHistory().Undo.Count == history, "Creature spell no-op materialized defaults or history.");
            var baseline = Read(doc); var two = GlobalProjectiles.Options.First(option => option.Form == 0);
            var edits = CreatureSpellControls.MainFields.Select(field => (Field: field, Value: field == "Shot" ? Number(two.ID) : CreatureSpellControls.Boolean(field) ? baseline.Raw(field) == "0" ? "1" : "0" : field == "RechargeTime" ? "1.5" : "2.25")).ToArray();
            var prepared = doc.UpdateCreatureSpellDraft(baseline, Inputs(baseline, edits));
            prepared = doc.UpdateCreatureSpellShotDraft(prepared, "Shot", two.Signed1 ? "-9" : "111", two.Signed2 ? "-7" : "222"); Exact(doc, initial);
            var expected = CreatureSpellControls.StoredFields.ToDictionary(field => field, prepared.Raw); doc.ApplyCreatureSpellDraft(prepared);
            Expect(expected.All(item => Read(doc).Raw(item.Key) == item.Value) && doc.GetHistory().Undo.Count == history + 1, "Creature spell parent/child Apply lost fields or split Undo.");
            void Packed(NativeMapDocument packed, Dictionary<NativeFile, byte[]>? before)
            {
                var record = Row(packed).Record!; var all = before?.ToDictionary(item => item.Key, item => item.Value.ToArray());
                foreach (var field in CreatureSpellControls.StoredFields)
                {
                    var storage = oracle.GetProperty("NativeStorage").GetProperty("CrSpells").GetProperty(field); var offset = record.Offset + storage.GetProperty("Offset").GetInt32(); var size = storage.GetProperty("Size").GetInt32(); var value = long.Parse(expected[field], CultureInfo.InvariantCulture);
                    var mask = storage.TryGetProperty("Mask", out var flag) ? flag.GetByte() : (byte)0;
                    var actual = mask != 0 ? (record.File.Data.Span[offset] & mask) == 0 ? 0L : 1L : size == 1 ? record.File.Data.Span[offset] : BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[offset..]);
                    Expect(actual == (size == 2 ? unchecked((ushort)value) : value), "Independent creature spell native offset/flag differs: " + field);
                    if (all is null) continue;
                    if (mask != 0) all[record.File][offset] = (byte)((all[record.File][offset] & ~mask) | (value == 0 ? 0 : mask));
                    else if (size == 1) all[record.File][offset] = (byte)value;
                    else BinaryPrimitives.WriteUInt16LittleEndian(all[record.File].AsSpan(offset), unchecked((ushort)value));
                }
                if (all is not null) Exact(packed, all);
            }
            if (!doc.IsLegacyProject) Packed(doc, initial);
            var applied = Snapshot(doc); var reopened = NativeMapDocument.Load(doc.SaveCopy(Path.Combine(directory, doc.IsLegacyProject ? "project" : "native")));
            Expect(expected.All(item => Read(reopened).Raw(item.Key) == item.Value), "Creature spell save/reopen lost values.");
            if (doc.IsLegacyProject) Packed(doc.CompileLegacyProject("CreatureSpellCompiled", native), null);
            doc.ApplyCreatureSpellDraft(doc.UpdateCreatureSpellDraft(Read(doc), Inputs(Read(doc)))); Exact(doc, applied);
            Expect(doc.GetHistory().Undo.Count == history + 1, "Creature spell no-op consumed history.");
            doc.Undo(); Exact(doc, initial); doc.Redo(); Exact(doc, applied); doc.Undo(); Exact(doc, initial); baseline = Read(doc);
            foreach (var field in CreatureSpellControls.MainFields)
            {
                if (field == "Shot")
                {
                    foreach (var choice in CreatureSpellControls.Choices(field))
                    { var edit = doc.UpdateCreatureSpellDraft(baseline, Inputs(baseline, (field, choice.Value))); doc.ApplyCreatureSpellDraft(edit); Expect(Read(doc).Raw(field) == choice.Value, "Original shot choice refused."); if (edit.Changed) doc.Undo(); Exact(doc, initial); }
                    Reject(() => doc.UpdateCreatureSpellDraft(baseline, Inputs(baseline, (field, "254")))); continue;
                }
                var maximum = CreatureSpellControls.Boolean(field) ? 1 : doc.IsLegacyProject ? 1024m + 4095m / 4096m : 65535m / 4096m;
                foreach (var value in new[] { "0", Number(maximum) })
                { var edit = doc.UpdateCreatureSpellDraft(baseline, Inputs(baseline, (field, value))); doc.ApplyCreatureSpellDraft(edit); Expect(decimal.Parse(Read(doc).Display(field), CultureInfo.InvariantCulture) == decimal.Parse(value, CultureInfo.InvariantCulture), "Creature spell endpoint differs."); if (edit.Changed) doc.Undo(); Exact(doc, initial); }
                foreach (var value in new[] { "-1", Number(maximum + 1), "1e2", "bad" })
                { Reject(() => doc.UpdateCreatureSpellDraft(baseline, Inputs(baseline, ("Range", "1"), (field, value)))); Exact(doc, initial); }
                if (!CreatureSpellControls.Boolean(field))
                { var format = GlobalDefinitionNumbers.Format("CrSpells", field, !doc.IsLegacyProject); Expect(format.Minimum == 0 && format.Maximum == maximum && format.Fractional, "Creature spell source/native fixed-point limit differs."); }
            }
            CheckNested(doc, initial, baseline, native);
            Reject(() => doc.UpdateCreatureSpellDraft(baseline, Inputs(baseline).Skip(1)));
            Reject(() => doc.UpdateCreatureSpellDraft(baseline, Inputs(baseline).Append(new("ShotData1", "0"))));
            Reject(() => doc.UpdateCreatureSpellDraft(baseline, Inputs(baseline).Append(new("Range", "1"))));
            Reject(() => NativeMapDocument.CreateNewMap(new("Foreign", "Foreign", 12, 12)).ApplyCreatureSpellDraft(baseline));
            Reject(() => doc.CreateCreatureSpellDraft(doc.GlobalDefinitionRows().First(row => row.Family == "Spells")));
            doc.SetGlobalDefinitionValues([new("CrSpells", 0, "ShotData2", baseline.Raw("ShotData2") == "0" ? "1" : "0")]); var stale = Snapshot(doc);
            Reject(() => doc.ApplyCreatureSpellDraft(baseline)); Exact(doc, stale); doc.Undo(); Exact(doc, initial);
            doc.SetGlobalDefinitionValues([new("CrSpells", 0, "Shot", "254")]);
            if (doc.IsLegacyProject)
            {
                doc.SetGlobalDefinitionValues([new("CrSpells", 0, "Range", "10000000")]);
                var record = Row(doc).Record!; doc.SetField(record, record.Fields.Single(field => field.Name == "Attacking"), "2");
            }
            var imported = Snapshot(doc); var importDraft = Read(doc);
            doc.ApplyCreatureSpellDraft(doc.UpdateCreatureSpellDraft(importDraft, Inputs(importDraft))); Exact(doc, imported);
            Reject(() => doc.UpdateCreatureSpellShotDraft(importDraft, "Shot", "1", "1"));
            doc.ApplyCreatureSpellDraft(doc.UpdateCreatureSpellDraft(importDraft, Inputs(importDraft, ("Shot", "0"))));
            Expect(Read(doc).Raw("Range") == importDraft.Raw("Range") && Read(doc).Raw("Attacking") == importDraft.Raw("Attacking"), "Creature spell repair normalized unrelated imports.");
            doc.Undo(); Exact(doc, imported); if (doc.IsLegacyProject) { doc.Undo(); doc.Undo(); } doc.Undo(); Exact(doc, initial);
            using (var canceled = doc.CreateCreatureGlobalsSession())
            { var draft = Read(canceled.WorkingDocument); canceled.WorkingDocument.ApplyCreatureSpellDraft(canceled.WorkingDocument.UpdateCreatureSpellDraft(draft, Inputs(draft, ("Range", "1.25")))); doc.PrepareCreatureGlobals(canceled); Exact(doc, initial); }
            using (var shared = doc.CreateCreatureGlobalsSession())
            {
                var draft = Read(shared.WorkingDocument); var child = shared.WorkingDocument.UpdateCreatureSpellDraft(draft, Inputs(draft, ("Shot", Number(two.ID)), ("Range", "1.25")));
                child = shared.WorkingDocument.UpdateCreatureSpellShotDraft(child, "Shot", "1", "2"); shared.WorkingDocument.ApplyCreatureSpellDraft(child);
                shared.WorkingDocument.SetGlobalDefinitionValues([new("Creatures", 0, "Health", "333")]); var plan = doc.PrepareCreatureGlobals(shared); Exact(doc, initial);
                var count = doc.GetHistory().Undo.Count; doc.ApplyCreatureGlobals(plan); Expect(doc.GetHistory().Undo.Count == count + 1 && Read(doc).Raw("Range") == "5120" && Read(doc).Raw("ShotData1") == "1" && Read(doc).Raw("ShotData2") == "2", "Creature/spell shared private composition lost child or split history."); doc.Undo(); Exact(doc, initial);
            }
            Console.WriteLine($"CREATURE SPELL DETAIL native={!doc.IsLegacyProject} main=9 nested=2 rows=41 shots=66 all-forms targets fixed4096 source-native-limits private-child shared-cancel one-undo compiler literal-bytes reopen stale foreign imports");
        }
        var missing = NativeMapDocument.Create(12, 12, "Missing"); Reject(() => missing.ApplyCreatureSpellDraft(Read(missing)));
    }

    private static void CheckNested(NativeMapDocument doc, Dictionary<NativeFile, byte[]> initial, CreatureSpellDraft baseline, NativeMapDocument compilerBasis)
    {
        Reject(() => doc.UpdateCreatureSpellShotDraft(baseline, "Upgrade", "0", "0"));
        foreach (var option in GlobalProjectiles.Options)
        {
            var parent = doc.UpdateCreatureSpellDraft(baseline, Inputs(baseline, ("Shot", Number(option.ID))));
            Expect(parent.Raw("ShotData1") == baseline.Raw("ShotData1") && parent.Raw("ShotData2") == baseline.Raw("ShotData2"), "Changing projectile modified unsubmitted child arguments.");
            if (option.Form == 255) { Reject(() => doc.UpdateCreatureSpellShotDraft(parent, "Shot", "0", "0")); continue; }
            if (option.Form is 0 or 1)
            {
                var submitted = doc.UpdateCreatureSpellShotDraft(parent, "Shot", option.Signed1 ? "-32768" : "0", option.Signed2 ? "-32768" : "0");
                Expect(submitted.Display("ShotData1") == (option.Signed1 ? "-32768" : "0") && (option.Form == 0 || submitted.Raw("ShotData2") == baseline.Raw("ShotData2")), "Signed child/form1 second retention differs.");
                Reject(() => doc.UpdateCreatureSpellShotDraft(parent, "Shot", "32768", "0"));
                if (option.Form == 0) Reject(() => doc.UpdateCreatureSpellShotDraft(parent, "Shot", "0", "32768"));
            }
            else
            {
                foreach (var target in GlobalProjectiles.Targets(option))
                {
                    var submitted = doc.UpdateCreatureSpellShotDraft(parent, "Shot", target.Value, option.Form == 3 ? "10" : option.Form == 2 ? "100" : "65535");
                    Expect(submitted.Raw("ShotData1") == target.Value && (option.Form != 4 || submitted.Raw("ShotData2") == (target.Value == "1" ? "65535" : "0")), "Creature spell target/source object zero differs.");
                }
                Reject(() => doc.UpdateCreatureSpellShotDraft(parent, "Shot", "65534", "1"));
                if (option.Form is 2 or 3) Reject(() => doc.UpdateCreatureSpellShotDraft(parent, "Shot", GlobalProjectiles.Targets(option)[0].Value, option.Form == 2 ? "101" : "11"));
                if (option.Form == 4)
                {
                    if (doc.IsLegacyProject) Expect(doc.UpdateCreatureSpellShotDraft(parent, "Shot", "1", "80000").Raw("ShotData2") == "80000", "Creature spell project gold maximum refused.");
                    else Reject(() => doc.UpdateCreatureSpellShotDraft(parent, "Shot", "1", "80000"));
                    Reject(() => doc.UpdateCreatureSpellShotDraft(parent, "Shot", "1", "80001"));
                }
            }
            Exact(doc, initial);
        }
        var objectType = GlobalProjectiles.Options.First(option => option.Form == 4); var objectID = GlobalProjectiles.Targets(objectType).First(choice => choice.Value != "1").Value;
        doc.SetGlobalDefinitionValues([new("CrSpells", 0, "Shot", Number(objectType.ID)), new("CrSpells", 0, "ShotData1", objectID), new("CrSpells", 0, "ShotData2", "1234")]);
        var imported = Snapshot(doc); var draft = Read(doc); doc.ApplyCreatureSpellDraft(doc.UpdateCreatureSpellDraft(draft, Inputs(draft))); Exact(doc, imported);
        var childOK = doc.UpdateCreatureSpellShotDraft(draft, "Shot", objectID, "ignored disabled gold"); Exact(doc, imported);
        Expect(childOK.Raw("ShotData2") == "0", "Explicit child non-gold Save retained argument."); doc.ApplyCreatureSpellDraft(childOK); doc.Undo(); Exact(doc, imported); doc.Undo(); Exact(doc, initial);
        var signedType = GlobalProjectiles.Options.First(option => option.Form is 0 or 1 && option.Signed1);
        doc.SetGlobalDefinitionValues([new("CrSpells", 0, "Shot", Number(signedType.ID)), new("CrSpells", 0, "ShotData1", "65527")]);
        var signedImport = Snapshot(doc); var signedDraft = Read(doc); Expect(signedDraft.Display("ShotData1") == "-9", "Creature spell imported signed WORD display differs.");
        var signedNoop = doc.UpdateCreatureSpellShotDraft(signedDraft, "Shot", "-9", signedDraft.Display("ShotData2"));
        Expect(signedNoop.Raw("ShotData1") == "65527", "Unchanged signed creature spell normalized WORD alias."); doc.ApplyCreatureSpellDraft(signedNoop); Exact(doc, signedImport); doc.Undo(); Exact(doc, initial);
        if (doc.IsLegacyProject)
            foreach (var option in GlobalProjectiles.Options.Where(option => option.Form is 1 or 255))
            {
                doc.SetGlobalDefinitionValues([new("CrSpells", 0, "Shot", Number(option.ID)), new("CrSpells", 0, "ShotData1", "123"), new("CrSpells", 0, "ShotData2", "456")]);
                var retained = Snapshot(doc); var parent = Read(doc); if (option.Form == 1) parent = doc.UpdateCreatureSpellShotDraft(parent, "Shot", "123", "ignored second");
                doc.ApplyCreatureSpellDraft(parent); Exact(doc, retained); var compiled = doc.CompileLegacyProject("CreatureSpellInactive", compilerBasis);
                Expect(Read(compiled).Raw("ShotData2") == "456" && Read(doc).Raw("ShotData2") == "456", "Creature spell compiler incorrectly cleared retained second argument."); doc.Undo(); Exact(doc, initial);
            }
    }
}
