using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

/// <summary>Original editor's 32-bit structs, including its older on-disk variants.
/// Pointer values and padding are preserved, never dereferenced or offered for editing.</summary>
public static class LegacyProjectSchemas
{
    private sealed record Layout(int Size, int Alignment, IReadOnlyList<NativeField> Fields);
    private static readonly Dictionary<string, Layout> layouts = Load();
    public static int Size(string name) => layouts[name].Size;
    public static IReadOnlyList<NativeField> Fields(string name) => layouts[name].Fields;

    private static Dictionary<string, Layout> Load()
    {
        var headers = new[] { "Global", "Map", "Things", "Bands", "RectEx", "Triggers", "GlobalVariables", "Compatibility" };
        var text = string.Join("\n", headers.Select(name =>
        {
            using var stream = typeof(LegacyProjectSchemas).Assembly.GetManifestResourceStream("Legacy." + name + ".h")!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }));
        text = Regex.Replace(text, @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
        // Each serialized MAPTHING union contains only 32-bit members. The raw value
        // is shared by gold, level, shots, status and spell, depending on node.type.
        text = Regex.Replace(text, @"union\s*\{[^{}]*\}\s*data\s*;", "LONG dataAll;");
        var constants = Regex.Matches(text, @"#define\s+(\w+)\s+(\d+)\b").ToDictionary(match => match.Groups[1].Value, match => int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), StringComparer.Ordinal);
        var bodies = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(text, @"typedef\s+struct\s+\w+\s*\{([^{}]*)\}\s*([\w,]+)\s*;"))
            foreach (var alias in match.Groups[2].Value.Split(',')) bodies[alias] = match.Groups[1].Value;
        var result = new Dictionary<string, Layout>(StringComparer.Ordinal);
        Layout Build(string name)
        {
            if (result.TryGetValue(name, out var existing)) return existing;
            var fields = new List<NativeField>(); var offset = 0; var maximumAlignment = 1;
            foreach (var declaration in bodies[name].Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var match = Regex.Match(declaration, @"^(unsigned\s+char|unsigned\s+long|struct\s+NODE|\w+)\s+(\*\s*)?(\w+)((?:\s*\[[^\]]+\])*)$");
                if (!match.Success) throw new InvalidDataException($"Unmodelled legacy declaration in {name}: {declaration}");
                var type = match.Groups[1].Value; var member = match.Groups[3].Value; var pointer = match.Groups[2].Success;
                var dimensions = Regex.Matches(match.Groups[4].Value, @"\[([^\]]+)\]").Select(dimension => dimension.Groups[1].Value.Split('+').Sum(term => int.TryParse(term.Trim(), out var value) ? value : constants[term.Trim()])).ToArray();
                var count = dimensions.Aggregate(1, (left, right) => checked(left * right));
                if (count > 100_000) throw new InvalidDataException("Legacy array is too large.");
                var primitive = type switch
                {
                    "BYTE" or "char" or "unsigned char" => (1, NativeValueKind.Byte),
                    "WORD" => (2, NativeValueKind.UInt16),
                    "short" => (2, member is "id" or "or" or "and" or "next" or "trigger" ? NativeValueKind.UInt16 : NativeValueKind.Int16),
                    "DWORD" or "UINT" or "ULONG" or "unsigned long" => (4, NativeValueKind.UInt32),
                    "LONG" or "long" or "BOOL" => (4, NativeValueKind.Int32),
                    _ => (0, NativeValueKind.Byte)
                };
                var nested = !pointer && primitive.Item1 == 0 ? Build(type) : null;
                var size = pointer ? 4 : nested?.Size ?? primitive.Item1;
                var alignment = pointer ? 4 : nested?.Alignment ?? size;
                offset = Align(offset, alignment); maximumAlignment = Math.Max(maximumAlignment, alignment);
                var skip = pointer || member.StartsWith("pad", StringComparison.OrdinalIgnoreCase) || member.Equals("unused", StringComparison.OrdinalIgnoreCase) || member is "ext" or "previd" or "expanded";
                if (!skip)
                {
                    if ((type == "char" || member == "SpeechFile") && dimensions.Length == 1)
                        fields.Add(new(member, offset, count, NativeValueKind.WesternAnsi));
                    else
                        for (var index = 0; index < count; index++)
                        {
                            var label = member;
                            var remainder = index;
                            foreach (var (dimension, position) in dimensions.Select((value, position) => (value, position)))
                            {
                                var stride = dimensions.Skip(position + 1).Aggregate(1, (left, right) => left * right);
                                label += "[" + remainder / stride + "]"; remainder %= stride;
                            }
                            if (nested is null) fields.Add(new(label, offset + index * size, size, primitive.Item2));
                            else foreach (var field in nested.Fields) fields.Add(field with { Name = label + "." + field.Name, Offset = offset + index * size + field.Offset });
                        }
                }
                offset += size * count;
            }
            return result[name] = new(Align(offset, maximumAlignment), maximumAlignment, fields);
        }
        foreach (var name in new[] { "NODE", "MAPPROPERTIES", "MAPOPTIONS", "MAPTHING", "MAPRECTEX", "MTHBAND", "TRIGGER", "ACTION", "TRIGGERLINK", "GLOBALTERRAIN", "GLOBALROOM", "GLOBALDOOR", "GLOBALTRAP", "GLOBALSPELL", "GLOBALCREATURE", "GLOBALCRSPELLS", "MAPTHINGV1", "MAPTHINGV2", "MAPTHINGV4", "MAPTHINGV5", "MTHBANDV1", "MTHBANDV2", "MTHBANDV3", "MTHBANDV4", "MAPPROPERTIESV1", "MAPPROPERTIESV2", "MAPPROPERTIESV4", "MAPPROPERTIESV5", "MAPPROPERTIESV6", "MAPOPTIONSV1", "MAPOPTIONSV2", "TRIGGERLINKV2", "MAPACTIONPOINTV1", "MAPTRIGGERV1", "MAPACTIONV1" }) Build(name);
        return result;
    }
    private static int Align(int value, int alignment) => (value + alignment - 1) / alignment * alignment;
}
