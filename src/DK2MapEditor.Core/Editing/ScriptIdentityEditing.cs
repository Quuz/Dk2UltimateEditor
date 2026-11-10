using System.Buffers.Binary;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Remap every actual incoming connection for an unambiguous event.
    /// Duplicate old IDs with references cannot be assigned a guessed owner.
    /// Zero is the no-event sentinel; repairing a zero ID never rewrites zeros.</summary>
    private void RemapScriptIdentity(NativeRecord record, NativeField field, byte[] encoded)
    {
        if (!ScriptGraph.IsEvent(record) || field.Name != (IsLegacyProject ? "id" : "ID")) return;
        var oldID = ScriptGraph.ID(record);
        var newID = BinaryPrimitives.ReadUInt16LittleEndian(encoded);
        if (oldID == newID) return;
        var records = Records();
        var events = records.Where(ScriptGraph.IsEvent).ToArray();
        if (newID == 0 || events.Any(item => !SameRecord(item, record) && ScriptGraph.ID(item) == newID))
            throw new ArgumentException("Script IDs must be nonzero and unique.");
        if (IsLegacyProject && (record.Category == "Project.TRIGGER" ? newID >= 32768 : newID < 32768))
            throw new ArgumentException("Historical condition IDs require 1–32767; action IDs require 32768–65535.");
        if (oldID == 0) return;
        // The dictionary index omits additional records with duplicate IDs.
        // Inspect every physical event so their connections are never missed.
        var incoming = ScriptGraph.Build(this).Roots.Concat(events.SelectMany(ScriptGraph.Edges))
            .Where(edge => edge.Target == oldID).ToArray();
        var backlinks = IsLegacyProject ? events.Where(item =>
            BinaryPrimitives.ReadUInt16LittleEndian(item.File.Data.Span.Slice(item.Offset + LegacyProjectSchemas.Size("NODE"), 2)) == oldID).ToArray() : [];
        if (events.Count(item => ScriptGraph.ID(item) == oldID) > 1 && (incoming.Length != 0 || backlinks.Length != 0))
            throw new InvalidOperationException("The old scenario ID is shared by several events and has references. Detach the ambiguous references before changing this ID.");
        var text = newID.ToString(CultureInfo.InvariantCulture);
        foreach (var edge in incoming) edge.Field.Encode(text).CopyTo(edge.Owner.File.Bytes, edge.Owner.Offset + edge.Field.Offset);
        foreach (var item in backlinks)
            BinaryPrimitives.WriteUInt16LittleEndian(item.File.Bytes.AsSpan(item.Offset + LegacyProjectSchemas.Size("NODE")), newID);
    }
}
