using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
string Value(NativeRecord record, string field) => record.Read(record.Fields.Single(candidate => candidate.Name == field));
void Set(NativeMapDocument document, NativeRecord record, string field, string value) => document.SetField(record, record.Fields.Single(candidate => candidate.Name == field), value);
byte[] Bytes(NativeRecord record) => record.File.Data.Slice(record.Offset, record.Length).ToArray();
string Key(NativeRecord record) => string.Join(",", new[] { "dwGateLocationX", "dwGateLocationY", "bGateType", "bGlobalPlayer" }.Select(field => Value(record, field)));
NativeRecord[] Gates(NativeMapDocument map) => map.Records().Where(record => record.Category == "GAMEGATE").ToArray();
var root = Path.GetFullPath(args.Single()); Check(!Directory.Exists(root), "Use fresh evidence."); Directory.CreateDirectory(root);
const string level = "DK2Preview_6a88240eaf814f78";
var basis = NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
var manual = Path.GetFullPath("tests/Fixtures/NativeRoundtrip/" + level + ".dkm");
var protectedFiles = basis.Files.Select(file => file.SourcePath).Append(manual).Concat(Directory.GetFiles(Path.GetDirectoryName(manual)!, "*.native-source-*.zip"))
    .Distinct().ToDictionary(path => path, path => SHA256.HashData(File.ReadAllBytes(path)));
