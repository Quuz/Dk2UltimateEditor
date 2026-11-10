using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

/// <summary>Original protected output names. The display title is separate
/// from the filename used by this editor's game-bundle export.</summary>
public static class CompilerMapNames
{
    private static readonly IReadOnlySet<string> reserved = ReadReserved();
    public static bool IsReserved(string name) => reserved.Contains(name);
    public static string OutputStem(string sourcePath)
    {
        var name = Path.GetFileNameWithoutExtension(sourcePath);
        return string.IsNullOrWhiteSpace(name) || name.Length > 32
            || name.Any(character => character > 127 || Path.GetInvalidFileNameChars().Contains(character))
            ? "CompiledMap" : name;
    }
    private static IReadOnlySet<string> ReadReserved()
    {
        using var stream = typeof(CompilerMapNames).Assembly.GetManifestResourceStream("Legacy.Compiler_Compile.c")!;
        using var reader = new StreamReader(stream);
        var text = Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
        var body = Regex.Match(text, @"ReservedNames\[\]\s*=\s*\{([^}]+)\}").Groups[1].Value;
        var names = Regex.Matches(body, "\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToArray();
        if (names.Length != 63 || names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 63)
            throw new InvalidDataException("Unverified original protected map-name catalogue.");
        return names.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
