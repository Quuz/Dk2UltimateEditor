using System.Buffers.Binary;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Properties_UpdateTriggers shifts these three actions without
    /// clipping, disabling or deleting targets and without a MapChanges increment.
    /// Project operands use source LONG arithmetic; native data has the WORD
    /// projection written by the original compiler. Generic Resize stays strict.</summary>
    private void RelocateResizeScriptTargets(int width, int height, int shiftX, int shiftY, bool editorCaller)
    {
        foreach (var record in Records())
        {
            var project = record.Category == "Project.ACTION";
            if (project)
            {
                if (record.Read(record.Fields.Single(field => field.Name == "ctrl")) is not ("1" or "22" or "49")) continue;
            }
            else if (record.File.Signature != 0xD2 || record.Length != 24 || record.File.UInt32(record.Offset) != 0xD6
                || record.File.Data.Span[record.Offset + 22] is not (1 or 0x16 or 0x31)) continue;

            foreach (var (parameter, offset, shift, bound) in new[] { (5, 12, shiftX, width), (7, 14, shiftY, height) })
            {
                var field = project ? record.Fields.Single(item => item.Name == "param" + parameter) : null;
                var before = project ? int.Parse(record.Read(field!), CultureInfo.InvariantCulture)
                    : BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span.Slice(record.Offset + offset, 2));
                var value = (long)before + shift;
                if (!editorCaller && (value < 0 || value >= bound))
                    throw new InvalidOperationException("Resizing would put a scripted target outside the map. Adjust its target first.");
                if (project)
                    field!.Encode(unchecked((int)value).ToString(CultureInfo.InvariantCulture)).CopyTo(record.File.Bytes, record.Offset + field.Offset);
                else
                    BinaryPrimitives.WriteUInt16LittleEndian(record.File.Bytes.AsSpan(record.Offset + offset, 2), unchecked((ushort)value));
            }
        }
    }
}
