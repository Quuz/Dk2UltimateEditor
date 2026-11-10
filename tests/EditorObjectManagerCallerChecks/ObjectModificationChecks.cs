using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class ObjectModificationChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException) { return; } throw new Exception("Unsafe modification accepted."); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> before) => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Modification/undo changed unexpected bytes.");
    private static NativeMapDocument New(bool native) => NativeMapDocument.CreateNewMap(new("Modify", "Modify", 16, 14, NativeFormat: native));
    private static void Add(NativeMapDocument doc, ObjectBatchTemplate template, TilePoint position)
        => doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(template, new(Position: position, Flags: (ObjectBatchFlags)127)), [0]);
    private static ObjectBatchModificationPlan Modify(NativeMapDocument doc, IEnumerable<NativeRecord> selected, ObjectBatchTemplate template,
        ObjectBatchModificationOptions options, params ObjectModificationDecision[] decisions)
    {
        var plan = doc.PlanObjectBatchModification(selected, template, options);
        return doc.ReviewObjectBatchModification(plan, decisions.Length == 0 ? plan.Candidates.Select(_ => ObjectModificationDecision.Modify) : decisions);
    }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2MapEditor.ObjectModification", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            Rules(); foreach (var native in new[] { false, true }) { Transactions(native, directory); Collisions(native); Scopes(native, directory); Large(native); }
            Historical(directory); Reserved(directory);
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void Rules()
    {
        var old = new ObjectBatchTemplate(PlacedThingKind.Creature, 5, 1, 67, 53, 7, 0xABCDEF04, 0xFEDCBA07, 13, 21, 4);
        var target = new ObjectBatchTemplate(PlacedThingKind.Creature, 2, 7, 234, 987, 9, 0x12345678, 0x87654321, 27, 32, 6);
        var masks = new[] { ObjectModificationMask.Type, ObjectModificationMask.Owner, ObjectModificationMask.Level, ObjectModificationMask.GoldHeld,
            ObjectModificationMask.Health, ObjectModificationMask.CreatureOptions, ObjectModificationMask.HeroOptions, ObjectModificationMask.Objective };
        // Every combination is compared with an independent field-by-field model, including unknown bits.
        for (var subset = 0; subset < 256; subset++) foreach (var bitMode in Enum.GetValues<ObjectModificationBits>())
        {
            var mask = masks.Where((_, index) => (subset & (1 << index)) != 0).Aggregate(ObjectModificationMask.None, (a, b) => a | b);
            var options = new ObjectBatchModificationOptions(mask, CreatureBits: bitMode, CreatureMask: 0xA5A5A5A5, HeroBits: bitMode, HeroMask: 0x5A5A5A5A);
            var actual = ObjectModificationRules.Transform(old, target, options, 3, false);
            bool H(int bit) => (subset & (1 << bit)) != 0;
            uint B(uint a, uint b, uint selected) => bitMode == ObjectModificationBits.All ? b : (a & ~selected) | (b & selected);
            var expected = new ObjectBatchTemplate(old.Kind, H(0) ? 2u : 5u, H(1) ? (byte)7 : (byte)1, H(4) ? 234u : 67u,
                H(3) ? 987u : 53u, H(2) ? 9 : 7, H(6) ? B(old.HeroOptions, target.HeroOptions, 0x5A5A5A5A) : old.HeroOptions,
                H(5) ? B(old.CreatureOptions, target.CreatureOptions, 0xA5A5A5A5) : old.CreatureOptions,
                H(7) ? 27u : 13u, H(7) ? 32u : 21u, H(7) ? 6u : 4u);
            Expect(actual.Template == expected && actual.Warnings == 0, "Masked source transformation differs from independent model.");
        }
        var nonhero = ObjectModificationRules.Transform(old, target, new(ObjectModificationMask.Owner), 3);
        Expect(nonhero.Template is { Owner: 7, HeroOptions: 0, Objective: 0, Area: 0, Enemy: 0 } && nonhero.Warnings == ObjectBatchWarning.Incompatible, "Nonhero normalization not applied.");
        var hero = ObjectModificationRules.Transform(old with { Objective = 0 }, target, new(ObjectModificationMask.None), 3);
        Expect(hero.Template.Objective == 19 && hero.Warnings == ObjectBatchWarning.Incompatible, "Hero default objective wrong.");
        Expect(ObjectModificationRules.Transform(old, target with { Owner = 0 }, new(ObjectModificationMask.Owner), 6).Template.Owner == 6, "Automatic owner not resolved.");
        foreach (var kind in new[] { PlacedThingKind.Door, PlacedThingKind.Trap }) foreach (var id in new uint[] { 1, 6, 7 }) foreach (var data in new[] { -1, 0, 1, 2, 4, 9 })
        {
            var value = new ObjectBatchTemplate(kind, id, 3, DataAll: data);
            var actual = ObjectModificationRules.Transform(value, value, new(ObjectModificationMask.None), 3);
            var expected = data; var warning = false;
            if (kind == PlacedThingKind.Trap) { if (data == 2) expected = 0; if (data == 1) { expected = 4; warning = true; } }
            else { if (data == 4) expected = 0; if (id is 6 or 7 && expected is not (0 or 2)) { expected = 0; warning = true; } }
            Expect(actual.Template.DataAll == expected && actual.Warnings == (warning ? ObjectBatchWarning.Incompatible : 0), "Door/trap union normalization wrong.");
        }
        foreach (var kind in new[] { PlacedThingKind.Object, PlacedThingKind.SpecialItem }) foreach (var id in new uint[] { 1, 2, 3, 4, 28, 117 }) foreach (var data in new[] { -1, 0, 1, 999 })
        {
            var value = new ObjectBatchTemplate(kind, id, 2, DataAll: data);
            var actual = ObjectModificationRules.Transform(value, value, new(ObjectModificationMask.None), 3);
            var valid = kind == PlacedThingKind.Object && (id is 1 or 2 or 3 || id == 4 && LegacyCompilerRules.Elements("Spells").Any(spell => spell.ID == unchecked((uint)data)));
            Expect(actual.Template.DataAll == (valid ? data : 0), "Object/special data conversion wrong.");
        }
        Reject(() => ObjectModificationRules.Transform(old, ObjectBatchTemplate.Default(PlacedThingKind.Trap), new(ObjectModificationMask.Type), 3));
        Reject(() => ObjectModificationRules.Transform(old, target, new((ObjectModificationMask)1), 3));
    }
    private static void Transactions(bool native, string directory)
    {
        var doc = New(native); var initial = ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Owner = 3, Objective = 0, Health = 72, GoldHeld = 13, DataAll = 4 };
        Add(doc, initial, new(3, 4)); Add(doc, initial with { Health = 81, GoldHeld = 19 }, new(6, 7));
        var before = Snapshot(doc); var entries = doc.ObjectManagerEntries(); var history = doc.GetHistory().Undo.Count;
        var options = new ObjectBatchModificationOptions(ObjectModificationMask.Coordinates | ObjectModificationMask.Health, new(7, 3), Flags: (ObjectBatchFlags)127);
        var plan = Modify(doc, entries.Select(entry => entry.Record), initial with { Health = 93, GoldHeld = 999 }, options); Exact(before);
        Expect(plan.Candidates.Select(candidate => candidate.Position).SequenceEqual(new[] { new TilePoint(7, 3), new TilePoint(10, 6) }), "Translation not anchored at selected minimum.");
        var notifications = 0; doc.Changed += (_, _) => notifications++;
        Expect(doc.ApplyObjectBatchModification(plan) == 2 && notifications == 1 && doc.GetHistory().Undo.Count == history + 1, "Modification not one transaction.");
        var moved = doc.ObjectManagerEntries(); Expect(moved.Select(entry => entry.Thing.Position).SequenceEqual(plan.Candidates.Select(candidate => candidate.Position)), "Coordinates not serialized.");
        foreach (var entry in moved)
        {
            var field = entry.Record.Fields.Single(field => field.Name == (native ? "dwGlobalHealth" : "health")); Expect(entry.Record.Read(field) == "93", "Health not written.");
            var gold = entry.Record.Fields.Single(field => field.Name == (native ? "bGlobalGoldHeld" : "goldheld"));
            Expect(entry.Record.Read(gold) is "13" or "19", "Unmasked gold changed.");
        }
        if (!native) Expect(moved.Select(entry => entry.Identity).SequenceEqual(entries.Select(entry => entry.Identity)), "Stable project UID changed.");
        foreach (var pair in before.Where(pair => pair.Key.Signature != (native ? 0xBE : NativeMapDocument.LegacyThings))) Exact(new() { [pair.Key] = pair.Value });
        var after = Snapshot(doc); doc.Undo(); Exact(before); doc.Redo(); Exact(after);
        var saved = doc.SaveCopy(Path.Combine(directory, "modify-" + native)); Expect(NativeMapDocument.Load(saved).ObjectManagerEntries().Select(entry => entry.Thing.Position).SequenceEqual(moved.Select(entry => entry.Thing.Position)), "Save/reopen lost modified positions.");
        doc.Undo(); Exact(before); var redo = doc.GetHistory().Redo.Count;
        var noOp = Modify(doc, doc.ObjectManagerEntries().Select(entry => entry.Record), initial, new(ObjectModificationMask.Health, Flags: (ObjectBatchFlags)127), ObjectModificationDecision.Modify, ObjectModificationDecision.Keep);
        Expect(doc.ApplyObjectBatchModification(noOp) == 0 && doc.GetHistory().Redo.Count == redo, "No-op consumed redo or reordered rows."); Exact(before);
        Reject(() => New(native).ApplyObjectBatchModification(plan));
        doc.Paint([new(11, 11)], new(8, 3, 1)); var edited = Snapshot(doc); Reject(() => doc.ApplyObjectBatchModification(plan)); Exact(edited); doc.Undo(); Exact(before);
        IEnumerable<ObjectModificationDecision> Mutating() { doc.Paint([new(11, 11)], new(4, 2, 1)); yield return ObjectModificationDecision.Modify; yield return ObjectModificationDecision.Modify; }
        Reject(() => doc.ReviewObjectBatchModification(plan, Mutating())); doc.Undo(); Exact(before);
        var same = doc.PlanObjectBatchModification(doc.ObjectManagerEntries().Select(entry => entry.Record), initial, options with { Coordinates = ObjectModificationCoordinates.Same });
        Expect(same.Candidates.All(candidate => candidate.Position == new TilePoint(7, 3)), "Creature same-position mode unavailable.");
        var invalid = doc.PlanObjectBatchModification(doc.ObjectManagerEntries().Select(entry => entry.Record), initial, options with { Position = new(-1, 2) });
        Expect(invalid.Candidates[0].Locked, "Negative destination not locked.");
        invalid = doc.ReviewObjectBatchModification(invalid, [ObjectModificationDecision.Remove, ObjectModificationDecision.Keep]);
        Expect(doc.ApplyObjectBatchModification(invalid) == 1, "Locked destination prevented source removal."); doc.Undo(); Exact(before);
    }
    private static void Collisions(bool native)
    {
        var doc = New(native); var template = ObjectBatchTemplate.Default(PlacedThingKind.Object);
        Add(doc, template, new(2, 2)); Add(doc, template, new(3, 2));
        var before = Snapshot(doc); var entries = doc.ObjectManagerEntries();
        var plan = doc.PlanObjectBatchModification(entries.Select(entry => entry.Record), template,
            new(ObjectModificationMask.Coordinates, new(3, 2), Coordinates: ObjectModificationCoordinates.Same, Flags: (ObjectBatchFlags)127));
        Expect(plan.Candidates.Select(candidate => candidate.Position).SequenceEqual(new[] { new TilePoint(3, 2), new TilePoint(4, 2) }), "Noncreature same mode did not force group translation.");
        Expect(plan.Candidates.All(candidate => candidate.Replacement is null), "Vacating selected source incorrectly replaced.");
        var review = doc.ReviewObjectBatchModification(plan, [ObjectModificationDecision.Modify, ObjectModificationDecision.Keep]);
        Expect(review.Candidates[0].Replacement?.Record.Offset == entries[1].Record.Offset && review.Candidates[0].Decision == ObjectModificationDecision.Keep, "Kept object did not trigger a new replacement decision."); Exact(before);
        var approved = doc.ReviewObjectBatchModification(review, [ObjectModificationDecision.Modify, ObjectModificationDecision.Keep]);
        Expect(doc.ApplyObjectBatchModification(approved) == 1 && doc.ObjectManagerEntries().Single().Thing.Position == new TilePoint(3, 2), "Explicit second replacement choice not applied."); doc.Undo(); Exact(before);
        // Initially default Keep can also expose a collision at application; no silent deletion.
        var outside = doc.PlanObjectBatchModification(entries.Select(entry => entry.Record), template, new(ObjectModificationMask.Coordinates, new(14, 2), Flags: (ObjectBatchFlags)127));
        Expect(outside.Candidates[1].Locked, "Right boundary guard wrong.");
        Reject(() => doc.ReviewObjectBatchModification(plan, [ObjectModificationDecision.Modify])); Exact(before);
        doc.ApplyObjectBatchModification(doc.ReviewObjectBatchModification(plan, [ObjectModificationDecision.Remove, ObjectModificationDecision.Keep]));
        Expect(doc.ObjectManagerEntries().Single().Thing.Position == new TilePoint(3, 2), "Remove deleted replacement instead of source."); doc.Undo(); Exact(before);
    }
    private static void Scopes(bool native, string directory)
    {
        var doc = New(native); var creature = ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Owner = 3, Objective = 0 };
        Add(doc, creature, new(2, 2)); Add(doc, creature, new(3, 3));
        var owner = doc.ScriptOwners().First(owner => owner.Scope == 2); var root = doc.EnsureScriptScope(owner);
        doc.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER"));
        var before = Snapshot(doc); var graph = ScriptGraph.Build(doc); var rootID = graph.Roots.Single(edge => edge.Target != 0 && edge.Scope == 2).Target;
        var options = new ObjectBatchModificationOptions(ObjectModificationMask.Owner, Flags: (ObjectBatchFlags)127);
        var plan = doc.PlanObjectBatchModification(doc.ObjectManagerEntries().Select(entry => entry.Record), creature with { Owner = 1 }, options);
        Expect(plan.Candidates.Count == 1 && !plan.Candidates[0].Source.Scripted, "Allocated UID confused with actual scripted state.");
        foreach (byte newOwner in new byte[] { 1, 2, 7 })
        {
            plan = Modify(doc, doc.ObjectManagerEntries().Select(entry => entry.Record), creature with { Owner = newOwner }, options with { IncludeScripted = true });
            Expect(doc.ApplyObjectBatchModification(plan) == 2 && doc.ObjectManagerEntries().All(entry => entry.Thing.Owner == newOwner), "Owner migration failed.");
            Expect(ScriptGraph.Build(doc).Roots.Any(edge => edge.Scope == 2 && edge.Target == rootID) && ScriptGraph.Build(doc).Events.Count == graph.Events.Count, "Owner-dependent native signature lost scenario root.");
            if (native) Expect(doc.ObjectManagerEntries().All(entry => entry.Record.Category == (newOwner == 1 ? "GAMEHERO" : newOwner == 2 ? "GAMENEUTRAL" : "GAMEKEEPER")), "Native creature signature not migrated.");
            else
            {
                var compiled = doc.CompileLegacyProject("ModifiedCompiled", NativeMapDocument.Create(16, 14, "Basis"));
                Expect(compiled.ObjectManagerEntries().Count == 2 && compiled.ObjectManagerEntries().All(entry => entry.Thing.Owner == newOwner)
                    && ScriptGraph.Build(compiled).Roots.Any(edge => edge.Scope == 2 && edge.Target == rootID), "Modified project compilation lost ownership/scenario root.");
            }
            var saved = doc.SaveCopy(Path.Combine(directory, $"owner-{native}-{newOwner}")); Expect(NativeMapDocument.Load(saved).ObjectManagerEntries().All(entry => entry.Thing.Owner == newOwner), "Owner migration not reopenable.");
            doc.Undo(); Exact(before);
        }
        var things = New(native); Add(things, ObjectBatchTemplate.Default(PlacedThingKind.Object), new(2, 2));
        var objectOwner = things.ScriptOwners().Single(item => item.Scope == 128); var binding = things.EnsureScriptScope(objectOwner);
        things.InsertScriptEvent(binding.Owner, binding.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER"));
        var objectBefore = Snapshot(things); var events = ScriptGraph.Build(things).Events.Count;
        var conversion = Modify(things, things.ObjectManagerEntries().Select(entry => entry.Record), ObjectBatchTemplate.Default(PlacedThingKind.SpecialItem), new(ObjectModificationMask.Type, IncludeScripted: true, Flags: (ObjectBatchFlags)127));
        things.ApplyObjectBatchModification(conversion); Expect(things.ObjectManagerEntries().Single().Thing.Kind == PlacedThingKind.SpecialItem
            && ScriptGraph.Build(things).Roots.Any(edge => edge.Scope == 256 && edge.Target != 0) && ScriptGraph.Build(things).Events.Count == events, "Object/special scope not migrated."); things.Undo(); Exact(objectBefore);
        var doors = New(native); Add(doors, ObjectBatchTemplate.Default(PlacedThingKind.Door), new(2, 2));
        var doorOwner = doors.ScriptOwners().Single(item => item.Scope == 64); var doorRoot = doors.EnsureScriptScope(doorOwner);
        doors.InsertScriptEvent(doorRoot.Owner, doorRoot.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER"));
        var doorBefore = Snapshot(doors);
        var trap = Modify(doors, doors.ObjectManagerEntries().Select(entry => entry.Record), ObjectBatchTemplate.Default(PlacedThingKind.Trap), new(ObjectModificationMask.Type, IncludeScripted: true, Flags: (ObjectBatchFlags)127));
        Expect(trap.Candidates.Single().DetachesScript, "Door-to-trap binding loss not shown."); Reject(() => doors.ApplyObjectBatchModification(trap)); Exact(doorBefore);
        doors.ApplyObjectBatchModification(trap, true); Expect(doors.ObjectManagerEntries().Single().Thing.Kind == PlacedThingKind.Trap && ScriptGraph.Build(doors).Events.Count == 1
            && !ScriptGraph.Build(doors).Roots.Any(edge => edge.Scope == 64 && edge.Target != 0), "Approved detachment deleted events or left obsolete binding."); doors.Undo(); Exact(doorBefore);
    }
    private static void Historical(string directory)
    {
        var basis = New(false); Add(basis, ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Owner = 3, Objective = 0 }, new(2, 2));
        var source = basis.ObjectManagerEntries().Single().Record; var container = LegacySectionFile.Read(basis.SaveCopy(Path.Combine(directory, "history-basis")));
        for (var version = 1; version <= 5; version++)
        {
            var structure = version switch { 1 => "MAPTHINGV1", 2 or 3 => "MAPTHINGV2", 4 => "MAPTHINGV4", _ => "MAPTHINGV5" };
            var payload = new byte[LegacyProjectSchemas.Size(structure)];
            foreach (var field in LegacyProjectSchemas.Fields(structure)) if (source.Fields.FirstOrDefault(item => item.Name == field.Name) is { } current) field.Encode(source.Read(current)).CopyTo(payload, field.Offset);
            var path = Path.Combine(directory, $"history-{version}.dkm"); File.WriteAllBytes(path, LegacySectionFile.Create(container.Sections.Select(section => section.Signature == NativeMapDocument.LegacyThings ? new(section.Signature, version, payload) : section)).ToArray());
            var doc = NativeMapDocument.Load(path); var before = Snapshot(doc); var template = ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Health = 91 };
            var plan = Modify(doc, doc.ObjectManagerEntries().Select(entry => entry.Record), template, new(ObjectModificationMask.Health, Flags: (ObjectBatchFlags)127));
            Reject(() => doc.ApplyObjectBatchModification(plan)); Exact(before);
            plan = Modify(doc, doc.ObjectManagerEntries().Select(entry => entry.Record), template, new(ObjectModificationMask.Health, Flags: (ObjectBatchFlags)127, UpgradeHistoricalThings: true));
            Expect(doc.ApplyObjectBatchModification(plan) == 1 && doc.ObjectManagerEntries().Single().Record.File.Data.Span[4] == 6, "Explicit Things-only upgrade failed.");
            foreach (var pair in before.Where(pair => pair.Key.Signature != NativeMapDocument.LegacyThings)) Exact(new() { [pair.Key] = pair.Value });
            doc.Undo(); Exact(before);
        }
    }
    private static void Reserved(string directory)
    {
        var basis = New(true); Add(basis, ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Owner = 3, Objective = 0 }, new(2, 2));
        var descriptor = basis.SaveCopy(Path.Combine(directory, "reserved")); var original = basis.ObjectManagerEntries().Single().Record;
        var path = Path.Combine(directory, "reserved", original.File.ExportPath.Replace('/', Path.DirectorySeparatorChar)); var bytes = File.ReadAllBytes(path);
        bytes.AsSpan(original.Offset + 16, 4).Fill(0x97); bytes[original.Offset + 21] = 0x86; bytes[original.Offset + 28] = 0x75;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 0); File.WriteAllBytes(path, [.. bytes, 0xFE, 0xDC, 0xBA]);
        var doc = NativeMapDocument.Load(descriptor); var before = Snapshot(doc);
        var template = ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Health = 81, Owner = 3 };
        var plan = Modify(doc, doc.ObjectManagerEntries().Select(entry => entry.Record), template, new(ObjectModificationMask.Health, Flags: (ObjectBatchFlags)127));
        doc.ApplyObjectBatchModification(plan); var row = doc.ObjectManagerEntries().Single().Record;
        var actual = row.File.Data.Span.Slice(row.Offset, row.Length); var expected = before[row.File].AsSpan(original.Offset, original.Length).ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(24), 81); Expect(actual.SequenceEqual(expected) && row.File.Data.Span[^3..].SequenceEqual(new byte[] { 0xFE, 0xDC, 0xBA }), "Same-layout edit lost reserved bytes/tail.");
        doc.Undo(); Exact(before);
        plan = Modify(doc, doc.ObjectManagerEntries().Select(entry => entry.Record), template with { Owner = 2 }, new(ObjectModificationMask.Owner, Flags: (ObjectBatchFlags)127));
        doc.ApplyObjectBatchModification(plan); row = doc.ObjectManagerEntries().Single().Record;
        Expect(row.Length == 32 && row.File.Data.Span.Slice(row.Offset + 16, 4).ToArray().All(value => value == 0x97) && row.File.Data.Span[row.Offset + 21] == 0x86
            && BinaryPrimitives.ReadUInt32LittleEndian(row.File.Data.Span[8..]) == row.File.Length, "Shrinking owner migration lost common reserved bytes or underflowed size."); doc.Undo(); Exact(before);
    }
    private static void Large(bool native)
    {
        var doc = NativeMapDocument.CreateNewMap(new("LargeModify", "LargeModify", 40, 40, NativeFormat: native)); var template = ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Owner = 3, Objective = 0 };
        doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(template, new(ObjectBatchMode.Rectangle, WholeMap: false, Selection: new(1, 1, 32, 32), Flags: (ObjectBatchFlags)127)));
        var before = Snapshot(doc); var plan = Modify(doc, doc.ObjectManagerEntries().Select(entry => entry.Record), template with { Health = 69 }, new(ObjectModificationMask.Health, Flags: (ObjectBatchFlags)127));
        Expect(doc.ApplyObjectBatchModification(plan) == 1024, "Large bulk modification truncated."); doc.Undo(); Exact(before);
    }
    public static void CheckLoaded(NativeMapDocument doc)
    {
        var before = Snapshot(doc); var originalHistory = doc.GetHistory().Undo.Count;
        var chosen = doc.ObjectManagerEntries().Where(entry => entry.CanRemove && entry.Thing.Kind == PlacedThingKind.Creature).Take(7).ToArray();
        if (chosen.Length == 0) return;
        var plan = doc.PlanObjectBatchModification(chosen.Select(entry => entry.Record), ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Health = 73 },
            new(ObjectModificationMask.Health, IncludeScripted: true, Flags: (ObjectBatchFlags)127));
        plan = doc.ReviewObjectBatchModification(plan, plan.Candidates.Select(candidate => candidate.Locked ? ObjectModificationDecision.Keep : ObjectModificationDecision.Modify)); Exact(before);
        var events = before.Where(pair => pair.Key.Signature == 0xD2).ToDictionary(pair => pair.Key, pair => pair.Value);
        doc.ApplyObjectBatchModification(plan); Exact(events);
        var approved = plan.Candidates.Count(candidate => candidate.Decision == ObjectModificationDecision.Modify);
        if (doc.GetHistory().Undo.Count != originalHistory)
        {
            Expect(doc.ObjectManagerEntries().Count(entry => entry.Thing.Kind == PlacedThingKind.Creature
                && entry.Record.Read(entry.Record.Fields.Single(field => field.Name == "dwGlobalHealth")) == "73") >= approved, "Loaded bulk health edit lost approved entries.");
            doc.Undo(); Exact(before);
        }
        else Exact(before);
    }
}
