using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Editing;

internal static class CreatureOptionChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Creature option edit changed unrelated/undo bytes."); }
    private static void Reject(Action action) { try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid creature option input accepted."); }
    private static CreatureOptionsDraft Draft(NativeMapDocument document, string group, int index = 0) => document.CreateCreatureOptionsDraft(document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == index), group);
    private static CreatureOptionInput[] Inputs(CreatureOptionsDraft draft) => CreatureOptions.Fields(draft.Group).Select(field => new CreatureOptionInput(field, draft.Display(field))).ToArray();
    private static CreatureOptionInput[] Set(CreatureOptionsDraft draft, params (string Field, string Value)[] edits) => Inputs(draft).Select(input => edits.FirstOrDefault(edit => edit.Field == input.Field) is var found && found.Field is not null ? input with { Value = found.Value } : input).ToArray();
    private static readonly (string Field, int Offset, int Size)[] offsets = [
        ("Res1",0xE0C,1),("Res1Rate",0xE0D,1),("Res2",0xE0E,1),("Res2Rate",0xE0F,1),("Res3",0xE10,1),("Res3Rate",0xE11,1),("Res4",0xE12,1),("Res4Rate",0xE13,1),
        ("SpecialAb1",0xF46,1),("SpecialAb1Data",0x14E9,2),("SpecialAb2",0xF47,1),("SpecialAb2Data",0x14ED,2),("Melee1",0xF42,1),("Melee2",0xF43,1),("Special1",0x1025,1),("Special2",0x1026,1),("Spell",0xF45,1),("GammaEffect",0xD3C,1),("WaddleScale",0xDBC,2),("OscillateScale",0xDC0,2),("WalkScale",0xD3D,1),("CameraRolls",0xEFF,1) ];
    private static void Packed(NativeMapDocument document, CreatureOptionsDraft draft)
    {
        var row = document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == draft.Index).Record!;
        foreach (var (field, offset, size) in offsets.Where(item => CreatureOptions.Fields(draft.Group).Contains(item.Field)))
        {
            var data = row.File.Data.Span.Slice(row.Offset + offset); var value = size == 2 ? (int)BinaryPrimitives.ReadUInt16LittleEndian(data) : data[0];
            if (field == "CameraRolls") value = (value & 0x10) == 0 ? 0 : 1;
            Expect(value.ToString(CultureInfo.InvariantCulture) == draft.Raw(field), "Independent creature option native offset differs: " + field);
        }
    }
    private static void ExpectedNativeEdit(NativeMapDocument document, Dictionary<NativeFile, byte[]> before, CreatureOptionsDraft draft)
    {
        var expected = before.ToDictionary(item => item.Key, item => item.Value.ToArray());
        var row = document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == draft.Index).Record!;
        foreach (var (field, offset, size) in offsets.Where(item => CreatureOptions.Fields(draft.Group).Contains(item.Field)))
        {
            var data = expected[row.File].AsSpan(row.Offset + offset); var value = uint.Parse(draft.Raw(field), CultureInfo.InvariantCulture);
            if (field == "CameraRolls") data[0] = (byte)((data[0] & ~0x10) | (value == 0 ? 0 : 0x10));
            else if (size == 1) data[0] = checked((byte)value);
            else BinaryPrimitives.WriteUInt16LittleEndian(data, checked((ushort)value));
        }
        Exact(document, expected);
    }
    public static void Run()
    {
        string[][] mapped = [["Res1","Res2","Res3","Res4"],["SpecialAb1","SpecialAb2"],["Melee1","Melee2","Special1","Special2","Spell"],["GammaEffect"]];
        int[] counts = [33,11,23,17];
        for (var list = 0; list < mapped.Length; list++) foreach (var field in mapped[list])
        {
            var choices = CreatureOptions.Choices(field); var expected = Enumerable.Range(0, counts[list]).Select(id => id.ToString(CultureInfo.InvariantCulture)).ToArray();
            if (list == 0) expected = ["0","1","2","3","15", .. expected.Skip(4).Where(id => id != "15")];
            Expect(choices.Select(choice => choice.Value).SequenceEqual(expected) && choices.All(choice => choice.English.Length > 0 && choice.Russian.Length > 0 && !choice.English.Contains("sz")), "Wrong bilingual source creature option list: " + field);
            Expect(choices.All(choice => choice.Value != "4294967295"), "Terminating sentinel became an option.");
        }
        Expect(CreatureOptions.Choices("Res1").Single(choice => choice.Value == "16").English == "Unknown source element 0x10", "Selectable source unknown given invented semantics.");
        Expect(CreatureOptions.Choices("Melee1").Single(choice => choice.Value == "15").English == "Dark arrow" && CreatureAttacks.Options("Melee").Single(choice => choice.ID == 15).Name.English == "Scythe", "Possession swipes confused with attack damage catalogue.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2-Options-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory, "basis")); var basis = Snapshot(native);
            var project = NativeMapDocument.CreateNewMap(new("Options", "Options", 20, 20));
            foreach (var document in new[] { native, project })
            {
                var initial = Snapshot(document); var notifications = 0; EventHandler handler = (_, _) => notifications++; document.Changed += handler;
                try
                {
                    foreach (var row in document.GlobalDefinitionRows().Where(row => row.Family == "Creatures")) foreach (var group in CreatureOptions.Groups)
                    {
                        var draft = document.CreateCreatureOptionsDraft(row, group); var unchanged = document.UpdateCreatureOptions(draft, Inputs(draft));
                        Expect(!unchanged.Changed, "Reapplying source options normalized values."); document.ApplyCreatureOptions(unchanged);
                    }
                    Exact(document, initial); Expect(notifications == 0, "Default re-entry created history/global array.");
                    foreach (var fields in mapped) foreach (var choice in CreatureOptions.Choices(fields[0]))
                    {
                        var group = fields[0] == "Res1" ? "Resistances" : "Possession"; var draft = Draft(document, group);
                        var edits = fields.Select(field => (field, choice.Value)).ToArray(); var prepared = document.UpdateCreatureOptions(draft, Set(draft, edits)); var before = notifications;
                        document.ApplyCreatureOptions(prepared); var read = Draft(document, group); var written = Snapshot(document);
                        Expect(fields.All(field => read.Raw(field) == choice.Value) && notifications == before + (prepared.Changed ? 1 : 0), "All catalogue fields not applied atomically.");
                        if (!document.IsLegacyProject) { Packed(document, read); ExpectedNativeEdit(document, initial, read); }
                        document.ApplyCreatureOptions(document.UpdateCreatureOptions(read, Inputs(read))); Exact(document, written);
                        if (prepared.Changed) { document.Undo(); Exact(document, initial); document.Redo(); Exact(document, written); document.Undo(); Exact(document, initial); }
                    }
                    foreach (var group in CreatureOptions.Groups)
                    {
                        var draft = Draft(document, group);
                        var edits = group == "Resistances" ? new (string,string)[] { ("Res1","1"),("Res1Rate","23"),("Res2","0"),("Res2Rate","77"),("Res3","32"),("Res3Rate","100"),("Res4","15"),("Res4Rate","0") }
                            : [("SpecialAb1","10"),("SpecialAb1Data","32767"),("SpecialAb2","0"),("SpecialAb2Data","123"),("Melee1","22"),("Melee2","15"),("Special1","1"),("Special2","2"),("Spell","18"),("GammaEffect","16"),("WaddleScale","1.000244140625"),("OscillateScale","0,000244140625"),("WalkScale","12"),("CameraRolls","1")];
                        var prepared = document.UpdateCreatureOptions(draft, Set(draft, edits)); var before = notifications; document.ApplyCreatureOptions(prepared); var full = Snapshot(document); var read = Draft(document, group);
                        Expect(notifications == before + 1 && (group != "Resistances" || read.Raw("Res2Rate") == "77"), "Group apply/None parameter retention differs.");
                        if (!document.IsLegacyProject) { Packed(document, read); ExpectedNativeEdit(document, initial, read); }
                        var reopened = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory, (document.IsLegacyProject ? "project-" : "native-") + group)));
                        foreach (var field in CreatureOptions.Fields(group)) Expect(Draft(reopened, group).Raw(field) == read.Raw(field), "Saved creature options differ."); Exact(document, full);
                        if (document.IsLegacyProject) { var compiled = document.CompileLegacyProject("Options" + group, native); Packed(compiled, Draft(compiled, group)); foreach (var field in CreatureOptions.Fields(group)) Expect(Draft(compiled, group).Raw(field) == read.Raw(field), "Compiled creature option differs."); Exact(document, full); }
                        Reject(() => document.ApplyCreatureOptions(prepared)); Exact(document, full); document.Undo(); Exact(document, initial);
                        foreach (var field in CreatureOptions.Fields(group).Where(field => CreatureOptions.Choices(field).Count == 0))
                        {
                            var format = CreatureOptions.Format(field, !document.IsLegacyProject);
                            foreach (var bound in new[] { format.Minimum!.Value, format.Maximum!.Value })
                            {
                                var endpoint = document.UpdateCreatureOptions(draft, Set(draft, (field, bound.ToString(CultureInfo.InvariantCulture)))); document.ApplyCreatureOptions(endpoint);
                                Expect(Draft(document, group).Raw(field) == endpoint.Raw(field), "Source option endpoint changed."); if (endpoint.Changed) document.Undo(); Exact(document, initial);
                            }
                            var step = format.Fractional ? 1m / 4096m : 1m;
                            foreach (var invalid in new[] { (format.Minimum.Value - step).ToString(CultureInfo.InvariantCulture), (format.Maximum.Value + step).ToString(CultureInfo.InvariantCulture), "invalid", "1e1", "0x10" })
                                Reject(() => document.UpdateCreatureOptions(draft, Set(draft, (field, invalid))));
                        }
                        Reject(() => document.UpdateCreatureOptions(draft, [])); Reject(() => document.UpdateCreatureOptions(draft, Inputs(draft).Append(Inputs(draft)[0])));
                        Reject(() => document.UpdateCreatureOptions(draft, Set(draft, (CreatureOptions.Fields(group)[0], "254")))); Exact(document, initial);
                    }
                    var unknown = document.IsLegacyProject ? "4294967295" : "255";
                    document.SetGlobalDefinitionValues([new("Creatures",0,"Res1",unknown), new("Creatures",0,"Res1Rate","255"), new("Creatures",0,"SpecialAb1",unknown),new("Creatures",0,"SpecialAb1Data","65535"),new("Creatures",0,"WalkScale","255"),new("Creatures",0,"WaddleScale","65535")]);
                    var imported = Snapshot(document);
                    foreach (var group in CreatureOptions.Groups)
                    {
                        var draft = Draft(document, group); document.ApplyCreatureOptions(document.UpdateCreatureOptions(draft, Inputs(draft))); Exact(document, imported);
                        var field = group == "Resistances" ? "Res1" : "SpecialAb1"; var repaired = document.UpdateCreatureOptions(draft, Set(draft, (field, "1"))); document.ApplyCreatureOptions(repaired);
                        Expect(Draft(document, group).Raw(group == "Resistances" ? "Res1Rate" : "SpecialAb1Data") == (group == "Resistances" ? "255" : "65535"), "Type repair normalized imported parameter."); document.Undo(); Exact(document, imported);
                    }
                    var savedUnknown = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory, document.IsLegacyProject ? "unknown-project" : "unknown-native"))); Expect(Draft(savedUnknown,"Resistances").Raw("Res1") == unknown && Draft(savedUnknown,"Possession").Raw("WaddleScale") == "65535", "Unknown options not retained in saved copy.");
                    if (document.IsLegacyProject) { var compiled = document.CompileLegacyProject("UnknownOptions", native); Expect(Draft(compiled,"Resistances").Raw("Res1") == "255" && Draft(compiled,"Possession").Raw("SpecialAb1") == "255" && Draft(compiled,"Possession").Raw("SpecialAb1Data") == "65535", "Original option casts changed."); }
                    document.Undo(); Exact(document, initial);
                    var pending = document.UpdateCreatureOptions(Draft(document,"Resistances"), Set(Draft(document,"Resistances"), ("Res1","1"),("Res1Rate","50")));
                    document.SetGlobalDefinitionValues([new("Creatures",0,"Speed","123")]); var otherGroup = Snapshot(document); document.ApplyCreatureOptions(pending); document.Undo(); Exact(document, otherGroup); document.Undo(); Exact(document, initial);
                    var foreign = NativeMapDocument.CreateNewMap(new("Foreign","Foreign",20,20)); Reject(() => foreign.ApplyCreatureOptions(Draft(document,"Resistances"))); Reject(() => Draft(document,"Attacks")); Exact(document, initial);
                }
                finally { document.Changed -= handler; }
            }
            Exact(native, basis); var missing = NativeMapDocument.Create(20,20,"Missing"); foreach (var group in CreatureOptions.Groups) { var draft = Draft(missing,group); Expect(!draft.Editable && draft.Raw(CreatureOptions.Fields(group)[0]).Length == 0,"Missing options inferred from defaults."); Reject(() => missing.ApplyCreatureOptions(draft)); }
        }
        finally { Directory.Delete(directory, true); }
    }
}
