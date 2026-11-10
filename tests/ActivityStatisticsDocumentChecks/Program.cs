using System.Buffers.Binary;
using System.Reflection;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
void Refuses<T>(Action action) where T : Exception
{ try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
if (args.Length != 1) throw new ArgumentException("Choose a new fixture directory.");
var directory = Path.GetFullPath(args[0]);
if (Directory.Exists(directory)) throw new IOException("Preserve existing fixtures.");
Directory.CreateDirectory(directory);
uint ticks = 300;
var now = new ActivityLocalTime(2026, 10, 5, 9, 15, 30, 20, 123);
// Inject only the already tested session dependency. Production factories still
// initialize automatically; their lifecycle is checked before each replacement.
void Clock(NativeMapDocument doc, Func<ActivityLocalTime>? clock = null)
{
    var field = typeof(NativeMapDocument).GetField("activityStatistics", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var imported = doc.ReadActivityStatistics();
    (field.GetValue(doc) as IDisposable)?.Dispose();
    field.SetValue(doc, new ActivityStatisticsSession(imported, () => ticks, clock ?? (() => now)));
}
ActivityStatisticsPayload Stored(string path) => ActivityStatisticsPayload.Decode(1,
    LegacySectionFile.Read(path).Sections.First(section => section.Signature == ActivityStatisticsPayload.Signature).Payload.Span);
var fresh = NativeMapDocument.CreateNewMap(new("DocumentActivity", "DocumentActivity", 24, 24));
var initial = fresh.ReadActivityStatistics()!;
Expect(initial.StartTime.CalendarValue() is not null && initial.CreatedTime.CalendarValue() is not null
    && initial.CompileTime == default && initial.SavedTimes == 0 && fresh.ActivityElapsedMilliseconds is not null,
    "New-map factory did not initialize runtime statistics/creation date.");
Expect(fresh.ReadStoredActivityStatistics() is null && fresh.ActivityStatisticsProblem == ActivityStatisticsProblem.None,
    "Fresh runtime session synthesized an imported KSTS section.");
Clock(fresh); ticks += 100;
var revision = fresh.EditRevision; var dirty = fresh.HasChanges; var fileBytes = fresh.Files.Select(file => file.Data.ToArray()).ToArray();
var first = fresh.SaveCopy(Path.Combine(directory, "first"));
Expect(Stored(first).SavedTimes == 1 && Stored(first).RealTicks == 100 && fresh.ReadActivityStatistics()!.SavedTimes == 1,
    "Actual project Save did not add KSTS/count/elapsed.");
Expect(fresh.EditRevision == revision && fresh.HasChanges == dirty
    && fresh.Files.Select((file, index) => file.Data.Span.SequenceEqual(fileBytes[index])).All(equal => equal)
    && fresh.ReadStoredActivityStatistics() is null, "Save changed imported/edit-history bytes.");
ticks += 100;
var second = fresh.SaveCopy(Path.Combine(directory, "second"));
Expect(Stored(second).SavedTimes == 2 && Stored(second).RealTicks == 200, "Second Save lost runtime cumulative count/time.");
now = now with { Minute = 31 }; ticks += 50;
var temporary = fresh.SaveCopy(Path.Combine(directory, "temporary"), temporary: true);
Expect(Stored(temporary).SavedTimes == 3 && fresh.ReadActivityStatistics()!.SavedTimes == 2
    && fresh.ReadActivityStatistics()!.RealTicks == 250 && fresh.ReadActivityStatistics()!.LastSaved == now,
    "Actual temporary Save did not restore only count.");
var beforeRefusal = fresh.ReadActivityStatistics();
Refuses<IOException>(() => fresh.SaveCopy(Path.Combine(directory, "second")));
Expect(ReferenceEquals(beforeRefusal, fresh.ReadActivityStatistics()), "Preflight failed Save changed statistics.");
Console.WriteLine("PASS initialized new-map session, actual ordinary/temporary Saves, optional KSTS creation, preflight refusal and independent history bytes");

// Literal original payload with invalid date words and opaque extension/tail.
var raw = new byte[197]; for (var index = 0; index < raw.Length; index++) raw[index] = (byte)(index * 37 + 11);
BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(16), 123);
BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(44), 7);
BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(64), 456);
var baseSections = LegacySectionFile.Read(first).Sections.Where(section => section.Signature != ActivityStatisticsPayload.Signature).ToArray();
foreach (var variant in new[] { "valid", "duplicate", "future", "short", "future-first", "missing" })
{
    var extra = new List<LegacySection>();
    if (variant != "missing") extra.Add(new(ActivityStatisticsPayload.Signature, variant is "future" or "future-first" ? 2 : 1,
        variant == "short" ? raw.AsMemory(0, 191) : raw));
    if (variant == "duplicate") extra.Add(new(ActivityStatisticsPayload.Signature, 2, new byte[] { 99 }));
    if (variant == "future-first") extra.Add(new(ActivityStatisticsPayload.Signature, 1, raw));
    var inputBytes = LegacySectionFile.Create(baseSections.Concat(extra)).ToArray();
    var input = Path.Combine(directory, variant + ".dkm"); File.WriteAllBytes(input, inputBytes);
    var doc = NativeMapDocument.Load(input); var beforeFiles = doc.Files.Select(file => file.Data.ToArray()).ToArray();
    var unsupported = variant is "future" or "short" or "future-first";
    if (unsupported)
    {
        Expect(doc.ReadActivityStatistics() is null && doc.ActivityElapsedMilliseconds is null
            && doc.ActivityStatisticsProblem == (variant == "short" ? ActivityStatisticsProblem.TruncatedSection : ActivityStatisticsProblem.UnsupportedVersion),
            "Unsupported stored statistics were normalized or exposed as valid runtime counters.");
        Refuses<InvalidDataException>(() => doc.ReadStoredActivityStatistics());
    }
    else
    {
        Expect(doc.ReadActivityStatistics() is not null && doc.ActivityStatisticsProblem == ActivityStatisticsProblem.None,
            "Loaded supported/missing stats did not initialize at document load.");
        if (variant != "missing")
        {
            Expect(doc.ReadActivityStatistics()!.TotalTicks == 456 && doc.ReadStoredActivityStatistics()!.TotalTicks == 123,
                "Resume did not privately adopt RealTicks or changed stored TotalTicks.");
            Expect(doc.ReadStoredActivityStatistics()!.Encode().AsSpan().SequenceEqual(raw), "Read changed opaque imported data.");
        }
        Clock(doc); ticks += 100;
    }
    var output = doc.SaveCopy(Path.Combine(directory, variant + "-copy"));
    Expect(File.ReadAllBytes(input).AsSpan().SequenceEqual(inputBytes)
        && doc.Files.Select((file, index) => file.Data.Span.SequenceEqual(beforeFiles[index])).All(equal => equal), "Save modified source/in-memory imported sections.");
    if (unsupported) Expect(File.ReadAllBytes(output).AsSpan().SequenceEqual(inputBytes), "Unsupported KSTS was overwritten or reordered on Save.");
    else
    {
        var saved = LegacySectionFile.Read(output); var current = doc.ReadActivityStatistics()!;
        Expect(Stored(output).Encode().AsSpan().SequenceEqual(current.Encode()), "Saved KSTS differs from captured runtime payload.");
        var beforeOther = baseSections.Concat(extra).Where(section => section.Signature != ActivityStatisticsPayload.Signature).ToArray();
        var afterOther = saved.Sections.Where(section => section.Signature != ActivityStatisticsPayload.Signature).ToArray();
        Expect(beforeOther.Length == afterOther.Length && beforeOther.Select((section, index) => section.Signature == afterOther[index].Signature
            && section.Version == afterOther[index].Version && section.Payload.Span.SequenceEqual(afterOther[index].Payload.Span)).All(equal => equal),
            "KSTS projection changed other section headers/payload/order.");
        if (variant != "missing")
        {
            var expected = raw.ToArray();
            BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(16), 456);
            BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(44), 8);
            BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(64), 556);
            ushort[] words = [now.Year, now.Month, now.DayOfWeek, now.Day, now.Hour, now.Minute, now.Second, now.Milliseconds];
            for (var index = 0; index < words.Length; index++) BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(48 + index * 2), words[index]);
            Expect(Stored(output).Encode().AsSpan().SequenceEqual(expected), "Save changed unrelated literal KSTS offsets/tail or source count/date.");
        }
        if (variant == "duplicate")
        {
            var last = saved.Sections.Last(section => section.Signature == ActivityStatisticsPayload.Signature);
            Expect(last.Version == 2 && last.Payload.Span.SequenceEqual(new byte[] { 99 }), "Save edited a later duplicate KSTS.");
        }
        var reopened = NativeMapDocument.Load(output);
        Expect(reopened.ReadActivityStatistics()!.TotalTicks == current.RealTicks && reopened.ReadActivityStatistics()!.SavedTimes == current.SavedTimes,
            "Saved-copy reopen did not resume persisted source time/count.");
    }
}
Console.WriteLine("PASS actual loaded/optional/duplicate/future/truncated project Saves, literal changed offsets, raw/opaque retention and saved-copy resume");

