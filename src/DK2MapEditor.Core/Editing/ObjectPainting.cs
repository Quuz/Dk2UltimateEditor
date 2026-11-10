namespace DK2MapEditor.Core.Editing;

/// <summary>Single source Things_Paint operation, deliberately distinct from manager insertion.
/// Creatures stack; other families update the first same-kind object, or remove ALL opposite-kind
/// objects before creating a fresh identity. The preview does not change any bytes.</summary>
public sealed class ObjectPaintingPlan
{
    internal ObjectPaintingPlan(NativeMapDocument document, byte[] fingerprint, ObjectBatchTemplate template,
        ObjectBatchTemplate effective, TilePoint position, ThingTerrainPermission permission, ObjectBatchWarning warnings,
        ObjectManagerEntry? updating, IReadOnlyList<ObjectManagerEntry> removals, bool locked, bool upgrade)
    {
        Document = document; Fingerprint = fingerprint; Template = template; EffectiveTemplate = effective; Position = position;
        Permission = permission; Warnings = warnings; Updating = updating; Removals = removals; Locked = locked; UpgradeHistoricalThings = upgrade;
    }
    internal NativeMapDocument Document { get; }
    internal byte[] Fingerprint { get; }
    public ObjectBatchTemplate Template { get; }
    public ObjectBatchTemplate EffectiveTemplate { get; }
    public TilePoint Position { get; }
    public ThingTerrainPermission Permission { get; }
    public ObjectBatchWarning Warnings { get; }
    public ObjectManagerEntry? Updating { get; }
    public IReadOnlyList<ObjectManagerEntry> Removals { get; }
    public bool Locked { get; }
    public bool UpgradeHistoricalThings { get; }
    public bool Creates => Updating is null;
    public bool TogglesDoor => Updating is { } old && Template.Kind == PlacedThingKind.Door
        && old.Thing.TypeID == Template.TypeID && old.Thing.Owner == EffectiveTemplate.Owner;
}

public sealed partial class NativeMapDocument
{
    /// <summary>Source Things_Paint accepts once, including an equal update,
    /// plus every opposite-kind removal through Things_RemoveAtMapPosEx.
    /// One parent Undo and one paint-usage increment belong to the entire call.</summary>
    public int ApplyEditorObjectPainting(ObjectPaintingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        EditEditorDrawing("Paint object", () =>
        {
            ApplyObjectPainting(plan);
            return new(true, unchecked(1u + (uint)plan.Removals.Count));
        });
        RecordActivity(ActivityCounterEvent.ObjectPaint);
        return 1;
    }

