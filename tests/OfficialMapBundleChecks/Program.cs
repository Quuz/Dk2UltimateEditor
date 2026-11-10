using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
Dictionary<uint, byte[]> Sections(byte[] bytes)
{
    var result = new Dictionary<uint, byte[]>();
    for (var offset = 0; offset < bytes.Length;)
    {
        Check(offset <= bytes.Length - 56, "Truncated packed header.");
        var signature = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
        var length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 8)));
        Check(length >= 56 && length <= bytes.Length - offset && !result.ContainsKey(signature), "Invalid or duplicate packed section.");
        result.Add(signature, bytes.AsSpan(offset, length).ToArray()); offset += length;
    }
    return result;
}
void Refused<T>(Action action) where T : Exception
{ try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
var root = Path.GetFullPath(args.Single()); Check(!Directory.Exists(root), "Use a fresh evidence directory."); Directory.CreateDirectory(root);
const string level = "DK2Preview_6a88240eaf814f78";
var basis = NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
var manual = Path.GetFullPath("tests/Fixtures/NativeRoundtrip/" + level + ".dkm");
var protectedFiles = basis.Files.Select(file => file.SourcePath).Append(manual)
    .Concat(Directory.GetFiles(Path.GetDirectoryName(manual)!, "*.native-source-*.zip")).Distinct().ToDictionary(path => path, Hash);
var project = NativeMapDocument.Load(manual);
var projectBefore = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
var compiled = project.CompileLegacyProject(level, basis);
var before = compiled.Files.ToDictionary(file => file, file => file.Data.ToArray());
var descriptor = compiled.SaveCopy(Path.Combine(root, "compiled"));
var maps = Path.GetDirectoryName(descriptor)!;
var expectedNames = new[] { level + ".kwd", level + "Globals.kld", level + "Map.kld", level + "Players.kld", level + "Things.kld", level + "Triggers.kld", level + "Variables.kld" };
Check(Directory.GetFiles(maps).Select(Path.GetFileName).Order().SequenceEqual(expectedNames.Order()), "Compiled map must have exactly the official seven filenames.");
var packed = Sections(File.ReadAllBytes(Path.Combine(maps, level + "Globals.kld")));
foreach (var file in compiled.Files.Where(file => LegacyGlobalSchemas.Find(file.Signature) is not null || file.Signature is 0xF0 or 0x104))
    Check(packed[file.Signature].AsSpan().SequenceEqual(file.Data.Span), "Packing changed a compiled definition family.");
var sourcePacked = Sections(basis.Files.Single(file => Path.GetFileName(file.ExportPath) == "level1Globals.kld").Data.ToArray());
foreach (var signature in new uint[] { 0x10E, 0xFA }) Check(packed[signature].AsSpan().SequenceEqual(sourcePacked[signature]), "Packing omitted or changed opaque effects/element definitions.");
Check(packed.Count == 11, "All eleven official native sections must remain in Globals.");
var reload = NativeMapDocument.Load(descriptor);
Check(reload.MissingReferences.Count == 0 && reload.Files.Count == 8, "Seven map files plus one shared catalogue must resolve.");
for (var y = 0; y < basis.Height; y++) for (var x = 0; x < basis.Width; x++) Check(reload.GetTile(x, y) == basis.GetTile(x, y), "Packing changed native cells.");
Check(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && projectBefore.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Export mutated the working documents.");
Console.WriteLine("PASS user's DKM compiles to seven official map files; all nine editable and two opaque definition families survive; complete reopen and exact 1800 cells");

reload.SetScenarioName(ScenarioNameKind.Variable, 4, "First alias after KWD reopening");
var firstAliasPath = reload.SaveCopy(Path.Combine(root, "first-alias-after-reopen"));
Check(Directory.GetFiles(Path.GetDirectoryName(firstAliasPath)!).Select(Path.GetFileName).Order().SequenceEqual(expectedNames.Order()), "First alias on a reopened official map gained an eighth game-directory file.");
Check(NativeMapDocument.Load(firstAliasPath).ReadScenarioNames().Slots.Single(slot => slot.Kind == ScenarioNameKind.Variable && slot.Index == 4).StoredName == "First alias after KWD reopening", "First alias did not reopen from external editor metadata.");
Console.WriteLine("PASS first alias added after opening a plain seven-file KWD stays outside the game map files");

var room = compiled.Records().First(record => record.Category == "Definition.Rooms");
var cost = room.Fields.First(field => field.Name == "Cost"); compiled.SetField(room, cost, (int.Parse(room.Read(cost)) + 1).ToString());
compiled.SetScenarioName(ScenarioNameKind.Variable, 2, "Saved editor alias");
var editedPath = compiled.SaveCopy(Path.Combine(root, "edited"));
var editedPacked = Sections(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(editedPath)!, level + "Globals.kld")));
Check(Directory.GetFiles(Path.GetDirectoryName(editedPath)!).Length == 7 && editedPacked[0x78].AsSpan().SequenceEqual(room.File.Data.Span), "An edited definition must stay inside Globals with exactly seven map files.");
Check(NativeMapDocument.Load(editedPath).ReadScenarioNames().Slots.Single(slot => slot.Kind == ScenarioNameKind.Variable && slot.Index == 2).StoredName == "Saved editor alias", "Editor metadata outside Maps was lost on reopening.");
Console.WriteLine("PASS changed room definitions stay in packed Globals; alias metadata reopens outside the seven game files");

