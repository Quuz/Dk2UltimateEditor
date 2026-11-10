using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DK2MapEditor.Core.Editing;

[Flags]
public enum HeroGateGenerationFlags : uint
{
    None = 0, GateDisplay = 1, GateDestroy = 2, GateRelease = 4,
    AreaDestroy = 0x200, AreaRelease = 0x400, Events = 0x100000, All = 0x100607
}
public sealed record HeroGateTemplate(string Name, string Info, uint Color, uint TextColor);
public sealed record HeroGateGenerationOptions(HeroGateGenerationFlags Flags, int Keepers,
    HeroGateTemplate Gate, HeroGateTemplate Area)
{
    public static HeroGateGenerationOptions Defaults(NativeMapDocument? document = null) => new(HeroGateGenerationFlags.All,
        document is null ? 1 : NewMapOptions.ForDocument(document).Players, new("Gate %g", "Area %a", 0xAAAAAA, 0xAAAAAA), new("Area %a", "Gate %g", 0xFFFF00, 0xFFFF00));
}
public sealed record HeroGateSite(int Type, TileRect Footprint, bool CannotDestroy, NativeRecord? Gate, NativeRecord? Area,
    uint? GateID, uint? AreaID, bool HasEvents);
public sealed record HeroGateGenerationItem(HeroGateSite Site, bool AddGate, bool AddArea, bool AddEvents,
    uint? GateID, uint? AreaID, TileRect GateBounds, TileRect AreaBounds, string GateName, string GateInfo, string AreaName, string AreaInfo);
public sealed class HeroGateGenerationPlan
{
    internal HeroGateGenerationPlan(NativeMapDocument owner, byte[] fingerprint, HeroGateGenerationOptions options, IReadOnlyList<HeroGateGenerationItem> items)
    { Owner = owner; Revision = owner.EditRevision; Fingerprint = fingerprint; Options = options; Items = items; }
    internal NativeMapDocument Owner { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    public HeroGateGenerationOptions Options { get; }
    public IReadOnlyList<HeroGateGenerationItem> Items { get; }
    public int Gates => Items.Count(item => item.AddGate);
    public int Areas => Items.Count(item => item.AddArea);
    public int Conditions => Items.Count(item => item.AddEvents) * Options.Keepers;
    public int Actions => Conditions;
    public bool HasWork => Gates + Areas + Conditions > 0;
}

public sealed partial class NativeMapDocument
{
    private static string GateValue(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
    private static uint GateRegionID(NativeRecord record) => uint.Parse(GateValue(record, record.Category == "GAMEAREA" ? "bAreaID" : "id"), CultureInfo.InvariantCulture);
    private byte[] GateGenerationFingerprint()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in TransactionFiles()) { hash.AppendData(Encoding.UTF8.GetBytes(file.ExportPath)); hash.AppendData(file.Data.Span); }
        return hash.GetHashAndReset();
    }

