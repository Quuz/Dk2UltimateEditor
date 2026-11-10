using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Editing;

internal static class GlobalDefinitionNumericChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> snapshot)
    { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]), "Numeric edit changed unrelated bytes/undo: " + file.ExportPath); }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException) { return; }
        throw new Exception("Invalid numeric input accepted.");
    }
    // Independent list from the original controls, not the production parser.
    private static readonly Dictionary<string, string[]> Fractional = new()
    {
        ["Terrains"] = ["TerrainLightH"], ["Rooms"] = ["TorchLightH", "TorchLightRad"],
        ["Traps"] = ["IdleEffectDelay", "ChargeTime", "RechargeTime", "ThreatDuration"], ["Spells"] = ["RechargeTime"],
        ["Creatures"] = ["Height", "EyeHeight", "DistanceCanSee", "DistanceCanHear", "Speed", "RunSpeed", "HungerRate", "StunDuration", "GuardDuration", "IdleDuration", "SlapDuration", "Shot1Delay", "Shot1X", "Shot1Y", "Shot1Z", "Shot2Delay", "Shot2X", "Shot2Y", "Shot2Z", "Shot3Delay", "Shot3X", "Shot3Y", "Shot3Z", "RechargeTime", "Range", "Delay", "WaddleScale", "OscillateScale", "LightX", "LightY", "LightZ", "LightRadius"],
        ["CrSpells"] = ["RechargeTime", "Range"]
    };
    private static readonly string[] Signed = ["HealthFromChicken", "SlapDamage", "TortureHealthCng", "OwnLandHealthInc", "PrayerMana", "PossessionManaCost", "AngerNoLair", "AngerNoFood", "AngerNoPay", "AngerNoWork", "AngerSlap", "AngerInHand", "TortureMoodCng"];
    private static string Value(NativeMapDocument document, string family, string name, int index = 0) => document.GlobalDefinitionValues(document.GlobalDefinitionRows().Single(row => row.Family == family && row.Index == index)).Single(field => field.Name == name).Value;
    internal static NativeMapDocument Fixture(string directory)
    {
        var path = NativeMapDocument.Create(20, 20, "Numbers").SaveCopy(directory);
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "DK2MapEditor.slnx"))) root = root.Parent;
        foreach (var family in LegacyGlobalSchemas.All)
        {
            var name = family.Group switch { "Terrains" => "Terrain", "Spells" => "KeeperSpells", "CrSpells" => "CreatureSpells", _ => family.Group };
            var defaults = File.ReadAllBytes(Path.Combine(root!.FullName, "Binaries", "Globals", "Default" + name + ".dkg"));
            var bytes = new byte[56 + defaults.Length];
            void Header(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
            Header(0, family.Signature); Header(4, 4); Header(8, (uint)bytes.Length); Header(12, family.Signature + 1); Header(16, 28); Header(20, (uint)(defaults.Length / family.RecordSize)); Header(48, family.Signature + 2); Header(52, (uint)defaults.Length);
            defaults.CopyTo(bytes, 56); File.WriteAllBytes(Path.Combine(directory, "Data", "Editor", name + ".kwd"), bytes);
        }
        return NativeMapDocument.Load(path);
    }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2-Numbers-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var native = Fixture(Path.Combine(directory, "basis")); var basis = Snapshot(native);
            var project = NativeMapDocument.CreateNewMap(new("Numbers", "Numbers", 20, 20));
            foreach (var family in LegacyGlobalSchemas.All)
                foreach (var field in family.Fields)
                {
                    var format = GlobalDefinitionNumbers.Format(family.Group, field.Name);
                    Expect(format.Fractional == (Fractional.TryGetValue(family.Group, out var names) && names.Contains(field.Name)), "Wrong source fractional field: " + family.Group + "." + field.Name);
                    Expect(format.SignedAttribute == (family.Group == "Creatures" && Signed.Contains(field.Name)), "Wrong signed field: " + field.Name);
                }
            Expect(Fractional.Values.Sum(names => names.Length) == 42, "Incomplete test catalogue.");
            // Every possible 12-bit fraction has an exact, re-enterable string.
            for (var fraction = 0; fraction < 4096; fraction++)
            {
                var shown = GlobalDefinitionNumbers.Display("Creatures", "Speed", (4096 + fraction).ToString(CultureInfo.InvariantCulture));
                Expect(decimal.Parse(shown, CultureInfo.InvariantCulture) * 4096m == 4096 + fraction, "Display lost a fractional bit.");
            }
            foreach (var document in new[] { native, project })
            {
                var initial = Snapshot(document); var notifications = 0; document.Changed += (_, _) => notifications++;
                var all = document.GlobalDefinitionRows().GroupBy(row => row.Family).Select(group => group.First()).SelectMany(row => document.GlobalDefinitionValues(row).Select(field => new GlobalDefinitionChange(row.Family, row.Index, field.Name, GlobalDefinitionNumbers.Display(row.Family, field.Name, field.Value)))).ToArray();
                document.SetGlobalDefinitionDisplayValues(all); Exact(document, initial); Expect(notifications == 0, "Re-entering defaults materialized a project override/history.");
                var edits = Fractional.SelectMany(pair => pair.Value.Select(name => new GlobalDefinitionChange(pair.Key, 0, name, "1,5"))).Concat(Signed.Select(name => new GlobalDefinitionChange("Creatures", 0, name, "-9"))).ToArray();
                var previous = edits.ToDictionary(edit => (edit.Family, edit.Field), edit => Value(document, edit.Family, edit.Field));
                document.SetGlobalDefinitionDisplayValues(edits); Expect(notifications == 1, "Numeric batch was not one transaction.");
                foreach (var edit in edits)
                {
                    var raw = Value(document, edit.Family, edit.Field);
                    var expected = edit.Value == "1,5" ? "6144" : document.IsLegacyProject ? "-9" : "65527";
                    if (edit.Value == "-9" && GlobalDefinitionNumbers.Display(edit.Family, edit.Field, previous[(edit.Family, edit.Field)]) == "-9") expected = previous[(edit.Family, edit.Field)];
                    Expect(raw == expected, "Wrong encoded source units: " + edit.Field + "=" + raw);
                    var row = document.GlobalDefinitionRows().Single(row => row.Family == edit.Family && row.Index == 0);
                    if (row.Record is null) { Expect(document.IsLegacyProject, "Missing native edited record."); continue; }
                    var field = row.Record.Fields.Single(field => field.Name == edit.Field);
                    var span = row.Record.File.Data.Span.Slice(row.Record.Offset + field.Offset, field.Size);
                    Expect(field.Size == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(span) == (edit.Value == "1,5" ? 6144 : 65527) : BinaryPrimitives.ReadInt32LittleEndian(span) == int.Parse(expected, CultureInfo.InvariantCulture), "Physical encoding differs from independent expectation.");
                }
                var edited = Snapshot(document);
                document.SetGlobalDefinitionDisplayValues(edits); Exact(document, edited); Expect(notifications == 1, "Re-entering edited values created history.");
                foreach (var invalid in new[] { "invalid", "1,2.3", "1e2", "1 000", "1.000.000", "-0.1", "32768", "999999999999999999999999999999999999" })
                { Reject(() => document.SetGlobalDefinitionDisplayValues([new("Creatures", 0, "Speed", invalid)])); Exact(document, edited); }
                Reject(() => document.SetGlobalDefinitionDisplayValues([new("Terrains", 0, "GoldValue", "123"), new("Creatures", 0, "Speed", "-1")])); Exact(document, edited);
                Reject(() => document.SetGlobalDefinitionDisplayValues([new("Creatures", 0, "Health", "1.5")]));
                Reject(() => document.SetGlobalDefinitionDisplayValues([new("Creatures", 0, "Res1Rate", "101")]));
                Reject(() => document.SetGlobalDefinitionDisplayValues([new("Creatures", 0, "Speed", "1"), new("Creatures", 0, "Speed", "2")])); Exact(document, edited);
                document.Undo(); Exact(document, initial); document.Redo(); Exact(document, edited);
                foreach (var upper in new[] { false, true })
                {
                    var endpoints = Fractional.SelectMany(pair => pair.Value.Select(name =>
                    {
                        var format = GlobalDefinitionNumbers.Format(pair.Key, name, !document.IsLegacyProject);
                        return new GlobalDefinitionChange(pair.Key, 0, name, (upper ? format.Maximum!.Value : format.Minimum!.Value).ToString(CultureInfo.InvariantCulture));
                    })).ToArray();
                    document.SetGlobalDefinitionDisplayValues(endpoints);
                    foreach (var endpoint in endpoints) Expect(GlobalDefinitionNumbers.Display(endpoint.Family, endpoint.Field, Value(document, endpoint.Family, endpoint.Field)) == decimal.Parse(endpoint.Value, CultureInfo.InvariantCulture).ToString("0.0###########", CultureInfo.InvariantCulture), "Source/packed fractional endpoint failed: " + endpoint.Field);
                    document.Undo(); Exact(document, edited);
                }
                document.SetGlobalDefinitionDisplayValues([new("Creatures", 0, "Speed", "0.000244140625"), new("Creatures", 0, "Shot1X", "-0.1")]);
                Expect(Value(document, "Creatures", "Speed") == "1" && Value(document, "Creatures", "Shot1X") == "-409", "Exact LSB or signed truncation failed.");
                document.Undo(); Exact(document, edited);
                if (!document.IsLegacyProject)
                {
                    Reject(() => document.SetGlobalDefinitionDisplayValues([new("Creatures", 0, "Speed", "16")]));
                    Reject(() => document.SetGlobalDefinitionDisplayValues([new("CrSpells", 0, "Range", "16")]));
                    Reject(() => document.SetGlobalDefinitionDisplayValues([new("Creatures", 0, "AngerNoFood", "-32769")])); Exact(document, edited);
                }
                else
                {
                    document.SetGlobalDefinitionDisplayValues([new("Creatures", 0, "Speed", "32767.999999"), new("Creatures", 0, "AngerNoFood", "-32769")]);
                    Expect(Value(document, "Creatures", "Speed") == "134217727" && Value(document, "Creatures", "AngerNoFood") == "-32769", "Project lost original wider input range."); document.Undo(); Exact(document, edited);
                }
                // Raw imports outside source limits must survive viewing/reapply.
                document.SetGlobalDefinitionValues([new("Creatures", 0, "HungerRate", "-1"), new("Creatures", 0, "Health", "65535"), new("Creatures", 0, "TortureHealthCng", "65527")]);
                var rawSnapshot = Snapshot(document); var before = notifications;
                document.SetGlobalDefinitionDisplayValues([new("Creatures", 0, "HungerRate", "1048575.999755859375"), new("Creatures", 0, "Health", "65535"), new("Creatures", 0, "TortureHealthCng", "-9")]);
                Exact(document, rawSnapshot); Expect(before == notifications, "Imported out-of-range bits/signed alias were normalized."); document.Undo(); Exact(document, edited);
                var saved = document.SaveCopy(Path.Combine(directory, document.IsLegacyProject ? "project" : "native")); var reopened = NativeMapDocument.Load(saved);
                foreach (var edit in edits) Expect(Value(reopened, edit.Family, edit.Field) == Value(document, edit.Family, edit.Field), "Numeric values changed after save/reopen.");
                if (document.IsLegacyProject)
                {
                    var compiled = document.CompileLegacyProject("NumbersCompiled", native);
                    foreach (var edit in edits) Expect(Value(compiled, edit.Family, edit.Field) == (edit.Value == "1,5" ? "6144" : "65527"), "Numeric values compiled incorrectly.");
                    Exact(document, edited);
                }
                document.Undo(); Exact(document, initial);
            }
            Exact(native, basis);
            Expect(GlobalDefinitionNumbers.Format("Creatures", "Speed", true).Maximum == 65535m / 4096m && GlobalDefinitionNumbers.Format("Creatures", "AngerNoFood", true).Minimum == -32768, "Native UI limit ignored packed width.");
            var missing = NativeMapDocument.Create(20, 20, "Missing"); var missingBytes = Snapshot(missing);
            Reject(() => missing.SetGlobalDefinitionDisplayValues([new("Creatures", 0, "Speed", "1")])); Exact(missing, missingBytes);
        }
        finally { Directory.Delete(directory, true); }
    }
}
