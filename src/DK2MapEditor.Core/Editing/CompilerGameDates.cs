using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private static void StampGeneratedGameDate(NativeFile file, int offset)
    {
        if (file.UInt32(4) != 4 || file.Signature is not (0xDC or 0xB4 or 0xBE or 0xD2 or 0xE6 or 0x6E or 0x78 or 0x8C or 0x82 or 0x96 or 0xAA or 0xA0)
            || offset != (file.Signature == 0xD2 ? 32 : 28) || offset + 20 > file.Length)
            throw new InvalidDataException("A verified native header is required for the compiler GAMEDATE layout.");
        // Compiler_SetDate uses GetLocalTime for each generated file. Its
        // zero-allocated GAMEDATE keeps the six padding bytes zero.
        var now = DateTime.Now; var bytes = file.Bytes.AsSpan(offset, 20); bytes.Clear();
        for (var part = 0; part <= 10; part += 10)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes[part..], checked((ushort)now.Year));
            bytes[part + 2] = (byte)now.Day; bytes[part + 3] = (byte)now.Month;
            bytes[part + 6] = (byte)now.Hour; bytes[part + 7] = (byte)now.Minute; bytes[part + 8] = (byte)now.Second;
        }
    }
    private static bool SourceGlobalDefinitionChanged(string family, IReadOnlyList<NativeRecord> records)
    {
        // Optional-array presence is not Properties_GlobalChanged. Compare
        // source project values to the exact reset defaults before native
        // BYTE/WORD narrowing; unchanged values keep the copied basis date.
        var rules = GlobalDefinitionDefaults.Rules(family);
        foreach (var index in LegacyCompilerRules.DefinitionOrder(family).Where(index => index >= 0).Distinct())
        {
            var record = records[index];
            foreach (var rule in rules)
            {
                var field = record.Fields.Single(item => item.Name == rule.Name);
                if (!field.Encode(record.Read(field)).AsSpan().SequenceEqual(field.Encode(GlobalDefinitionDefaults.ProjectDefault(rule, index)))) return true;
            }
        }
        return false;
    }
}
