using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private int nativeGateMutationDepth;

    // Import resolves the first physical gate; subsequent commands retain the
    // working link even while several gates share the same native coordinates.
    private void MutateNativeGate(NativeRecord gate, Action mutation, bool removing = false)
    {
        if (nativeGateMutationDepth != 0) { mutation(); return; }
        if (!NativeSourceGate(gate)) { mutation(); if (!removing) CompleteNativeGateInsertion(gate); return; }
        var old = NativeDerivedPosition(gate, true);
        var links = DecodeNativeGateLinks(NativeGateBindings); var address = NativeGateAddressOf(gate);
        var owned = links.Where(link => GateLinkOwns(link, address)).Select(link => (link.ActionFile, link.ActionOffset)).ToHashSet();
        var actions = NativeGateBindingRecords().Where(action => IsNativeDestroyAction(action) && owned.Contains((NativeBindingFile(action.File), action.Offset))).ToArray();
        nativeGateMutationDepth++;
        try
        {
            mutation();
            var current = removing || !NativeSourceGate(gate) ? (TilePoint?)null : NativeDerivedPosition(gate, true);
            if (current == old) return; // Equal acceptance preserves imported bytes.
            if (current is null && !removing)
                WriteNativeGateLinks(DecodeNativeGateLinks(nativeGateBindings!).Select(link => GateLinkOwns(link, address) ? link with { GateFile = -1, GateOrdinal = -1 } : link));
            var point = current is { } location ? new TilePoint(unchecked(location.X + 1), unchecked(location.Y + 1)) : new(1, 1);
            foreach (var action in actions)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(action.File.Bytes.AsSpan(action.Offset + 12, 2), unchecked((ushort)point.X));
                BinaryPrimitives.WriteUInt16LittleEndian(action.File.Bytes.AsSpan(action.Offset + 14, 2), unchecked((ushort)point.Y));
            }
            RefreshNativeGatePayloadStamps();
        }
        finally { nativeGateMutationDepth--; }
    }
}
