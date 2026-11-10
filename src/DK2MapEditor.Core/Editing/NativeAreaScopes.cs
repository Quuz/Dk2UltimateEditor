using System.Buffers.Binary;
using System.Security.Cryptography;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private const uint AreaScopesSignature = 0x53413244; // D2AS; editor working state, never an eighth game file.
    private const string AreaScopesSuffix = "#AreaScopes";
    private sealed record NativeAreaScope(int File, int Ordinal, uint ID, ushort Root, ushort RawRoot);
    private NativeFile? nativeAreaScopes;
    private byte[]? originalNativeAreaScopes, observedNativeAreaScopes;

    private NativeFile NativeAreaScopes
    {
        get
        {
            if (nativeAreaScopes is null)
            {
                nativeAreaScopes = new(MainFile.SourcePath + AreaScopesSuffix, MainFile.ExportPath + AreaScopesSuffix, EncodeNativeAreaScopes([]));
                SynchronizeNativeAreaScopes(); originalNativeAreaScopes = nativeAreaScopes.Data.ToArray();
            }
            else SynchronizeNativeAreaScopes();
            return nativeAreaScopes;
        }
    }
    private bool NativeAreaScopesChanged => !IsLegacyProject && !NativeAreaScopes.Data.Span.SequenceEqual(originalNativeAreaScopes);
    private Dictionary<(int File, int Ordinal), NativeRecord> NativeAreaAddresses()
    {
        var result = new Dictionary<(int, int), NativeRecord>();
        foreach (var file in files.Where(file => file.Signature == 0xBE))
        {
            var records = new List<NativeRecord>(); AppendNativeCollectionRecords(file, records); var ordinal = 0;
            foreach (var area in records.Where(record => record.Category == "GAMEAREA")) result.Add((NativeBindingFile(file), ordinal++), area);
        }
        return result;
    }
    private static ushort AreaRoot(NativeRecord area) => ushort.Parse(area.Read(area.Fields.Single(field => field.Name == "wAreaTrigger")));
    private static uint AreaID(NativeRecord area) => uint.Parse(area.Read(area.Fields.Single(field => field.Name == "bAreaID")));
    private static byte[] EncodeNativeAreaScopes(IReadOnlyList<NativeAreaScope> rows)
    {
        if (rows.Count > 1_000_000) throw new InvalidDataException("Too many working area scopes.");
        var bytes = new byte[checked(16 + rows.Count * 20)];
        WriteUInt32(bytes, 0, AreaScopesSignature); WriteUInt32(bytes, 4, 1); WriteUInt32(bytes, 8, (uint)bytes.Length); WriteUInt32(bytes, 12, (uint)rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i]; var offset = 16 + i * 20;
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), row.File);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + 4), row.Ordinal);
            WriteUInt32(bytes, offset + 8, row.ID);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset + 12), row.Root);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset + 14), row.RawRoot);
        }
        return bytes;
    }
    private static List<NativeAreaScope> DecodeNativeAreaScopes(NativeFile file)
    {
        if (file.Length < 16 || file.Signature != AreaScopesSignature || file.UInt32(4) != 1 || file.UInt32(8) != file.Length
            || file.UInt32(12) > 1_000_000 || 16L + file.UInt32(12) * 20L != file.Length)
            throw new InvalidDataException("Unverified working area-scope metadata.");
        var rows = new List<NativeAreaScope>();
        for (var offset = 16; offset < file.Length; offset += 20)
        {
            var row = new NativeAreaScope(BinaryPrimitives.ReadInt32LittleEndian(file.Data.Span.Slice(offset, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(file.Data.Span.Slice(offset + 4, 4)), file.UInt32(offset + 8),
                BinaryPrimitives.ReadUInt16LittleEndian(file.Data.Span.Slice(offset + 12, 2)), BinaryPrimitives.ReadUInt16LittleEndian(file.Data.Span.Slice(offset + 14, 2)));
            if (row.File < -1 || row.Ordinal < -1 || (row.File == -1) != (row.Ordinal == -1) || file.UInt32(offset + 16) != 0)
                throw new InvalidDataException("Invalid working area-scope address.");
            rows.Add(row);
        }
        if (rows.Where(row => row.File >= 0).DistinctBy(row => (row.File, row.Ordinal)).Count() != rows.Count(row => row.File >= 0))
            throw new InvalidDataException("Duplicate working area-scope address.");
        return rows;
    }
    private byte[] NativeAreaScopeFingerprint()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files.Where(file => file.Signature == 0xBE)) hash.AppendData(file.Data.Span);
        hash.AppendData(nativeAreaScopes!.Data.Span); return hash.GetHashAndReset();
    }
    private void WriteNativeAreaScopes(IReadOnlyList<NativeAreaScope> rows)
    { nativeAreaScopes!.Bytes = EncodeNativeAreaScopes(rows); observedNativeAreaScopes = null; }
    private void SynchronizeNativeAreaScopes()
    {
        var fingerprint = NativeAreaScopeFingerprint();
        if (observedNativeAreaScopes is { } prior && prior.AsSpan().SequenceEqual(fingerprint)) return;
        var areas = NativeAreaAddresses(); var rows = DecodeNativeAreaScopes(nativeAreaScopes!);
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i]; if (row.File < 0) continue;
            if (!areas.TryGetValue((row.File, row.Ordinal), out var area))
                throw new InvalidDataException("A working area scope has no physical area. Rebase collection removal first.");
            var root = AreaRoot(area);
            rows[i] = row with { ID = AreaID(area), Root = root, RawRoot = root };
        }
        foreach (var (address, area) in areas)
            if (AreaRoot(area) != 0 && !rows.Any(row => row.File == address.File && row.Ordinal == address.Ordinal))
                rows.Add(new(address.File, address.Ordinal, AreaID(area), AreaRoot(area), AreaRoot(area)));
        var bytes = EncodeNativeAreaScopes(rows);
        if (!nativeAreaScopes!.Data.Span.SequenceEqual(bytes)) nativeAreaScopes.Bytes = bytes;
        observedNativeAreaScopes = NativeAreaScopeFingerprint();
    }
    private void EnsureNativeAreaScope(NativeRecord area)
    {
        var rows = DecodeNativeAreaScopes(NativeAreaScopes);
        var address = NativeAreaAddresses().Single(pair => SameRecord(pair.Value, area)).Key;
        if (!rows.Any(row => row.File == address.File && row.Ordinal == address.Ordinal))
        { rows.Add(new(address.File, address.Ordinal, AreaID(area), AreaRoot(area), AreaRoot(area))); WriteNativeAreaScopes(rows); }
    }
    private bool HasNativeAreaScope(NativeRecord area)
    {
        var rows = DecodeNativeAreaScopes(NativeAreaScopes);
        var address = NativeAreaAddresses().Single(pair => SameRecord(pair.Value, area)).Key;
        return rows.Any(row => row.File == address.File && row.Ordinal == address.Ordinal);
    }
    public bool HasStoredScriptScope(ScriptEdge requested)
    {
        var root = CurrentScopeRoot(ScriptGraph.Build(this), requested);
        return IsLegacyProject || root.Owner.Category is "Working.AreaScope" or "Working.OwnerScope" || root.Target != 0
            || UniqueOwnerScope(root.Scope) && NativeOwnerState(root.Owner)?.Present == true
            || root.Owner.Category == "GAMEAREA" && HasNativeAreaScope(root.Owner);
    }
    internal IReadOnlyList<ScriptEdge> NativeOrphanAreaRoots()
    {
        if (IsLegacyProject) return [];
        var file = NativeAreaScopes; var rows = DecodeNativeAreaScopes(file); var result = new List<ScriptEdge>();
        for (var i = 0; i < rows.Count; i++) if (rows[i].File == -1)
        {
            var record = new NativeRecord(file, 16 + i * 20, 20, "Working.AreaScope", Array.AsReadOnly(new NativeField[] {
                new("bAreaID", 8, 4, NativeValueKind.UInt32), new("wAreaTrigger", 12, 2, NativeValueKind.UInt16) }));
            result.Add(new(record, record.Fields[1], rows[i].Root, 4));
        }
        return result;
    }
    private bool SetNativeOrphanAreaRoot(NativeRecord record, NativeField field, string value)
    {
        if (record.Category != "Working.AreaScope") return false;
        var current = NativeOrphanAreaRoots().SingleOrDefault(root => SameRecord(root.Owner, record) && root.Field == field)
            ?? throw new ArgumentException("Select a current orphan area root.");
        current.Field.Encode(value).CopyTo(current.Owner.File.Bytes, current.Owner.Offset + current.Field.Offset); return true;
    }
    private void RemoveNativeAreaScope(ScriptEdge root)
    {
        var rows = DecodeNativeAreaScopes(NativeAreaScopes);
        if (root.Owner.Category == "Working.AreaScope") rows.RemoveAt((root.Owner.Offset - 16) / 20);
        else if (root.Owner.Category == "GAMEAREA")
        {
            var address = NativeAreaAddresses().Single(pair => SameRecord(pair.Value, root.Owner)).Key;
            rows.RemoveAll(row => row.File == address.File && row.Ordinal == address.Ordinal);
        }
        else return;
        WriteNativeAreaScopes(rows);
    }
    private void RebaseNativeAreaDeletion(NativeRecord record)
    {
        if (record.Category != "GAMEAREA") return;
        var rows = DecodeNativeAreaScopes(NativeAreaScopes);
        var address = NativeAreaAddresses().Single(pair => SameRecord(pair.Value, record)).Key;
        WriteNativeAreaScopes(rows.Select(row => row.File != address.File ? row : row.Ordinal == address.Ordinal
            ? row with { File = -1, Ordinal = -1 } : row.Ordinal > address.Ordinal ? row with { Ordinal = row.Ordinal - 1 } : row).ToArray());
    }
    private void CompleteNativeAreaInsertion(NativeRecord area)
    {
        // Explicit/copied nonzero roots keep their own graph. Never assign an
        // older orphan to the same physical address or overwrite that graph.
        if (AreaRoot(area) != 0) return;
        var rows = DecodeNativeAreaScopes(NativeAreaScopes); var index = rows.FindIndex(row => row.File == -1 && row.ID == AreaID(area));
        if (index < 0) return;
        var address = NativeAreaAddresses().Single(pair => SameRecord(pair.Value, area)).Key; var saved = rows[index];
        rows[index] = saved with { File = address.File, Ordinal = address.Ordinal, RawRoot = saved.Root };
        area.Fields.Single(field => field.Name == "wAreaTrigger").Encode(saved.Root.ToString()).CopyTo(area.File.Bytes, area.Offset + 28);
        WriteNativeAreaScopes(rows);
    }
    private void RestoreCompiledNativeAreaScopes(IReadOnlyList<NativeRecord> source)
    {
        _ = NativeAreaScopes; var rows = new List<NativeAreaScope>(); var areas = NativeAreaAddresses(); var used = new HashSet<(int,int)>();
        foreach (var point in source.Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)
            && record.Read(record.Fields.Single(field => field.Name == "node.type")) == "4"))
        {
            var id = uint.Parse(point.Read(point.Fields.Single(field => field.Name == "area")));
            var target = ushort.Parse(point.Read(point.Fields.Single(field => field.Name == "trigger")));
            var matches = areas.Where(pair => AreaID(pair.Value) == id && !used.Contains(pair.Key)).ToArray();
            if (matches.Length == 0) rows.Add(new(-1, -1, id, target, target));
            else { var match = matches[0]; used.Add(match.Key); var root = AreaRoot(match.Value); rows.Add(new(match.Key.File, match.Key.Ordinal, id, root, root)); }
        }
        WriteNativeAreaScopes(rows);
    }
}
