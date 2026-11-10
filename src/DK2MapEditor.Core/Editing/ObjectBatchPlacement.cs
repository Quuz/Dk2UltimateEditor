using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

// Numeric values follow Things.h and Manager.h, not native record signatures.
public enum PlacedThingKind { Creature, Door, Trap, Object, SpecialItem }
public enum ObjectBatchMode { One, Rectangle, RandomRectangle }
public enum ObjectReplacementPolicy { All, SameType, SameFamily, None, Ask }

[Flags]
public enum ObjectBatchWarning
{
    None = 0, OutOfMap = 1, BadTerrain = 2, Boundary = 4, Enemy = 8, Fatal = 16,
    Replacement = 32, SameType = 64, SameFamily = 128, Incompatible = 256
}

[Flags]
public enum ObjectBatchFlags
{
    None = 0, AskOptions = 1, Preview = 2, IgnoreTerrain = 4, IgnoreFatals = 8,
    IgnoreEnemies = 16, IgnoreIncompatible = 32, IgnoreAsk = 64
}

/// <summary>Placement-only part of a template. Full editable template payloads
/// and transactional insertion are a separate stage; this never inserts data.</summary>
public sealed record ObjectPlacementTemplate(PlacedThingKind Kind, uint TypeID, byte Owner = 0);

public sealed record ObjectBatchPlacementOptions(ObjectBatchMode Mode = ObjectBatchMode.One,
    TilePoint Position = default, bool WholeMap = true, TileRect? Selection = null,
    int RandomFrequency = 2, ObjectReplacementPolicy Replacement = ObjectReplacementPolicy.Ask,
    ObjectBatchFlags Flags = ObjectBatchFlags.AskOptions | ObjectBatchFlags.Preview | ObjectBatchFlags.IgnoreAsk);

public sealed record ObjectPlacementExisting(NativeRecord Record, PlacedThingKind Kind, uint TypeID, byte Owner, TilePoint Position);

public sealed record ObjectBatchPlacementCandidate(int Index, TilePoint Position, byte Owner,
    ObjectBatchWarning Warnings, bool Locked, bool DefaultAdd, ObjectPlacementExisting? Replacement);

/// <summary>A read-only preview. Random candidates are sampled once; neither
/// inspection nor resolving checkbox decisions creates history or changes files.</summary>
public sealed class ObjectBatchPlacementPlan
{
    internal ObjectBatchPlacementPlan(NativeMapDocument document, byte[] fingerprint, ObjectPlacementTemplate template,
        ObjectBatchPlacementOptions options, IReadOnlyList<ObjectBatchPlacementCandidate> candidates, int sampledCells, bool preview)
    {
        Document = document; Fingerprint = fingerprint; Template = template; Options = options;
        Candidates = candidates; SampledCells = sampledCells; RequiresPreview = preview;
    }
    internal NativeMapDocument Document { get; }
    internal byte[] Fingerprint { get; }
    public ObjectPlacementTemplate Template { get; }
    public ObjectBatchPlacementOptions Options { get; }
    public IReadOnlyList<ObjectBatchPlacementCandidate> Candidates { get; }
    public int SampledCells { get; }
    public bool RequiresPreview { get; }
    public int DefaultAddCount => Candidates.Count(candidate => candidate.DefaultAdd);
}

public enum ThingTerrainPermission { BadTerrain, Allowed, Enemy, Fatal, Unverified }

/// <summary>Things_CanPaintEx semantics after the source game-to-editor terrain
/// lookup. Does not normalize native data or mistake its padding for an index.</summary>
public static class ThingPlacementRules
{
    private static readonly HashSet<byte> solid = [0xFE, 6, 7, 1, 2, 30, 9];
    private static readonly HashSet<byte> doorTrapRooms = [11, 15, 10, 24, 21, 23, 22, 13, 16, 19, 18, 20, 25, 17, 26, 36];
    private static readonly HashSet<byte> forbiddenPortals = [40, 12, 29, 33, 37];
    private static readonly IReadOnlyList<(LegacyCompilerRules.Element Entry, int Group)> tiles =
        new[] { "World", "Rooms", "Gates", "Walls" }.SelectMany((group, index) =>
            LegacyCompilerRules.Elements(group).Select(entry => (entry, index))).ToArray();

