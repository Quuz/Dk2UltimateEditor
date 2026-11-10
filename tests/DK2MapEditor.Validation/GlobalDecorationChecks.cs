using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Editing;

internal static class GlobalDecorationChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action) { try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid decoration accepted."); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Decoration changed unrelated/undo bytes."); }
    private static GlobalDecorationsDraft Draft(NativeMapDocument document, string family, int? index = null) => document.CreateGlobalDecorationsDraft(document.GlobalDefinitionRows().Single(row => row.Family == family && row.Index == (index ?? (family == "Terrains" ? 2 : 0))));
    private static GlobalDecorationInput[] Inputs(GlobalDecorationsDraft draft) => GlobalDefinitionDecorations.Fields(draft.Family).Select(field => new GlobalDecorationInput(field, draft.Raw(field))).ToArray();
    private static GlobalDecorationInput[] Set(GlobalDecorationsDraft draft, params (string Field, string Value)[] changes) => Inputs(draft).Select(input => changes.FirstOrDefault(change => change.Field == input.Field) is var found && found.Field is not null ? input with { Value = found.Value } : input).ToArray();
    private static readonly (string Family, string Field, int Offset, int Size)[] fields = [
        ("Terrains","MaxHealthEffect",0x1A4,2),("Terrains","DestroyedEffect",0x1A6,2),
        ("Rooms","BattleEffect",0x37A,2),("Rooms","Effects[0]",0x382,2),("Rooms","Effects[1]",0x384,2),("Rooms","Effects[2]",0x386,2),("Rooms","Effects[3]",0x388,2),("Rooms","Effects[4]",0x38A,2),("Rooms","Effects[5]",0x38C,2),
        ("Rooms","Objects[0]",0x39A,1),("Rooms","Objects[1]",0x39B,1),("Rooms","Objects[2]",0x39C,1),("Rooms","Objects[3]",0x39D,1),("Rooms","Objects[4]",0x39E,1),("Rooms","Objects[5]",0x39F,1),
        ("Doors","DeathEffect",0x232,2),("Traps","DeathEffect",0x1F8,2),("Traps","PowerlessEffect",0x1F4,2),("Traps","IdleEffect",0x1F6,2),
        ("Creatures","ArmourType",0xF76,1),("Creatures","SlapEffect",0xF3E,2),("Creatures","HealthEffect",0xCFD,2),("Creatures","EntranceEffect",0xF36,2),("Creatures","DeathEffect",0xF40,2) ];
    private static void Packed(NativeMapDocument document, GlobalDecorationsDraft draft, Dictionary<NativeFile, byte[]>? before = null)
    {
        var record = document.GlobalDefinitionRows().Single(row => row.Family == draft.Family && row.Index == draft.Index).Record!;
        var expected = before?.ToDictionary(item => item.Key, item => item.Value.ToArray());
        foreach (var (_, field, offset, size) in fields.Where(item => item.Family == draft.Family))
        {
            var bytes = record.File.Data.Span.Slice(record.Offset + offset); var actual = size == 1 ? bytes[0] : BinaryPrimitives.ReadUInt16LittleEndian(bytes);
            Expect(actual.ToString(CultureInfo.InvariantCulture) == draft.Raw(field), "Literal decoration offset differs: " + field);
            if (expected is not null)
            {
                var target = expected[record.File].AsSpan(record.Offset + offset); var number = uint.Parse(draft.Raw(field), CultureInfo.InvariantCulture);
                if (size == 1) target[0] = checked((byte)number); else BinaryPrimitives.WriteUInt16LittleEndian(target, checked((ushort)number));
            }
        }
        if (expected is not null) Exact(document, expected);
    }
    public static void Run()
    {
        Expect(EffectCatalogue.Choices.Select(choice => uint.Parse(choice.Value, CultureInfo.InvariantCulture)).SequenceEqual(Enumerable.Range(0,371).Select(id => (uint)id)), "Effect zero/order/terminator incorrect.");
        Expect(EffectCatalogue.Choices.All(choice => choice.English.Length > 0 && choice.Russian.Length > 0 && !choice.English.Contains("szEffect")), "Incomplete effect translations.");
        int[] empty = [0x37,0x7F,0x9C,0xE6,0x117,0x139,0x162,0x163,0x164,0x165,0x166,0x167];
        Expect(EffectCatalogue.Choices.Where(choice => choice.English == "Nothing (source label)").Select(choice => int.Parse(choice.Value)).SequenceEqual(empty), "Source Nothing entries lost or conflated with None.");
        Expect(EffectCatalogue.Choices.Where(choice => choice.English.Contains("[1.7]")).Select(choice => int.Parse(choice.Value)).SequenceEqual(Enumerable.Range(0x16D,6)), "Source version annotations lost.");
        uint[] masks = [3,7,0x90F,7,0xF03,1,0x3F03,0xF01,0x1F,0x101,0x900,0x1F1F,0x300,0x703,0,0x303,7,0,2,0,0,0,0,0];
        for (var index = 0; index < 24; index++) foreach (var prefix in new[] { "Effects", "Objects" }) for (var slot = 0; slot < 6; slot++)
            Expect(GlobalDefinitionDecorations.Enabled("Rooms",index,$"{prefix}[{slot}]") == ((masks[index] & (1u << (slot + (prefix == "Objects" ? 8 : 0)))) != 0), "Source room slot mask differs.");
        Expect(GlobalDefinitionDecorations.Choices("Rooms","Objects[0]").Select(choice => choice.Value).SequenceEqual(new[] { "0" }.Concat(LegacyCompilerRules.Elements("Objects").Select(entry => entry.ID.ToString(CultureInfo.InvariantCulture)))), "Room object catalogue lost None/order.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2-Decorations-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory,"basis")); var project = NativeMapDocument.CreateNewMap(new("Decorations","Decorations",20,20));
            foreach (var document in new[] { native, project })
            {
                var initial = Snapshot(document); var notifications = 0; EventHandler handler = (_,_) => notifications++; document.Changed += handler;
                try
                {
                    foreach (var row in document.GlobalDefinitionRows().Where(row => GlobalDefinitionDecorations.Families.Contains(row.Family)))
                    {
                        var draft = document.CreateGlobalDecorationsDraft(row); document.ApplyGlobalDecorations(document.UpdateGlobalDecorations(draft,Inputs(draft)));
                    }
                    Exact(document,initial); Expect(notifications == 0,"Default decoration no-op created array/history.");
                    foreach (var (family, field, _, _) in fields)
                    {
                        var indices = document.GlobalDefinitionRows().Where(row => row.Family == family && GlobalDefinitionDecorations.Enabled(family,row.Index,field)).Select(row => row.Index).ToArray();
                        if (indices.Length == 0) continue; // Source room Effects[5] is never active, not a writable invented slot.
                        foreach (var choice in GlobalDefinitionDecorations.Choices(family,field))
                        {
                            var draft = Draft(document,family,indices[0]); var prepared = document.UpdateGlobalDecorations(draft,Set(draft,(field,choice.Value))); var prior = notifications;
                            document.ApplyGlobalDecorations(prepared); var read = Draft(document,family,indices[0]); Expect(read.Raw(field) == choice.Value && notifications == prior + (prepared.Changed ? 1 : 0),"Choice mapped to wrong field/transaction.");
                            if (!document.IsLegacyProject) Packed(document,read,initial);
                            document.ApplyGlobalDecorations(document.UpdateGlobalDecorations(read,Inputs(read))); Expect(notifications == prior + (prepared.Changed ? 1 : 0),"No-op choice emitted notification.");
                            if (prepared.Changed) document.Undo(); Exact(document,initial);
                        }
                    }
                    foreach (var family in GlobalDefinitionDecorations.Families)
                    {
                        var index = family == "Rooms" ? 6 : family == "Terrains" ? 2 : 0; var draft = Draft(document,family,index);
                        var changes = GlobalDefinitionDecorations.Fields(family).Where(field => GlobalDefinitionDecorations.Enabled(family,index,field)).Select(field => (field,GlobalDefinitionDecorations.Choices(family,field)[^1].Value)).ToArray();
                        var prepared = document.UpdateGlobalDecorations(draft,Set(draft,changes)); var prior = notifications; document.ApplyGlobalDecorations(prepared); var after = Snapshot(document); var read = Draft(document,family,index);
                        Expect(prepared.Changed && notifications == prior + 1,"Whole decoration subset not atomic."); if (!document.IsLegacyProject) Packed(document,read,initial);
                        var reopened = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,(document.IsLegacyProject ? "project-" : "native-") + family)));
                        foreach (var field in GlobalDefinitionDecorations.Fields(family)) Expect(Draft(reopened,family,index).Raw(field) == read.Raw(field),"Saved decoration differs."); Exact(document,after);
                        if (document.IsLegacyProject) { var compiled = document.CompileLegacyProject("Decor" + family,native); Packed(compiled,Draft(compiled,family,index)); foreach (var field in GlobalDefinitionDecorations.Fields(family)) Expect(Draft(compiled,family,index).Raw(field) == read.Raw(field),"Compiled decoration differs."); Exact(document,after); }
                        Reject(() => document.ApplyGlobalDecorations(prepared)); Exact(document,after); document.Undo(); Exact(document,initial); document.Redo(); Exact(document,after); document.Undo(); Exact(document,initial);
                        Reject(() => document.UpdateGlobalDecorations(draft,[])); Reject(() => document.UpdateGlobalDecorations(draft,Inputs(draft).Append(Inputs(draft)[0])));
                        var fieldName = GlobalDefinitionDecorations.Fields(family)[0]; foreach (var bad in new[] { "4294967295","-1","371","65536","0x1","1.5","1e1","bad" }) Reject(() => document.UpdateGlobalDecorations(draft,Set(draft,(fieldName,bad)))); Exact(document,initial);
                    }
                    for (var index = 0; index < 24; index++) foreach (var field in GlobalDefinitionDecorations.Fields("Rooms").Where(field => !GlobalDefinitionDecorations.Enabled("Rooms",index,field)))
                    {
                        var draft = Draft(document,"Rooms",index); var replacement = draft.Raw(field) == "0" ? "1" : "0";
                        Reject(() => document.UpdateGlobalDecorations(draft,Set(draft,("BattleEffect","370"),(field,replacement)))); Exact(document,initial);
                    }
                    var unknownChanges = fields.Select(item => new GlobalDefinitionChange(item.Family,item.Family == "Terrains" ? 2 : 0,item.Field,document.IsLegacyProject ? "4294967295" : item.Size == 1 ? "255" : "65535")).ToArray();
                    document.SetGlobalDefinitionValues(unknownChanges); var imported = Snapshot(document);
                    foreach (var family in GlobalDefinitionDecorations.Families)
                    {
                        var draft = Draft(document,family); document.ApplyGlobalDecorations(document.UpdateGlobalDecorations(draft,Inputs(draft))); Exact(document,imported);
                        var field = GlobalDefinitionDecorations.Fields(family).First(name => GlobalDefinitionDecorations.Enabled(family,draft.Index,name)); document.ApplyGlobalDecorations(document.UpdateGlobalDecorations(draft,Set(draft,(field,"0"))));
                        foreach (var untouched in GlobalDefinitionDecorations.Fields(family).Where(name => name != field)) Expect(Draft(document,family).Raw(untouched) == draft.Raw(untouched),"Explicit repair normalized another imported slot."); document.Undo(); Exact(document,imported);
                    }
                    var reopenedUnknown = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,document.IsLegacyProject ? "unknown-project" : "unknown-native")));
                    foreach (var change in unknownChanges) Expect(Draft(reopenedUnknown,change.Family).Raw(change.Field) == change.Value,"Unknown decoration lost on save.");
                    if (document.IsLegacyProject) { var compiled = document.CompileLegacyProject("UnknownDecorations",native); foreach (var (family,field,_,size) in fields) Expect(Draft(compiled,family).Raw(field) == (size == 1 ? "255" : "65535"),"Original decoration cast differs."); Exact(document,imported); }
                    document.Undo(); Exact(document,initial);
                    var pending = document.UpdateGlobalDecorations(Draft(document,"Doors"),Set(Draft(document,"Doors"),("DeathEffect","370")));
                    document.SetGlobalDefinitionValues([new("Doors",0,"Material","7")]); var other = Snapshot(document); document.ApplyGlobalDecorations(pending); Expect(document.CreateGlobalRulesDraft(document.GlobalDefinitionRows().Single(row => row.Family == "Doors" && row.Index == 0)).Raw("Material") == "7","Decorations overwrote independent rules."); document.Undo(); Exact(document,other); document.Undo(); Exact(document,initial);
                    Reject(() => NativeMapDocument.CreateNewMap(new("Foreign","Foreign",20,20)).ApplyGlobalDecorations(Draft(document,"Doors"))); Reject(() => Draft(document,"Spells")); Exact(document,initial);
                }
                finally { document.Changed -= handler; }
            }
            var missing = NativeMapDocument.Create(20,20,"Missing"); foreach (var family in GlobalDefinitionDecorations.Families) { var draft = Draft(missing,family); Expect(!draft.Editable && draft.Raw(GlobalDefinitionDecorations.Fields(family)[0]).Length == 0,"Missing decoration default inferred."); Reject(() => missing.ApplyGlobalDecorations(draft)); }
        }
        finally { Directory.Delete(directory,true); }
    }
}