    /// <summary>Source-order detection; the last row/column are excluded just
    /// like RectEx_GetGatesList. Viewing never changes tiles or scripts.</summary>
    public IReadOnlyList<HeroGateSite> HeroGateSites()
    {
        var regions = MapRegions();
        var gates = regions.Where(region => region.Kind == MapRegionKind.Gate
            && (region.Record.Category != "GAMEGATE" || GateValue(region.Record, "bGateType") != "14")).ToArray();
        var areas = regions.Where(region => region.Kind == MapRegionKind.Area).ToArray();
        var graph = ScriptGraph.Build(this); var sites = new List<HeroGateSite>(); var covered = new bool[Width * Height];
        var gateTerrains = LegacyCompilerRules.Elements("Gates").Select(entry => ((byte)(entry.ID >> 24), (byte)(entry.ID >> 8))).ToHashSet();
        for (var y = 0; y < Height - 1; y++) for (var x = 0; x < Width - 1; x++)
        {
            if (covered[y * Width + x]) continue;
            var tile = GetTile(x, y);
            var type = (tile.Terrain, tile.Variant) switch { (29, 1) => 2, (33, 1) => 3, (37, 1) => 6, (37, 2) => 7, _ => 0 };
            if (type == 0) continue;
            var footprint = new TileRect(x, y, type == 3 ? 2 : type == 6 ? 3 : 1, type == 3 ? 2 : type == 7 ? 3 : 1);
            for (var row = y; row < Math.Min(Height, y + footprint.Height); row++)
                for (var column = x; column < Math.Min(Width, x + footprint.Width); column++) covered[row * Width + column] = true;
            bool Inside(TileRect bounds) => bounds.X >= x && bounds.Y >= y && (long)bounds.X + bounds.Width <= (long)x + footprint.Width && (long)bounds.Y + bounds.Height <= (long)y + footprint.Height;
            var gate = gates.FirstOrDefault(region => Inside(region.Bounds));
            var expectedArea = type == 3 ? new TileRect(x - 1, y - 1, 4, 4) : footprint;
            var area = areas.FirstOrDefault(region => type is 6 or 7 ? Inside(region.Bounds) : region.Bounds == expectedArea);
            var cannotDestroy = type != 3;
            if (!cannotDestroy)
                for (var dy = -1; dy <= 2; dy++) for (var dx = -1; dx <= 2; dx++)
                {
                    if (dx is >= 0 and <= 1 && dy is >= 0 and <= 1) continue;
                    var px = x + dx; var py = y + dy;
                    if (px < 0 || px >= Width || py < 0 || py >= Height) { cannotDestroy = true; continue; }
                    var neighbor = GetTile(px, py);
                    // Original catalogue discriminator, NOT native reserved byte.
                    // Safe intended boundary rule; do not reproduce the C literal
                    // 0x1E00000000 typo or out-of-bounds perimeter reads.
                    if (gateTerrains.Contains((neighbor.Terrain, neighbor.Variant)) || neighbor.Terrain is 1 or 7 or 30 or 35 or 254) cannotDestroy = true;
                }
            if (cannotDestroy) area = areas.FirstOrDefault(region => region.Bounds == footprint) ?? area;
            var gateID = gate is null ? (uint?)null : IsLegacyProject ? uint.Parse(GateValue(gate.Record, "id"), CultureInfo.InvariantCulture) : (uint)NativeGateID(gate.Record);
            var areaID = area is null ? (uint?)null : GateRegionID(area.Record);
            var hasEvents = false;
            if (!cannotDestroy && gate is not null && area is not null && graph.DuplicateIDs.Count == 0)
            {
                // RectEx_GetGatesList and the compiler use the first matching
                // area binding. A later duplicate cannot supply its events.
                var roots = graph.Roots.Where(root => root.Scope == 4 && (IsLegacyProject ? GateValue(root.Owner, "area") == areaID!.Value.ToString(CultureInfo.InvariantCulture) : SameRecord(root.Owner, area.Record))).Take(1);
                foreach (var root in roots)
                {
                    var visited = new HashSet<ushort>(); var id = root.Target;
                    while (id != 0 && visited.Add(id) && graph.Events.TryGetValue(id, out var item) && item.Type?.RecordSignature == 0xD5)
                    {
                        if (item.Type.Name == "TRIGGER_CLAIMAREA" && GateValue(item.Record, IsLegacyProject ? "param5" : "Param5") == "12")
                        {
                            var child = item.Edges.Single(edge => edge.Field.Name is "and" or "ChildAnd").Target;
                            if (graph.Reachable(child).Any(target => graph.Events.TryGetValue(target, out var action) && action.Type?.Name == "ACTION_DESTROYGATE")) { hasEvents = true; break; }
                        }
                        id = item.Edges.Single(edge => edge.Field.Name is "or" or "NextOr").Target;
                    }
                    if (hasEvents) break;
                }
            }
            sites.Add(new(type, footprint, cannotDestroy, gate?.Record, area?.Record, gateID, areaID, hasEvents));
        }
        return sites;
    }