    /// <summary>Projection exactly follows Compiler_GameMapToEditorMap. Invalid
    /// owners/options and unknown IDs use its fallback ONLY for evaluating rules.
    /// Map_CreateLimitWall projects all four outer edges to FE/neutral.</summary>
    public static MapTile EditorTile(MapTile raw, bool boundary = false)
    {
        if (boundary) return new(0xFE, 2, 1);
        var owner = raw.Owner is >= 1 and <= 7 ? raw.Owner : (byte)2;
        var variant = raw.Variant is 1 or 2 ? raw.Variant : (byte)1;
        var found = tiles.FirstOrDefault(item => (byte)(item.Entry.ID >> 24) == raw.Terrain
            && (byte)(item.Entry.ID >> 8) == variant && (item.Group != 3 || (byte)(item.Entry.ID >> 16) == owner));
        return found.Entry is null ? new(2, 2, 1) : new(raw.Terrain,
            found.Group == 1 ? owner : (byte)(found.Entry.ID >> 16), variant);
    }

    /// <param name="tile">Already projected source editor tile.</param>
    /// <param name="canWalkWater">Null means that a relevant definition could not be verified, not false.</param>
    public static ThingTerrainPermission Evaluate(MapTile tile, PlacedThingKind kind, uint typeID, byte owner,
        bool? canWalkWater = null, bool? canWalkLava = null)
    {
        if (!Enum.IsDefined(kind) || owner > 7) throw new ArgumentException("Unknown thing family or owner.");
        if (solid.Contains(tile.Terrain)) return ThingTerrainPermission.BadTerrain;
        if (kind == PlacedThingKind.Creature)
        {
            // Source water/lava checks happen BEFORE owner/territory checks.
            if (tile.Terrain is 4 or 5)
            {
                var safe = tile.Terrain == 4 ? canWalkWater : canWalkLava;
                return safe is null ? ThingTerrainPermission.Unverified : safe.Value ? ThingTerrainPermission.Allowed : ThingTerrainPermission.Fatal;
            }
            return owner == 0 || tile.Terrain is 3 or 31 || tile.Terrain == 20 && tile.Owner != owner || tile.Owner == owner
                ? ThingTerrainPermission.Allowed : ThingTerrainPermission.Enemy;
        }
        if (kind is PlacedThingKind.Door or PlacedThingKind.Trap)
        {
            if (doorTrapRooms.Contains(tile.Terrain) || tile.Terrain is 31 or 32 or 4 or 5 || owner != 0 && tile.Owner != owner)
                return ThingTerrainPermission.BadTerrain;
        }
        if (forbiddenPortals.Contains(tile.Terrain) || tile.Terrain is 14 or 28 or 34 && (kind != PlacedThingKind.Object || typeID != 28))
            return ThingTerrainPermission.BadTerrain;
        // Neutral exemption is for objects, NOT doors/traps after fallthrough.
        return owner == 0 || tile.Owner == owner || kind is PlacedThingKind.Object or PlacedThingKind.SpecialItem && owner == 2
            || kind == PlacedThingKind.Object && typeID == 0x82 ? ThingTerrainPermission.Allowed : ThingTerrainPermission.Enemy;
    }

    public static (bool Add, bool Locked, bool Preview) DefaultDecision(ObjectBatchWarning warnings,
        ObjectReplacementPolicy replacement, ObjectBatchFlags flags)
    {
        if (!Enum.IsDefined(replacement) || (flags & ~(ObjectBatchFlags)127) != 0 || (warnings & ~(ObjectBatchWarning)511) != 0)
            throw new ArgumentException("Unknown batch warning, replacement policy or flags.");
        if (warnings == ObjectBatchWarning.None) return (true, false, false);
        var problems = warnings & (ObjectBatchWarning.BadTerrain | ObjectBatchWarning.Fatal | ObjectBatchWarning.Enemy | ObjectBatchWarning.Incompatible);
        if (flags.HasFlag(ObjectBatchFlags.IgnoreTerrain)) problems &= ~ObjectBatchWarning.BadTerrain;
        if (flags.HasFlag(ObjectBatchFlags.IgnoreFatals)) problems &= ~ObjectBatchWarning.Fatal;
        if (flags.HasFlag(ObjectBatchFlags.IgnoreEnemies)) problems &= ~ObjectBatchWarning.Enemy;
        if (flags.HasFlag(ObjectBatchFlags.IgnoreIncompatible)) problems &= ~ObjectBatchWarning.Incompatible;
        var add = problems == 0; var preview = !add;
        // Preserve the source order: replacement can override other warnings.
        if (warnings.HasFlag(ObjectBatchWarning.Replacement))
        {
            add = replacement switch
            {
                ObjectReplacementPolicy.All => true,
                ObjectReplacementPolicy.SameType => warnings.HasFlag(ObjectBatchWarning.SameType),
                ObjectReplacementPolicy.SameFamily => warnings.HasFlag(ObjectBatchWarning.SameFamily),
                _ => false
            };
            if (replacement == ObjectReplacementPolicy.Ask) preview = true;
        }
        var locked = (warnings & (ObjectBatchWarning.OutOfMap | ObjectBatchWarning.Boundary)) != 0;
        if (locked) { add = false; if (flags.HasFlag(ObjectBatchFlags.IgnoreAsk)) preview = true; }
        return (add, locked, preview);
    }
}

