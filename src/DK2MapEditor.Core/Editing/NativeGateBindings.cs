using System.Buffers.Binary;
using System.Security.Cryptography;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private const uint GateBindingsSignature = 0x42473244; // D2GB, working metadata only.
    private const string GateBindingsSuffix = "#GateBindings";
    private sealed record NativeGateAddress(int File, int Ordinal);
    private sealed record NativeGateIdentity(int File, int Ordinal, int ID);
    private sealed record NativeGateLink(int ActionFile, int ActionOffset, int GateFile, int GateOrdinal, ushort X, ushort Y, int GateID = 0);
    private bool compilingNativeGateIdentities;
    private NativeFile? nativeGateBindings;
    private byte[]? originalNativeGateBindings;
    private byte[]? observedNativeGateBindings;
    private bool synchronizingNativeGateBindings;

    private NativeFile NativeGateBindings
    {
        get
        {
            if (nativeGateBindings is null)
            {
                var bytes = EncodeNativeGateLinks([]);
                nativeGateBindings = new(MainFile.SourcePath + GateBindingsSuffix, MainFile.ExportPath + GateBindingsSuffix, bytes);
                SynchronizeNativeGateBindings();
                originalNativeGateBindings = nativeGateBindings.Data.ToArray();
            }
            else SynchronizeNativeGateBindings();
            return nativeGateBindings;
        }
    }
    private bool NativeGateBindingsChanged => !IsLegacyProject && !NativeGateBindings.Data.Span.SequenceEqual(originalNativeGateBindings);

    // Work only on the two native collections; definition catalogues do not
    // participate in gate identity or need reparsing on each transaction.
    private IReadOnlyList<NativeRecord> NativeGateBindingRecords()
    {
        var records = new List<NativeRecord>();
        foreach (var file in files.Where(file => file.Signature is 0xBE or 0xD2)) AppendNativeCollectionRecords(file, records);
        return records;
    }
    private int NativeBindingFile(NativeFile file) => files.Where(item => item.Signature == file.Signature).ToList().IndexOf(file);
    private Dictionary<NativeGateAddress, NativeRecord> NativeGateAddresses(IReadOnlyList<NativeRecord> records)
    {
        var result = new Dictionary<NativeGateAddress, NativeRecord>();
        foreach (var group in records.Where(record => record.Category == "GAMEGATE").GroupBy(record => record.File))
        {
            var index = 0;
            foreach (var gate in group) result.Add(new(NativeBindingFile(group.Key), index++), gate);
        }
        return result;
    }
    private NativeGateAddress NativeGateAddressOf(NativeRecord gate) => NativeGateAddresses(NativeGateBindingRecords())
        .Single(pair => SameRecord(pair.Value, gate)).Key;
    private static bool IsNativeDestroyAction(NativeRecord record) => NativeDerivedAction(record) && record.File.Data.Span[record.Offset + 22] == 32;
    private static bool GateLinkOwns(NativeGateLink link, NativeGateAddress gate) => link.GateFile == gate.File && link.GateOrdinal == gate.Ordinal;

    private static byte[] EncodeNativeGateLinks(IReadOnlyList<NativeGateLink> links, IReadOnlyList<NativeGateIdentity>? identities = null)
    {
        identities ??= [];
        if (links.Count > 1_000_000 || identities.Count > 1_000_000) throw new InvalidDataException("Too many working gate bindings.");
        var bytes = new byte[checked(20 + links.Count * 24 + identities.Count * 12)];
        WriteUInt32(bytes, 0, GateBindingsSignature); WriteUInt32(bytes, 4, 2); WriteUInt32(bytes, 8, (uint)bytes.Length); WriteUInt32(bytes, 12, (uint)links.Count); WriteUInt32(bytes, 16, (uint)identities.Count);
        for (var i = 0; i < links.Count; i++)
        {
            var link = links[i]; var offset = 20 + i * 24;
            foreach (var (relative, value) in new[] { (0, link.ActionFile), (4, link.ActionOffset), (8, link.GateFile), (12, link.GateOrdinal) })
                BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + relative), value);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset + 16), link.X); BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset + 18), link.Y);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + 20), link.GateID);
        }
        for (var i = 0; i < identities.Count; i++)
        {
            var identity = identities[i]; var offset = 20 + links.Count * 24 + i * 12;
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), identity.File);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + 4), identity.Ordinal);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + 8), identity.ID);
        }
        return bytes;
    }
    private static (List<NativeGateLink> Links, List<NativeGateIdentity> Identities) DecodeNativeGateState(NativeFile file)
    {
        if (file.Length < 16 || file.Signature != GateBindingsSignature || file.UInt32(4) is not (1 or 2) || file.UInt32(8) != file.Length || file.UInt32(12) > 1_000_000)
            throw new InvalidDataException("Unverified working gate-binding metadata.");
        var old = file.UInt32(4) == 1; var header = old ? 16 : 20; var row = old ? 20 : 24;
        if (file.Length < header || !old && file.UInt32(16) > 1_000_000
            || header + file.UInt32(12) * (long)row + (old ? 0 : file.UInt32(16) * 12L) != file.Length)
            throw new InvalidDataException("Unverified working gate-binding metadata size.");
        var result = new List<NativeGateLink>();
        for (var offset = header; offset < header + file.UInt32(12) * (long)row; offset += row)
        {
            int N(int relative) => BinaryPrimitives.ReadInt32LittleEndian(file.Data.Span.Slice(offset + relative, 4));
            var link = new NativeGateLink(N(0), N(4), N(8), N(12), BinaryPrimitives.ReadUInt16LittleEndian(file.Data.Span.Slice(offset + 16, 2)), BinaryPrimitives.ReadUInt16LittleEndian(file.Data.Span.Slice(offset + 18, 2)), old ? 0 : N(20));
            if (link.ActionFile < 0 || link.ActionOffset < 60 || link.GateFile < -1 || link.GateOrdinal < -1 || (link.GateFile == -1) != (link.GateOrdinal == -1) || link.GateID < 0)
                throw new InvalidDataException("Invalid working gate address.");
            result.Add(link);
        }
        if (result.DistinctBy(link => (link.ActionFile, link.ActionOffset)).Count() != result.Count) throw new InvalidDataException("Duplicate working action address.");
        var identities = new List<NativeGateIdentity>();
        if (!old) for (var offset = checked(header + (int)file.UInt32(12) * row); offset < file.Length; offset += 12)
        {
            int N(int relative) => BinaryPrimitives.ReadInt32LittleEndian(file.Data.Span.Slice(offset + relative, 4));
            var identity = new NativeGateIdentity(N(0), N(4), N(8));
            if (identity.File < 0 || identity.Ordinal < 0 || identity.ID <= 0) throw new InvalidDataException("Invalid working gate identity.");
            identities.Add(identity);
        }
        if (identities.DistinctBy(identity => (identity.File, identity.Ordinal)).Count() != identities.Count || identities.DistinctBy(identity => identity.ID).Count() != identities.Count)
            throw new InvalidDataException("Duplicate working gate identity.");
        return (result, identities);
    }
    private static List<NativeGateLink> DecodeNativeGateLinks(NativeFile file) => DecodeNativeGateState(file).Links;
    private byte[] NativeGateBindingFingerprint()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files.Where(file => file.Signature is 0xBE or 0xD2)) hash.AppendData(file.Data.Span);
        hash.AppendData(nativeGateBindings!.Data.Span); return hash.GetHashAndReset();
    }
    private void WriteNativeGateLinks(IEnumerable<NativeGateLink> links)
    {
        WriteNativeGateState(links, DecodeNativeGateState(nativeGateBindings!).Identities);
    }
    private void WriteNativeGateState(IEnumerable<NativeGateLink> links, IEnumerable<NativeGateIdentity> identities)
    {
        nativeGateBindings!.Bytes = EncodeNativeGateLinks(links.OrderBy(link => link.ActionFile).ThenBy(link => link.ActionOffset).ToArray(), identities.OrderBy(identity => identity.File).ThenBy(identity => identity.Ordinal).ToArray());
        observedNativeGateBindings = null;
    }
    private void SynchronizeNativeGateBindings()
    {
        if (synchronizingNativeGateBindings) return;
        var fingerprint = NativeGateBindingFingerprint();
        if (observedNativeGateBindings is { } previous && previous.AsSpan().SequenceEqual(fingerprint)) return;
        synchronizingNativeGateBindings = true;
        try
        {
            var state = DecodeNativeGateState(nativeGateBindings!);
            var prior = state.Links.ToDictionary(link => (link.ActionFile, link.ActionOffset));
            var records = NativeGateBindingRecords(); var gates = NativeGateAddresses(records); var result = new List<NativeGateLink>();
            var identities = state.Identities.Where(identity => gates.TryGetValue(new(identity.File, identity.Ordinal), out var gate) && NativeSourceGate(gate)).ToList();
            var used = identities.Select(identity => identity.ID).ToHashSet();
            foreach (var (address, gate) in gates.Where(pair => NativeSourceGate(pair.Value)))
                if (!identities.Any(identity => identity.File == address.File && identity.Ordinal == address.Ordinal))
                {
                    var id = 1; while (used.Contains(id)) id = checked(id + 1);
                    identities.Add(new(address.File, address.Ordinal, id)); used.Add(id);
                }
            foreach (var action in records.Where(IsNativeDestroyAction))
            {
                var file = NativeBindingFile(action.File); var point = NativeDerivedPayload(action);
                if (prior.TryGetValue((file, action.Offset), out var saved) && saved.X == point.X && saved.Y == point.Y)
                {
                    // Version 1 only retained addresses. Upgrade a live address;
                    // its already-lost deleted ID cannot be reconstructed.
                    var id = saved.GateID;
                    if (id == 0 && saved.GateFile >= 0)
                        id = identities.FirstOrDefault(identity => identity.File == saved.GateFile && identity.Ordinal == saved.GateOrdinal)?.ID ?? 0;
                    var owner = identities.SingleOrDefault(identity => identity.ID == id);
                    saved = saved with { GateID = id, GateFile = owner?.File ?? -1, GateOrdinal = owner?.Ordinal ?? -1 };
                    result.Add(saved); continue;
                }
                var target = gates.FirstOrDefault(pair => NativeSourceGate(pair.Value) && NativeDerivedPosition(pair.Value, true) == new TilePoint(point.X - 1, point.Y - 1)).Key;
                var targetID = target is null ? 0 : identities.Single(identity => identity.File == target.File && identity.Ordinal == target.Ordinal).ID;
                result.Add(new(file, action.Offset, target?.File ?? -1, target?.Ordinal ?? -1, (ushort)point.X, (ushort)point.Y, targetID));
            }
            var bytes = EncodeNativeGateLinks(result.OrderBy(link => link.ActionFile).ThenBy(link => link.ActionOffset).ToArray(), identities.OrderBy(identity => identity.File).ThenBy(identity => identity.Ordinal).ToArray());
            if (!nativeGateBindings!.Data.Span.SequenceEqual(bytes)) nativeGateBindings.Bytes = bytes;
            observedNativeGateBindings = NativeGateBindingFingerprint();
        }
        finally { synchronizingNativeGateBindings = false; }
    }
    internal NativeRecord? NativeGateTarget(NativeRecord action)
    {
        if (IsLegacyProject || !IsNativeDestroyAction(action)) return null;
        var link = DecodeNativeGateLinks(NativeGateBindings).Single(item => item.ActionFile == NativeBindingFile(action.File) && item.ActionOffset == action.Offset);
        return link.GateFile < 0 ? null : NativeGateAddresses(NativeGateBindingRecords()).GetValueOrDefault(new(link.GateFile, link.GateOrdinal));
    }
    private static string NativeGateChoiceKey(NativeRecord gate) => gate.File.ExportPath + ":" + gate.Offset.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private string? NativeGateTargetKey(NativeRecord action) => NativeGateTarget(action) is { } gate ? NativeGateChoiceKey(gate) : null;
    private void SetNativeGateTarget(NativeRecord action, NativeRecord? gate)
    {
        if (IsLegacyProject || !IsNativeDestroyAction(action)) return;
        var links = DecodeNativeGateLinks(NativeGateBindings); var address = gate is null ? null : NativeGateAddressOf(gate); var point = NativeDerivedPayload(action); var file = NativeBindingFile(action.File);
        links.RemoveAll(link => link.ActionFile == file && link.ActionOffset == action.Offset);
        var id = gate is null ? 0 : NativeGateID(gate);
        links.Add(new(file, action.Offset, address?.File ?? -1, address?.Ordinal ?? -1, (ushort)point.X, (ushort)point.Y, id)); WriteNativeGateLinks(links);
    }
    private void RefreshNativeGatePayloadStamps()
    {
        if (nativeGateBindings is null) return;
        var actions = NativeGateBindingRecords().Where(IsNativeDestroyAction).ToDictionary(action => (NativeBindingFile(action.File), action.Offset));
        WriteNativeGateLinks(DecodeNativeGateLinks(nativeGateBindings).Where(link => actions.ContainsKey((link.ActionFile, link.ActionOffset))).Select(link =>
        {
            var point = NativeDerivedPayload(actions[(link.ActionFile, link.ActionOffset)]); return link with { X = (ushort)point.X, Y = (ushort)point.Y };
        }));
    }
    private void RebaseNativeGateDeletion(NativeRecord record)
    {
        if (IsLegacyProject || record.Category != "GAMEGATE") return;
        var state = DecodeNativeGateState(NativeGateBindings); var address = NativeGateAddressOf(record);
        WriteNativeGateState(state.Links.Select(link => link.GateFile != address.File || link.GateOrdinal < address.Ordinal ? link
            : link.GateOrdinal == address.Ordinal ? link with { GateFile = -1, GateOrdinal = -1 } : link with { GateOrdinal = link.GateOrdinal - 1 }),
            state.Identities.Where(identity => identity.File != address.File || identity.Ordinal != address.Ordinal).Select(identity => identity.File == address.File && identity.Ordinal > address.Ordinal ? identity with { Ordinal = identity.Ordinal - 1 } : identity));
    }
    private void RebaseNativeActionInsertion(NativeFile file, int offset, int length)
    {
        if (file.Signature != 0xD2) return;
        var index = NativeBindingFile(file); var links = DecodeNativeGateLinks(NativeGateBindings);
        if (!links.Any(link => link.ActionFile == index && link.ActionOffset >= offset)) return;
        WriteNativeGateLinks(links.Select(link => link.ActionFile == index && link.ActionOffset >= offset
            ? link with { ActionOffset = checked(link.ActionOffset + length) } : link));
    }

    private void RebaseNativeActionDeletion(IEnumerable<NativeRecord> removed)
    {
        var rows = removed.Where(record => record.File.Signature == 0xD2).ToArray(); if (rows.Length == 0) return;
        var links = DecodeNativeGateLinks(NativeGateBindings);
        WriteNativeGateLinks(links.Where(link => !rows.Any(row => NativeBindingFile(row.File) == link.ActionFile && row.Offset == link.ActionOffset)).Select(link =>
            link with { ActionOffset = link.ActionOffset - rows.Where(row => NativeBindingFile(row.File) == link.ActionFile && row.Offset < link.ActionOffset).Sum(row => row.Length) }));
    }
    private void ReinitializeNativeGateBindings()
    {
        if (IsLegacyProject) return;
        _ = NativeGateBindings; nativeGateBindings!.Bytes = EncodeNativeGateLinks([]); observedNativeGateBindings = null; SynchronizeNativeGateBindings();
    }
    private void ApplyNativeGateParameterBinding(ScenarioParameterDraft draft)
    {
        if (IsLegacyProject || draft.Type != "ACTION_DESTROYGATE") return;
        if (draft.NativeGateKey is { } key)
            SetNativeGateTarget(draft.Record, NativeGateBindingRecords().Single(record => NativeSourceGate(record) && NativeGateChoiceKey(record) == key));
        else if (draft.NativeGateBaselineKey is not null)
        {
            var point = NativeDerivedPayload(draft.Record);
            SetNativeGateTarget(draft.Record, NativeGateBindingRecords().FirstOrDefault(record => NativeSourceGate(record) && NativeDerivedPosition(record, true) == new TilePoint(point.X - 1, point.Y - 1)));
        }
    }
}
