using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public sealed record RecordTemplate(string Structure, uint FileSignature, uint RecordSignature, int Size)
{
    public static IReadOnlyList<RecordTemplate> All { get; } =
    [
        new("GAMEOBJECT", 0xBE, 0xC2, 32), new("GAMETRAP", 0xBE, 0xC3, 24), new("GAMEDOOR", 0xBE, 0xC4, 28),
        new("GAMEAREA", 0xBE, 0xC5, 64), new("GAMENEUTRAL", 0xBE, 0xC6, 32), new("GAMEHERO", 0xBE, 0xC7, 40),
        new("GAMEKEEPER", 0xBE, 0xC8, 36), new("GAMEBAND", 0xBE, 0xC9, 563), new("GAMEEFFECT", 0xBE, 0xCB, 44),
        new("GAMEGATE", 0xBE, 0xCC, 28), new("GAMEPLAYER", 0xB4, 0, 205),
        new("GAMEVARCREATURECOUNT", 0xE6, 1, 16), new("GAMEVARTHINGSTATUS", 0xE6, 2, 16),
        new("GAMEVARALLIANCE", 0xE6, 0x42, 16), new("GAMEVARCREATURE", 0xE6, 0x4D, 16),
        new("GAMEGLOBALVARIABLE", 0xE6, 0x100, 16), new("Trigger", 0xD2, 0xD5, 24), new("Action", 0xD2, 0xD6, 24)
    ];

    internal byte[] Create(TilePoint position, byte owner, ushort id)
    {
        var bytes = new byte[Size];
        if (FileSignature != 0xB4) BinaryPrimitives.WriteUInt32LittleEndian(bytes, RecordSignature);
        if (FileSignature is 0xBE or 0xD2) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), (uint)Size - 8);
        if (Structure is "Trigger" or "Action")
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(16), id);
            bytes[23] = 1;
            bytes[22] = Structure == "Trigger" ? (byte)1 : (byte)0xA;
            if (Structure == "Trigger") { bytes[8] = 3; bytes[10] = 1; }
            return bytes;
        }
        foreach (var field in LegacySchemas.Fields(Structure))
        {
            string? value = field.Name switch
            {
                "bGlobalPlayer" => owner.ToString(),
                "bGlobalLevel" => "1",
                "dwGlobalHealth" or "wGateHealth" => "100",
                "bGlobalCreatureID" or "bObjectID" or "bTrapID" or "bDoorID" => "1",
                "bAreaID" or "bBandID" or "bEffectID" => id.ToString(),
                "bHeroBandID" => "255",
                "wEffectWidth" or "wEffectHeight" => "1",
                "bEffectFrequency" => "1",
                "bAreaFlags" => "16",
                _ when field.Name.EndsWith("LocationX", StringComparison.Ordinal) || field.Name.EndsWith("StartX", StringComparison.Ordinal) || field.Name.EndsWith("EndX", StringComparison.Ordinal) => position.X.ToString(),
                _ when field.Name.EndsWith("LocationY", StringComparison.Ordinal) || field.Name.EndsWith("StartY", StringComparison.Ordinal) || field.Name.EndsWith("EndY", StringComparison.Ordinal) => position.Y.ToString(),
                _ => null
            };
            if (value is not null) field.Encode(value).CopyTo(bytes, field.Offset);
        }
        if (Structure == "GAMEBAND")
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 9);
            // Sixteen fixed hero slots begin at byte 51, NOT sizeof(GAMEBAND).
            for (var index = 0; index < 16; index++) bytes[51 + index * 32 + 30] = (byte)id;
        }
        return bytes;
    }
}