public sealed partial class NativeMapDocument
{
    private static uint PlacementNumber(NativeRecord record, string name, uint fallback = 0)
        => record.Fields.FirstOrDefault(field => field.Name == name) is { } field
            ? unchecked((uint)long.Parse(record.Read(field), CultureInfo.InvariantCulture)) : fallback;

    private static ObjectPlacementExisting? PlacementExisting(NativeRecord record)
    {
        if (record.Fields.Count == 0) return null;
        PlacedThingKind? kind = record.File.Signature == LegacyThings
            ? PlacementNumber(record, "node.type", uint.MaxValue) is var type && type <= 4 ? (PlacedThingKind)type : null
            : record.File.Signature != 0xBE ? null : record.Category switch
            {
                "GAMEHERO" or "GAMENEUTRAL" or "GAMEKEEPER" => PlacedThingKind.Creature,
                "GAMEDOOR" => PlacedThingKind.Door, "GAMETRAP" => PlacedThingKind.Trap,
                "GAMEOBJECT" => PlacementNumber(record, "bObjectID") is >= 0x75 and <= 0x81 ? PlacedThingKind.SpecialItem : PlacedThingKind.Object,
                _ => null
            };
        if (kind is null) return null;
        var idField = record.File.Signature == LegacyThings ? record.Fields.FirstOrDefault(field => field.Name == "id") : ValueCatalogues.PrimaryField(record);
        if (idField is null) return null;
        var id = PlacementNumber(record, idField.Name);
        var owner = PlacementNumber(record, record.File.Signature == LegacyThings ? "owner" : "bGlobalPlayer");
        if (record.Category == "GAMEHERO") owner = 1; else if (record.Category == "GAMENEUTRAL") owner = 2;
        // Original Game_AddThing repairs only values above MAP_MAXPLAYERS.
        if (owner > 7) owner = 2;
        var x = record.Fields.FirstOrDefault(field => record.File.Signature == LegacyThings ? field.Name == "x" : field.Name.EndsWith("LocationX", StringComparison.Ordinal));
        var y = record.Fields.FirstOrDefault(field => record.File.Signature == LegacyThings ? field.Name == "y" : field.Name.EndsWith("LocationY", StringComparison.Ordinal));
        if (x is null || y is null) return null;
        return new(record, kind.Value, id, (byte)owner, new(unchecked((int)PlacementNumber(record, x.Name)), unchecked((int)PlacementNumber(record, y.Name))));
    }

