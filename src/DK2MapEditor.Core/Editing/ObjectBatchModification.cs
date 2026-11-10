using System.Globalization;

namespace DK2MapEditor.Core.Editing;

[Flags]
public enum ObjectModificationMask
{
    None = 0, Coordinates = 0x100, Type = 0x200, Owner = 0x400, Level = 0x800,
    GoldHeld = 0x1000, Health = 0x2000, CreatureOptions = 0x4000,
    HeroOptions = 0x8000, Objective = 0x10000, Data = 0x100000,
    All = Coordinates | Type | Owner | Level | GoldHeld | Health | CreatureOptions | HeroOptions | Objective | Data
}
public enum ObjectModificationCoordinates { Same, RelativeMinimum }
public enum ObjectModificationBits { All, Mask }
public enum ObjectModificationDecision { Keep, Remove, Modify }

public sealed record ObjectBatchModificationOptions(ObjectModificationMask Mask,
    TilePoint Position = default, ObjectModificationCoordinates Coordinates = ObjectModificationCoordinates.RelativeMinimum,
    ObjectModificationBits CreatureBits = ObjectModificationBits.All, uint CreatureMask = uint.MaxValue,
    ObjectModificationBits HeroBits = ObjectModificationBits.All, uint HeroMask = uint.MaxValue,
    ObjectReplacementPolicy Replacement = ObjectReplacementPolicy.Ask,
    ObjectBatchFlags Flags = ObjectBatchFlags.AskOptions | ObjectBatchFlags.Preview | ObjectBatchFlags.IgnoreAsk | ObjectBatchFlags.IgnoreIncompatible,
    bool IncludeScripted = false, bool UpgradeHistoricalThings = false);

public sealed record ObjectBatchModificationCandidate(int Index, ObjectManagerEntry Source,
    ObjectBatchTemplate Template, TilePoint Position, ObjectBatchWarning Warnings, bool Locked,
    ObjectModificationDecision Decision, ObjectManagerEntry? Replacement, bool ChangesScriptScope, bool DetachesScript);

/// <summary>Immutable proposals and explicit Keep/Remove/Modify choices. Reviewing a changed
/// choice re-evaluates collisions; newly exposed replacement requires another explicit choice.</summary>
public sealed class ObjectBatchModificationPlan
{
    internal ObjectBatchModificationPlan(NativeMapDocument owner, byte[] fingerprint, ObjectBatchTemplate template,
        ObjectBatchModificationOptions options, IReadOnlyList<ObjectBatchModificationCandidate> candidates, bool multi)
    { Owner = owner; Fingerprint = fingerprint; Template = template; Options = options; Candidates = candidates; Multiple = multi; }
    internal NativeMapDocument Owner { get; }
    internal byte[] Fingerprint { get; }
    public ObjectBatchTemplate Template { get; }
    public ObjectBatchModificationOptions Options { get; }
    public IReadOnlyList<ObjectBatchModificationCandidate> Candidates { get; }
    public bool Multiple { get; }
}

/// <summary>Pure source pass-5 transformation. Native packing happens only at application,
/// so source DWORD masks and union conversions are not prematurely truncated to bytes.</summary>
public static class ObjectModificationRules
{
    public static int Family(PlacedThingKind kind) => kind switch
    { PlacedThingKind.Creature => 0, PlacedThingKind.Door or PlacedThingKind.Trap => 1, PlacedThingKind.Object or PlacedThingKind.SpecialItem => 2, _ => throw new ArgumentOutOfRangeException(nameof(kind)) };

