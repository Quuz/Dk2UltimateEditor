using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

/// <summary>Source PathAreas, TrImages and PvIcons identities; display only.</summary>
public static class EditorListImageResources
{
    public const int PatrolArea = 1501;
    public static int? ScenarioDestination(ScriptEdge edge) => edge.Scope == 0 ? ScenarioEvent(edge.Owner) : ScenarioScope(edge.Scope);
    public static int? ScenarioScope(uint scope) => scope switch
    {
        1 => 202, 2 or 16 => 203, 4 => 204, 8 => 205, 32 => 209,
        64 => 206, 128 => 207, 256 => 208, _ => null
    };
    public static int? ScenarioEvent(NativeRecord record)
    {
        if (record.File.Length < 8 || record.Offset < 0 || record.Length < 0 || record.Offset > record.File.Length - record.Length) return null;
        if (record.File.Signature == NativeMapDocument.LegacyEvents && record.File.UInt32(4) is 2 or 3)
        {
            if (record.Category == "Project.TRIGGER" && record.Length == LegacyProjectSchemas.Size("TRIGGER")) return 200;
            if (record.Category == "Project.ACTION" && record.Length == LegacyProjectSchemas.Size("ACTION")) return 201;
            return null;
        }
        if (record.File.Signature != 0xD2 || record.Length != 24 || record.Offset < 0 || record.Offset > record.File.Length - 24) return null;
        return BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[record.Offset..]) switch
        { 0xD5 => 200, 0xD6 => 201, _ => null };
    }
    public static int? ManagerProblem(bool locked, ObjectBatchWarning warnings, bool accepted)
    {
        if (accepted) return null;
        if (locked) return 800;
        // The source's second assignment takes precedence when both masks match.
        if ((warnings & (ObjectBatchWarning.OutOfMap | ObjectBatchWarning.Replacement)) != 0) return 801;
        if ((warnings & (ObjectBatchWarning.BadTerrain | ObjectBatchWarning.Enemy | ObjectBatchWarning.Fatal | ObjectBatchWarning.Incompatible)) != 0) return 802;
        return null;
    }
}
