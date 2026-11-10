using System.Buffers.Binary;
using System.Collections.Immutable;

namespace DK2MapEditor.Core.Editing;

/// <summary>Eight literal Win32 SYSTEMTIME words. Invalid imported values stay intact.</summary>
public readonly record struct ActivityLocalTime(ushort Year, ushort Month, ushort DayOfWeek, ushort Day,
    ushort Hour, ushort Minute, ushort Second, ushort Milliseconds)
{
    public static ActivityLocalTime FromDateTime(DateTime local) => new((ushort)local.Year, (ushort)local.Month,
        (ushort)local.DayOfWeek, (ushort)local.Day, (ushort)local.Hour, (ushort)local.Minute,
        (ushort)local.Second, (ushort)local.Millisecond);

    public DateTime? CalendarValue()
    {
        // Native creation dates use 0xffff for unknown weekday; it is not part
        // of the actual calendar date. Do not normalize the stored word.
        if (Year is 0 or > 9999 || Month is 0 or > 12 || Day is 0 or > 31
            || Hour > 23 || Minute > 59 || Second > 59 || Milliseconds > 999) return null;
        try { return new DateTime(Year, Month, Day, Hour, Minute, Second, Milliseconds, DateTimeKind.Unspecified); }
        catch (ArgumentOutOfRangeException) { return null; }
    }
}

/// <summary>The complete KSTS version1 payload, including unused extension and opaque tail.</summary>
public sealed record ActivityStatisticsPayload(ActivityLocalTime StartTime, uint TotalTicks, uint LMBClicks,
    uint RMBClicks, uint UsageOfPaintThings, uint UsageOfPaintFill, uint UsageOfPaint, uint UsageOfPaintRnd,
    uint SavedTimes, ActivityLocalTime LastSaved, uint RealTicks, uint UsageOfUndo,
    ActivityLocalTime CreatedTime, ActivityLocalTime CompileTime, ImmutableArray<uint> Extension,
    ImmutableArray<byte> OpaqueTail)
{
    public const int Size = 192;
    public const int Version = 1;
    public const uint Signature = 0x5354534B; // KSTS, little endian

    public static ActivityStatisticsPayload New(ActivityLocalTime started, ActivityLocalTime created = default,
        ActivityLocalTime compiled = default) => new(started, 0, 0, 0, 0, 0, 0, 0, 0, default, 0, 0,
            created, compiled, ImmutableArray.CreateRange(new uint[22]), ImmutableArray<byte>.Empty);

    public static ActivityStatisticsPayload Decode(int version, ReadOnlySpan<byte> bytes)
    {
        if (version != Version) throw new InvalidDataException("Unsupported activity statistics version.");
        if (bytes.Length < Size) throw new InvalidDataException("Truncated activity statistics payload.");
        var extension = ImmutableArray.CreateBuilder<uint>(22);
        for (var index = 0; index < 22; index++) extension.Add(U32(bytes, 104 + index * 4));
        return new(Time(bytes, 0), U32(bytes, 16), U32(bytes, 20), U32(bytes, 24), U32(bytes, 28),
            U32(bytes, 32), U32(bytes, 36), U32(bytes, 40), U32(bytes, 44), Time(bytes, 48),
            U32(bytes, 64), U32(bytes, 68), Time(bytes, 72), Time(bytes, 88), extension.MoveToImmutable(),
            ImmutableArray.CreateRange(bytes[Size..].ToArray()));
    }

    public byte[] Encode()
    {
        if (Extension.IsDefault || Extension.Length != 22 || OpaqueTail.IsDefault)
            throw new InvalidDataException("Invalid activity statistics extension.");
        var bytes = new byte[checked(Size + OpaqueTail.Length)];
        PutTime(bytes, 0, StartTime); Put(bytes, 16, TotalTicks); Put(bytes, 20, LMBClicks);
        Put(bytes, 24, RMBClicks); Put(bytes, 28, UsageOfPaintThings); Put(bytes, 32, UsageOfPaintFill);
        Put(bytes, 36, UsageOfPaint); Put(bytes, 40, UsageOfPaintRnd); Put(bytes, 44, SavedTimes);
        PutTime(bytes, 48, LastSaved); Put(bytes, 64, RealTicks); Put(bytes, 68, UsageOfUndo);
        PutTime(bytes, 72, CreatedTime); PutTime(bytes, 88, CompileTime);
        for (var index = 0; index < 22; index++) Put(bytes, 104 + index * 4, Extension[index]);
        OpaqueTail.AsSpan().CopyTo(bytes.AsSpan(Size));
        return bytes;
    }

    private static uint U32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    private static ushort U16(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]);
    private static void Put(Span<byte> bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..], value);
    private static ActivityLocalTime Time(ReadOnlySpan<byte> bytes, int offset) => new(U16(bytes, offset), U16(bytes, offset + 2),
        U16(bytes, offset + 4), U16(bytes, offset + 6), U16(bytes, offset + 8), U16(bytes, offset + 10), U16(bytes, offset + 12), U16(bytes, offset + 14));
    private static void PutTime(Span<byte> bytes, int offset, ActivityLocalTime value)
    {
        ushort[] words = [value.Year, value.Month, value.DayOfWeek, value.Day, value.Hour, value.Minute, value.Second, value.Milliseconds];
        for (var index = 0; index < words.Length; index++) BinaryPrimitives.WriteUInt16LittleEndian(bytes[(offset + index * 2)..], words[index]);
    }
}