    public ObjectPaintingPlan PlanObjectPainting(ObjectBatchTemplate template, TilePoint position, bool upgradeHistoricalThings = false)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (!Enum.IsDefined(template.Kind) || template.Owner > 7) throw new ArgumentException("Unknown object family or owner.");
        // Reuse structural layout validation, not the manager's decisions or its resolved-owner terrain check.
        var structural = PlanObjectBatchPlacement(new(template.Kind, template.TypeID, template.Owner), new(Position: position));
        var candidate = structural.Candidates.Single(); var records = Records();
        var outside = position.X < 0 || position.Y < 0 || position.X >= Width || position.Y >= Height;
        var tile = outside ? new MapTile(0xFE, 2, 1) : ThingPlacementRules.EditorTile(GetTile(position.X, position.Y), IsEditorProtectedCell(position.X, position.Y));
        var (water, lava) = template.Kind == PlacedThingKind.Creature ? PlacementCreatureFlags(template.TypeID, records) : ((bool?)null, (bool?)null);
        // Things_CanPaint checks the RAW template owner; owner zero bypasses territory checks.
        var permission = ThingPlacementRules.Evaluate(tile, template.Kind, template.TypeID, template.Owner, water, lava);
        var owner = template.Owner == 0 ? tile.Owner : template.Owner;
        var entries = ObjectManagerEntries();
        var updating = template.Kind == PlacedThingKind.Creature ? null : entries.FirstOrDefault(entry => entry.Thing.Kind == template.Kind && entry.Thing.Position == position);
        var opposite = template.Kind switch
        {
            PlacedThingKind.Door => PlacedThingKind.Trap, PlacedThingKind.Trap => PlacedThingKind.Door,
            PlacedThingKind.Object => PlacedThingKind.SpecialItem, PlacedThingKind.SpecialItem => PlacedThingKind.Object,
            _ => (PlacedThingKind?)null
        };
        var removals = updating is not null || opposite is null ? [] : entries.Where(entry => entry.Thing.Kind == opposite && entry.Thing.Position == position).ToArray();
        ObjectBatchTemplate effective;
        if (updating is not null)
        {
            var old = ModificationTemplate(updating);
            effective = old with { TypeID = template.TypeID, Owner = owner, HeroOptions = template.HeroOptions,
                DataAll = template.Kind == PlacedThingKind.Door && old.TypeID == template.TypeID && old.Owner == owner
                    ? old.DataAll == 0 ? 1 : 0 : template.DataAll };
        }
        else effective = owner == 1 ? template with { Owner = owner }
            : template with { Owner = owner, HeroOptions = 0, Objective = 0, Area = 0, Enemy = 0 };
        var warnings = candidate.Warnings & (ObjectBatchWarning.OutOfMap | ObjectBatchWarning.Boundary | ObjectBatchWarning.Incompatible);
        warnings |= permission switch { ThingTerrainPermission.BadTerrain => ObjectBatchWarning.BadTerrain,
            ThingTerrainPermission.Enemy => ObjectBatchWarning.Enemy, ThingTerrainPermission.Fatal => ObjectBatchWarning.Fatal,
            ThingTerrainPermission.Unverified => ObjectBatchWarning.Incompatible, _ => ObjectBatchWarning.None };
        if (removals.Length != 0) warnings |= ObjectBatchWarning.Replacement;
        var locked = candidate.Locked || permission is ThingTerrainPermission.BadTerrain or ThingTerrainPermission.Unverified
            || updating is { CanRemove: false } || removals.Any(entry => !entry.CanRemove);
        if (!structural.Fingerprint.AsSpan().SequenceEqual(GateGenerationFingerprint())) throw new InvalidOperationException("The object painting preview is stale.");
        return new(this, structural.Fingerprint, template, effective, position, permission, warnings, updating, Array.AsReadOnly(removals), locked, upgradeHistoricalThings);
    }

    public int ApplyObjectPainting(ObjectPaintingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (!ReferenceEquals(plan.Document, this) || !plan.Fingerprint.AsSpan().SequenceEqual(GateGenerationFingerprint()))
            throw new InvalidOperationException("Refresh the object painting preview after map changes.");
        if (plan.Locked) throw new InvalidOperationException("The selected terrain or object layout does not allow painting this object.");
        if (plan.Updating is { } updating)
        {
            var candidate = new ObjectBatchModificationCandidate(0, updating, plan.EffectiveTemplate, plan.Position, plan.Warnings, false,
                ObjectModificationDecision.Modify, null, false, false);
            var modification = new ObjectBatchModificationPlan(this, plan.Fingerprint, plan.Template,
                new(ObjectModificationMask.None, UpgradeHistoricalThings: plan.UpgradeHistoricalThings), Array.AsReadOnly(new[] { candidate }), false);
            // The source updates same-kind entries even if an opposite-kind entry also occupies this tile.
            return ApplyObjectBatchModificationCore(modification, false, false, "Paint object");
        }
        var placement = new ObjectBatchPlacementPlan(this, plan.Fingerprint,
            new(plan.EffectiveTemplate.Kind, plan.EffectiveTemplate.TypeID, plan.EffectiveTemplate.Owner), new(Position: plan.Position),
            Array.AsReadOnly(new[] { new ObjectBatchPlacementCandidate(0, plan.Position, plan.EffectiveTemplate.Owner, plan.Warnings, false, true, null) }), 1, true);
        return ApplyObjectBatchInsertionCore(new(plan.EffectiveTemplate, placement, plan.UpgradeHistoricalThings), [0],
            plan.Removals.Select(entry => entry.Record).ToArray(),"Paint object",editorCreation:true);
    }
}
