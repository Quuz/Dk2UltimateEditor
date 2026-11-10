using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DK2MapEditor.Core.Editing;

public enum UnusedScriptProblem { Layout, Identity, Scope, MissingLink, Cycle, EventType, ActionCoordinates, ActionTarget, PetBand }
public sealed record UnusedScriptIssue(UnusedScriptProblem Problem, ushort EventID, string Detail);
public sealed record UnusedScriptEvent(ushort ID, bool Condition, string? TypeName);

/// <summary>Immutable read-only consent data. Neither mutable records nor the
/// deletion authority/fingerprint are exposed to callers.</summary>
public sealed class UnusedScriptPlan
{
    internal UnusedScriptPlan(NativeMapDocument owner, long revision, byte[] fingerprint,
        IReadOnlyList<UnusedScriptEvent> events, IReadOnlyList<UnusedScriptIssue> issues, int reachable)
    { Owner = owner; Revision = revision; Fingerprint = fingerprint; Events = events; Issues = issues; ReachableCount = reachable; }
    internal NativeMapDocument Owner { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    public IReadOnlyList<UnusedScriptEvent> Events { get; }
    public IReadOnlyList<UnusedScriptIssue> Issues { get; }
    public int ReachableCount { get; }
    public int Conditions => Events.Count(item => item.Condition);
    public int Actions => Events.Count - Conditions;
    public bool HasWork => Events.Count != 0;
    public bool CanApply => Issues.Count == 0;
}

public sealed partial class NativeMapDocument
{
    /// <summary>Ordered transaction buffers, including editor metadata and path identities; no buffers are exposed.</summary>
    public string ReadEditingFingerprint() => Convert.ToHexString(UnusedScriptFingerprint()).ToLowerInvariant();

    private byte[] UnusedScriptFingerprint()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[4];
        foreach (var file in TransactionFiles())
        {
            foreach (var path in new[] { file.SourcePath, file.ExportPath })
            {
                var encoded = Encoding.UTF8.GetBytes(path); BinaryPrimitives.WriteInt32LittleEndian(length, encoded.Length);
                hash.AppendData(length); hash.AppendData(encoded);
            }
            BinaryPrimitives.WriteInt32LittleEndian(length, file.Length); hash.AppendData(length); hash.AppendData(file.Data.Span);
        }
        return hash.GetHashAndReset();
    }
    private static bool ScriptCondition(NativeRecord record) => record.Category == "Project.TRIGGER"
        || record.File.Signature == 0xD2 && record.File.UInt32(record.Offset) == 0xD5;

