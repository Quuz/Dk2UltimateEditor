using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record PlayerSettingsRow(uint ID, NativeRecord Record, string Name, long Gold, bool Computer, long? Health, long AI,
    bool Editable, bool ProfileEditable, bool Resettable);
public sealed record PlayerSettingsChange(uint ID, string Name, int Gold, bool Computer, int? Health);

public static class PlayerSettings
{
    public static IReadOnlyList<ValueChoice> AIProfiles { get; } =
    [new("0", "Master", "Мастер"), new("1", "Conqueror", "Завоеватель"), new("2", "Psychopath", "Психопат"),
     new("3", "Stalwart", "Непоколебимый"), new("4", "Wise old man", "Старый мудрец"), new("5", "Idiot", "Идиот"),
     new("6", "Jailer", "Тюремщик"), new("7", "Tough guy", "Крепкий орешек"), new("8", "Paranoid", "Параноик")];
    internal static string DefaultName(uint player) => player switch { 1 => "Good Player", 2 => "Neutral Player", >= 3 and <= 7 => "Keeper " + (player - 2), _ => throw new ArgumentOutOfRangeException(nameof(player)) };
    internal static byte[] Defaults(uint player)
    {
        if (player is < 1 or > 7) throw new ArgumentOutOfRangeException(nameof(player));
        var fields = LegacyProjectSchemas.Fields("MAPOPTIONS").ToDictionary(field => field.Name);
        var bytes = new byte[LegacyProjectSchemas.Size("MAPOPTIONS")]; // Source ZeroMemory, including CreaturesAvailable.
        void Set(string name, object value) => Encode(fields[name], Convert.ToString(value, CultureInfo.InvariantCulture)!).CopyTo(bytes, fields[name].Offset);
        foreach (var assignment in LegacyCompilerRules.DefaultPlayerAI) Set(assignment.Target, assignment.Source);
        Set("PlayerHealth", 100); Set("PlayerGold", 3000); Set("PlayerComputer", player <= 2 ? 1 : 0);
        Set("PlayerName", DefaultName(player));
        foreach (var coordinate in new[] { "PlayerCPUAreaX1", "PlayerCPUAreaY1", "PlayerCPUAreaX2", "PlayerCPUAreaY2", "PlayerCameraX", "PlayerCameraY" }) Set(coordinate, -1);
        for (var index = 0; index < 45; index++) Set($"CreaturesCount[{index}]", player <= 2 ? -1 : index is 13 or 14 ? 20 : index is >= 1 and <= 12 && index is not (6 or 11) ? 32 : -1);
        if (player >= 3)
            foreach (var (array, count, enabled) in new[] { ("Rooms", 15, 15), ("Traps", 14, 11), ("Doors", 8, 6), ("Spells", 12, 12) })
                for (var index = 0; index < count; index++) Set($"{array}[{index}]", index >= enabled ? -1 : array == "Spells" && index is >= 2 and <= 10 ? 2 : 3);
        return bytes;
    }
    private static byte[] Encode(NativeField field, string value) => field.Encode(field.Kind == NativeValueKind.Byte && value == "-1" ? "255" : value);
}

