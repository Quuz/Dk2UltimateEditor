using System.Buffers.Binary;
using System.Collections.Immutable;
using DK2MapEditor.Core.Editing;

void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
void Refuses<T>(Action action) where T : Exception
{ try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }

// Literal original byte layout: all four SYSTEMTIME blocks, ten DWORD values,
// 22 reserved DWORDs and an extra opaque tail, independently authored here.
var raw = new byte[197];
for (var index = 0; index < raw.Length; index++) raw[index] = (byte)(index * 37 + 11);
void Dword(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(offset), value);
Dword(16, 123); Dword(20, 1); Dword(24, 2); Dword(28, 3); Dword(32, 4); Dword(36, 5);
Dword(40, 6); Dword(44, 7); Dword(64, 456); Dword(68, 8);
var decoded = ActivityStatisticsPayload.Decode(1, raw);
Expect(decoded.TotalTicks == 123 && decoded.LMBClicks == 1 && decoded.RMBClicks == 2 && decoded.UsageOfPaintThings == 3
    && decoded.UsageOfPaintFill == 4 && decoded.UsageOfPaint == 5 && decoded.UsageOfPaintRnd == 6 && decoded.SavedTimes == 7
    && decoded.RealTicks == 456 && decoded.UsageOfUndo == 8, "Literal DWORD offsets differ from the original header.");
Expect(decoded.StartTime.Year == BinaryPrimitives.ReadUInt16LittleEndian(raw) && decoded.LastSaved.Year == BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(48))
    && decoded.CreatedTime.Year == BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(72)) && decoded.CompileTime.Year == BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(88)), "SYSTEMTIME offsets differ.");
Expect(decoded.Extension.Length == 22 && decoded.Extension[0] == BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(104))
    && decoded.Extension[21] == BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(188)) && decoded.OpaqueTail.Length == 5, "Extension/tail lost.");
Expect(decoded.Encode().AsSpan().SequenceEqual(raw), "Literal source bytes changed on no-op codec round trip.");
var edited = decoded with { LMBClicks = 0xA1B2C3D4 };
var expected = raw.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(20), 0xA1B2C3D4);
Expect(edited.Encode().AsSpan().SequenceEqual(expected), "A single counter changed unrelated bytes.");
for (var length = 0; length < 192; length++) Refuses<InvalidDataException>(() => ActivityStatisticsPayload.Decode(1, new byte[length]));
foreach (var version in new[] { -1, 0, 2, int.MaxValue }) Refuses<InvalidDataException>(() => ActivityStatisticsPayload.Decode(version, raw));
Refuses<InvalidDataException>(() => (decoded with { Extension = ImmutableArray<uint>.Empty }).Encode());
var random = new Random(1909);
for (var iteration = 0; iteration < 256; iteration++)
{
    var imported = new byte[192 + iteration % 23]; random.NextBytes(imported);
    Expect(ActivityStatisticsPayload.Decode(1, imported).Encode().AsSpan().SequenceEqual(imported), "Opaque/invalid imported payload normalized.");
}
Console.WriteLine("PASS literal192-byte KSTS layout, ten DWORDs/four dates, reserved/tail retention, malformed/future guards and256 opaque round trips");

var invalid = new ActivityLocalTime(65535, 500, 65535, 42, 99, 99, 99, 65535);
Expect(invalid.CalendarValue() is null, "Invalid imported date was treated as valid.");
var weekdayUnknown = new ActivityLocalTime(2026, 10, 65535, 9, 15, 17, 19, 321);
Expect(weekdayUnknown.CalendarValue() == new DateTime(2026, 10, 9, 15, 17, 19, 321) && weekdayUnknown.DayOfWeek == 65535, "Native unknown weekday changed calendar date or raw data.");
Expect(new ActivityLocalTime(2026, 2, 0, 30, 0, 0, 0, 0).CalendarValue() is null && default(ActivityLocalTime).CalendarValue() is null, "Invalid day/empty source date became valid.");
Console.WriteLine("PASS literal local dates, missing/invalid calendar and native0xffff weekday preserve raw words");

uint ticks = 0xFFFFFFFA;
var now = weekdayUnknown;
using var session = new ActivityStatisticsSession(decoded, () => ticks, () => now);
Expect(session.Snapshot().TotalTicks == 456 && decoded.TotalTicks == 123 && decoded.Encode().AsSpan().SequenceEqual(raw), "Resume changed input instead of adopting RealTicks privately.");
ticks = 6;
Expect(session.ElapsedMilliseconds() == 468, "Source tick wrap or persisted elapsed differs.");
foreach (var operation in Enum.GetValues<ActivityCounterEvent>()) session.Record(operation);
var counts = session.Snapshot();
Expect(counts.LMBClicks == 2 && counts.RMBClicks == 3 && counts.UsageOfPaintThings == 4 && counts.UsageOfPaintFill == 5
    && counts.UsageOfPaint == 6 && counts.UsageOfPaintRnd == 7 && counts.UsageOfUndo == 9 && counts.SavedTimes == 7, "Source event counters crossed or affected save count.");
