using System.Globalization;
using DK2MapEditor.Core.Editing;

internal static class GlobalProjectileChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Projectile changed unrelated bytes/undo: " + file.ExportPath); }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException) { return; }
        throw new Exception("Invalid projectile edit accepted.");
    }
    private static GlobalProjectileState State(NativeMapDocument document, string family, string slot) => document.GlobalProjectile(document.GlobalDefinitionRows().Single(row => row.Family == family && row.Index == 0), slot);
    public static void Run()
    {
        var forms = "FF,01,02,00,01,04,00,01,01,00,01,01,01,00,00,00,00,00,00,00,01,01,01,00,00,00,01,00,01,00,01,01,FF,01,00,01,01,00,00,00,00,00,00,01,01,00,01,01,01,01,00,00,01,01,00,00,01,01,00,01,03,00,00,01,00,00".Split(',').Select(text => int.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
        var label1 = "0,1,0,1,2,0,3,4,4,2,4,1,1,0,1,1,1,1,1,1,2,1,4,1,1,0,1,1,4,0,1,4,0,6,1,1,1,1,5,0,0,0,0,4,1,4,4,1,1,1,0,1,1,3,0,1,1,1,1,1,0,0,1,1,1,1".Split(',').Select(int.Parse).ToArray();
        var label2 = "0,0,0,1,0,0,2,0,0,2,0,0,0,0,1,1,1,1,1,1,0,0,0,1,1,0,0,1,0,0,0,0,0,0,1,0,0,1,2,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,1,0,0,1,0,0,0,2,0,1,1".Split(',').Select(int.Parse).ToArray();
        Expect(GlobalProjectiles.Options.Count == 66, "Missing source types.");
        foreach (var option in GlobalProjectiles.Options)
        {
            Expect(option.Form == forms[option.ID] && option.Label1 == label1[option.ID] && option.Label2 == label2[option.ID] && option.Signed1 == (option.ID is 4 or 9 or 20) && !option.Signed2, "Wrong original parameter metadata: " + option.ID);
            Expect(option.Name.English.Length > 0 && option.Name.Russian.Length > 0 && !option.Name.English.Contains("sz"), "Missing named projectile translation.");
        }
        var directory = Path.Combine(Path.GetTempPath(), "DK2-Projectiles-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory, "basis")); var basis = Snapshot(native);
            var project = NativeMapDocument.CreateNewMap(new("Projectiles", "Projectiles", 20, 20));
            ProjectileEditorAcceptanceChecks.Run(native, project);
            foreach (var document in new[] { native, project })
            {
                var initial = Snapshot(document);
                foreach (var (family, slot) in new[] { ("Traps", "Shot"), ("Spells", "Shot"), ("Spells", "Upgrade"), ("CrSpells", "Shot") })
                {
                    foreach (var option in GlobalProjectiles.Options)
                    {
                        var old = State(document, family, slot); var before = Snapshot(document); var changes = 0;
                        EventHandler handler = (_, _) => changes++; document.Changed += handler;
                        try
                        {
                            var first = option.Form switch { 2 or 3 => "7", 4 => "1", _ => option.Signed1 ? "-123" : "123" };
                            var second = option.Form switch { 2 => "25", 3 => "8", 4 => "500", _ => "45" };
                            document.SetGlobalProjectile(family, 0, slot, option.ID, first, second);
                            var state = State(document, family, slot);
                            var expectedFirst = option.Form == 255 ? old.Data1 : option.Signed1 && !document.IsLegacyProject ? "65413" : first;
                            var expectedSecond = option.Form is 1 or 255 ? old.Data2 : second;
                            Expect(state.Type == option.ID.ToString(CultureInfo.InvariantCulture) && state.Data1 == expectedFirst && state.Data2 == expectedSecond, "Wrong compound encoding/preservation: " + family + "/" + slot + "/" + option.ID);
                            var edited = Snapshot(document); var actualChange = before.Any(pair => !pair.Value.AsSpan().SequenceEqual(pair.Key.Data.Span));
                            Expect(changes == (actualChange ? 1 : 0), "Compound edit not one transaction.");
                            document.SetGlobalProjectile(family, 0, slot, option.ID, GlobalProjectiles.DisplayParameter(option, state.Data1, false), GlobalProjectiles.DisplayParameter(option, state.Data2, true));
                            Exact(document, edited); Expect(changes == (actualChange ? 1 : 0), "Unchanged compound edit created history.");
                            if (actualChange) { document.Undo(); Exact(document, before); document.Redo(); Exact(document, edited); document.Undo(); Exact(document, before); }
                        }
                        finally { document.Changed -= handler; }
                    }
                    Exact(document, initial);
                    foreach (var (id, low, high) in new[] { (4u, "-32768", "32767"), (9u, "-32768", "32767"), (20u, "-32768", "32767"), (3u, "0", "32767") })
                        foreach (var endpoint in new[] { low, high })
                        {
                            document.SetGlobalProjectile(family, 0, slot, id, endpoint, "32767");
                            var state = State(document, family, slot); var expected = endpoint == "-32768" && !document.IsLegacyProject ? "32768" : endpoint;
                            Expect(state.Data1 == expected && (id is 4 or 20 || state.Data2 == "32767"), "Source parameter endpoint failed."); document.Undo(); Exact(document, initial);
                        }
                    document.SetGlobalProjectile(family, 0, slot, 5, "1", document.IsLegacyProject ? "80000" : "65535");
                    var gold = Snapshot(document);
                    Reject(() => document.SetGlobalProjectile(family, 0, slot, 5, "1", document.IsLegacyProject ? "80001" : "65536")); Exact(document, gold);
                    document.SetGlobalProjectile(family, 0, slot, 5, "4", "ignored"); Expect(State(document, family, slot).Data2 == "0", "Non-gold object did not clear irrelevant gold on actual change.");
                    document.Undo(); Exact(document, gold); document.Undo(); Exact(document, initial);
                    foreach (var (type, first, second) in new[] { (4u, "-32769", "0"), (4u, "32768", "0"), (1u, "-1", "ignored"), (3u, "1", "-1"), (2u, "7", "101"), (2u, "999", "1"), (60u, "7", "0"), (60u, "7", "11"), (5u, "999", "1"), (255u, "0", "0"), (1u, "1.5", "0"), (1u, "0x10", "0") })
                    { Reject(() => document.SetGlobalProjectile(family, 0, slot, type, first, second)); Exact(document, initial); }
                    Reject(() => document.SetGlobalProjectile(family, 0, "Unknown", 0, "0", "0")); Exact(document, initial);
                    // WORD aliases / inactive data are preserved on unchanged input.
                    var prefix = slot == "Upgrade" ? "Upg" : ""; var typeName = family == "CrSpells" ? "Shot" : prefix + "ShotType";
                    document.SetGlobalDefinitionValues([new(family, 0, typeName, "4"), new(family, 0, prefix + "ShotData1", "65527"), new(family, 0, prefix + "ShotData2", "23456")]);
                    var aliases = Snapshot(document); document.SetGlobalProjectile(family, 0, slot, 4, "-9", "ignored"); Exact(document, aliases); document.Undo(); Exact(document, initial);
                    var imported = document.IsLegacyProject ? "-1" : "65535";
                    document.SetGlobalDefinitionValues([new(family, 0, typeName, "2"), new(family, 0, prefix + "ShotData1", imported), new(family, 0, prefix + "ShotData2", "999")]);
                    var unknownTarget = Snapshot(document); document.SetGlobalProjectile(family, 0, slot, 2, imported, "999"); Exact(document, unknownTarget);
                    document.SetGlobalProjectile(family, 0, slot, 2, "7", "999"); Expect(State(document, family, slot).Data1 == "7" && State(document, family, slot).Data2 == "999", "Explicit target repair normalized imported count."); document.Undo(); Exact(document, unknownTarget); document.Undo(); Exact(document, initial);
                    document.SetGlobalDefinitionValues([new(family, 0, typeName, "5"), new(family, 0, prefix + "ShotData1", "4"), new(family, 0, prefix + "ShotData2", "999")]);
                    var inactive = Snapshot(document); document.SetGlobalProjectile(family, 0, slot, 5, "4", "ignored"); Exact(document, inactive); document.Undo(); Exact(document, initial);
                }
                // Save all four independent slots; source compiler only clears
                // inactive data2 in traps/keeper spells, not creature spells.
                document.SetGlobalDefinitionValues([new("Traps", 0, "ShotType", "1"), new("Traps", 0, "ShotData1", "123"), new("Traps", 0, "ShotData2", "456"), new("Spells", 0, "ShotType", "0"), new("Spells", 0, "ShotData1", "789"), new("Spells", 0, "ShotData2", "456"), new("Spells", 0, "UpgShotType", "4"), new("Spells", 0, "UpgShotData1", "65527"), new("Spells", 0, "UpgShotData2", "456"), new("CrSpells", 0, "Shot", "1"), new("CrSpells", 0, "ShotData1", "123"), new("CrSpells", 0, "ShotData2", "456")]);
                var written = Snapshot(document); var saved = document.SaveCopy(Path.Combine(directory, document.IsLegacyProject ? "project" : "native")); var reopened = NativeMapDocument.Load(saved);
                foreach (var (family, slot) in new[] { ("Traps", "Shot"), ("Spells", "Shot"), ("Spells", "Upgrade"), ("CrSpells", "Shot") }) Expect(State(reopened, family, slot) == State(document, family, slot), "Compound values changed after save/reopen.");
                if (document.IsLegacyProject)
                {
                    var compiled = document.CompileLegacyProject("ProjectileCompiled", native);
                    Expect(State(compiled, "Traps", "Shot").Data2 == "0" && State(compiled, "Spells", "Shot").Data2 == "0" && State(compiled, "Spells", "Upgrade").Data2 == "0" && State(compiled, "CrSpells", "Shot").Data2 == "456", "Original inactive compiler arguments differ.");
                    Expect(State(compiled, "Traps", "Shot").Data1 == "123" && State(compiled, "Spells", "Shot").Data1 == "789" && State(compiled, "Spells", "Upgrade").Data1 == "65527", "Compiler cleared first/active argument."); Exact(document, written);
                }
                document.Undo(); Exact(document, initial);
            }
            Exact(native, basis);
            var missing = NativeMapDocument.Create(20, 20, "Missing"); var missingBytes = Snapshot(missing);
            Reject(() => missing.SetGlobalProjectile("Traps", 0, "Shot", 1, "123", "0")); Exact(missing, missingBytes);
        }
        finally { Directory.Delete(directory, true); }
    }
}
