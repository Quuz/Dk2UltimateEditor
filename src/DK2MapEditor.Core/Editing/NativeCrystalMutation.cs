using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private bool retainRawCrystalTargets;

    private TilePoint[] NativeCrystalOrder()
    {
        if (IsLegacyProject) return [];
        var records = new List<NativeRecord>();
        foreach (var file in files.Where(file => file.Signature == 0xBE)) AppendNativeCollectionRecords(file, records);
        return records.Where(NativeSourceCrystal).Select(record => NativeDerivedPosition(record, false)).ToArray();
    }

    // The original stores no independent ShowCrystal point: the writer selects
    // the first ID28 in MapObjects after all accepted object commands. Run once
    // at the outer transaction boundary, including callers that replace whole
    // prepared collections rather than invoking SetField/DeleteRecord.
    private void CompleteNativeCrystalMutation(TilePoint[] before)
    {
        if (IsLegacyProject || retainRawCrystalTargets) return;
        var after = NativeCrystalOrder();
        if (before.AsSpan().SequenceEqual(after)) return;
        var point = after.Length == 0 ? new TilePoint(-1, -1) : after[0];
        var actions = new List<NativeRecord>();
        foreach (var file in files.Where(file => file.Signature == 0xD2)) AppendNativeCollectionRecords(file, actions);
        foreach (var action in actions.Where(record => NativeDerivedAction(record) && record.File.Data.Span[record.Offset + 22] == 2))
        {
            BinaryPrimitives.WriteUInt16LittleEndian(action.File.Bytes.AsSpan(action.Offset + 12, 2), unchecked((ushort)point.X));
            BinaryPrimitives.WriteUInt16LittleEndian(action.File.Bytes.AsSpan(action.Offset + 14, 2), unchecked((ushort)point.Y));
        }
    }
}
