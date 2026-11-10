using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>All editable MAPTHING manager-template properties. Position and fresh
/// identity are supplied by insertion, never copied from an existing entity.</summary>
public sealed record ObjectBatchTemplate(PlacedThingKind Kind, uint TypeID, byte Owner = 0,
    uint Health = 0, uint GoldHeld = 0, int DataAll = 0, uint HeroOptions = 0,
    uint CreatureOptions = 0, uint Objective = 0, uint Area = 0, uint Enemy = 0)
{
    public static ObjectBatchTemplate Default(PlacedThingKind kind) => kind switch
    {
        PlacedThingKind.Creature => new(kind, 1, Health: 100, DataAll: 1, CreatureOptions: 9, Objective: 19),
        PlacedThingKind.Door => new(kind, 6), PlacedThingKind.Trap => new(kind, 4, DataAll: 4),
        PlacedThingKind.Object => new(kind, 1, 2), PlacedThingKind.SpecialItem => new(kind, 0x75, 2),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}

public sealed record ObjectBatchInsertionPlan(ObjectBatchTemplate Template,
    ObjectBatchPlacementPlan Placement, bool UpgradeHistoricalThings);

public sealed partial class NativeMapDocument
{
    public ObjectBatchInsertionPlan PlanObjectBatchInsertion(ObjectBatchTemplate template,
        ObjectBatchPlacementOptions options, bool upgradeHistoricalThings = false, Func<int, int>? randomSample = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        var placement = PlanObjectBatchPlacement(new(template.Kind, template.TypeID, template.Owner), options, randomSample);
        return new(template, placement, upgradeHistoricalThings);
    }

    /// <summary>Apply the sampled preview once, in source row order. All bytes
    /// are prepared before committing one transaction; no random resampling.</summary>
    public int ApplyObjectBatchInsertion(ObjectBatchInsertionPlan plan, IEnumerable<int>? selectedCandidateIndices = null)
        => ApplyObjectBatchInsertionCore(plan, selectedCandidateIndices, null, "Batch add objects");

    private int ApplyObjectBatchInsertionCore(ObjectBatchInsertionPlan plan, IEnumerable<int>? selectedCandidateIndices,
        IReadOnlyList<NativeRecord>? removalOverride, string description,bool editorCreation=false)
    {
        ArgumentNullException.ThrowIfNull(plan); ArgumentNullException.ThrowIfNull(plan.Template);
        if (plan.Placement.Template != new ObjectPlacementTemplate(plan.Template.Kind, plan.Template.TypeID, plan.Template.Owner))
            throw new ArgumentException("The full template does not match its placement preview.");
        var selected = ResolveObjectBatchPlacement(plan.Placement, selectedCandidateIndices);
        if (selected.Count == 0) return 0; // No upgrade, notifications or redo consumption.
        var records = Records();
        var removed = removalOverride?.ToArray() ?? selected.Where(candidate => candidate.Replacement is not null).Select(candidate => candidate.Replacement!.Record)
            .DistinctBy(record => (record.File, record.Offset)).ToArray();
        var prepared = new Dictionary<NativeFile, byte[]>();
        if (IsLegacyProject)
        {
            // Reserve all known identities, including orphan scenario scopes. A
            // historical nested/unknown section needs the existing explicit upgrade.
            if (records.Any(record => record.Category == "Project.Opaque" && record.File.Signature is LegacyThings or LegacyBands or LegacyEvents))
                throw new InvalidDataException("Upgrade or repair historical/unknown thing, band and scenario sections before batch insertion.");
            var file = projectSections.Single(section => section.Signature == LegacyThings);
            var version = checked((int)file.UInt32(4));
            if (version != 6 && !plan.UpgradeHistoricalThings)
                throw new InvalidOperationException("Confirm upgrading the historical Things section to retain every template property.");
            var oldThings = records.Where(record => record.File == file).ToArray();
            var upgraded = version == 6 ? file.Bytes : UpgradeThings(file, version);
            var stride = LegacyProjectSchemas.Size("MAPTHING");
            var removeOffsets = removed.Select(record => record.Offset).ToHashSet();
            var ranges = oldThings.Select((record, index) => (record, index)).Where(item => removeOffsets.Contains(item.record.Offset))
                .Select(item => (Offset: 12 + item.index * stride, Length: stride)).ToArray();
            var thingsLength = (long)upgraded.Length - ranges.Length * (long)stride + selected.Count * (long)stride;
            if (thingsLength > MaximumFileBytes) throw new InvalidDataException("Batch insertion exceeds the Things section size limit.");
            var links = records.Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)).ToArray();
            var obsolete = new HashSet<NativeRecord>();
            var removedKeys = removed.Select(record => (record.File, record.Offset)).ToHashSet();
            foreach (var old in editorCreation ? [] : removed)
            {
                foreach (var scope in ProjectScopes(old))
                {
                    var identity = PlacementNumber(old, scope.IdentityField);
                    if (identity == 0) continue;
                    var owned = links.Where(link => PlacementNumber(link, "node.type") == scope.Scope && PlacementNumber(link, scope.Target) == identity).ToArray();
                    if (owned.Length == 0) continue;
                    if (oldThings.Any(other => !removedKeys.Contains((other.File, other.Offset)) && ProjectScopes(other)
                        .Any(otherScope => otherScope.Scope == scope.Scope && PlacementNumber(other, otherScope.IdentityField) == identity)))
                        throw new InvalidDataException("Replacement has an ambiguous shared project identity; repair it before removing its scenario binding.");
                    foreach (var link in owned) obsolete.Add(link);
                }
            }
            foreach (var group in obsolete.GroupBy(record => record.File))
            {
                var bytes = BatchSplice(group.Key.Bytes, group.Select(record => (record.Offset, record.Length)).ToArray(), group.Key.Length, []);
                WriteUInt32(bytes, 12, checked(group.Key.UInt32(12) - (uint)group.Count()));
                WriteUInt32(bytes, 8, (uint)bytes.Length - 12); prepared[group.Key] = bytes;
            }
            // Include optional sections only when they would actually be saved.
            var length = 12L + projectSections.Where(section => !optionalProjectSections.Contains(section)
                || section == file || prepared.ContainsKey(section) || !section.Bytes.AsSpan().SequenceEqual(originals[section]))
                .Sum(section => section == file ? thingsLength : prepared.TryGetValue(section, out var data) ? data.LongLength : section.Length);
            var mapSection = projectSections.Single(section => section.Signature == Formats.LegacySectionFile.TileSectionSignature);
            length += 20L + Width * (long)Height * 4 - mapSection.Length;
            if (length > MaximumFileBytes) throw new InvalidDataException("Batch insertion exceeds the project size limit.");
            var used = UsedProjectUniqueIDs(); uint next = 1;
            var additions = new List<byte[]>(selected.Count);
            foreach (var candidate in selected)
            {
                if(!editorCreation){while(used.Contains(next))next=checked(next+1);used.Add(next);}
                additions.Add(BatchProjectBytes(plan.Template,candidate,editorCreation?0:next));
            }
            prepared[file] = BatchSplice(upgraded, ranges, upgraded.Length, additions);
            WriteUInt32(prepared[file], 8, (uint)prepared[file].Length - 12);
        }
        else
        {
            var file = CollectionFile(0xBE);
            var totalCount = (long)file.UInt32(20) + selected.Count - removed.Count(record => record.File == file);
            if (totalCount > 1_000_000) throw new InvalidDataException("Batch insertion exceeds the native record-count limit.");
            var additions = selected.Select(candidate => BatchNativeBytes(plan.Template, candidate)).ToArray();
            foreach (var target in removed.Select(record => record.File).Append(file).Distinct())
            {
                var deleting = removed.Where(record => record.File == target).ToArray();
                var adding = target == file ? additions : [];
                var byteDelta = checked(adding.Sum(data => data.Length) - deleting.Sum(record => record.Length));
                var count = (long)target.UInt32(20) + adding.Length - deleting.Length;
                if (count is < 0 or > 1_000_000) throw new InvalidDataException("Batch insertion exceeds the native record-count limit.");
                var end = NativeCollectionDataEnd(target);
                var bytes = BatchSplice(target.Bytes, deleting.Select(record => (record.Offset, record.Length)).ToArray(), end, adding);
                WriteUInt32(bytes, 8, AdjustedNativeStoredSize(target.UInt32(8), byteDelta, bytes.Length));
                WriteUInt32(bytes, 52, AdjustedNativeStoredSize(target.UInt32(52), byteDelta, end - 56 + byteDelta));
                WriteUInt32(bytes, 20, (uint)count); prepared[target] = bytes;
            }
        }
        // A caller's enumerable can run code; the resolver checks before it is
        // enumerated, so recheck immediately before committing prepared bytes.
        ResolveObjectBatchPlacement(plan.Placement, selected.Select(candidate => candidate.Index));
        Edit(description, () =>
        {
            if (!IsLegacyProject) RebaseNativeOwnerRemovals(removed, preservePoints: editorCreation);
            foreach (var (file, bytes) in prepared) file.Bytes = bytes;
        });
        return selected.Count;
    }

    private static byte[] BatchSplice(byte[] source, IReadOnlyList<(int Offset, int Length)> removals, int insertionOffset,
        IReadOnlyList<byte[]> additions)
    {
        if (insertionOffset < 0 || insertionOffset > source.Length) throw new InvalidDataException("Invalid batch insertion boundary.");
        var sorted = removals.OrderBy(range => range.Offset).ToArray(); var previous = 0;
        foreach (var range in sorted)
        {
            if (range.Length <= 0 || range.Offset < previous || range.Offset > insertionOffset - range.Length)
                throw new InvalidDataException("Invalid overlapping batch replacement ranges.");
            previous = range.Offset + range.Length;
        }
        var length = (long)source.Length - sorted.Sum(range => (long)range.Length) + additions.Sum(data => (long)data.Length);
        if (length > MaximumFileBytes) throw new InvalidDataException("Batch insertion exceeds the safe file size limit.");
        var result = new byte[checked((int)length)]; int read = 0, write = 0;
        foreach (var range in sorted)
        {
            source.AsSpan(read, range.Offset - read).CopyTo(result.AsSpan(write)); write += range.Offset - read; read = range.Offset + range.Length;
        }
        source.AsSpan(read, insertionOffset - read).CopyTo(result.AsSpan(write)); write += insertionOffset - read;
        foreach (var data in additions) { data.CopyTo(result, write); write += data.Length; }
        source.AsSpan(insertionOffset).CopyTo(result.AsSpan(write)); return result;
    }

    private static byte[] BatchProjectBytes(ObjectBatchTemplate template, ObjectBatchPlacementCandidate candidate, uint identity)
    {
        var bytes = new byte[LegacyProjectSchemas.Size("MAPTHING")];
        var fields = LegacyProjectSchemas.Fields("MAPTHING");
        void Set(string name, long value)
        {
            var field = fields.Single(field => field.Name == name);
            EncodeCompilerValue(field, value.ToString(CultureInfo.InvariantCulture)).CopyTo(bytes, field.Offset);
        }
        Set("node.type", (uint)template.Kind); Set("x", candidate.Position.X); Set("y", candidate.Position.Y);
        Set("id", template.TypeID); Set("owner", candidate.Owner); Set("health", template.Health); Set("goldheld", template.GoldHeld);
        Set("dataAll", template.DataAll); Set("herosoptions", template.HeroOptions); Set("creaturesoptions", template.CreatureOptions);
        Set("objective", template.Objective); Set("area", template.Area); Set("uniqueid", identity); Set("ennemy", template.Enemy);
        return bytes;
    }

    private static byte[] BatchNativeBytes(ObjectBatchTemplate template, ObjectBatchPlacementCandidate candidate)
    {
        var structure = template.Kind switch
        {
            PlacedThingKind.Creature => candidate.Owner == 1 ? "GAMEHERO" : candidate.Owner == 2 ? "GAMENEUTRAL" : "GAMEKEEPER",
            PlacedThingKind.Door => "GAMEDOOR", PlacedThingKind.Trap => "GAMETRAP", _ => "GAMEOBJECT"
        };
        var layout = RecordTemplate.All.Single(item => item.Structure == structure);
        var bytes = layout.Create(candidate.Position, candidate.Owner, 0);
        foreach (var field in LegacySchemas.Fields(structure))
        {
            long? value = field.Name switch
            {
                "bObjectID" or "bDoorID" or "bTrapID" or "bGlobalCreatureID" => template.TypeID,
                "bGlobalGoldHeld" => template.GoldHeld, "bGlobalLevel" or "bDoorLocked" or "bTrapShots" => template.DataAll,
                "bGlobalBehavior" => template.CreatureOptions, "dwGlobalHealth" => template.Health,
                "bHeroTargetArea" => template.Area, "bHeroObjective" => template.Objective, "bHeroOptions" => template.HeroOptions,
                "bGlobalPlayer" when structure == "GAMEHERO" => template.Enemy,
                "dwObjectGoldValue" => template.Kind == PlacedThingKind.Object && template.TypeID is 1 or 2 or 3 ? template.DataAll : 0,
                "dwObjectSpell" => template.Kind == PlacedThingKind.Object && template.TypeID == 4 ? template.DataAll : 0,
                _ => null
            };
            if (value is not null) EncodeCompilerValue(field, value.Value.ToString(CultureInfo.InvariantCulture)).CopyTo(bytes, field.Offset);
        }
        return bytes;
    }
}
