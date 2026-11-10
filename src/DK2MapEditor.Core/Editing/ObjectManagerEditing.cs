using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public enum ObjectManagerSort { Position, Name, Owner, ID, Type }

/// <summary>A verified physical entry; an allocated project UID alone is not a scenario.</summary>
public sealed record ObjectManagerEntry(ObjectPlacementExisting Thing, int DataAll, uint Identity,
    bool Scripted, bool CanRemove)
{
    public NativeRecord Record => Thing.Record;
    public string Name(string language, int gameVersion = 17) => ValueCatalogues.ForTemplate(Thing.Kind switch
    {
        PlacedThingKind.Creature => "Creature", PlacedThingKind.Door => "Door", PlacedThingKind.Trap => "Trap",
        PlacedThingKind.Object => "Object", _ => "MagicalObject"
    }, gameVersion).FirstOrDefault(choice => choice.Value == Thing.TypeID.ToString(CultureInfo.InvariantCulture))?.Name(language)
        ?? "#" + Thing.TypeID.ToString(CultureInfo.InvariantCulture);
}

public sealed class ObjectRemovalPlan
{
    internal ObjectRemovalPlan(NativeMapDocument owner, byte[] fingerprint, IReadOnlyList<ObjectManagerEntry> entries)
    { Owner = owner; Fingerprint = fingerprint; Entries = entries; }
    internal NativeMapDocument Owner { get; }
    internal byte[] Fingerprint { get; }
    public IReadOnlyList<ObjectManagerEntry> Entries { get; }
    public int ScriptedCount => Entries.Count(entry => entry.Scripted);
}

public static class ObjectManagerOrdering
{
    // Source manager lists keep doors before traps and specials before objects.
    // Unlike its one-sided C returns, this is an antisymmetric comparison.
    public static int Compare(ObjectManagerEntry first, ObjectManagerEntry second, ObjectManagerSort method,
        string language = "en", int gameVersion = 17)
    {
        if (!Enum.IsDefined(method)) throw new ArgumentOutOfRangeException(nameof(method));
        static int Rank(PlacedThingKind kind) => kind switch
        { PlacedThingKind.Creature => 0, PlacedThingKind.Door => 1, PlacedThingKind.Trap => 2, PlacedThingKind.SpecialItem => 3, _ => 4 };
        var order = Rank(first.Thing.Kind).CompareTo(Rank(second.Thing.Kind)); if (order != 0) return order;
        order = method switch
        {
            ObjectManagerSort.Position => first.Thing.Position.Y.CompareTo(second.Thing.Position.Y) is var y && y != 0
                ? y : first.Thing.Position.X.CompareTo(second.Thing.Position.X),
            ObjectManagerSort.Name => StringComparer.OrdinalIgnoreCase.Compare(first.Name(language, gameVersion), second.Name(language, gameVersion)),
            ObjectManagerSort.Owner => first.Thing.Owner.CompareTo(second.Thing.Owner),
            ObjectManagerSort.ID => first.Thing.TypeID.CompareTo(second.Thing.TypeID), _ => 0
        };
        if (order != 0 || method == ObjectManagerSort.Position) return order;
        order = CatalogueOffset(first.Thing).CompareTo(CatalogueOffset(second.Thing));
        return order != 0 ? order : first.DataAll.CompareTo(second.DataAll);
    }
    private static int CatalogueOffset(ObjectPlacementExisting thing)
    {
        var groups = thing.Kind switch
        {
            PlacedThingKind.Creature => new[] { "BadCreatures", "BadElites", "Heros" }, PlacedThingKind.Door => ["Doors"],
            PlacedThingKind.Trap => ["Traps"], PlacedThingKind.Object => ["Objects"], _ => ["Magics"]
        };
        var offset = 0;
        foreach (var group in groups) foreach (var item in LegacyCompilerRules.Elements(group))
        { if (item.ID == thing.TypeID) return offset; offset++; }
        return 0; // Things_GetOffsetFromID's unknown fallback.
    }
    public static IReadOnlyList<ObjectManagerEntry> Sort(IEnumerable<ObjectManagerEntry> entries, ObjectManagerSort method,
        string language = "en", int gameVersion = 17)
    {
        if (!Enum.IsDefined(method)) throw new ArgumentOutOfRangeException(nameof(method));
        return Array.AsReadOnly(entries.OrderBy(entry => entry, Comparer<ObjectManagerEntry>.Create((a, b) => Compare(a, b, method, language, gameVersion))).ToArray());
    }
}

