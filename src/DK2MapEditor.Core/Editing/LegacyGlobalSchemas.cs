using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

/// <summary>The native offsets are extracted from the original compiler's assignments.
/// Only explicit constant offsets are accepted; arbitrary C expressions are never evaluated.</summary>
public static class LegacyGlobalSchemas
{
    public sealed record Definition(uint Signature, string Group, int RecordSize, IReadOnlyList<NativeField> Fields)
    {
        public bool SupportsRecordSize(int size) => size == RecordSize || Signature == 0xAA && size == 5537;
    }
    private static readonly IReadOnlyList<Definition> definitions = Load();
    public static IReadOnlyList<Definition> All => definitions;
    public static Definition? Find(uint signature) => definitions.FirstOrDefault(definition => definition.Signature == signature);

    private static IReadOnlyList<Definition> Load()
    {
        using var stream = typeof(LegacyGlobalSchemas).Assembly.GetManifestResourceStream("Legacy.Compiler_Generate.c")!;
        using var reader = new StreamReader(stream);
        var text = Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
        (uint signature, string group, int size)[] layouts = [(0x6E, "Terrains", 552), (0x78, "Rooms", 1055), (0x8C, "Doors", 616), (0x82, "Traps", 579), (0x96, "Spells", 406), (0xAA, "Creatures", 5449), (0xA0, "CrSpells", 266)];
        var fields = layouts.ToDictionary(layout => layout.group, _ => new List<NativeField>());
        const string pointer = @"\*\(\(\s*(?<type>BYTE|WORD|LONG|DWORD|short)\s*\*\)\(\s*(?:Terrain|Room|Door|Trap|Spell|Creature|CrSpell)\s*\+\s*(?<offset>0x[0-9A-Fa-f]+)\s*\)\)";
        const string property = @"MapGlobalVariables\.(?<group>\w+)\[Offset\]\.(?<name>\w+(?:\[\d+\])?)";
        foreach (Match match in Regex.Matches(text, pointer + @"\s*=\s*(?:\([^)]+\))?\s*" + property + @"\s*;"))
            Add(match, false, null);
        foreach (Match match in Regex.Matches(text, @"if\s*\(\s*" + property + @"\s*\)\s*" + pointer + @"\s*\|=\s*(?:\([^)]+\))?\s*(?<mask>0x[0-9A-Fa-f]+)\s*;"))
            Add(match, true, match.Groups["mask"].Value);
        foreach (Match match in Regex.Matches(text, pointer + @"\s*\|=\s*(?:\([^)]+\))?\s*\(\s*\(" + property + @"\)\s*\?\s*(?<mask>0x[0-9A-Fa-f]+)\s*:\s*0x00\s*\)\s*;"))
            Add(match, true, match.Groups["mask"].Value);
        return layouts.Select(layout => new Definition(layout.signature, layout.group, layout.size, fields[layout.group].AsReadOnly())).ToArray();

        void Add(Match match, bool flag, string? mask)
        {
            var type = match.Groups["type"].Value;
            var kind = type switch { "BYTE" => NativeValueKind.Byte, "WORD" => NativeValueKind.UInt16, "short" => NativeValueKind.Int16, "DWORD" => NativeValueKind.UInt32, _ => NativeValueKind.Int32 };
            var size = type switch { "BYTE" => 1, "WORD" or "short" => 2, _ => 4 };
            var offset = int.Parse(match.Groups["offset"].Value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var group = match.Groups["group"].Value;
            var name = match.Groups["name"].Value;
            if (offset + size > layouts.Single(layout => layout.group == group).size) throw new InvalidDataException("Global property extends past its native record.");
            if (!fields[group].Any(field => field.Name == name))
                fields[group].Add(new(name, offset, size, kind, flag ? byte.Parse(mask![2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture) : (byte)0));
        }
    }
}
