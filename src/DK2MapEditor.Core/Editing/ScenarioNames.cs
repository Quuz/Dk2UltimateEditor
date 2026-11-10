using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public enum ScenarioNameKind { Variable, Timer }
public sealed record ScenarioNameSlot(ScenarioNameKind Kind, int Index, string StoredName, int References);
public sealed record ScenarioNameTable(bool Supported, bool ReferencesVerified, IReadOnlyList<ScenarioNameSlot> Slots);
public sealed class ScenarioNameDraft
{
    internal ScenarioNameDraft(NativeMapDocument owner, long revision, byte[] fingerprint, ScenarioNameKind kind, int index, string name)
    { Owner = owner; Revision = revision; Fingerprint = fingerprint; Kind = kind; Index = index; Name = name; }
    internal NativeMapDocument Owner { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    public ScenarioNameKind Kind { get; }
    public int Index { get; }
    public string Name { get; }
}

public sealed partial class NativeMapDocument
{
    public const string ScenarioNamesExtension = ".dk2names";
    private const uint ScenarioNamesSignature = 0x4E533244; // D2SN, editor-only companion, not a game collection.
    private const int ScenarioNamesSize = 12 + 146 * 64;
    private static readonly NativeField ScenarioAlias = new("Name", 0, 64, NativeValueKind.WesternAnsi);
    private NativeFile? nativeScenarioNames;
    private byte[]? originalNativeScenarioNames;
    private bool nativeScenarioNamesLoaded;
    private NativeFile NativeScenarioNames
    {
        get
        {
            if (nativeScenarioNames is null)
            {
                var bytes = new byte[ScenarioNamesSize]; WriteUInt32(bytes, 0, ScenarioNamesSignature);
                WriteUInt32(bytes, 4, 1); WriteUInt32(bytes, 8, ScenarioNamesSize);
                nativeScenarioNames = new(MainFile.SourcePath + ScenarioNamesExtension, MainFile.ExportPath + ScenarioNamesExtension, bytes);
                originalNativeScenarioNames = (byte[])bytes.Clone();
            }
            return nativeScenarioNames;
        }
    }
    // Editor metadata participates in history/fingerprints without pretending
    // to be a KWD-linked game file in Files/Records/structural validation.
    private IEnumerable<NativeFile> TransactionFiles() => IsLegacyProject ? files.Append(LevelModifierDeltaFile).Append(LevelModifierOwnedFile) : files.Append(NativeScenarioNames).Append(NativeGameTarget).Append(NativeResizeFill).Append(NativeGateBindings).Append(NativeAreaScopes).Append(NativeOwnerScopes);
    private bool ScenarioNamesChanged => !IsLegacyProject && !NativeScenarioNames.Data.Span.SequenceEqual(originalNativeScenarioNames);
    private void LoadNativeScenarioNames()
    {
        var file = NativeScenarioNames;
        if (!File.Exists(file.SourcePath))
        {
            // Official compiled maps keep their seven game files in Maps;
            // editor-only alias metadata lives in a sibling directory.
            var leaf = Path.GetFileName(MainFile.SourcePath) + ScenarioNamesExtension;
            var metadataPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(MainFile.SourcePath)!, "..", "EditorMetadata", leaf));
            if (File.Exists(metadataPath))
                nativeScenarioNames = file = new(metadataPath, Path.Combine("Data", "Editor", "EditorMetadata", leaf), file.Data.ToArray());
        }
        if (!File.Exists(file.SourcePath)) return;
        using var stream = new FileStream(file.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is < 12 or > 65536) throw new InvalidDataException("Editor scenario-name metadata exceeds bounded header/size limits.");
        var bytes = new byte[(int)stream.Length]; stream.ReadExactly(bytes); file.Bytes = bytes;
        originalNativeScenarioNames = (byte[])bytes.Clone(); nativeScenarioNamesLoaded = true;
        // Unknown signatures/versions/layouts remain opaque and export exactly;
        // they never become guessed editable alias tables.
    }
    public bool CanEditScenarioNames => IsLegacyProject
        ? projectSections.Single(file => file.Signature == LegacyEvents).UInt32(4) is 2 or 3
        : NativeScenarioNames.Length == ScenarioNamesSize && NativeScenarioNames.Signature == ScenarioNamesSignature
            && NativeScenarioNames.UInt32(4) == 1 && NativeScenarioNames.UInt32(8) == ScenarioNamesSize;
    private static int ScenarioSlot(ScenarioNameKind kind, int index)
    {
        if (!Enum.IsDefined(kind) || index < 0 || index >= (kind == ScenarioNameKind.Variable ? 129 : 17))
            throw new ArgumentOutOfRangeException(nameof(index));
        return kind == ScenarioNameKind.Variable ? index : 129 + index;
    }
    private (NativeFile File, int Offset) ScenarioAliasLocation(ScenarioNameKind kind, int index)
    {
        var slot = ScenarioSlot(kind, index);
        if (!CanEditScenarioNames) throw new InvalidOperationException("Scenario-name layout is unverified; upgrade the historical project or retain opaque metadata.");
        if (!IsLegacyProject) return (NativeScenarioNames, 12 + slot * 64);
        // Link/condition/action insertions change offsets. Always resolve the
        // current bounded alias table by its end, never a cached record offset.
        var file = projectSections.Single(file => file.Signature == LegacyEvents);
        return (file, file.Length - 146 * 64 + slot * 64);
    }
    public void SetScenarioName(ScenarioNameKind kind, int index, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _ = Records(); // Independently bounded current collection/table before a write.
        var location = ScenarioAliasLocation(kind, index);
        if (ScenarioAlias.Read(location.File.Data.Span.Slice(location.Offset, 64)) == name) return;
        var encoded = ScenarioAlias.Encode(name);
        Edit("Rename scenario variable or timer", () => encoded.CopyTo(location.File.Bytes, location.Offset));
    }
    public ScenarioNameDraft CreateScenarioNameDraft(ScenarioNameKind kind, int index)
    {
        if (transactionOpen) throw new InvalidOperationException("Finish the current edit before naming a scenario slot.");
        _ = Records(); var location = ScenarioAliasLocation(kind, index);
        return new(this, EditRevision, UnusedScriptFingerprint(), kind, index, ScenarioAlias.Read(location.File.Data.Span.Slice(location.Offset, 64)));
    }
    public void ApplyScenarioNameDraft(ScenarioNameDraft draft, string name)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (transactionOpen || !ReferenceEquals(draft.Owner, this) || draft.Revision != EditRevision
            || !UnusedScriptFingerprint().AsSpan().SequenceEqual(draft.Fingerprint))
            throw new InvalidOperationException("Refresh the scenario-name draft after document changes.");
        SetScenarioName(draft.Kind, draft.Index, name);
    }
    private static long ScenarioParameter(NativeRecord record, int number)
    {
        var name = record.Category is "Project.TRIGGER" or "Project.ACTION" ? "param" + number : "Param" + number;
        var field = record.Fields.FirstOrDefault(field => field.Name == name);
        return field is null ? long.MinValue : long.Parse(record.Read(field), CultureInfo.InvariantCulture);
    }
    public ScenarioNameTable ReadScenarioNames()
    {
        if (!CanEditScenarioNames) return new(false, false, Array.AsReadOnly(Array.Empty<ScenarioNameSlot>()));
        var records = Records(); var variables = new int[129]; var timers = new int[17]; var verified = true;
        void Count(int[] counts, long index) { if (index >= 0 && index < counts.Length) counts[(int)index]++; }
        foreach (var record in records)
        {
            if (record.File.Signature == 0xD2 && !ScriptGraph.IsEvent(record)) verified = false;
            if (!ScriptGraph.IsEvent(record)) continue;
            // Count physical records, not the ID-indexed graph: duplicate IDs
            // do not erase source usage occurrences. All unlinked entries count.
            var type = EntityReferences.EventType(record);
            if (type is null) { verified = false; continue; }
            switch (type.Name)
            {
                case "TRIGGER_VARIABLE":
                    Count(variables, ScenarioParameter(record, 2));
                    if (ScenarioParameter(record, 3) == 2) Count(variables, ScenarioParameter(record, 4));
                    break;
                case "ACTION_SETVARIABLE":
                    Count(variables, ScenarioParameter(record, 1));
                    if (ScenarioParameter(record, 2) == 0x0C) Count(variables, ScenarioParameter(record, 5));
                    break;
                case "TRIGGER_TIMER": Count(timers, ScenarioParameter(record, 2)); break;
                case "ACTION_SETTIMER": Count(timers, ScenarioParameter(record, 1)); break;
                // Original manager excludes ACTION_DISPLAYTIMER deliberately.
            }
        }
        var rows = new List<ScenarioNameSlot>(146);
        foreach (var kind in new[] { ScenarioNameKind.Variable, ScenarioNameKind.Timer })
            for (var index = 0; index < (kind == ScenarioNameKind.Variable ? 129 : 17); index++)
            {
                var location = ScenarioAliasLocation(kind, index);
                rows.Add(new(kind, index, ScenarioAlias.Read(location.File.Data.Span.Slice(location.Offset, 64)),
                    (kind == ScenarioNameKind.Variable ? variables : timers)[index]));
            }
        return new(true, verified, rows.AsReadOnly());
    }
    /// <summary>Original operand lists, retaining unknown numeric values in the
    /// caller. Inactive secondary operands have no named selector.</summary>
    public IReadOnlyList<ValueChoice> ScenarioNameChoices(NativeRecord record, NativeField field)
    {
        if (!ScriptGraph.IsEvent(record) || !CanEditScenarioNames) return [];
        var type = EntityReferences.EventType(record)?.Name;
        var parameter = field.Name.StartsWith("Param", StringComparison.Ordinal) ? field.Name[5..]
            : field.Name.StartsWith("param", StringComparison.Ordinal) ? field.Name[5..] : "";
        ScenarioNameKind kind; var count = 0;
        switch (type)
        {
            case "TRIGGER_VARIABLE" when parameter == "2" || parameter == "4" && ScenarioParameter(record, 3) == 2:
            case "ACTION_SETVARIABLE" when parameter == "1" || parameter == "5" && ScenarioParameter(record, 2) is 0x0C or 0x14 or 0x24:
                kind = ScenarioNameKind.Variable; count = 129; break;
            case "TRIGGER_TIMER" when parameter == "2":
            case "ACTION_DISPLAYTIMER" when parameter == "1": kind = ScenarioNameKind.Timer; count = 17; break;
            case "ACTION_SETTIMER" when parameter == "1": kind = ScenarioNameKind.Timer; count = 16; break;
            default: return [];
        }
        _ = Records();
        var choices = new List<ValueChoice>(count);
        for (var index = 0; index < count; index++)
        {
            var location = ScenarioAliasLocation(kind, index); var name = ScenarioAlias.Read(location.File.Data.Span.Slice(location.Offset, 64));
            var special = kind == ScenarioNameKind.Variable ? index == 128 : index == 16;
            var en = special ? kind == ScenarioNameKind.Variable ? "Score" : "Time limit" : name.Length == 0 ? index.ToString(CultureInfo.InvariantCulture) : name;
            var ru = special ? kind == ScenarioNameKind.Variable ? "Счёт" : "Лимит времени" : en;
            choices.Add(new(index.ToString(CultureInfo.InvariantCulture), en, ru));
        }
        return choices.AsReadOnly();
    }
    private void CopyScenarioNamesToNative(NativeMapDocument output)
    {
        var table = ReadScenarioNames();
        if (!table.Supported || output.IsLegacyProject) throw new InvalidOperationException("A verified editor alias table and native output are required.");
        // Copy raw slots, including padding/noncanonical imported names. No
        // per-slot Encode normalization and no changes to game event arguments.
        foreach (var row in table.Slots)
        {
            var source = ScenarioAliasLocation(row.Kind, row.Index); var target = output.ScenarioAliasLocation(row.Kind, row.Index);
            source.File.Data.Span.Slice(source.Offset, 64).CopyTo(target.File.Bytes.AsSpan(target.Offset, 64));
        }
    }
    private (string path, byte[] data)? ScenarioNamesExport()
    {
        if (IsLegacyProject) return null;
        var file = NativeScenarioNames;
        var stem = Path.GetFileNameWithoutExtension(MainFile.ExportPath);
        var path = MainFile.Signature == 0xDC && files.Any(candidate =>
            candidate.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && Path.GetFileName(candidate.ExportPath).Equals(stem + "Globals.kld", StringComparison.OrdinalIgnoreCase))
            ? Path.Combine("Data", "Editor", "EditorMetadata", Path.GetFileName(MainFile.ExportPath) + ScenarioNamesExtension)
            : file.ExportPath;
        return nativeScenarioNamesLoaded || file.Data.Span[12..].IndexOfAnyExcept((byte)0) >= 0 || !CanEditScenarioNames
            ? (path, file.Bytes) : null;
    }
}
