using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
string Value(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
void Set(NativeMapDocument map, NativeRecord record, string name, string value) => map.SetField(record, record.Fields.Single(field => field.Name == name), value);
byte[] Bytes(NativeRecord record) => record.File.Data.Slice(record.Offset, record.Length).ToArray();
Span<byte> FixtureBytes(NativeRecord record)
{
    Check(MemoryMarshal.TryGetArray(record.File.Data, out var segment), "Fixture backing array unavailable.");
    return segment.AsSpan().Slice(record.Offset, record.Length);
}
string Key(NativeRecord record) => record.Category + ":" + Value(record, record.Category switch { "GAMEAREA" => "bAreaID", "GAMEBAND" => "bBandID", _ => "bGlobalPlayer" });
bool Selected(NativeRecord record) => record.Category is "GAMEAREA" or "GAMEBAND" or "GAMEPLAYER";
byte[] Normalized(NativeRecord record, IReadOnlyDictionary<ushort, ushort> ids)
{
    var result = Bytes(record);
    void Roots(IEnumerable<NativeField> fields, int offset)
    {
        foreach (var field in fields.Where(field => field.Name.EndsWith("Trigger", StringComparison.Ordinal)))
        {
            var original = ushort.Parse(field.Read(result.AsSpan(offset)));
            if (original != 0) { Check(ids.ContainsKey(original), "Missing original root mapping."); field.Encode(ids[original].ToString()).CopyTo(result, offset + field.Offset); }
        }
    }
    Roots(record.Fields, 0);
    if (record.Category == "GAMEBAND") for (var index = 0; index < 16; index++) Roots(LegacySchemas.Fields("GAMEBANDHERO"), 51 + 32 * index);
    return result;
}
var directory = Path.GetFullPath(args.Single()); Check(!Directory.Exists(directory), "Use fresh evidence."); Directory.CreateDirectory(directory);
const string level = "DK2Preview_6a88240eaf814f78";
var basis = NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
var manual = Path.GetFullPath("tests/Fixtures/NativeRoundtrip/" + level + ".dkm");
var archivePath = Directory.GetFiles(Path.GetDirectoryName(manual)!, "*.native-source-*.zip").Single();
var hashes = basis.Files.Select(file => file.SourcePath).Append(manual).Append(archivePath).Distinct().ToDictionary(path => path, path => SHA256.HashData(File.ReadAllBytes(path)));
var mapping = EditorSourceArchive.Read(archivePath).NativeEventIDs;
var expected = basis.Records().Where(Selected).ToDictionary(Key, record => Normalized(record, mapping));
var user = NativeMapDocument.Load(manual);
var before = user.Files.ToDictionary(file => file, file => file.Data.ToArray()); var revision = user.EditRevision; var history = user.GetHistory().Undo.Count;
void VerifyActual(NativeMapDocument compiled)
{
    var actual = compiled.Records().Where(Selected).ToDictionary(Key, Bytes);
    Check(expected.Count == 97 && actual.Count == expected.Count, "Unexpected official area/band/player count.");
    foreach (var (key, bytes) in expected) Check(actual[key].AsSpan().SequenceEqual(bytes), "Whole official record differs after root normalization: " + key);
}
var compiled = user.CompileLegacyProject(level, basis); VerifyActual(compiled);
var exported = compiled.SaveCopy(Path.Combine(directory, "compiled-user-save")); VerifyActual(NativeMapDocument.Load(exported));
Check(Directory.GetFiles(Path.GetDirectoryName(exported)!).Length == 7, "More than seven game files.");
Check(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && user.EditRevision == revision && user.GetHistory().Undo.Count == history, "Compilation modified user project/history.");
Console.WriteLine("PASS all97 whole official area/band/player records, normalized original roots, seven-file save/reopen, immutable project");

var source = NativeMapDocument.Create(18, 18, "OpaqueRecords", basis);
source.Paint(Enumerable.Range(9, 5).SelectMany(y => Enumerable.Range(9, 5).Select(x => new TilePoint(x, y))), new MapTile(14, 3, 1));
var heart = source.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEGATE"), new(11, 11), 3); Set(source, heart, "bGateType", "14");
var area = source.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEAREA"), new(4, 4)); Set(source, area, "bAreaID", "7");
FixtureBytes(area)[27] = 0xA3;
for (var index = 32; index < 64; index++) FixtureBytes(area)[index] = (byte)(index + 93);
var band = source.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(0, 0)); Set(source, band, "bBandID", "9");
FixtureBytes(band).Slice(51, 16 * 32).Clear();
for (var index = 8; index < 40; index++) FixtureBytes(band)[index] = (byte)(index + 71);
for (var index = 43; index < 51; index++) FixtureBytes(band)[index] = (byte)(index + 17);
var member = source.Records().Single(record => record.Category == "BandHero 1");
Set(source, member, "bGlobalCreatureID", "3"); Set(source, member, "bGlobalBehavior", "1"); Set(source, member, "bGlobalLevel", "2");
Set(source, member, "dwGlobalHealth", "100"); Set(source, member, "bHeroBandID", "9"); Set(source, member, "bGlobalPlayer", "1");
var player = source.Records().Single(record => record.Category == "GAMEPLAYER");
foreach (var offset in new[] { 13, 14, 15, 107, 119, 140, 141, 148, 149, 153 }) FixtureBytes(player)[offset] = (byte)(offset + 2);
Set(source, player, "szPlayerName", "Keeper");
for (var index = 24; index < 32; index++) FixtureBytes(player)[LegacySchemas.Offset("GAMEPLAYER", "szPlayerName") + index] = (byte)(150 + index);
var synthetic = Path.Combine(directory, "synthetic.dkm");
using (var save = new EditorSaveSession(source)) { var plan = save.PrepareSaveAs(synthetic); Check(!plan.HasBlockingIssues, "Synthetic native import blocked: " + JsonSerializer.Serialize(plan.ConversionIssues)); Check(save.Save(plan, true), "Synthetic archive save refused."); }
var sourceRecords = source.Records().Where(Selected).ToDictionary(Key, Bytes);
var loaded = NativeMapDocument.Load(synthetic); var clean = loaded.CompileLegacyProject(level, basis);
foreach (var record in clean.Records().Where(Selected)) if (sourceRecords.TryGetValue(Key(record), out var bytes)) Check(Bytes(record).AsSpan().SequenceEqual(bytes), "Synthetic literal opaque record not retained: " + Key(record));
Console.WriteLine("PASS literal unknown area bytes, band header including source version word, player unused bytes and name tail");
NativeRecord ProjectArea(NativeMapDocument map) => map.Records().Single(record => record.Category == "Project.MAPRECTEX" && Value(record, "node.type") == "0");
NativeRecord ProjectBand(NativeMapDocument map) => map.Records().Single(record => record.Category == "Project.MTHBAND");
NativeRecord ProjectPlayer(NativeMapDocument map) => map.Records().Where(record => record.Category == "Project.MAPOPTIONS").ElementAt(2);
NativeMapDocument BareCompile(NativeMapDocument map, string label)
{
    var target = Path.Combine(directory, label + ".dkm");
    using (var save = new EditorSaveSession(map, synthetic)) save.Save(save.PrepareSaveAs(target), true);
    var bare = Path.Combine(directory, label + "-bare.dkm"); File.Copy(target, bare);
    return NativeMapDocument.Load(bare).CompileLegacyProject(level, basis);
}
void VerifyEdit(string label, string category, Action<NativeMapDocument> edit)
{
    var map = NativeMapDocument.Load(synthetic); edit(map);
    var fresh = BareCompile(map, label); var retained = map.CompileLegacyProject(level, basis);
    var actual = retained.Records().Where(record => record.Category == category).ToDictionary(Key, Bytes);
    foreach (var record in fresh.Records().Where(record => record.Category == category)) Check(actual[Key(record)].AsSpan().SequenceEqual(Bytes(record)), "Changed " + label + " restored obsolete source data.");
    Console.WriteLine("PASS fresh compiler values for " + label);
}
VerifyEdit("area-delay", "GAMEAREA", map => Set(map, ProjectArea(map), "waitdelay", "88"));
VerifyEdit("area-moved", "GAMEAREA", map => { Set(map, ProjectArea(map), "rect.x1", "6"); Set(map, ProjectArea(map), "rect.x2", "6"); });
VerifyEdit("area-identity", "GAMEAREA", map => Set(map, ProjectArea(map), "id", "8"));
VerifyEdit("band-health", "GAMEBAND", map => Set(map, ProjectBand(map), "heros[0].health", "77"));
VerifyEdit("band-member-identity", "GAMEBAND", map => Set(map, ProjectBand(map), "heros[0].uniqueid", "900"));
VerifyEdit("band-identity", "GAMEBAND", map => Set(map, ProjectBand(map), "id", "10"));
VerifyEdit("player-gold", "GAMEPLAYER", map => Set(map, ProjectPlayer(map), "PlayerGold", "3000"));
VerifyEdit("player-name", "GAMEPLAYER", map => Set(map, ProjectPlayer(map), "PlayerName", "Changed"));
var undo = NativeMapDocument.Load(synthetic); Set(undo, ProjectArea(undo), "waitdelay", "88"); undo.Undo();
Check(Bytes(undo.CompileLegacyProject(level, basis).Records().Single(record => record.Category == "GAMEAREA")).AsSpan().SequenceEqual(Bytes(area)), "Undo failed to recover original opaque area bytes.");
var renamed = NativeMapDocument.Load(synthetic); Set(renamed, ProjectArea(renamed), "name", "Editor alias");
Check(Bytes(renamed.CompileLegacyProject(level, basis).Records().Single(record => record.Category == "GAMEAREA")).AsSpan().SequenceEqual(Bytes(area)), "Editor alias discarded unchanged native payload.");
var resized = NativeMapDocument.Load(synthetic); resized.Resize(20, 20, 1, 1, new MapTile(2, 2, 1));
var resizedFresh = BareCompile(resized, "resized");
foreach (var record in resizedFresh.Records().Where(Selected)) Check(Bytes(resized.CompileLegacyProject(level, basis).Records().Where(Selected).Single(candidate => Key(candidate) == Key(record))).AsSpan().SequenceEqual(Bytes(record)), "Resize restored obsolete raw records.");
var newArea = NativeMapDocument.Load(synthetic); var added = newArea.AddLegacyRecord("Area", new(2, 2));
Check(Bytes(newArea.CompileLegacyProject(level, basis).Records().Single(record => record.Category == "GAMEAREA" && Value(record, "bAreaID") == Value(added, "id"))).AsSpan(32, 32).ToArray().All(value => value == 0), "New area inherited original opaque bytes.");
var ambiguous = NativeMapDocument.Load(synthetic); var duplicate = ambiguous.AddLegacyRecord("Area", new(4, 4));
var refused = false; try { Set(ambiguous, duplicate, "id", "7"); } catch (ArgumentException) { refused = true; }
Check(refused, "Interactive editing allowed ambiguous area identity.");
Check(Bytes(ambiguous.CompileLegacyProject(level, basis).Records().Single(record => record.Category == "GAMEAREA" && Value(record, "bAreaID") == Value(duplicate, "id"))).AsSpan(32, 32).ToArray().All(value => value == 0), "New area inherited an existing identity's opaque bytes.");
Console.WriteLine("PASS Undo, editor alias, resize, new identity and ambiguous current records");
Check(hashes.All(pair => SHA256.HashData(File.ReadAllBytes(pair.Key)).AsSpan().SequenceEqual(pair.Value)), "Protected source or user save changed.");
File.WriteAllText(Path.Combine(directory, "actual-native-record-retention.json"), JsonSerializer.Serialize(new { Passed = true, WholeOfficialRecordsPreserved = expected.Count, WholeOfficialCounts = new { Areas = 79, Bands = 15, Players = 3 }, SyntheticOpaqueBytesAndTextTail = true, ChangedIdentityGeometryAndValues = true, UndoResizeNewAndAmbiguous = true, SevenFiles = true, SourceUnchanged = true, ComputerUseUsed = false, GameLaunched = false }, new JsonSerializerOptions { WriteIndented = true }));