    public static (ObjectBatchTemplate Template, ObjectBatchWarning Warnings) Transform(ObjectBatchTemplate old,
        ObjectBatchTemplate template, ObjectBatchModificationOptions options, byte automaticOwner, bool multiple = true)
    {
        ArgumentNullException.ThrowIfNull(old); ArgumentNullException.ThrowIfNull(template); ArgumentNullException.ThrowIfNull(options);
        if (Family(old.Kind) != Family(template.Kind) || old.Owner > 7 || template.Owner > 7 || automaticOwner > 7
            || (options.Mask & ~ObjectModificationMask.All) != 0 || !Enum.IsDefined(options.CreatureBits) || !Enum.IsDefined(options.HeroBits))
            throw new ArgumentException("Unknown or incompatible modification template/masks.");
        bool Has(ObjectModificationMask mask) => options.Mask.HasFlag(mask);
        static uint Bits(uint old, uint value, ObjectModificationBits mode, uint mask) => mode == ObjectModificationBits.All ? value : (old & ~mask) | (value & mask);
        var result = old with
        {
            Kind = Has(ObjectModificationMask.Type) ? template.Kind : old.Kind,
            TypeID = Has(ObjectModificationMask.Type) ? template.TypeID : old.TypeID,
            Owner = Has(ObjectModificationMask.Owner) ? template.Owner == 0 ? automaticOwner : template.Owner : old.Owner
        };
        var warnings = ObjectBatchWarning.None;
        if (Family(old.Kind) == 0)
        {
            result = result with
            {
                DataAll = Has(ObjectModificationMask.Level) ? template.DataAll : old.DataAll,
                GoldHeld = Has(ObjectModificationMask.GoldHeld) ? template.GoldHeld : old.GoldHeld,
                Health = Has(ObjectModificationMask.Health) ? template.Health : old.Health,
                CreatureOptions = Has(ObjectModificationMask.CreatureOptions) ? Bits(old.CreatureOptions, template.CreatureOptions, options.CreatureBits, options.CreatureMask) : old.CreatureOptions,
                HeroOptions = Has(ObjectModificationMask.HeroOptions) ? Bits(old.HeroOptions, template.HeroOptions, options.HeroBits, options.HeroMask) : old.HeroOptions,
                Objective = Has(ObjectModificationMask.Objective) ? template.Objective : old.Objective,
                Area = Has(ObjectModificationMask.Objective) ? template.Area : old.Area,
                Enemy = Has(ObjectModificationMask.Objective) ? template.Enemy : old.Enemy
            };
            if (multiple && result.Owner != 1 && (result.HeroOptions != 0 || result.Objective != 0))
            { result = result with { HeroOptions = 0, Objective = 0, Area = 0, Enemy = 0 }; warnings |= ObjectBatchWarning.Incompatible; }
            if (multiple && result.Owner == 1 && result.Objective == 0)
            { result = result with { Objective = 19 }; warnings |= ObjectBatchWarning.Incompatible; }
        }
        else
        {
            result = result with { DataAll = Has(ObjectModificationMask.Data) ? template.DataAll : old.DataAll };
            if (!multiple) return (result, warnings);
            var data = result.DataAll;
            if (result.Kind == PlacedThingKind.Door)
            {
                if (data == 4) data = 0;
                if (result.TypeID is 6 or 7 && data is not (0 or 2)) { data = 0; warnings |= ObjectBatchWarning.Incompatible; }
            }
            else if (result.Kind == PlacedThingKind.Trap)
            {
                if (data == 2) data = 0;
                if (data == 1) { data = 4; warnings |= ObjectBatchWarning.Incompatible; }
            }
            else if (result.Kind == PlacedThingKind.Object && result.TypeID == 4)
            {
                if (!LegacyCompilerRules.Elements("Spells").Any(spell => spell.ID == unchecked((uint)data)))
                { data = 0; warnings |= ObjectBatchWarning.Incompatible; }
            }
            else if ((result.Kind == PlacedThingKind.SpecialItem || result.TypeID is not (1 or 2 or 3)) && data != 0)
            { data = 0; warnings |= ObjectBatchWarning.Incompatible; }
            result = result with { DataAll = data };
        }
        return (result, warnings);
    }
}

public sealed partial class NativeMapDocument
{
    public ObjectBatchTemplate ObjectTemplateForModification(NativeRecord record)
    {
        var selection = PlanObjectRemoval([record]);
        var result = ModificationTemplate(selection.Entries.Single());
        if (!selection.Fingerprint.AsSpan().SequenceEqual(GateGenerationFingerprint()))
            throw new InvalidOperationException("The document changed while reading the object template.");
        return result;
    }
    private static uint ModificationScope(PlacedThingKind kind) => kind switch
    { PlacedThingKind.Creature => 2, PlacedThingKind.Door => 64, PlacedThingKind.Object => 128, PlacedThingKind.SpecialItem => 256, _ => 0 };

