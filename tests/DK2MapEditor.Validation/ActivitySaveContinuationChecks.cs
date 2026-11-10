using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Reflection;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class ActivitySaveContinuationChecks
{
    private static readonly FieldInfo Statistics = typeof(NativeMapDocument)
        .GetField("activityStatistics", BindingFlags.Instance | BindingFlags.NonPublic)!;

    internal static void Run(Action<string, Action> run)
    {
        foreach (var area in new[] { false, true })
        {
            var kind = area ? "AREA" : "native";
            run($"{kind} first DKM Save continues live clock, exact activity payload and independent counters", () => Continue(area));
            run($"{kind} temporary conversion keeps source session and restores only save count", () => Temporary(area));
            run($"{kind} failed conversion Save retains source session and source write side effects", () => Failure(area));
        }
    }

    private sealed class Clock
    {
        internal uint Tick = 0xfffffff0;
        internal int LocalReads;
        internal ActivityLocalTime Now = new(2026, 10, 5, 23, 18, 27, 31, 123);
        internal ActivityLocalTime ReadLocal() { LocalReads++; return Now; }
        internal void Advance(uint value) => Tick = unchecked(Tick + value);
    }

    private sealed record Fixture(string Directory, NativeMapDocument Document, Clock Clock);
    private static Fixture Create(bool area)
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2ActivitySaveContinuation-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        var document = NativeMapDocument.CreateNewMap(new("ActivitySave", "ActivitySave", 24, 24,
            PlaceHearts: true, NativeFormat: !area));
        if (area)
        {
            var project = document.SaveCopy(Path.Combine(directory, "area-basis"));
            var bytes = File.ReadAllBytes(project);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, LegacySectionFile.AreaFileSignature);
            var path = Path.Combine(directory, "input.dkz");
            File.WriteAllBytes(path, bytes);
            document = NativeMapDocument.Load(path);
        }
        var clock = new Clock();
        var payload = ActivityStatisticsPayload.New(clock.Now, clock.Now, clock.Now) with
        {
            TotalTicks = 99, RealTicks = 2700, SavedTimes = uint.MaxValue,
            LMBClicks = 17, RMBClicks = 19, UsageOfUndo = 5,
            Extension = Enumerable.Range(1, 22).Select(i => (uint)i * 17).ToImmutableArray(),
            OpaqueTail = ImmutableArray.Create<byte>(0, 255, 41, 0, 77)
        };
        (Statistics.GetValue(document) as IDisposable)?.Dispose();
        Statistics.SetValue(document, new ActivityStatisticsSession(payload, () => clock.Tick, clock.ReadLocal));
        clock.Tick = 0x20; // The session clock crosses the unsigned DWORD boundary.
        return new(directory, document, clock);
    }

    private static ActivityStatisticsPayload Stored(string path)
    {
        var section = LegacySectionFile.Read(path).Sections.First(item => item.Signature == ActivityStatisticsPayload.Signature);
        return ActivityStatisticsPayload.Decode(section.Version, section.Payload.Span);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static bool Same(ActivityStatisticsPayload? left, ActivityStatisticsPayload? right)
        => left is not null && right is not null && left.Encode().AsSpan().SequenceEqual(right.Encode());

    private static void Continue(bool area)
    {
        var f = Create(area); var source = f.Document;
        var files = source.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var history = source.GetHistory(); var revision = source.EditRevision;
        using var save = new EditorSaveSession(source);
        var path = Path.Combine(f.Directory, "converted.dkm");
        var plan = save.PrepareSaveAs(path);
        Require(!plan.HasBlockingIssues, "Fixture conversion unexpectedly has blocking issues.");
        var before = source.ReadActivityStatistics()!;
        var outputElapsed = source.ActivityElapsedMilliseconds;
        Require(outputElapsed == 2748, "Deterministic source clock/RealTicks fixture differs.");
        Require(save.Save(plan, acceptLosses: true, createOutput: name => new AdvanceStream(name, f.Clock)), "First Save refused.");
        var continued = save.Document;
        Require(!ReferenceEquals(continued, source), "The conversion did not adopt its new project document.");
        Require(Same(continued.ReadActivityStatistics(), source.ReadActivityStatistics()), "Conversion rebased or discarded the live activity payload.");
        Require(continued.ActivityElapsedMilliseconds == source.ActivityElapsedMilliseconds && continued.ActivityElapsedMilliseconds == 3548,
            "Conversion restarted the timer or lost time spent writing/flushing.");
        Require(f.Clock.LocalReads == 1, "Adoption read a new activity date.");
        var stored = Stored(path);
        Require(stored.TotalTicks == before.TotalTicks && stored.RealTicks == outputElapsed && stored.SavedTimes == 0,
            "Persisted statistics lost the source write boundary or DWORD save-count wrap.");
        Require(Same(stored, source.ReadActivityStatistics()), "Stored payload differs beyond the ordinary writer projection.");
        var reopened = NativeMapDocument.Load(path);
        Require(reopened.ReadActivityStatistics()!.TotalTicks == stored.RealTicks,
            "Actual file reload stopped using persisted RealTicks as its fresh timer base.");

        f.Clock.Advance(600);
        var old = source.ReadActivityStatistics()!;
        continued.RecordActivity(ActivityCounterEvent.RightMapClick);
        Require(source.ReadActivityStatistics()!.RMBClicks == old.RMBClicks && continued.ReadActivityStatistics()!.RMBClicks == old.RMBClicks + 1,
            "The adopted document and detached source share mutable activity state.");
        var beforeSecond = continued.ReadActivityStatistics()!;
        Require(save.Save(save.PrepareSave(), acceptLosses: true, createOutput: name => new AdvanceStream(name, f.Clock)), "Repeated Save refused.");
        Require(ReferenceEquals(save.Document, continued) && continued.ActivityElapsedMilliseconds == 4948,
            "Repeated Save replaced the live session or lost elapsed time.");
        Require(continued.ReadActivityStatistics()!.TotalTicks == beforeSecond.TotalTicks && Stored(path).SavedTimes == 1 && f.Clock.LocalReads == 2,
            "Repeated Save changed the baseline or counted/dated twice.");
        f.Clock.Advance(100);
        f.Clock.Now = f.Clock.Now with { Second = 59 };
        continued.CompilationTaskFinished();
        Require(continued.ReadActivityStatistics()!.CompileTime == f.Clock.Now && source.ReadActivityStatistics()!.CompileTime == old.CompileTime,
            "Continued session lost its clock/date provider or altered detached-source statistics.");
        Require(files.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && source.EditRevision == revision
            && source.GetHistory().Undo.Count == history.Undo.Count, "Save continuation edited input buffers or history.");
        Console.WriteLine("  Activity Save fixture: " + f.Directory);
    }

    private static void Temporary(bool area)
    {
        var f = Create(area); using var save = new EditorSaveSession(f.Document);
        var before = f.Document.ReadActivityStatistics()!;
        var path = Path.Combine(f.Directory, "temporary.dkm");
        save.SaveTemporary(path, name => new AdvanceStream(name, f.Clock));
        var after = f.Document.ReadActivityStatistics()!;
        Require(ReferenceEquals(save.Document, f.Document) && save.TargetPath is null, "Temporary conversion adopted a document/ordinary target.");
        Require(after.SavedTimes == before.SavedTimes && after.RealTicks == 2748 && after.TotalTicks == before.TotalTicks
            && Stored(path).SavedTimes == 0 && f.Document.ActivityElapsedMilliseconds == 3548 && f.Clock.LocalReads == 1,
            "Temporary save restarted elapsed time, discarded dates/ticks or retained its temporary count.");
    }

    private static void Failure(bool area)
    {
        var f = Create(area); using var save = new EditorSaveSession(f.Document);
        var path = Path.Combine(f.Directory, "failed.dkm"); var before = f.Document.ReadActivityStatistics()!;
        var refused = false;
        try { save.Save(save.PrepareSaveAs(path), true, createOutput: _ => throw new IOException("Before file creation")); }
        catch (IOException) { refused = true; }
        Require(refused && Same(before, f.Document.ReadActivityStatistics()), "Failure before output creation changed activity.");
        refused = false;
        try { save.Save(save.PrepareSaveAs(path), true, createOutput: name => new AdvanceStream(name, f.Clock, fail: true)); }
        catch (IOException) { refused = true; }
        var after = f.Document.ReadActivityStatistics()!;
        Require(refused && !File.Exists(path) && save.TargetPath is null && save.LastSave is null
            && ReferenceEquals(save.Document, f.Document), "Failed write adopted a document/target or reported successful Save.");
        Require(after.SavedTimes == 0 && after.TotalTicks == before.TotalTicks && after.RealTicks == 2748
            && f.Document.ActivityElapsedMilliseconds == 3448 && f.Clock.LocalReads == 1,
            "Failed write discarded the original source side effects or restarted the session.");
        Require(save.Save(save.PrepareSaveAs(path), true, createOutput: name => new AdvanceStream(name, f.Clock)), "Retry failed.");
        Require(Same(save.Document.ReadActivityStatistics(), f.Document.ReadActivityStatistics())
            && save.Document.ActivityElapsedMilliseconds == 4248 && Stored(path).SavedTimes == 1,
            "Retry did not continue the failed source session.");
    }

    private sealed class AdvanceStream(string path, Clock clock, bool fail = false) : Stream
    {
        private readonly FileStream output = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        public override bool CanRead => false;
        public override bool CanSeek => output.CanSeek;
        public override bool CanWrite => true;
        public override long Length => output.Length;
        public override long Position { get => output.Position; set => output.Position = value; }
        public override void Flush() { clock.Advance(100); output.Flush(); }
        private void BeforeWrite() { clock.Advance(700); if (fail) throw new IOException("After activity capture"); }
        public override void Write(byte[] buffer, int offset, int count) { BeforeWrite(); output.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { BeforeWrite(); output.Write(buffer); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => output.Seek(offset, origin);
        public override void SetLength(long value) => output.SetLength(value);
        protected override void Dispose(bool disposing) { if (disposing) output.Dispose(); base.Dispose(disposing); }
    }
}
