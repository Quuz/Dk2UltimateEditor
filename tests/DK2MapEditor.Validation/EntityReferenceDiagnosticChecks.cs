using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class EntityReferenceDiagnosticChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    // Simulate bytes already present in imported files, not an allowed identity edit.
    private static void Imported(NativeRecord record, string name, string value)
    {
        Expect(MemoryMarshal.TryGetArray(record.File.Data,out var array),"Fixture backing bytes unavailable.");
        var field = record.Fields.Single(item => item.Name == name);
        field.Encode(value).CopyTo(array.Array!,array.Offset + record.Offset + field.Offset);
    }
    private static IReadOnlyList<MapDiagnostic> Check(NativeMapDocument document, bool scripts = true)
    {
        var bytes = document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        var revision = document.EditRevision; var dirty = document.HasChanges;
        var undo = document.GetHistory().Undo.Count; var redo = document.GetHistory().Redo.Count; var notifications = 0;
        EventHandler handler = (_,_) => notifications++; document.Changed += handler;
        IReadOnlyList<MapDiagnostic> result;
        try { result = EntityReferenceDiagnostics.Validate(document,scripts); }
        finally { document.Changed -= handler; }
        Expect(result.All(item => item.Severity == DiagnosticSeverity.Warning),"Imported reference diagnostic blocked preservation.");
        Expect(document.EditRevision == revision && document.HasChanges == dirty && notifications == 0
            && document.GetHistory().Undo.Count == undo && document.GetHistory().Redo.Count == redo,"Diagnostics changed history or notifications.");
        Expect(bytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)),"Diagnostics changed imported bytes.");
        return result;
    }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(),"DK2EntityReferences-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var projectPath = Path.Combine(directory,"project.dkm");
            File.WriteAllBytes(projectPath,TextMapFile.Parse("GUI_GENERATED_MAP\nReferences\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF").ToArray());
            foreach (var project in new[] { false,true })
            {
                var document = project ? NativeMapDocument.Load(projectPath) : NativeMapDocument.Create(12,12,"References");
                NativeRecord Add(string legacy,string native) => project ? document.AddLegacyRecord(legacy,new(1,1))
                    : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == native),new(1,1));
                void Set(NativeRecord record,string legacy,string native,string value) => Imported(record,project ? legacy : native,value);
                var area = Add("Area","GAMEAREA"); var band = Add("Band","GAMEBAND"); var effect = Add("Effect","GAMEEFFECT");
                Set(area,"id","bAreaID","7"); Set(band,"id","bBandID","0"); Set(effect,"id","bEffectID","7");
                var hero = Add("Creature","GAMEHERO"); Set(hero,"area","bHeroTargetArea","7");
                Expect(Check(document).Count == 0,"Valid namespace collision, band zero or unassigned band sentinel was diagnosed.");
                Set(hero,"area","bHeroTargetArea","99");
                var missing = Check(document);
                Expect(missing.Count == 1 && missing[0].Code == "DK2N026" && missing[0].Message.Contains("Area target 99"),"Missing physical area not identified.");
                Set(hero,"area","bHeroTargetArea","0"); Expect(Check(document).Count == 0,"Optional zero area was treated as missing.");
                Set(area,"nextid","bAreaNextID","99"); Expect(Check(document).Single().Code == "DK2N026","Missing patrol successor not reported.");
                Set(area,"nextid","bAreaNextID","0");
                document.AddBandMember(band,17);
                var member = project ? band : document.Records().Single(record => record.File == band.File && record.Offset == band.Offset + 51);
                Set(member,"heros[0].area","bHeroTargetArea","99");
                Expect(Check(document).Single().Code == "DK2N026","Active member's missing target was not reported.");
                Set(member,"heros[0].area","bHeroTargetArea","0");
                if (project) Imported(band,"heros[1].area","99");
                else
                {
                    var inactive = document.Records().Single(record => record.File == band.File && record.Offset == band.Offset + 83);
                    Imported(inactive,"bHeroTargetArea","99"); Imported(inactive,"bHeroBandID","99");
                }
                Expect(Check(document).Count == 0,"Inactive slot bytes were interpreted as live references.");
                var duplicate = Add("Area","GAMEAREA"); Set(duplicate,"id","bAreaID","7");
                Set(hero,"area","bHeroTargetArea","7");
                var ambiguous = Check(document);
                Expect(ambiguous.Count(item => item.Code == "DK2N028") == 2 && ambiguous.Count(item => item.Code == "DK2N027") == 1,
                    "Imported duplicate identity and ambiguous target were not distinguished.");
                Set(duplicate,"id","bAreaID","8"); Set(hero,"area","bHeroTargetArea","0");
                var action = Add("Action","Action"); document.SetEventType(action,LegacyEventSchemas.All.Single(type => type.Name == "ACTION_TOGGLEEFFECT"));
                action = document.Records().Single(record => record.File == action.File && record.Offset == action.Offset && record.Category == action.Category);
                Set(action,"param1","Param1","99");
                Expect(Check(document).Single().Message.Contains("Effect target 99"),"Script effect target missing from diagnostics.");
                Expect(Check(document,false).Count == 0,"Original no-scenario projection checked omitted script operands.");
                Set(action,"param1","Param1","7"); Expect(Check(document).Count == 0,"Valid effect target was confused with equal area ID.");
                var saved = document.SaveCopy(Path.Combine(directory,project ? "project-export" : "native-export"));
                var reloaded = NativeMapDocument.Load(saved);
                Expect(Check(reloaded).Count == 0,"Reopening changed entity diagnostic interpretation.");
                Set(hero,"area","bHeroTargetArea","99");
                Expect(NativeMapValidator.Validate(document).Any(item => item.Code == "DK2N026"),"Main validator omitted entity diagnostics.");
            }
            // An opaque native thing may contain an unverified target identity.
            // Absence cannot be inferred from just the understood record subset.
            var unknown = NativeMapDocument.Create(12,12,"Unknown");
            var unknownHero = unknown.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEHERO"),new(1,1));
            Imported(unknownHero,"bHeroTargetArea","99");
            var unknownEntry = unknown.SaveCopy(Path.Combine(directory,"unknown"));
            var things = unknown.Files.Single(file => file.Signature == 0xBE); var thingsPath = Path.Combine(directory,"unknown",things.ExportPath);
            var data = File.ReadAllBytes(thingsPath); var bytes = new byte[data.Length + 24]; data.CopyTo(bytes,0);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(data.Length),0xFE);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(data.Length + 4),16);
            foreach (var offset in new[] {8,52}) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset),BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset)) + 24);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20),BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(20)) + 1);
            File.WriteAllBytes(thingsPath,bytes);
            Expect(Check(NativeMapDocument.Load(unknownEntry)).Count == 0,"Opaque target layouts produced a fabricated missing-identity warning.");
        }
        finally { Directory.Delete(directory,true); }
    }
}