    private ObjectBatchTemplate ModificationTemplate(ObjectManagerEntry entry)
    {
        var r = entry.Record; uint N(string field, uint fallback = 0) => PlacementNumber(r, field, fallback);
        if (IsLegacyProject)
        {
            var version = r.File.UInt32(4);
            return new(entry.Thing.Kind, entry.Thing.TypeID, entry.Thing.Owner, N("health"), N("goldheld"),
                version <= 4 && entry.Thing.Kind == PlacedThingKind.Trap ? 4 : entry.DataAll,
                N(version <= 3 ? "options" : "herosoptions"), N("creaturesoptions", version <= 3 ? 9u : 0),
                version <= 3 ? entry.Thing.Owner == 1 ? 19u : 0u : N("objective"), N("area"), N("ennemy"));
        }
        return new(entry.Thing.Kind, entry.Thing.TypeID, entry.Thing.Owner, N("dwGlobalHealth"), N("bGlobalGoldHeld"), entry.DataAll,
            N("bHeroOptions"), N("bGlobalBehavior"), N("bHeroObjective"), N("bHeroTargetArea"), r.Category == "GAMEHERO" ? N("bGlobalPlayer") : 0);
    }

    public ObjectBatchModificationPlan PlanObjectBatchModification(IEnumerable<NativeRecord> selected, ObjectBatchTemplate template,
        ObjectBatchModificationOptions options)
        => PlanObjectBatchModificationCore(selected,template,options,null);

    public ObjectBatchModificationPlan PlanEditorObjectBatchModification(EditorThingRequest request,ObjectBatchTemplate template,ObjectBatchModificationOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(template);ArgumentNullException.ThrowIfNull(options);
        if(request.Command!=EditorThingCommand.Modify || !request.IsAccepted || !request.IsClosed || !IsThingRequestCurrent(request) || request.ResultSelection.Count==0)
            throw new InvalidOperationException("Resolve the current source modification request before preparing its values.");
        return PlanObjectBatchModificationCore(request.ResultSelection,template,options with{IncludeScripted=true},request.Count>1);
    }
    private ObjectBatchModificationPlan PlanObjectBatchModificationCore(IEnumerable<NativeRecord> selected,ObjectBatchTemplate template,
        ObjectBatchModificationOptions options,bool? sourceMultiple)
    {
        ArgumentNullException.ThrowIfNull(template); ArgumentNullException.ThrowIfNull(options);
        if (!Enum.IsDefined(options.Coordinates) || !Enum.IsDefined(options.Replacement) || (options.Flags & ~(ObjectBatchFlags)127) != 0)
            throw new ArgumentException("Unknown modification options.");
        var selection = PlanObjectRemoval(selected); var multi = sourceMultiple ?? selection.Entries.Count > 1;
        if (selection.Entries.Any(entry => ObjectModificationRules.Family(entry.Thing.Kind) != ObjectModificationRules.Family(template.Kind)))
            throw new ArgumentException("Select one original manager family for modification.");
        var entries = selection.Entries.Where(entry => options.IncludeScripted || !entry.Scripted).ToArray();
        var minX = entries.Length == 0 ? 0 : entries.Min(entry => entry.Thing.Position.X);
        var minY = entries.Length == 0 ? 0 : entries.Min(entry => entry.Thing.Position.Y);
        var candidates = new List<ObjectBatchModificationCandidate>(); var records = Records();
        var creatureFlags = new Dictionary<uint, (bool? Water, bool? Lava)>();
        foreach (var entry in entries)
        {
            var position = entry.Thing.Position;
            if (options.Mask.HasFlag(ObjectModificationMask.Coordinates))
                position = options.Coordinates == ObjectModificationCoordinates.Same && (ObjectModificationRules.Family(template.Kind) == 0 || !multi)
                    ? options.Position : new(checked((int)((long)position.X + options.Position.X - minX)), checked((int)((long)position.Y + options.Position.Y - minY)));
            var outside = position.X < 0 || position.Y < 0 || position.X >= Width - 1 || position.Y >= Height - 1;
            var tile = position.X < 0 || position.Y < 0 || position.X >= Width || position.Y >= Height ? new MapTile(0xFE, 2, 1)
                : ThingPlacementRules.EditorTile(GetTile(position.X, position.Y), IsEditorProtectedCell(position.X, position.Y));
            var transformed = ObjectModificationRules.Transform(ModificationTemplate(entry), template, options, tile.Owner, multi);
            var result = transformed.Template; var warnings = transformed.Warnings;
            if (outside) warnings |= ObjectBatchWarning.OutOfMap;
            if (tile.Terrain == 0xFE) warnings |= ObjectBatchWarning.Boundary;
            if (result.Kind == PlacedThingKind.Creature && !creatureFlags.ContainsKey(result.TypeID))
                creatureFlags[result.TypeID] = PlacementCreatureFlags(result.TypeID, records);
            var (water, lava) = creatureFlags.GetValueOrDefault(result.TypeID);
            var terrain = ThingPlacementRules.Evaluate(tile, result.Kind, result.TypeID, result.Owner, water, lava);
            warnings |= terrain switch
            { ThingTerrainPermission.BadTerrain => ObjectBatchWarning.BadTerrain, ThingTerrainPermission.Enemy => ObjectBatchWarning.Enemy,
                ThingTerrainPermission.Fatal => ObjectBatchWarning.Fatal, ThingTerrainPermission.Unverified => ObjectBatchWarning.Incompatible, _ => ObjectBatchWarning.None };
            var scopeChange = ModificationScope(entry.Thing.Kind) != ModificationScope(result.Kind);
            var detaches = sourceMultiple is null && entry.Scripted && scopeChange && ModificationScope(result.Kind) == 0;
            if (sourceMultiple is null && scopeChange && entry.Scripted) warnings |= ObjectBatchWarning.Incompatible;
            var locked = outside || tile.Terrain == 0xFE || !entry.CanRemove || terrain == ThingTerrainPermission.Unverified;
            candidates.Add(new(candidates.Count, entry, result, position, warnings, locked, ObjectModificationDecision.Keep, null, scopeChange, detaches));
        }
        var preliminary = new ObjectBatchModificationPlan(this, selection.Fingerprint, template, options, candidates.AsReadOnly(), multi);
        return ModificationCollisions(preliminary, candidates.Select(_ => ObjectModificationDecision.Modify).ToArray(), initial: true);
    }

