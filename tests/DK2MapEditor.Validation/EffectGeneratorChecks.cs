using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Editing;

internal static class EffectGeneratorChecks
{
    private static void Expect(bool condition,string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action) { try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { return; } throw new Exception("Invalid effect-generator operation accepted."); }
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document,Dictionary<NativeFile,byte[]> snapshot) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]),"Effect-generator changed unrelated/undo bytes."); }
    private static EffectGeneratorDraft Draft(NativeMapDocument document,NativeRecord record) => document.CreateEffectGeneratorDraft(record);
    private static EffectGeneratorInput[] Inputs(EffectGeneratorDraft draft) => EffectGenerators.Fields.Select(field => new EffectGeneratorInput(field,draft.Raw(field))).ToArray();
    private static EffectGeneratorInput[] Set(EffectGeneratorDraft draft,params (string Field,string Value)[] changes) => Inputs(draft).Select(input => changes.FirstOrDefault(change => change.Field == input.Field) is var change && change.Field is not null ? input with { Value = change.Value } : input).ToArray();
    private static void Packed(NativeMapDocument document,EffectGeneratorDraft draft,Dictionary<NativeFile,byte[]> before)
    {
        var expected = before.ToDictionary(item => item.Key,item => item.Value.ToArray());
        for (var index = 0; index < 5; index++)
        {
            var field = EffectGenerators.Fields[index]; var native = !document.IsLegacyProject;
            var offset = native ? index == 4 ? 36 : 28 + index * 2 : 60 + index * 4;
            var target = expected[draft.Record.File].AsSpan(draft.Record.Offset + offset); var value = int.Parse(draft.Raw(field),CultureInfo.InvariantCulture);
            if (!native) BinaryPrimitives.WriteInt32LittleEndian(target,value);
            else if (index == 4) target[0] = checked((byte)value); else BinaryPrimitives.WriteUInt16LittleEndian(target,checked((ushort)value));
        }
        Exact(document,expected);
    }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(),"DK2-EffectGenerators-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var legacy in new[] { false,true })
            {
                var document = legacy ? NativeMapDocument.CreateNewMap(new("Effects","Effects",20,20)) : NativeMapDocument.Create(20,20,"Effects");
                var record = document.AddMapRegion(MapRegionKind.Effect,new(2,3,4,5)); var second = document.AddMapRegion(MapRegionKind.Effect,new(8,9,1,1)); var area = document.AddMapRegion(MapRegionKind.Area,new(1,1,2,2));
                if (!legacy)
                {
                    var root = Path.Combine(directory,"opaque-padding"); var entry = document.SaveCopy(root); var path = Path.Combine(root,record.File.ExportPath); var bytes = File.ReadAllBytes(path);
                    for (var index = 0; index < 6; index++) bytes[record.Offset + 38 + index] = (byte)(0xA1 + index);
                    for (var index = 0; index < 8; index++) bytes[record.Offset + 16 + index] = (byte)(0xD1 + index);
                    File.WriteAllBytes(path,bytes); document = NativeMapDocument.Load(entry);
                    record = document.MapRegions().Single(region => region.Kind == MapRegionKind.Effect && region.Bounds.X == 2).Record;
                    second = document.MapRegions().Single(region => region.Kind == MapRegionKind.Effect && region.Bounds.X == 8).Record;
                    area = document.MapRegions().Single(region => region.Kind == MapRegionKind.Area).Record;
                }
                var draft = Draft(document,record); Expect(draft.Editable && draft.Raw("Frequency") == "1" && EffectGenerators.Fields.Take(4).All(field => draft.Raw(field) == "0"),"New generator/source layout/default frequency differs.");
                Expect(record.Length == (legacy ? 292 : 44),"Literal generator record size differs.");
                var originalField = record.Fields.Single(field => field.Name == EffectGenerators.StorageField(record,"Effect1"));
                Expect(!EffectGenerators.Supported(record with { Length = record.Length + 1 }) && !EffectGenerators.Supported(record with { Fields = record.Fields.Where(field => field != originalField).ToArray() })
                    && !EffectGenerators.Supported(record with { Fields = record.Fields.Select(field => field == originalField ? field with { Offset = field.Offset + 1 } : field).ToArray() }),"Unknown generator sizes/field layouts guessed as supported.");
                for (var index = 0; index < 4; index++)
                {
                    var field = record.Fields.Single(field => field.Name == EffectGenerators.StorageField(record,EffectGenerators.Fields[index]));
                    Expect(ValueCatalogues.ForField(record,field).Select(choice => choice.Value).SequenceEqual(EffectCatalogue.Choices.Select(choice => choice.Value)),"Generic effect slot catalogue missing.");
                }
                var id = record.Fields.Single(field => field.Name == (legacy ? "id" : "bEffectID")); var originalID = record.Read(id); Expect(ValueCatalogues.ForField(record,id).Count == 0,"Generator identity incorrectly treated as effect type.");
                Reject(() => Draft(document,area)); var initial = Snapshot(document); var changes = 0; EventHandler handler = (_,_) => changes++; document.Changed += handler;
                try
                {
                    document.ApplyEffectGenerator(document.UpdateEffectGenerator(draft,Inputs(draft))); Exact(document,initial); Expect(changes == 0,"Default no-op emitted change.");
                    foreach (var field in EffectGenerators.Fields.Take(4)) foreach (var choice in EffectCatalogue.Choices)
                    {
                        draft = Draft(document,record); var prepared = document.UpdateEffectGenerator(draft,Set(draft,(field,choice.Value))); var count = changes; document.ApplyEffectGenerator(prepared);
                        var read = Draft(document,record); Expect(read.Raw(field) == choice.Value && changes == count + (prepared.Changed ? 1 : 0),"Effect slot ID/transaction differs."); Packed(document,read,initial);
                        document.ApplyEffectGenerator(document.UpdateEffectGenerator(read,Inputs(read))); Expect(changes == count + (prepared.Changed ? 1 : 0),"Effect no-op created history.");
                        if (prepared.Changed) document.Undo(); Exact(document,initial);
                    }
                    foreach (var frequency in new[] { "1","2","127","254","255" })
                    {
                        draft = Draft(document,record); var prepared = document.UpdateEffectGenerator(draft,Set(draft,("Frequency",frequency))); document.ApplyEffectGenerator(prepared); Packed(document,Draft(document,record),initial); if (prepared.Changed) document.Undo(); Exact(document,initial);
                    }
                    draft = Draft(document,record);
                    foreach (var field in EffectGenerators.Fields) foreach (var invalid in new[] { "-1","371","65536","2147483648","1.5","1e1","0x1","bad" }) Reject(() => document.UpdateEffectGenerator(draft,Set(draft,(field,invalid))));
                    foreach (var invalid in new[] { "0","256","-2147483648" }) Reject(() => document.UpdateEffectGenerator(draft,Set(draft,("Effect1","370"),("Frequency",invalid))));
                    Reject(() => document.UpdateEffectGenerator(draft,[])); Reject(() => document.UpdateEffectGenerator(draft,Inputs(draft).Append(Inputs(draft)[0]))); Exact(document,initial);
                    var preparedAll = document.UpdateEffectGenerator(draft,Set(draft,("Effect1","+0370"),("Effect2","0"),("Effect3","55"),("Effect4","256"),("Frequency","+0255"))); var countAll = changes;
                    document.ApplyEffectGenerator(preparedAll); var full = Snapshot(document); var readAll = Draft(document,record); Expect(changes == countAll + 1,"All generator options not one transaction."); Packed(document,readAll,initial);
                    Expect(document.GetMapRegion(record)!.Bounds == new TileRect(2,3,4,5) && record.Read(id) == originalID && Draft(document,second).Raw("Frequency") == "1","Options altered bounds/identity/another generator.");
                    var copy = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,legacy ? "project" : "native"))); var reopened = copy.MapRegions().Single(region => region.Kind == MapRegionKind.Effect && region.Bounds.X == 2).Record;
                    Expect(EffectGenerators.Fields.All(field => Draft(copy,reopened).Raw(field) == readAll.Raw(field)),"Saved generator options differ."); Exact(document,full);
                    if (legacy)
                    {
                        var compiled = document.CompileLegacyProject("EffectCompile",NativeMapDocument.Create(20,20,"Basis")); var compiledRecord = compiled.MapRegions().Single(region => region.Kind == MapRegionKind.Effect && region.Bounds.X == 2).Record;
                        Expect(EffectGenerators.Fields.All(field => Draft(compiled,compiledRecord).Raw(field) == readAll.Raw(field)) && compiled.GetMapRegion(compiledRecord)!.Bounds == new TileRect(2,3,4,5),"Compiled options/bounds differ."); Exact(document,full);
                    }
                    Reject(() => document.ApplyEffectGenerator(preparedAll)); Exact(document,full); document.Undo(); Exact(document,initial); document.Redo(); Exact(document,full); document.Undo(); Exact(document,initial);
                    var unknown = legacy ? new[] { "-1","-2147483648","2147483647","65536","-2147483648" } : new[] { "65535","65534","65533","65532","0" };
                    document.Edit("Import unknown generator",() => { for (var index = 0; index < 5; index++) document.SetField(record,record.Fields.Single(field => field.Name == EffectGenerators.StorageField(record,EffectGenerators.Fields[index])),unknown[index]); });
                    var imported = Snapshot(document); var importedDraft = Draft(document,record); document.ApplyEffectGenerator(document.UpdateEffectGenerator(importedDraft,Inputs(importedDraft))); Exact(document,imported);
                    var repaired = document.UpdateEffectGenerator(importedDraft,Set(importedDraft,("Effect1","0"))); document.ApplyEffectGenerator(repaired); Expect(EffectGenerators.Fields.Skip(1).All(field => Draft(document,record).Raw(field) == importedDraft.Raw(field)),"Explicit repair normalized other unknown options."); document.Undo(); Exact(document,imported);
                    var savedUnknown = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,legacy ? "unknown-project" : "unknown-native"))); var unknownRecord = savedUnknown.MapRegions().Single(region => region.Kind == MapRegionKind.Effect && region.Bounds.X == 2).Record;
                    Expect(EffectGenerators.Fields.Select(field => Draft(savedUnknown,unknownRecord).Raw(field)).SequenceEqual(unknown),"Unknown values lost on save.");
                    if (legacy) { var compiled = document.CompileLegacyProject("UnknownEffects",NativeMapDocument.Create(20,20,"Basis")); var compiledRecord = compiled.MapRegions().Single(region => region.Kind == MapRegionKind.Effect && region.Bounds.X == 2).Record; Expect(EffectGenerators.Fields.Select(field => Draft(compiled,compiledRecord).Raw(field)).SequenceEqual(new[] { "65535","0","65535","0","0" }),"Original LONG-to-WORD/BYTE casts differ."); }
                    document.Undo(); Exact(document,initial);
                    var pending = document.UpdateEffectGenerator(Draft(document,record),Set(Draft(document,record),("Effect1","1")));
                    document.SetMapRegionBounds(record,new(3,4,4,5)); var moved = Snapshot(document); Reject(() => document.ApplyEffectGenerator(pending)); Exact(document,moved); document.Undo(); Exact(document,initial);
                    var foreign = NativeMapDocument.Create(20,20,"Foreign"); Reject(() => foreign.ApplyEffectGenerator(Draft(document,record))); Reject(() => foreign.CreateEffectGeneratorDraft(record));
                    document.SetField(record,id,"7"); var identity = Snapshot(document); Reject(() => document.ApplyEffectGenerator(pending)); Exact(document,identity); document.Undo(); Exact(document,initial);
                    document.DeleteRecord(record); var removed = Snapshot(document); Reject(() => document.ApplyEffectGenerator(pending)); Exact(document,removed); document.Undo(); Exact(document,initial);
                    if (!legacy)
                    {
                        var root = Path.Combine(directory,"unknown-layout"); var entry = document.SaveCopy(root); var path = Path.Combine(root,record.File.ExportPath); var bytes = File.ReadAllBytes(path);
                        var end = record.Offset + record.Length; var extended = bytes.AsSpan(0,end).ToArray().Concat(new byte[] { 0xF1,0xF2,0xF3,0xF4 }).Concat(bytes.AsSpan(end).ToArray()).ToArray();
                        BinaryPrimitives.WriteUInt32LittleEndian(extended.AsSpan(record.Offset + 4),40); File.WriteAllBytes(path,extended);
                        var unknownDocument = NativeMapDocument.Load(entry); var variantRecord = unknownDocument.Records().Single(item => item.Category == "GAMEEFFECT" && item.Offset == record.Offset); var unknownDraft = Draft(unknownDocument,variantRecord); var stable = Snapshot(unknownDocument);
                        Expect(variantRecord.Length == 48 && !unknownDraft.Editable && EffectGenerators.Fields.All(field => unknownDraft.Raw(field) == ""),"Extended unknown generator inferred editable prefix values.");
                        Reject(() => unknownDocument.UpdateEffectGenerator(unknownDraft,Inputs(unknownDraft))); Reject(() => unknownDocument.ApplyEffectGenerator(unknownDraft)); Exact(unknownDocument,stable);
                        var variantReopened = NativeMapDocument.Load(unknownDocument.SaveCopy(Path.Combine(directory,"unknown-layout-copy")));
                        Expect(variantReopened.Files.Single(file => file.ExportPath == variantRecord.File.ExportPath).Data.Span.SequenceEqual(extended),"Unknown generator opaque layout lost on save.");
                    }
                }
                finally { document.Changed -= handler; }
            }
        }
        finally { Directory.Delete(directory,true); }
    }
}
