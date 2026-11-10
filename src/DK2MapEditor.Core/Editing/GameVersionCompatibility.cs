using System.Globalization;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

/// <summary>Original TestVersion notices, not hard compilation errors or
/// warning-counter increments. These describe source requirements, not a
/// guarantee that an arbitrary level plays correctly with the selected target.</summary>
public static class GameVersionCompatibility
{
    public static IReadOnlyList<MapDiagnostic> Validate(NativeMapDocument document, bool includeScripts = true)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.TargetGameVersion is not { } target || target >= 17) return Array.Empty<MapDiagnostic>();
        var records = document.Records();
        var things61 = false; var things70 = false;
        var conditions61 = false; var conditions70 = false;
        var actions61 = false; var actions70 = false;
        var objects = new List<(NativeRecord Record, int X, int Y)>();
        foreach (var record in records)
        {
            var thing = ReadThing(record);
            if (thing is { } placed)
            {
                if (placed.Kind == 0) { things61 |= placed.ID is >= 0x20 and <= 0x2C; things70 |= placed.ID == 0x2D; }
                if (placed.Kind == 2) things70 |= placed.ID == 0x10;
                if (placed.Kind == 3 && placed.ID is 0x8A or 0x8B or 0x8D or 0x8E or 0x8F or 0x90) objects.Add((record, placed.X, placed.Y));
            }
            if (!includeScripts || !ScriptGraph.IsEvent(record) || !VerifiedEvent(record) || EntityReferences.EventType(record) is not { } type) continue;
            if (type.RecordSignature == 0xD6 && type.Type == 0x01 && Parameter(record, 1) is { } creature)
            { actions61 |= creature is >= 0x20 and <= 0x2C; actions70 |= creature == 0x2D; }
            if (type.RecordSignature != 0xD5 || Parameter(record, 2) is not { } parameter) continue;
            if (type.Type is 0x27 or 0x14 or 0x16 or 0x34 or 0x49)
            { conditions61 |= parameter is >= 0x20 and <= 0x2C; conditions70 |= parameter == 0x2D; }
            if (type.Type == 0x13) conditions70 |= parameter == 0x2D;
            // Deliberate original quirk: the TRAPS condition sets the 1.61
            // flag, whereas a placed trap of the same ID sets the 1.70 flag.
            if (type.Type == 0x1C) conditions61 |= parameter == 0x10;
        }
        var result = new List<MapDiagnostic>();
        void Notice(bool present, string code, string message)
        { if (present) result.Add(new(DiagnosticSeverity.Information, code, message, document.MainFile.SourcePath)); }
        if (target < 16)
        {
            Notice(things61, "DK2V001", "Placed creatures require DK2 1.61.");
            Notice(conditions61, "DK2V002", "Scenario conditions require DK2 1.61.");
            Notice(actions61, "DK2V003", "Scenario actions require DK2 1.61.");
        }
        Notice(things70, "DK2V004", "Placed creatures or traps require DK2 1.70.");
        Notice(conditions70, "DK2V005", "Scenario conditions require DK2 1.70.");
        Notice(actions70, "DK2V006", "Scenario actions require DK2 1.70.");
        foreach (var item in objects)
            result.Add(new(DiagnosticSeverity.Information, "DK2V007", $"Placed object at ({item.X}, {item.Y}) requires DK2 1.70.", item.Record.File.SourcePath, new(item.X, item.Y, 1, 1)));
        return result.AsReadOnly();
    }
    private static bool VerifiedEvent(NativeRecord record) => record.File.Signature == NativeMapDocument.LegacyEvents
        ? record.File.UInt32(4) is 2 or 3
        : record.File.UInt32(4) == 4;
    private static long? Parameter(NativeRecord record, int number)
    {
        var project = record.File.Signature == NativeMapDocument.LegacyEvents;
        var field = record.Fields.SingleOrDefault(field => field.Name == (project ? "param" : "Param") + number);
        // LOSTCREATURES / CREATURESLEVEL do not serialize param2 in native
        // records. Files_Game skips those bytes and its zeroed allocation
        // leaves param2=0. Never interpret their reserved byte 9 as a species.
        return field is null || field.BitMask != 0 || field.Kind != (project ? NativeValueKind.Int32 : NativeValueKind.Byte)
            ? null : long.Parse(record.Read(field), CultureInfo.InvariantCulture);
    }
    private static (int Kind, long ID, int X, int Y)? ReadThing(NativeRecord record)
    {
        var project = record.File.Signature == NativeMapDocument.LegacyThings;
        if (project ? record.File.UInt32(4) is < 1 or > 6 || !record.Category.StartsWith("Project.MAPTHING", StringComparison.Ordinal)
            : record.File.Signature != 0xBE || record.File.UInt32(4) != 4 || RecordTemplate.All.FirstOrDefault(item => item.Structure == record.Category) is not { } template
                || template.Size != record.Length || record.Fields.Count != LegacySchemas.Fields(record.Category).Count) return null;
        long? Read(string name) => record.Fields.SingleOrDefault(field => field.Name == name && field.BitMask == 0) is { } field
            ? long.Parse(record.Read(field), CultureInfo.InvariantCulture) : null;
        var kind = project ? Read("node.type") : record.Category switch
        { "GAMEHERO" or "GAMENEUTRAL" or "GAMEKEEPER" => 0L, "GAMETRAP" => 2L, "GAMEOBJECT" => 3L, _ => null };
        if (kind is not (0 or 2 or 3)) return null;
        var id = Read(project ? "id" : kind == 0 ? "bGlobalCreatureID" : kind == 2 ? "bTrapID" : "bObjectID");
        var x = project ? Read("x") : record.Fields.FirstOrDefault(field => field.Name.EndsWith("LocationX", StringComparison.Ordinal)) is { } xf ? Read(xf.Name) : null;
        var y = project ? Read("y") : record.Fields.FirstOrDefault(field => field.Name.EndsWith("LocationY", StringComparison.Ordinal)) is { } yf ? Read(yf.Name) : null;
        return id is { } identifier && x is { } xx && y is { } yy ? ((int)kind.Value, identifier, checked((int)xx), checked((int)yy)) : null;
    }
}
