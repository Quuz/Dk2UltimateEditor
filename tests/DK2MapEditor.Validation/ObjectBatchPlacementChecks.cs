using System.Buffers.Binary;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class ObjectBatchPlacementChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { return; }
        throw new Exception("Invalid placement preview/selection accepted.");
    }
    private static string Repo()
    {
        var directory = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(directory, "Source"))) directory = Path.GetDirectoryName(directory)!;
        return directory;
    }
    public static void Run()
    {
        SourceEvidence(); CheckRules(); CheckProjection(); CheckDecisions();
        var directory = Path.Combine(Path.GetTempPath(), "DK2MapEditor.ObjectBatchPlacement", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false, true }) CheckPlans(native, directory);
            CheckNativeDefinitions(directory); CheckUnknownLayouts(directory); CheckHistoricalProjects(directory); CheckLargeMap();
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void SourceEvidence()
    {
        var source = File.ReadAllText(Path.Combine(Repo(), "Source/Sources/Things/Things_Functions.c"));
        source = source[source.IndexOf("int Things_CanPaintEx(", StringComparison.Ordinal)..];
        Expect(source.Contains("MapID == 0x05") && source.Contains("MapID == 0x04") && source.Contains("CanWalkLava") && source.Contains("CanWalkWater")
            && source.Contains("(MapID == 0x14)") && source.Contains("(Id == 0x1C)") && source.Contains("(Id != 0x82)"), "Source placement exception evidence changed.");
        Expect(Regex.Matches(source, @"return\(3\)").Count == 2 && Regex.Matches(source, @"return\(2\)").Count == 2,
            "Source terrain verdict branches changed.");
        var manager = File.ReadAllText(Path.Combine(Repo(), "Source/Sources/Things/Things_Manager.c"));
        var add = manager[manager.IndexOf("void Things_ManagerAdd(", StringComparison.Ordinal)..];
        add = add[..add.IndexOf("int Things_ManagerAddOptions(", StringComparison.Ordinal)];
        Expect(add.Contains("MapClient.Width = Map.Properties.Width-1;") && add.Contains("MapClient.X = 1;")
            && add.Contains("Count = random(Config.ManagerAddParams.AddRandomFreq);") && add.Contains("Template->newthing.owner = MapID;"), "Source candidate/auto-owner evidence changed.");
        Expect(add.IndexOf("Things_GetAtMapPos(&MapDoors", StringComparison.Ordinal) < add.IndexOf("Things_GetAtMapPos(&MapTraps", StringComparison.Ordinal)
            && add.IndexOf("Things_GetAtMapPos(&MapObjects", StringComparison.Ordinal) < add.IndexOf("Things_GetAtMapPos(&MapMagicalObjects", StringComparison.Ordinal), "Source overlap priority changed.");
        Expect(add.IndexOf("case MNGR_REMPLACEALL:", StringComparison.Ordinal) > add.IndexOf("MNGR_FLAGIGNORETERRAIN", StringComparison.Ordinal)
            && add.Contains("Template->locked = TRUE;"), "Source preview order/lock evidence changed.");
        var header = File.ReadAllText(Path.Combine(Repo(), "Source/Includes/Manager.h"));
        var names = new[] { "OUTOFMAP", "BADTERRAIN", "LIMIT", "ENEMY", "FATAL", "REMPLACE", "REMSAME", "REMSAMEFAMILY", "INCOMPATIBLE" };
        for (var index = 0; index < names.Length; index++)
        {
            var match = Regex.Match(header, @"#define\s+MNGR_WARN" + names[index] + @"\s+0x([0-9A-Fa-f]+)");
            Expect(match.Success && Convert.ToInt32(match.Groups[1].Value, 16) == 1 << index, "Source warning flags changed.");
        }
    }
    // Independent literal-switch transcription of the C return paths. The
    // production implementation uses grouped tables and boolean expressions.
    private static int ReferencePermission(byte terrain, byte tileOwner, PlacedThingKind kind, uint id, byte owner, bool water, bool lava)
    {
        switch (terrain) { case 254: case 6: case 7: case 1: case 2: case 30: case 9: return 0; }
        switch (kind)
        {
            case PlacedThingKind.Creature:
                if (terrain == 5) return lava ? 1 : 3;
                if (terrain == 4) return water ? 1 : 3;
                if (owner == 0) break;
                switch (terrain) { case 4: case 5: case 3: case 31: return 1; }
                if (terrain == 20 && tileOwner != owner) return 1;
                if (tileOwner != owner) return 2;
                break;
            case PlacedThingKind.Door:
            case PlacedThingKind.Trap:
                switch (terrain)
                {
                    case 11: case 15: case 10: case 24: case 21: case 23: case 22: case 13:
                    case 16: case 19: case 18: case 20: case 25: case 17: case 26: case 36:
                    case 31: case 32: case 4: case 5: return 0;
                }
                if (owner != 0 && tileOwner != owner) return 0;
                goto case PlacedThingKind.Object;
            case PlacedThingKind.Object:
            case PlacedThingKind.SpecialItem:
                switch (terrain)
                {
                    case 14: case 28: case 34:
                        if (kind == PlacedThingKind.Object && id == 28) break;
                        return 0;
                    case 40: case 12: case 29: case 33: case 37: return 0;
                }
                if (owner == 0) break;
                if (kind == PlacedThingKind.Object && owner == 2) break;
                if (kind == PlacedThingKind.SpecialItem && owner == 2) break;
                if (tileOwner != owner && (kind != PlacedThingKind.Object || id != 130)) return 2;
                break;
        }
        return 1;
    }
    private static void CheckRules()
    {
        var comparisons = 0;
        foreach (var kind in Enum.GetValues<PlacedThingKind>()) for (var terrain = 0; terrain < 256; terrain++)
            for (byte tileOwner = 1; tileOwner <= 7; tileOwner++) for (byte owner = 0; owner <= 7; owner++)
                foreach (uint id in new uint[] { 1, 28, 130, uint.MaxValue }) foreach (var flags in Enumerable.Range(0, 4))
                {
                    var water = (flags & 1) != 0; var lava = (flags & 2) != 0;
                    var expected = ReferencePermission((byte)terrain, tileOwner, kind, id, owner, water, lava);
                    var actual = ThingPlacementRules.Evaluate(new((byte)terrain, tileOwner, 1), kind, id, owner, water, lava);
                    Expect((int)actual == expected, $"Source placement mismatch: {kind} terrain={terrain} tile={tileOwner} owner={owner} ID={id} flags={flags}.");
                    comparisons++;
                }
        Expect(ThingPlacementRules.Evaluate(new(4, 2, 1), PlacedThingKind.Creature, 1, 3) == ThingTerrainPermission.Unverified
            && ThingPlacementRules.Evaluate(new(8, 3, 1), PlacedThingKind.Creature, 1, 3) == ThingTerrainPermission.Allowed,
            "Unknown definitions confused with false flags or required on ordinary terrain.");
        Reject(() => ThingPlacementRules.Evaluate(new(8, 3, 1), (PlacedThingKind)10, 1, 3));
        Reject(() => ThingPlacementRules.Evaluate(new(8, 3, 1), PlacedThingKind.Object, 1, 8));
        Console.WriteLine($"      source placement return-path comparisons={comparisons}");
    }
    private static MapTile ReferenceTile(MapTile raw, bool boundary = false)
    {
        if (boundary) return new(254, 2, 1);
        var player = raw.Owner == 0 || raw.Owner > 7 ? (byte)2 : raw.Owner;
        var option = raw.Variant != 1 && raw.Variant != 2 ? (byte)1 : raw.Variant;
        foreach (var group in new[] { "World", "Rooms", "Gates", "Walls" }) foreach (var entry in LegacyCompilerRules.Elements(group))
        {
            var packed = (uint)(raw.Terrain << 24 | player << 16 | option << 8);
            if ((entry.ID & (group == "Walls" ? 0xFFFFFF00u : 0xFF00FF00u)) != (packed & (group == "Walls" ? 0xFFFFFF00u : 0xFF00FF00u))) continue;
            return new(raw.Terrain, group == "Rooms" ? player : (byte)(entry.ID >> 16), option);
        }
        return new(2, 2, 1);
    }
    private static void CheckProjection()
    {
        var comparisons = 0;
        for (var terrain = 0; terrain < 256; terrain++) foreach (byte owner in new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 255 })
            foreach (byte variant in new byte[] { 0, 1, 2, 3, 255 }) foreach (byte padding in new byte[] { 0, 1, 2, 3, 254, 255 })
                foreach (var boundary in new[] { false, true })
                {
                    var raw = new MapTile((byte)terrain, owner, variant, padding);
                    Expect(ThingPlacementRules.EditorTile(raw, boundary) == ReferenceTile(raw, boundary), "Source conversion/fallback/reserved-byte projection mismatch.");
                    comparisons++;
                }
        Expect(ThingPlacementRules.EditorTile(new(254, 7, 1, 0)).Terrain == 254, "Interior explicit FE was not protected.");
        Console.WriteLine($"      source game-to-editor projection comparisons={comparisons}");
    }
    private static (bool Add, bool Locked, bool Preview) ReferenceDecision(int warnings, int policy, int flags)
    {
        var add = false; var preview = false; var locked = false;
        if (warnings == 0) return (true, false, false);
        var temporary = warnings & (2 | 16 | 8 | 256);
        if ((flags & 4) != 0) temporary &= ~2; if ((flags & 8) != 0) temporary &= ~16;
        if ((flags & 16) != 0) temporary &= ~8; if ((flags & 32) != 0) temporary &= ~256;
        if (temporary == 0) add = true; else preview = true;
        if ((warnings & 32) != 0)
        {
            switch (policy)
            {
                case 0: add = true; break;
                case 1: add = (warnings & 64) != 0; break;
                case 2: add = (warnings & 128) != 0; break;
                case 3: add = false; break;
                case 4: add = false; preview = true; break;
            }
        }
        if ((warnings & (1 | 4)) != 0) { add = false; locked = true; if ((flags & 64) != 0) preview = true; }
        return (add, locked, preview);
    }
    private static void CheckDecisions()
    {
        var comparisons = 0;
        for (var warnings = 0; warnings < 512; warnings++) for (var policy = 0; policy < 5; policy++) for (var flags = 0; flags < 128; flags++)
        {
            Expect(ThingPlacementRules.DefaultDecision((ObjectBatchWarning)warnings, (ObjectReplacementPolicy)policy, (ObjectBatchFlags)flags)
                == ReferenceDecision(warnings, policy, flags), "Original warning override/replacement/locking order changed."); comparisons++;
        }
        Reject(() => ThingPlacementRules.DefaultDecision((ObjectBatchWarning)512, ObjectReplacementPolicy.All, 0));
        Reject(() => ThingPlacementRules.DefaultDecision(0, (ObjectReplacementPolicy)5, 0));
        Console.WriteLine($"      source preview decision comparisons={comparisons}");
    }
    private static NativeRecord Add(NativeMapDocument document, PlacedThingKind kind, TilePoint point, uint id, byte owner = 3)
    {
        var category = kind switch { PlacedThingKind.Creature => "GAMEKEEPER", PlacedThingKind.Door => "GAMEDOOR", PlacedThingKind.Trap => "GAMETRAP", _ => "GAMEOBJECT" };
        var record = document.IsLegacyProject ? document.AddLegacyRecord(kind == PlacedThingKind.SpecialItem ? "MagicalObject" : kind.ToString(), point, owner)
            : document.AddRecord(RecordTemplate.All.Single(item => item.Structure == category), point, owner);
        document.SetField(record, ValueCatalogues.PrimaryField(record)!, id.ToString()); return record;
    }
    private static void CheckPlans(bool native, string directory)
    {
        var document = NativeMapDocument.CreateNewMap(new("Placement", "Placement", 12, 10, NativeFormat: native));
        document.Paint(MapDrawing.Rectangle(new(0, 0, 12, 10), true), new(8, 3, 1, 253));
        Add(document, PlacedThingKind.Trap, new(3, 3), 2); var firstDoor = Add(document, PlacedThingKind.Door, new(3, 3), 1);
        Add(document, PlacedThingKind.Door, new(3, 3), 2);
        Add(document, PlacedThingKind.SpecialItem, new(4, 3), 0x75); var firstObject = Add(document, PlacedThingKind.Object, new(4, 3), 1);
        Add(document, PlacedThingKind.Object, new(4, 3), 2); Add(document, PlacedThingKind.Creature, new(5, 3), 1);
        Add(document, PlacedThingKind.Object, new(6, 3), 1);
        document.Paint([new(6, 3)], new(14, 3, 1)); document.Paint([new(7, 3)], new(254, 2, 1));
        document.Paint([new(2, 2)], new(4, 7, 1, 253));
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = document.GetHistory(); var events = 0;
        document.Changed += (_, _) => events++; document.HistoryChanged += (_, _) => events++;
        void Unchanged()
        {
            Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && events == 0
                && history.Undo.SequenceEqual(document.GetHistory().Undo) && history.Redo.SequenceEqual(document.GetHistory().Redo), "Read-only placement changed bytes/history or emitted events.");
        }
        var defaults = new ObjectBatchPlacementOptions(ObjectBatchMode.Rectangle);
        Expect(defaults.RandomFrequency == 2 && defaults.Replacement == ObjectReplacementPolicy.Ask && (int)defaults.Flags == 67, "Source batch defaults wrong.");
        var whole = document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1, 0), defaults);
        Expect(whole.SampledCells == 99 && whole.Candidates.Count == 99 && whole.Candidates[0].Position == new TilePoint(1, 1)
            && whole.Candidates[^1].Position == new TilePoint(11, 9), "Whole-map range silently narrowed.");
        foreach (var candidate in whole.Candidates)
        {
            var edge = candidate.Position.X == 11 || candidate.Position.Y == 9;
            if (edge) Expect(candidate.Locked && !candidate.DefaultAdd && candidate.Owner == 2 && candidate.Warnings.HasFlag(ObjectBatchWarning.Boundary), "Original whole-map last edge was not locked/projected.");
        }
        Expect(whole.Candidates.Single(candidate => candidate.Position == new TilePoint(2, 2)).Owner == 2,
            "Auto-owner read raw water owner instead of original editor projection.");
        var sourceOverride = document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1, 3), new(Position: new(6, 3), Replacement: ObjectReplacementPolicy.All, Flags: 0));
        Expect(sourceOverride.Candidates[0].Warnings.HasFlag(ObjectBatchWarning.BadTerrain) && sourceOverride.Candidates[0].DefaultAdd
            && !sourceOverride.Candidates[0].Locked && sourceOverride.RequiresPreview, "Replacement did not preserve the original warning override order.");
        foreach (var kind in Enum.GetValues<PlacedThingKind>()) foreach (var policy in Enum.GetValues<ObjectReplacementPolicy>())
        {
            var point = kind is PlacedThingKind.Door or PlacedThingKind.Trap ? new TilePoint(3, 3) : kind == PlacedThingKind.Creature ? new(5, 3) : new(4, 3);
            var plan = document.PlanObjectBatchPlacement(new(kind, 1, 3), new(Position: point, Replacement: policy, Flags: 0));
            var candidate = plan.Candidates.Single();
            if (kind == PlacedThingKind.Creature) Expect(candidate.Replacement is null && candidate.DefaultAdd && !plan.RequiresPreview, "Creatures used object overlap replacement.");
            else
            {
                var expected = kind is PlacedThingKind.Door or PlacedThingKind.Trap ? firstDoor : firstObject;
                Expect(candidate.Replacement?.Record.Offset == expected.Offset && candidate.Replacement.Record.File == expected.File, "Door/object-first source replacement ordering changed.");
                var sameFamily = kind is PlacedThingKind.Door or PlacedThingKind.Object;
                Expect(candidate.Warnings.HasFlag(ObjectBatchWarning.SameFamily) == sameFamily && candidate.Warnings.HasFlag(ObjectBatchWarning.SameType) == sameFamily,
                    "Replacement family confused with pair grouping.");
                Expect(candidate.DefaultAdd == (policy == ObjectReplacementPolicy.All || sameFamily && policy is ObjectReplacementPolicy.SameType or ObjectReplacementPolicy.SameFamily), "Replacement default wrong.");
                if (policy == ObjectReplacementPolicy.Ask) Expect(plan.RequiresPreview && document.ResolveObjectBatchPlacement(plan, [0]).Count == 1, "Ask preview cannot explicitly enable replacement.");
            }
        }
        var samples = 0;
        var random = document.PlanObjectBatchPlacement(new(PlacedThingKind.Door, 1, 3), new(ObjectBatchMode.RandomRectangle, WholeMap: false,
            Selection: new(0, 0, 8, 5), RandomFrequency: 3), maximum => { Expect(maximum == 3, "Sampler frequency wrong."); return samples++ % maximum; });
        Expect(samples == 40 && random.SampledCells == 40 && random.Candidates.Count == 14, "Random sampled only validated/interior candidates.");
        Expect(random.Candidates.Select(candidate => candidate.Position).SequenceEqual(Enumerable.Range(0, 40).Where(index => index % 3 == 0).Select(index => new TilePoint(index % 8, index / 8))), "Random order changed.");
        var none = document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), new(ObjectBatchMode.RandomRectangle), _ => 1);
        Expect(none.Candidates.Count == 0 && !none.RequiresPreview && document.ResolveObjectBatchPlacement(none).Count == 0, "Empty random plan fabricated a candidate/preview.");
        var explicitBorder = document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1, 3), new(Position: new(7, 3), Replacement: ObjectReplacementPolicy.All, Flags: (ObjectBatchFlags)127));
        Expect(explicitBorder.Candidates[0].Locked, "Interior FE lock confused with native padding.");
        Reject(() => document.ResolveObjectBatchPlacement(explicitBorder, [0]));
        foreach (var point in new[] { new TilePoint(-1, 3), new TilePoint(12, 3), new TilePoint(3, -1), new TilePoint(3, 10), new TilePoint(0, 3), new TilePoint(11, 3) })
        {
            var plan = document.PlanObjectBatchPlacement(new(PlacedThingKind.Creature, 1, 0), new(Position: point, Flags: (ObjectBatchFlags)127));
            Expect(plan.Candidates.Single().Locked && !plan.Candidates.Single().DefaultAdd, "Out-of-map/boundary override accepted.");
            Reject(() => document.ResolveObjectBatchPlacement(plan, [0]));
        }
        var ordinary = document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1, 3), new(Position: new(1, 1)));
        Reject(() => document.ResolveObjectBatchPlacement(ordinary, [0, 0])); Reject(() => document.ResolveObjectBatchPlacement(ordinary, [-1]));
        Reject(() => document.ResolveObjectBatchPlacement(ordinary, [1]));
        Reject(() => NativeMapDocument.Create(12, 10, "Foreign").ResolveObjectBatchPlacement(ordinary));
        Reject(() => document.PlanObjectBatchPlacement(new((PlacedThingKind)5, 1), defaults));
        Reject(() => document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1, 8), defaults));
        Reject(() => document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), defaults with { Mode = (ObjectBatchMode)9 }));
        Reject(() => document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), defaults with { WholeMap = false }));
        foreach (var rectangle in new[] { new TileRect(0, 0, -1, 1), new TileRect(0, 0, 0, 1), new TileRect(0, 0, 1025, 1), new TileRect(int.MaxValue, 1, 2, 2) })
            Reject(() => document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), defaults with { WholeMap = false, Selection = rectangle }));
        foreach (var frequency in new[] { 0, 1, 33, int.MaxValue })
            Reject(() => document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), defaults with { Mode = ObjectBatchMode.RandomRectangle, RandomFrequency = frequency }));
        foreach (var frequency in new[] { 2, 32 })
            Expect(document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), defaults with { Mode = ObjectBatchMode.RandomRectangle, RandomFrequency = frequency }, _ => 0).Candidates.Count == 99, "Valid source frequency endpoint rejected.");
        foreach (var sample in new[] { -1, 2 }) Reject(() => document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), defaults with { Mode = ObjectBatchMode.RandomRectangle }, _ => sample));
        Reject(() => document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), defaults with { Mode = ObjectBatchMode.RandomRectangle }, _ => throw new InvalidOperationException("Fixture sample failure")));
        Expect(document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), new(Position: new(1, 1), RandomFrequency: 0), _ => throw new Exception("One mode sampled")) is not null, "Inactive random option was validated.");
        Unchanged();
        var saved = document.SaveCopy(Path.Combine(directory, "plans-" + native)); var reopened = NativeMapDocument.Load(saved);
        Expect(Projection(whole) == Projection(reopened.PlanObjectBatchPlacement(whole.Template, whole.Options)), "Placement plan changed after save/reopen."); Unchanged();
        if (!native) CheckProjectFlags(document);
        else CheckMissingDefinitionDefaults(document);
        var currentPreview = document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1, 3), new(Position: new(1, 1)));
        // An explicit edit, even unrelated to objects, invalidates a plan.
        document.Paint([new(1, 1)], new(3, 2, 1)); Reject(() => document.ResolveObjectBatchPlacement(currentPreview));
        document.Undo(); Expect(document.ResolveObjectBatchPlacement(currentPreview).Count == 1, "Byte-exact restoration did not revive an equivalent preview.");
        var priorRedo = document.GetHistory().Redo.ToArray(); document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), defaults);
        Expect(priorRedo.SequenceEqual(document.GetHistory().Redo), "Read-only preview consumed redo.");
    }
    private static string Projection(ObjectBatchPlacementPlan plan) => string.Join(";", plan.Candidates.Select(candidate =>
        $"{candidate.Index},{candidate.Position.X},{candidate.Position.Y},{candidate.Owner},{(int)candidate.Warnings},{candidate.Locked},{candidate.DefaultAdd},{candidate.Replacement?.Kind},{candidate.Replacement?.TypeID}"));
    private static void CheckMissingDefinitionDefaults(NativeMapDocument document)
    {
        Expect(!document.Files.Any(file => file.Signature == 0xAA), "Missing-default fixture unexpectedly contains native creatures.");
        var data = File.ReadAllBytes(Path.Combine(Repo(), "Binaries/Globals/DefaultCreatures.dkg"));
        var order = LegacyCompilerRules.DefinitionOrder("Creatures"); var catalogue = GlobalDefinitionDefaults.Catalogue("Creatures");
        for (var i = 0; i < order.Count; i++)
        {
            if (order[i] < 0) continue; var id = catalogue[order[i]].ID;
            foreach (var terrain in new byte[] { 4, 5 })
            {
                document.Paint([new(1, 2)], new(terrain, 2, 1));
                var candidate = document.PlanObjectBatchPlacement(new(PlacedThingKind.Creature, id, 3), new(Position: new(1, 2))).Candidates.Single();
                var fatal = (data[i * 5449 + 0xEFC] & (terrain == 4 ? 0x10 : 0x20)) == 0;
                Expect(candidate.Warnings.HasFlag(ObjectBatchWarning.Fatal) == fatal && !candidate.Locked, "Source-preserved default creature flags were not used.");
                document.Undo();
            }
        }
        foreach (var terrain in new byte[] { 4, 5 })
        {
            document.Paint([new(1, 2)], new(terrain, 2, 1));
            var candidate = document.PlanObjectBatchPlacement(new(PlacedThingKind.Creature, uint.MaxValue, 3), new(Position: new(1, 2))).Candidates.Single();
            Expect(candidate.Warnings.HasFlag(ObjectBatchWarning.Fatal) == (terrain == 5), "Source unknown creature ID water/lava fallback changed."); document.Undo();
        }
    }
    private static void CheckProjectFlags(NativeMapDocument document)
    {
        var row = document.GlobalDefinitionRows().First(row => row.Family == "Creatures" && row.ID == 1);
        foreach (var safe in new[] { false, true }) foreach (var terrain in new byte[] { 4, 5 })
        {
            document.SetGlobalDefinitionValues([new("Creatures", row.Index, "CanWalkWater", safe ? "1" : "0"), new("Creatures", row.Index, "CanWalkLava", safe ? "1" : "0")]);
            document.Paint([new(1, 2)], new(terrain, 2, 1));
            var plan = document.PlanObjectBatchPlacement(new(PlacedThingKind.Creature, 1, 3), new(Position: new(1, 2)));
            Expect(plan.Candidates[0].Warnings.HasFlag(ObjectBatchWarning.Fatal) == !safe && !plan.Candidates[0].Locked, "Project edited flags ignored.");
            document.SetGlobalDefinitionValues([new("Creatures", row.Index, "CanWalkLava", safe ? "0" : "1")]);
            Reject(() => document.ResolveObjectBatchPlacement(plan));
            document.Undo(); Expect(document.ResolveObjectBatchPlacement(plan, [0]).Count == 1, "Fatal warning could not be explicitly overridden in preview."); document.Undo();
        }
    }
    private static void CheckNativeDefinitions(string directory)
    {
        var source = NativeMapDocument.Create(12, 10, "Definitions"); source.Paint([new(2, 2)], new(4, 2, 1));
        var path = source.SaveCopy(Path.Combine(directory, "definitions"));
        var data = File.ReadAllBytes(Path.Combine(Repo(), "Binaries/Globals/DefaultCreatures.dkg"));
        var bytes = new byte[56 + data.Length]; data.CopyTo(bytes, 56);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, 0xAA); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), (uint)bytes.Length); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), (uint)data.Length / 5449);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 0xAB); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), 28); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48), 0xAC);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(52), (uint)data.Length);
        var definitionPath = Path.Combine(directory, "definitions", "Data", "Editor", "Creatures.kwd"); File.WriteAllBytes(definitionPath, bytes);
        var document = NativeMapDocument.Load(path); var row = document.GlobalDefinitionRows().First(row => row.Family == "Creatures" && row.ID == 1);
        foreach (var safe in new[] { false, true })
        {
            document.SetGlobalDefinitionValues([new("Creatures", row.Index, "CanWalkWater", safe ? "1" : "0")]);
            Expect(document.PlanObjectBatchPlacement(new(PlacedThingKind.Creature, 1, 3), new(Position: new(2, 2))).Candidates.Single().Warnings.HasFlag(ObjectBatchWarning.Fatal) == !safe, "Edited native creature flags ignored.");
        }
        // Real 5537-byte records retain the known prefix. Unsupported adjacent
        // sizes must still refuse to infer creature safety flags.
        var count = data.Length / 5449; var extended = new byte[56 + count * 5537]; bytes.AsSpan(0, 56).CopyTo(extended);
        BinaryPrimitives.WriteUInt32LittleEndian(extended.AsSpan(8), (uint)extended.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(extended.AsSpan(52), (uint)extended.Length - 56);
        for (var index = 0; index < count; index++) data.AsSpan(index * 5449, 5449).CopyTo(extended.AsSpan(56 + index * 5537));
        File.WriteAllBytes(definitionPath, extended); var verified = NativeMapDocument.Load(path);
        var known = verified.PlanObjectBatchPlacement(new(PlacedThingKind.Creature, 1, 3), new(Position: new(2, 2), Flags: (ObjectBatchFlags)127));
        Expect(!known.Candidates[0].Locked && !known.Candidates[0].Warnings.HasFlag(ObjectBatchWarning.Incompatible), "Verified 5537-byte flags were not used for placement.");
        var unsupported = new byte[56 + count * 5538]; bytes.AsSpan(0, 56).CopyTo(unsupported);
        BinaryPrimitives.WriteUInt32LittleEndian(unsupported.AsSpan(8), (uint)unsupported.Length); BinaryPrimitives.WriteUInt32LittleEndian(unsupported.AsSpan(52), (uint)unsupported.Length - 56);
        for (var index = 0; index < count; index++) data.AsSpan(index * 5449, 5449).CopyTo(unsupported.AsSpan(56 + index * 5538));
        File.WriteAllBytes(definitionPath, unsupported); var future = NativeMapDocument.Load(path);
        var unknown = future.PlanObjectBatchPlacement(new(PlacedThingKind.Creature, 1, 3), new(Position: new(2, 2), Flags: (ObjectBatchFlags)127));
        Expect(unknown.Candidates[0].Locked && unknown.Candidates[0].Warnings.HasFlag(ObjectBatchWarning.Incompatible), "Unsupported native definition treated as a default/false flag.");
        Reject(() => future.ResolveObjectBatchPlacement(unknown, [0]));
        Expect(!future.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1, 2), new(Position: new(2, 2))).Candidates[0].Locked, "Unrelated definition prevented object placement planning.");
    }
    private static void CheckUnknownLayouts(string directory)
    {
        var basis = NativeMapDocument.Create(12, 10, "Malformed"); var record = Add(basis, PlacedThingKind.Door, new(2, 2), 1);
        var path = basis.SaveCopy(Path.Combine(directory, "malformed-things")); var filePath = Path.Combine(directory, "malformed-things", record.File.ExportPath);
        var bytes = File.ReadAllBytes(filePath);
        // Known signature but partial editable fields: reader is lossless; planner
        // must report incompatible rather than throwing NullReference/Single.
        var shortBytes = bytes[..68]; BinaryPrimitives.WriteUInt32LittleEndian(shortBytes.AsSpan(4), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(shortBytes.AsSpan(8), 68); BinaryPrimitives.WriteUInt32LittleEndian(shortBytes.AsSpan(52), 12);
        BinaryPrimitives.WriteUInt32LittleEndian(shortBytes.AsSpan(60), 4); File.WriteAllBytes(filePath, shortBytes);
        var partial = NativeMapDocument.Load(path);
        var plan = partial.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1, 3), new(Position: new(2, 2), Replacement: ObjectReplacementPolicy.All, Flags: (ObjectBatchFlags)127));
        Expect(plan.Candidates[0].Locked && plan.Candidates[0].Warnings.HasFlag(ObjectBatchWarning.Incompatible), "Partial collection record was assumed complete.");
        Reject(() => partial.ResolveObjectBatchPlacement(plan, [0]));
        var tiny = NativeMapDocument.Create(1, 1, "Tiny");
        Expect(tiny.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), new(ObjectBatchMode.Rectangle)).Candidates.Count == 0, "Tiny whole-map range fabricated candidates.");
    }
    private static void CheckHistoricalProjects(string directory)
    {
        var project = NativeMapDocument.CreateNewMap(new("HistoryPlacement", "HistoryPlacement", 12, 10));
        project.Paint(MapDrawing.Rectangle(new(0, 0, 12, 10), true), new(8, 3, 1, 197));
        foreach (var kind in Enum.GetValues<PlacedThingKind>()) Add(project, kind, new(2, 2), kind == PlacedThingKind.SpecialItem ? 0x75u : 1u);
        var saved = project.SaveCopy(Path.Combine(directory, "history-basis")); var container = LegacySectionFile.Read(saved);
        var source = project.Records().Where(record => record.File.Signature == NativeMapDocument.LegacyThings).ToArray();
        var expected = Enum.GetValues<PlacedThingKind>().Select(kind => Projection(project.PlanObjectBatchPlacement(new(kind, 1, 3), new(Position: new(2, 2))))).ToArray();
        for (var version = 1; version <= 6; version++)
        {
            var structure = version switch { 1 => "MAPTHINGV1", 2 or 3 => "MAPTHINGV2", 4 => "MAPTHINGV4", 5 => "MAPTHINGV5", _ => "MAPTHING" };
            var stride = LegacyProjectSchemas.Size(structure); var fields = LegacyProjectSchemas.Fields(structure);
            var payload = new byte[source.Length * stride];
            for (var index = 0; index < source.Length; index++) foreach (var field in fields)
            {
                if (source[index].Fields.FirstOrDefault(item => item.Name == field.Name) is { } original)
                    field.Encode(source[index].Read(original)).CopyTo(payload, index * stride + field.Offset);
            }
            var bytes = LegacySectionFile.Create(container.Sections.Select(section => section.Signature == NativeMapDocument.LegacyThings ? new(section.Signature, version, payload) : section)).ToArray();
            var path = Path.Combine(directory, "historical-things-" + version + ".dkm"); File.WriteAllBytes(path, bytes);
            var historical = NativeMapDocument.Load(path); var snapshots = historical.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var actual = Enum.GetValues<PlacedThingKind>().Select(kind => Projection(historical.PlanObjectBatchPlacement(new(kind, 1, 3), new(Position: new(2, 2))))).ToArray();
            Expect(actual.SequenceEqual(expected), "Historical thing schema changed placement/replacement decisions.");
            Expect(snapshots.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && !historical.CanUndo && !historical.CanRedo
                && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes), "Historical planning upgraded/modified source.");
        }
        var futurePath = Path.Combine(directory, "future-things.dkm");
        File.WriteAllBytes(futurePath, LegacySectionFile.Create(container.Sections.Select(section => section.Signature == NativeMapDocument.LegacyThings ? new(section.Signature, 7, "FUTURE"u8.ToArray()) : section)).ToArray());
        var future = NativeMapDocument.Load(futurePath);
        var blocked = future.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1, 3), new(Position: new(2, 2), Flags: (ObjectBatchFlags)127));
        Expect(blocked.Candidates[0].Locked && blocked.Candidates[0].Warnings.HasFlag(ObjectBatchWarning.Incompatible), "Unknown project schema was hidden by ordinary terrain.");
        Reject(() => future.ResolveObjectBatchPlacement(blocked, [0]));
    }
    private static void CheckLargeMap()
    {
        var document = NativeMapDocument.Create(1024, 1024, "LargePlacement"); var calls = 0;
        var before = document.TileFile.Data.ToArray();
        var plan = document.PlanObjectBatchPlacement(new(PlacedThingKind.Object, 1), new(ObjectBatchMode.RandomRectangle, RandomFrequency: 32), _ => calls++ % 32);
        var expected = Enumerable.Range(0, 1023 * 1023).Where(index => index % 32 == 0).Select(index => new TilePoint(1 + index % 1023, 1 + index / 1023));
        Expect(calls == 1023 * 1023 && plan.Candidates.Select(candidate => candidate.Position).SequenceEqual(expected), "Large-map candidates clipped/reordered/resampled.");
        Expect(document.TileFile.Data.Span.SequenceEqual(before) && !document.CanUndo && !document.CanRedo, "Large-map plan created map changes.");
    }
    public static void CheckLoaded(NativeMapDocument document)
    {
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = document.GetHistory();
        foreach (var kind in Enum.GetValues<PlacedThingKind>())
        {
            var selection = new TileRect(0, 0, Math.Min(8, document.Width), Math.Min(8, document.Height));
            var plan = document.PlanObjectBatchPlacement(new(kind, kind == PlacedThingKind.SpecialItem ? 0x75u : 1u),
                new(ObjectBatchMode.Rectangle, WholeMap: false, Selection: selection));
            Expect(plan.SampledCells == selection.Width * selection.Height, "Loaded candidate region incomplete.");
            foreach (var candidate in plan.Candidates)
            {
                var tile = ReferenceTile(document.GetTile(candidate.Position.X, candidate.Position.Y), candidate.Position.X == 0 || candidate.Position.Y == 0 || candidate.Position.X == document.Width - 1 || candidate.Position.Y == document.Height - 1);
                Expect(candidate.Owner == tile.Owner, "Loaded plan auto-owner/source projection wrong.");
                if (tile.Terrain == 254) Expect(candidate.Locked && candidate.Warnings.HasFlag(ObjectBatchWarning.Boundary), "Loaded source boundary was not locked.");
                if (kind != PlacedThingKind.Creature)
                {
                    var verdict = ReferencePermission(tile.Terrain, tile.Owner, kind, plan.Template.TypeID, candidate.Owner, false, false);
                    Expect(candidate.Warnings.HasFlag(ObjectBatchWarning.BadTerrain) == (verdict == 0), "Loaded source terrain rule differs.");
                }
            }
            _ = document.ResolveObjectBatchPlacement(plan);
        }
        Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && history.Undo.SequenceEqual(document.GetHistory().Undo) && history.Redo.SequenceEqual(document.GetHistory().Redo), "Corpus placement previews mutated source/history.");
    }
}
