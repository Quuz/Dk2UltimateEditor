using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

/// <summary>Verified assignment and catalogue rules extracted from the preserved compiler.</summary>
public static class LegacyCompilerRules
{
    public sealed record Assignment(string Target, string Source);
    public sealed record Element(uint ID, uint Variable, int Icon, string Name, string? AsciiName = null,
        int MinWidth = 0, int MinHeight = 0, int MaxWidth = 0, int MaxHeight = 0);
    private static readonly ConcurrentDictionary<string, IReadOnlyList<Element>> elementCatalogues = new(StringComparer.Ordinal);
    public static IReadOnlyList<Assignment> Metadata { get; } = ReadMetadata();
    public static IReadOnlyList<Assignment> Players { get; } = ReadPlayers();
    public static IReadOnlyList<(int Offset, byte Value)> MetadataConstants { get; } = ReadMetadataConstants();
    private static IReadOnlyList<(int Offset, byte Value)> ReadMetadataConstants()
    {
        var text = Read("Compiler_Generate.c");
        text = text[..text.IndexOf("int Compiler_GeneratePlayersFile", StringComparison.Ordinal)];
        // Link-table bytes are generated separately, never copied as magic offsets.
        return Regex.Matches(text, @"WorkFile->(bMainUnused5|bMainUnknown3)\[(\d+)\]\s*=\s*(0x[0-9A-Fa-f]+|\d+)\s*;")
            .Select(match => (LegacySchemas.Offset("GAMEINFO", match.Groups[1].Value) + int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), checked((byte)Number(match.Groups[3].Value)))).ToArray();
    }
    private static IReadOnlyList<Assignment> ReadMetadata()
    {
        var text = Read("Compiler_Generate.c");
        text = text[..text.IndexOf("int Compiler_GeneratePlayersFile", StringComparison.Ordinal)];
        var result = Regex.Matches(text, @"WorkFile->(\w+)\s*=\s*(?:\(\w+\))?Map\.Properties\.(\w+)\s*;")
            .Select(match => new Assignment(match.Groups[1].Value, match.Groups[2].Value)).ToList();
        result.AddRange(Regex.Matches(text, @"PadFix\s*=\s*\(WORD\s*\*\)&WorkFile->(\w+)\[0\];\s*\*PadFix\s*=\s*Map\.Properties\.(\w+);").Select(match => new Assignment(match.Groups[1].Value, match.Groups[2].Value)));
        result.AddRange(new[] { "Name", "Description", "Author", "Email", "Information" }.Select(name => new Assignment("wstrMain" + name, name)));
        result.Add(new("szMainSpeech", "SpeechFile"));
        return result.AsReadOnly();
    }
    private static IReadOnlyList<Assignment> ReadPlayers()
    {
        var text = Read("Compiler_Generate.c");
        var start = text.IndexOf("int Compiler_GeneratePlayersFile", StringComparison.Ordinal);
        var end = text.IndexOf("int Compiler_GenerateThingsFile", start, StringComparison.Ordinal);
        return Regex.Matches(text[start..end], @"Player\)->(\w+)(?:\[0\])?\)\)\s*=\s*\(\w+\)MapOptions\[i\]\.(\w+(?:\[\d+\])?)\s*;")
            .Select(match => new Assignment(match.Groups[1].Value, match.Groups[2].Value)).ToArray();
    }
    public static IReadOnlyList<Element> Elements(string group) => elementCatalogues.GetOrAdd(group, ReadElements);
    private static IReadOnlyList<Element> ReadElements(string group)
    {
        var text = Read("Datas_Things.c") + Read("Datas_Map.c") + Read("Datas_Game.c");
        if (group is "TrCreatures" or "TrRooms" or "Terrain" or "Interface") text += Read("Datas_Triggers.c");
        if (group.StartsWith('G')) text += Read("Datas_GlobalVariables.c");
        if (group == "Effects") text += Read("Datas_Effects.c");
        var body = Regex.Match(text, @"ELEMENT\s+" + Regex.Escape(group) + @"\[\]\s*=\s*\{([\s\S]*?)\};");
        if (!body.Success) throw new ArgumentException("Unknown original element catalogue: " + group);
        var names = Regex.Matches(Read("Texts_ASCII.c"), "char\\s+(\\w+)\\[\\]\\s*=\\s*\"([^\"]*)\";")
            .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value);
        return Regex.Matches(body.Groups[1].Value, @"\{\s*(0x[0-9A-Fa-f]+|\d+)\s*,\s*(-?0x[0-9A-Fa-f]+|-?\d+)\s*,\s*(\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(sz\w+|NULL)\s*,\s*(\w+)\s*\}")
            .Select(match => new Element(unchecked((uint)Number(match.Groups[1].Value)), unchecked((uint)Number(match.Groups[2].Value)), int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture), match.Groups[8].Value, names.GetValueOrDefault(match.Groups[9].Value),
                int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture), int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture), int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture), int.Parse(match.Groups[7].Value, CultureInfo.InvariantCulture)))
            .Where(element => element.ID != 0).ToArray();
    }
    private static readonly ConcurrentDictionary<int, IReadOnlyList<Assignment>> playerProfiles = new();
    public static IReadOnlyList<Assignment> DefaultPlayerAI => PlayerAIProfile(0);
    public static IReadOnlyList<Assignment> PlayerAIProfile(int profile)
    {
        if (profile is < 0 or > 8) throw new ArgumentOutOfRangeException(nameof(profile));
        return playerProfiles.GetOrAdd(profile, ReadPlayerAIProfile);
    }
    private static IReadOnlyList<Assignment> ReadPlayerAIProfile(int profile)
    {
        var text = Read("Properties_Map_Global.c");
        var start = text.IndexOf("void Properties_MapOptionsKeeperAI(", StringComparison.Ordinal);
        start = text.IndexOf("case " + profile + ":", start, StringComparison.Ordinal);
        var end = text.IndexOf("break;", start, StringComparison.Ordinal);
        var assignments = Regex.Matches(text[start..end], @"MapOptions->(\w+(?:\[\d+\])?)\s*=\s*(TRUE|FALSE|0x[0-9A-Fa-f]+|-?\d+)\s*;")
            .Select(match => new Assignment(match.Groups[1].Value, match.Groups[2].Value switch { "TRUE" => "1", "FALSE" => "0", var value => Number(value).ToString(CultureInfo.InvariantCulture) })).ToArray();
        if (assignments.Length != 84 || assignments.Select(item => item.Target).Distinct().Count() != 84
            || assignments.Single(item => item.Target == "PlayerAI").Source != profile.ToString(CultureInfo.InvariantCulture))
            throw new InvalidDataException("Unverified original AI profile.");
        return Array.AsReadOnly(assignments);
    }
    // The embedded source is immutable. Do not reread and parse all seven
    // ordering tables on every definition-row/default lookup.
    private static readonly ConcurrentDictionary<string, IReadOnlyList<int>> definitionOrders = new(StringComparer.Ordinal);
    public static IReadOnlyList<int> DefinitionOrder(string group) => definitionOrders.GetOrAdd(group, ReadDefinitionOrder);
    private static IReadOnlyList<int> ReadDefinitionOrder(string group)
    {
        var name = group switch { "Terrains" => "GTOrder", "Rooms" => "GROrder", "Doors" => "GDOrder", "Traps" => "GAOrder", "Spells" => "GSOrder", "Creatures" => "GCOrder", "CrSpells" => "GOOrder", _ => throw new ArgumentException("Unknown global family.") };
        var body = Regex.Match(Read("Datas_GlobalDefault.c"), name + @"\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value;
        return Array.AsReadOnly(Regex.Matches(body, @"\(DWORD\s*\*\)\s*(0x[0-9A-Fa-f]+|-?\d+)\s*,\s*NULL").Select(match => (int)Number(match.Groups[1].Value)).TakeWhile(value => value != -1).ToArray());
    }
    private static long Number(string value) => value.StartsWith("0x", StringComparison.Ordinal) ? long.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture) : long.Parse(value, CultureInfo.InvariantCulture);
    private static string Read(string name)
    {
        using var stream = typeof(LegacyCompilerRules).Assembly.GetManifestResourceStream("Legacy." + name)!;
        using var reader = new StreamReader(stream);
        return Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
    }
}
