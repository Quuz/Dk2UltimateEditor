using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Editing;

internal static class GlobalRuleChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Rule edit changed unrelated or undo bytes."); }
    private static void Reject(Action action) { try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid named rule accepted."); }
    private static GlobalRulesDraft Draft(NativeMapDocument document, string family, int index = 0) => document.CreateGlobalRulesDraft(document.GlobalDefinitionRows().Single(row => row.Family == family && row.Index == index));
    private static GlobalRuleInput[] Inputs(GlobalRulesDraft draft) => GlobalDefinitionRules.Fields(draft.Family).Select(field => new GlobalRuleInput(field, draft.Raw(field))).ToArray();
    private static GlobalRuleInput[] Set(GlobalRulesDraft draft, params (string Field, string Value)[] edits) => Inputs(draft).Select(input => edits.FirstOrDefault(edit => edit.Field == input.Field) is var found && found.Field is not null ? input with { Value = found.Value } : input).ToArray();
    private static readonly (string Family, string Field, int Offset, int Size, uint[] IDs)[] fields = [
        ("Doors","Material",0x226,1,[0,1,2,3,4,5,6,7]),("Doors","Crate",0x246,1,[0,52,53,54,55,56,57]),("Doors","Key",0x247,1,[0,17,104,105,106,107,137]),
        ("Traps","Material",0x22F,1,[0,1,2,3,4,5,6,7]),("Traps","Crate",0x20E,1,[0,5,36,37,38,39,92,40,41,42,43,44,138]),("Traps","TriggerType",0x20B,1,[1,2,3]),("Traps","TriggerData",0x1DC,2,[]),
        ("Spells","HandAnim",0x194,1,[0,1]),("Spells","NoGoHandAnim",0x195,1,[0,1]),("Spells","CastRule",0xEF,1,[1,2,3,4,5]),("Spells","TargetRule",0xDA,1,[1,2,3,4,5,6]) ];
    private static void Packed(NativeMapDocument document, GlobalRulesDraft draft, Dictionary<NativeFile,byte[]>? before = null)
    {
        var row = document.GlobalDefinitionRows().Single(row => row.Family == draft.Family && row.Index == draft.Index).Record!;
        var expected = before?.ToDictionary(item => item.Key, item => item.Value.ToArray());
        foreach (var (_, field, offset, size, _) in fields.Where(item => item.Family == draft.Family))
        {
            var data = row.File.Data.Span.Slice(row.Offset + offset); var value = size == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(data) : data[0];
            Expect(value.ToString(CultureInfo.InvariantCulture) == draft.Raw(field), "Literal named-rule offset differs: " + draft.Family + "." + field);
            if (expected is not null)
            {
                var bytes = expected[row.File].AsSpan(row.Offset + offset); var number = uint.Parse(draft.Raw(field), CultureInfo.InvariantCulture);
                if (size == 1) bytes[0] = checked((byte)number); else BinaryPrimitives.WriteUInt16LittleEndian(bytes, checked((ushort)number));
            }
        }
        if (expected is not null) Exact(document, expected);
    }
    public static void Run()
    {
        foreach (var (family, field, _, _, ids) in fields)
        {
            var choices = GlobalDefinitionRules.Choices(family, field);
            Expect(choices.Select(choice => uint.Parse(choice.Value, CultureInfo.InvariantCulture)).SequenceEqual(ids) && choices.All(choice => choice.English.Length > 0 && choice.Russian.Length > 0 && !choice.English.Contains("sz")), "Wrong bilingual named-rule list: " + family + "." + field);
        }
        Expect(GlobalDefinitionRules.Choices("Doors","Material").Single(choice => choice.Value == "7").English == "Glass" && GlobalDefinitionRules.Choices("Doors","Key")[0].English == "No key", "Contextual material/key names wrong.");
        Expect(GlobalDefinitionRules.Choices("Spells","CastRule").All(choice => choice.Value != "0") && GlobalDefinitionRules.Choices("Spells","TargetRule").All(choice => choice.Value != "0") && GlobalDefinitionRules.Choices("Traps","TriggerType").All(choice => choice.Value != "0"), "Zero terminating sentinel became None.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2-Rules-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var native = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory,"basis")); var basis = Snapshot(native); var project = NativeMapDocument.CreateNewMap(new("Rules","Rules",20,20));
            foreach (var document in new[] { native, project })
            {
                var initial = Snapshot(document); var notifications = 0; EventHandler handler = (_,_) => notifications++; document.Changed += handler;
                try
                {
                    foreach (var row in document.GlobalDefinitionRows().Where(row => GlobalDefinitionRules.Families.Contains(row.Family)))
                    {
                        var draft = document.CreateGlobalRulesDraft(row); var unchanged = document.UpdateGlobalRules(draft, Inputs(draft)); Expect(!unchanged.Changed,"Default rule re-entry normalized bytes."); document.ApplyGlobalRules(unchanged);
                    }
                    Exact(document,initial); Expect(notifications == 0,"Default-backed no-op created override/history.");
                    foreach (var (family, field, _, _, ids) in fields.Where(item => item.IDs.Length > 0)) foreach (var id in ids)
                    {
                        var draft = Draft(document,family); var prepared = document.UpdateGlobalRules(draft, Set(draft,(field,id.ToString(CultureInfo.InvariantCulture)))); var before = notifications; document.ApplyGlobalRules(prepared);
                        var read = Draft(document,family); var changed = Snapshot(document); Expect(read.Raw(field) == id.ToString(CultureInfo.InvariantCulture) && notifications == before + (prepared.Changed ? 1 : 0),"Rule applied to wrong field/history.");
                        if (!document.IsLegacyProject) Packed(document,read,initial);
                        document.ApplyGlobalRules(document.UpdateGlobalRules(read,Inputs(read))); Exact(document,changed);
                        if (prepared.Changed) { document.Undo(); Exact(document,initial); document.Redo(); Exact(document,changed); document.Undo(); Exact(document,initial); }
                    }
                    foreach (var family in GlobalDefinitionRules.Families)
                    {
                        var draft = Draft(document,family); (string,string)[] edits = family switch {
                            "Doors" => [("Material","7"),("Crate","57"),("Key","137")], "Traps" => [("Material","3"),("Crate","138"),("TriggerType","3"),("TriggerData","32767")],
                            _ => [("HandAnim","1"),("NoGoHandAnim","0"),("CastRule","5"),("TargetRule","6")] };
                        var prepared = document.UpdateGlobalRules(draft,Set(draft,edits)); var before = notifications; document.ApplyGlobalRules(prepared); var full = Snapshot(document); var read = Draft(document,family);
                        Expect(notifications == before + 1,"Named rules not applied in one transaction."); if (!document.IsLegacyProject) Packed(document,read,initial);
                        var saved = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,(document.IsLegacyProject ? "project-" : "native-") + family)));
                        foreach (var field in GlobalDefinitionRules.Fields(family)) Expect(Draft(saved,family).Raw(field) == read.Raw(field),"Saved named rule differs."); Exact(document,full);
                        if (document.IsLegacyProject) { var compiled = document.CompileLegacyProject("Rules" + family,native); Packed(compiled,Draft(compiled,family)); foreach (var field in GlobalDefinitionRules.Fields(family)) Expect(Draft(compiled,family).Raw(field) == read.Raw(field),"Compiled named rule differs."); Exact(document,full); }
                        Reject(() => document.ApplyGlobalRules(prepared)); Exact(document,full); document.Undo(); Exact(document,initial);
                        Reject(() => document.UpdateGlobalRules(draft,[])); Reject(() => document.UpdateGlobalRules(draft,Inputs(draft).Append(Inputs(draft)[0])));
                        foreach (var invalid in new[] { "254","-1","4294967296","0x01","1e1","invalid" }) Reject(() => document.UpdateGlobalRules(draft,Set(draft,(GlobalDefinitionRules.Fields(family)[0],invalid)))); Exact(document,initial);
                    }
                    foreach (var type in new[] { "1","2","3" }) foreach (var value in new[] { "0","32767" })
                    {
                        var draft = Draft(document,"Traps"); var prepared = document.UpdateGlobalRules(draft,Set(draft,("TriggerType",type),("TriggerData",value))); document.ApplyGlobalRules(prepared);
                        Expect(Draft(document,"Traps").Raw("TriggerData") == value,"Trigger endpoint differs."); if (prepared.Changed) document.Undo(); Exact(document,initial);
                    }
                    var trap = Draft(document,"Traps"); foreach (var invalid in new[] { "-1","32768","1.1","1e1","0x10","invalid" }) Reject(() => document.UpdateGlobalRules(trap,Set(trap,("TriggerType","1"),("TriggerData",invalid))));
                    var canonical = document.UpdateGlobalRules(trap,Set(trap,("TriggerType","+01"),("TriggerData","25"))); document.ApplyGlobalRules(canonical); Expect(Draft(document,"Traps").Raw("TriggerType") == "1" && Draft(document,"Traps").Raw("TriggerData") == "25","Canonical trigger input rejected."); document.Undo(); Exact(document,initial);
                    var unknown = document.IsLegacyProject ? "4294967295" : "255";
                    document.SetGlobalDefinitionValues(fields.Select(item => new GlobalDefinitionChange(item.Family,0,item.Field,item.Field == "TriggerData" ? "65535" : unknown))); var imported = Snapshot(document);
                    foreach (var family in GlobalDefinitionRules.Families)
                    {
                        var draft = Draft(document,family); document.ApplyGlobalRules(document.UpdateGlobalRules(draft,Inputs(draft))); Exact(document,imported);
                        var field = GlobalDefinitionRules.Fields(family)[0]; var known = GlobalDefinitionRules.Choices(family,field)[0].Value; document.ApplyGlobalRules(document.UpdateGlobalRules(draft,Set(draft,(field,known))));
                        Expect(Draft(document,family).Raw(GlobalDefinitionRules.Fields(family)[1]) == unknown,"Type repair normalized another imported field."); document.Undo(); Exact(document,imported);
                    }
                    var importedTrap = Draft(document,"Traps"); Reject(() => document.UpdateGlobalRules(importedTrap,Set(importedTrap,("TriggerData","25")))); Exact(document,imported);
                    var repairedTrap = document.UpdateGlobalRules(importedTrap,Set(importedTrap,("TriggerType","1"))); document.ApplyGlobalRules(repairedTrap); Expect(Draft(document,"Traps").Raw("TriggerData") == "65535","Trigger repair cleared its data."); document.Undo(); Exact(document,imported);
                    var savedUnknown = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,document.IsLegacyProject ? "unknown-project" : "unknown-native"))); foreach (var family in GlobalDefinitionRules.Families) Expect(Draft(savedUnknown,family).Raw(GlobalDefinitionRules.Fields(family)[0]) == unknown,"Unknown rule changed in saved copy.");
                    if (document.IsLegacyProject) { var compiled = document.CompileLegacyProject("UnknownRules",native); foreach (var family in GlobalDefinitionRules.Families) Expect(Draft(compiled,family).Raw(GlobalDefinitionRules.Fields(family)[0]) == "255","Original rule BYTE casts changed."); Expect(Draft(compiled,"Traps").Raw("TriggerData") == "65535","Original trigger WORD cast changed."); }
                    document.Undo(); Exact(document,initial);
                    var pending = document.UpdateGlobalRules(Draft(document,"Traps"),Set(Draft(document,"Traps"),("Material","7")));
                    document.SetGlobalDefinitionValues([new("Traps",0,"Health","123")]); var other = Snapshot(document); document.ApplyGlobalRules(pending); Expect(document.GlobalDefinitionValues(document.GlobalDefinitionRows().Single(row => row.Family == "Traps" && row.Index == 0)).Single(value => value.Name == "Health").Value == "123","Rule apply overwrote independent numeric data."); document.Undo(); Exact(document,other); document.Undo(); Exact(document,initial);
                    var foreign = NativeMapDocument.CreateNewMap(new("Foreign","Foreign",20,20)); Reject(() => foreign.ApplyGlobalRules(Draft(document,"Doors"))); Reject(() => Draft(document,"Creatures")); Exact(document,initial);
                }
                finally { document.Changed -= handler; }
            }
            Exact(native,basis); var missing = NativeMapDocument.Create(20,20,"Missing"); foreach (var family in GlobalDefinitionRules.Families) { var draft = Draft(missing,family); Expect(!draft.Editable && draft.Raw(GlobalDefinitionRules.Fields(family)[0]).Length == 0,"Missing rules inferred from source defaults."); Reject(() => missing.ApplyGlobalRules(draft)); }
        }
        finally { Directory.Delete(directory,true); }
    }
}
