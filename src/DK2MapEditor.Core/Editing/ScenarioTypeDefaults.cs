using System.Buffers.Binary;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

// Editor defaults and save-time normalization are separate from compiler padding.
internal static class ScenarioTypeDefaults
{
    private sealed record Catalogue(Dictionary<string, uint[]> Changed, HashSet<string> ZeroOnSave,
        Dictionary<uint, (string Type, uint[] Values)> Initial);
    private static readonly Catalogue catalogue = Load();
    internal static bool ClearsParametersOnSave(string type) => catalogue.ZeroOnSave.Contains(type);

    internal static uint[] Values(LegacyEventSchemas.EventType type, uint[] previous, bool initial, bool save = true)
    {
        var seed = catalogue.Initial[type.RecordSignature];
        var result = initial ? (uint[])seed.Values.Clone() : (uint[])previous.Clone();
        if ((!initial || type.Name != seed.Type) && catalogue.Changed.TryGetValue(type.Name, out var changed))
            result = (uint[])changed.Clone();
        if (save && catalogue.ZeroOnSave.Contains(type.Name)) Array.Clear(result);
        return result;
    }

    internal static LegacyEventSchemas.EventType InitialType(uint signature) =>
        LegacyEventSchemas.All.Single(type => type.Name == catalogue.Initial[signature].Type);

    private static Catalogue Load()
    {
        var changed = new Dictionary<string, uint[]>();
        var zero = new HashSet<string>();
        var initial = new Dictionary<uint, (string, uint[])>();
        foreach (var (kind, resource, signature) in new[] {
            ("Trigger", "Legacy.Triggers_Triggers.c", 0xD5u), ("Action", "Legacy.Triggers_Actions.c", 0xD6u) })
        {
            using var stream = typeof(ScenarioTypeDefaults).Assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidDataException("Original editor defaults are missing.");
            using var reader = new StreamReader(stream);
            var source = Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
            var defaults = Body(source, "Triggers_" + kind + "DlgSetDefault");
            foreach (Match branch in Regex.Matches(defaults,
                @"(?<cases>(?:\s*case\s+(?:TRIGGER|ACTION)_\w+\s*:\s*)+)(?<body>[\s\S]*?)\bbreak\s*;"))
            {
                if (!branch.Groups["body"].Value.Contains("Temp" + kind + ".ctrl != NewCtrl", StringComparison.Ordinal))
                    throw new InvalidDataException("Original default type guard differs.");
                var values = Assignments(branch.Groups["body"].Value, kind);
                foreach (Match name in Regex.Matches(branch.Groups["cases"].Value, @"(?:TRIGGER|ACTION)_\w+"))
                    changed.Add(name.Value, values);
            }
            var creation = Body(source, "Triggers_" + kind + "New");
            var first = Regex.Match(creation, @"if\s*\(!Context->" + kind + @"Ctx\.Temp" + kind + @"\.ctrl\)\s*\{(?<body>[^{}]+)\}");
            if (!first.Success) throw new InvalidDataException("Original first-event defaults differ.");
            var initialType = Regex.Match(first.Groups["body"].Value, @"\.ctrl\s*=\s*((?:TRIGGER|ACTION)_\w+)\s*;");
            if (!initialType.Success) throw new InvalidDataException("Original first-event type is missing.");
            initial.Add(signature, (initialType.Groups[1].Value, Assignments(first.Groups["body"].Value, kind)));
            var save = Body(source, "Triggers_" + kind + "DlgParamsSave");
            var saveBranch = Regex.Match(save, @"switch\([^)]*\.ctrl\)\s*\{(?<cases>(?:\s*case\s+(?:TRIGGER|ACTION)_\w+\s*:\s*)+)(?<body>[\s\S]*?)\bbreak\s*;");
            if (!saveBranch.Success || Assignments(saveBranch.Groups["body"].Value, kind).Any(value => value != 0))
                throw new InvalidDataException("Original parameter-free save branch differs.");
            foreach (Match name in Regex.Matches(saveBranch.Groups["cases"].Value, @"(?:TRIGGER|ACTION)_\w+")) zero.Add(name.Value);
        }
        if (changed.Count != 70 || zero.Count != 26 || changed.Keys.Concat(zero).Any(name => !LegacyEventSchemas.All.Any(type => type.Name == name)))
            throw new InvalidDataException("Original creation/type-change catalogue is incomplete.");
        return new(changed, zero, initial);
    }

