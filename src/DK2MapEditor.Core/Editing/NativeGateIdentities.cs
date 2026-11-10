using System.Buffers.Binary;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    internal int NativeGateID(NativeRecord gate)
    {
        var state = DecodeNativeGateState(NativeGateBindings); var address = NativeGateAddressOf(gate);
        return state.Identities.Single(identity => identity.File == address.File && identity.Ordinal == address.Ordinal).ID;
    }

    private int NativeGateTargetID(NativeRecord action)
        => !IsNativeDestroyAction(action) ? 0 : DecodeNativeGateLinks(NativeGateBindings)
            .Single(link => link.ActionFile == NativeBindingFile(action.File) && link.ActionOffset == action.Offset).GateID;

    private void SetNativeGateTargetID(NativeRecord action, int id)
    {
        var state = DecodeNativeGateState(NativeGateBindings); var owner = state.Identities.SingleOrDefault(identity => identity.ID == id);
        var file = NativeBindingFile(action.File); var point = NativeDerivedPayload(action);
        state.Links.RemoveAll(link => link.ActionFile == file && link.ActionOffset == action.Offset);
        state.Links.Add(new(file, action.Offset, owner?.File ?? -1, owner?.Ordinal ?? -1, (ushort)point.X, (ushort)point.Y, id));
        WriteNativeGateState(state.Links, state.Identities);
    }

    // Source MapGates IDs survive deletion in ACTION.param5. Reusing a freed
    // ID deliberately restores that action's owner, regardless of coordinates.
    // Reading/synchronizing metadata alone never rewrites native game payloads.
    private void CompleteNativeGateInsertion(NativeRecord gate)
    {
        if (compilingNativeGateIdentities || !NativeSourceGate(gate)) return;
        var id = NativeGateID(gate);
        if (id > 255) throw new InvalidOperationException("No unused gate IDs remain in the original 1–255 range.");
        var links = DecodeNativeGateLinks(NativeGateBindings);
        var actions = links.Where(link => link.GateID == id).Select(link => (link.ActionFile, link.ActionOffset)).ToHashSet();
        var point = NativeDerivedPosition(gate, true);
        foreach (var action in NativeGateBindingRecords().Where(action => IsNativeDestroyAction(action) && actions.Contains((NativeBindingFile(action.File), action.Offset))))
        {
            BinaryPrimitives.WriteUInt16LittleEndian(action.File.Bytes.AsSpan(action.Offset + 12), unchecked((ushort)(point.X + 1)));
            BinaryPrimitives.WriteUInt16LittleEndian(action.File.Bytes.AsSpan(action.Offset + 14), unchecked((ushort)(point.Y + 1)));
        }
        RefreshNativeGatePayloadStamps();
    }

    private void RestoreCompiledNativeGateIdentities(IReadOnlyList<NativeRecord> source)
    {
        static int Number(NativeRecord record, string field) => int.Parse(record.Read(record.Fields.Single(item => item.Name == field)), CultureInfo.InvariantCulture);
        var projectGates = source.Where(record => record.Category == "Project.MAPRECTEX" && Number(record, "node.type") == 1).ToArray();
        var gates = NativeGateAddresses(NativeGateBindingRecords()).Where(pair => NativeSourceGate(pair.Value)).ToArray();
        if (projectGates.Length != gates.Length) throw new InvalidDataException("Compiled gate identities do not match the verified project gates.");
        var identities = gates.Select((pair, index) => new NativeGateIdentity(pair.Key.File, pair.Key.Ordinal, Number(projectGates[index], "id"))).ToArray();
        var actions = source.Where(record => record.Category == "Project.ACTION" && Number(record, "ctrl") == 32).ToDictionary(record => Number(record, "id"));
        var links = DecodeNativeGateLinks(NativeGateBindings);
        WriteNativeGateState(links.Select(link =>
        {
            var file = files.Where(file => file.Signature == 0xD2).ElementAt(link.ActionFile);
            var eventID = BinaryPrimitives.ReadUInt16LittleEndian(file.Data.Span.Slice(link.ActionOffset + 16));
            var id = actions.TryGetValue(eventID, out var action) ? Number(action, "param5") : link.GateID;
            var owner = identities.SingleOrDefault(identity => identity.ID == id);
            return link with { GateID = id, GateFile = owner?.File ?? -1, GateOrdinal = owner?.Ordinal ?? -1 };
        }), identities);
    }
}
