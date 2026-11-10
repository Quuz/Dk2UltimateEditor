using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public sealed record GlobalDefinitionSection(string Name, string Family, uint Mask);
public sealed record GlobalDefinitionRow(string Family, int Index, uint ID, string Symbol, NativeRecord? Record, bool Editable);
public sealed record GlobalDefinitionValue(string Name, string Section, string Value, string DefaultValue, bool Editable);
public sealed record GlobalDefinitionChange(string Family, int Index, string Field, string Value);

/// <summary>The exact fourteen source reset sections. Defaults are semantic
/// assignments, never whole-record replacements of unknown native bytes.</summary>
public static class GlobalDefinitionDefaults
{
    public static IReadOnlyList<GlobalDefinitionSection> Sections { get; } = Array.AsReadOnly(new[] {
        new GlobalDefinitionSection("Terrains", "Terrains", 1), new("Rooms", "Rooms", 2), new("Doors", "Doors", 4), new("Traps", "Traps", 8), new("Spells", "Spells", 16),
        new("Attributes", "Creatures", 0x100), new("Parameters", "Creatures", 0x200), new("Flags", "Creatures", 0x400), new("Attacks", "Creatures", 0x800),
        new("Resistances", "Creatures", 0x1000), new("Possession", "Creatures", 0x2000), new("Jobs", "Creatures", 0x4000), new("Light", "Creatures", 0x8000), new("CrSpells", "CrSpells", 0x10000) });
    internal sealed record Rule(string Family, string Section, string Name, NativeField ReadField, string Cast);
    private static readonly Lazy<IReadOnlyList<Rule>> rules = new(ReadRules);
    private static readonly Lazy<IReadOnlyDictionary<string, byte[]>> defaults = new(() => new ReadOnlyDictionary<string, byte[]>(LegacyGlobalSchemas.All.ToDictionary(family => family.Group, family =>
    {
        using var stream = typeof(GlobalDefinitionDefaults).Assembly.GetManifestResourceStream("Legacy." + FileName(family.Group))!;
        using var buffer = new MemoryStream(); stream.CopyTo(buffer); var bytes = buffer.ToArray();
        if (bytes.Length != LegacyCompilerRules.DefinitionOrder(family.Group).Count * family.RecordSize) throw new InvalidDataException("Unverified original default array: " + family.Group);
        return bytes;
    })));
    internal static string Structure(string family) => family switch { "Terrains" => "GLOBALTERRAIN", "Rooms" => "GLOBALROOM", "Doors" => "GLOBALDOOR", "Traps" => "GLOBALTRAP", "Spells" => "GLOBALSPELL", "Creatures" => "GLOBALCREATURE", "CrSpells" => "GLOBALCRSPELLS", _ => throw new ArgumentException("Unknown global family.") };
    private static string CatalogueName(string family) => family switch { "Terrains" => "GTerrains", "Rooms" => "GRooms", "Doors" => "GDoors", "Traps" => "GTraps", "Spells" => "GSpells", "Creatures" => "GCreatures", "CrSpells" => "GCrSpellsLst", _ => throw new ArgumentException("Unknown global family.") };
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<LegacyCompilerRules.Element>>> catalogues = new(() =>
    {
        using var stream = typeof(GlobalDefinitionDefaults).Assembly.GetManifestResourceStream("Legacy.Datas_GlobalVariables.c")!;
        using var reader = new StreamReader(stream); var text = Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
        return LegacyGlobalSchemas.All.ToDictionary(family => family.Group, family => (IReadOnlyList<LegacyCompilerRules.Element>)Regex.Matches(
            Regex.Match(text, @"ELEMENT\s+" + CatalogueName(family.Group) + @"\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value,
            @"\{\s*(0x[0-9A-Fa-f]+)\s*,\s*(?:0x[0-9A-Fa-f]+)\s*,\s*(\d+)\s*,\s*-?\d+\s*,\s*-?\d+\s*,\s*-?\d+\s*,\s*-?\d+\s*,\s*(sz\w+)\s*,[^}]*\}")
            .Select(match => new LegacyCompilerRules.Element(uint.Parse(match.Groups[1].Value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture), 0, int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), match.Groups[3].Value)).ToArray());
    });
    public static IReadOnlyList<LegacyCompilerRules.Element> Catalogue(string family) => catalogues.Value[family];
    internal static int ProjectCount(string family) => family == "Creatures" ? 46 : Catalogue(family).Count;
    private static string FileName(string family) => "Default" + (family switch { "Terrains" => "Terrain", "Spells" => "KeeperSpells", "CrSpells" => "CreatureSpells", _ => family }) + ".dkg";
    internal static IReadOnlyList<Rule> Rules(string family) => rules.Value.Where(rule => rule.Family == family).ToArray();
    internal static ReadOnlySpan<byte> NativeDefault(string family, int index)
    {
        var definition = LegacyGlobalSchemas.All.Single(item => item.Group == family); var order = LegacyCompilerRules.DefinitionOrder(family);
        var nativeIndex = order.ToList().IndexOf(index); if (nativeIndex < 0) throw new ArgumentOutOfRangeException(nameof(index));
        return defaults.Value[family].AsSpan(nativeIndex * definition.RecordSize, definition.RecordSize);
    }
    internal static string ProjectDefault(Rule rule, int index)
    {
        var value = rule.ReadField.Read(NativeDefault(rule.Family, index));
        if (rule.Cast == "UINT" && rule.ReadField.Kind == NativeValueKind.Int32) value = unchecked((uint)int.Parse(value, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture);
        return value;
    }
    private static IReadOnlyList<Rule> ReadRules()
    {
        using var stream = typeof(GlobalDefinitionDefaults).Assembly.GetManifestResourceStream("Legacy.Properties_Global_Functions.c")!;
        using var reader = new StreamReader(stream); var text = Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
        text = text[text.IndexOf("void Properties_GlobalReset(", StringComparison.Ordinal)..]; text = text[..text.IndexOf("int Properties_GlobalChanged(", StringComparison.Ordinal)];
        var blocks = Regex.Split(text, @"if\s*\(Flags&MGRF_(\w+)\)");
        var result = new List<Rule>();
        var names = new Dictionary<string, string> { ["TERRAINS"] = "Terrains", ["ROOMS"] = "Rooms", ["DOORS"] = "Doors", ["TRAPS"] = "Traps", ["SPELLS"] = "Spells", ["CRATTRS"] = "Attributes", ["CRPARAMS"] = "Parameters", ["CRFLAGS"] = "Flags", ["CRATTACKS"] = "Attacks", ["CRRESISTANCES"] = "Resistances", ["CRPOSSESSION"] = "Possession", ["CRJOBS"] = "Jobs", ["CRLIGHT"] = "Light", ["CREATURESPELLS"] = "CrSpells" };
        for (var index = 1; index < blocks.Length; index += 2)
        {
            var section = names[blocks[index]];
            foreach (Match match in Regex.Matches(blocks[index + 1], @"Variables->(?<family>\w+)\[Offset\]\.(?<name>\w+(?:\[\d+\])?)\s*=\s*\((?<cast>\w+)\)(?<expression>[^;]+);"))
            {
                var family = match.Groups["family"].Value; var name = match.Groups["name"].Value; var cast = match.Groups["cast"].Value; var expression = match.Groups["expression"].Value;
                var pointer = Regex.Match(expression, @"\*\(\(\s*(BYTE|WORD|LONG|short)\s*\*\)\(Default\+(0x[0-9A-Fa-f]+)\)\)");
                var flags = Regex.Match(expression, @"\*\(Default\+(0x[0-9A-Fa-f]+)\)");
                if (!pointer.Success && !flags.Success) throw new InvalidDataException("Unverified source default: " + name);
                var offset = int.Parse((pointer.Success ? pointer.Groups[2] : flags.Groups[1]).Value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var kind = pointer.Success ? pointer.Groups[1].Value switch { "WORD" => NativeValueKind.UInt16, "LONG" => NativeValueKind.Int32, "short" => NativeValueKind.Int16, _ => NativeValueKind.Byte } : NativeValueKind.Byte;
                var size = kind switch { NativeValueKind.Int32 => 4, NativeValueKind.UInt16 or NativeValueKind.Int16 => 2, _ => 1 };
                var mask = cast == "BOOL" ? byte.Parse(Regex.Match(expression, @"&\s*0x([0-9A-Fa-f]+)").Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture) : (byte)0;
                var native = LegacyGlobalSchemas.All.Single(item => item.Group == family).Fields.Single(field => field.Name == name);
                if (native.Offset != offset || native.Size != size || native.BitMask != mask) throw new InvalidDataException("Reset/compiler layout disagreement: " + family + "." + name);
                result.Add(new(family, section, name, new(name, offset, size, kind, mask), cast));
            }
        }
        if (result.Count != 262 || result.Select(rule => (rule.Family, rule.Name)).Distinct().Count() != 262 || result.Select(rule => rule.Section).Distinct().Count() != 14)
            throw new InvalidDataException("Original reset assignments are incomplete.");
        return result.AsReadOnly();
    }
}

public sealed partial class NativeMapDocument
{
    public IReadOnlyList<GlobalDefinitionRow> GlobalDefinitionRows()
    {
        var records = Records(); var result = new List<GlobalDefinitionRow>();
        var globalFiles = projectSections.Where(file => file.Signature == LegacyGlobals).ToArray();
        var projectVerified = globalFiles.Length == 0 || globalFiles.Length == 1 && globalFiles[0].UInt32(4) == 1
            && (globalFiles[0].UInt32(12) & ~0x1FF1Fu) == 0 && !records.Any(record => record.File == globalFiles[0] && record.Category == "Project.Opaque");
        foreach (var family in LegacyGlobalSchemas.All)
        {
            var order = LegacyCompilerRules.DefinitionOrder(family.Group); var catalogue = GlobalDefinitionDefaults.Catalogue(family.Group);
            var candidates = records.Where(record => record.Category == (IsLegacyProject ? "Project." + GlobalDefinitionDefaults.Structure(family.Group) : "Definition." + family.Group)).ToArray();
            var supported = IsLegacyProject ? projectVerified && (candidates.Length == 0 || candidates.Length == GlobalDefinitionDefaults.ProjectCount(family.Group))
                : VerifiedDefinitionCatalogue(family, candidates);
            for (var index = 0; index < catalogue.Count; index++)
            {
                var item = catalogue[index]; var nativeIndex = order.ToList().IndexOf(index);
                var position = IsLegacyProject ? index : nativeIndex;
                var record = position >= 0 && position < candidates.Length ? candidates[position] : null;
                result.Add(new(family.Group, index, item.ID, item.Name, record, supported && nativeIndex >= 0 && (IsLegacyProject || record is not null)));
            }
        }
        return result;
    }
    public IReadOnlyList<GlobalDefinitionValue> GlobalDefinitionValues(GlobalDefinitionRow row)
    {
        var current = GlobalDefinitionRows().Single(item => item.Family == row.Family && item.Index == row.Index);
        return GlobalDefinitionDefaults.Rules(row.Family).Select(rule =>
        {
            var field = current.Record?.Fields.FirstOrDefault(field => field.Name == rule.Name);
            var value = !current.Editable ? "" : field is null ? GlobalDefinitionDefaults.ProjectDefault(rule, row.Index) : current.Record!.Read(field);
            var reference = IsLegacyProject ? GlobalDefinitionDefaults.ProjectDefault(rule, row.Index)
                : LegacyGlobalSchemas.All.Single(family => family.Group == row.Family).Fields.Single(field => field.Name == rule.Name).Read(GlobalDefinitionDefaults.NativeDefault(row.Family, row.Index));
            return new GlobalDefinitionValue(rule.Name, rule.Section, value, reference, current.Editable);
        }).ToArray();
    }
    public void SetGlobalDefinitionValues(IEnumerable<GlobalDefinitionChange> changes)
    {
        var requested = changes.ToArray(); if (requested.Length == 0) return;
        if (requested.Select(change => (change.Family, change.Index, change.Field)).Distinct().Count() != requested.Length) throw new ArgumentException("Duplicate definition targets.");
        var rows = GlobalDefinitionRows();
        var writes = requested.Select(change =>
        {
            var row = rows.SingleOrDefault(row => row.Family == change.Family && row.Index == change.Index) ?? throw new ArgumentException("Unknown definition target.");
            if (!row.Editable) throw new InvalidOperationException("A verified definition layout is required.");
            var rule = GlobalDefinitionDefaults.Rules(change.Family).SingleOrDefault(rule => rule.Name == change.Field) ?? throw new ArgumentException("Unknown definition field.");
            var field = IsLegacyProject ? LegacyProjectSchemas.Fields(GlobalDefinitionDefaults.Structure(change.Family)).Single(field => field.Name == change.Field)
                : row.Record!.Fields.Single(field => field.Name == change.Field);
            var bytes = field.Encode(change.Value);
            if (rule.Cast == "BOOL" && (bytes[0] > 1 || bytes.Skip(1).Any(value => value != 0))) throw new ArgumentException("A source flag must be zero or one.");
            var old = row.Record is null ? field.Encode(GlobalDefinitionDefaults.ProjectDefault(rule, change.Index)) : field.Encode(row.Record.Read(field));
            return (change, bytes, NoOp: old.AsSpan().SequenceEqual(bytes));
        }).Where(write => !write.NoOp).ToArray();
        if (writes.Length == 0) return;
        Edit("Change global definitions", () =>
        {
            if (IsLegacyProject) EnsureProjectDefinitionFamilies(writes.Select(write => write.change.Family).Distinct().ToArray());
            var current = GlobalDefinitionRows();
            foreach (var (change, bytes, _) in writes)
            {
                var row = current.Single(row => row.Family == change.Family && row.Index == change.Index); var record = row.Record!;
                var field = record.Fields.Single(field => field.Name == change.Field); var offset = record.Offset + field.Offset;
                if (field.BitMask != 0) record.File.Bytes[offset] = (byte)((record.File.Bytes[offset] & ~field.BitMask) | (bytes[0] == 0 ? 0 : field.BitMask));
                else bytes.CopyTo(record.File.Bytes, offset);
            }
        });
    }
    public void ResetGlobalDefinitions(string section, IEnumerable<int>? indices = null)
    {
        var selected = GlobalDefinitionDefaults.Sections.SingleOrDefault(item => item.Name == section) ?? throw new ArgumentException("Unknown source reset section.");
        var rows = GlobalDefinitionRows().Where(row => row.Family == selected.Family).ToArray();
        if (!IsLegacyProject && indices is null && (rows.All(row => !row.Editable) || rows.Any(row => row.Record is not null && !row.Editable)))
            throw new InvalidOperationException("A verified available definition catalogue is required.");
        var targets = indices?.ToArray() ?? rows.Where(row => IsLegacyProject || row.Editable).Select(row => row.Index).ToArray();
        if (targets.Distinct().Count() != targets.Length || targets.Any(index => !rows.Any(row => row.Index == index && row.Editable))) throw new InvalidOperationException("Unverified or duplicate reset target.");
        var rules = GlobalDefinitionDefaults.Rules(selected.Family).Where(rule => rule.Section == section).ToArray();
        SetGlobalDefinitionValues(targets.SelectMany(index => rules.Select(rule => new GlobalDefinitionChange(selected.Family, index, rule.Name,
            IsLegacyProject ? GlobalDefinitionDefaults.ProjectDefault(rule, index) : LegacyGlobalSchemas.All.Single(family => family.Group == selected.Family).Fields.Single(field => field.Name == rule.Name).Read(GlobalDefinitionDefaults.NativeDefault(selected.Family, index))))));
    }
    private void EnsureProjectDefinitionFamilies(IReadOnlyList<string> families)
    {
        var existing = projectSections.SingleOrDefault(file => file.Signature == LegacyGlobals);
        var flags = existing?.UInt32(12) ?? 0u;
        // Files_Editor writes the entire creature array with MGRF_CREATURES,
        // not just the mask of the reset page that was edited.
        var requested = families.Aggregate(flags, (mask, family) => mask | GlobalDefinitionDefaults.Sections.Where(section => section.Family == family).Aggregate(0u, (value, section) => value | section.Mask));
        if (requested == flags) return;
        var old = Records(); var payload = new List<byte>(BitConverter.GetBytes(requested));
        foreach (var family in LegacyGlobalSchemas.All)
        {
            var familyMask = GlobalDefinitionDefaults.Sections.Where(section => section.Family == family.Group).Aggregate(0u, (mask, section) => mask | section.Mask);
            if ((requested & familyMask) == 0) continue;
            var structure = GlobalDefinitionDefaults.Structure(family.Group); var present = old.Where(record => record.Category == "Project." + structure).ToArray();
            if (present.Length > 0) { foreach (var record in present) payload.AddRange(record.File.Bytes.AsSpan(record.Offset, record.Length).ToArray()); continue; }
            var fields = LegacyProjectSchemas.Fields(structure);
            for (var index = 0; index < GlobalDefinitionDefaults.ProjectCount(family.Group); index++)
            {
                var bytes = new byte[LegacyProjectSchemas.Size(structure)];
                if (index < GlobalDefinitionDefaults.Catalogue(family.Group).Count)
                    foreach (var rule in GlobalDefinitionDefaults.Rules(family.Group)) fields.Single(field => field.Name == rule.Name).Encode(GlobalDefinitionDefaults.ProjectDefault(rule, index)).CopyTo(bytes, fields.Single(field => field.Name == rule.Name).Offset);
                payload.AddRange(bytes);
            }
        }
        var sectionBytes = new byte[12 + payload.Count]; WriteUInt32(sectionBytes, 0, LegacyGlobals); WriteUInt32(sectionBytes, 4, 1); WriteUInt32(sectionBytes, 8, (uint)payload.Count); payload.CopyTo(sectionBytes, 12);
        if (existing is not null) existing.Bytes = sectionBytes;
        else throw new InvalidOperationException("The optional global section must be initialized before transactions.");
    }
}
