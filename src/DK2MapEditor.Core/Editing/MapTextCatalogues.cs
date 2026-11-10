using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public sealed record MapTextTable(ushort Identifier, string English, string Russian, string? CurrentFile, string? BriefingFile)
{
    public string Name(string language) => language == "ru" ? Russian : English;
}
public sealed record MapSpeechResource(string Name, string FilePath, string TextFile);

/// <summary>Original metadata choice lists, deliberately separate from resource authoring.</summary>
public static class MapTextCatalogues
{
    public static IReadOnlyList<MapTextTable> Tables { get; } = ReadTables();
    public static IReadOnlyList<ValueChoice> IntroCreatures { get; } = Array.AsReadOnly(new[] { new ValueChoice("0", "None", "Нет") }
        .Concat(new[] { "BadCreatures", "BadElites", "Heros" }.SelectMany(group => ValueCatalogues.Elements(group))).ToArray());
    private static IReadOnlyList<MapTextTable> ReadTables()
    {
        using var stream = typeof(MapTextCatalogues).Assembly.GetManifestResourceStream("Legacy.Datas_Map.c")!;
        using var reader = new StreamReader(stream); var source = reader.ReadToEnd();
        var body = Regex.Match(source, @"TextFiles\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value;
        var rows = Regex.Matches(body, "\\(char \\*\\)(\\d+),\\s*\"[^\"]*\",\\s*(NULL|\"[^\"]*\"),\\s*(NULL|\"[^\"]*\")");
        var result = rows.Select(match => {
            var id = ushort.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            string? File(int group) => match.Groups[group].Value == "NULL" ? null : match.Groups[group].Value.Trim('"');
            var current = File(2); var briefing = File(3);
            var label = current switch {
                null => ("None", "Нет"),
                "MYPETDUNGEON.STR" => ($"My Pet Dungeon {(id == 26 ? 1 : id - 33)}", $"Мой питомник {(id == 26 ? 1 : id - 33)}"),
                "MULTIPLAYER.STR" => ($"Multiplayer {(id == 25 ? 1 : id - 39)}", $"Сетевая игра {(id == 25 ? 1 : id - 39)}"),
                _ when current.StartsWith("SECRET", StringComparison.Ordinal) => ($"Secret {current[6..^4]}", $"Секретный уровень {current[6..^4]}"),
                _ when current.StartsWith("DEMO", StringComparison.Ordinal) => ($"Demo {current[4..^4]}", $"Демо {current[4..^4]}"),
                _ => ($"Level {current[5..^4]}", $"Уровень {current[5..^4]}") };
            return new MapTextTable(id, label.Item1, label.Item2, current, briefing);
        }).ToArray();
        if (result.Length != 59 || result.Select(row => row.Identifier).Distinct().Count() != 59 || result[0].Identifier != 0)
            throw new InvalidDataException("Unverified original text table catalogue.");
        return Array.AsReadOnly(result);
    }

    public static string SpeechTextFile(string bank)
    {
        VerifySegment(bank); var separator = bank.IndexOf('_'); var suffix = separator >= 0 ? bank[(separator + 1)..] : bank;
        if (suffix.Length == 0) throw new ArgumentException("A speech bank suffix is required.", nameof(bank));
        return (suffix.Equals("MENTOR", StringComparison.OrdinalIgnoreCase) ? "Speech" : suffix) + ".STR";
    }
    public static IReadOnlyList<MapSpeechResource> SpeechBanks(string gameRoot)
    {
        var folder = Path.Combine(Path.GetFullPath(gameRoot), "Data", "Sound", "sfx");
        if (!Directory.Exists(folder)) return Array.Empty<MapSpeechResource>();
        return Array.AsReadOnly(Directory.EnumerateDirectories(folder).Where(directory => (File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0)
            .Where(directory => Path.GetFileName(directory).StartsWith("SPEECH_", StringComparison.OrdinalIgnoreCase))
            .Select(directory => {
                var name = Path.GetFileName(directory); VerifySegment(name);
                return new MapSpeechResource(name.ToUpperInvariant(), Path.Combine(directory, name + "HD.SDT"), SpeechTextFile(name));
            }).OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase).ToArray());
    }
    public static string TextResourcePath(string gameRoot, string filename)
    { VerifySegment(filename); return Path.Combine(Path.GetFullPath(gameRoot), "Data", "Text", "Default", filename); }
    private static void VerifySegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or ".." || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException("Select a single resource filename, not a path.", nameof(value));
    }
}