public enum ActivityCounterEvent { LeftMapClick, RightMapClick, ObjectPaint, Fill, Paint, RandomPaint, Undo }

/// <summary>Source activity counters are independent of map Undo and file revisions.
/// The caller must wire the original UI/event/save/compile points; this class alone
/// does not claim document or UI integration.</summary>
public sealed class ActivityStatisticsSession : IDisposable
{
    private ActivityStatisticsPayload payload;
    private readonly Func<uint> tickCount;
    private readonly Func<ActivityLocalTime> localTime;
    private readonly uint startedTick;
    private ActivityStatisticsSave? activeSave;
    private bool closed, preparingSave;
    public bool IsClosed => closed;

    public ActivityStatisticsSession(ActivityStatisticsPayload? imported = null, Func<uint>? tickCount = null,
        Func<ActivityLocalTime>? localTime = null, ActivityLocalTime created = default, ActivityLocalTime compiled = default)
    {
        this.tickCount = tickCount ?? (() => unchecked((uint)Environment.TickCount64));
        this.localTime = localTime ?? (() => ActivityLocalTime.FromDateTime(DateTime.Now));
        payload = imported is null ? ActivityStatisticsPayload.New(this.localTime(), created, compiled)
            : imported with { TotalTicks = imported.RealTicks };
        // Validate complete caller-owned immutable data before using it.
        _ = payload.Encode();
        startedTick = this.tickCount();
    }

    private ActivityStatisticsSession(ActivityStatisticsSession source)
    {
        source.Current();
        if (source.activeSave is not null || source.preparingSave)
            throw new InvalidOperationException("Finish the statistics write before continuing its session.");
        // Save conversion adopts a new document, but is not a new load. Keep
        // the original clock origin, including time spent writing the output.
        // The payload is immutable; later counters remain document-local.
        payload = source.payload;
        tickCount = source.tickCount;
        localTime = source.localTime;
        startedTick = source.startedTick;
    }

    internal ActivityStatisticsSession CopyForSaveContinuation() => new(this);

    public ActivityStatisticsPayload Snapshot() { Current(); return payload; }
    public uint ElapsedMilliseconds()
    { Current(); var elapsed = unchecked(payload.TotalTicks + (tickCount() - startedTick)); Current(); return elapsed; }

    public void Record(ActivityCounterEvent operation)
    {
        Current();
        payload = operation switch
        {
            ActivityCounterEvent.LeftMapClick => payload with { LMBClicks = unchecked(payload.LMBClicks + 1) },
            ActivityCounterEvent.RightMapClick => payload with { RMBClicks = unchecked(payload.RMBClicks + 1) },
            ActivityCounterEvent.ObjectPaint => payload with { UsageOfPaintThings = unchecked(payload.UsageOfPaintThings + 1) },
            ActivityCounterEvent.Fill => payload with { UsageOfPaintFill = unchecked(payload.UsageOfPaintFill + 1) },
            ActivityCounterEvent.Paint => payload with { UsageOfPaint = unchecked(payload.UsageOfPaint + 1) },
            ActivityCounterEvent.RandomPaint => payload with { UsageOfPaintRnd = unchecked(payload.UsageOfPaintRnd + 1) },
            ActivityCounterEvent.Undo => payload with { UsageOfUndo = unchecked(payload.UsageOfUndo + 1) },
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    /// <summary>Invoke after a project output file is created and before writing KSTS.
    /// Failed writes retain these source side effects; temporary saves restore only SavedTimes.</summary>
    public ActivityStatisticsSave BeginProjectWrite(bool temporary = false)
    {
        Current();
        if (activeSave is not null || preparingSave) throw new InvalidOperationException("An activity statistics save is already active.");
        var before = payload;
        preparingSave = true;
        try
        {
            var elapsed = ElapsedMilliseconds(); var now = localTime(); Current();
            if (!ReferenceEquals(before, payload)) throw new InvalidOperationException("Activity statistics changed while preparing a save.");
            payload = before with { SavedTimes = unchecked(before.SavedTimes + 1), RealTicks = elapsed, LastSaved = now };
            activeSave = new(this, before.SavedTimes, temporary, payload);
            return activeSave;
        }
        finally { preparingSave = false; }
    }

    /// <summary>Source Compiler_Task records this date when the task ends, even after reported errors.</summary>
    public void CompilationTaskFinished() { Current(); var now = localTime(); Current(); payload = payload with { CompileTime = now }; }

    internal void Finish(ActivityStatisticsSave operation, uint previousCount, bool temporary)
    {
        if (closed || !ReferenceEquals(activeSave, operation)) return;
        if (temporary) payload = payload with { SavedTimes = previousCount };
        activeSave = null;
    }
    private void Current() => ObjectDisposedException.ThrowIf(closed, this);
    public void Dispose() { closed = true; activeSave = null; }
}

public sealed class ActivityStatisticsSave : IDisposable
{
    private ActivityStatisticsSession? owner;
    private readonly uint previousCount;
    private readonly bool temporary;
    public ActivityStatisticsPayload Output { get; }
    internal ActivityStatisticsSave(ActivityStatisticsSession owner, uint previousCount, bool temporary, ActivityStatisticsPayload output)
    { this.owner = owner; this.previousCount = previousCount; this.temporary = temporary; Output = output; }
    public void Dispose()
    { var current = owner; owner = null; current?.Finish(this, previousCount, temporary); }
}