    public static string FormatHeroGateText(string pattern, uint? gate, uint? area, int size)
    {
        if (size is not (64 or 128)) throw new ArgumentOutOfRangeException(nameof(size));
        var output = new StringBuilder(); var missing = false;
        for (var index = 0; index < pattern.Length && output.Length < size - 4; index++)
        {
            if (pattern[index] != '%') { output.Append(pattern[index]); continue; }
            if (++index == pattern.Length) break;
            var specifier = char.ToLowerInvariant(pattern[index]);
            if (specifier == '%') output.Append('%');
            else if (specifier is 'a' or 'z') { if (area is null) missing = true; else output.Append(area.Value.ToString(CultureInfo.InvariantCulture)); }
            else if (specifier is 'g' or 'p') { if (gate is null) missing = true; else output.Append(gate.Value.ToString(CultureInfo.InvariantCulture)); }
            // Unknown specifiers disappear, as in the source formatter.
        }
        return missing ? "" : output.ToString();
    }

    public HeroGateGenerationPlan PlanHeroGateGeneration(HeroGateGenerationOptions options)
    {
        if ((options.Flags & ~HeroGateGenerationFlags.All) != 0 || options.Keepers is < 1 or > 5) throw new ArgumentException("Unverified generator flags or keeper count.");
        if (options.Flags.HasFlag(HeroGateGenerationFlags.Events) && (!options.Flags.HasFlag(HeroGateGenerationFlags.GateDestroy) || !options.Flags.HasFlag(HeroGateGenerationFlags.AreaDestroy)))
            throw new ArgumentException("Destruction events require both gate-destruction and area-destruction options.");
        foreach (var template in new[] { options.Gate, options.Area })
            foreach (var (name, text) in new[] { ("name", template.Name), ("info", template.Info) })
                _ = LegacyProjectSchemas.Fields("MAPRECTEX").Single(field => field.Name == name).Encode(text);
        var sites = HeroGateSites(); var flags = options.Flags;
        var areaRecords = Records().Where(record => MapRegionKindOf(record) == MapRegionKind.Area).ToArray();
        var usedAreas = areaRecords.Select(GateRegionID).ToHashSet();
        var gateRecords = Records().Where(record => MapRegionKindOf(record) == MapRegionKind.Gate && (record.Category != "GAMEGATE" || GateValue(record, "bGateType") != "14")).ToArray();
        var usedGates = gateRecords.Select(record => IsLegacyProject ? uint.Parse(GateValue(record, "id"), CultureInfo.InvariantCulture) : (uint)NativeGateID(record)).ToHashSet();
        var needs = sites.Select(site => (Site: site,
            AddGate: site.Gate is null && (flags.HasFlag(HeroGateGenerationFlags.GateRelease) || site.Type is 6 or 7 && flags.HasFlag(HeroGateGenerationFlags.GateDisplay) || !site.CannotDestroy && flags.HasFlag(HeroGateGenerationFlags.GateDestroy)),
            AddArea: site.Area is null && (flags.HasFlag(HeroGateGenerationFlags.AreaRelease) || !site.CannotDestroy && flags.HasFlag(HeroGateGenerationFlags.AreaDestroy)),
            AddEvents: !site.CannotDestroy && !site.HasEvents && flags.HasFlag(HeroGateGenerationFlags.Events))).ToArray();
        var neededAreas = needs.Count(item => item.AddArea); var neededGates = needs.Count(item => item.AddGate);
        if ((long)areaRecords.Length + neededAreas > 255 || (long)gateRecords.Length + neededGates > 255)
        {
            var revision = EditRevision; var fingerprint = GateGenerationFingerprint();
            throw new GateGenerationCapacityException(areaRecords.Length, gateRecords.Length, neededAreas, neededGates,
                () => !IsSaveLocked && EditRevision == revision && fingerprint.AsSpan().SequenceEqual(GateGenerationFingerprint()));
        }
        var references = Records().SelectMany(EntityReferences.From).Where(LiveReference).ToArray();
        uint Allocate(HashSet<uint> used)
        { for (uint id = 1; id <= 255; id++) if (used.Add(id)) return id; throw new InvalidOperationException("No unused area/gate IDs remain."); }
        var items = new List<HeroGateGenerationItem>();
        foreach (var (site, addGate, addArea, addEvents) in needs)
        {
            if (addGate || addArea || addEvents)
                ValidateMapRegionBounds(MapRegionKind.Area, site.Footprint);
            var gateID = addGate ? Allocate(usedGates) : site.GateID; var areaID = addArea ? Allocate(usedAreas) : site.AreaID;
            if (addArea && references.Any(reference => reference.Kind == EntityKind.Area && reference.Record.Read(reference.Field) == areaID!.Value.ToString(CultureInfo.InvariantCulture)))
                throw new InvalidOperationException("A new identity would acquire dangling references. Repair them before generation.");
            var center = site.Type == 6 ? new TilePoint(site.Footprint.X + 1, site.Footprint.Y) : site.Type == 7 ? new(site.Footprint.X, site.Footprint.Y + 1) : new(site.Footprint.X, site.Footprint.Y);
            var areaBounds = !site.CannotDestroy ? new TileRect(site.Footprint.X - 1, site.Footprint.Y - 1, 4, 4) : site.Type is 6 or 7 ? new(center.X, center.Y, 1, 1) : site.Footprint;
            if (addGate) ValidateMapRegionBounds(MapRegionKind.Gate, new(center.X, center.Y, 1, 1));
            if (addArea) ValidateMapRegionBounds(MapRegionKind.Area, areaBounds);
            if (addEvents && (gateID is null or 0 or > 255 || areaID is null or 0 or > 255)) throw new InvalidOperationException("Destruction events require verified gate and area identities.");
            if (addEvents && (!addArea && areaRecords.Count(record => GateRegionID(record) == areaID) != 1
                || IsLegacyProject && !addGate && gateRecords.Count(record => GateValue(record, "id") == gateID!.Value.ToString(CultureInfo.InvariantCulture)) != 1))
                throw new InvalidOperationException("Repair shared area/gate identities before generating events.");
            items.Add(new(site, addGate, addArea, addEvents, gateID, areaID, new(center.X, center.Y, 1, 1), areaBounds,
                FormatHeroGateText(options.Gate.Name, gateID, areaID, 64), FormatHeroGateText(options.Gate.Info, gateID, areaID, 128),
                FormatHeroGateText(options.Area.Name, gateID, areaID, 64), FormatHeroGateText(options.Area.Info, gateID, areaID, 128)));
        }
        return new(this, GateGenerationFingerprint(), options, items.AsReadOnly());
    }

