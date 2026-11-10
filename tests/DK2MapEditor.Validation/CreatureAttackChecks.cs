using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Editing;

internal static class CreatureAttackChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document,Dictionary<NativeFile,byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]),"Attack edit changed unrelated/undo bytes."); }
    private static void Reject(Action action) { try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid attack input accepted."); }
    private static CreatureAttacksDraft Draft(NativeMapDocument document,int index = 0) => document.CreateCreatureAttacksDraft(document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == index));
    private static CreatureAttackInput[] Inputs(CreatureAttacksDraft draft,string slot) => CreatureAttacks.Parameters(slot).Select(field => new CreatureAttackInput(field,draft.Display(field))).ToArray();
    private static CreatureAttackInput[] Samples(string slot) => CreatureAttacks.Parameters(slot).Select(field => new CreatureAttackInput(field,field switch {
        "RechargeTime" => "2.25", "Range" => "1.125", "Dammage" => "32767", "Delay" => "0.5", _ when field.EndsWith("Level") => "10", _ when field.EndsWith("Delay") => "1.000244140625", _ when field.EndsWith('X') => "-1.25", _ when field.EndsWith('Y') => "0.000244140625", _ => "2.5" })).ToArray();
    private static readonly (string Field,int Offset,int Size)[] offsets = [
        ("Shot1ID",0xDDA,1),("Shot1Delay",0xDD4,4),("Shot1Level",0xDDB,1),("Shot1X",0xDC4,4),("Shot1Y",0xDC8,4),("Shot1Z",0xDCC,4),
        ("Shot2ID",0xDF2,1),("Shot2Delay",0xDEC,4),("Shot2Level",0xDF3,1),("Shot2X",0xDDC,4),("Shot2Y",0xDE0,4),("Shot2Z",0xDE4,4),
        ("Shot3ID",0xE0A,1),("Shot3Delay",0xE04,4),("Shot3Level",0xE0B,1),("Shot3X",0xDF4,4),("Shot3Y",0xDF8,4),("Shot3Z",0xDFC,4),
        ("MeleeID",0xF50,1),("RechargeTime",0xEF8,4),("Range",0xEEC,4),("Dammage",0xF14,2),("Delay",0xCF9,4) ];
    private static void Packed(NativeMapDocument document,CreatureAttacksDraft draft)
    {
        var row = document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == draft.Index).Record!;
        foreach (var (field,offset,size) in offsets)
        {
            var bytes = row.File.Data.Span.Slice(row.Offset + offset); var actual = size == 1 ? bytes[0] : size == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadInt32LittleEndian(bytes);
            Expect(actual.ToString(CultureInfo.InvariantCulture) == draft.Raw(field),"Independent native attack offset/cast differs: " + field);
        }
    }
    public static void Run()
    {
        foreach (var slot in CreatureAttacks.Slots)
        {
            var options = CreatureAttacks.Options(slot); uint[] expected = slot == "Melee" ? [1,2,3,15, .. Enumerable.Range(4,38).Where(id => id != 15).Select(id => (uint)id)] : Enumerable.Range(0,42).Select(id => (uint)id).ToArray();
            Expect(options.Select(option => option.ID).SequenceEqual(expected) && options.All(option => !option.Name.English.Contains("sz") && option.Name.Russian.Length > 0),"Wrong named source spell/melee catalogue: " + slot);
        }
        Expect(CreatureAttacks.Options("Melee").Take(4).Select(option => option.Name.English).SequenceEqual(new[] { "Blade","Point","Body","Scythe" }) && CreatureAttacks.Options("Shot1").Single(option => option.ID == 15).Name.English == "Protection","Contextual melee names confused with creature spells.");
        var directory = Path.Combine(Path.GetTempPath(),"DK2-Attacks-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory,"basis")); var basis = Snapshot(native); var project = NativeMapDocument.CreateNewMap(new("Attacks","Attacks",20,20));
            foreach (var document in new[] { native,project })
            {
                var initial = Snapshot(document); var notifications = 0; EventHandler handler = (_,_) => notifications++; document.Changed += handler;
                try
                {
                    foreach (var row in document.GlobalDefinitionRows().Where(row => row.Family == "Creatures"))
                    {
                        var draft = document.CreateCreatureAttacksDraft(row);
                        foreach (var slot in CreatureAttacks.Slots) draft = document.UpdateCreatureAttack(draft,slot,draft.Raw(CreatureAttacks.TypeField(slot)),Inputs(draft,slot));
                        Expect(!draft.Changed,"Reapplying source attack defaults normalized bits."); document.ApplyCreatureAttacks(draft);
                    }
                    Exact(document,initial); Expect(notifications == 0,"Default-backed/no-op attack editing created history.");
                    foreach (var slot in CreatureAttacks.Slots)
                    {
                        foreach (var option in CreatureAttacks.Options(slot))
                        {
                            var original = Draft(document); var edited = document.UpdateCreatureAttack(original,slot,option.Name.Value,Samples(slot)); Exact(document,initial);
                            var prior = notifications; document.ApplyCreatureAttacks(edited); Expect(notifications == prior + (edited.Changed ? 1 : 0),"Attack batch not one atomic notification.");
                            var read = Draft(document); var written = Snapshot(document);
                            Expect(read.Raw(CreatureAttacks.TypeField(slot)) == option.Name.Value,"Attack type applied to wrong slot.");
                            foreach (var field in CreatureAttacks.Parameters(slot))
                            {
                                var expected = field switch {
                                    "RechargeTime" => "9216", "Range" => "4608", "Dammage" => "32767", "Delay" => "2048", _ when field.EndsWith("Level") => "10", _ when field.EndsWith("Delay") => "4097", _ when field.EndsWith('X') => "-5120", _ when field.EndsWith('Y') => "1", _ => "10240" };
                                Expect(read.Raw(field) == expected,"Wrong source fixed-point/ordinary attack encoding: " + field);
                            }
                            if (!document.IsLegacyProject) Packed(document,read);
                            var unchanged = document.UpdateCreatureAttack(read,slot,option.Name.Value,Inputs(read,slot)); prior = notifications; document.ApplyCreatureAttacks(unchanged); Exact(document,written); Expect(prior == notifications,"Attack no-op added history.");
                            if (edited.Changed) { document.Undo(); Exact(document,initial); document.Redo(); Exact(document,written); document.Undo(); Exact(document,initial); }
                        }
                    }
                    var all = Draft(document); foreach (var slot in CreatureAttacks.Slots) all = document.UpdateCreatureAttack(all,slot,slot == "Melee" ? "15" : "41",Samples(slot));
                    var beforeAll = notifications; document.ApplyCreatureAttacks(all); var allBytes = Snapshot(document); Expect(notifications == beforeAll + 1,"All four attacks not applied together.");
                    var readAll = Draft(document); if (!document.IsLegacyProject) Packed(document,readAll);
                    Reject(() => document.ApplyCreatureAttacks(all)); Exact(document,allBytes);
                    var saved = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,document.IsLegacyProject ? "project" : "native")));
                    foreach (var (field,_,_) in offsets) Expect(Draft(saved).Raw(field) == readAll.Raw(field),"Attack values changed after save/reopen.");
                    if (document.IsLegacyProject) { var compiled = document.CompileLegacyProject("AttacksCompiled",native); Packed(compiled,Draft(compiled)); foreach (var (field,_,_) in offsets) Expect(Draft(compiled).Raw(field) == readAll.Raw(field),"Compiled attack differs from original assignment."); Exact(document,allBytes); }
                    document.Undo(); Exact(document,initial);
                    foreach (var slot in CreatureAttacks.Slots)
                    {
                        var draft = Draft(document); var type = slot == "Melee" ? "1" : "1";
                        foreach (var field in CreatureAttacks.Parameters(slot))
                        {
                            var format = CreatureAttacks.Format(field,!document.IsLegacyProject);
                            foreach (var endpoint in new[] { format.Minimum!.Value,format.Maximum!.Value })
                            {
                                var inputs = Samples(slot).Select(input => input.Field == field ? input with { Value = endpoint.ToString(CultureInfo.InvariantCulture) } : input).ToArray();
                                var edge = document.UpdateCreatureAttack(draft,slot,type,inputs); document.ApplyCreatureAttacks(edge);
                                Expect(Draft(document).Display(field) == GlobalDefinitionNumbers.Display("Creatures",field,edge.Raw(field)),"Attack endpoint round-trip differs."); document.Undo(); Exact(document,initial);
                            }
                            var step = format.Fractional ? 1m/4096 : 1;
                            foreach (var invalid in new[] { (format.Minimum!.Value-step).ToString(CultureInfo.InvariantCulture),(format.Maximum!.Value+step).ToString(CultureInfo.InvariantCulture),"not a number","0x10" })
                            { Reject(() => document.UpdateCreatureAttack(draft,slot,type,Samples(slot).Select(input => input.Field == field ? input with { Value = invalid } : input))); Exact(document,initial); }
                        }
                        Reject(() => document.UpdateCreatureAttack(draft,slot,"999",Samples(slot))); Reject(() => document.UpdateCreatureAttack(draft,slot,"1",[]));
                        Reject(() => document.UpdateCreatureAttack(draft,slot,"1",Samples(slot).Append(Samples(slot)[0]))); Exact(document,initial);
                    }
                    // Disabling a previously edited slot retains its pending
                    // parameters, as the original SaveChangesCr copies them.
                    var active = document.UpdateCreatureAttack(Draft(document),"Shot1","1",Samples("Shot1"));
                    var dormant = document.UpdateCreatureAttack(active,"Shot1","0",Inputs(active,"Shot1"));
                    document.ApplyCreatureAttacks(dormant); var dormantBytes = Snapshot(document); var dormantRead = Draft(document);
                    Expect(dormantRead.Raw("Shot1ID") == "0" && dormantRead.Raw("Shot1Delay") == "4097" && dormantRead.Raw("Shot1X") == "-5120","Choosing None discarded pending parameters.");
                    document.ApplyCreatureAttacks(document.UpdateCreatureAttack(dormantRead,"Shot1","0",Inputs(dormantRead,"Shot1"))); Exact(document,dormantBytes);
                    Reject(() => document.UpdateCreatureAttack(dormantRead,"Shot1","0",CreatureAttacks.Parameters("Shot1").Select(field => new CreatureAttackInput(field,"invalid")))); Exact(document,dormantBytes);
                    if (dormant.Changed) { document.Undo(); Exact(document,initial); }
                    // Unknown BYTE/UINT types and imported ordinary values are
                    // retained, including a negative raw unsigned 20.12 alias.
                    var unknown = document.IsLegacyProject ? "4294967295" : "255"; var level = document.IsLegacyProject ? "-1" : "255";
                    document.SetGlobalDefinitionValues([new("Creatures",0,"Shot1ID",unknown),new("Creatures",0,"Shot1Level",level),new("Creatures",0,"Shot1Delay","-1"),new("Creatures",0,"MeleeID",unknown),new("Creatures",0,"Dammage","65535")]);
                    var imported = Snapshot(document); var importedDraft = Draft(document); foreach (var slot in new[] { "Shot1","Melee" }) importedDraft = document.UpdateCreatureAttack(importedDraft,slot,unknown,Inputs(importedDraft,slot)); document.ApplyCreatureAttacks(importedDraft); Exact(document,imported);
                    var importedCopy = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,document.IsLegacyProject ? "unknown-project" : "unknown-native"))); Expect(Draft(importedCopy).Raw("Shot1ID") == unknown && Draft(importedCopy).Raw("Shot1Level") == level && Draft(importedCopy).Raw("Shot1Delay") == "-1", "Unknown attack changed after save/reopen.");
                    if (document.IsLegacyProject) { var compiled = document.CompileLegacyProject("UnknownAttacks",native); Expect(Draft(compiled).Raw("Shot1ID") == "255" && Draft(compiled).Raw("Shot1Level") == "255" && Draft(compiled).Raw("Shot1Delay") == "-1" && Draft(compiled).Raw("Dammage") == "65535","Original attack BYTE/WORD/LONG casts changed."); Exact(document,imported); }
                    document.Undo(); Exact(document,initial);
                    var pending = document.UpdateCreatureAttack(Draft(document),"Shot1","1",Samples("Shot1")); document.SetGlobalDefinitionValues([new("Creatures",0,"Speed","123")]); var unrelated = Snapshot(document);
                    document.ApplyCreatureAttacks(pending); Expect(document.GlobalDefinitionValues(document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == 0)).Single(value => value.Name == "Speed").Value == "123","Attack apply overwrote a different source group."); document.Undo(); Exact(document,unrelated); document.Undo(); Exact(document,initial);
                    var foreign = NativeMapDocument.CreateNewMap(new("Foreign","Foreign",20,20)); Reject(() => foreign.ApplyCreatureAttacks(Draft(document))); Reject(() => document.UpdateCreatureAttack(Draft(document),"Unknown","1",[])); Exact(document,initial);
                }
                finally { document.Changed -= handler; }
            }
            Exact(native,basis); var missing = NativeMapDocument.Create(20,20,"Missing"); var readOnly = Draft(missing); Expect(!readOnly.Editable && readOnly.Raw("Shot1ID").Length == 0,"Missing attacks inferred from defaults."); Reject(() => missing.ApplyCreatureAttacks(readOnly));
        }
        finally { Directory.Delete(directory,true); }
    }
}
