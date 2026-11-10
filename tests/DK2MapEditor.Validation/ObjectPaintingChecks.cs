using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class ObjectPaintingChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException) { return; } throw new Exception("Unsafe object painting accepted."); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> before) => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Object painting/undo changed unexpected bytes.");
    private static NativeMapDocument New(bool native)
    {
        var doc = NativeMapDocument.CreateNewMap(new("Paint", "Paint", 12, 10, NativeFormat: native));
        doc.Paint([new(2, 2), new(3, 3), new(4, 4)], new(3, 2, 1)); return doc;
    }
    private static void Seed(NativeMapDocument doc, ObjectBatchTemplate template, TilePoint position)
        => doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(template, new(Position: position, Flags: (ObjectBatchFlags)127)), [0]);
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2MapEditor.ObjectPainting", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false, true }) { Terrain(native); Creation(native); Updates(native, directory); Opposites(native, directory); }
            foreach (var native in new[] { false, true }) NoOp(native);
            Historical(directory);
            EditorObjectPaintingCallerChecks.Run();
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void Terrain(bool native)
    {
        var doc = New(native); var before = Snapshot(doc);
        foreach (var kind in Enum.GetValues<PlacedThingKind>())
        {
            var template = ObjectBatchTemplate.Default(kind);
            foreach (var point in new[] { new TilePoint(-1, 2), new TilePoint(12, 2), new TilePoint(0, 2), new TilePoint(1, 1) })
            { var blocked = doc.PlanObjectPainting(template, point); Expect(blocked.Locked, "Outside/border/solid terrain paintable."); Reject(() => doc.ApplyObjectPainting(blocked)); Exact(before); }
        }
        var trap = ObjectBatchTemplate.Default(PlacedThingKind.Trap);
        // Source raw-owner 0 bypasses the unclaimed neutral territory check, unlike explicit owner 3.
        Expect(!doc.PlanObjectPainting(trap, new(2, 2)).Locked, "Automatic owner incorrectly refused on unclaimed land.");
        Expect(doc.PlanObjectPainting(trap with { Owner = 3 }, new(2, 2)).Locked, "Door/trap enemy territory not refused.");
        foreach (var terrain in new byte[] { 4, 5, 11, 14, 40 })
        { doc.Paint([new(2, 2)], new(terrain, 3, 1)); Expect(doc.PlanObjectPainting(trap, new(2, 2)).Locked, "Door/trap forbidden terrain accepted."); }
        doc.Paint([new(2, 2)], new(5, 2, 1));
        var fatal = doc.PlanObjectPainting(ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { TypeID = 999 }, new(2, 2));
        Expect(!fatal.Locked && fatal.Permission == ThingTerrainPermission.Fatal, "Source nonzero fatal permission became hard refusal.");
        before = Snapshot(doc); doc.ApplyObjectPainting(fatal); doc.Undo(); Exact(before);
        doc.Paint([new(2, 2)], new(8, 3, 1));
        var enemy = doc.PlanObjectPainting(ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Owner = 4 }, new(2, 2));
        Expect(!enemy.Locked && enemy.Permission == ThingTerrainPermission.Enemy, "Source enemy warning became hard refusal.");
        before = Snapshot(doc); doc.ApplyObjectPainting(enemy); doc.Undo(); Exact(before);
        var stale = doc.PlanObjectPainting(trap, new(3, 3)); doc.Paint([new(3, 3)], new(2, 2, 1)); before = Snapshot(doc);
        Reject(() => doc.ApplyObjectPainting(stale)); Exact(before); Reject(() => New(native).ApplyObjectPainting(stale));
    }
    private static void Creation(bool native)
    {
        foreach (var kind in Enum.GetValues<PlacedThingKind>()) foreach (var owner in new byte[] { 0, 1, 2 })
        {
            var doc = New(native); doc.Paint([new(2, 2)], new(8, owner == 0 ? (byte)3 : owner, 1));
            var template = ObjectBatchTemplate.Default(kind) with { Owner = owner, HeroOptions = 27, Objective = 13, Area = 6, Enemy = 4, Health = 72, GoldHeld = 19 };
            var before = Snapshot(doc); var plan = doc.PlanObjectPainting(template, new(2, 2)); Exact(before);
            Expect(plan.EffectiveTemplate.Owner == (owner == 0 ? 3 : owner) && plan.EffectiveTemplate.HeroOptions == (owner == 1 ? 27u : 0u)
                && plan.EffectiveTemplate.Objective == (owner == 1 ? 13u : 0u) && plan.EffectiveTemplate.Area == (owner == 1 ? 6u : 0u)
                && plan.EffectiveTemplate.Enemy == (owner == 1 ? 4u : 0u), "Fresh nonhero template normalization differs from source.");
            var changes = 0; doc.Changed += (_, _) => changes++; var history = doc.GetHistory().Undo.Count;
            Expect(doc.ApplyObjectPainting(plan) == 1 && changes == 1 && doc.GetHistory().Undo.Count == history + 1, "Single creation not atomic.");
            var first = doc.ObjectManagerEntries().Single(); Expect(first.Thing.Kind == kind && first.Thing.Owner == plan.EffectiveTemplate.Owner, "Painted family/owner wrong.");
            if (kind == PlacedThingKind.Creature)
            {
                doc.ApplyObjectPainting(doc.PlanObjectPainting(template, new(2, 2)));
                Expect(doc.ObjectManagerEntries().Count == 2, "Creature click updated existing creature instead of stacking.");
                if (!native) Expect(doc.ObjectManagerEntries().All(entry => entry.Identity == 0), "Source newly painted creatures must defer identities until an Action Point is attached.");
                using (var point = doc.CreateEditorScenarioPointSession(initialScope: 2))
                    Expect(point.Choices.Count(choice => choice.CanSelect) == 2, "Both freshly painted creatures must remain available to the Action Point owner page.");
                doc.Undo();
            }
            doc.Undo(); Exact(before);
        }
    }
    private static void Updates(bool native, string directory)
    {
        foreach (var kind in new[] { PlacedThingKind.Door, PlacedThingKind.Trap, PlacedThingKind.Object, PlacedThingKind.SpecialItem })
        {
            var doc = New(native); var template = ObjectBatchTemplate.Default(kind) with { Owner = 2, Health = 71, GoldHeld = 17, CreatureOptions = 0x87654321, HeroOptions = 27, Objective = 13, Area = 6, Enemy = 4, DataAll = 2 };
            Seed(doc, template, new(2, 2)); doc.DuplicateRecord(doc.ObjectManagerEntries().Single().Record);
            var first = doc.ObjectManagerEntries().First(); var secondBytes = first.Record.File.Data.Slice(doc.ObjectManagerEntries()[1].Record.Offset, first.Record.Length).ToArray();
            if (kind != PlacedThingKind.Trap)
            { var scope = kind == PlacedThingKind.Door ? 64u : kind == PlacedThingKind.Object ? 128u : 256u; var owner = doc.ScriptOwners().First(owner => owner.Scope == scope); var root = doc.EnsureScriptScope(owner); doc.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER")); }
            var before = Snapshot(doc); first = doc.ObjectManagerEntries().First(); var original = doc.ObjectTemplateForModification(first.Record);
            var incoming = template with { TypeID = kind == PlacedThingKind.SpecialItem ? 118u : 3u, Health = 999, GoldHeld = 999, CreatureOptions = 1, HeroOptions = 5, Objective = 27, Area = 9, Enemy = 7, DataAll = 7 };
            var plan = doc.PlanObjectPainting(incoming, new(2, 2));
            Expect(plan.Updating?.Record.Offset == first.Record.Offset && plan.Removals.Count == 0, "Painting failed first same-kind lookup.");
            Expect(plan.EffectiveTemplate.Health == original.Health && plan.EffectiveTemplate.GoldHeld == original.GoldHeld && plan.EffectiveTemplate.CreatureOptions == original.CreatureOptions
                && plan.EffectiveTemplate.Objective == original.Objective && plan.EffectiveTemplate.Area == original.Area && plan.EffectiveTemplate.Enemy == original.Enemy
                && plan.EffectiveTemplate.HeroOptions == 5, "Update overwrote source-retained fields or normalized old nonhero.");
            doc.ApplyObjectPainting(plan); var rows = doc.ObjectManagerEntries();
            Expect(rows.Count == 2 && rows[0].Identity == first.Identity && rows[0].Thing.TypeID == incoming.TypeID, "Update lost stable identity/type.");
            Expect(rows[1].Record.File.Data.Span.Slice(rows[1].Record.Offset, rows[1].Record.Length).SequenceEqual(secondBytes), "Same-kind update touched another entry.");
            foreach (var pair in before.Where(pair => pair.Key.Signature != (native ? 0xBE : NativeMapDocument.LegacyThings))) Exact(new() { [pair.Key] = pair.Value });
            var saved = doc.SaveCopy(Path.Combine(directory, $"update-{native}-{kind}")); Expect(NativeMapDocument.Load(saved).ObjectManagerEntries()[0].Thing.TypeID == incoming.TypeID, "Paint update not reopenable.");
            var after = Snapshot(doc); doc.Undo(); Exact(before); doc.Redo(); Exact(after); doc.Undo();
        }
        var doors = New(native); var door = ObjectBatchTemplate.Default(PlacedThingKind.Door) with { Owner = 2, DataAll = 2 };
        Seed(doors, door, new(2, 2));
        foreach (var expected in new[] { 0, 1, 0, 1 })
        { var plan = doors.PlanObjectPainting(door, new(2, 2)); Expect(plan.TogglesDoor && plan.EffectiveTemplate.DataAll == expected, "Barricade/nonzero lock toggle normalized incorrectly."); doors.ApplyObjectPainting(plan); Expect(doors.ObjectManagerEntries().Single().DataAll == expected, "Door toggle not serialized."); }
    }
    private static void Opposites(bool native, string directory)
    {
        foreach (var kind in new[] { PlacedThingKind.Door, PlacedThingKind.Trap, PlacedThingKind.Object, PlacedThingKind.SpecialItem })
        {
            var opposite = kind switch { PlacedThingKind.Door => PlacedThingKind.Trap, PlacedThingKind.Trap => PlacedThingKind.Door, PlacedThingKind.Object => PlacedThingKind.SpecialItem, _ => PlacedThingKind.Object };
            var doc = New(native); Seed(doc, ObjectBatchTemplate.Default(opposite) with { Owner = 2 }, new(2, 2));
            if (opposite != PlacedThingKind.Trap)
            { var scope = opposite == PlacedThingKind.Door ? 64u : opposite == PlacedThingKind.Object ? 128u : 256u; var root = doc.EnsureScriptScope(doc.ScriptOwners().Single(owner => owner.Scope == scope)); doc.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER")); }
            doc.DuplicateRecord(doc.ObjectManagerEntries().Single().Record); Seed(doc, ObjectBatchTemplate.Default(opposite) with { Owner = 2 }, new(3, 3));
            var before = Snapshot(doc); var oldIdentities = doc.ObjectManagerEntries().Select(entry => entry.Identity).ToArray(); var events = ScriptGraph.Build(doc).Events.Count;
            var plan = doc.PlanObjectPainting(ObjectBatchTemplate.Default(kind) with { Owner = 2 }, new(2, 2));
            Expect(plan.Creates && plan.Removals.Count == 2, "Creation only removed first opposite-kind entry.");
            var history = doc.GetHistory().Undo.Count; doc.ApplyObjectPainting(plan); var rows = doc.ObjectManagerEntries();
            Expect(rows.Count == 2 && rows.Count(entry => entry.Thing.Kind == kind) == 1 && rows.Count(entry => entry.Thing.Kind == opposite) == 1
                && doc.GetHistory().Undo.Count == history + 1, "Opposite removal/creation not atomic or affected another cell.");
            if (!native) Expect(!oldIdentities.Contains(rows.Single(entry => entry.Thing.Kind == kind).Identity), "Replacement copied a removed object's identity.");
            Expect(ScriptGraph.Build(doc).Events.Count == events, "Replacement deleted scenario events.");
            var saved = doc.SaveCopy(Path.Combine(directory, $"opposite-{native}-{kind}")); Expect(NativeMapDocument.Load(saved).ObjectManagerEntries().Count == 2, "Replacement not reopenable.");
            if (!native)
            {
                var compiled = doc.CompileLegacyProject("PaintCompiled", NativeMapDocument.Create(12, 10, "Basis"));
                Expect(compiled.ObjectManagerEntries().Count == 2 && compiled.ObjectManagerEntries().Any(entry => entry.Thing.Kind == kind), "Painted project does not compile to matching objects.");
            }
            doc.Undo(); Exact(before);
            // Imported coexisting opposite entries are kept when a same-kind entry is updated.
            var added = native ? doc.AddRecord(RecordTemplate.All.Single(layout => layout.Structure == (kind == PlacedThingKind.Door ? "GAMEDOOR" : kind == PlacedThingKind.Trap ? "GAMETRAP" : "GAMEOBJECT")), new(2, 2), 2)
                : doc.AddLegacyRecord(kind == PlacedThingKind.SpecialItem ? "MagicalObject" : kind.ToString(), new(2, 2), 2);
            doc.SetField(added, ValueCatalogues.PrimaryField(added)!, ObjectBatchTemplate.Default(kind).TypeID.ToString());
            var coexist = doc.PlanObjectPainting(ObjectBatchTemplate.Default(kind) with { Owner = 2 }, new(2, 2));
            Expect(!coexist.Creates && coexist.Removals.Count == 0, "Existing same-kind update scheduled opposite removals.");
            var count = doc.ObjectManagerEntries().Count; doc.ApplyObjectPainting(coexist); Expect(doc.ObjectManagerEntries().Count == count, "Same-kind update deleted coexisting opposites.");
        }
    }
    private static void Historical(string directory)
    {
        var basis = New(false); Seed(basis, ObjectBatchTemplate.Default(PlacedThingKind.Door) with { Owner = 2 }, new(2, 2));
        var source = basis.ObjectManagerEntries().Single().Record; var container = LegacySectionFile.Read(basis.SaveCopy(Path.Combine(directory, "history-basis")));
        for (var version = 1; version <= 5; version++)
        {
            var structure = version switch { 1 => "MAPTHINGV1", 2 or 3 => "MAPTHINGV2", 4 => "MAPTHINGV4", _ => "MAPTHINGV5" };
            var payload = new byte[LegacyProjectSchemas.Size(structure)];
            foreach (var field in LegacyProjectSchemas.Fields(structure)) if (source.Fields.FirstOrDefault(item => item.Name == field.Name) is { } current) field.Encode(source.Read(current)).CopyTo(payload, field.Offset);
            var path = Path.Combine(directory, $"history-{version}.dkm"); File.WriteAllBytes(path, LegacySectionFile.Create(container.Sections.Select(section => section.Signature == NativeMapDocument.LegacyThings ? new(section.Signature, version, payload) : section)).ToArray());
            var doc = NativeMapDocument.Load(path); var before = Snapshot(doc); var template = ObjectBatchTemplate.Default(PlacedThingKind.Door) with { Owner = 2 };
            Reject(() => doc.ApplyObjectPainting(doc.PlanObjectPainting(template, new(2, 2)))); Exact(before);
            doc.ApplyObjectPainting(doc.PlanObjectPainting(template, new(2, 2), true)); Expect(doc.ObjectManagerEntries().Single().Record.File.Data.Span[4] == 6, "Explicit Things-only upgrade failed.");
            foreach (var pair in before.Where(pair => pair.Key.Signature != NativeMapDocument.LegacyThings)) Exact(new() { [pair.Key] = pair.Value }); doc.Undo(); Exact(before);
        }
    }
    private static void NoOp(bool native)
    {
        var doc = New(native); var trap = ObjectBatchTemplate.Default(PlacedThingKind.Trap) with { Owner = 2 };
        Seed(doc, trap, new(2, 2)); Seed(doc, ObjectBatchTemplate.Default(PlacedThingKind.Creature), new(3, 3)); doc.Undo();
        var before = Snapshot(doc); var history = doc.GetHistory().Undo.Count; var notifications = 0; doc.Changed += (_, _) => notifications++;
        Expect(doc.CanRedo && doc.ApplyObjectPainting(doc.PlanObjectPainting(trap, new(2, 2))) == 0
            && doc.CanRedo && doc.GetHistory().Undo.Count == history && notifications == 0, "Byte-identical single update consumed redo or history."); Exact(before);
    }

    public static void CheckLoaded(NativeMapDocument doc)
    {
        var entry = doc.ObjectManagerEntries().FirstOrDefault(entry => entry.CanRemove && entry.Thing.Kind == PlacedThingKind.Door);
        if (entry is null) return;
        var before = Snapshot(doc); var plan = doc.PlanObjectPainting(doc.ObjectTemplateForModification(entry.Record), entry.Thing.Position); Exact(before);
        if (plan.Locked) return;
        var history = doc.GetHistory().Undo.Count; var count = doc.ObjectManagerEntries().Count;
        doc.ApplyObjectPainting(plan);
        Expect(doc.ObjectManagerEntries().Count == count && doc.GetHistory().Undo.Count == history + 1, "Loaded door toggle changed object count/history.");
        foreach (var pair in before.Where(pair => pair.Key.Signature != 0xBE)) Exact(new() { [pair.Key] = pair.Value });
        doc.Undo(); Exact(before);
    }
}