var imported = NativeMapDocument.Load(editedPath);
var importedRoom = imported.Records().First(record => record.Category == "Definition.Rooms");
var importedCost = importedRoom.Fields.First(field => field.Name == "Cost");
imported.SetField(importedRoom, importedCost, (int.Parse(importedRoom.Read(importedCost)) + 1).ToString());
imported.SetScenarioName(ScenarioNameKind.Timer, 3, "Reopened timer");
var importedBytes = imported.Files.ToDictionary(file => file.ExportPath, file => file.Data.ToArray());
var resavedPath = imported.SaveCopy(Path.Combine(root, "reopened-edited"));
Check(Directory.GetFiles(Path.GetDirectoryName(resavedPath)!).Select(Path.GetFileName).Order().SequenceEqual(expectedNames.Order()), "Reopened official map gained an eighth game-directory file.");
var resaved = NativeMapDocument.Load(resavedPath);
foreach (var file in resaved.Files) Check(file.Data.Span.SequenceEqual(importedBytes[file.ExportPath]), "Resaving a reopened official map changed native bytes or the descriptor.");
Check(resaved.ReadScenarioNames().Slots.Single(slot => slot.Kind == ScenarioNameKind.Timer && slot.Index == 3).StoredName == "Reopened timer", "Reopened alias edit was lost.");
var resavedAgain = resaved.SaveCopy(Path.Combine(root, "reopened-again"));
Check(Directory.GetFiles(Path.GetDirectoryName(resavedAgain)!).Length == 7, "Second reopening gained an eighth map file.");
Console.WriteLine("PASS native KWD reopen/edit/resave twice retains seven files, exact native bytes, packed room edits and editor aliases");

var officialSource = NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
var officialBefore = officialSource.Files.ToDictionary(file => file.ExportPath, file => file.Data.ToArray());
officialSource.SetScenarioName(ScenarioNameKind.Timer, 5, "Original map timer");
var officialAliasPath = officialSource.SaveCopy(Path.Combine(root, "original-official-alias"));
Check(Directory.GetFiles(Path.GetDirectoryName(officialAliasPath)!).Length == 7, "First alias on an installed official map gained an eighth game-directory file.");
var officialReload = NativeMapDocument.Load(officialAliasPath);
foreach (var file in officialReload.Files) Check(file.Data.Span.SequenceEqual(officialBefore[file.ExportPath]), "Alias export changed original official game bytes.");
Check(officialReload.ReadScenarioNames().Slots.Single(slot => slot.Kind == ScenarioNameKind.Timer && slot.Index == 5).StoredName == "Original map timer", "Original map alias was lost.");
Console.WriteLine("PASS original official level1 first-alias export/reopen keeps seven map files and every original game byte");

var fakeGame = Path.Combine(root, "fake-game"); var fakeMaps = Path.Combine(fakeGame, "Data", "Editor", "Maps"); Directory.CreateDirectory(fakeMaps);
var sharedSource = basis.Files.Single(file => Path.GetFileName(file.ExportPath) == "GlobalVariables.kwd");
var sharedPath = Path.Combine(fakeGame, sharedSource.ExportPath); Directory.CreateDirectory(Path.GetDirectoryName(sharedPath)!); File.WriteAllBytes(sharedPath, sharedSource.Data.ToArray());
var sharedBefore = Hash(sharedPath);
var preview = compiled.PrepareGamePreview(level); Check(preview.FileCount == 7, "Game preview must contain seven files.");
var published = preview.Install(fakeGame);
Check(published.LevelName == level && published.CreatedFiles.Count == 7 && published.CreatedFiles[^1] == published.DescriptorPath, "Fixed-name seven-file preview must publish descriptor last.");
Check(Directory.GetFiles(fakeMaps).Select(Path.GetFileName).Order().SequenceEqual(expectedNames.Order()), "Preview did not use official names.");
var publishedReload = NativeMapDocument.Load(published.DescriptorPath);
Check(publishedReload.MissingReferences.Count == 0 && Hash(sharedPath) == sharedBefore, "Preview must resolve and never replace shared game data.");
var existing = Directory.GetFiles(fakeMaps).ToDictionary(path => path, Hash);
Refused<IOException>(() => preview.Install(fakeGame)); Check(existing.All(pair => Hash(pair.Key) == pair.Value), "Collision changed existing map files.");
var badGame = Path.Combine(root, "wrong-game"); Directory.CreateDirectory(Path.Combine(badGame, "Data", "Editor", "Maps"));
var badShared = Path.Combine(badGame, sharedSource.ExportPath); Directory.CreateDirectory(Path.GetDirectoryName(badShared)!); File.WriteAllText(badShared, "Different installation catalogue");
Refused<InvalidDataException>(() => preview.Install(badGame)); Check(Directory.GetFiles(Path.Combine(badGame, "Data", "Editor", "Maps")).Length == 0, "Shared mismatch created partial map files.");
Refused<ArgumentException>(() => compiled.PrepareGamePreview("../bad"));
Check(protectedFiles.All(pair => Hash(pair.Key) == pair.Value), "Source maps or user's DKM/ZIP changed.");
Console.WriteLine("PASS fixed-name seven-file preview, descriptor last, shared catalogue validation, source immutability and collision guards");
File.WriteAllText(Path.Combine(root, "actual-official-bundle.json"), JsonSerializer.Serialize(new { Passed = true, Level = level, MapFiles = expectedNames, PackedSections = packed.Keys.Select(signature => signature.ToString("X")).ToArray(), SharedCatalogueUnchanged = true, SourceUnchanged = true, ComputerUseUsed = false, GameLaunched = false, OfficialEditorOpened = false }, new JsonSerializerOptions { WriteIndented = true }));