Refuses<ArgumentOutOfRangeException>(() => session.Record((ActivityCounterEvent)999));
using var overflow = new ActivityStatisticsSession(decoded with { RealTicks = 0xFFFFFFF0, LMBClicks = uint.MaxValue, SavedTimes = uint.MaxValue }, () => ticks, () => now);
ticks += 32;
Expect(overflow.ElapsedMilliseconds() == 16, "Source DWORD elapsed overflow differs."); overflow.Record(ActivityCounterEvent.LeftMapClick);
Expect(overflow.Snapshot().LMBClicks == 0, "Source unsigned counter overflow differs.");
using (var write = overflow.BeginProjectWrite()) Expect(write.Output.SavedTimes == 0, "Source unsigned save overflow differs.");
Console.WriteLine("PASS loaded RealTicks/private input retention, seven independent source events and unsigned tick/counter wrap");

ticks += 100;
var elapsed = session.ElapsedMilliseconds();
using (var write = session.BeginProjectWrite())
{
    Expect(write.Output.SavedTimes == 8 && write.Output.RealTicks == elapsed && write.Output.LastSaved == now, "Project write did not capture source count/time/date.");
    Refuses<InvalidOperationException>(() => session.BeginProjectWrite());
    // A failed write has exactly the same source side effects as a successful
    // section write: disposal is not a map/history rollback.
}
Expect(session.Snapshot().SavedTimes == 8, "Failed project write incorrectly rolled back source count.");
now = now with { Minute = 18 }; ticks += 200;
using (var temporary = session.BeginProjectWrite(true)) Expect(temporary.Output.SavedTimes == 9 && temporary.Output.LastSaved == now, "Temporary output omitted source count/date.");
Expect(session.Snapshot().SavedTimes == 8 && session.Snapshot().LastSaved == now && session.Snapshot().RealTicks == session.ElapsedMilliseconds(), "Temporary write did not restore only count.");
now = now with { Minute = 19 }; session.CompilationTaskFinished();
Expect(session.Snapshot().CompileTime == now, "Compile-task end date not recorded.");
Expect(session.Snapshot().Extension.SequenceEqual(decoded.Extension) && session.Snapshot().OpaqueTail.SequenceEqual(decoded.OpaqueTail) && session.Snapshot().CreatedTime == decoded.CreatedTime, "Runtime operations changed imported opaque/creation fields.");
Console.WriteLine("PASS source regular/failed/temporary project write effects, nested save refusal, compile-task date and opaque retention");

bool fault = false;
using var faulted = new ActivityStatisticsSession(decoded, () => 0, () => fault ? throw new IOException("Clock unavailable") : now);
var before = faulted.Snapshot(); fault = true;
Refuses<IOException>(() => faulted.BeginProjectWrite());
Expect(ReferenceEquals(before, faulted.Snapshot()), "Failed time acquisition partly incremented the save.");
fault = false; using (var retry = faulted.BeginProjectWrite()) Expect(retry.Output.SavedTimes == 8, "Failed prepare left a locked session.");
ActivityStatisticsSession? recursive = null; bool recurse = false;
recursive = new ActivityStatisticsSession(decoded, () => 0, () => { if (recurse) recursive!.BeginProjectWrite(); return now; });
recurse = true; Refuses<InvalidOperationException>(() => recursive.BeginProjectWrite());
Expect(recursive.Snapshot().SavedTimes == 7, "Reentrant clock partly changed source count.");
recurse = false; using (var retry = recursive.BeginProjectWrite()) Expect(retry.Output.SavedTimes == 8, "Reentrant prepare left a locked session.");
recursive.Dispose();
bool close = false; ActivityStatisticsSession? closing = null;
closing = new ActivityStatisticsSession(decoded, () => 0, () => { if (close) closing!.Dispose(); return now; });
close = true; Refuses<ObjectDisposedException>(() => closing.BeginProjectWrite());
Refuses<ObjectDisposedException>(() => closing.Record(ActivityCounterEvent.Undo));
var late = new ActivityStatisticsSession(decoded, () => 0, () => now); var pending = late.BeginProjectWrite(true);
late.Dispose(); pending.Dispose(); pending.Dispose();
Refuses<ObjectDisposedException>(() => late.Snapshot());
Console.WriteLine("PASS failing/reentrant clocks, owner closure, prepare recovery and idempotent late save cleanup");

// Actual document saves now update source statistics. The stronger
// ActivityStatisticsDocumentChecks suite owns persistence/lifecycle cases.
Console.WriteLine("ACTIVITY CORE MODEL CHECKS PASSED: document workflow checked in separate harness");