    private static uint[] Assignments(string source, string kind)
    {
        var assignments = Regex.Matches(source, @"Temp" + kind + @"\.param([1-8])\s*=\s*(-?(?:0x[0-9A-Fa-f]+|\d+))\s*;");
        if (assignments.Count != 8 || assignments.Select(match => match.Groups[1].Value).Distinct().Count() != 8)
            throw new InvalidDataException("Original eight-parameter default assignment is incomplete.");
        var values = new uint[8];
        foreach (Match assignment in assignments)
        {
            var text = assignment.Groups[2].Value;
            var value = text.StartsWith("0x", StringComparison.Ordinal)
                ? long.Parse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                : long.Parse(text, CultureInfo.InvariantCulture);
            values[int.Parse(assignment.Groups[1].Value, CultureInfo.InvariantCulture) - 1] = unchecked((uint)value);
        }
        return values;
    }

    private static string Body(string source, string name)
    {
        var definition = Regex.Match(source, @"^(?:void|int)\s+" + name + @"\([^)]*\)\s*\{", RegexOptions.Multiline);
        if (!definition.Success) throw new InvalidDataException("Original editor function is missing: " + name);
        var start = definition.Index + definition.Length; var end = start; var depth = 1;
        while (depth != 0 && end < source.Length)
        { if (source[end] == '{') depth++; else if (source[end] == '}') depth--; end++; }
        if (depth != 0) throw new InvalidDataException("Original editor function is truncated.");
        return source[start..(end - 1)];
    }
}

public sealed class ScenarioEventTypeChange
{
    internal ScenarioEventTypeChange(NativeMapDocument owner, NativeRecord record, LegacyEventSchemas.EventType type, long revision, byte[] fingerprint)
    { Owner = owner; Record = record; Type = type; Revision = revision; Fingerprint = fingerprint; }
    internal NativeMapDocument Owner { get; }
    internal NativeRecord Record { get; }
    internal LegacyEventSchemas.EventType Type { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
}

public sealed partial class NativeMapDocument
{
    public ScenarioEventTypeChange PrepareScenarioEventTypeChange(NativeRecord record, LegacyEventSchemas.EventType type)
    {
        if (transactionOpen) throw new InvalidOperationException("Finish the current edit before selecting an event type.");
        var current = CurrentScenarioRecord(record);
        if (ScenarioSignature(current) != type.RecordSignature || !ReferenceEquals(LegacyEventSchemas.Find(type.RecordSignature, type.Type), type))
            throw new ArgumentException("This event type does not match the current event category.");
        return new(this, current, type, EditRevision, UnusedScriptFingerprint());
    }

    public void ApplyScenarioEventTypeChange(ScenarioEventTypeChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (transactionOpen || !ReferenceEquals(change.Owner, this) || change.Revision != EditRevision
            || !UnusedScriptFingerprint().AsSpan().SequenceEqual(change.Fingerprint))
            throw new InvalidOperationException("The document changed after selecting an event type.");
        ChangeScenarioEventType(change.Record, change.Type);
    }

    /// <summary>Human type selection: same type retains values; changed types use original editor defaults.</summary>
    public void ChangeScenarioEventType(NativeRecord record, LegacyEventSchemas.EventType type) =>
        Edit(type.Name, () => WriteScenarioEventType(record, type, false));

    /// <summary>Add a human-created event using the original first-event defaults and repeat value.</summary>
    public NativeRecord AddScenarioEvent(uint signature, TilePoint position, LegacyEventSchemas.EventType? type = null)
    {
        if (signature is not (0xD5 or 0xD6)) throw new ArgumentException("Unknown scenario record category.");
        type ??= ScenarioTypeDefaults.InitialType(signature);
        if (type.RecordSignature != signature) throw new ArgumentException("Event type does not match the selected category.");
        ushort id = 0;
        Edit("Add scenario event", () =>
        {
            var kind = signature == 0xD5 ? "Trigger" : "Action";
            var added = IsLegacyProject ? AddLegacyRecord(kind, position)
                : AddRecord(RecordTemplate.All.Single(template => template.Structure == kind), position);
            id = ScriptGraph.ID(added);
            WriteScenarioEventType(added, type, true);
        });
        return ScriptGraph.Build(this).Events[id].Record;
    }

