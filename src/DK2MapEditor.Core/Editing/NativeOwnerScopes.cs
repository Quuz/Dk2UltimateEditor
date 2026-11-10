using System.Buffers.Binary;
using System.Security.Cryptography;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private const uint OwnerScopesSignature = 0x534F3244; // D2OS, editor state only.
    private const string OwnerScopesSuffix = "#OwnerScopes";
    private sealed record NativeOwnerAddress(int File, int Ordinal, int Member = -1);
    private sealed record NativeOwnerSite(NativeOwnerAddress Address, NativeRecord Record, uint Scope, uint Band, bool Importable);
    private sealed record NativeOwnerScope(int File, int Ordinal, int Member, uint Scope, uint ID, uint Band, ushort Root, bool Present);
    private NativeFile? nativeOwnerScopes;
    private byte[]? originalNativeOwnerScopes, observedNativeOwnerScopes;
    private static bool UniqueOwnerScope(uint scope) => scope is 2 or 8 or 16 or 64 or 128 or 256;
    private static uint NativeUniqueScope(NativeRecord record) => record.Category switch
    {
        "GAMEKEEPER" or "GAMEHERO" or "GAMENEUTRAL" => 2,
        "GAMEDOOR" => 64, "GAMEBAND" => 8,
        "GAMEOBJECT" => record.Fields.FirstOrDefault(field => field.Name == "bObjectID") is { } id
            ? uint.Parse(record.Read(id)) is >= 0x75 and <= 0x81 ? 256u : 128u : 0,
        _ => 0
    };
    private static NativeField OwnerTrigger(NativeRecord record) => record.Fields.Single(field => field.Name.EndsWith("Trigger", StringComparison.Ordinal));
    private static ushort OwnerRoot(NativeRecord record) => record.Category=="GAMETRAP"?(ushort)0:ushort.Parse(record.Read(OwnerTrigger(record)));
    private IReadOnlyList<NativeOwnerSite> NativeOwnerSites()
    {
        var result = new List<NativeOwnerSite>();
        foreach (var file in files.Where(file => file.Signature == 0xBE))
        {
            var records = new List<NativeRecord>(); AppendNativeCollectionRecords(file, records); var ordinal = 0;
            foreach (var record in records.Where(record => !record.Category.StartsWith("BandHero", StringComparison.Ordinal)))
            {
                var address = new NativeOwnerAddress(NativeBindingFile(file), ordinal++); var scope = NativeUniqueScope(record);
                if (scope == 0 && record.Category!="GAMETRAP" || scope!=0 && record.Fields.Count(field => field.Name.EndsWith("Trigger", StringComparison.Ordinal)) != 1) continue;
                var band = scope == 8 ? uint.Parse(record.Read(record.Fields.Single(field => field.Name == "bBandID"))) : 0;
                result.Add(new(address, record, scope, band, true));
                if (scope != 8 || record.Length != 563) continue;
                var prefix = true;
                for (var member = 0; member < 16; member++)
                {
                    var slot = records.Single(row => row.Offset == record.Offset + 51 + member * 32 && row.Category.StartsWith("BandHero", StringComparison.Ordinal));
                    prefix &= slot.Read(slot.Fields.Single(field => field.Name == "bGlobalCreatureID")) != "0" && slot.Read(slot.Fields.Single(field => field.Name == "bGlobalBehavior")) != "0";
                    result.Add(new(address with { Member = member }, slot, 16, band, prefix));
                }
            }
        }
        return result;
    }
    private NativeFile NativeOwnerScopes
    {
        get
        {
            if (nativeOwnerScopes is null)
            {
                nativeOwnerScopes = new(MainFile.SourcePath + OwnerScopesSuffix, MainFile.ExportPath + OwnerScopesSuffix, EncodeNativeOwnerScopes([]));
                SynchronizeNativeOwnerScopes(); originalNativeOwnerScopes = nativeOwnerScopes.Data.ToArray();
            }
            else SynchronizeNativeOwnerScopes();
            return nativeOwnerScopes;
        }
    }
    private bool NativeOwnerScopesChanged => !IsLegacyProject && !NativeOwnerScopes.Data.Span.SequenceEqual(originalNativeOwnerScopes);
    private static byte[] EncodeNativeOwnerScopes(IReadOnlyList<NativeOwnerScope> rows)
    {
        if (rows.Count > 1_000_000) throw new InvalidDataException("Too many working owner scopes.");
        var bytes = new byte[checked(16 + rows.Count * 36)];
        WriteUInt32(bytes, 0, OwnerScopesSignature); WriteUInt32(bytes, 4, rows.Any(row=>row.Scope==0)?2u:1u); WriteUInt32(bytes, 8, (uint)bytes.Length); WriteUInt32(bytes, 12, (uint)rows.Count);
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index]; var offset = 16 + index * 36;
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), row.File);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + 4), row.Ordinal);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + 8), row.Member);
            WriteUInt32(bytes, offset + 12, row.Scope); WriteUInt32(bytes, offset + 16, row.ID); WriteUInt32(bytes, offset + 20, row.Band);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset + 24), row.Root);
            WriteUInt32(bytes, offset + 28, row.Present ? 1u : 0u);
        }
        return bytes;
    }
    private static List<NativeOwnerScope> DecodeNativeOwnerScopes(NativeFile file)
    {
        if (file.Length < 16 || file.Signature != OwnerScopesSignature || file.UInt32(4) is not (1 or 2) || file.UInt32(8) != file.Length
            || file.UInt32(12) > 1_000_000 || 16L + file.UInt32(12) * 36L != file.Length) throw new InvalidDataException("Unverified working owner-scope metadata.");
        var rows = new List<NativeOwnerScope>();
        for (var offset = 16; offset < file.Length; offset += 36)
        {
            int N(int relative) => BinaryPrimitives.ReadInt32LittleEndian(file.Data.Span.Slice(offset + relative, 4));
            var row = new NativeOwnerScope(N(0), N(4), N(8), file.UInt32(offset + 12), file.UInt32(offset + 16), file.UInt32(offset + 20),
                BinaryPrimitives.ReadUInt16LittleEndian(file.Data.Span.Slice(offset + 24, 2)), file.UInt32(offset + 28) != 0);
            if (row.File < -1 || row.Ordinal < -1 || (row.File == -1) != (row.Ordinal == -1) || row.Member is < -1 or > 15
                || !UniqueOwnerScope(row.Scope) && !(file.UInt32(4)==2 && row.Scope==0 && row.File>=0 && row.Member==-1 && row.Root==0 && !row.Present && row.Band==0)
                || (row.Scope == 16) != (row.Member >= 0) || file.UInt32(offset + 28) > 1
                || BinaryPrimitives.ReadUInt16LittleEndian(file.Data.Span.Slice(offset + 26, 2)) != 0 || file.UInt32(offset + 32) != 0
                || row.File == -1 && !row.Present) throw new InvalidDataException("Invalid working owner-scope row.");
            rows.Add(row);
        }
        if (rows.Where(row => row.File >= 0).DistinctBy(row => (row.File, row.Ordinal, row.Member)).Count() != rows.Count(row => row.File >= 0))
            throw new InvalidDataException("Duplicate working owner address.");
        return rows;
    }
    private byte[] NativeOwnerFingerprint()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files.Where(file => file.Signature == 0xBE)) hash.AppendData(file.Data.Span);
        hash.AppendData(nativeOwnerScopes!.Data.Span); return hash.GetHashAndReset();
    }
    private void WriteNativeOwnerScopes(IReadOnlyList<NativeOwnerScope> rows)
    { nativeOwnerScopes!.Bytes = EncodeNativeOwnerScopes(rows); observedNativeOwnerScopes = null; }
    private static uint NextNativeOwnerID(IEnumerable<NativeOwnerScope> rows)
    {
        // Reserve orphan identities too, consistently with the current DKM
        // safety contract: a new owner must not silently acquire an old point.
        var used = rows.Select(row => row.ID).ToHashSet(); uint result = 1;
        while (used.Contains(result)) result = checked(result + 1); return result;
    }
    private void SynchronizeNativeOwnerScopes()
    {
        var fingerprint = NativeOwnerFingerprint();
        if (observedNativeOwnerScopes is { } previous && previous.AsSpan().SequenceEqual(fingerprint)) return;
        var sites = NativeOwnerSites(); var addresses = sites.ToDictionary(site => site.Address); var rows = DecodeNativeOwnerScopes(nativeOwnerScopes!);
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index]; if (row.File < 0) continue;
            if (!addresses.TryGetValue(new(row.File, row.Ordinal, row.Member), out var site))
                throw new InvalidDataException("A working scenario owner disappeared without rebasing its identity.");
            if(row.Scope==0 && site.Record.Category!="GAMETRAP")throw new InvalidDataException("An identity-only owner must reference a trap record.");
            var root = OwnerRoot(site.Record);
            rows[index] = row with { Scope = site.Scope, Band = row.Member >= 0 ? row.Band : site.Band, Root = root, Present = row.Present || root != 0 };
        }
        foreach (var site in sites.Where(site => site.Importable && OwnerRoot(site.Record) != 0))
            if (!rows.Any(row => row.File == site.Address.File && row.Ordinal == site.Address.Ordinal && row.Member == site.Address.Member))
                rows.Add(new(site.Address.File, site.Address.Ordinal, site.Address.Member, site.Scope, NextNativeOwnerID(rows), site.Band, OwnerRoot(site.Record), true));
        var bytes = EncodeNativeOwnerScopes(rows);
        if (!nativeOwnerScopes!.Data.Span.SequenceEqual(bytes)) nativeOwnerScopes.Bytes = bytes;
        observedNativeOwnerScopes = NativeOwnerFingerprint();
    }
    private NativeOwnerScope? NativeOwnerState(NativeRecord record)
    {
        if (IsLegacyProject) return null;
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes); var site = NativeOwnerSites().SingleOrDefault(site => SameRecord(site.Record, record));
        return site is null ? null : rows.SingleOrDefault(row => row.File == site.Address.File && row.Ordinal == site.Address.Ordinal && row.Member == site.Address.Member);
    }
    private Dictionary<(NativeFile File, int Offset, int Length), NativeOwnerScope> NativeOwnerStates()
    {
        if (IsLegacyProject) return [];
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes).Where(row => row.File >= 0)
            .ToDictionary(row => new NativeOwnerAddress(row.File, row.Ordinal, row.Member));
        return NativeOwnerSites().Where(site => rows.ContainsKey(site.Address))
            .ToDictionary(site => (site.Record.File, site.Record.Offset, site.Record.Length), site => rows[site.Address]);
    }
    private void EnsureNativeOwnerScope(NativeRecord record)
    {
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes); var site = NativeOwnerSites().SingleOrDefault(site => SameRecord(site.Record, record));
        if (site is null || !UniqueOwnerScope(site.Scope)) return;
        var index = rows.FindIndex(row => row.File == site.Address.File && row.Ordinal == site.Address.Ordinal && row.Member == site.Address.Member);
        if (index < 0) rows.Add(new(site.Address.File, site.Address.Ordinal, site.Address.Member, site.Scope, NextNativeOwnerID(rows), site.Band, OwnerRoot(record), true));
        else
        {
            if (rows[index].ID == 0 || rows.Any(row => !ReferenceEquals(row, rows[index]) && row.ID == rows[index].ID))
                throw new InvalidOperationException("Repair the shared or zero owner identity before attaching a point.");
            rows[index] = rows[index] with { Present = true };
        }
        WriteNativeOwnerScopes(rows);
    }
    private void ReallocateNativeOwnerIdentity(NativeRecord owner)
    {
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes); var site = NativeOwnerSites().Single(site => SameRecord(site.Record, owner));
        var index = rows.FindIndex(row => row.File == site.Address.File && row.Ordinal == site.Address.Ordinal && row.Member == site.Address.Member);
        if (index < 0) { EnsureNativeOwnerScope(owner); return; }
        var id = NextNativeOwnerID(rows.Where((_, position) => position != index));
        if (rows[index].ID != id) { rows[index] = rows[index] with { ID = id }; WriteNativeOwnerScopes(rows); }
    }
    private void SetEditorNativeMemberBandOperand(ScriptOwner owner)
    {
        if (owner.Scope != 16) return;
        var ordinal = Records().Where(IsBand).ToList().FindIndex(band => owner.BandRecord is { } parent && SameRecord(band, parent));
        if (ordinal < 0) throw new InvalidOperationException("The selected member has no physical parent band.");
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes); var site = NativeOwnerSites().Single(site => SameRecord(site.Record, owner.Record));
        var index = rows.FindIndex(row => row.File == site.Address.File && row.Ordinal == site.Address.Ordinal && row.Member == site.Address.Member);
        if (index < 0) throw new InvalidOperationException("Create the member point before storing its band operand.");
        if (rows[index].Band != (uint)ordinal) { rows[index] = rows[index] with { Band = (uint)ordinal }; WriteNativeOwnerScopes(rows); }
    }
    internal IReadOnlyList<ScriptEdge> NativeOrphanOwnerRoots()
    {
        if (IsLegacyProject) return [];
        var file = NativeOwnerScopes; var rows = DecodeNativeOwnerScopes(file); var roots = new List<ScriptEdge>();
        for (var index = 0; index < rows.Count; index++) if (rows[index].File < 0 && rows[index].Present)
        {
            var record = new NativeRecord(file, 16 + index * 36, 36, "Working.OwnerScope", Array.AsReadOnly(new NativeField[] {
                new("uniqueid", 16, 4, NativeValueKind.UInt32), new("band", 20, 4, NativeValueKind.UInt32), new("wOwnerTrigger", 24, 2, NativeValueKind.UInt16) }));
            roots.Add(new(record, record.Fields[2], rows[index].Root, rows[index].Scope));
        }
        return roots;
    }
    private bool SetNativeOrphanOwnerRoot(NativeRecord record, NativeField field, string value)
    {
        if (record.Category != "Working.OwnerScope") return false;
        var root = NativeOrphanOwnerRoots().SingleOrDefault(root => SameRecord(root.Owner, record) && root.Field == field)
            ?? throw new ArgumentException("Select a current orphan owner root.");
        root.Field.Encode(value).CopyTo(root.Owner.File.Bytes, root.Owner.Offset + root.Field.Offset); return true;
    }
    private void RemoveNativeOwnerScope(ScriptEdge root, bool releaseIdentity = false)
    {
        if (root.Owner.Category != "Working.OwnerScope" && !UniqueOwnerScope(root.Scope)) return;
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes);
        if (root.Owner.Category == "Working.OwnerScope") rows.RemoveAt((root.Owner.Offset - 16) / 36);
        else
        {
            var site = NativeOwnerSites().Single(site => SameRecord(site.Record, root.Owner));
            var index = rows.FindIndex(row => row.File == site.Address.File && row.Ordinal == site.Address.Ordinal && row.Member == site.Address.Member);
            if (index < 0) return;
            if (releaseIdentity) rows.RemoveAt(index); else rows[index] = rows[index] with { Root = 0, Present = false };
        }
        WriteNativeOwnerScopes(rows);
    }
    private void RebaseNativeOwnerRemovals(IReadOnlyList<NativeRecord> removing, bool preservePoints)
    {
        var native = removing.Where(record => record.File.Signature == 0xBE).ToArray(); if (native.Length == 0) return;
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes); var deleted = new Dictionary<int, HashSet<int>>();
        foreach (var group in native.GroupBy(record => record.File))
        {
            var records = new List<NativeRecord>(); AppendNativeCollectionRecords(group.Key, records);
            var top = records.Where(record => !record.Category.StartsWith("BandHero", StringComparison.Ordinal)).ToArray();
            var positions = group.Select(record => Array.FindIndex(top, item => SameRecord(item, record))).ToHashSet();
            if (positions.Contains(-1)) throw new InvalidOperationException("Select current top-level owners before removing them.");
            deleted.Add(NativeBindingFile(group.Key), positions);
        }
        var result = new List<NativeOwnerScope>();
        foreach (var row in rows)
        {
            if (!deleted.TryGetValue(row.File, out var positions)) { result.Add(row); continue; }
            if (positions.Contains(row.Ordinal))
            { if (preservePoints && row.Present) result.Add(row with { File = -1, Ordinal = -1 }); }
            else result.Add(row with { Ordinal = row.Ordinal - positions.Count(position => position < row.Ordinal) });
        }
        WriteNativeOwnerScopes(result);
    }
    private void RebaseNativeMemberRemoval(NativeRecord band, int member, int count)
    {
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes); var site = NativeOwnerSites().Single(site => site.Scope == 8 && SameRecord(site.Record, band));
        WriteNativeOwnerScopes(rows.Where(row => row.File != site.Address.File || row.Ordinal != site.Address.Ordinal || row.Member != member)
            .Select(row => row.File == site.Address.File && row.Ordinal == site.Address.Ordinal && row.Member > member && row.Member < count
                ? row with { Member = row.Member - 1 } : row).ToArray());
    }
    private void ForgetNativeMemberIdentity(NativeRecord member)
    {
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes); var site = NativeOwnerSites().Single(site => SameRecord(site.Record, member));
        rows.RemoveAll(row => row.File == site.Address.File && row.Ordinal == site.Address.Ordinal && row.Member == site.Address.Member);
        WriteNativeOwnerScopes(rows);
    }
    private void RebaseNativeMemberMapping(NativeRecord band, IReadOnlyList<int> origins)
    {
        if (origins.Count != 16 || origins.Where(origin => origin >= 0).Distinct().Count() != origins.Count(origin => origin >= 0)
            || origins.Any(origin => origin is < -1 or > 15)) throw new InvalidDataException("Invalid native member mapping.");
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes); var site = NativeOwnerSites().Single(site => site.Scope == 8 && SameRecord(site.Record, band));
        var result = new List<NativeOwnerScope>();
        foreach (var row in rows)
        {
            if (row.File != site.Address.File || row.Ordinal != site.Address.Ordinal || row.Member < 0) { result.Add(row); continue; }
            var member = Enumerable.Range(0, 16).FirstOrDefault(index => origins[index] == row.Member, -1);
            if (member >= 0) result.Add(row with { Member = member });
            else if (row.Present) result.Add(row with { File = -1, Ordinal = -1 });
        }
        WriteNativeOwnerScopes(result);
    }
    private void RebaseNativeMemberMove(NativeRecord band, int from, int to)
    {
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes); var site = NativeOwnerSites().Single(site => site.Scope == 8 && SameRecord(site.Record, band));
        int Move(int slot) => slot == from ? to : to < from && slot >= to && slot < from ? slot + 1 : to > from && slot > from && slot <= to ? slot - 1 : slot;
        WriteNativeOwnerScopes(rows.Select(row => row.File == site.Address.File && row.Ordinal == site.Address.Ordinal && row.Member >= 0
            ? row with { Member = Move(row.Member) } : row).ToArray());
    }
    private void CopyNativeOwnerScopes(NativeRecord source, NativeRecord destination)
    {
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes); var sites = NativeOwnerSites();
        var from = sites.FirstOrDefault(site => SameRecord(site.Record, source));
        var to = sites.FirstOrDefault(site => SameRecord(site.Record, destination)); if (from is null || to is null) return;
        foreach (var original in rows.Where(row => row.File == from.Address.File && row.Ordinal == from.Address.Ordinal).ToArray())
        {
            var site = sites.SingleOrDefault(site => site.Address.File == to.Address.File && site.Address.Ordinal == to.Address.Ordinal && site.Address.Member == original.Member && site.Importable);
            if (site is null) continue;
            var index = rows.FindIndex(row => row.File == site.Address.File && row.Ordinal == site.Address.Ordinal && row.Member == site.Address.Member);
            if (index < 0) rows.Add(new(site.Address.File, site.Address.Ordinal, site.Address.Member, site.Scope, NextNativeOwnerID(rows), site.Band, OwnerRoot(site.Record), original.Present));
        }
        WriteNativeOwnerScopes(rows);
    }
    private void RebaseNativeOwnerRewrite(NativeFile file, byte[] after, IReadOnlyList<NativeRecord> removed,
        IReadOnlyDictionary<int, int> relocated, bool preservePoints)
    {
        if (file.Signature != 0xBE) return;
        var rows = DecodeNativeOwnerScopes(NativeOwnerScopes); var originals = new List<NativeRecord>(); AppendNativeCollectionRecords(file, originals);
        var top = originals.Where(record => !record.Category.StartsWith("BandHero", StringComparison.Ordinal)).ToArray();
        var temporary = new NativeFile(file.SourcePath, file.ExportPath, after); var rewritten = new List<NativeRecord>(); AppendNativeCollectionRecords(temporary, rewritten);
        var newTop = rewritten.Where(record => !record.Category.StartsWith("BandHero", StringComparison.Ordinal)).ToArray();
        var newIndices = newTop.Select((record, index) => (record.Offset, index)).ToDictionary(item => item.Offset, item => item.index);
        var deleting = removed.Select(record => record.Offset).ToHashSet(); var fileIndex = NativeBindingFile(file); var result = new List<NativeOwnerScope>();
        var categoryChanges=new HashSet<NativeOwnerAddress>();
        foreach (var row in rows)
        {
            if (row.File != fileIndex) { result.Add(row); continue; }
            if (row.Ordinal >= top.Length) throw new InvalidDataException("Invalid working owner ordinal.");
            var original = top[row.Ordinal]; int destination;
            if (relocated.TryGetValue(original.Offset, out var moved)) destination = moved;
            else if (deleting.Contains(original.Offset))
            { if (preservePoints && row.Present) result.Add(row with { File = -1, Ordinal = -1 }); continue; }
            else destination = original.Offset - removed.Where(record => record.Offset < original.Offset).Sum(record => record.Length);
            if (!newIndices.TryGetValue(destination, out var ordinal)) throw new InvalidDataException("A rewritten owner has no verified destination.");
            var target = newTop[ordinal];
            if (row.Member >= 0) target = rewritten.Single(record => record.Offset == target.Offset + 51 + row.Member * 32 && record.Category.StartsWith("BandHero", StringComparison.Ordinal));
            var scope = row.Member >= 0 ? 16u : NativeUniqueScope(target);
            if(preservePoints && scope!=row.Scope)
            {
                if(row.Present)result.Add(row with{File=-1,Ordinal=-1});
                if(scope!=0 || target.Category=="GAMETRAP")
                {result.Add(row with{Ordinal=ordinal,Scope=scope,Band=0,Root=0,Present=false});categoryChanges.Add(new(row.File,ordinal,row.Member));}
                continue;
            }
            if (scope == 0 && !(preservePoints && target.Category=="GAMETRAP") || scope != row.Scope && OwnerRoot(target) == 0 && row.Present)
            { if (preservePoints && row.Present) result.Add(row with { File = -1, Ordinal = -1 }); continue; }
            result.Add(row with { Ordinal = ordinal, Scope = scope, Root = OwnerRoot(target) });
        }
        if(preservePoints)
            foreach(var live in result.Where(row=>row.Member==-1 && row.Scope is 64 or 128 or 256 && !row.Present
                && categoryChanges.Contains(new(row.File,row.Ordinal,row.Member))).ToArray())
            {
                // Source category migration retains the UID. Returning to a
                // former category reconnects only its unique literal point.
                var candidates=result.Where(row=>row.File<0 && row.Present && row.Scope==live.Scope && row.ID==live.ID).ToArray();
                if(candidates.Length!=1 || result.Count(row=>row.File>=0 && row.ID==live.ID)!=1)continue;
                var point=candidates[0];var target=newTop[live.Ordinal];
                OwnerTrigger(target).Encode(point.Root.ToString()).CopyTo(after,target.Offset+OwnerTrigger(target).Offset);
                var index=result.IndexOf(live);result[index]=live with{Root=point.Root,Present=true,Band=point.Band};result.Remove(point);
            }
        WriteNativeOwnerScopes(result);
    }
    private void RestoreCompiledNativeOwnerScopes(IReadOnlyList<NativeRecord> source)
    {
        _ = NativeOwnerScopes; var sites = NativeOwnerSites(); var rows = new List<NativeOwnerScope>();
        var links = source.Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)
            && UniqueOwnerScope(uint.Parse(record.Read(record.Fields.Single(field => field.Name == "node.type"))))).ToArray();
        var emitted = new HashSet<int>();
        void Bind(NativeRecord owner, uint id)
        {
            var site = sites.SingleOrDefault(site => SameRecord(site.Record, owner)); if (site is null || id == 0) return;
            var found = site.Scope==0?-1:Array.FindIndex(links, link => uint.Parse(link.Read(link.Fields.Single(field => field.Name == "node.type"))) == site.Scope
                && uint.Parse(link.Read(link.Fields.Single(field => field.Name == ScopeTarget(site.Scope)))) == id);
            if (found >= 0) emitted.Add(found);
            var band = site.Scope == 16 && found >= 0 ? uint.Parse(links[found].Read(links[found].Fields.Single(field => field.Name == "band"))) : site.Band;
            rows.Add(new(site.Address.File, site.Address.Ordinal, site.Address.Member, site.Scope, id, band, OwnerRoot(owner), found >= 0));
        }
        var things = Records().Where(record => record.File.Signature == 0xBE && PlacementExisting(record) is not null).ToArray();
        var projected = source.Where(record => record.Category == "Project.MAPTHING").OrderBy(CompilerThingOrder).ToArray();
        if (things.Length != projected.Length) throw new InvalidDataException("Compiled owner projection changed thing count.");
        for (var index = 0; index < things.Length; index++) Bind(things[index], uint.Parse(projected[index].Read(projected[index].Fields.Single(field => field.Name == "uniqueid"))));
        var bands = Records().Where(record => record.Category == "GAMEBAND").ToArray(); var projectedBands = source.Where(IsBand).ToArray();
        if (bands.Length != projectedBands.Length) throw new InvalidDataException("Compiled owner projection changed band count.");
        for (var index = 0; index < bands.Length; index++)
        {
            var band = projectedBands[index];
            Bind(bands[index], uint.Parse(band.Read(band.Fields.Single(field => field.Name == "uniqueid"))));
            for (var member = 0; member < 16; member++)
            {
                if (band.Read(band.Fields.Single(field => field.Name == $"heros[{member}].isvalid")) == "0") break;
                Bind(sites.Single(site => site.Record.File == bands[index].File && site.Record.Offset == bands[index].Offset + 51 + member * 32).Record,
                    uint.Parse(band.Read(band.Fields.Single(field => field.Name == $"heros[{member}].uniqueid"))));
            }
        }
        for (var index = 0; index < links.Length; index++) if (!emitted.Contains(index))
        {
            var link = links[index]; var scope = uint.Parse(link.Read(link.Fields.Single(field => field.Name == "node.type")));
            rows.Add(new(-1, -1, scope == 16 ? 0 : -1, scope, uint.Parse(link.Read(link.Fields.Single(field => field.Name == ScopeTarget(scope)))),
                scope == 16 ? uint.Parse(link.Read(link.Fields.Single(field => field.Name == "band"))) : 0,
                ushort.Parse(link.Read(link.Fields.Single(field => field.Name == "trigger"))), true));
        }
        WriteNativeOwnerScopes(rows);
    }
}