    /// <summary>All three source insertion modes and all five object families.
    /// This is the planning stage, not a substitute for the insertion workflow.</summary>
    public ObjectBatchPlacementPlan PlanObjectBatchPlacement(ObjectPlacementTemplate template,
        ObjectBatchPlacementOptions options, Func<int, int>? randomSample = null)
    {
        ArgumentNullException.ThrowIfNull(template); ArgumentNullException.ThrowIfNull(options);
        if (!Enum.IsDefined(template.Kind) || template.Owner > 7 || !Enum.IsDefined(options.Mode)
            || !Enum.IsDefined(options.Replacement) || (options.Flags & ~(ObjectBatchFlags)127) != 0)
            throw new ArgumentException("Unknown object placement options.");
        if (options.Mode == ObjectBatchMode.RandomRectangle && options.RandomFrequency is < 2 or > 32)
            throw new ArgumentOutOfRangeException(nameof(options), "Object random frequency must be 2–32.");
        TileRect bounds;
        if (options.Mode == ObjectBatchMode.One) bounds = new(options.Position.X, options.Position.Y, 1, 1);
        else if (options.WholeMap) bounds = new(1, 1, Width - 1, Height - 1);
        else bounds = options.Selection ?? throw new ArgumentException("Rectangle insertion requires a selection.");
        if (bounds.Width < 0 || bounds.Height < 0 || bounds.Width > 1024 || bounds.Height > 1024
            || !options.WholeMap && options.Mode != ObjectBatchMode.One && (bounds.Width == 0 || bounds.Height == 0)
            || (long)bounds.X + bounds.Width > int.MaxValue || (long)bounds.Y + bounds.Height > int.MaxValue)
            throw new ArgumentException("Invalid object candidate rectangle.");
        // One immutable fingerprint includes definitions, tiles, objects AND scripts.
        var fingerprint = GateGenerationFingerprint(); var records = Records();
        var thingFiles = files.Where(file => file.Signature == (IsLegacyProject ? LegacyThings : 0xBE)).ToArray();
        var supported = thingFiles.Length > 0 && thingFiles.All(file => IsLegacyProject ? file.UInt32(4) is >= 1 and <= 6 : file.UInt32(4) == 4)
            && !records.Any(record => thingFiles.Contains(record.File) && (record.Category == "Project.Opaque" || record.Fields.Count == 0
                || !IsLegacyProject && RecordTemplate.All.FirstOrDefault(template => template.Structure == record.Category) is { } layout
                    && (record.Length != layout.Size || record.Fields.Count != LegacySchemas.Fields(record.Category).Count)));
        var existing = records.Select(PlacementExisting).OfType<ObjectPlacementExisting>().ToArray();
        // Source chooses FIRST door before FIRST trap; ordinary before special,
        // regardless of their mixed native file order. Creatures never replace.
        var overlaps = new Dictionary<TilePoint, ObjectPlacementExisting>();
        var order = template.Kind switch
        {
            PlacedThingKind.Door or PlacedThingKind.Trap => new[] { PlacedThingKind.Door, PlacedThingKind.Trap },
            PlacedThingKind.Object or PlacedThingKind.SpecialItem => new[] { PlacedThingKind.Object, PlacedThingKind.SpecialItem },
            _ => []
        };
        foreach (var kind in order) foreach (var item in existing.Where(item => item.Kind == kind)) overlaps.TryAdd(item.Position, item);
        // Definitions are resolved once per plan, never once per map cell.
        var (water, lava) = template.Kind == PlacedThingKind.Creature ? PlacementCreatureFlags(template.TypeID, records) : ((bool?)null, (bool?)null);
        randomSample ??= new Random().Next;
        var candidates = new List<ObjectBatchPlacementCandidate>(); var sampledCells = 0;
        var preview = options.Flags.HasFlag(ObjectBatchFlags.Preview);
        for (var y = 0; y < bounds.Height; y++) for (var x = 0; x < bounds.Width; x++)
        {
            sampledCells++;
            if (options.Mode == ObjectBatchMode.RandomRectangle)
            {
                var sample = randomSample(options.RandomFrequency);
                if (sample < 0 || sample >= options.RandomFrequency) throw new ArgumentException("Object sampler returned an out-of-range value.");
                if (sample != 0) continue;
            }
            var position = new TilePoint(bounds.X + x, bounds.Y + y);
            var outside = position.X < 0 || position.Y < 0 || position.X >= Width || position.Y >= Height;
            var tile = outside ? new MapTile(0xFE, 2, 1) : ThingPlacementRules.EditorTile(GetTile(position.X, position.Y), IsEditorProtectedCell(position.X, position.Y));
            var owner = template.Owner == 0 ? tile.Owner : template.Owner;
            var warning = outside ? ObjectBatchWarning.OutOfMap | ObjectBatchWarning.BadTerrain : ObjectBatchWarning.None;
            if (!outside)
            {
                warning |= ThingPlacementRules.Evaluate(tile, template.Kind, template.TypeID, owner, water, lava) switch
                {
                    ThingTerrainPermission.BadTerrain => ObjectBatchWarning.BadTerrain,
                    ThingTerrainPermission.Enemy => ObjectBatchWarning.Enemy,
                    ThingTerrainPermission.Fatal => ObjectBatchWarning.Fatal,
                    ThingTerrainPermission.Unverified => ObjectBatchWarning.Incompatible,
                    _ => ObjectBatchWarning.None
                };
                if (tile.Terrain == 0xFE) warning |= ObjectBatchWarning.Boundary;
            }
            overlaps.TryGetValue(position, out var old);
            if (old is not null)
            {
                warning |= ObjectBatchWarning.Replacement;
                if (old.Kind == template.Kind)
                { warning |= ObjectBatchWarning.SameFamily; if (old.TypeID == template.TypeID) warning |= ObjectBatchWarning.SameType; }
            }
            if (!supported) warning |= ObjectBatchWarning.Incompatible;
            var decision = ThingPlacementRules.DefaultDecision(warning, options.Replacement, options.Flags);
            // Unknown serialization layouts/required flags cannot be made safe by
            // a checkbox. Preserve that explicit extra modern guard in the preview.
            var locked = decision.Locked || !supported || warning.HasFlag(ObjectBatchWarning.Incompatible);
            preview |= decision.Preview || locked;
            candidates.Add(new(candidates.Count, position, owner, warning, locked, decision.Add && !locked, old));
        }
        if (!fingerprint.AsSpan().SequenceEqual(GateGenerationFingerprint())) throw new InvalidOperationException("The document changed during placement planning.");
        return new(this, fingerprint, template, options, candidates.AsReadOnly(), sampledCells, candidates.Count != 0 && preview);
    }