var originalGates = Gates(basis).ToDictionary(Key, Bytes);
var project = NativeMapDocument.Load(manual);
var before = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
var compiled = project.CompileLegacyProject(level, basis);
Check(originalGates.Count == 4 && Gates(compiled).Length == 4, "Unexpected real gate count.");
foreach (var gate in Gates(compiled)) Check(Bytes(gate).AsSpan().SequenceEqual(originalGates[Key(gate)]), "Unedited official gate differs: " + Key(gate));
var exported = compiled.SaveCopy(Path.Combine(root, "compiled-user-save"));
foreach (var gate in Gates(NativeMapDocument.Load(exported))) Check(Bytes(gate).AsSpan().SequenceEqual(originalGates[Key(gate)]), "Export/reopen changed original gate.");
Check(Directory.GetFiles(Path.GetDirectoryName(exported)!).Length == 7 && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Gate retention changed seven-file output or project.");
Console.WriteLine("PASS user's DKM+ZIP retains all four whole28-byte official gates, including source-only IDs/orientation; seven-file export/reopen and immutable project");

// Independent synthetic source values: no meaning is assigned to these bytes.
var source = NativeMapDocument.Create(18, 18, "GateSource", basis);
var gateSource = source.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEGATE"), new(4, 4), 1);
Set(source, gateSource, "bGateType", "33"); Set(source, gateSource, "bGateID", "91"); Set(source, gateSource, "bGateOrientation", "6");
source.Paint([new(4, 4), new(5, 4), new(4, 5), new(5, 5)], new MapTile(33, 1, 1));
var heart = source.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEGATE"), new(11, 11), 3);
Set(source, heart, "bGateType", "14"); Set(source, heart, "bGateID", "117");
source.Paint(Enumerable.Range(9, 5).SelectMany(y => Enumerable.Range(9, 5).Select(x => new TilePoint(x, y))), new MapTile(14, 3, 1));
var synthetic = Path.Combine(root, "synthetic.dkm");
using (var save = new EditorSaveSession(source)) { var plan = save.PrepareSaveAs(synthetic); Check(!plan.HasBlockingIssues, "Synthetic source projection blocked."); save.Save(plan, true); }
NativeRecord ProjectGate(NativeMapDocument map) => map.Records().Single(record => record.Category == "Project.MAPRECTEX" && Value(record, "node.type") == "1");
NativeMapDocument BareCompile(NativeMapDocument map, string name)
{
    using var save = new EditorSaveSession(map, synthetic);
    var saved = Path.Combine(root, name + ".dkm"); save.Save(save.PrepareSaveAs(saved), true);
    var bare = Path.Combine(root, name + "-bare.dkm"); File.Copy(saved, bare);
    return NativeMapDocument.Load(bare).CompileLegacyProject(level, basis);
}
NativeRecord SyntheticGate(NativeMapDocument map) => Gates(map).Single(record => Value(record, "bGateType") != "14");
var clean = NativeMapDocument.Load(synthetic);
Check(Bytes(SyntheticGate(clean.CompileLegacyProject(level, basis))).AsSpan().SequenceEqual(Bytes(gateSource)), "Synthetic source-only values were not retained.");
var moved = NativeMapDocument.Load(synthetic); var movedGate = ProjectGate(moved);
Set(moved, movedGate, "rect.x1", "6"); Set(moved, movedGate, "rect.x2", "6");
var movedFresh = BareCompile(moved, "moved");
Check(Bytes(SyntheticGate(moved.CompileLegacyProject(level, basis))).AsSpan().SequenceEqual(Bytes(SyntheticGate(movedFresh))), "Moved gate restored obsolete source bytes.");
var footprint = NativeMapDocument.Load(synthetic); footprint.Paint([new(5, 5)], new MapTile(4, 1, 1));
var footprintFresh = BareCompile(footprint, "footprint");
Check(Bytes(SyntheticGate(footprint.CompileLegacyProject(level, basis))).AsSpan().SequenceEqual(Bytes(SyntheticGate(footprintFresh))), "Changed footprint restored source-only gate bytes despite identical generated type.");
var neighbor = NativeMapDocument.Load(synthetic); neighbor.Paint([new(5, 3)], new MapTile(4, 1, 1));
var neighborFresh = BareCompile(neighbor, "neighbor");
Check(Bytes(SyntheticGate(neighbor.CompileLegacyProject(level, basis))).AsSpan().SequenceEqual(Bytes(SyntheticGate(neighborFresh))), "Changed local terrain restored source-only gate bytes.");
var far = NativeMapDocument.Load(synthetic); far.Paint([new(1, 1)], new MapTile(4, 1, 1));
Check(Bytes(SyntheticGate(far.CompileLegacyProject(level, basis))).AsSpan().SequenceEqual(Bytes(gateSource)), "Unrelated distant paint discarded unchanged gate bytes.");
var renamed = NativeMapDocument.Load(synthetic); Set(renamed, ProjectGate(renamed), "id", "7");
var renamedFresh = BareCompile(renamed, "identity");
Check(Bytes(SyntheticGate(renamed.CompileLegacyProject(level, basis))).AsSpan().SequenceEqual(Bytes(SyntheticGate(renamedFresh))), "Changed gate identity restored source-only bytes.");
var retargeted = NativeMapDocument.Load(synthetic); retargeted.Paint([new(4, 4)], new MapTile(37, 1, 1));
var retargetedFresh = BareCompile(retargeted, "retargeted");
Check(Bytes(SyntheticGate(retargeted.CompileLegacyProject(level, basis))).AsSpan().SequenceEqual(Bytes(SyntheticGate(retargetedFresh)))
    && Value(SyntheticGate(retargetedFresh), "bGateType") == "37", "Retargeted gate restored obsolete type/orientation bytes.");
var currentAmbiguity = NativeMapDocument.Load(synthetic); currentAmbiguity.AddLegacyRecord("Gate", new(4, 4));
Check(Gates(currentAmbiguity.CompileLegacyProject(level, basis)).Where(record => Value(record, "bGateType") != "14").All(record => Value(record, "bGateID") == "1"), "Ambiguous current gates were matched by guess.");
var undo = NativeMapDocument.Load(synthetic); var undoGate = ProjectGate(undo);
Set(undo, undoGate, "rect.x1", "6"); Set(undo, undoGate, "rect.x2", "6"); undo.Undo(); undo.Undo();
Check(Bytes(SyntheticGate(undo.CompileLegacyProject(level, basis))).AsSpan().SequenceEqual(Bytes(gateSource)), "Undo failed to recover original source-only gate bytes.");
Console.WriteLine("PASS moved gates, changed identity, footprint/local terrain, unrelated paint and exact Undo guard source-only bytes");

var health = NativeMapDocument.Load(synthetic);
var options = health.Records().Where(record => record.Category == "Project.MAPOPTIONS").ElementAt(2); Set(health, options, "PlayerHealth", "77");
var healthGate = Gates(health.CompileLegacyProject(level, basis)).Single(record => Value(record, "bGateType") == "14");
Check(Value(healthGate, "wGateHealth") == "77" && Value(healthGate, "bGateID") == "117", "Source-only heart bytes overwrote new health or were discarded by unrelated health edit.");
var owner = NativeMapDocument.Load(synthetic); owner.Paint(Enumerable.Range(9, 5).SelectMany(y => Enumerable.Range(9, 5).Select(x => new TilePoint(x, y))), new MapTile(14, 4, 1));
var ownerFresh = BareCompile(owner, "owner");
var newHeart = Gates(owner.CompileLegacyProject(level, basis)).Single(record => Value(record, "bGateType") == "14");
Check(Bytes(newHeart).AsSpan().SequenceEqual(Bytes(Gates(ownerFresh).Single(record => Value(record, "bGateType") == "14"))) && Value(newHeart, "bGlobalPlayer") == "4", "Changed heart owner restored another owner's source bytes.");
var resized = NativeMapDocument.Load(synthetic); resized.Resize(20, 20, 1, 1, new MapTile(2, 2, 1)); var resizedFresh = BareCompile(resized, "resized");
Check(Gates(resized.CompileLegacyProject(level, basis)).Select(Bytes).Select(Convert.ToHexString).Order().SequenceEqual(Gates(resizedFresh).Select(Bytes).Select(Convert.ToHexString).Order()), "Resize/shift restored obsolete native gates.");
var duplicatedSource = NativeMapDocument.Create(18, 18, "AmbiguousGate", basis);
duplicatedSource.Paint([new(4, 4), new(5, 4), new(4, 5), new(5, 5)], new MapTile(33, 1, 1));
foreach (var id in new[] { 91, 92 })
{
    var gate = duplicatedSource.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEGATE"), new(4, 4), 1);
    Set(duplicatedSource, gate, "bGateType", "33"); Set(duplicatedSource, gate, "bGateID", id.ToString());
}
var ambiguousPath = Path.Combine(root, "ambiguous.dkm");
using (var save = new EditorSaveSession(duplicatedSource)) save.Save(save.PrepareSaveAs(ambiguousPath), true);
var ambiguous = NativeMapDocument.Load(ambiguousPath);
Check(Gates(ambiguous.CompileLegacyProject(level, basis)).All(record => Value(record, "bGateID") == "1"), "Ambiguous same-location source gates were matched by guess.");
Check(protectedFiles.All(pair => SHA256.HashData(File.ReadAllBytes(pair.Key)).AsSpan().SequenceEqual(pair.Value)), "Source maps or user's DKM/ZIP changed.");
Console.WriteLine("PASS fresh heart health, changed owner, resize/shift, duplicate source ambiguity and protected game inputs");
File.WriteAllText(Path.Combine(root, "actual-native-gate-retention.json"), JsonSerializer.Serialize(new { Passed = true, OriginalWholeGateRecordsPreserved = 4, SyntheticIDsAssignedNoMeaning = true, MovedAndTerrainGuardsPassed = true, OwnerHealthUndoAndAmbiguityPassed = true, SevenGameFiles = true, SourceUnchanged = true, ComputerUseUsed = false, GameLaunched = false }, new JsonSerializerOptions { WriteIndented = true }));