    internal bool IsGateGenerationApprovalCurrent(HeroGateGenerationPlan plan) => ReferenceEquals(plan.Owner, this)
        && !IsSaveLocked && EditRevision == plan.Revision && plan.Fingerprint.AsSpan().SequenceEqual(GateGenerationFingerprint());

    public void ApplyHeroGateGeneration(HeroGateGenerationPlan plan)
        => ApplyHeroGateGenerationCore(plan, false);

    /// <summary>Editor caller: one atomic Undo, with the source's separate
    /// increments for each new region, logical scope and event. Native zero
    /// roots count as absent logical links, as in the original game importer.</summary>
    public void ApplyEditorHeroGateGeneration(HeroGateGenerationPlan plan)
        => ApplyHeroGateGenerationCore(plan, true);

    private void ApplyHeroGateGenerationCore(HeroGateGenerationPlan plan, bool editorCaller)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Owner != this || !GateGenerationFingerprint().AsSpan().SequenceEqual(plan.Fingerprint)) throw new InvalidOperationException("Refresh the generation preview after map changes.");
        uint increments = 0;
        EditCore("Generate hero gates, areas and destruction events", () =>
        {
            var conditionType = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_CLAIMAREA");
            var actionType = LegacyEventSchemas.All.Single(type => type.Name == "ACTION_DESTROYGATE");
            var generated = new List<(HeroGateGenerationItem Item, NativeRecord? Gate, NativeRecord? Area)>();
            void Template(NativeRecord record, HeroGateTemplate template, string name, string info)
            {
                if (!IsLegacyProject) return; // Names/colours do not exist in native records.
                foreach (var (field, value) in new[] { ("name", name), ("info", info), ("rect.color", template.Color.ToString(CultureInfo.InvariantCulture)), ("rect.textcolor", template.TextColor.ToString(CultureInfo.InvariantCulture)) })
                    SetField(record, record.Fields.Single(item => item.Name == field), value);
            }
            foreach (var item in plan.Items)
            {
                var gate = item.AddGate ? AddMapRegion(MapRegionKind.Gate, item.GateBounds) : item.Site.Gate;
                var area = item.AddArea ? AddMapRegion(MapRegionKind.Area, item.AreaBounds) : item.Site.Area;
                if (item.AddGate) increments++;
                if (item.AddArea) increments++;
                if (item.AddArea && GateRegionID(area!) != item.AreaID || IsLegacyProject && item.AddGate && uint.Parse(GateValue(gate!, "id"), CultureInfo.InvariantCulture) != item.GateID)
                    throw new InvalidOperationException("Allocated region identities do not match the preview.");
                if (item.AddGate) Template(gate!, plan.Options.Gate, item.GateName, item.GateInfo);
                if (item.AddArea) Template(area!, plan.Options.Area, item.AreaName, item.AreaInfo);
                generated.Add((item, gate, area));
            }
            foreach (var (item, gate, area) in generated.Where(value => value.Item.AddEvents))
            {
                if (gate is null || area is null) throw new InvalidOperationException("The planned event owner/gate is missing.");
                var owner = ScriptOwners().Single(candidate => candidate.Scope == 4 && SameRecord(candidate.Record, area));
                var existingScope = FindScriptScope(owner);
                // DKM may retain a real, empty TRIGGERLINK. Native GAMEAREA
                // embeds the root word: import creates a link only if nonzero
                // (Triggers_CreateActionPtFromArea), so these cases differ.
                if (existingScope is null || !HasStoredScriptScope(existingScope)) increments++;
                var root = EnsureScriptScope(owner); var graph = ScriptGraph.Build(this); RequireUnambiguousGraph(graph);
                if (graph.HasCycle(root.Target)) throw new InvalidOperationException("Repair the cyclic area scenario before generation.");
                if (graph.Reachable(root.Target).Any(id => !graph.Events.ContainsKey(id))) throw new InvalidOperationException("Repair the area scenario's missing links before generation.");
                var tail = root; var visited = new HashSet<ushort>();
                while (tail.Target != 0)
                {
                    if (!visited.Add(tail.Target) || !graph.Events.TryGetValue(tail.Target, out var previous)) throw new InvalidOperationException("Repair the area scenario's missing links before generation.");
                    tail = previous.Edges.Single(edge => edge.Field.Name is "or" or "next" or "NextOr");
                }
                void Argument(ushort id, string field, string value)
                { var record = ScriptGraph.Build(this).Events[id].Record; SetField(record, record.Fields.Single(candidate => candidate.Name == field), value); }
                for (var keeper = 3; keeper < plan.Options.Keepers + 3; keeper++)
                {
                    var conditionID = ScriptGraph.ID(InsertScriptEvent(tail.Owner, tail.Field, conditionType));
                    increments++;
                    Argument(conditionID, IsLegacyProject ? "param1" : "Param1", "5"); Argument(conditionID, IsLegacyProject ? "param2" : "Param2", keeper.ToString(CultureInfo.InvariantCulture));
                    Argument(conditionID, IsLegacyProject ? "param5" : "Param5", "12"); Argument(conditionID, IsLegacyProject ? "repeat" : "Repeat", "1");
                    var condition = ScriptGraph.Build(this).Events[conditionID];
                    var child = condition.Edges.Single(edge => edge.Field.Name is "and" or "ChildAnd");
                    var actionID = ScriptGraph.ID(InsertScriptEvent(child.Owner, child.Field, actionType));
                    increments++;
                    if (IsLegacyProject) Argument(actionID, "param5", item.GateID!.Value.ToString(CultureInfo.InvariantCulture));
                    else
                    {
                        var bounds = GetMapRegion(gate)!.Bounds;
                        Argument(actionID, "NativeArgument5", (bounds.X + 1).ToString(CultureInfo.InvariantCulture));
                        Argument(actionID, "NativeArgument7", (bounds.Y + 1).ToString(CultureInfo.InvariantCulture));
                        SetNativeGateTarget(ScriptGraph.Build(this).Events[actionID].Record,gate);
                    }
                    Argument(actionID, IsLegacyProject ? "one" : "Repeat", "1");
                    tail = ScriptGraph.Build(this).Events[conditionID].Edges.Single(edge => edge.Field.Name is "or" or "NextOr");
                }
            }
        }, editorCaller ? () => new EditorDrawingCompletion(increments != 0, increments) : null);
    }
}
