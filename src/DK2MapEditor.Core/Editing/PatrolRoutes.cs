using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record PatrolPoint(NativeRecord Record, uint ID, uint NextID, uint WaitDelay, MapRegion? Region);
// EndID is zero, a missing/ambiguous target, or a repeated loop entry. The
// closing occurrence is separate: removing it opens the loop, not the area.
public sealed record PatrolRoute(IReadOnlyList<PatrolPoint> Points, uint EndID, bool Ambiguous = false)
{
    public bool Closed => EndID != 0 && Points.Any(point => point.ID == EndID);
    public int OccurrenceCount => Points.Count + (EndID == 0 ? 0 : 1);
}

public sealed partial class NativeMapDocument
{
    private static NativeField PatrolField(NativeRecord record, string native, string project) =>
        record.Fields.Single(field => field.Name == (record.Category == "GAMEAREA" ? native : project));
    private static uint PatrolNumber(NativeRecord record, string native, string project) =>
        uint.Parse(record.Read(PatrolField(record, native, project)), CultureInfo.InvariantCulture);
    private static uint PatrolWaitNumber(NativeRecord record)
    {
        var field=PatrolField(record,"wAreaWaitDelay","waitdelay");
        // Project waitdelay is a source LONG. Retain its bits for read-only
        // graphs; the source numeric request displays them as a signed LONG.
        return field.Kind==NativeValueKind.Int32?unchecked((uint)int.Parse(record.Read(field),CultureInfo.InvariantCulture))
            :uint.Parse(record.Read(field),CultureInfo.InvariantCulture);
    }

    public IReadOnlyList<PatrolPoint> PatrolAreas() => Records().Where(record => MapRegionKindOf(record) == MapRegionKind.Area)
        .Select(record => new PatrolPoint(record, PatrolNumber(record, "bAreaID", "id"),
            PatrolNumber(record, "bAreaNextID", "nextid"), PatrolWaitNumber(record), GetMapRegion(record))).ToArray();

    private static PatrolRoute TracePatrol(PatrolPoint first, IReadOnlyDictionary<uint, PatrolPoint[]> index)
    {
        var points = new List<PatrolPoint>(); var used = new HashSet<uint>(); var current = first;
        while (true)
        {
            points.Add(current); used.Add(current.ID);
            if (current.ID is 0 or > 255 || index[current.ID].Length != 1) return new(points, current.NextID, true);
            if (current.NextID == 0 || used.Contains(current.NextID)) return new(points, current.NextID);
            if (!index.TryGetValue(current.NextID, out var targets)) return new(points, current.NextID);
            if (targets.Length != 1 || current.NextID > 255) return new(points, current.NextID, true);
            current = targets[0];
        }
    }

    /// <summary>Read-only routes, including shared tails, partial/self loops and
    /// broken imported links. Viewing never repairs or invents links.</summary>
    public IReadOnlyList<PatrolRoute> PatrolRoutes()
    {
        var areas = PatrolAreas(); var index = areas.GroupBy(point => point.ID).ToDictionary(group => group.Key, group => group.ToArray());
        var incoming = areas.Where(point => point.NextID != 0).Select(point => point.NextID).ToHashSet();
        var seen = new HashSet<uint>(); var routes = new List<PatrolRoute>();
        void Add(PatrolPoint point) { var route = TracePatrol(point, index); routes.Add(route); foreach (var item in route.Points) seen.Add(item.ID); }
        // Each head gets its complete view, even when another head shares its tail.
        foreach (var point in areas.Where(point => point.NextID != 0 && !incoming.Contains(point.ID))) Add(point);
        foreach (var point in areas.Where(point => point.NextID != 0 || incoming.Contains(point.ID)))
            if (!seen.Contains(point.ID)) Add(point);
        return routes;
    }

    public PatrolRoute PatrolRouteFrom(NativeRecord record)
    {
        var areas = PatrolAreas(); var point = areas.SingleOrDefault(point => SameRecord(point.Record, record))
            ?? throw new ArgumentException("Select a current area record.", nameof(record));
        return TracePatrol(point, areas.GroupBy(item => item.ID).ToDictionary(group => group.Key, group => group.ToArray()));
    }