var undoDoc = NativeMapDocument.Load(second); Clock(undoDoc);
var undoCount = undoDoc.ReadActivityStatistics()!.UsageOfUndo;
undoDoc.Undo(); Expect(undoDoc.ReadActivityStatistics()!.UsageOfUndo == undoCount, "Unavailable Undo counted.");
var tile = undoDoc.GetTile(12, 12); undoDoc.Paint([new(12, 12)], new(4, 2, 1));
undoDoc.RecordActivity(ActivityCounterEvent.LeftMapClick); undoDoc.Undo();
Expect(undoDoc.GetTile(12, 12) == tile && undoDoc.ReadActivityStatistics()!.UsageOfUndo == undoCount + 1
    && undoDoc.ReadActivityStatistics()!.LMBClicks == 1, "Actual Undo lost or reverted independent activity counters.");
undoDoc.Redo(); Expect(undoDoc.ReadActivityStatistics()!.UsageOfUndo == undoCount + 1, "Redo counted as Undo.");
Refuses<InvalidOperationException>(() => undoDoc.Edit("nested undo", undoDoc.Undo));
Expect(undoDoc.ReadActivityStatistics()!.UsageOfUndo == undoCount + 1, "Refused Undo counted.");
Refuses<ArgumentOutOfRangeException>(() => undoDoc.RecordActivity((ActivityCounterEvent)999));
Console.WriteLine("PASS actual successful/empty/refused Undo, independent counter history, Redo exclusion and event validation");

