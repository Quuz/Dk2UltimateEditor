using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static class GlobalVariablesSessionChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; }
        throw new Exception("An invalid shared-global operation was accepted.");
    }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> expected)
    { Expect(document.Files.Count == expected.Count, "File topology changed."); foreach (var file in document.Files) Expect(expected[file].AsSpan().SequenceEqual(file.Data.Span), "Unrelated bytes/history changed: " + file.ExportPath); }
    private static GlobalDefinitionRow Row(NativeMapDocument document, string family, int index = 0) => document.GlobalDefinitionRows().Single(row => row.Family == family && row.Index == index);
    private static string Value(NativeMapDocument document, string family, int index, string field) => document.GlobalDefinitionValues(Row(document, family, index)).Single(value => value.Name == field).Value;
    private static JsonDocument Oracle()
    { using var stream = typeof(GlobalVariablesSessionChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.GlobalVariablesSessionSource.json")!; return JsonDocument.Parse(stream); }
    private static NativeMapDocument[] Documents(string scope) => [NativeMapDocument.CreateNewMap(new(scope, scope, 20, 20)), GlobalDefinitionNumericChecks.Fixture(Path.Combine(Path.GetTempPath(), "DK2SharedGlobals", Guid.NewGuid().ToString("N"), scope))];
    private static void Raw(NativeFile file, int offset, Action<byte[], int> write)
    { Expect(MemoryMarshal.TryGetArray(file.Data, out var data), "Fixture backing unavailable."); write(data.Array!, data.Offset + offset); }

    public static void Composition()
    {
        using var oracle = Oracle();
        var pages = oracle.RootElement.GetProperty("Pages").EnumerateArray().ToArray();
        Expect(pages.Select(page => page.GetProperty("Family").GetString()).SequenceEqual(GlobalVariablesSession.Families), "Five-page source order differs.");
        Expect(pages.Sum(page => page.GetProperty("Rows").GetInt32()) == 98 && oracle.RootElement.GetProperty("EditableFamilyFieldValues").GetInt32() == 2016, "Original shared scope is incomplete.");
        foreach (var doc in Documents("SharedComposition"))
        {
            doc.SetHistoryDepth(65535); var original = Snapshot(doc); var history = doc.GetHistory().Undo.Count; var notices = 0;
            doc.Changed += (_, _) => notices++;
            using (var untouched = doc.CreateGlobalVariablesSession())
            {
                var noOp = doc.PrepareGlobalVariables(untouched); Expect(!noOp.Changed, "Opening the five-page context changed defaults.");
                doc.ApplyGlobalVariables(noOp); Exact(doc, original);
                Expect(doc.GetHistory().Undo.Count == history && notices == 0, "No-op consumed history/notification.");
                Reject(() => doc.PrepareGlobalVariables(untouched));
            }
            var session = doc.CreateGlobalVariablesSession(); var working = session.WorkingDocument;
            var changes = new List<GlobalDefinitionChange>();
            foreach (var page in pages)
            {
                var family = page.GetProperty("Family").GetString()!; var rows = working.GlobalDefinitionRows().Where(row => row.Family == family).ToArray();
                var sourceFields = page.GetProperty("EditableFields").EnumerateArray().Select(field => field.GetString()).ToArray();
                Expect(rows.Length == page.GetProperty("Rows").GetInt32() && rows.All(row => row.Editable), "Missing original page rows.");
                Expect(working.GlobalDefinitionValues(rows[0]).Select(field => field.Name).Order().SequenceEqual(sourceFields.Order()), "Shared page granted opaque field permission.");
                var field = family switch { "Terrains" => "GoldValue", "Rooms" or "Doors" => "Cost", "Traps" => "GoldCost", _ => "ManaCost" };
                foreach (var row in rows)
                    changes.Add(new(family, row.Index, field, Value(doc, family, row.Index, field) == "777" ? "778" : "777"));
            }
            working.SetGlobalDefinitionValues(changes); Exact(doc, original);
            // Submit both nested spell slots to the same private document.
            var spell = working.CreateGlobalSpellDraft(Row(working, "Spells"));
            var shot = GlobalProjectiles.Options.First(option => option.Form == 0);
            spell = working.UpdateGlobalSpellDraft(spell, GlobalSpellControls.MainFields.Select(field => new GlobalSpellInput(field,
                field is "ShotType" or "UpgShotType" ? shot.ID.ToString(CultureInfo.InvariantCulture) : spell.Display(field))));
            spell = working.UpdateGlobalSpellShotDraft(spell, "Shot", shot.Signed1 ? "-9" : "11", shot.Signed2 ? "-7" : "12");
            spell = working.UpdateGlobalSpellShotDraft(spell, "Upgrade", shot.Signed1 ? "-8" : "21", shot.Signed2 ? "-6" : "22");
            working.ApplyGlobalSpellDraft(spell); Exact(doc, original);
            var plan = doc.PrepareGlobalVariables(session);
            Expect(plan.ChangedRows == 98 && plan.Changes.Count >= 98, "Common Apply lost a privately edited row/family.");
            var privateValues = plan.Changes.ToDictionary(change => (change.Family, change.Index, change.Field), change => change.Value);
            var literal = original.ToDictionary(item => item.Key, item => item.Value.ToArray());
            if (!doc.IsLegacyProject)
                foreach (var change in plan.Changes)
                {
                    var name = change.Family switch { "Terrains" => "Terrain", "Rooms" => "Room", "Doors" => "Door", "Traps" => "Trap", _ => "Spell" };
                    using var stream = typeof(GlobalVariablesSessionChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.Global" + name + "Source.json")!;
                    using var fieldOracle = JsonDocument.Parse(stream);
                    var storage = fieldOracle.RootElement.GetProperty("NativeStorage").GetProperty(change.Field);
                    var record = Row(doc, change.Family, change.Index).Record!; var offset = record.Offset + storage.GetProperty("Offset").GetInt32();
                    var number = long.Parse(change.Value, CultureInfo.InvariantCulture); var bytes = literal[record.File];
                    if (storage.TryGetProperty("Mask", out var flag)) { var mask = flag.GetByte(); bytes[offset] = (byte)((bytes[offset] & ~mask) | (number == 0 ? 0 : mask)); }
                    else if (storage.GetProperty("Size").GetInt32() == 1) bytes[offset] = (byte)number;
                    else if (storage.GetProperty("Size").GetInt32() == 2) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), unchecked((ushort)number));
                    else BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), unchecked((uint)number));
                }
            var reentrantRejected = false;
            void Reentrant(object? sender, EventArgs args) { Reject(() => doc.ApplyGlobalVariables(plan)); reentrantRejected = true; }
            doc.Changed += Reentrant; doc.ApplyGlobalVariables(plan); doc.Changed -= Reentrant;
            Expect(reentrantRejected && notices == 1 && doc.GetHistory().Undo.Count == history + 1, "Five-family commit is not one guarded Undo/notification.");
            Expect(privateValues.All(item => Value(doc, item.Key.Family, item.Key.Index, item.Key.Field) == item.Value), "Common Apply lost private/nested values.");
            if (!doc.IsLegacyProject) Exact(doc, literal);
            else
            {
                var basis = GlobalDefinitionNumericChecks.Fixture(Path.Combine(Path.GetTempPath(), "DK2SharedGlobals", Guid.NewGuid().ToString("N"), "compiler"));
                var compiled = doc.CompileLegacyProject("SharedCompiled", basis);
                Expect(privateValues.All(item => Value(compiled, item.Key.Family, item.Key.Index, item.Key.Field) == item.Value), "Shared project changes were not compiled compatibly.");
            }
            var applied = Snapshot(doc);
            var reopened = NativeMapDocument.Load(doc.SaveCopy(Path.Combine(Path.GetTempPath(), "DK2SharedGlobals", Guid.NewGuid().ToString("N"))));
            Expect(privateValues.All(item => Value(reopened, item.Key.Family, item.Key.Index, item.Key.Field) == item.Value), "Shared values did not survive save/reopen.");
            doc.Undo(); Exact(doc, original); doc.Redo(); Exact(doc, applied); doc.Undo(); Exact(doc, original);
            Reject(() => doc.ApplyGlobalVariables(plan));
            using (var canceled = doc.CreateGlobalVariablesSession())
            {
                canceled.WorkingDocument.SetGlobalDefinitionValues(changes); doc.ResetGlobalVariables(canceled, "Traps");
                Exact(doc, original);
            }
            Exact(doc, original);
            if (doc.IsLegacyProject)
            {
                using var reverted = doc.CreateGlobalVariablesSession();
                var initial = Value(doc, "Spells", 0, "ManaCost");
                reverted.WorkingDocument.SetGlobalDefinitionValues([new("Spells", 0, "ManaCost", initial == "999" ? "998" : "999")]);
                reverted.WorkingDocument.SetGlobalDefinitionValues([new("Spells", 0, "ManaCost", initial)]);
                var noOp = doc.PrepareGlobalVariables(reverted); Expect(!noOp.Changed, "A privately reverted family materialized live defaults.");
                var count = doc.GetHistory().Undo.Count; doc.ApplyGlobalVariables(noOp); Exact(doc, original); Expect(doc.GetHistory().Undo.Count == count, "Private revert consumed live history.");
            }
            Console.WriteLine($"SHARED COMPOSITION native={!doc.IsLegacyProject} pages=5 rows=98 private-slots cancel one-undo reentrant reopen no-op");
        }
    }

    public static void ResetsAndScope()
    {
        using var oracle = Oracle();
        var pages = oracle.RootElement.GetProperty("Pages").EnumerateArray().ToArray();
        // Independently parse the captured complete original reset function,
        // rather than asking the implementation which fields to reset.
        var reset = oracle.RootElement.GetProperty("Functions").GetProperty("Properties_GlobalReset").GetString()!;
        foreach (var page in pages)
        {
            var family = page.GetProperty("Family").GetString()!;
            var source = Regex.Matches(reset, @"Variables->" + family + @"\[Offset\]\.(\w+(?:\[\d+\])?)\s*=")
                .Select(match => match.Groups[1].Value).ToArray();
            Expect(source.Order().SequenceEqual(page.GetProperty("EditableFields").EnumerateArray().Select(field => field.GetString()).Order()), "Captured reset assignments disagree with the source page.");
        }
        foreach (var doc in Documents("SharedReset"))
        {
            doc.SetHistoryDepth(65535);
            var imports = new List<GlobalDefinitionChange>();
            foreach (var row in doc.GlobalDefinitionRows().Where(row => GlobalVariablesSession.Families.Contains(row.Family)))
                foreach (var field in doc.GlobalDefinitionValues(row))
                {
                    // Source flags stay valid. Numeric and named imports are
                    // intentionally outside UI choices but within their storage.
                    var flag = Regex.IsMatch(reset, @"Variables->" + row.Family + @"\[Offset\]\." + Regex.Escape(field.Name) + @"\s*=\s*\(BOOL\)");
                    imports.Add(new(row.Family, row.Index, field.Name, flag ? field.Value == "0" ? "1" : "0" : "127"));
                }
            Expect(imports.Count == 2016, "Reset coverage omitted source fields/hidden ShotDelay.");
            doc.SetGlobalDefinitionValues(imports); var imported = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
            using (var current = doc.CreateGlobalVariablesSession())
            {
                foreach (var page in pages) doc.ResetGlobalVariables(current, page.GetProperty("Family").GetString()!, 0);
                foreach (var row in current.WorkingDocument.GlobalDefinitionRows().Where(row => GlobalVariablesSession.Families.Contains(row.Family)))
                    foreach (var field in current.WorkingDocument.GlobalDefinitionValues(row))
                        Expect(field.Value == (row.Index == 0 ? field.DefaultValue : Value(doc, row.Family, row.Index, field.Name)), "Current reset escaped its row or omitted a source field.");
                doc.PrepareGlobalVariables(current); Exact(doc, imported);
            }
            using (var all = doc.CreateGlobalVariablesSession())
            {
                foreach (var page in pages) doc.ResetGlobalVariables(all, page.GetProperty("Family").GetString()!);
                foreach (var row in all.WorkingDocument.GlobalDefinitionRows().Where(row => GlobalVariablesSession.Families.Contains(row.Family)))
                    Expect(all.WorkingDocument.GlobalDefinitionValues(row).All(field => field.Value == field.DefaultValue), "Family reset omitted an original field/row.");
                Exact(doc, imported); var plan = doc.PrepareGlobalVariables(all); doc.ApplyGlobalVariables(plan);
                Expect(doc.GetHistory().Undo.Count == history + 1, "All-page reset split the common transaction.");
                doc.Undo(); Exact(doc, imported);
            }
            foreach (var outside in new Action<NativeMapDocument>[] {
                working => working.SetGlobalDefinitionValues([new("Creatures", 0, "Speed", "122")]),
                working => working.SetGlobalDefinitionValues([new("CrSpells", 0, "Range", "122")]),
                working => working.Paint([new TilePoint(1, 1)], new MapTile(1, 1, 1)) })
            {
                using var context = doc.CreateGlobalVariablesSession(); outside(context.WorkingDocument);
                Reject(() => doc.PrepareGlobalVariables(context)); Exact(doc, imported);
            }
            using (var context = doc.CreateGlobalVariablesSession())
            {
                Reject(() => doc.ResetGlobalVariables(context, "Creatures")); Reject(() => doc.ResetGlobalVariables(context, "Spells", 99));
                var plan = doc.PrepareGlobalVariables(context);
                context.WorkingDocument.SetGlobalDefinitionValues([new("Spells", 0, "ManaCost", "125")]);
                Reject(() => doc.ApplyGlobalVariables(plan)); Exact(doc, imported);
                var current = doc.PrepareGlobalVariables(context);
                doc.SetGlobalDefinitionValues([new("Rooms", 0, "Cost", "126")]); var changed = Snapshot(doc);
                Reject(() => doc.ApplyGlobalVariables(current)); Exact(doc, changed); doc.Undo(); Exact(doc, imported);
                Reject(() => doc.PrepareGlobalVariables(context)); // Undo never revives stale ownership.
            }
            using (var context = doc.CreateGlobalVariablesSession())
            {
                Reject(() => NativeMapDocument.CreateNewMap(new("Foreign", "Foreign", 12, 12)).PrepareGlobalVariables(context));
                doc.Edit("Open live transaction", () => Reject(() => doc.PrepareGlobalVariables(context)));
                context.WorkingDocument.Edit("Open private transaction", () => Reject(() => doc.PrepareGlobalVariables(context)));
                context.Dispose(); Reject(() => doc.ResetGlobalVariables(context, "Doors"));
            }
            Exact(doc, imported);
            Console.WriteLine($"SHARED RESET native={!doc.IsLegacyProject} values=2016 current=5 all=5 hidden-shot-delay private-cancel scope stale foreign transaction");
        }
    }

    public static void OpaqueGuards()
    {
        foreach (var doc in Documents("SharedOpaque"))
        {
            doc.SetHistoryDepth(65535);
            if (doc.IsLegacyProject)
            {
                doc.SetGlobalDefinitionValues([new("Rooms", 0, "Cost", "111")]);
                var room = Row(doc, "Rooms").Record!; var ceiling = room.Fields.Single(field => field.Name == "CeilingHeight");
                doc.SetField(room, ceiling, "123456789");
            }
            else
            {
                foreach (var family in new[] { "Rooms", "Creatures", "CrSpells" })
                { var record = Row(doc, family).Record!; Raw(record.File, record.Offset + 10, (bytes, offset) => bytes[offset] ^= 0x5a); }
            }
            var initial = Snapshot(doc);
            using (var context = doc.CreateGlobalVariablesSession())
            {
                context.WorkingDocument.SetGlobalDefinitionValues([new("Rooms", 1, "Cost", "222"), new("Spells", 0, "ManaCost", "333")]);
                doc.ResetGlobalVariables(context, "Rooms", 0); var plan = doc.PrepareGlobalVariables(context); doc.ApplyGlobalVariables(plan);
                if (doc.IsLegacyProject) Expect(Row(doc, "Rooms").Record!.Read(Row(doc, "Rooms").Record!.Fields.Single(field => field.Name == "CeilingHeight")) == "123456789", "Reset/apply overwrote unused room ceiling.");
                doc.Undo(); Exact(doc, initial);
            }
            using (var context = doc.CreateGlobalVariablesSession())
            {
                var record = Row(context.WorkingDocument, "Rooms").Record!;
                var offset = doc.IsLegacyProject ? record.Fields.Single(field => field.Name == "CeilingHeight").Offset : 10;
                Raw(record.File, record.Offset + offset, (bytes, index) => bytes[index] ^= 0x20);
                Reject(() => doc.PrepareGlobalVariables(context)); Exact(doc, initial);
            }
            using (var context = doc.CreateGlobalVariablesSession())
            {
                var plan = doc.PrepareGlobalVariables(context); var record = Row(context.WorkingDocument, "Rooms").Record!;
                var offset = doc.IsLegacyProject ? record.Fields.Single(field => field.Name == "CeilingHeight").Offset : 10;
                Raw(record.File, record.Offset + offset, (bytes, index) => bytes[index] ^= 0x20);
                Reject(() => doc.ApplyGlobalVariables(plan)); Exact(doc, initial);
            }
            using (var context = doc.CreateGlobalVariablesSession())
            {
                var record = Row(doc, "Rooms").Record!; var original = record.File.Data.Span[record.Offset + 10];
                Raw(record.File, record.Offset + 10, (bytes, index) => bytes[index] ^= 0x20);
                Reject(() => doc.PrepareGlobalVariables(context));
                Raw(record.File, record.Offset + 10, (bytes, index) => bytes[index] = original); Exact(doc, initial);
            }
            Console.WriteLine($"SHARED OPAQUE native={!doc.IsLegacyProject} imported-room-ceiling unedited-creatures bytes-without-revision plan-and-owner-fingerprint");
        }
        Reject(() => NativeMapDocument.Create(12, 12, "Missing").CreateGlobalVariablesSession());
    }
}