    private PatrolPoint CurrentPatrolPoint(NativeRecord record)
    {
        var areas = PatrolAreas(); var point = areas.SingleOrDefault(point => SameRecord(point.Record, record))
            ?? throw new ArgumentException("Select a current area record.", nameof(record));
        if (point.ID is 0 or > 255 || areas.Count(item => item.ID == point.ID) != 1)
            throw new InvalidOperationException("Patrol editing requires an unambiguous area ID in 1–255.");
        return point;
    }
    private PatrolRoute CurrentPatrolRoute(PatrolRoute requested)
    {
        if (requested.Points.Count == 0) throw new ArgumentException("Select a patrol route.", nameof(requested));
        var current = PatrolRouteFrom(requested.Points[0].Record);
        if (current.Ambiguous || requested.Ambiguous || current.EndID != requested.EndID || current.Points.Count != requested.Points.Count
            || current.Points.Zip(requested.Points).Any(pair => !SameRecord(pair.First.Record, pair.Second.Record) || pair.First.ID != pair.Second.ID || pair.First.NextID != pair.Second.NextID))
            throw new InvalidOperationException("The route changed or contains ambiguous area identities. Refresh it before editing.");
        return current;
    }
    private void SetPatrolNext(PatrolPoint point, uint next) =>
        SetField(point.Record, PatrolField(point.Record, "bAreaNextID", "nextid"), next.ToString(CultureInfo.InvariantCulture));
    private void SetPatrolWait(PatrolPoint point, int wait)
    {
        if (wait is < 0 or > 250) throw new ArgumentOutOfRangeException(nameof(wait), "Original patrol pauses are 0–250.");
        SetField(point.Record, PatrolField(point.Record, "wAreaWaitDelay", "waitdelay"), wait.ToString(CultureInfo.InvariantCulture));
    }
    public void SetPatrolPause(NativeRecord record, int wait) => Edit("Change patrol pause", () => SetPatrolWait(CurrentPatrolPoint(record), wait));

    public void LinkPatrolAreas(NativeRecord from, NativeRecord to) => Edit("Draw patrol link", () =>
    {
        var source = CurrentPatrolPoint(from); var target = CurrentPatrolPoint(to);
        if (source.NextID != 0) throw new InvalidOperationException("Continue from an unlinked patrol endpoint.");
        SetPatrolNext(source, target.ID); // Joining existing paths and self loops are legal.
    });

    public void AppendPatrolPoint(PatrolRoute requested, NativeRecord record, int wait) => Edit("Append patrol point", () =>
    {
        var route = CurrentPatrolRoute(requested); var target = CurrentPatrolPoint(record);
        if (route.EndID != 0) throw new InvalidOperationException("Append requires an open route with an unlinked endpoint.");
        SetPatrolWait(target, wait); SetPatrolNext(route.Points[^1], target.ID);
        // Preserve target's existing outgoing link: original append allows linked areas.
    });

    public void InsertPatrolPoint(PatrolRoute requested, int occurrence, NativeRecord record, int wait) => Edit("Insert patrol point", () =>
    {
        var route = CurrentPatrolRoute(requested); var target = CurrentPatrolPoint(record);
        if (occurrence < 0 || occurrence >= route.OccurrenceCount) throw new ArgumentOutOfRangeException(nameof(occurrence));
        if (target.NextID != 0) throw new InvalidOperationException("An inserted area must not already have an outgoing patrol link.");
        var destination = occurrence < route.Points.Count ? route.Points[occurrence].ID : route.EndID;
        SetPatrolWait(target, wait);
        if (occurrence > 0) SetPatrolNext(route.Points[occurrence - 1], target.ID);
        SetPatrolNext(target, destination);
    });

    public void RemovePatrolOccurrence(PatrolRoute requested, int occurrence) => Edit("Remove patrol point link", () =>
    {
        var route = CurrentPatrolRoute(requested);
        if (occurrence < 0 || occurrence >= route.OccurrenceCount) throw new ArgumentOutOfRangeException(nameof(occurrence));
        var sequence = route.Points.Select(point => point.ID).Concat(route.EndID == 0 ? [] : new[] { route.EndID }).ToList();
        var removed = sequence[occurrence]; var successor = occurrence + 1 < sequence.Count ? sequence[occurrence + 1] : 0;
        if (occurrence > 0) SetPatrolNext(route.Points[occurrence - 1], successor);
        sequence.RemoveAt(occurrence);
        // A closing occurrence refers to an area still present earlier in the
        // route. Only clear its link if it vanished or became the final point.
        var point = route.Points.FirstOrDefault(point => point.ID == removed);
        if (point is not null && (!sequence.Contains(removed) || sequence.Count == 0 || sequence[^1] == removed)) SetPatrolNext(point, 0);
    });

    public void ClearPatrolRoute(PatrolRoute requested) => Edit("Remove patrol route links", () =>
    {
        var route = CurrentPatrolRoute(requested);
        foreach (var point in route.Points) if (point.NextID != 0) SetPatrolNext(point, 0);
    });

    public NativeRecord? FindPatrolArea(TileRect selection, bool unlinkedOnly = false)
    {
        ValidateMapRegionBounds(MapRegionKind.Area, selection);
        PatrolPoint? match = null;
        foreach (var point in PatrolAreas())
        {
            if (unlinkedOnly && point.NextID != 0 || point.Region is not { Bounds: var bounds }) continue;
            if (selection.X < bounds.X || selection.Y < bounds.Y || (long)selection.X + selection.Width > (long)bounds.X + bounds.Width || (long)selection.Y + selection.Height > (long)bounds.Y + bounds.Height) continue;
            if (selection == bounds) return point.Record;
            // Original DrawPath chooses the later candidate unless it is larger
            // in BOTH dimensions; this is not a smallest-area sort.
            if (match?.Region is { } previous && bounds.Width > previous.Bounds.Width && bounds.Height > previous.Bounds.Height) continue;
            match = point;
        }
        return match?.Record;
    }
}
