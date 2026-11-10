using System.Buffers.Binary;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public static class LegacyEventSchemas
{
    public sealed record EventType(uint RecordSignature, byte Type, string Name, IReadOnlyList<NativeField> Arguments, byte[] DefaultPayload);
    private static readonly IReadOnlyList<EventType> eventTypes = Load();
    public static IReadOnlyList<EventType> All => eventTypes;
    public static EventType? Find(uint signature, byte type) => eventTypes.FirstOrDefault(item => item.RecordSignature == signature && item.Type == type);
    private static readonly IReadOnlyDictionary<string, uint> scopes = ReadScopes();
    private static readonly IReadOnlyDictionary<string, int> parameterPages = ReadParameterPages();
    public static uint AllowedScopes(EventType type) => scopes[type.Name];
    public static int ParameterPageID(string name) => parameterPages.TryGetValue(name, out var page) ? page : -1;
    private static IReadOnlyDictionary<string, int> ReadParameterPages()
    {
        var result = Regex.Matches(Read("Legacy.Datas_Triggers.c"), @"\{\s*((?:TRIGGER|ACTION)_\w+)\s*,\s*\w+\s*,\s*(-?\d+)\s*,\s*[^{}]+\}")
            .ToDictionary(match => match.Groups[1].Value, match => int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        if (result.Count != eventTypes.Count || eventTypes.Any(type => !result.ContainsKey(type.Name))
            || result.Count(item => item.Value != -1) != 69)
            throw new InvalidDataException("Original scenario parameter-page catalogue is incomplete.");
        return result;
    }
    private static IReadOnlyDictionary<string, uint> ReadScopes()
    {
        var header = Read("Legacy.Triggers.h");
        var constants = Regex.Matches(header, @"#define\s+(SCOPE_\w+)\s+0x([0-9A-Fa-f]+)")
            .ToDictionary(match => match.Groups[1].Value, match => uint.Parse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        var result = Regex.Matches(Read("Legacy.Datas_Triggers.c"), @"\{\s*((?:TRIGGER|ACTION)_\w+)\s*,\s*\w+\s*,\s*-?\d+\s*,\s*([^{}]+)\}")
            .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value.Split('|', StringSplitOptions.TrimEntries).Aggregate(0u, (value, name) => value | constants[name]));
        if (eventTypes.Any(type => !result.ContainsKey(type.Name))) throw new InvalidDataException("Original scenario scope catalogue is incomplete.");
        return result;
    }
    public static IReadOnlyList<NativeField> Fields(uint signature, byte type)
    {
        var arguments = Find(signature, type)?.Arguments ?? [new("Argument1", 8, 4, NativeValueKind.Int32), new("Argument2", 12, 4, NativeValueKind.Int32)];
        return [.. arguments, new("ID", 16, 2, NativeValueKind.UInt16), new("NextOr", 18, 2, NativeValueKind.UInt16), new("ChildAnd", 20, 2, NativeValueKind.UInt16), new("Type", 22, 1, NativeValueKind.Byte), new("Repeat", 23, 1, NativeValueKind.Byte)];
    }

    private static IReadOnlyList<EventType> Load()
    {
        var header = Read("Legacy.Triggers.h");
        var compiler = Read("Legacy.Compiler_Generate.c");
        var catalogue = Read("Legacy.Datas_Triggers.c");
        var constants = Regex.Matches(header, @"#define\s+((?:TRIGGER|ACTION)_\w+)\s+0x([0-9A-Fa-f]+)")
            .Cast<Match>().Where(match => Convert.ToUInt32(match.Groups[2].Value, 16) <= 255)
            .ToDictionary(match => match.Groups[1].Value, match => Convert.ToByte(match.Groups[2].Value, 16));
        var supported = Regex.Matches(catalogue, @"\{((?:TRIGGER|ACTION)_\w+),").Cast<Match>().Select(match => match.Groups[1].Value).ToHashSet();
        var result = new List<EventType>();
        foreach (var (prefix, variable, pointer, signature) in new[] { ("TRIGGER", "Trigger", "TriggerPtr", 0xD5u), ("ACTION", "Action", "ActionPtr", 0xD6u) })
        {
            var start = compiler.IndexOf("int Compiler_Generate" + (signature == 0xD5 ? "Triggers" : "Actions") + "(", StringComparison.Ordinal);
            var end = compiler.IndexOf("WriteFile(FileHandle,", start, StringComparison.Ordinal);
            var body = compiler[start..end];
            var layouts = new Dictionary<string, (IReadOnlyList<NativeField> fields, byte[] defaults)>();
            foreach (Match branch in Regex.Matches(body, @"(?<cases>(?:case\s+" + prefix + @"_\w+\s*:\s*)+)(?<body>[\s\S]*?)\bbreak\s*;"))
            {
                var fields = new List<NativeField>();
                var defaults = new byte[8];
                var offset = 0;
                foreach (Match write in Regex.Matches(branch.Groups["body"].Value, @"\*\(\((?<type>BYTE|WORD|LONG)\s*\*\)" + pointer + @"\)\+\+\s*=\s*(?<value>[^;]+);"))
                {
                    var type = write.Groups["type"].Value;
                    var size = type switch { "BYTE" => 1, "WORD" => 2, _ => 4 };
                    var kind = type switch { "BYTE" => NativeValueKind.Byte, "WORD" => NativeValueKind.UInt16, _ => NativeValueKind.Int32 };
                    var expression = write.Groups["value"].Value;
                    var parameter = Regex.Match(expression, @"^\s*(?:\((?:BYTE|WORD|LONG)\))?\s*" + variable + @"->param(\d+)\s*$");
                    if (parameter.Success) fields.Add(new("Param" + parameter.Groups[1].Value, 8 + offset, size, kind));
                    else
                    {
                        var constant = Regex.Match(expression, @"^\s*(?:\((?:BYTE|WORD|LONG)\))?\s*(0x[0-9A-Fa-f]+|\d+)\s*$");
                        if (constant.Success)
                        {
                            var number = constant.Groups[1].Value;
                            var value = number.StartsWith("0x", StringComparison.Ordinal) ? uint.Parse(number[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture) : uint.Parse(number, CultureInfo.InvariantCulture);
                            if (size == 1) defaults[offset] = checked((byte)value);
                            else if (size == 2) BinaryPrimitives.WriteUInt16LittleEndian(defaults.AsSpan(offset), checked((ushort)value));
                            else BinaryPrimitives.WriteUInt32LittleEndian(defaults.AsSpan(offset), value);
                        }
                        else fields.Add(new("NativeArgument" + (offset + 1), 8 + offset, size, kind));
                    }
                    offset += size;
                }
                if (offset != 8) throw new InvalidDataException("Unexpected event argument layout in the legacy compiler.");
                foreach (Match name in Regex.Matches(branch.Groups["cases"].Value, prefix + @"_\w+")) layouts[name.Value] = (fields, defaults);
            }
            foreach (var name in supported.Where(name => name.StartsWith(prefix + "_", StringComparison.Ordinal)))
            {
                var layout = layouts.TryGetValue(name, out var found) ? found
                    : (new NativeField[] { new("Param1", 8, 4, NativeValueKind.Int32), new("Param5", 12, 4, NativeValueKind.Int32) }, new byte[8]);
                result.Add(new(signature, constants[name], name, layout.Item1, layout.Item2));
            }
        }
        return result.AsReadOnly();
    }
    private static string Read(string resource)
    {
        using var stream = typeof(LegacyEventSchemas).Assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        return Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
    }
}
