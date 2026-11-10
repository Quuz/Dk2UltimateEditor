using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class CompiledOwnerOrderChecks
{
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        foreach (var shared in new[] { false, true })
        {
            const string name = "DK2Preview_6a88240eaf814f78";
            var project = NativeMapDocument.CreateNewMap(new(Width: 20, Height: 20, DisplayName: name, FileName: name));
            project.Paint(MapDrawing.Rectangle(new(2, 2, 5, 5), true), new(14, 3, 1));
            var source = new List<NativeRecord>();
            for (var copy = 0; copy < 2; copy++)
                foreach (var kind in new[] { "Creature", "Door", "Object", "MagicalObject" })
                {
                    var record = project.AddLegacyRecord(kind, new(2 + source.Count, 10)); source.Add(record);
                    if (kind == "MagicalObject")
                        project.SetField(record, record.Fields.Single(field => field.Name == "id"), ValueCatalogues.ForTemplate("MagicalObject").First().Value);
                    if (shared)
                    {
                        var field = record.Fields.Single(field => field.Name == "uniqueid");
                        project.Edit("Imported cross-category identity fixture", () =>
                        {
                            Require(MemoryMarshal.TryGetArray(record.File.Data, out var buffer), "Fixture buffer unavailable.");
                            field.Encode((42 + copy).ToString()).CopyTo(buffer.Array!, buffer.Offset + record.Offset + field.Offset);
                        });
                    }
                }
            var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
            if (shared)
            {
                // Existing global identity ambiguity deliberately blocks creating
                // a new binding; typed imported links are a separate I18 route.
                var unchanged = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
                foreach (var record in source)
                {
                    var owner = project.ScriptOwners().Single(owner => owner.Record.File == record.File && owner.Record.Offset == record.Offset);
                    var refused = false;
                    try { project.EnsureScriptScope(owner); } catch (InvalidOperationException) { refused = true; }
                    Require(refused, "Creating a binding guessed an owner with shared global identity.");
                    foreach (var (file, bytes) in unchanged) Require(file.Data.Span.SequenceEqual(bytes), "Refused ambiguous binding changed source bytes.");
                }
                continue;
            }
            foreach (var record in source)
            {
                var owner = project.ScriptOwners().Single(owner => owner.Record.File == record.File && owner.Record.Offset == record.Offset);
                var root = project.EnsureScriptScope(owner); project.InsertScriptEvent(root.Owner, root.Field, timer);
            }
            var before = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var expected = ScriptGraph.Build(project).Roots.Where(root => root.Target != 0).Select(root => (root.Scope, root.Target)).Order().ToArray();
            var native = project.CompileLegacyProject(name, NativeMapDocument.Create(20, 20, name));
            var actual = ScriptGraph.Build(native).Roots.Where(root => root.Target != 0).ToArray();
            Require(actual.Select(root => (root.Scope, root.Target)).Order().SequenceEqual(expected), "Compiler created duplicate/orphan owner roots after category reordering. Shared=" + shared
                + " Expected=" + string.Join(";", expected) + " Actual=" + string.Join(";", actual.Select(root => (root.Scope, root.Target, root.Owner.Category))));
            Require(actual.All(root => root.Owner.Category != "Working.OwnerScope"), "A live source owner became a virtual orphan.");
            var converted = native.PrepareProjectConversion();
            Require(!converted.HasBlockingIssues, "Verified compiled owners cannot be converted back into DKM.");
            Require(ScriptGraph.Build(converted.Project).Roots.Where(root => root.Target != 0).Select(root => (root.Scope, root.Target)).Order().SequenceEqual(expected), "Native-to-DKM changed the compiled owner graph.");
            foreach (var (file, bytes) in before) Require(file.Data.Span.SequenceEqual(bytes), "Compilation changed the source project.");
        }
    }
}
