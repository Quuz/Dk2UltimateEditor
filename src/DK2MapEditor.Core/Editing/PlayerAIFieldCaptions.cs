namespace DK2MapEditor.Core.Editing;

/// <summary>Contextual captions for the raw player property grid, sharing the custom AI vocabulary.</summary>
public static class PlayerAIFieldCaptions
{
    public static ValueChoice? ForField(NativeRecord record, NativeField field)
    {
        if (field.BitMask != 0 || !record.Fields.Contains(field) || record.File.Length < 8
            || record.Offset < 0 || record.Length < 0 || record.Offset > record.File.Length - record.Length)
            return null;

        string? sourceName;
        if (record.Category == "GAMEPLAYER" && record.File.Signature == 0xB4
            && record.File.UInt32(4) == 4 && record.Length == LegacySchemas.Size("GAMEPLAYER")
            && LegacySchemas.Fields("GAMEPLAYER").Contains(field))
        {
            sourceName = LegacyCompilerRules.Players.FirstOrDefault(mapping => mapping.Target == field.Name)?.Source;
        }
        else if (record.File.Signature == NativeMapDocument.LegacyPlayers
            && (record.Category == "Project.MAPOPTIONS" && record.File.UInt32(4) == 3
                || record.Category == "Project.MAPOPTIONSV2" && record.File.UInt32(4) == 2)
            && record.Length == LegacyProjectSchemas.Size(record.Category[8..])
            && LegacyProjectSchemas.Fields(record.Category[8..]).Contains(field))
        {
            sourceName = field.Name;
        }
        else return null;

        var parameter = PlayerAIOptions.Parameters.FirstOrDefault(parameter => parameter.Name == sourceName);
        if (parameter is null) return null;
        // The typed AI checkbox exposes only bit 0x20. The raw grid still edits the whole byte.
        if (parameter.Name == "PlayerCreaturesSlap")
            return new(field.Name, "Creature slap flags (0x20: do not slap)", "Флаги пощёчин существам (0x20: не бить)");
        return new(field.Name, parameter.English, parameter.Russian);
    }
}