public sealed partial class NativeMapDocument
{
    private NativeRecord? PlayerHeart(uint id) => Records().LastOrDefault(record => record.Category == "GAMEGATE" && record.File.UInt32(4) == 4
        && RuleNumber(record, "bGateType") == 14 && RuleNumber(record, "bGlobalPlayer") == id);
    public IReadOnlyList<PlayerSettingsRow> PlayerSettingsRows()
    {
        var allRecords = Records();
        var playerRecords = allRecords.Where(record => IsLegacyProject ? record.File.Signature == LegacyPlayers && record.Category != "Project.Opaque" : record.Category == "GAMEPLAYER").ToArray();
        var hearts = allRecords.Where(record => record.Category == "GAMEGATE" && record.File.UInt32(4) == 4 && RuleNumber(record, "bGateType") == 14)
            .GroupBy(record => RuleNumber(record, "bGlobalPlayer")).ToDictionary(group => group.Key, group => group.Last());
        var ids = playerRecords.Select((record, index) => IsLegacyProject ? (uint)index + 1 : RuleNumber(record, "bGlobalPlayer")).ToArray();
        return playerRecords.Select((record, index) =>
        {
            var id = ids[index]; var fields = record.Fields.ToDictionary(field => field.Name);
            string Read(string project, string native, string fallback = "0")
            {
                if (record.Category == "Project.MAPOPTIONSV1") project = project switch { "PlayerGold" => "Gold", "PlayerHealth" => "Health", "PlayerComputer" => "ComputerPlayer", "PlayerAI" => "AI", _ => project };
                return fields.TryGetValue(IsLegacyProject ? project : native, out var field)
                    ? !IsLegacyProject && native == "szPlayerName" ? (field with { Kind = NativeValueKind.WesternAnsi }).Read(record.File.Data.Span.Slice(record.Offset, record.Length)) : record.Read(field) : fallback;
            }
            var valid = id is >= 1 and <= 7 && ids.Count(value => value == id) == 1;
            var editable = valid && (IsLegacyProject ? record.Category is "Project.MAPOPTIONS" or "Project.MAPOPTIONSV2" : record.Length == 205 && record.File.UInt32(4) == 4);
            var profileEditable = editable && LegacyCompilerRules.DefaultPlayerAI.All(assignment => IsLegacyProject ? fields.ContainsKey(assignment.Target)
                : assignment.Target == "PlayerUseCountInFirstAttack" || LegacyCompilerRules.Players.Any(mapping => mapping.Source == assignment.Target && fields.ContainsKey(mapping.Target)));
            var heart = IsLegacyProject ? null : hearts.GetValueOrDefault(id);
            long? health = IsLegacyProject ? fields.ContainsKey(record.Category == "Project.MAPOPTIONSV1" ? "Health" : "PlayerHealth") ? long.Parse(Read("PlayerHealth", ""), CultureInfo.InvariantCulture) : null
                : heart is null ? null : RuleNumber(heart, "wGateHealth");
            return new PlayerSettingsRow(id, record, Read("PlayerName", "szPlayerName", ""), long.Parse(Read("PlayerGold", "bPlayerGold"), CultureInfo.InvariantCulture),
                Read("PlayerComputer", "bPlayerCPU") != "0", health, long.Parse(Read("PlayerAI", "bPlayerAI"), CultureInfo.InvariantCulture), editable, profileEditable,
                valid && (IsLegacyProject ? record.Category == "Project.MAPOPTIONS" : editable && MapVariableFile is { } variables && variables.UInt32(4) == 4));
        }).ToArray();
    }
    private PlayerSettingsRow[] SelectedPlayers(IEnumerable<uint> ids, Func<PlayerSettingsRow, bool> supported)
    {
        var selected = ids.ToArray(); var rows = PlayerSettingsRows();
        if (selected.Length == 0 || selected.Distinct().Count() != selected.Length) throw new ArgumentException("Select each player once.");
        return selected.Select(id =>
        {
            var matches = rows.Where(row => row.ID == id).ToArray();
            if (matches.Length != 1 || !supported(matches[0])) throw new ArgumentException("Select a verified unambiguous player (upgrade missing project fields first).");
            return matches[0];
        }).ToArray();
    }
    private void WritePlayer(NativeRecord record, string name, string value)
    {
        var field = record.Fields.Single(field => field.Name == name);
        // Native names use the original CP1252 encoding, not lossy ASCII.
        var encoded = (name == "szPlayerName" ? field with { Kind = NativeValueKind.WesternAnsi } : field).Encode(value);
        encoded.CopyTo(record.File.Bytes, record.Offset + field.Offset);
    }
    public void SetPlayerSettings(IEnumerable<PlayerSettingsChange> changes)
    {
        var (selected,rows)=ValidatePlayerSettingsChanges(changes);
        Edit("Change player name, resources and control", () =>
        {
            foreach (var (change, row) in selected.Zip(rows))
            {
                WritePlayer(row.Record, IsLegacyProject ? "PlayerName" : "szPlayerName", change.Name);
                WritePlayer(row.Record, IsLegacyProject ? "PlayerGold" : "bPlayerGold", change.Gold.ToString(CultureInfo.InvariantCulture));
                WritePlayer(row.Record, IsLegacyProject ? "PlayerComputer" : "bPlayerCPU", change.Computer ? "1" : "0");
                if (change.Health is { } health)
                {
                    if (IsLegacyProject) WritePlayer(row.Record, "PlayerHealth", health.ToString(CultureInfo.InvariantCulture));
                    else { var heart = PlayerHeart(row.ID)!; WritePlayer(heart, "wGateHealth", health.ToString(CultureInfo.InvariantCulture)); }
                }
            }
        });
    }
    private (PlayerSettingsChange[] Changes,PlayerSettingsRow[] Rows) ValidatePlayerSettingsChanges(IEnumerable<PlayerSettingsChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var selected = changes.ToArray(); var rows = SelectedPlayers(selected.Select(change => change.ID), row => row.Editable);
        foreach (var (change, row) in selected.Zip(rows))
        {
            if (change.Gold is < 0 or > 999999 || change.Health is < 0 or > 100 || change.Health is not null && row.Health is null) throw new ArgumentOutOfRangeException(nameof(changes));
            var name = row.Record.Fields.Single(field => field.Name == (IsLegacyProject ? "PlayerName" : "szPlayerName"));
            (IsLegacyProject ? name : name with { Kind = NativeValueKind.WesternAnsi }).Encode(change.Name);
        }
        return (selected,rows);
    }
    public void SetPlayerAIType(IEnumerable<uint> ids, int profile)
    {
        _ = LegacyCompilerRules.PlayerAIProfile(profile); var rows = SelectedPlayers(ids, row => row.ProfileEditable);
        Edit("Change player AI type only", () => { foreach (var row in rows) WritePlayer(row.Record, IsLegacyProject ? "PlayerAI" : "bPlayerAI", profile.ToString(CultureInfo.InvariantCulture)); });
    }
    public void ApplyPlayerAIProfile(IEnumerable<uint> ids, int profile)
    {
        var assignments = LegacyCompilerRules.PlayerAIProfile(profile); var rows = SelectedPlayers(ids, row => row.ProfileEditable);
        Edit("Apply original AI profile defaults", () =>
        {
            foreach (var row in rows) foreach (var assignment in assignments)
            {
                if (IsLegacyProject) WritePlayer(row.Record, assignment.Target, assignment.Source);
                else foreach (var mapping in LegacyCompilerRules.Players.Where(mapping => mapping.Source == assignment.Target))
                    EncodeCompilerValue(row.Record.Fields.Single(field => field.Name == mapping.Target), assignment.Source).CopyTo(row.Record.File.Bytes, row.Record.Offset + row.Record.Fields.Single(field => field.Name == mapping.Target).Offset);
            }
        });
    }
    public void ResetPlayerSettings(IEnumerable<uint> ids)
    {
        var rows = SelectedPlayers(ids, row => row.Resettable); var defaults = rows.ToDictionary(row => row.ID, row => PlayerSettings.Defaults(row.ID));
        var sourceFields = LegacyProjectSchemas.Fields("MAPOPTIONS").ToDictionary(field => field.Name);
        Edit("Restore original player defaults", () =>
        {
            foreach (var row in rows)
            {
                var bytes = defaults[row.ID];
                if (IsLegacyProject)
                    foreach (var field in row.Record.Fields) WritePlayer(row.Record, field.Name, sourceFields[field.Name].Read(bytes));
                else
                {
                    foreach (var mapping in LegacyCompilerRules.Players)
                    {
                        var field = row.Record.Fields.Single(field => field.Name == mapping.Target);
                        EncodeCompilerValue(field, sourceFields[mapping.Source].Read(bytes)).CopyTo(row.Record.File.Bytes, row.Record.Offset + field.Offset);
                    }
                    WritePlayer(row.Record, "szPlayerName", sourceFields["PlayerName"].Read(bytes));
                    var camera = row.ID <= 2 ? new TilePoint(0, 0) : HeartPosition((byte)row.ID);
                    WritePlayer(row.Record, "bPlayerCameraX", camera.X.ToString(CultureInfo.InvariantCulture)); WritePlayer(row.Record, "bPlayerCameraY", camera.Y.ToString(CultureInfo.InvariantCulture));
                    foreach (var (field, value) in new[] { ("bPlayerBuildAreaX1", 2), ("bPlayerBuildAreaY1", 2), ("bPlayerBuildAreaX2", Width - 2), ("bPlayerBuildAreaY2", Height - 2) }) WritePlayer(row.Record, field, value.ToString(CultureInfo.InvariantCulture));
                    if (PlayerHeart(row.ID) is { } heart) WritePlayer(heart, "wGateHealth", "100");
                    foreach (var kind in new[] { MapRuleKind.Rooms, MapRuleKind.Traps, MapRuleKind.Doors, MapRuleKind.Spells, MapRuleKind.CreaturePool })
                        foreach (var element in MapRules.Elements(kind))
                        {
                            var field = sourceFields[kind == MapRuleKind.CreaturePool ? $"CreaturesCount[{element.Index}]" : $"{kind}[{element.Index}]"];
                            var value = long.Parse(field.Read(bytes), CultureInfo.InvariantCulture);
                            WriteNativeMapRule(new(new(kind, row.ID, element.ID), value == (kind == MapRuleKind.CreaturePool ? -1 : 255) ? null : value));
                        }
                }
            }
        });
    }
}
