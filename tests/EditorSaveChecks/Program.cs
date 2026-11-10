using System.Buffers.Binary;
using System.Reflection;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

var directory = Path.Combine(Path.GetTempPath(), "DK2EditorSaveChecks", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
void Refuses<T>(Action action) where T : Exception
{ try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
var fixture = 0;
NativeMapDocument Clean()
{
    var created = NativeMapDocument.CreateNewMap(new("SaveCheck", "SaveCheck", 16, 16));
    var path = created.SaveCopy(Path.Combine(directory, "fixture-" + fixture++), temporary: true);
    var doc = NativeMapDocument.Load(path);
    Expect(doc.IsLegacyProject && !doc.HasChanges, "Loaded DKM fixture is not clean.");
    return doc;
}
void EqualPaint(NativeMapDocument doc)
    => doc.EditEditorDrawing("Equal paint", () => new(true, 1));
Dictionary<NativeFile, byte[]> Bytes(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
bool Same(NativeMapDocument doc, Dictionary<NativeFile, byte[]> bytes) => bytes.Count == doc.Files.Count && bytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value));
try
{
    // Literal expected callback sequences exercise source boundary and failure
    // behavior, independently of elapsed wall time or a mocked passing writer.
    var timer = new EditorAutoSaveTimer { IntervalMinutes = 1 }; var attempts = 0;
    for (var n = 1; n <= 60; n++) Expect(!timer.Tick(false, 1, () => attempts++), "Autosave fired before callback61.");
    Expect(timer.Ticks == 60 && attempts == 0, "One-minute countdown differs.");
    Expect(timer.Tick(false, 1, () => attempts++) && attempts == 1 && timer.Ticks == 0, "Callback61 did not save/reset.");
    timer.IntervalMinutes = 0;
    for (var n = 0; n < 100; n++) Expect(!timer.Tick(false, 1, () => attempts++), "Disabled autosave wrote.");
    Expect(timer.Ticks == 0 && attempts == 1, "Disabled timer advanced.");
    timer.IntervalMinutes = 5;
    for (var n = 0; n < 300; n++) Expect(!timer.Tick(false, 1, () => attempts++), "Default fired before301.");
    Expect(timer.Tick(false, 1, () => attempts++) && attempts == 2, "Default callback301 differs.");
    Console.WriteLine("PASS autosave disabled, one-minute callback61 and default callback301 boundaries");

    timer = new() { IntervalMinutes = 1 }; attempts = 0;
    for (var n = 0; n < 60; n++) timer.Tick(false, 1, () => attempts++);
    Expect(!timer.Tick(true, 1, () => attempts++) && timer.Ticks == 61, "Locked due tick was reset.");
    Expect(!timer.Tick(true, 1, () => attempts++) && timer.Ticks == 62, "Second locked due tick differs.");
    Expect(timer.Tick(false, 1, () => attempts++) && timer.Ticks == 0 && attempts == 1, "Unlock did not save overdue state.");
    for (var n = 0; n < 60; n++) timer.Tick(false, 1, () => attempts++);
    Expect(!timer.Tick(false, 0, () => attempts++) && timer.Ticks == 0 && attempts == 1, "Clean due did not reset without writing.");
    for (var n = 0; n < 60; n++) timer.Tick(false, 1, () => attempts++);
    Refuses<IOException>(() => timer.Tick(false, 1, () => { attempts++; throw new IOException("late failure"); }));
    Expect(timer.Ticks == 0 && attempts == 2 && !timer.Tick(false, 1, () => attempts++), "Failed autosave immediately retried.");
    for (var n = 1; n < 60; n++) timer.Tick(false, 1, () => attempts++);
    Expect(timer.Tick(false, 1, () => { Refuses<InvalidOperationException>(() => timer.Tick(false, 1, () => attempts++)); attempts++; }) && attempts == 3,
        "Nested autosave changed timer/writer state.");
    timer.Reset(); Expect(timer.Ticks == 0, "Lifecycle reset did not clear timer.");
    Console.WriteLine("PASS autosave lock deferral, clean due reset, failed-write interval and nested callback refusal");

    var doc = Clean(); var session = new EditorProjectSaveSession(doc);
    var path = Path.Combine(directory, "normal.dkm"); var before = Bytes(doc); var revision = doc.EditRevision;
    Expect(session.TargetPath is null && !session.CanSave, "New session invented normal target.");
    Refuses<InvalidOperationException>(() => session.Save());
    EqualPaint(doc); Expect(doc.HasChanges && doc.CanUndo && doc.EditorChangeCount == 1, "Byte-equal paint did not set source dirty state.");
    for (var n = 0; n < 12; n++) session.AutoSave.Tick(false, 1, () => throw new Exception("Not due"));
    var count = doc.ReadActivityStatistics()!.SavedTimes; var historyNotices = 0; var changes = 0;
    var staleNames = doc.CreateScenarioNameDraft(ScenarioNameKind.Variable, 0);
    doc.HistoryChanged += (_, _) => historyNotices++; doc.Changed += (_, _) => changes++;
    session.SaveAs(path);
    Expect(File.Exists(path) && session.TargetPath == Path.GetFullPath(path) && !session.CanSave
        && !doc.HasChanges && !doc.CanUndo && !doc.CanRedo && doc.EditorChangeCount == 0
        && doc.GetHistory().ChangeCount == 0 && doc.GetHistory().SnapshotBytes == 0
        && Same(doc, before) && doc.EditRevision == revision + 2 && historyNotices == 1 && changes == 1 && session.AutoSave.Ticks == 0,
        "Normal Save lost bytes or did not reset source/history/lifetime exactly once.");
    var reopened = NativeMapDocument.Load(path);
    Refuses<InvalidOperationException>(() => doc.ApplyScenarioNameDraft(staleNames, "Stale after Save"));
    Expect(reopened.ReadStoredActivityStatistics()!.SavedTimes == unchecked(count + 1) && doc.ReadActivityStatistics()!.SavedTimes == unchecked(count + 1), "Normal DKM statistics were not written.");
    var tile = doc.GetTile(3, 3); doc.Paint([new(3, 3)], tile with { Owner = tile.Owner == 2 ? (byte)3 : (byte)2 });
    Expect(doc.HasChanges && session.CanSave, "Post-save edit did not enable normal Save.");
    doc.Undo(); Expect(!doc.HasChanges && !session.CanSave && doc.CanRedo, "Post-save Undo did not restore saved baseline.");
    session.Save(); Expect(!doc.CanRedo && doc.GetHistory().SnapshotBytes == 0, "Successful normal save did not discard redo.");
    Console.WriteLine("PASS ordinary DKM SaveAs/reopen, equal-paint dirty reset, history/lifetime notices and saved baseline");

    doc = Clean(); session = new(doc, path); EqualPaint(doc); before = Bytes(doc); revision = doc.EditRevision;
    var oldFile = File.ReadAllBytes(path); var oldCount = doc.ReadActivityStatistics()!.SavedTimes;
    Refuses<IOException>(() => session.Save(_ => throw new IOException("before creation")));
    Expect(doc.ReadActivityStatistics()!.SavedTimes == oldCount && Same(doc, before) && doc.EditRevision == revision && doc.CanUndo && doc.HasChanges
        && File.ReadAllBytes(path).AsSpan().SequenceEqual(oldFile) && session.TargetPath == Path.GetFullPath(path), "Early write failure reset state or replaced target.");
    Refuses<IOException>(() => session.Save(staging => new FailWriteStream(new FileStream(staging, FileMode.CreateNew))));
    Expect(doc.ReadActivityStatistics()!.SavedTimes == unchecked(oldCount + 1) && Same(doc, before) && doc.EditRevision == revision && doc.CanUndo && doc.HasChanges
        && File.ReadAllBytes(path).AsSpan().SequenceEqual(oldFile), "Late normal failure lost source statistics/history or old target.");
    Refuses<ArgumentException>(() => session.SaveAs(Path.Combine(directory, "wrong.map")));
    Refuses<DirectoryNotFoundException>(() => session.SaveAs(Path.Combine(directory, "missing", "map.dkm")));
    Expect(!Directory.EnumerateFiles(directory, ".dk2-save-*").Any(), "Failed save leaked generated staging file.");
    Console.WriteLine("PASS ordinary DKM early/late failures, path refusal, preserved old target and generated-file cleanup");

    doc = Clean(); session = new(doc, path); EqualPaint(doc); before = Bytes(doc); revision = doc.EditRevision;
    count = doc.ReadActivityStatistics()!.SavedTimes; var history = doc.GetHistory();
    uint saveTicks = 100;
    var saveTime = new ActivityLocalTime(2026, 10, 6, 10, 12, 30, 40, 123);
    var statisticsField = typeof(NativeMapDocument).GetField("activityStatistics", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var importedStatistics = doc.ReadActivityStatistics()!;
    (statisticsField.GetValue(doc) as IDisposable)!.Dispose();
    statisticsField.SetValue(doc, new ActivityStatisticsSession(importedStatistics, () => saveTicks, () => saveTime));
    saveTicks = 350;
    var recovery = Path.Combine(directory, "Autosave.dkm");
    session.SaveTemporary(recovery);
    Expect(session.TargetPath == Path.GetFullPath(path) && doc.ReadActivityStatistics()!.SavedTimes == count
        && Same(doc, before) && doc.EditRevision == revision && doc.HasChanges && doc.CanUndo
        && doc.GetHistory().Undo.Count == history.Undo.Count && doc.GetHistory().SnapshotBytes == history.SnapshotBytes,
        "Temporary DKM changed target/history/bytes/lifetime or retained saved count.");
    var temporaryStatistics = NativeMapDocument.Load(recovery).ReadStoredActivityStatistics()!;
    Expect(temporaryStatistics.SavedTimes == unchecked(count + 1) && temporaryStatistics.LastSaved == saveTime
        && temporaryStatistics.RealTicks == unchecked(importedStatistics.RealTicks + 250)
        && doc.ReadActivityStatistics()!.LastSaved == saveTime && doc.ReadActivityStatistics()!.RealTicks == temporaryStatistics.RealTicks,
        "Temporary output/runtime lost attempted count, literal local date or elapsed time.");
    saveTicks = 450; saveTime = saveTime with { Minute = 31 };
    Refuses<IOException>(() => session.SaveTemporary(recovery, staging => new FailWriteStream(new FileStream(staging, FileMode.CreateNew))));
    Expect(doc.ReadActivityStatistics()!.SavedTimes == count && NativeMapDocument.Load(recovery).ReadStoredActivityStatistics()!.SavedTimes == unchecked(count + 1)
        && doc.HasChanges && doc.CanUndo && Same(doc, before) && doc.EditRevision == revision
        && doc.ReadActivityStatistics()!.LastSaved == saveTime && doc.ReadActivityStatistics()!.RealTicks == unchecked(importedStatistics.RealTicks + 350),
        "Failed temporary save reset state, discarded local time effects or retained runtime increment.");
    Console.WriteLine("PASS temporary DKM reopen, saved-count restoration, late failure and exact history/target preservation");

    doc = Clean(); session = new(doc); session.AutoSave.IntervalMinutes = 1;
    recovery = Path.Combine(directory, "autosave-tick.dkm"); EqualPaint(doc);
    for (var n = 0; n < 60; n++) Expect(!session.TickAutoSave(recovery), "Actual autosave wrote before61.");
    Expect(!File.Exists(recovery) && session.TickAutoSave(recovery) && File.Exists(recovery)
        && doc.HasChanges && doc.EditorChangeCount == 1 && doc.CanUndo && session.TargetPath is null, "Actual due DKM autosave was not temporary.");
    doc.Undo(); File.Delete(recovery);
    for (var n = 0; n < 61; n++) Expect(!session.TickAutoSave(recovery), "Clean source count0 autosaved.");
    Expect(!File.Exists(recovery) && session.AutoSave.Ticks == 0, "Clean source state wrote recovery or failed reset.");
    Console.WriteLine("PASS actual DKM autosave callback, equal source paint eligibility and clean due no-write");

    doc = Clean(); session = new(doc); EqualPaint(doc); before = Bytes(doc); revision = doc.EditRevision;
    path = Path.Combine(directory, "guarded.dkm");
    session.SaveAs(path, staging =>
    {
        Refuses<InvalidOperationException>(() => doc.Undo());
        Refuses<InvalidOperationException>(() => doc.Redo());
        Refuses<InvalidOperationException>(() => doc.SetHistoryDepth(0));
        Refuses<InvalidOperationException>(() => EqualPaint(doc));
        Refuses<InvalidOperationException>(() => session.SaveAs(path));
        Refuses<InvalidOperationException>(() => doc.SaveCopy(Path.Combine(directory, "nested-export")));
        return new FileStream(staging, FileMode.CreateNew);
    });
    Expect(Same(doc, before) && !doc.HasChanges && doc.EditRevision == revision + 1, "Save guard damaged outer save.");
    EqualPaint(doc); oldFile = File.ReadAllBytes(path);
    Refuses<ObjectDisposedException>(() => session.SaveAs(path, staging => { session.Dispose(); return new FileStream(staging, FileMode.CreateNew); }));
    Expect(doc.HasChanges && doc.CanUndo && File.ReadAllBytes(path).AsSpan().SequenceEqual(oldFile), "Closed owner published or reset save.");
    Refuses<ObjectDisposedException>(() => session.Save());
    var native = NativeMapDocument.CreateNewMap(new("Native", "Native", 16, 16, NativeFormat: true));
    var nativeSession = new EditorProjectSaveSession(native);
    Refuses<InvalidOperationException>(() => nativeSession.SaveAs(Path.Combine(directory, "native.dkm")));
    Expect(!File.Exists(Path.Combine(directory, "native.dkm")), "Unimplemented native conversion silently substituted output.");
    Console.WriteLine("PASS DKM save edit/reentry/closed-owner guards and explicit pending native conversion");

    var originalProject = LegacySectionFile.Read(path);
    var opaque = new LegacySection(0x11223344, 9, new byte[] { 0, 255, 17, 39, 128 });
    var missingThingsPath = Path.Combine(directory, "missing-things.dkm");
    File.WriteAllBytes(missingThingsPath, LegacySectionFile.Create(originalProject.Sections.Where(section => section.Signature != NativeMapDocument.LegacyThings).Append(opaque)).ToArray());
    doc = NativeMapDocument.Load(missingThingsPath); session = new(doc);
    doc.AddLegacyRecord("Creature", new(4, 5), 3);
    var addedThings = doc.Files.Single(file => file.Signature == NativeMapDocument.LegacyThings).Data.Span[12..].ToArray();
    var materializedPath = Path.Combine(directory, "materialized.dkm"); session.SaveAs(materializedPath);
    session.Save();
    var twice = LegacySectionFile.Read(materializedPath);
    Expect(twice.Sections.Single(section => section.Signature == NativeMapDocument.LegacyThings).Payload.Span.SequenceEqual(addedThings),
        "Second Save lost a materialized optional things section.");
    var retainedOpaque = twice.Sections.Single(section => section.Signature == opaque.Signature);
    var originalOpaqueIndex = LegacySectionFile.Read(missingThingsPath).Sections.Count - 1;
    Expect(retainedOpaque.Version == 9 && retainedOpaque.Payload.Span.SequenceEqual(opaque.Payload.Span)
        && Array.FindIndex(twice.Sections.ToArray(), section => section.Signature == opaque.Signature) == originalOpaqueIndex,
        "Second Save changed opaque bytes or its original position. A newly added optional section may follow it.");
    var opaqueBase = originalProject.Sections.Where(section => section.Signature != ActivityStatisticsPayload.Signature).ToArray();
    foreach (var variant in new[] { "future-first", "short-first", "missing" })
    {
        var extras = new List<LegacySection> { opaque };
        if (variant != "missing")
        {
            extras.Add(new(ActivityStatisticsPayload.Signature, variant == "future-first" ? 2 : 1, new byte[] { 1, 7, 255 }));
            extras.Add(originalProject.Sections.First(section => section.Signature == ActivityStatisticsPayload.Signature));
        }
        var raw = LegacySectionFile.Create(opaqueBase.Concat(extras)).ToArray();
        var variantPath = Path.Combine(directory, variant + ".dkm"); File.WriteAllBytes(variantPath, raw);
        doc = NativeMapDocument.Load(variantPath); session = new(doc); EqualPaint(doc);
        var savedPath = Path.Combine(directory, variant + "-saved.dkm"); session.SaveAs(savedPath);
        var saved = LegacySectionFile.Read(savedPath);
        var retained = saved.Sections.Where(section => section.Signature != ActivityStatisticsPayload.Signature).ToArray();
        var expected = opaqueBase.Append(opaque).ToArray();
        Expect(retained.Length == expected.Length && retained.Zip(expected).All(pair => pair.First.Signature == pair.Second.Signature
            && pair.First.Version == pair.Second.Version && pair.First.Payload.Span.SequenceEqual(pair.Second.Payload.Span)), "Save normalized unrelated sections or their order.");
        if (variant == "missing") Expect(saved.Sections.Count(section => section.Signature == ActivityStatisticsPayload.Signature) == 1
            && NativeMapDocument.Load(savedPath).ReadStoredActivityStatistics()!.SavedTimes == 1, "Missing optional KSTS was not added on normal Save.");
        else Expect(File.ReadAllBytes(savedPath).AsSpan().SequenceEqual(raw), "Unsupported first KSTS or later duplicate was rewritten.");
        Expect(!doc.HasChanges && !doc.CanUndo && doc.EditorChangeCount == 0, "Opaque save did not adopt clean logical baseline.");
    }
    Console.WriteLine("PASS repeated Save retains materialized optional sections, opaque order and future/truncated/duplicate/missing KSTS");

    Console.WriteLine("EDITOR PROJECT SAVE CHECKS PASSED: MAP/native conversion/windows/full integration remain pending");
}
finally
{
    var allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DK2EditorSaveChecks")) + Path.DirectorySeparatorChar;
    var resolved = Path.GetFullPath(directory);
    if (!resolved.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new IOException("Refuse cleanup outside private fixture root.");
    if (Directory.Exists(resolved)) Directory.Delete(resolved, recursive: true);
}

sealed class FailWriteStream(Stream inner) : Stream
{
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => inner.Length;
    public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
    public override void Flush() => inner.Flush();
    public override void Write(byte[] buffer, int offset, int count) { inner.Write(buffer, offset, Math.Min(count, 4)); throw new IOException("Injected after file creation."); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
}
