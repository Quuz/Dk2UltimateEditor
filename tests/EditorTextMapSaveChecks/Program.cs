using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

var directory = Path.Combine(Path.GetTempPath(), "DK2TextMapSaveChecks", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
void Refuses<T>(Action action) where T : Exception
{ try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
var fixture = 0;
NativeMapDocument Clean(bool native)
{
    var created = NativeMapDocument.CreateNewMap(new("TextSave", "TextSave", 16, 16, NativeFormat: native));
    return NativeMapDocument.Load(created.SaveCopy(Path.Combine(directory, "fixture-" + fixture++), temporary: true));
}
Dictionary<NativeFile, byte[]> Bytes(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
bool Same(NativeMapDocument doc, Dictionary<NativeFile, byte[]> before) => before.Count == doc.Files.Count && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value));
void EqualPaint(NativeMapDocument doc) => doc.EditEditorDrawing("Equal", () => new(true, 1));
void Version(NativeMapDocument doc, string value)
{
    var draft = doc.CreateMapGeneralDraft();
    doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, draft.Values.Select(pair => new MapGeneralInput(pair.Key, pair.Key == "Version" ? value : pair.Value))));
}
try
{
    foreach (var native in new[] { false, true })
    {
        var kind = native ? "native" : "project";
        var doc = Clean(native); var session = new EditorTextMapSaveSession(doc);
        var path = Path.Combine(directory, kind + ".map");
        EqualPaint(doc); var before = Bytes(doc); var revision = doc.EditRevision; var history = doc.GetHistory();
        var statistics = doc.ReadActivityStatistics(); var draft = doc.CreateScenarioNameDraft(ScenarioNameKind.Variable, 0);
        var plan = session.PrepareSaveAs(path);
        Expect(plan.NeedsLossConsent && plan.Losses.Any(loss => loss.Code == "AdditionalSettings") && !File.Exists(path)
            && Same(doc, before) && ReferenceEquals(doc.ReadActivityStatistics(), statistics) && doc.EditRevision == revision && doc.CanUndo,
            "MAP preparation wrote or changed state, or failed to expose loss report.");
        Expect(!session.Save(plan, acceptLosses: false) && session.TargetPath is null && doc.GetHistory().SnapshotBytes == history.SnapshotBytes && doc.HasChanges,
            "Declined loss report changed target/history/dirty state.");
        var changes = 0; doc.Changed += (_, _) => changes++;
        Expect(session.Save(plan, acceptLosses: true) && File.Exists(path) && session.TargetPath == Path.GetFullPath(path)
            && Same(doc, before) && !doc.HasChanges && !doc.CanUndo && !doc.CanRedo && doc.EditorChangeCount == 0
            && doc.GetHistory().SnapshotBytes == 0 && doc.EditRevision == revision + 1 && changes == 1
            && ReferenceEquals(doc.ReadActivityStatistics(), statistics), "Normal MAP Save changed DKM statistics or failed baseline/history/target reset.");
        Refuses<InvalidOperationException>(() => doc.ApplyScenarioNameDraft(draft, "stale"));
        var parsed = TextMapFile.Read(path); var loaded = NativeMapDocument.Load(path);
        Expect(parsed.ReadTiles().Width == doc.Width && parsed.ReadTiles().Height == doc.Height
            && loaded.GetTile(3, 3) == doc.GetTile(3, 3) && !loaded.HasChanges && !loaded.CanUndo && loaded.EditorChangeCount == 0,
            "Normal MAP load/reopen was marked as an unsaved binary import or changed tiles.");
        var loadedSession = new EditorTextMapSaveSession(loaded, path);
        Expect(loadedSession.TargetPath == Path.GetFullPath(path) && !loadedSession.CanSave, "Loaded MAP did not retain clean normal target.");
        Expect(NativeMapDocument.ImportTextMap(path).HasChanges, "Explicit binary-project import lost its separate unsaved semantics.");
        Console.WriteLine($"PASS {kind} MAP loss preparation/decline, normal Save/reopen, clean loaded target and no DKM statistics side effects");

        EqualPaint(doc); before = Bytes(doc); revision = doc.EditRevision; statistics = doc.ReadActivityStatistics(); var oldFile = File.ReadAllBytes(path);
        var oldPlan = session.PrepareSave(); EqualPaint(doc);
        Refuses<InvalidOperationException>(() => session.Save(oldPlan, true));
        plan = session.PrepareSave(); revision = doc.EditRevision;
        Refuses<IOException>(() => session.Save(plan, true, _ => throw new IOException("before creation")));
        Refuses<IOException>(() => session.Save(plan, true, staging => new FailWriteStream(new FileStream(staging, FileMode.CreateNew))));
        Expect(Same(doc, before) && ReferenceEquals(doc.ReadActivityStatistics(), statistics) && doc.HasChanges && doc.CanUndo
            && doc.EditRevision == revision && File.ReadAllBytes(path).AsSpan().SequenceEqual(oldFile), "MAP early/late failure reset state, changed statistics or damaged old file.");
        Refuses<ArgumentException>(() => session.PrepareSaveAs(Path.Combine(directory, "wrong.dkm")));
        var wrongOwner = new EditorTextMapSaveSession(doc);
        Refuses<InvalidOperationException>(() => wrongOwner.Save(plan, true));
        Console.WriteLine($"PASS {kind} MAP stale consent, wrong owner/path and early/late failures preserve exact state/old target");

        Expect(session.Save(plan, true, staging =>
        {
            Refuses<InvalidOperationException>(() => doc.Undo()); Refuses<InvalidOperationException>(() => doc.Redo());
            Refuses<InvalidOperationException>(() => EqualPaint(doc));
            Refuses<InvalidOperationException>(() => session.PrepareSave());
            Refuses<InvalidOperationException>(() => session.Save(plan, true));
            return new FileStream(staging, FileMode.CreateNew);
        }), "Outer MAP save failed after rejecting reentry.");
        var tile = doc.GetTile(3, 3); doc.Paint([new(3, 3)], tile with { Owner = tile.Owner == 2 ? (byte)3 : (byte)2 });
        Expect(doc.HasChanges && session.CanSave, "Post-MAP-save edit did not enable Save.");
        doc.Undo(); Expect(!doc.HasChanges && !session.CanSave, "Post-MAP-save Undo lost the saved baseline.");
        EqualPaint(doc); plan = session.PrepareSave(); oldFile = File.ReadAllBytes(path);
        Refuses<ObjectDisposedException>(() => session.Save(plan, true, staging => { session.Dispose(); return new FileStream(staging, FileMode.CreateNew); }));
        Expect(doc.HasChanges && doc.CanUndo && File.ReadAllBytes(path).AsSpan().SequenceEqual(oldFile), "Closed MAP owner published or reset state.");
        Console.WriteLine($"PASS {kind} MAP write guards, post-save edit/Undo and closed owner preserve working data");
    }

    var metadata = Clean(true); var metadataSession = new EditorTextMapSaveSession(metadata);
    Version(metadata, "16"); metadata.SetScenarioName(ScenarioNameKind.Variable, 0, "Preserved working alias");
    var metadataBefore = Bytes(metadata);
    var metadataPlan = metadataSession.PrepareSaveAs(Path.Combine(directory, "metadata.map"));
    Expect(metadataSession.Save(metadataPlan, true) && !metadata.HasChanges && metadata.TargetGameVersion == 16 && Same(metadata, metadataBefore)
        && metadata.ReadScenarioNames().Slots.Single(slot => slot.Kind == ScenarioNameKind.Variable && slot.Index == 0).StoredName == "Preserved working alias",
        "MAP save failed to adopt native target/alias working baseline without deleting hidden data.");
    using var properties = metadata.CreateMapPropertiesSession();
    Expect(!properties.WorkingDocument.HasChanges && properties.WorkingDocument.TargetGameVersion == 16, "Properties clone did not capture native saved working baseline.");
    Version(metadata, "13"); metadata.Undo();
    Expect(metadata.TargetGameVersion == 16 && !metadata.HasChanges, "Native target Undo did not restore MAP-save baseline.");
    Expect(!Directory.EnumerateFiles(directory, ".dk2-map-save-*").Any(), "MAP write leaked generated staging file.");
    Console.WriteLine("PASS native MAP-save alias/target baseline, exact working bytes, properties clone and subsequent metadata Undo");
    Console.WriteLine("EDITOR TEXT MAP SAVE CHECKS PASSED: native DKM conversion/window integration/full regression remain pending");
}
finally
{
    var allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DK2TextMapSaveChecks")) + Path.DirectorySeparatorChar;
    var resolved = Path.GetFullPath(directory);
    if (!resolved.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new IOException("Refuse cleanup outside private MAP fixtures.");
    if (Directory.Exists(resolved)) Directory.Delete(resolved, recursive: true);
}
sealed class FailWriteStream(Stream inner) : Stream
{
    public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true;
    public override long Length => inner.Length;
    public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
    public override void Flush() => inner.Flush();
    public override void Write(byte[] bytes, int offset, int count) { inner.Write(bytes, offset, Math.Min(count, 4)); throw new IOException("After creation"); }
    public override int Read(byte[] bytes, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
}