public sealed partial class NativeMapDocument
{
    public IReadOnlyList<ObjectManagerEntry> ObjectManagerEntries()
    {
        var records = Records();
        var nativeStates = NativeOwnerStates();
        var links = records.Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)).ToArray();
        var bound = links.SelectMany(link => new[] { (2u, "creature"), (64u, "door"), (128u, "object"), (256u, "object") }
            .Where(scope => PlacementNumber(link, "node.type") == scope.Item1)
            .Select(scope => (scope.Item1, ID: PlacementNumber(link, scope.Item2)))).Where(key => key.ID != 0).ToHashSet();
        var opaqueEvents = records.Any(record => record.File.Signature == LegacyEvents && record.Category == "Project.Opaque");
        return Array.AsReadOnly(records.Select(record =>
        {
            var thing = PlacementExisting(record); if (thing is null) return null;
            var state = nativeStates.GetValueOrDefault((record.File, record.Offset, record.Length));
            var identity = IsLegacyProject ? PlacementNumber(record, "uniqueid") : state?.ID ?? 0;
            var scripted = IsLegacyProject ? ProjectScopes(record).Any(scope => identity != 0 && bound.Contains((scope.Scope, identity)))
                : state?.Present == true;
            var canRemove = IsLegacyProject ? !opaqueEvents && record.File.UInt32(4) is >= 1 and <= 6
                : record.File.UInt32(4) == 4 && RecordTemplate.All.FirstOrDefault(template => template.Structure == record.Category) is { } layout
                    && record.Length == layout.Size && record.Fields.Count == LegacySchemas.Fields(record.Category).Count;
            var data = IsLegacyProject ? unchecked((int)PlacementNumber(record, "dataAll")) : thing.Kind switch
            {
                PlacedThingKind.Creature => unchecked((int)PlacementNumber(record, "bGlobalLevel")),
                PlacedThingKind.Door => unchecked((int)PlacementNumber(record, "bDoorLocked")),
                PlacedThingKind.Trap => unchecked((int)PlacementNumber(record, "bTrapShots")),
                PlacedThingKind.Object when thing.TypeID is 1 or 2 or 3 => unchecked((int)PlacementNumber(record, "dwObjectGoldValue")),
                PlacedThingKind.Object when thing.TypeID == 4 => unchecked((int)PlacementNumber(record, "dwObjectSpell")), _ => 0
            };
            return new ObjectManagerEntry(thing, data, identity, scripted, canRemove);
        }).OfType<ObjectManagerEntry>().ToArray());
    }

    public ObjectRemovalPlan PlanObjectRemoval(IEnumerable<NativeRecord> selected)
    {
        ArgumentNullException.ThrowIfNull(selected);
        var fingerprint = GateGenerationFingerprint();
        var current = ObjectManagerEntries().ToDictionary(entry => (entry.Record.File, entry.Record.Offset));
        var entries = new List<ObjectManagerEntry>(); var seen = new HashSet<(NativeFile, int)>();
        foreach (var record in selected)
        {
            if (!current.TryGetValue((record.File, record.Offset), out var entry) || record.Length != entry.Record.Length
                || record.Category != entry.Record.Category || !record.Fields.SequenceEqual(entry.Record.Fields))
                throw new ArgumentException("Select current, verified physical objects.");
            if (seen.Add((record.File, record.Offset))) entries.Add(entry);
        }
        if (!fingerprint.AsSpan().SequenceEqual(GateGenerationFingerprint())) throw new InvalidOperationException("The document changed while selecting objects.");
        return new(this, fingerprint, entries.AsReadOnly());
    }

    /// <summary>Delete approved physical entries and their exact project scope bindings,
    /// never shared/unlinked events. No upgrade, pointer rewrite or terrain change.</summary>
    public int ApplyObjectRemoval(ObjectRemovalPlan plan, bool includeScripted = false)
        => ApplyObjectRemovalCore(plan, includeScripted, false);

    private int ApplyObjectRemovalCore(ObjectRemovalPlan plan, bool includeScripted, bool editorCaller)
    {
        ArgumentNullException.ThrowIfNull(plan);
        void Current()
        {
            if (!ReferenceEquals(plan.Owner, this) || !plan.Fingerprint.AsSpan().SequenceEqual(GateGenerationFingerprint()))
                throw new InvalidOperationException("Refresh the object removal preview after map changes.");
        }
        Current();
        var entries = plan.Entries.Where(entry => includeScripted || !entry.Scripted).ToArray();
        if (entries.Any(entry => !entry.CanRemove)) throw new InvalidDataException("The object/scenario layout is not verified for removal.");
        if (entries.Length == 0) return 0;
        var removing = entries.Select(entry => entry.Record).ToList();
        if (IsLegacyProject && !editorCaller)
        {
            var removedKeys = entries.Select(entry => (entry.Record.File, entry.Record.Offset)).ToHashSet();
            var remaining = ObjectManagerEntries().Where(entry => !removedKeys.Contains((entry.Record.File, entry.Record.Offset))).ToArray();
            var links = Records().Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)).ToArray();
            foreach (var entry in entries)
                foreach (var scope in ProjectScopes(entry.Record))
                {
                    if (entry.Identity == 0) continue;
                    var owned = links.Where(link => PlacementNumber(link, "node.type") == scope.Scope && PlacementNumber(link, scope.Target) == entry.Identity).ToArray();
                    if (owned.Length == 0) continue;
                    if (remaining.Any(other => other.Identity == entry.Identity && ProjectScopes(other.Record).Any(value => value.Scope == scope.Scope)))
                        throw new InvalidDataException("A retained object shares this scenario identity; repair it or explicitly remove all its owners.");
                    removing.AddRange(owned);
                }
        }
        var prepared = new Dictionary<NativeFile, byte[]>();
        foreach (var group in removing.DistinctBy(record => (record.File, record.Offset)).GroupBy(record => record.File))
        {
            var file = group.Key; var records = group.ToArray();
            var end = IsLegacyProject ? file.Length : NativeCollectionDataEnd(file);
            var bytes = BatchSplice(file.Bytes, records.Select(record => (record.Offset, record.Length)).ToArray(), end, []);
            if (IsLegacyProject)
            {
                WriteUInt32(bytes, 8, (uint)bytes.Length - 12);
                if (file.Signature == LegacyEvents) WriteUInt32(bytes, 12, checked(file.UInt32(12) - (uint)records.Length));
            }
            else
            {
                var delta = records.Sum(record => (long)record.Length);
                WriteUInt32(bytes, 8, AdjustedNativeStoredSize(file.UInt32(8), -delta, bytes.Length));
                WriteUInt32(bytes, 52, AdjustedNativeStoredSize(file.UInt32(52), -delta, checked(end - 56 - (int)delta)));
                WriteUInt32(bytes, 20, checked(file.UInt32(20) - (uint)records.Length));
            }
            prepared[file] = bytes;
        }
        Current(); Edit("Batch remove objects", () =>
        {
            if (!IsLegacyProject) RebaseNativeOwnerRemovals(removing, preservePoints: editorCaller);
            foreach (var (file, bytes) in prepared) file.Bytes = bytes;
        });
        return entries.Length;
    }
}