    private (bool? Water, bool? Lava) PlacementCreatureFlags(uint id, IReadOnlyList<NativeRecord> records)
    {
        var catalogue = GlobalDefinitionDefaults.Catalogue("Creatures");
        var index = catalogue.ToList().FindIndex(entry => entry.ID == id);
        // Exact source unknown-ID loops: water falls through safe; lava fatal.
        if (index < 0) return (true, false);
        var family = LegacyGlobalSchemas.All.Single(item => item.Group == "Creatures"); var fields = family.Fields;
        var order = LegacyCompilerRules.DefinitionOrder("Creatures"); var nativeIndex = order.ToList().IndexOf(index);
        if (nativeIndex < 0) return (null, null);
        bool? Read(string name)
        {
            var field = fields.Single(item => item.Name == name);
            var defaultValue = field.Read(GlobalDefinitionDefaults.NativeDefault("Creatures", index)) != "0";
            if (IsLegacyProject)
            {
                if (records.Any(record => record.File.Signature == LegacyGlobals && record.Category == "Project.Opaque")) return null;
                var rows = records.Where(record => record.Category == "Project.GLOBALCREATURE").ToArray();
                if (rows.Length == 0) return defaultValue;
                if (rows.Length != GlobalDefinitionDefaults.ProjectCount("Creatures")) return null;
                return PlacementNumber(rows[index], name) != 0;
            }
            var groups = records.Where(record => record.Category == "Definition.Creatures").GroupBy(record => record.File).ToArray();
            if (groups.Length == 0)
                return files.Any(file => file.Signature == 0xAA || file.Signature == 0xF0 && file.ExportPath.EndsWith("Globals.kld", StringComparison.OrdinalIgnoreCase)) ? null : defaultValue;
            var selected = groups.Last().ToArray();
            if (nativeIndex >= selected.Length || !VerifiedDefinitionCatalogue(family, selected)) return null;
            return PlacementNumber(selected[nativeIndex], name) != 0;
        }
        return (Read("CanWalkWater"), Read("CanWalkLava"));
    }

    /// <summary>Validate preview selections against the original document and
    /// plan. This still does not insert/remove records or change any map bytes.</summary>
    public IReadOnlyList<ObjectBatchPlacementCandidate> ResolveObjectBatchPlacement(ObjectBatchPlacementPlan plan,
        IEnumerable<int>? selectedCandidateIndices = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Document != this) throw new ArgumentException("The placement preview belongs to another document.");
        if (!plan.Fingerprint.AsSpan().SequenceEqual(GateGenerationFingerprint())) throw new InvalidOperationException("The placement preview is stale; rebuild it.");
        var selected = selectedCandidateIndices?.ToArray() ?? plan.Candidates.Where(candidate => candidate.DefaultAdd).Select(candidate => candidate.Index).ToArray();
        if (selected.Distinct().Count() != selected.Length || selected.Any(index => index < 0 || index >= plan.Candidates.Count))
            throw new ArgumentException("Duplicate or unknown placement candidate.");
        var requested = selected.ToHashSet();
        var result = plan.Candidates.Where(candidate => requested.Contains(candidate.Index)).ToArray();
        if (result.Any(candidate => candidate.Locked)) throw new InvalidOperationException("A locked placement candidate cannot be enabled.");
        return new ReadOnlyCollection<ObjectBatchPlacementCandidate>(result);
    }
}