    private NativeRecord CurrentScenarioRecord(NativeRecord record)
    {
        if (IsLegacyProject) RequireLegacyCollection(record); else RequireCollectionRecord(record);
        var current = Records().Single(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length);
        if (!ScriptGraph.IsEvent(current)) throw new ArgumentException("Select a current scenario event.");
        if (current.Length != (IsLegacyProject ? 56 : 24) || !IsLegacyProject && current.File.UInt32(4) != 4)
            throw new InvalidOperationException("The scenario event layout/version is unverified.");
        return current;
    }

    private uint ScenarioSignature(NativeRecord record) => IsLegacyProject
        ? record.Category == "Project.TRIGGER" ? 0xD5u : 0xD6u : record.File.UInt32(record.Offset);

    private void WriteScenarioEventType(NativeRecord record, LegacyEventSchemas.EventType type, bool initial, bool save = true)
    {
        var current = CurrentScenarioRecord(record);
        if (ScenarioSignature(current) != type.RecordSignature || !ReferenceEquals(LegacyEventSchemas.Find(type.RecordSignature, type.Type), type))
            throw new ArgumentException("This event type does not match the current record catalogue/category.");
        var typeOffset = current.Offset + (IsLegacyProject ? 54 : 22);
        var previousType = current.File.Bytes[typeOffset];
        if (!initial && previousType == type.Type) return;
        var previous = new uint[8];
        foreach (var field in current.Fields)
        {
            var match = Regex.Match(field.Name, @"^(?:param|Param)([1-8])$");
            if (match.Success) previous[int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) - 1] =
                unchecked((uint)long.Parse(current.Read(field), CultureInfo.InvariantCulture));
        }
        var values = ScenarioTypeDefaults.Values(type, previous, initial, save);
        if (IsLegacyProject)
        {
            for (var number = 1; number <= 8; number++)
            {
                var field = current.Fields.Single(field => field.Name == "param" + number);
                BinaryPrimitives.WriteUInt32LittleEndian(current.File.Bytes.AsSpan(current.Offset + field.Offset, 4), values[number - 1]);
            }
        }
        else
        {
            var payload = (byte[])type.DefaultPayload.Clone();
            foreach (var field in type.Arguments)
            {
                var match = Regex.Match(field.Name, @"^Param([1-8])$");
                if (!match.Success)
                {
                    if (type.Name is "ACTION_SHOWCRYSTAL" or "ACTION_DESTROYGATE") continue;
                    throw new InvalidDataException("Unsupported original compiler parameter mapping.");
                }
                var value = values[int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) - 1];
                // Original compiler BYTE/WORD/LONG casts intentionally retain low bits.
                if (field.Size == 1) payload[field.Offset - 8] = unchecked((byte)value);
                else if (field.Size == 2) BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(field.Offset - 8), unchecked((ushort)value));
                else if (field.Size == 4) BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(field.Offset - 8), value);
                else throw new InvalidDataException("Unsupported original compiler operand width.");
            }
            if (type.Name == "ACTION_SHOWCRYSTAL")
            {
                var crystal = Records().FirstOrDefault(item => item.Category == "GAMEOBJECT"
                    && item.Read(item.Fields.Single(field => field.Name == "bObjectID")) == "28");
                for (var axis = 0; axis < 2; axis++)
                {
                    var coordinate = crystal is null ? -1 : int.Parse(crystal.Read(crystal.Fields.Single(field => field.Name == (axis == 0 ? "dwObjectLocationX" : "dwObjectLocationY"))), CultureInfo.InvariantCulture);
                    BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4 + axis * 2), unchecked((ushort)coordinate));
                }
            }
            if (type.Name == "ACTION_DESTROYGATE")
            {
                var gate = MapRegions().FirstOrDefault(item => item.Kind == MapRegionKind.Gate
                    && EntityReferences.Identity(item.Record) is { } identity
                    && uint.Parse(item.Record.Read(identity.Field), CultureInfo.InvariantCulture) == values[4]);
                BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4), gate is null ? (ushort)0 : checked((ushort)(gate.Bounds.X + 1)));
                BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6), gate is null ? (ushort)0 : checked((ushort)(gate.Bounds.Y + 1)));
            }
            payload.CopyTo(current.File.Bytes, current.Offset + 8);
        }
        current.File.Bytes[typeOffset] = type.Type;
        if (initial) current.File.Bytes[current.Offset + (IsLegacyProject ? 55 : 23)] = 1;
    }
}
