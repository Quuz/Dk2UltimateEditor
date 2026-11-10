using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
string Value(NativeRecord record, string field) => record.Read(record.Fields.Single(candidate => candidate.Name == field));
void Set(NativeMapDocument document, NativeRecord record, string field, string value) => document.SetField(record, record.Fields.Single(candidate => candidate.Name == field), value);
void U32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
void U16(byte[] bytes, int offset, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), value);
var directory = Path.GetFullPath(args.Single()); Check(!Directory.Exists(directory), "Use a fresh evidence directory."); Directory.CreateDirectory(directory);
var basis = NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
var sourceHashes = basis.Files.ToDictionary(file => file.SourcePath, file => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file.SourcePath))));
var cases = new List<int>();
foreach (var active in new[] { 0, 1, 15, 16 })
{
    var project = NativeMapDocument.CreateNewMap(new("BandCompiler", "Band compiler", 24, 24, PlaceHearts: true));
    var expected = new Dictionary<byte, byte[]>();
    foreach (var bandID in new byte[] { 7, 11 })
    {
        var band = project.AddLegacyRecord("Band", new(0, 0)); Set(project, band, "id", bandID.ToString());
        var trigger = project.AddLegacyRecord("Trigger", new(0, 0)); var triggerID = ushort.Parse(Value(trigger, "id"));
        var link = project.AddLegacyRecord("TriggerLink", new(0, 0));
        Set(project, link, "node.type", "8"); Set(project, link, "band", Value(band, "uniqueid")); Set(project, link, "trigger", triggerID.ToString());
        // Literal original Compiler_GenerateHeros wire layout:563 bytes,
        // header51,16 slots32. This oracle does not use native schema offsets.
        var bytes = new byte[563]; U32(bytes, 0, 0xC9); U32(bytes, 4, 555); U32(bytes, 8, 9); U16(bytes, 40, triggerID); bytes[42] = bandID;
        for (var index = 0; index < active; index++)
        {
            project.AddBandMember(band, 17, index % 10 + 1, (uint)(index % 5 + 3));
            Set(project, band, $"heros[{index}].goldheld", (513 + index).ToString());
            Set(project, band, $"heros[{index}].health", (9876 + index).ToString());
            Set(project, band, $"heros[{index}].options2", index == 0 ? "43" : "45");
            Set(project, band, $"heros[{index}].options", "1");
            var memberLink = project.AddLegacyRecord("TriggerLink", new(0, 0));
            Set(project, memberLink, "node.type", "16"); Set(project, memberLink, "creature", Value(band, $"heros[{index}].uniqueid")); Set(project, memberLink, "trigger", triggerID.ToString());
            var offset = 51 + index * 32;
            bytes[offset + 12] = unchecked((byte)(513 + index)); bytes[offset + 14] = (byte)(index % 10 + 1);
            bytes[offset + 15] = index == 0 ? (byte)43 : (byte)45; U32(bytes, offset + 20, (uint)(9876 + index));
            U16(bytes, offset + 24, triggerID); bytes[offset + 26] = (byte)(index % 5 + 3); bytes[offset + 27] = 19;
            bytes[offset + 28] = 17; bytes[offset + 30] = bandID; bytes[offset + 31] = 1;
        }
        if (active < 16)
        {
            // Inactive fields may retain stale values. The original compiler
            // stops at isvalid=0 and writes zero bytes for the entire suffix.
            Set(project, band, $"heros[{active}].goldheld", "777");
            Set(project, band, $"heros[{active}].health", "12345");
        }
        expected.Add(bandID, bytes);
    }
    var before = project.Files.ToDictionary(file => file, file => file.Data.ToArray()); var revision = project.EditRevision; var history = project.GetHistory().Undo.Count;
    var compiled = project.CompileLegacyProject("DK2Preview_6a88240eaf814f78", basis);
    void Verify(NativeMapDocument map)
    {
        foreach (var record in map.Records().Where(record => record.Category == "GAMEBAND"))
        {
            var id = byte.Parse(Value(record, "bBandID"));
            Check(record.File.Data.Span.Slice(record.Offset, record.Length).SequenceEqual(expected[id]), "Full literal563-byte band differs: active=" + active + " ID=" + id);
            Check(map.BandMembers(record).Count(member => member.Active) == active, "Compilation changed the active prefix.");
        }
    }
    Verify(compiled); var saved = compiled.SaveCopy(Path.Combine(directory, "active-" + active)); Verify(NativeMapDocument.Load(saved));
    Check(Directory.GetFiles(Path.GetDirectoryName(saved)!).Length == 7, "Compiler changed seven-file publication.");
    Check(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && revision == project.EditRevision && history == project.GetHistory().Undo.Count, "Compilation/export modified the editable project or history.");
    cases.Add(active); Console.WriteLine("PASS literal563-byte records, two distinct band IDs, root/member triggers, active=" + active + ", zero inactive suffix, stale inactive fields ignored, seven-file save/reopen and immutable project");
}
var native = NativeMapDocument.Create(8, 8, "NativeTemplate");
var nativeBand = native.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(0, 0));
var nativeID = byte.Parse(Value(nativeBand, "bBandID"));
for (var index = 0; index < 16; index++) Check(nativeBand.File.Data.Span[nativeBand.Offset + 51 + index * 32 + 30] == nativeID, "Compiler fix changed direct native editing defaults.");
Check(sourceHashes.All(pair => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(pair.Key))) == pair.Value), "Original game data changed.");
File.WriteAllText(Path.Combine(directory, "actual-inactive-band-compiler.json"), JsonSerializer.Serialize(new { Passed = true, ActiveCounts = cases, FullLiteralRecordsChecked = 8, NativeTemplatePreserved = true, SourceUnchanged = true, ComputerUseUsed = false, GameLaunched = false }, new JsonSerializerOptions { WriteIndented = true }));
