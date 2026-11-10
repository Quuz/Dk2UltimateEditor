using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

/// <summary>Uses the original primitive declarations to avoid duplicating hundreds of offsets.
/// Borland's 32-bit ABI is explicitly modelled. Headers, padding and unknown fields are excluded.</summary>
public static class LegacySchemas
{
    private static readonly Dictionary<string, Layout> Layouts = Load();
    public static IReadOnlyList<NativeField> Fields(string structure) => Layouts[structure].Fields;
    public static int Size(string structure) => Layouts[structure].Size;
    public static int Offset(string structure, string name) => Layouts[structure].Offsets[name];
    public static NativeField Field(string structure, string name) => Fields(structure).Single(field => field.Name == name);

    private static Dictionary<string, Layout> Load()
    {
        using var stream = typeof(LegacySchemas).Assembly.GetManifestResourceStream("Legacy.Game.h")!;
        using var reader = new StreamReader(stream);
        var text = Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
        var layouts = new Dictionary<string, Layout>(StringComparer.Ordinal);
        foreach (Match structure in Regex.Matches(text, @"typedef\s+struct\s+(\w+)\s*\{([^}]+)\}\s*\w+\s*;"))
        {
            var offset = 0;
            var alignment = 1;
            var fields = new List<NativeField>();
            var offsets = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Match declaration in Regex.Matches(structure.Groups[2].Value, @"(\w+)\s+(\w+)(?:\[([\d*]+)\])?\s*;"))
            {
                var type = declaration.Groups[1].Value;
                var name = declaration.Groups[2].Value;
                var count = declaration.Groups[3].Success
                    ? declaration.Groups[3].Value.Split('*').Select(int.Parse).Aggregate(1, (left, right) => left * right) : 1;
                var (size, align, kind) = type switch
                {
                    "BYTE" or "char" => (1, 1, NativeValueKind.Byte),
                    "WORD" or "wchar_t" => (2, 2, NativeValueKind.UInt16),
                    "DWORD" => (4, 4, NativeValueKind.UInt32),
                    _ when layouts.TryGetValue(type, out var nested) => (nested.Size, nested.Alignment, NativeValueKind.Byte),
                    _ => throw new InvalidDataException($"Unsupported legacy declaration: {type} {name}")
                };
                offset = (offset + align - 1) / align * align;
                alignment = Math.Max(alignment, align);
                var bytes = size * count;
                offsets.Add(name, offset);
                if (IsEditable(name) && !layouts.ContainsKey(type))
                {
                    kind = type switch
                    {
                        "wchar_t" => NativeValueKind.Unicode,
                        "char" when name is "szPlayerName" or "szMainSpeech" => NativeValueKind.WesternAnsi,
                        "char" => NativeValueKind.Ascii,
                        "BYTE" when count == 2 => NativeValueKind.UInt16,
                        "BYTE" when count == 4 => NativeValueKind.Int32,
                        _ => kind
                    };
                    if (count == 1 || type is "char" or "wchar_t" || count is 2 or 4)
                        fields.Add(new(name, offset, bytes, kind));
                }
                offset += bytes;
            }
            layouts.Add(structure.Groups[1].Value, new((offset + alignment - 1) / alignment * alignment, alignment, fields, offsets));
        }
        return layouts;
    }

    private static bool IsEditable(string name) =>
        !Regex.IsMatch(name, @"Unknown|Unused|Pad\d|Header|Version|FileSize|DefinesSize|DefineSize|ParamsSize|^szFile|^dwBandSize");

    private sealed record Layout(int Size, int Alignment, IReadOnlyList<NativeField> Fields, IReadOnlyDictionary<string, int> Offsets);
}