    private void CurrentModification(ObjectBatchModificationPlan plan)
    {
        if (!ReferenceEquals(plan.Owner, this) || !plan.Fingerprint.AsSpan().SequenceEqual(GateGenerationFingerprint()))
            throw new InvalidOperationException("Refresh the object modification preview after map changes.");
    }

    public ObjectBatchModificationPlan ReviewObjectBatchModification(ObjectBatchModificationPlan plan, IEnumerable<ObjectModificationDecision> decisions)
    {
        ArgumentNullException.ThrowIfNull(plan); ArgumentNullException.ThrowIfNull(decisions); CurrentModification(plan);
        var chosen = decisions.ToArray(); CurrentModification(plan);
        if (chosen.Length != plan.Candidates.Count || chosen.Any(choice => !Enum.IsDefined(choice))) throw new ArgumentException("Choose Keep, Remove or Modify for each candidate.");
        return ModificationCollisions(plan, chosen, initial: false);
    }

    private ObjectBatchModificationPlan ModificationCollisions(ObjectBatchModificationPlan plan, ObjectModificationDecision[] decisions, bool initial)
    {
        CurrentModification(plan);
        var active = plan.Candidates.Where(candidate => initial || decisions[candidate.Index] != ObjectModificationDecision.Keep)
            .Select(candidate => (candidate.Source.Record.File, candidate.Source.Record.Offset)).ToHashSet();
        var existing = ObjectManagerEntries(); var result = new List<ObjectBatchModificationCandidate>();
        var overlaps = existing.OrderBy(entry => entry.Thing.Kind is PlacedThingKind.Door or PlacedThingKind.Object ? 0 : 1)
            .GroupBy(entry => (ObjectModificationRules.Family(entry.Thing.Kind), entry.Thing.Position)).ToDictionary(group => group.Key, group => group.First());
        foreach (var candidate in plan.Candidates)
        {
            var group = ObjectModificationRules.Family(candidate.Template.Kind);
            // Query the first family-priority entry BEFORE ignoring selected sources, matching source pass 6/9.
            var overlap = group == 0 ? null : overlaps.GetValueOrDefault((group, candidate.Position));
            if (overlap is not null && active.Contains((overlap.Record.File, overlap.Record.Offset))) overlap = null;
            var warnings = candidate.Warnings & ~(ObjectBatchWarning.Replacement | ObjectBatchWarning.SameType | ObjectBatchWarning.SameFamily);
            if (overlap is not null)
            {
                warnings |= ObjectBatchWarning.Replacement;
                if (overlap.Thing.Kind == candidate.Template.Kind)
                { warnings |= ObjectBatchWarning.SameFamily; if (overlap.Thing.TypeID == candidate.Template.TypeID) warnings |= ObjectBatchWarning.SameType; }
            }
            var chosen = decisions[candidate.Index];
            if (initial) chosen = ThingPlacementRules.DefaultDecision(warnings, plan.Options.Replacement, plan.Options.Flags).Add && !candidate.Locked
                ? ObjectModificationDecision.Modify : ObjectModificationDecision.Keep;
            else if (chosen == ObjectModificationDecision.Modify && (candidate.Locked || !SameModificationOverlap(candidate.Replacement, overlap)))
                chosen = ObjectModificationDecision.Keep; // No implicit consent for an object that was previously assumed to vacate.
            result.Add(candidate with { Warnings = warnings, Replacement = overlap, Decision = chosen });
        }
        CurrentModification(plan);
        return new(this, plan.Fingerprint, plan.Template, plan.Options, result.AsReadOnly(), plan.Multiple);
    }