foreach (var isTemporary in new[] { false, true })
{
    var failed = NativeMapDocument.Load(second); var destination = Path.Combine(directory, "publication-race-" + isTemporary);
    var before = failed.ReadActivityStatistics()!;
    now = now with { Minute = (ushort)(isTemporary ? 33 : 32) };
    Clock(failed, () => { Directory.CreateDirectory(destination); return now; }); ticks += 99;
    // The injected time provider runs only after the real output FileStream was
    // created. Occupying destination then makes final atomic publication fail.
    Refuses<IOException>(() => failed.SaveCopy(destination, temporary: isTemporary));
    var after = failed.ReadActivityStatistics()!;
    Expect(after.SavedTimes == before.SavedTimes + (isTemporary ? 0u : 1u)
        && after.LastSaved == now && after.RealTicks == before.RealTicks + 99 && Directory.GetFiles(destination).Length == 0,
        "Post-creation failed Save lost source count/time/date effects or published a partial project.");
    Expect(!Directory.EnumerateDirectories(directory, ".dk2-export-*").Any(), "Generated failed export staging was leaked.");
}
Console.WriteLine("PASS actual post-file-creation failed publication, ordinary/temporary source effects and atomic cleanup");

var native = NativeMapDocument.CreateNewMap(new("NativeStats", "NativeStats", 24, 24, NativeFormat: true));
Expect(native.ReadActivityStatistics()!.CreatedTime.CalendarValue() is not null && native.ReadStoredActivityStatistics() is null,
    "Native new-map factory lacks session/date or has a fictitious stored KSTS.");
Clock(native); native.RecordActivity(ActivityCounterEvent.RightMapClick);
var nativePath = native.SaveCopy(Path.Combine(directory, "native"));
Expect(native.ReadActivityStatistics()!.SavedTimes == 0 && native.ReadActivityStatistics()!.RMBClicks == 1
    && !Directory.EnumerateFiles(Path.Combine(directory, "native"), "*", SearchOption.AllDirectories).Any(path => Path.GetFileName(path).Contains("KSTS", StringComparison.OrdinalIgnoreCase)),
    "Game Save invented project statistics persistence/count.");
var header = File.ReadAllBytes(nativePath); var date = header.AsSpan(28);
BinaryPrimitives.WriteUInt16LittleEndian(date, 2026); date[2] = 9; date[3] = 10; date[6] = 6; date[7] = 7; date[8] = 8;
File.WriteAllBytes(nativePath, header);
var nativeLoaded = NativeMapDocument.Load(nativePath);
var expectedDate = new ActivityLocalTime(2026, 10, ushort.MaxValue, 9, 6, 7, 8, 0);
Expect(nativeLoaded.ReadActivityStatistics()!.CreatedTime == expectedDate && nativeLoaded.ReadActivityStatistics()!.CompileTime == expectedDate
    && nativeLoaded.ReadActivityStatistics()!.SavedTimes == 0 && nativeLoaded.ReadStoredActivityStatistics() is null,
    "Native load did not derive exact source dates/unknown weekday or reset runtime counts.");
var areaBytes = File.ReadAllBytes(second); BinaryPrimitives.WriteUInt32LittleEndian(areaBytes, LegacySectionFile.AreaFileSignature);
var areaPath = Path.Combine(directory, "area.dkz"); File.WriteAllBytes(areaPath, areaBytes);
var area = NativeMapDocument.Load(areaPath); var areaCopy = area.SaveCopy(Path.Combine(directory, "area-copy"));
Expect(LegacySectionFile.Read(areaCopy).Signature == LegacySectionFile.AreaFileSignature && Stored(areaCopy).SavedTimes == 3,
    "Project statistics Save changed the legacy AREA outer format.");
Console.WriteLine("PASS native factory/load original game-header dates, runtime-only counters, game Save exclusion and legacy AREA signature retention");
Console.WriteLine("ACTIVITY DOCUMENT CORE SMOKE PASSED: actual UI/click/paint/compile wiring and full regression remain pending");