    /// <summary>Starts at every stored root, NOT only successfully resolved
    /// physical owners. Shared DAGs are valid; retained cycles/missing edges are
    /// blockers. Unlinked malformed components are never silently repaired.</summary>
    public UnusedScriptPlan PlanUnusedScriptRemoval()
    {
        if (transactionOpen) throw new InvalidOperationException("Finish the current edit before previewing unused events.");
        var revision = EditRevision; var fingerprint = UnusedScriptFingerprint();
        var records = Records(); var issues = new List<UnusedScriptIssue>();
        void Issue(UnusedScriptProblem problem, ushort id, string detail) => issues.Add(new(problem, id, detail));
        UnusedScriptPlan Result(IReadOnlyList<UnusedScriptEvent> unused, int reachable) => new(this, revision, fingerprint,
            Array.AsReadOnly(unused.ToArray()), issues.AsReadOnly(), reachable);
        foreach (var record in records)
        {
            if (record.Category == "Project.Opaque" && record.File.Signature is LegacyEvents or LegacyInfo or LegacyPlayers or LegacyThings or LegacyAreas or LegacyBands
                || !IsLegacyProject && (record.File.Signature == 0xD2 && !ScriptGraph.IsEvent(record)
                    || record.File.Signature == 0xBE && (record.Category.StartsWith("Record ", StringComparison.Ordinal) || record.Category == "GAMEBAND" && record.Length != 563)))
                Issue(UnusedScriptProblem.Layout, 0, record.Category);
            if (!IsLegacyProject)
            {
                var structure = record.Category == "Map" ? "GAMEINFO" : record.Category.StartsWith("BandHero", StringComparison.Ordinal) ? "GAMEBANDHERO" : record.Category;
                if (structure.StartsWith("GAME", StringComparison.Ordinal))
                    foreach (var field in LegacySchemas.Fields(structure).Where(field => field.Name.EndsWith("Trigger", StringComparison.Ordinal)))
                        if (!record.Fields.Any(item => item.Name == field.Name && item.Offset == field.Offset && item.Size == field.Size))
                            Issue(UnusedScriptProblem.Layout, 0, structure + "." + field.Name);
            }
        }
        if (issues.Count != 0) return Result([], 0);
        var graph = ScriptGraph.Build(this);
        foreach (var id in graph.DuplicateIDs) Issue(UnusedScriptProblem.Identity, id, "zero/duplicate event ID");
        foreach (var item in graph.Events.Values)
            if (IsLegacyProject && (ScriptCondition(item.Record) ? item.ID >= 32768 : item.ID < 32768))
                Issue(UnusedScriptProblem.Identity, item.ID, "wrong signed event category");
        foreach (var file in files.Where(file => file.Signature == 0xD2))
        {
            var entries = graph.Events.Values.Where(item => item.Record.File == file).ToArray();
            if (file.UInt32(4) != 4 || file.UInt32(20) != entries.Count(item => ScriptCondition(item.Record)) || file.UInt32(24) != entries.Count(item => !ScriptCondition(item.Record)))
                Issue(UnusedScriptProblem.Layout, 0, "native condition/action counts");
        }
        if (issues.Count != 0) return Result([], 0);
        foreach (var root in graph.Roots)
            if (!ScriptGraph.IsKnownScope(root.Scope)) Issue(UnusedScriptProblem.Scope, 0, root.Scope.ToString(CultureInfo.InvariantCulture));

        var colors = new Dictionary<ushort, byte>(); var pending = new Stack<(ushort ID, int Next)>();
        foreach (var root in graph.Roots.Where(root => root.Target != 0).Select(root => root.Target).Distinct())
        {
            if (!graph.Events.ContainsKey(root)) { Issue(UnusedScriptProblem.MissingLink, root, "root"); continue; }
            if (colors.GetValueOrDefault(root) != 0) continue;
            colors[root] = 1; pending.Push((root, 0));
            while (pending.TryPop(out var frame))
            {
                var item = graph.Events[frame.ID];
                if (frame.Next == item.Edges.Count) { colors[frame.ID] = 2; continue; }
                pending.Push((frame.ID, frame.Next + 1)); var edge = item.Edges[frame.Next];
                if (edge.Target == 0) continue;
                if (!graph.Events.ContainsKey(edge.Target)) { Issue(UnusedScriptProblem.MissingLink, item.ID, edge.Field.Name + "=" + edge.Target); continue; }
                var color = colors.GetValueOrDefault(edge.Target);
                if (color == 1) Issue(UnusedScriptProblem.Cycle, item.ID, edge.Field.Name + "=" + edge.Target);
                else if (color == 0) { colors[edge.Target] = 1; pending.Push((edge.Target, 0)); }
            }
        }
        foreach (var id in colors.Keys)
        {
            var item = graph.Events[id];
            if (item.Type is null) Issue(UnusedScriptProblem.EventType, id, "unverified reachable event type");
        }
        // Index physical targets once. Re-scanning all records for each action
        // would turn a bounded graph walk into quadratic work on large maps.
        var targets = records.Select(EntityReferences.Identity).OfType<EntityIdentity>()
            .GroupBy(identity => (identity.Kind, ID: long.Parse(identity.Record.Read(identity.Field), CultureInfo.InvariantCulture)))
            .ToDictionary(group => group.Key, group => group.Count());
        long Value(NativeRecord record, string name) => record.Fields.FirstOrDefault(field => field.Name == name) is { } field
            ? long.Parse(record.Read(field), CultureInfo.InvariantCulture) : 0;
        var crystal = records.Any(record => IsLegacyProject ? record.Category.StartsWith("Project.MAPTHING", StringComparison.Ordinal) && Value(record, "node.type") == 3 && Value(record, "id") == 28
            : record.Category == "GAMEOBJECT" && Value(record, "bObjectID") == 28);
        var info = records.FirstOrDefault(record => record.Category.StartsWith("Project.MAPPROPERTIES", StringComparison.Ordinal) || record.Category == "Map");
        var pets = info is not null && (Value(info, IsLegacyProject ? "Options2" : "bMainOptions2") & 0x20) != 0;
        foreach (var id in colors.Keys)
            if (graph.Events[id] is { Type: not null } item && !ScriptCondition(item.Record)) CheckUnusedScriptAction(item, targets, crystal, pets, issues);
        var candidates = graph.Events.Values.Where(item => !colors.ContainsKey(item.ID))
            .OrderBy(item => files.IndexOf(item.Record.File)).ThenBy(item => item.Record.Offset)
            .Select(item => new UnusedScriptEvent(item.ID, ScriptCondition(item.Record), item.Type?.Name)).ToArray();
        return Result(candidates, colors.Count);
    }