    private static bool SameModificationOverlap(ObjectManagerEntry? first, ObjectManagerEntry? second) => first is null ? second is null
        : second is not null && ReferenceEquals(first.Record.File, second.Record.File) && first.Record.Offset == second.Record.Offset;

    public int ApplyObjectBatchModification(ObjectBatchModificationPlan plan, bool allowScriptDetachment = false)
        => ApplyObjectBatchModificationCore(plan, allowScriptDetachment, true, "Batch modify objects");

    private int ApplyObjectBatchModificationCore(ObjectBatchModificationPlan plan, bool allowScriptDetachment,
        bool reviewCollisions, string description, bool editorManagerCaller = false)
    {
        ArgumentNullException.ThrowIfNull(plan); CurrentModification(plan);
        if (reviewCollisions)
        {
            var checkedPlan = ReviewObjectBatchModification(plan, plan.Candidates.Select(candidate => candidate.Decision));
            if (checkedPlan.Candidates.Where(candidate => candidate.Decision != plan.Candidates[candidate.Index].Decision).Any())
                throw new InvalidOperationException("Review newly exposed collisions before applying modifications.");
        }
        var modifying = plan.Candidates.Where(candidate => candidate.Decision == ObjectModificationDecision.Modify).ToArray();
        var modifications = modifying.ToDictionary(candidate => (candidate.Source.Record.File, candidate.Source.Record.Offset));
        var removing = plan.Candidates.Where(candidate => candidate.Decision == ObjectModificationDecision.Remove).Select(candidate => candidate.Source)
            .Concat(modifying.Where(candidate => candidate.Replacement is not null).Select(candidate => candidate.Replacement!))
            .DistinctBy(entry => (entry.Record.File, entry.Record.Offset)).ToArray();
        if (modifying.Any(candidate => candidate.Locked) || removing.Any(entry => !entry.CanRemove)) throw new InvalidDataException("The object layout is not verified for this change.");
        if (modifying.Length == 0 && removing.Length == 0) return 0;
        if (!editorManagerCaller && modifying.Any(candidate => candidate.DetachesScript) && !allowScriptDetachment)
            throw new InvalidOperationException("Confirm detaching the scenario when converting a door to a trap; events are retained.");
        var prepared = new Dictionary<NativeFile, byte[]>(); var allRecords = Records();
        var ownerRewrites = new Dictionary<NativeFile, (NativeRecord[] Removed, Dictionary<int, int> Relocated)>();
        var removedKeys = removing.Select(entry => (entry.Record.File, entry.Record.Offset)).ToHashSet();
        if (modifying.Any(candidate => removedKeys.Contains((candidate.Source.Record.File, candidate.Source.Record.Offset))))
            throw new InvalidOperationException("A modification source cannot also be an approved replacement.");
        var obsolete = new HashSet<NativeRecord>();
        if (IsLegacyProject)
        {
            var file = projectSections.Single(section => section.Signature == LegacyThings); var version = (int)file.UInt32(4);
            if (modifying.Length != 0 && version != 6 && !plan.Options.UpgradeHistoricalThings)
                throw new InvalidOperationException("Confirm upgrading the historical Things section before bulk modification.");
            var originalRows = allRecords.Where(record => record.File == file).ToArray(); var stride = LegacyProjectSchemas.Size("MAPTHING");
            var baseBytes = version == 6 || modifying.Length == 0 ? file.Bytes : UpgradeThings(file, version);
            var changes = new Dictionary<int, byte[]>(); var deleting = new List<(int Offset, int Length)>();
            var links = allRecords.Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)).ToArray();
            foreach (var entry in removing.Concat(modifying.Where(candidate => candidate.ChangesScriptScope).Select(candidate => candidate.Source)))
            {
                if (editorManagerCaller) continue; // Source copies MAPTHING, leaving every point/operand literal.
                if (entry.Identity == 0) continue;
                var oldScope = ModificationScope(entry.Thing.Kind); if (oldScope == 0) continue;
                var owned = links.Where(link => PlacementNumber(link, "node.type") == oldScope
                    && PlacementNumber(link, ScopeTarget(oldScope)) == entry.Identity).ToArray();
                if (owned.Length == 0) continue;
                if (ObjectManagerEntries().Any(other => other.Identity == entry.Identity && ModificationScope(other.Thing.Kind) == oldScope
                    && (other.Record.File != entry.Record.File || other.Record.Offset != entry.Record.Offset) && !removedKeys.Contains((other.Record.File, other.Record.Offset))))
                    throw new InvalidDataException("Repair the shared scenario identity before changing its owner category.");
                var changing = modifications.GetValueOrDefault((entry.Record.File, entry.Record.Offset));
                var newScope = changing is null ? 0 : ModificationScope(changing.Template.Kind);
                foreach (var link in owned)
                {
                    if (newScope == 0) { obsolete.Add(link); continue; }
                    if (links.Any(other => other != link && PlacementNumber(other, "node.type") == newScope && PlacementNumber(other, ScopeTarget(newScope)) == entry.Identity))
                        throw new InvalidDataException("The destination category already has a binding for this identity.");
                    ValidateModificationScope(link, newScope, allowScriptDetachment, out var detach);
                    if (detach) { obsolete.Add(link); continue; }
                    if (!prepared.TryGetValue(link.File, out var eventBytes)) prepared[link.File] = eventBytes = link.File.Bytes.ToArray();
                    var field = link.Fields.Single(field => field.Name == "node.type"); field.Encode(newScope.ToString(CultureInfo.InvariantCulture)).CopyTo(eventBytes, link.Offset + field.Offset);
                }
            }
            for (var index = 0; index < originalRows.Length; index++)
            {
                var record = originalRows[index]; var offset = version == 6 || modifying.Length == 0 ? record.Offset : 12 + index * stride;
                var length = version == 6 || modifying.Length != 0 ? stride : record.Length;
                if (removedKeys.Contains((record.File, record.Offset))) { deleting.Add((offset, length)); continue; }
                var candidate = modifications.GetValueOrDefault((record.File, record.Offset));
                if (candidate is null) continue;
                var bytes = baseBytes.AsSpan(offset, stride).ToArray(); WriteModificationProject(bytes, candidate);
                if (!baseBytes.AsSpan(offset, stride).SequenceEqual(bytes)) changes[offset] = bytes;
            }
            // Pure no-op keeps physical order, redo and historical version. Actual multi-edit appends modified sources in chosen order.
            if (!editorManagerCaller && changes.Count == 0 && deleting.Count == 0 && prepared.Count == 0 && obsolete.Count == 0) return 0;
            var additions = new List<byte[]>();
            var rowIndices = originalRows.Select((record, index) => (record.Offset, index)).ToDictionary(item => item.Offset, item => item.index);
            foreach (var candidate in modifying)
            {
                var index = rowIndices[candidate.Source.Record.Offset];
                var offset = version == 6 ? originalRows[index].Offset : 12 + index * stride;
                var bytes = changes.GetValueOrDefault(offset) ?? baseBytes.AsSpan(offset, stride).ToArray();
                if (plan.Multiple) { deleting.Add((offset, stride)); additions.Add(bytes); }
                else { baseBytes = baseBytes.ToArray(); bytes.CopyTo(baseBytes, offset); }
            }
            var things = BatchSplice(baseBytes, deleting, baseBytes.Length, additions); WriteUInt32(things, 8, (uint)things.Length - 12); prepared[file] = things;
        }
        else
        {
            foreach (var group in removing.Select(entry => entry.Record.File).Concat(modifying.Select(candidate => candidate.Source.Record.File)).Distinct())
            {
                var changes = modifying.Where(candidate => candidate.Source.Record.File == group).Select(candidate => (Candidate: candidate, Bytes: ModificationNativeBytes(candidate, allowScriptDetachment, editorManagerCaller))).ToArray();
                var removed = removing.Where(entry => entry.Record.File == group).Select(entry => (entry.Record.Offset, entry.Record.Length)).ToList();
                if (!editorManagerCaller && removed.Count == 0 && changes.All(change => group.Data.Span.Slice(change.Candidate.Source.Record.Offset, change.Candidate.Source.Record.Length).SequenceEqual(change.Bytes))) continue;
                var source = group.Bytes.ToArray(); var additions = new List<byte[]>(); var appended = new List<NativeRecord>();
                foreach (var (candidate, bytes) in changes)
                {
                    if (plan.Multiple || bytes.Length != candidate.Source.Record.Length)
                    { removed.Add((candidate.Source.Record.Offset, candidate.Source.Record.Length)); additions.Add(bytes); appended.Add(candidate.Source.Record); }
                    else bytes.CopyTo(source, candidate.Source.Record.Offset);
                }
                var end = NativeCollectionDataEnd(group);
                var result = BatchSplice(source, removed, end, additions);
                var delta = result.Length - group.Length; WriteUInt32(result, 8, AdjustedNativeStoredSize(group.UInt32(8), delta, result.Length));
                WriteUInt32(result, 52, AdjustedNativeStoredSize(group.UInt32(52), delta, end - 56 + delta));
                WriteUInt32(result, 20, checked((uint)(group.UInt32(20) - removed.Count + (long)additions.Count))); prepared[group] = result;
                var insertion = checked(end - removed.Sum(row => row.Length));
                var relocated = new Dictionary<int, int>();
                for (var index = 0; index < appended.Count; index++)
                { relocated.Add(appended[index].Offset, insertion); insertion = checked(insertion + additions[index].Length); }
                var offsets = removed.Select(row => row.Offset).ToHashSet();
                ownerRewrites.Add(group, (allRecords.Where(record => record.File == group && offsets.Contains(record.Offset)
                    && !record.Category.StartsWith("BandHero", StringComparison.Ordinal)).ToArray(), relocated));
            }
        }
        foreach (var group in obsolete.GroupBy(record => record.File))
        {
            var file = group.Key; var source = prepared.GetValueOrDefault(file) ?? file.Bytes;
            var result = BatchSplice(source, group.Select(record => (record.Offset, record.Length)).ToArray(), source.Length, []);
            WriteUInt32(result, 8, (uint)result.Length - 12); WriteUInt32(result, 12, checked(file.UInt32(12) - (uint)group.Count())); prepared[file] = result;
        }
        if (IsLegacyProject && 12L + projectSections.Sum(file => (long)(prepared.GetValueOrDefault(file)?.Length ?? file.Length)) > MaximumFileBytes)
            throw new InvalidDataException("Bulk modification exceeds the project size limit.");
        CurrentModification(plan); if (prepared.Count == 0) return 0;
        Edit(description, () =>
        {
            foreach (var (file, bytes) in prepared)
            {
                if (ownerRewrites.TryGetValue(file, out var rewrite))
                    RebaseNativeOwnerRewrite(file, bytes, rewrite.Removed, rewrite.Relocated, preservePoints: editorManagerCaller);
                file.Bytes = bytes;
            }
        });
        return modifying.Length + plan.Candidates.Count(candidate => candidate.Decision == ObjectModificationDecision.Remove);
    }

    private void ValidateModificationScope(NativeRecord root, uint newScope, bool allowDetach, out bool detach)
    {
        var graph = ScriptGraph.Build(this); var field = root.Fields.First(field => field.Name == "trigger" || field.Name.EndsWith("Trigger", StringComparison.Ordinal));
        var id = unchecked((ushort)PlacementNumber(root, field.Name));
        detach = id != 0 && (graph.DuplicateIDs.Count != 0 || graph.Reachable(id).Any(target => !graph.Events.TryGetValue(target, out var item)
            || item.Type is null || (LegacyEventSchemas.AllowedScopes(item.Type) & newScope) == 0));
        if (detach && !allowDetach) throw new InvalidOperationException("The retained scenario is incompatible with the new category; explicitly detach its binding or keep the category.");
    }

    private static void WriteModificationProject(byte[] bytes, ObjectBatchModificationCandidate candidate)
    {
        var fresh = BatchProjectBytes(candidate.Template, new(0, candidate.Position, candidate.Template.Owner, 0, false, true, null), candidate.Source.Identity);
        foreach (var field in LegacyProjectSchemas.Fields("MAPTHING").Where(field => !field.Name.StartsWith("node.", StringComparison.Ordinal) || field.Name == "node.type"))
            fresh.AsSpan(field.Offset, field.Size).CopyTo(bytes.AsSpan(field.Offset));
        // All NODE linkage bytes and stable unique identity remain untouched.
    }

    private byte[] ModificationNativeBytes(ObjectBatchModificationCandidate candidate, bool allowDetach, bool editorManagerCaller = false)
    {
        var record = candidate.Source.Record;
        var bytes = BatchNativeBytes(candidate.Template, new(0, candidate.Position, candidate.Template.Owner, 0, false, true, null));
        var structure = candidate.Template.Kind switch
        { PlacedThingKind.Creature => candidate.Template.Owner == 1 ? "GAMEHERO" : candidate.Template.Owner == 2 ? "GAMENEUTRAL" : "GAMEKEEPER",
            PlacedThingKind.Door => "GAMEDOOR", PlacedThingKind.Trap => "GAMETRAP", _ => "GAMEOBJECT" };
        var fields = LegacySchemas.Fields(structure);
        if (structure == record.Category)
        {
            var result = record.File.Data.Span.Slice(record.Offset, record.Length).ToArray();
            foreach (var field in fields.Where(field => !field.Name.EndsWith("Trigger", StringComparison.Ordinal) && field.Name != "bHeroBandID"))
            {
                // Inactive object slots are not template fields; retain their imported raw payloads unless type/data actually changes.
                if (field.Name is "dwObjectSpell" or "dwObjectGoldValue" && candidate.Template.TypeID == candidate.Source.Thing.TypeID
                    && candidate.Template.DataAll == candidate.Source.DataAll) continue;
                bytes.AsSpan(field.Offset, field.Size).CopyTo(result.AsSpan(field.Offset));
            }
            bytes = result;
        }
        else
        {
            // Verified common reserved locations across source C2-C8 layouts. Other family-specific bytes have no destination field.
            record.File.Data.Span.Slice(record.Offset + 16, 4).CopyTo(bytes.AsSpan(16));
            if (candidate.Template.Kind == PlacedThingKind.Creature) bytes[21] = record.File.Data.Span[record.Offset + 21];
            var band = record.Fields.FirstOrDefault(field => field.Name == "bHeroBandID");
            if (band is not null && fields.FirstOrDefault(field => field.Name == "bHeroBandID") is { } target)
                record.File.Data.Span.Slice(record.Offset + band.Offset, band.Size).CopyTo(bytes.AsSpan(target.Offset));
        }
        var oldRoot = record.Fields.FirstOrDefault(field => field.Name.EndsWith("Trigger", StringComparison.Ordinal));
        var newRoot = fields.FirstOrDefault(field => field.Name.EndsWith("Trigger", StringComparison.Ordinal));
        if (oldRoot is not null && newRoot is not null)
        {
            if (editorManagerCaller && candidate.ChangesScriptScope)
            { newRoot.Encode("0").CopyTo(bytes,newRoot.Offset);return bytes; }
            var detach = false; if (candidate.ChangesScriptScope) ValidateModificationScope(record, ModificationScope(candidate.Template.Kind), allowDetach, out detach);
            if (!detach) record.File.Data.Span.Slice(record.Offset + oldRoot.Offset, oldRoot.Size).CopyTo(bytes.AsSpan(newRoot.Offset));
        }
        return bytes;
    }
}
