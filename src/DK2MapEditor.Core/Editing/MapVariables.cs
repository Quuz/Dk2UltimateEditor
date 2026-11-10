using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record MapVariableDefinition(uint ID, ValueChoice Name, ValueChoice Group, uint DefaultBits, int Minimum, int Maximum, bool FixedPoint)
{
    public int DefaultValue => unchecked((int)DefaultBits);
    // Misc_FloatToText uses an unsigned 20.12 value, not IEEE float or 16.16.
    public string Display(uint bits) => FixedPoint ? (bits / 4096m).ToString("0.0#######", CultureInfo.InvariantCulture)
        : unchecked((int)bits).ToString(CultureInfo.InvariantCulture);
}
public sealed record MapVariableCell(MapVariableDefinition Definition, uint? Bits, int RecordCount, bool Editable, bool Resettable);
public sealed record MapVariableChange(uint ID, int Value);

public static class MapVariables
{
    public static IReadOnlyList<MapVariableDefinition> Definitions { get; } = ReadDefinitions();
    private static IReadOnlyList<MapVariableDefinition> ReadDefinitions()
    {
        using var stream = typeof(MapVariables).Assembly.GetManifestResourceStream("Editor.MapVariableNames")!;
        using var reader = new StreamReader(stream);
        var names = new Dictionary<string, ValueChoice>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var parts = line.Split('|');
            if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace) || !names.TryAdd("szMapVar" + parts[0], new(parts[0], parts[1], parts[2])))
                throw new InvalidDataException("Invalid bilingual map variable label.");
        }
        ValueChoice? group = null; var result = new List<MapVariableDefinition>();
        foreach (var item in LegacyCompilerRules.Elements("MapVars"))
        {
            if (!names.TryGetValue(item.Name, out var name)) throw new InvalidDataException("Missing map variable label: " + item.Name);
            if (item.ID == uint.MaxValue) { group = name; continue; }
            if (item.ID >= 256 || group is null) throw new InvalidDataException("Unverified original map variable catalogue.");
            result.Add(new(item.ID, name with { Value = item.ID.ToString(CultureInfo.InvariantCulture) }, group, item.Variable, item.MinWidth, item.MinHeight, item.Icon != 0));
        }
        if (result.Select(item => item.ID).Distinct().Count() != result.Count) throw new InvalidDataException("Duplicate original variable key.");
        return result.AsReadOnly();
    }
}

