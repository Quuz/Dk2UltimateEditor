using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public enum ActivityStatisticsProblem { None, UnsupportedVersion, TruncatedSection }

public sealed partial class NativeMapDocument
{
    private ActivityStatisticsSession? activityStatistics = new();
    public ActivityStatisticsProblem ActivityStatisticsProblem { get; private set; }

    /// <summary>Runtime counters belong to the document, independently of Undo
    /// bytes and read-only imported sections. Reading never starts the timer.</summary>
    public ActivityStatisticsPayload? ReadActivityStatistics() => activityStatistics?.Snapshot();
    public uint? ActivityElapsedMilliseconds => activityStatistics?.ElapsedMilliseconds();

    public void RecordActivity(ActivityCounterEvent operation)
    {
        if (!Enum.IsDefined(operation)) throw new ArgumentOutOfRangeException(nameof(operation));
        activityStatistics?.Record(operation);
    }

    /// <summary>Original Compiler_Task updates its completion date for Test,
    /// Compile and failed tasks, independently of map bytes/Undo.</summary>
    public void CompilationTaskFinished() => activityStatistics?.CompilationTaskFinished();

    internal void ContinueActivityStatisticsFrom(NativeMapDocument source)
    {
        if (ReferenceEquals(this, source)) return;
        var continued = source.activityStatistics?.CopyForSaveContinuation();
        activityStatistics?.Dispose();
        activityStatistics = continued;
        ActivityStatisticsProblem = source.ActivityStatisticsProblem;
    }

    private void InitializeActivityStatistics(bool newMap = false)
    {
        activityStatistics?.Dispose();
        activityStatistics = null;
        ActivityStatisticsProblem = ActivityStatisticsProblem.None;
        var stored = IsLegacyProject ? projectSections.FirstOrDefault(file => file.Signature == ActivityStatisticsPayload.Signature) : null;
        if (!newMap && stored is not null)
        {
            if (stored.UInt32(4) != ActivityStatisticsPayload.Version)
            { ActivityStatisticsProblem = ActivityStatisticsProblem.UnsupportedVersion; return; }
            if (stored.Length - 12 < ActivityStatisticsPayload.Size)
            { ActivityStatisticsProblem = ActivityStatisticsProblem.TruncatedSection; return; }
            activityStatistics = new(ReadStoredActivityStatistics());
            return;
        }
        var created = newMap ? ActivityLocalTime.FromDateTime(DateTime.Now) : default;
        var compiled = default(ActivityLocalTime);
        if (!newMap && !IsLegacyProject && MainFile.Signature == 0xDC)
        {
            var date = MainFile.Data.Span[LegacySchemas.Offset("GAMEINFO", "dateMainDate")..];
            created = new(BinaryPrimitives.ReadUInt16LittleEndian(date[LegacySchemas.Offset("GAMEDATE", "wGlobalCreationYear")..]),
                date[LegacySchemas.Offset("GAMEDATE", "bGlobalCreationMonth")], ushort.MaxValue,
                date[LegacySchemas.Offset("GAMEDATE", "bGlobalCreationDate")], date[LegacySchemas.Offset("GAMEDATE", "bGlobalCreationHour")],
                date[LegacySchemas.Offset("GAMEDATE", "bGlobalCreationMinute")], date[LegacySchemas.Offset("GAMEDATE", "bGlobalCreationSeconds")], 0);
            compiled = created;
        }
        activityStatistics = new(created: created, compiled: compiled);
    }

    // Project a captured save payload onto the first KSTS only. The in-memory
    // imported bytes/history remain intact; later duplicates and unknown bytes
    // keep their original order. Missing optional KSTS is added on actual Save.
    private static byte[] ProjectSavedActivity(byte[] project, ActivityStatisticsPayload payload)
    {
        var bytes = payload.Encode();
        var offset = 12; var previousLength = 0;
        while (offset < project.Length)
        {
            var size = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(project.AsSpan(offset + 8)));
            if (BinaryPrimitives.ReadUInt32LittleEndian(project.AsSpan(offset)) == ActivityStatisticsPayload.Signature)
            { previousLength = checked(12 + size); break; }
            offset = checked(offset + 12 + size);
        }
        var length = checked((long)project.Length - previousLength + 12 + bytes.Length);
        if (length > MaximumFileBytes) throw new InvalidDataException("Legacy project exceeds its safe size limit.");
        var output = new byte[(int)length];
        project.AsSpan(0, offset).CopyTo(output);
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(offset), ActivityStatisticsPayload.Signature);
        BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(offset + 4), ActivityStatisticsPayload.Version);
        BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(offset + 8), bytes.Length);
        bytes.CopyTo(output, offset + 12);
        project.AsSpan(offset + previousLength).CopyTo(output.AsSpan(offset + 12 + bytes.Length));
        BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(8), output.Length - 12);
        _ = Formats.LegacySectionFile.Parse(output);
        return output;
    }

    /// <summary>Reads the first stored project KSTS section, as the original
    /// editor does. Missing KSTS and native game maps have no stored payload.
    /// This read does not start a timer, normalize data or alter edit history.</summary>
    public ActivityStatisticsPayload? ReadStoredActivityStatistics()
    {
        if (!IsLegacyProject) return null;
        var section = projectSections.FirstOrDefault(file => file.Signature == ActivityStatisticsPayload.Signature);
        if (section is null) return null;
        return ActivityStatisticsPayload.Decode(
            BinaryPrimitives.ReadInt32LittleEndian(section.Data.Span[4..]), section.Data.Span[12..]);
    }
}
