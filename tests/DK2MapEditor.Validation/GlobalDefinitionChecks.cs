using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class GlobalDefinitionChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new Exception(message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> bytes)
    { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Definition edit/reset/undo changed unexpected bytes: " + file.ExportPath); }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException) { return; }
        throw new Exception("Invalid global definition edit was accepted.");
    }
    private static string FileName(string family) => family switch { "Terrains" => "Terrain", "Spells" => "KeeperSpells", "CrSpells" => "CreatureSpells", _ => family };
    private static void CheckDefinitionOrders()
    {
        // Literal source array order, including opaque -2 entries but excluding
        // the -1 terminators. Shared cached values must never become mutable.
        Dictionary<string,int[]> expected = new()
        {
            ["Terrains"] = [1,2,3,4,5,6,7,8,9,17,15,30,23,14,16,24,19,26,25,27,20,22,21,18,29,28,-2,33,34,0,10,11,35,36,12,31,37,13,-2,32,-2],
            ["Rooms"] = [3,1,16,9,0,2,10,5,12,11,13,6,8,7,4,15,14,19,20,21,22,17,23,-2,18,-2],
            ["Doors"] = [1,3,2,4,5,0,6,7],
            ["Traps"] = [5,8,9,0,1,2,10,4,3,6,7,14,-2,11,12,13],
            ["Spells"] = [2,1,5,0,11,3,8,6,9,4,10,7,12],
            ["Creatures"] = [0,44,10,7,2,5,1,11,6,4,8,3,30,29,34,33,35,31,36,37,41,12,13,9,32,42,15,39,40,38,43,18,16,17,19,20,21,22,23,24,25,26,27,28,14,-2],
            ["CrSpells"] = Enumerable.Range(0,41).ToArray()
        };
        foreach (var (family,values) in expected)
        {
            var order = LegacyCompilerRules.DefinitionOrder(family); Expect(order.SequenceEqual(values),"Cached definition order differs from literal source: " + family);
            Parallel.For(0,16,_ => Expect(ReferenceEquals(order,LegacyCompilerRules.DefinitionOrder(family)),"Definition order repeatedly parsed/shared cache lost."));
            Expect(order is IList<int> { IsReadOnly: true },"Definition order cache exposed mutable array.");
            try { ((IList<int>)order)[0] = -999; throw new Exception("Definition order cache changed through mutable cast."); } catch (NotSupportedException) { }
            Expect(LegacyCompilerRules.DefinitionOrder(family).SequenceEqual(values),"Failed mutation poisoned definition order.");
        }
        Reject(() => LegacyCompilerRules.DefinitionOrder("NotAnOriginalFamily"));
    }
    public static void Run()
    {
        CheckDefinitionOrders();
        GlobalDefinitionImageChecks.Run();
        var project = NativeMapDocument.CreateNewMap(new("Definitions", "Definitions", 20, 20));
        var rows = project.GlobalDefinitionRows();
        if (rows.Count != 184 || rows.Any(row => !row.Editable)) throw new Exception("Incomplete source definition catalogue: " + string.Join(",", rows.GroupBy(row => row.Family).Select(group => group.Key + "=" + group.Count())));
        if (rows.GroupBy(row => row.Family).Sum(group => project.GlobalDefinitionValues(group.First()).Count) != 262) throw new Exception("Incomplete source definition/default fields.");
        var bytes = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var changed = project.HasChanges;
        project.SetGlobalDefinitionValues([new("Terrains", 6, "GoldValue", "444")]);
        if (project.GlobalDefinitionValues(project.GlobalDefinitionRows().Single(row => row.Family == "Terrains" && row.Index == 6)).Single(field => field.Name == "GoldValue").Value != "444") throw new Exception("Default-backed edit failed.");
        project.Undo(); foreach (var file in project.Files) if (!file.Data.Span.SequenceEqual(bytes[file])) throw new Exception("Default-backed undo changed bytes.");
        if (project.HasChanges != changed) throw new Exception("First global-family undo changed the initial dirty state.");
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "DK2MapEditor.slnx"))) root = root.Parent;
        var source = LegacyGlobalSchemas.All.ToDictionary(family => family.Group, family => File.ReadAllBytes(Path.Combine(root!.FullName, "Binaries", "Globals", "Default" + FileName(family.Group) + ".dkg")));
        var directory = Path.Combine(Path.GetTempPath(), "DK2-Definitions-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var notifications = 0; project.Changed += (_, _) => notifications++;
            foreach (var section in GlobalDefinitionDefaults.Sections) project.ResetGlobalDefinitions(section.Name);
            Exact(project, bytes); Expect(notifications == 0, "Default reset materialized missing arrays or history.");
            project.Redo(); Expect(project.GlobalDefinitionRows().Count(row => row.Record is not null) == 38, "Redo failed to restore first family."); project.Undo();
            var savedBlank = project.SaveCopy(Path.Combine(directory, "blank"));
            Expect(!LegacySectionFile.Read(savedBlank).Sections.Any(section => section.Signature == NativeMapDocument.LegacyGlobals), "Unchanged optional KGBL leaked into serialization.");
            var basisPath = NativeMapDocument.Create(20, 20, "Basis").SaveCopy(Path.Combine(directory, "basis"));
            foreach (var family in LegacyGlobalSchemas.All)
            {
                var defaults = source[family.Group]; var count = defaults.Length / family.RecordSize;
                Expect(defaults.Length == count * family.RecordSize && count == LegacyCompilerRules.DefinitionOrder(family.Group).Count, "Source default size/order mismatch.");
                var nativeBytes = new byte[56 + defaults.Length];
                void Header(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(nativeBytes.AsSpan(offset), value);
                Header(0, family.Signature); Header(4, 4); Header(8, (uint)nativeBytes.Length); Header(12, family.Signature + 1); Header(16, 28); Header(20, (uint)count); Header(48, family.Signature + 2); Header(52, (uint)defaults.Length);
                defaults.CopyTo(nativeBytes, 56); File.WriteAllBytes(Path.Combine(directory, "basis", "Data", "Editor", FileName(family.Group) + ".kwd"), nativeBytes);
            }
            var basis = NativeMapDocument.Load(basisPath); var basisBytes = Snapshot(basis);
            foreach (var row in basis.GlobalDefinitionRows().Where(row => row.Family is not ("Terrains" or "Rooms")))
            {
                var name = ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue(row.Family)[row.Index], row.ID.ToString());
                Expect(name.English.Length > 0 && name.Russian.Length > 0, "Missing bilingual definition name.");
            }
            Check(basis, source, false, directory); Check(project, source, true, directory);
            var beforeCompile = Snapshot(project); var compiled = project.CompileLegacyProject("DefinitionsCompiled", basis);
            Exact(project, beforeCompile); Exact(basis, basisBytes);
            foreach (var row in compiled.GlobalDefinitionRows())
            {
                var definition = LegacyGlobalSchemas.All.Single(family => family.Group == row.Family); var physical = LegacyCompilerRules.DefinitionOrder(row.Family).ToList().IndexOf(row.Index);
                foreach (var field in definition.Fields)
                    Expect(row.Record!.Read(field) == field.Read(source[row.Family].AsSpan(physical * definition.RecordSize, definition.RecordSize)), "Project defaults compiled to wrong native bits: " + row.Family + "." + field.Name);
            }
            var saved = project.SaveCopy(Path.Combine(directory, "project"));
            Expect(Projection(project) == Projection(NativeMapDocument.Load(saved)), "Global values changed after project save/reopen.");
            var sections = LegacySectionFile.Read(saved); var global = sections.Sections.Single(section => section.Signature == NativeMapDocument.LegacyGlobals);
            // The serialized array has one unused creature slot beyond the
            // 45 named species. Never reset or silently zero imported bytes.
            var preservedPayload = global.Payload.ToArray();
            var creatureOffset = 4 + new[] { ("GLOBALTERRAIN", 38), ("GLOBALROOM", 24), ("GLOBALDOOR", 8), ("GLOBALTRAP", 15), ("GLOBALSPELL", 13) }.Sum(item => LegacyProjectSchemas.Size(item.Item1) * item.Item2);
            var unusedOffset = creatureOffset + 45 * LegacyProjectSchemas.Size("GLOBALCREATURE");
            preservedPayload.AsSpan(unusedOffset, LegacyProjectSchemas.Size("GLOBALCREATURE")).Fill(0xA7);
            var preservedPath = Path.Combine(directory, "unused.dkm");
            File.WriteAllBytes(preservedPath, LegacySectionFile.Create(sections.Sections.Select(section => section == global ? new LegacySection(section.Signature, section.Version, preservedPayload) : section)).ToArray());
            var preserved = NativeMapDocument.Load(preservedPath); var preservedBytes = Snapshot(preserved);
            foreach (var section in GlobalDefinitionDefaults.Sections) preserved.ResetGlobalDefinitions(section.Name);
            Exact(preserved, preservedBytes); preserved.SetGlobalDefinitionValues([new("Creatures", 0, "Health", "999")]);
            var preservedGlobal = preserved.Files.Single(file => file.Signature == NativeMapDocument.LegacyGlobals);
            Expect(preservedGlobal.Data.Span.Slice(12 + unusedOffset, LegacyProjectSchemas.Size("GLOBALCREATURE")).ToArray().All(value => value == 0xA7), "Unnamed creature slot was modified."); preserved.Undo(); Exact(preserved, preservedBytes);
            foreach (var (version, flags) in new[] { (99, 0x1FF1Fu), (1, 0x8001FF1Fu) })
            {
                var payload = global.Payload.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(payload, flags);
                var future = Path.Combine(directory, $"unknown-{version}.dkm");
                File.WriteAllBytes(future, LegacySectionFile.Create(sections.Sections.Select(section => section == global ? new LegacySection(section.Signature, version, payload) : section)).ToArray());
                var unknown = NativeMapDocument.Load(future); var unknownBytes = Snapshot(unknown);
                Expect(unknown.GlobalDefinitionRows().All(row => !row.Editable) && unknown.GlobalDefinitionValues(unknown.GlobalDefinitionRows()[0]).All(value => value.Value == ""), "Unknown flags/version exposed guessed current values.");
                Reject(() => unknown.ResetGlobalDefinitions("Terrains")); Reject(() => unknown.CompileLegacyProject("UnknownDefinitions", basis)); Exact(unknown, unknownBytes);
            }
            var missing = NativeMapDocument.Create(20, 20, "Missing"); Expect(missing.GlobalDefinitionRows().All(row => !row.Editable), "Missing native definitions invented editable defaults.");
            Reject(() => missing.SetGlobalDefinitionValues([new("Terrains", 0, "GoldValue", "1")]));
            var nativeSave = basis.SaveCopy(Path.Combine(directory, "native")); Expect(Projection(basis) == Projection(NativeMapDocument.Load(nativeSave)), "Definition save/reopen changed native values.");
            var variantPath = basis.SaveCopy(Path.Combine(directory, "variant"));
            var creatureFile = Path.Combine(directory, "variant", "Data", "Editor", "Creatures.kwd");
            var originalCreature = File.ReadAllBytes(creatureFile); var creatureCount = source["Creatures"].Length / 5449;
            var variant = new byte[56 + creatureCount * 5537]; originalCreature.AsSpan(0, 56).CopyTo(variant);
            BinaryPrimitives.WriteUInt32LittleEndian(variant.AsSpan(8), (uint)variant.Length); BinaryPrimitives.WriteUInt32LittleEndian(variant.AsSpan(52), (uint)variant.Length - 56);
            for (var index = 0; index < creatureCount; index++) { originalCreature.AsSpan(56 + index * 5449, 5449).CopyTo(variant.AsSpan(56 + index * 5537)); variant.AsSpan(56 + index * 5537 + 5449, 88).Fill(0xCD); }
            File.WriteAllBytes(creatureFile, variant); var verifiedVariant = NativeMapDocument.Load(variantPath); var verifiedBytes = Snapshot(verifiedVariant);
            Expect(verifiedVariant.GlobalDefinitionRows().All(row => row.Editable), "Verified 5537-byte prefix disabled known definitions.");
            verifiedVariant.SetGlobalDefinitionValues([new("Terrains", 0, "GoldValue", "1"), new("Creatures", 0, "Health", "1")]);
            foreach (var record in verifiedVariant.Records().Where(record => record.Category == "Definition.Creatures"))
                Expect(record.File.Data.Span.Slice(record.Offset + 5449, 88).ToArray().All(value => value == 0xCD), "Known definition edit changed opaque 5537-byte tail.");
            verifiedVariant.Undo(); Exact(verifiedVariant, verifiedBytes);
            var unsupported = new byte[56 + creatureCount * 5538]; originalCreature.AsSpan(0, 56).CopyTo(unsupported);
            BinaryPrimitives.WriteUInt32LittleEndian(unsupported.AsSpan(8), (uint)unsupported.Length); BinaryPrimitives.WriteUInt32LittleEndian(unsupported.AsSpan(52), (uint)unsupported.Length - 56);
            for (var index = 0; index < creatureCount; index++) originalCreature.AsSpan(56 + index * 5449, 5449).CopyTo(unsupported.AsSpan(56 + index * 5538));
            File.WriteAllBytes(creatureFile, unsupported); var unknownVariant = NativeMapDocument.Load(variantPath); var variantBytes = Snapshot(unknownVariant);
            Expect(unknownVariant.GlobalDefinitionRows().Where(row => row.Family == "Creatures").All(row => !row.Editable) && unknownVariant.GlobalDefinitionRows().Where(row => row.Family != "Creatures").All(row => row.Editable), "Unknown 5538-byte variant guessed offsets or disabled known families.");
            Reject(() => unknownVariant.SetGlobalDefinitionValues([new("Terrains", 0, "GoldValue", "1"), new("Creatures", 0, "Health", "1")])); Exact(unknownVariant, variantBytes);
            // Two separately declared arrays whose combined record count
            // happens to equal the source count are not a verified catalogue.
            var splitPath = basis.SaveCopy(Path.Combine(directory, "split"));
            var terrainFile = Path.Combine(directory, "split", "Data", "Editor", "Terrain.kwd"); var originalTerrain = File.ReadAllBytes(terrainFile);
            byte[] Chunk(int start, int count)
            {
                var chunk = new byte[56 + count * 552]; originalTerrain.AsSpan(0, 56).CopyTo(chunk);
                BinaryPrimitives.WriteUInt32LittleEndian(chunk.AsSpan(8), (uint)chunk.Length); BinaryPrimitives.WriteUInt32LittleEndian(chunk.AsSpan(20), (uint)count); BinaryPrimitives.WriteUInt32LittleEndian(chunk.AsSpan(52), (uint)chunk.Length - 56);
                originalTerrain.AsSpan(56 + start * 552, count * 552).CopyTo(chunk.AsSpan(56)); return chunk;
            }
            File.WriteAllBytes(terrainFile, Chunk(0, 20).Concat(Chunk(20, 21)).ToArray()); var split = NativeMapDocument.Load(splitPath); var splitBytes = Snapshot(split);
            Expect(split.GlobalDefinitionRows().Where(row => row.Family == "Terrains").All(row => !row.Editable), "Split duplicate sections were guessed to have one catalogue order."); Reject(() => split.ResetGlobalDefinitions("Terrains")); Exact(split, splitBytes);
            foreach (var family in LegacyGlobalSchemas.All)
                Expect(File.ReadAllBytes(Path.Combine(root!.FullName, "Binaries", "Globals", "Default" + FileName(family.Group) + ".dkg")).AsSpan().SequenceEqual(source[family.Group]), "Original defaults modified.");
        }
        finally { Directory.Delete(directory, true); }
    }
    private static string Projection(NativeMapDocument document) => string.Join("\n", document.GlobalDefinitionRows().SelectMany(row => document.GlobalDefinitionValues(row).Select(field => $"{row.Family}:{row.Index}:{field.Name}={field.Value}/{field.DefaultValue}")));
    private static void Check(NativeMapDocument document, Dictionary<string, byte[]> source, bool project, string directory)
    {
        var initial = Snapshot(document); var selected = document.GlobalDefinitionRows().GroupBy(row => row.Family).Select(group => group.First()).ToArray();
        var changes = selected.SelectMany(row => document.GlobalDefinitionValues(row).Select(value => new GlobalDefinitionChange(row.Family, row.Index, value.Name,
            LegacyGlobalSchemas.All.Single(family => family.Group == row.Family).Fields.Single(field => field.Name == value.Name).BitMask != 0
                ? value.Value == "0" ? "1" : "0" : value.Value == "25" ? "26" : "25"))).ToArray();
        document.SetGlobalDefinitionValues(changes); var edited = Snapshot(document);
        Expect(document.GlobalDefinitionRows().All(row => row.Editable && row.Record is not null), "Batch did not materialize all seven arrays.");
        if (project) Expect(BinaryPrimitives.ReadUInt32LittleEndian(document.Files.Single(file => file.Signature == NativeMapDocument.LegacyGlobals).Data.Span[12..]) == 0x1FF1F, "Original whole-family KGBL masks differ.");
        document.Undo(); Exact(document, initial); document.Redo(); Exact(document, edited);
        var saved = document.SaveCopy(Path.Combine(directory, project ? "edited-project" : "edited-native")); var reopened = NativeMapDocument.Load(saved);
        Expect(Projection(document) == Projection(reopened), "Edited definitions changed after save/reopen."); Exact(document, edited);
        if (!project) foreach (var family in LegacyGlobalSchemas.All)
        {
            var file = reopened.Files.Single(file => file.Signature == family.Signature);
            Expect(file.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar) && file.Data.Span.SequenceEqual(document.Files.Single(item => item.Signature == family.Signature).Data.Span), "Changed definitions were not exact map-scoped exports.");
        }
        foreach (var section in GlobalDefinitionDefaults.Sections)
        {
            var row = document.GlobalDefinitionRows().Single(row => row.Family == section.Family && row.Index == selected.Single(item => item.Family == section.Family).Index);
            var values = document.GlobalDefinitionValues(row); var expected = Snapshot(document);
            foreach (var value in values.Where(value => value.Section == section.Name))
            {
                var field = row.Record!.Fields.Single(field => field.Name == value.Name); var encoded = field.Encode(value.DefaultValue); var offset = row.Record.Offset + field.Offset;
                if (field.BitMask != 0) expected[row.Record.File][offset] = (byte)((expected[row.Record.File][offset] & ~field.BitMask) | (encoded[0] == 0 ? 0 : field.BitMask));
                else encoded.CopyTo(expected[row.Record.File], offset);
            }
            document.ResetGlobalDefinitions(section.Name, [row.Index]); Exact(document, expected);
            document.Undo(); Exact(document, edited); document.Redo(); Exact(document, expected); document.Undo();
        }
        foreach (var section in GlobalDefinitionDefaults.Sections) document.ResetGlobalDefinitions(section.Name);
        foreach (var row in document.GlobalDefinitionRows())
        {
            var definition = LegacyGlobalSchemas.All.Single(family => family.Group == row.Family); var physical = LegacyCompilerRules.DefinitionOrder(row.Family).ToList().IndexOf(row.Index);
            foreach (var value in document.GlobalDefinitionValues(row)) Expect(value.Value == value.DefaultValue, "Reset-all missed a source row/group.");
            if (!project) foreach (var field in definition.Fields)
                Expect(row.Record!.Read(field) == field.Read(source[row.Family].AsSpan(physical * definition.RecordSize, definition.RecordSize)), "Native source reset/default order wrong.");
        }
        if (!project) Exact(document, initial);
        var unchanged = Snapshot(document); var history = document.CanUndo; var notifications = 0;
        document.Changed += (_, _) => notifications++;
        foreach (var section in GlobalDefinitionDefaults.Sections) document.ResetGlobalDefinitions(section.Name);
        Exact(document, unchanged); Expect(document.CanUndo == history && notifications == 0, "No-op reset created history/notifications.");
        Reject(() => document.SetGlobalDefinitionValues([new("Terrains", 0, "GoldValue", "444"), new("Terrains", 0, "GoldValue", "555")]));
        Reject(() => document.SetGlobalDefinitionValues([new("Terrains", 0, "GoldValue", "444"), new("Unknown", 0, "GoldValue", "555")]));
        var flag = document.GlobalDefinitionValues(document.GlobalDefinitionRows().First(row => row.Family == "Creatures")).First(value => value.Section == "Flags");
        Reject(() => document.SetGlobalDefinitionValues([new("Terrains", 0, "GoldValue", "444"), new("Creatures", 0, flag.Name, "2")]));
        Reject(() => document.ResetGlobalDefinitions("Attributes", [0, 0])); Reject(() => document.ResetGlobalDefinitions("Attributes", [999])); Exact(document, unchanged);
    }
}
