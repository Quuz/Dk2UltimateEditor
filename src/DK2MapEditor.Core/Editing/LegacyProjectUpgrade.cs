using System.Buffers.Binary;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private static int CurrentProjectVersion(uint signature) => signature switch
    {
        LegacyInfo => 7, LegacyPlayers => 3, LegacyThings => 6,
        LegacyBands => 5, LegacyEvents => 3, LegacyAreas or LegacyGlobals => 1, _ => 0
    };

    public bool HasLegacyUpgrade => IsLegacyProject && projectSections.Any(file => CurrentProjectVersion(file.Signature) > file.UInt32(4));

    /// <summary>Explicit historical-reader conversion, reversible as one edit.
    /// Current and unknown non-editor sections are left byte-for-byte untouched.</summary>
    public void UpgradeLegacyProject() => Edit("Upgrade historical project", () =>
    {
        if (!IsLegacyProject) throw new InvalidOperationException("Only historical editor projects can be upgraded.");
        _ = Records(); // Validate every recognized layout before converting any buffer.
        if (projectSections.Where(file => CurrentProjectVersion(file.Signature) != 0).GroupBy(file => file.Signature).Any(group => group.Count() != 1))
            throw new InvalidDataException("Duplicate editor sections make historical conversion ambiguous.");
        foreach (var file in projectSections)
        {
            var current = CurrentProjectVersion(file.Signature); var version = file.UInt32(4);
            if (current == 0 || version == current) continue;
            if (version > current)
                throw new InvalidDataException("This project section has no verified upgrade path: 0x" + file.Signature.ToString("X") + " version " + version + ".");
        }
        foreach (var file in projectSections)
        {
            var version = (int)file.UInt32(4); var current = CurrentProjectVersion(file.Signature);
            if (current == 0 || version == current) continue;
            file.Bytes = file.Signature switch
            {
                LegacyInfo => UpgradeMetadata(file, version),
                LegacyPlayers => UpgradePlayers(file, version),
                LegacyThings => UpgradeThings(file, version),
                LegacyBands => UpgradeBands(file, version),
                LegacyEvents => version == 1 ? UpgradeNestedEvents(file) : UpgradeEventLinks(file),
                _ => throw new InvalidDataException("Unverified project upgrade.")
            };
        }
        _ = Records();
        if (files.Sum(file => (long)file.Length) > 128L * 1024 * 1024 || projectSections.Sum(file => (long)file.Length) + 12 > MaximumFileBytes)
            throw new InvalidDataException("Upgraded project exceeds its safe size limit.");
    });

    private sealed class UpgradeBuffer(string structure)
    {
        public byte[] Bytes { get; } = new byte[LegacyProjectSchemas.Size(structure)];
        private readonly IReadOnlyDictionary<string, NativeField> fields = LegacyProjectSchemas.Fields(structure).ToDictionary(field => field.Name);
        public void Set(string name, object value)
        {
            var field = fields[name];
            field.Encode(Convert.ToString(value, CultureInfo.InvariantCulture)!).CopyTo(Bytes, field.Offset);
        }
        public void SetCast(string name, string value)
        {
            var field = fields[name]; EncodeCompilerValue(field, value).CopyTo(Bytes, field.Offset);
        }
        public void SetPrevious(int id) => BinaryPrimitives.WriteUInt16LittleEndian(Bytes.AsSpan(LegacyProjectSchemas.Size("NODE")), checked((ushort)id));
        public void CopyFields(ReadOnlySpan<byte> source, string sourceStructure, Func<string, string?>? rename = null)
        {
            foreach (var sourceField in LegacyProjectSchemas.Fields(sourceStructure))
            {
                var name = rename is null ? sourceField.Name : rename(sourceField.Name);
                if (name is null || !fields.TryGetValue(name, out var destination)) continue;
                if (sourceField.Size != destination.Size || sourceField.Kind != destination.Kind)
                    throw new InvalidDataException("Unverified historical field conversion: " + sourceField.Name);
                source.Slice(sourceField.Offset, sourceField.Size).CopyTo(Bytes.AsSpan(destination.Offset));
            }
        }
    }

    private static byte[] UpgradeSection(NativeFile file, int version, IEnumerable<byte[]> records)
    {
        var payload = records.ToArray(); var size = 12L + payload.Sum(record => (long)record.Length);
        if (size > MaximumFileBytes) throw new InvalidDataException("Upgraded section exceeds its safe size limit.");
        var bytes = new byte[(int)size]; WriteUInt32(bytes, 0, file.Signature); WriteUInt32(bytes, 4, (uint)version); WriteUInt32(bytes, 8, (uint)size - 12);
        var offset = 12;
        foreach (var record in payload) { record.CopyTo(bytes, offset); offset += record.Length; }
        return bytes;
    }

    private static byte[] UpgradeMetadata(NativeFile file, int version)
    {
        var target = new UpgradeBuffer("MAPPROPERTIES");
        foreach (var element in LegacyCompilerRules.Elements("MapVars").Where(element => element.ID < 256))
            target.Set($"Variables[{element.ID}]", unchecked((int)element.Variable));
        target.Set("Version", 17); target.Set("Options", 4); target.Set("GameTicks", 4); target.Set("Soundtrack", 1);
        // The old reader copies the whole metadata prefix, including padding
        // that acquired a meaning in later versions, then applies these fixes.
        file.Data.Span[12..].CopyTo(target.Bytes);
        if (version <= 5) target.Set("Version", 17);
        if (version <= 2) target.Set("Options", 4);
        if (version <= 4) { target.Set("GameTicks", 4); target.Set("Soundtrack", 1); }
        if (version == 1) target.Set("SpeechFile", "");
        if (version >= 4)
        {
            target.Set("Variables[155]", 0x2800); target.Set("Variables[158]", 0x1333);
            target.Set("Variables[221]", 0x1800); target.Set("Variables[220]", 0x1800);
        }
        return UpgradeSection(file, 7, [target.Bytes]);
    }

    private static byte[] UpgradePlayers(NativeFile file, int version)
    {
        var sourceStructure = version == 1 ? "MAPOPTIONSV1" : "MAPOPTIONSV2";
        var stride = LegacyProjectSchemas.Size(sourceStructure); var result = new List<byte[]>();
        for (var index = 0; index < 7; index++)
        {
            var target = new UpgradeBuffer("MAPOPTIONS");
            foreach (var rule in LegacyCompilerRules.DefaultPlayerAI) target.Set(rule.Target, rule.Source);
            var source = file.Data.Span.Slice(12 + index * stride, stride);
            if (version == 2) source.CopyTo(target.Bytes);
            else target.CopyFields(source, sourceStructure, name => name switch
            {
                "Health" => "PlayerHealth", "Gold" => "PlayerGold", "ComputerPlayer" => "PlayerComputer",
                "CPUAreaX1" => "PlayerCPUAreaX1", "CPUAreaY1" => "PlayerCPUAreaY1", "CPUAreaX2" => "PlayerCPUAreaX2", "CPUAreaY2" => "PlayerCPUAreaY2",
                "HeartX" or "HeartY" or "AI" => null, _ => name
            });
            target.Set("PlayerCameraX", -1); target.Set("PlayerCameraY", -1);
            result.Add(target.Bytes);
        }
        return UpgradeSection(file, 3, result);
    }

    private static byte[] UpgradeThings(NativeFile file, int version)
    {
        var structure = version switch { 1 => "MAPTHINGV1", 2 or 3 => "MAPTHINGV2", 4 => "MAPTHINGV4", _ => "MAPTHINGV5" };
        var stride = LegacyProjectSchemas.Size(structure); var result = new List<byte[]>();
        for (var offset = 12; offset < file.Length; offset += stride)
        {
            result.Add(UpgradeHistoricalThing(file.Data.Span.Slice(offset, stride),version));
        }
        return UpgradeSection(file, 6, result);
    }

    // Shared original Editor_ExtractThings recipe. A selection projection uses
    // a private result; it never upgrades or edits the historical source document.
    internal static byte[] UpgradeHistoricalThing(ReadOnlySpan<byte> source,int version)
    {
        if(version is <1 or >5)throw new ArgumentOutOfRangeException(nameof(version));
        var structure=version switch {1=>"MAPTHINGV1",2 or 3=>"MAPTHINGV2",4=>"MAPTHINGV4",_=>"MAPTHINGV5"};
        if(source.Length!=LegacyProjectSchemas.Size(structure))throw new InvalidDataException("Invalid historical selection thing size.");
        var fields=LegacyProjectSchemas.Fields(structure);var target=new UpgradeBuffer("MAPTHING");
        target.CopyFields(source,structure,name=>name switch {"options"=>"herosoptions","objective" when version<=3=>null,_=>name});
        var type=fields.Single(field=>field.Name=="node.type").Read(source);
        if(version<=3&&type=="0")
        {
            target.Set("creaturesoptions",9);
            if(fields.Single(field=>field.Name=="owner").Read(source)=="1") {target.Set("objective",19);target.Set("ennemy",0);}
        }
        if(version<=4&&type=="2")target.Set("dataAll",4);
        return target.Bytes;
    }

    private static byte[] UpgradeBands(NativeFile file, int version)
    {
        var structure = "MTHBANDV" + version; var stride = LegacyProjectSchemas.Size(structure);
        var sentinel = LegacyProjectSchemas.Fields(structure).Single(field => field.Name == "heros[16].isvalid");
        var result = new List<byte[]>();
        for (var offset = 12; offset < file.Length; offset += stride)
        {
            var source = file.Data.Span.Slice(offset, stride);
            if (sentinel.Read(source) != "0") throw new InvalidDataException("The historical seventeenth band slot is active; repair it before upgrade.");
            var target = new UpgradeBuffer("MTHBAND");
            target.CopyFields(source, structure, name => name == "node.type" || name.StartsWith("heros[16].", StringComparison.Ordinal)
                || version <= 2 && name.EndsWith(".objective", StringComparison.Ordinal)
                || version <= 3 && name.EndsWith(".options2", StringComparison.Ordinal) ? null : name);
            for (var index = 0; index < 16; index++)
            {
                if (version <= 2) target.Set($"heros[{index}].objective", 19);
                if (version <= 3) target.Set($"heros[{index}].options2", index == 0 ? 43 : 45);
            }
            result.Add(target.Bytes);
        }
        return UpgradeSection(file, 5, result);
    }

    private static byte[] UpgradeEventLinks(NativeFile file)
    {
        var count = checked((int)file.UInt32(12)); var stride = LegacyProjectSchemas.Size("TRIGGERLINKV2");
        var records = new List<byte[]> { file.Data.Span.Slice(12, 12).ToArray() };
        for (var index = 0; index < count; index++)
        {
            var target = new UpgradeBuffer("TRIGGERLINK");
            target.CopyFields(file.Data.Span.Slice(24 + index * stride, stride), "TRIGGERLINKV2");
            records.Add(target.Bytes);
        }
        records.Add(file.Data.Span[(24 + count * stride)..].ToArray());
        return UpgradeSection(file, 3, records);
    }

    private static byte[] UpgradeNestedEvents(NativeFile file)
    {
        var cursor = 12; var links = new List<byte[]>(); var conditions = new List<byte[]>(); var actions = new List<byte[]>();
        uint Word()
        {
            if (cursor > file.Length - 4) throw new InvalidDataException("Truncated historical scenario header.");
            var value = file.UInt32(cursor); cursor += 4; return value;
        }
        int Count(int maximum=32767)
        {
            var count = Word();
            if (count > maximum) throw new InvalidDataException("Historical scenario count exceeds its signed-ID limit.");
            return (int)count;
        }
        byte[] Record(string structure)
        {
            var size = LegacyProjectSchemas.Size(structure);
            if (cursor > file.Length - size) throw new InvalidDataException("Truncated historical scenario record: " + structure);
            var bytes = file.Data.Span.Slice(cursor, size).ToArray(); cursor += size; return bytes;
        }
        static string Value(byte[] bytes, string structure, string name) => LegacyProjectSchemas.Fields(structure).Single(field => field.Name == name).Read(bytes);
        static byte Event(string name) => LegacyEventSchemas.All.Single(type => type.Name == name).Type;
        var scopes = Count();
        for (var scopeIndex = 0; scopeIndex < scopes; scopeIndex++)
        {
            if (Word() != 0xC5) throw new InvalidDataException("Invalid historical action-point marker.");
            var conditionCount = Count(); var scope = Record("MAPACTIONPOINTV1"); var link = new UpgradeBuffer("TRIGGERLINK");
            link.Set("node.type", 4); link.Set("area", Value(scope, "MAPACTIONPOINTV1", "area"));
            link.Set("trigger", conditionCount == 0 ? 0 : conditions.Count + 1); links.Add(link.Bytes);
            var previous = 0;
            for (var conditionIndex = 0; conditionIndex < conditionCount; conditionIndex++)
            {
                if (conditions.Count >= 32767) throw new InvalidDataException("Too many historical conditions.");
                if (Word() != 0xD5) throw new InvalidDataException("Invalid historical condition marker.");
                var actionCount = Count(32768); var old = Record("MAPTRIGGERV1"); var condition = new UpgradeBuffer("TRIGGER");
                string C(string field) => Value(old, "MAPTRIGGERV1", field);
                var conditionID = conditions.Count + 1;
                condition.Set("node.type", 0xD5); condition.Set("id", conditionID); condition.SetPrevious(previous);
                condition.Set("or", conditionIndex + 1 < conditionCount ? conditionID + 1 : 0);
                condition.Set("and", actionCount == 0 ? 0 : 65535 - actions.Count);
                condition.Set("repeat", C("reusable") == "0" ? 1 : 255);
                void Param(string destination, string source) => condition.SetCast(destination, C(source));
                switch (C("node.type"))
                {
                    case "0": condition.Set("ctrl", Event("TRIGGER_CLAIMAREA")); condition.Set("param1", 5); Param("param2", "player"); Param("param5", "squares"); break;
                    case "1": condition.Set("ctrl", Event("TRIGGER_CREAENTERAREA")); condition.Set("param1", 5); Param("param2", "player"); condition.Set("param5", 1); break;
                    case "2": condition.Set("ctrl", Event("TRIGGER_SELECTALLAREA")); Param("param2", "player"); break;
                    case "3": condition.Set("ctrl", Event("TRIGGER_TIMEELAPSED")); condition.Set("param1", 5); Param("param5", "seconds"); break;
                    default: throw new InvalidDataException("Unknown historical condition type: " + C("node.type"));
                }
                conditions.Add(condition.Bytes); previous = conditionID;
                var previousAction = conditionID;
                for (var actionIndex = 0; actionIndex < actionCount; actionIndex++)
                {
                    if (actions.Count >= 32768) throw new InvalidDataException("Too many historical actions.");
                    if (Word() != 0xD6) throw new InvalidDataException("Invalid historical action marker.");
                    var oldAction = Record("MAPACTIONV1"); var action = new UpgradeBuffer("ACTION");
                    string A(string field) => Value(oldAction, "MAPACTIONV1", field);
                    var actionID = 65535 - actions.Count;
                    action.Set("node.type", 0xD6); action.Set("id", actionID); action.Set("one", 1); action.SetPrevious(previousAction);
                    action.Set("next", actionIndex + 1 < actionCount ? actionID - 1 : 0);
                    void Arg(string destination, string source) => action.SetCast(destination, A(source));
                    string type;
                    switch (A("node.type"))
                    {
                        case "0": type = "ACTION_RELEASEBAND"; Arg("param1", "band"); Arg("param5", "area"); break;
                        case "1": type = "ACTION_DESTROYGATE"; Arg("param5", "gate"); break;
                        case "2": type = "ACTION_SHOWAREA"; Arg("param1", "area"); Arg("param2", "activate"); break;
                        case "3": type = "ACTION_ENABLEFOG"; Arg("param1", "area"); Arg("param2", "activate"); break;
                        case "4": type = "ACTION_ADDCREATURE"; Arg("param1", "creatureid"); Arg("param2", "player"); Arg("param3", "level");
                            action.SetCast("param5", (long.Parse(A("x"), CultureInfo.InvariantCulture) - 1).ToString(CultureInfo.InvariantCulture));
                            action.SetCast("param7", (long.Parse(A("y"), CultureInfo.InvariantCulture) - 1).ToString(CultureInfo.InvariantCulture)); break;
                        case "5": type = "ACTION_CHANGESQUARE"; Arg("param1", "terrainid"); Arg("param2", "player"); Arg("param5", "x"); Arg("param7", "y"); break;
                        case "6": type = "ACTION_ADDELEMENT"; Arg("param1", "player"); Arg("param2", "type"); Arg("param3", "element"); action.Set("param4", 1); break;
                        case "7": type = "ACTION_SHOWELEMENT"; Arg("param1", "type"); Arg("param2", "element"); action.Set("param3", A("activate") == "0" ? 1 : 0); break;
                        case "8": type = "ACTION_DISPLAYTIMER"; action.Set("param1", 16); Arg("param5", "seconds"); break;
                        case "9": type = "ACTION_SHOWNEXTROOM"; Arg("param1", "room"); break;
                        default: throw new InvalidDataException("Unknown historical action type: " + A("node.type"));
                    }
                    action.Set("ctrl", Event(type)); actions.Add(action.Bytes); previousAction = actionID;
                }
            }
        }
        if (cursor != file.Length) throw new InvalidDataException("Unexpected trailing historical scenario data.");
        var counts = new byte[12]; WriteUInt32(counts, 0, (uint)links.Count); WriteUInt32(counts, 4, (uint)conditions.Count); WriteUInt32(counts, 8, (uint)actions.Count);
        return UpgradeSection(file, 3, new[] { counts }.Concat(links).Concat(conditions).Concat(actions).Append(new byte[146 * 64]));
    }
}
