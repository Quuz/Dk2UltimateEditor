using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static class CameraPathChecks
{
    internal static string Projection(NativeMapDocument document) => string.Join("|", document.Records().Where(CameraPaths.IsCameraAction)
        .Select(record => record.File.ExportPath + ":" + record.Offset + ":" + Convert.ToHexString(record.File.Data.Slice(record.Offset, record.Length).Span)));
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Source"))) root = Path.GetDirectoryName(root) ?? throw new Exception("Source missing.");
        var catalogue = File.ReadAllText(Path.Combine(root, "Source/Sources/Datas/Datas_CameraPaths.c"));
        var expected = Regex.Matches(catalogue, @"\{\s*(\d+),\s*(\d+),\s*szCameraPath\d+,(TRUE|FALSE)\}")
            .Select(match => (ID: int.Parse(match.Groups[1].Value), Show: match.Groups[3].Value == "TRUE")).ToArray();
        Expect(expected.Length == 52 && expected.Count(item => item.Show) == 24
            && CameraPaths.Choices.Select(choice => (choice.ID, choice.ShowCell)).SequenceEqual(expected), "Source camera catalogue/defaults differ.");
        foreach (var choice in CameraPaths.Choices)
        {
            Expect(choice.AreaMode == (choice.ShowCell ? 0 : 1) && choice.English.Length > 20 && choice.Russian.Length > 20, "Missing translated camera description or inverted cell mode.");
            var bmp = File.ReadAllBytes(Path.Combine(root, "Source/Bitmaps/Chemins de caméra/Chemin n°" + choice.ID.ToString("D3") + ".bmp"));
            Expect(bmp[0] == 'B' && bmp[1] == 'M' && BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(18)) == 256
                && Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(22))) == 192, "Preserved camera diagram dimensions differ.");
        }
        var caller = File.ReadAllText(Path.Combine(root, "Source/Sources/Triggers/Triggers_Actions.c"));
        Expect(caller.Contains("(CameraPath.ShowCell)?200:201") && caller.Contains("Context->ActionCtx.TempAction.param3 = 0;"), "Source cell/mode inversion unverified.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2CameraPaths-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try { foreach (var native in new[] { false, true }) CheckDocument(directory, native); }
        finally { Directory.Delete(directory, true); }
    }
    private static void CheckDocument(string directory, bool native)
    {
        var document = NativeMapDocument.CreateNewMap(new("CameraTest", "CameraTest", 20, 20, NativeFormat: native, PlaceHearts: true));
        NativeRecord Add() => native ? document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "Action"), new(2,2)) : document.AddLegacyRecord("Action", new(2,2));
        var action = Add(); document.SetEventType(action, LegacyEventSchemas.All.Single(type => type.Name == "ACTION_CAMERAPATH"));
        action = document.Records().Single(item => item.File == action.File && item.Offset == action.Offset);
        void Set(int parameter, string value) => document.SetField(action, action.Fields.Single(field => field.Name == CameraPaths.FieldName(action, parameter)), value);
        Set(1, native ? "255" : "9999"); Set(2, "7"); Set(3, "9");
        if (native)
            document.Edit("Retain opaque camera bytes", () =>
            {
                Expect(MemoryMarshal.TryGetArray(action.File.Data, out var memory), "Fixture storage missing.");
                for (var offset = 11; offset <= 15; offset++) memory.Array![memory.Offset + action.Offset + offset] = (byte)(0xA0 + offset);
            });
        var events = 0; document.Changed += (_, _) => events++;
        foreach (var choice in CameraPaths.Choices)
        {
            var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); var draft = document.CreateCameraPathDraft(action); var startingEvents = events;
            Expect(draft.PathID == (native ? 255 : 9999), "Opening camera draft normalized unknown input.");
            document.ApplyCameraPath(draft, choice.ID);
            foreach (var file in document.Files)
            {
                var expected = before[file].ToArray();
                if (file == action.File)
                {
                    var id = action.Fields.Single(field => field.Name == CameraPaths.FieldName(action, 1));
                    var mode = action.Fields.Single(field => field.Name == CameraPaths.FieldName(action, 3));
                    id.Encode(choice.ID.ToString()).CopyTo(expected, action.Offset + id.Offset); mode.Encode(choice.AreaMode.ToString()).CopyTo(expected, action.Offset + mode.Offset);
                }
                Expect(file.Data.Span.SequenceEqual(expected), "Camera selection changed area/links/IDs/opaque or unrelated bytes.");
            }
            Expect(events == startingEvents + 1, "Camera selection was not one atomic edit.");
            var changed = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); document.Undo();
            foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Camera path undo not exact.");
            document.Redo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(changed[file]), "Camera path redo not exact.");
            var noOpEvents = events; document.ApplyCameraPath(document.CreateCameraPathDraft(action), choice.ID); Expect(events == noOpEvents, "Same camera selection created history/event.");
            var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(directory, $"{native}-{choice.ID}")); var reopened = NativeMapDocument.Load(saved);
            // Project MainFile is the original serialized envelope; live edits
            // reside in its separate sections. Compare those, then the complete
            // regenerated envelope across a second unchanged save.
            if(!native)SaveProjectionChecks.RoundTrip(document,reopened,changed.ToDictionary(pair=>pair.Key.ExportPath,pair=>pair.Value));
            else foreach(var file in reopened.Files)Expect(file.Data.Span.SequenceEqual(changed.Single(pair=>pair.Key.ExportPath==file.ExportPath).Value),"Native camera save changed exact editable bytes.");
            var savedAgain = SaveProjectionChecks.SaveCopy(reopened, Path.Combine(directory, $"{native}-{choice.ID}-again"));
            Expect(native ? File.ReadAllBytes(saved).AsSpan().SequenceEqual(File.ReadAllBytes(savedAgain)) : SaveProjectionChecks.BeforeEquals(reopened,File.ReadAllBytes(saved)), "Complete unchanged camera document serialization changed bytes.");
            if (!native)
            {
                var compiled = document.CompileLegacyProject("CameraCompiled", NativeMapDocument.Create(3,3,"Basis"));
                var compiledAction = compiled.Records().Single(item => CameraPaths.IsCameraAction(item));
                Expect(compiled.CreateCameraPathDraft(compiledAction).PathID == choice.ID && compiledAction.Read(compiledAction.Fields.Single(field => field.Name == "Param3")) == choice.AreaMode.ToString(), "Project camera choice compiled incorrectly.");
            }
            document.Undo();
        }
        var stale = document.CreateCameraPathDraft(action); Set(2, "8"); var snapshot = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); var failures = events;
        Reject(() => document.ApplyCameraPath(stale, 50)); Reject(() => document.ApplyCameraPath(document.CreateCameraPathDraft(action), 999));
        Reject(() => NativeMapDocument.Create(3,3,"Foreign").ApplyCameraPath(stale,50));
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]), "Rejected camera draft changed bytes.");
        Expect(events == failures, "Rejected camera selection created event/history.");
        document.Undo(); document.ApplyCameraPath(stale, 50); document.Undo();
        var other = Add(); document.SetEventType(other, LegacyEventSchemas.All.Single(type => type.Name == "ACTION_ROTATECAMERA"));
        other = document.Records().Single(item => item.File == other.File && item.Offset == other.Offset);
        Expect(!CameraPaths.IsCameraAction(other), "Unrelated camera action got a path chooser."); Reject(() => document.CreateCameraPathDraft(other));
    }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { return; }
        throw new Exception("Invalid/stale camera action accepted.");
    }
}