public sealed partial class NativeMapDocument
{
    private NativeFile? MapVariableFile => files.LastOrDefault(file => file.Signature == 0xE6 && !Path.GetFileName(file.ExportPath).Equals("GlobalVariables.kwd", StringComparison.OrdinalIgnoreCase)
            && file.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        ?? files.LastOrDefault(file => file.Signature == 0xE6 && !Path.GetFileName(file.ExportPath).Equals("GlobalVariables.kwd", StringComparison.OrdinalIgnoreCase));
    // Unlike generic collection insertion, map-properties commands must never
    // fall back to the shared installation file when paths/order are unusual.
    private NativeRecord AddMapVariableRecord(RecordTemplate template)
    {
        var file = MapVariableFile ?? throw new InvalidOperationException("The map variable collection is missing.");
        var offset = 0;
        Edit("Add map variable override", () => offset = InsertRecord(file, template.Create(new(0, 0), 3, 0)));
        return Records().Single(record => record.File == file && record.Offset == offset);
    }
    public IReadOnlyList<MapVariableCell> MapVariableCells()
    {
        var records = Records();
        var info = IsLegacyProject ? records.SingleOrDefault(record => record.File.Signature == LegacyInfo) : null;
        var slots = info?.Fields.ToDictionary(field => field.Name);
        // Game_ExtractVariables reads the map's Variables.kld, not the separate
        // GlobalVariables.kwd reference. Shared installation data stay opaque
        // to this original-map-properties workflow.
        var native = records.Where(record => record.Category == "GAMEGLOBALVARIABLE" && record.File == MapVariableFile)
            .GroupBy(record => record.File.UInt32(record.Offset)).ToDictionary(group => group.Key, group => group.ToArray());
        return MapVariables.Definitions.Select(definition =>
        {
            uint? bits; int count; bool supported;
            if (IsLegacyProject)
            {
                var field = slots?.GetValueOrDefault($"Variables[{definition.ID}]"); count = field is null ? 0 : 1; supported = field is not null;
                bits = field is null ? null : unchecked((uint)int.Parse(info!.Read(field), CultureInfo.InvariantCulture));
            }
            else
            {
                var matches = native.GetValueOrDefault(definition.ID) ?? []; count = matches.Length; supported = MapVariableFile is not null;
                bits = matches.LastOrDefault() is { } record ? RuleNumber(record, "dwGlobalData") : null;
            }
            return new MapVariableCell(definition, bits, count, supported && !definition.FixedPoint, supported && (!definition.FixedPoint || IsLegacyProject));
        }).ToArray();
    }
    public void SetMapVariables(IEnumerable<MapVariableChange> changes)
    {
        var selected = changes.ToArray(); var cells = MapVariableCells().ToDictionary(cell => cell.Definition.ID);
        if (selected.Select(change => change.ID).Distinct().Count() != selected.Length) throw new ArgumentException("Select each variable once.");
        foreach (var change in selected)
        {
            if (!cells.TryGetValue(change.ID, out var cell) || !cell.Editable) throw new ArgumentException("Select verified editable map variables (upgrade missing project fields first).");
            if (change.Value < cell.Definition.Minimum || change.Value > cell.Definition.Maximum) throw new ArgumentOutOfRangeException(nameof(changes), $"Variable 0x{change.ID:X2}: {cell.Definition.Minimum} … {cell.Definition.Maximum}.");
        }
        Edit("Change global map variables", () =>
        {
            foreach (var change in selected) WriteMapVariable(change.ID, unchecked((uint)change.Value));
        });
    }
    public void ResetMapVariables(IEnumerable<uint> ids)
    {
        var selected = ids.Distinct().ToArray(); var cells = MapVariableCells().ToDictionary(cell => cell.Definition.ID);
        foreach (var id in selected)
            if (!cells.TryGetValue(id, out var cell) || !cell.Resettable) throw new ArgumentException("Select verified resettable variables.");
        Edit("Restore original global map variable defaults", () =>
        {
            // The original compiler omits default-valued integer variables and
            // all four fixed-point variables. Reset in native files removes
            // integer overrides; it does not infer the installation's defaults.
            if (IsLegacyProject) foreach (var id in selected) WriteMapVariable(id, cells[id].Definition.DefaultBits);
            else RemoveMapVariables(selected);
        });
    }
    public void ClearMapVariableOverrides(IEnumerable<uint> ids)
    {
        if (IsLegacyProject) throw new InvalidOperationException("Project variables are an array, not optional overrides.");
        var selected = ids.Distinct().ToArray(); var cells = MapVariableCells().ToDictionary(cell => cell.Definition.ID);
        if (selected.Any(id => !cells.TryGetValue(id, out var cell) || !cell.Editable)) throw new ArgumentException("Select verified integer variables.");
        Edit("Remove native global map variable overrides", () => RemoveMapVariables(selected));
    }
    private void RemoveMapVariables(IReadOnlyList<uint> ids)
    {
        var keys = ids.ToHashSet();
        foreach (var record in Records().Where(record => record.Category == "GAMEGLOBALVARIABLE" && record.File == MapVariableFile && keys.Contains(record.File.UInt32(record.Offset))).OrderByDescending(record => record.Offset)) DeleteRecord(record);
    }
    private void WriteMapVariable(uint id, uint bits)
    {
        if (IsLegacyProject)
        {
            var record = Records().Single(record => record.File.Signature == LegacyInfo);
            SetField(record, record.Fields.Single(field => field.Name == $"Variables[{id}]"), unchecked((int)bits).ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            var record = Records().LastOrDefault(record => record.Category == "GAMEGLOBALVARIABLE" && record.File == MapVariableFile && record.File.UInt32(record.Offset) == id);
            if (record is null)
            {
                // The first DWORD is the dispatch key, not a generic editable
                // header. Construct it directly with a preflight-verified ID.
                record = AddMapVariableRecord(new("GAMEGLOBALVARIABLE", 0xE6, id, 16));
            }
            SetField(record, record.Fields.Single(field => field.Name == "dwGlobalData"), bits.ToString(CultureInfo.InvariantCulture));
        }
    }
}