    private void CheckUnusedScriptAction(ScriptEvent item, IReadOnlyDictionary<(EntityKind Kind, long ID), int> targets,
        bool crystal, bool pets, List<UnusedScriptIssue> issues)
    {
        long Value(NativeRecord record, string name) => record.Fields.FirstOrDefault(field => field.Name == name) is { } field
            ? long.Parse(record.Read(field), CultureInfo.InvariantCulture) : 0;
        long Param(int number) => Value(item.Record, (IsLegacyProject ? "param" : "Param") + number);
        void Issue(UnusedScriptProblem problem, string detail) => issues.Add(new(problem, item.ID, detail));
        bool Target(EntityKind kind, long id) => targets.GetValueOrDefault((kind, id)) == 1 && (id != 0 || kind == EntityKind.Band);
        void Area(int parameter) { if (!Target(EntityKind.Area, Param(parameter))) Issue(UnusedScriptProblem.ActionTarget, "area=" + Param(parameter)); }
        switch (item.Type!.Name)
        {
            case "ACTION_ADDCREATURE": case "ACTION_CHANGESQUARE": case "ACTION_CHANGEOWNER":
                // TaskTestAction checks upper bounds only. Source resize
                // shifts retain negative LONG targets; the writer uses WORD casts.
                if (Param(5) >= Width || Param(7) >= Height) Issue(UnusedScriptProblem.ActionCoordinates, "outside map");
                break;
            case "ACTION_SHOWAREA": case "ACTION_ENABLEFOG": case "ACTION_MOVECAMERA": case "ACTION_ROTATECAMERA": Area(1); break;
            case "ACTION_CAMERAPATH": Area(2); break;
            case "ACTION_SHOWCRYSTAL":
                if (!crystal) Issue(UnusedScriptProblem.ActionTarget, "crystal object=28");
                break;
            case "ACTION_RELEASEBAND":
                if (!Target(EntityKind.Band, Param(1))) Issue(UnusedScriptProblem.ActionTarget, "band=" + Param(1));
                Area(5);
                if (Param(2) != 0 && !pets) Issue(UnusedScriptProblem.PetBand, "enable source pet-band option");
                break;
        }
    }

    /// <summary>Read-only lifetime check for both Keep and Remove decisions.
    /// Undo cannot make an old preview current again.</summary>
    public bool IsUnusedScriptPlanCurrent(UnusedScriptPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return !transactionOpen && plan.Owner == this && plan.Revision == EditRevision
            && UnusedScriptFingerprint().AsSpan().SequenceEqual(plan.Fingerprint);
    }

    /// <summary>Explicit confirmed operation, one exact Undo/Redo. No owner,
    /// root, retained edge/backlink or opaque tail is repaired/reassigned.</summary>
    public void ApplyUnusedScriptRemoval(UnusedScriptPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (!IsUnusedScriptPlanCurrent(plan))
            throw new InvalidOperationException("Refresh the unused-event preview after document changes.");
        var current = PlanUnusedScriptRemoval();
        if (!plan.CanApply || !current.CanApply || !plan.Events.SequenceEqual(current.Events))
            throw new InvalidOperationException("The unused-event preview has unresolved graph or layout problems.");
        if (!current.HasWork) return;
        var graph = ScriptGraph.Build(this); var remove = current.Events.Select(item => graph.Events[item.ID].Record).ToArray();
        Edit("Remove unused scenario conditions and actions", () =>
        {
            foreach (var group in remove.GroupBy(record => record.File))
            {
                var file = group.Key; var ordered = group.OrderBy(record => record.Offset).ToArray();
                var byteCount = ordered.Sum(record => record.Length); var conditions = ordered.Count(ScriptCondition); var actions = ordered.Length - conditions;
                var bytes = new byte[file.Length - byteCount]; var source = 0; var destination = 0;
                foreach (var record in ordered)
                {
                    var length = record.Offset - source;
                    file.Bytes.AsSpan(source, length).CopyTo(bytes.AsSpan(destination)); destination += length; source = record.Offset + record.Length;
                }
                file.Bytes.AsSpan(source).CopyTo(bytes.AsSpan(destination));
                if (IsLegacyProject)
                {
                    WriteUInt32(bytes, 8, (uint)bytes.Length - 12);
                    WriteUInt32(bytes, 16, checked(file.UInt32(16) - (uint)conditions)); WriteUInt32(bytes, 20, checked(file.UInt32(20) - (uint)actions));
                }
                else
                {
                    WriteUInt32(bytes, 8, AdjustedNativeStoredSize(file.UInt32(8), -byteCount, bytes.Length));
                    WriteUInt32(bytes, 20, checked(file.UInt32(20) - (uint)conditions)); WriteUInt32(bytes, 24, checked(file.UInt32(24) - (uint)actions));
                    var remainingBytes = graph.Events.Values.Where(item => item.Record.File == file).Sum(item => item.Record.Length) - byteCount;
                    WriteUInt32(bytes, 56, AdjustedNativeStoredSize(file.UInt32(56), -byteCount, remainingBytes));
                }
                RebaseNativeActionDeletion(ordered);
                file.Bytes = bytes;
            }
        });
    }
}
