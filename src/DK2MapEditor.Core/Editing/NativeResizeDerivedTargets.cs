using System.Buffers.Binary;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private sealed record NativeResizeDerivedTarget(NativeRecord Action, bool Crystal, TilePoint? Gate);
    internal static bool NativeDerivedAction(NativeRecord record) => record.File.Signature == 0xD2 && record.File.UInt32(4) == 4
        && record.Length == 24 && record.File.UInt32(record.Offset) == 0xD6 && record.File.Data.Span[record.Offset + 22] is 2 or 0x20;
    internal static bool NativeSourceGate(NativeRecord record) => record.Category == "GAMEGATE" && record.File.UInt32(4) == 4
        && record.Length == 28 && record.File.UInt32(record.Offset + 4) == 20
        && record.Read(record.Fields.Single(field => field.Name == "bGateType")) != "14";
    internal static bool NativeSourceCrystal(NativeRecord record) => record.Category == "GAMEOBJECT" && record.File.UInt32(4) == 4
        && record.Length == LegacySchemas.Size("GAMEOBJECT") && record.File.UInt32(record.Offset + 4) == record.Length - 8
        && record.Read(record.Fields.Single(field => field.Name == "bObjectID")) == "28";
    internal static TilePoint NativeDerivedPosition(NativeRecord record, bool gate)
    {
        int Value(string axis) => unchecked((int)uint.Parse(record.Read(record.Fields.Single(field => field.Name == (gate ? "dwGateLocation" : "dwObjectLocation") + axis)), CultureInfo.InvariantCulture));
        return new(Value("X"), Value("Y"));
    }
    internal static TilePoint NativeDerivedPayload(NativeRecord action) => new(
        BinaryPrimitives.ReadUInt16LittleEndian(action.File.Data.Span.Slice(action.Offset + 12, 2)),
        BinaryPrimitives.ReadUInt16LittleEndian(action.File.Data.Span.Slice(action.Offset + 14, 2)));

    // The reader resolves gate coordinates before any objects move. Duplicate
    // gate positions have the same source result under uniform resize/cropping;
    // neither bGateID nor an event dictionary supplies a guessed identity.
    private NativeResizeDerivedTarget[] CaptureNativeResizeDerivedTargets()
    {
        var records = Records();
        return records.Where(NativeDerivedAction).Select(action =>
        {
            var crystal = action.File.Data.Span[action.Offset + 22] == 2;
            var gate = crystal ? null : NativeGateTarget(action);
            return new NativeResizeDerivedTarget(action, crystal, gate is null ? null : NativeDerivedPosition(gate, true));
        }).ToArray();
    }
    private void ReprojectNativeResizeDerivedTargets(NativeResizeDerivedTarget[] targets, int shiftX, int shiftY)
    {
        var records = Records();
        // The editor boundary-removal pass follows ResizeCore. Evaluate its
        // exact interior rule now so a boundary/cropped first object cannot
        // remain the compiler's global first portal gem.
        bool Survives(TilePoint point) => point.X > 0 && point.Y > 0 && point.X < Width - 1 && point.Y < Height - 1;
        var crystal = records.Where(NativeSourceCrystal).Select(record => NativeDerivedPosition(record, false)).FirstOrDefault(Survives, new(-1, -1));
        var gates = records.Where(NativeSourceGate).Select(record => NativeDerivedPosition(record, true)).Where(Survives).ToHashSet();
        foreach (var target in targets)
        {
            var point = crystal;
            if (!target.Crystal)
            {
                var shifted = target.Gate is { } old ? new TilePoint(unchecked(old.X + shiftX), unchecked(old.Y + shiftY)) : new(-1, -1);
                // RectEx_GetRectLocation returns0 for a missing ID; writer adds1.
                point = target.Gate is not null && gates.Contains(shifted) ? new(shifted.X + 1, shifted.Y + 1) : new(1, 1);
            }
            BinaryPrimitives.WriteUInt16LittleEndian(target.Action.File.Bytes.AsSpan(target.Action.Offset + 12, 2), unchecked((ushort)point.X));
            BinaryPrimitives.WriteUInt16LittleEndian(target.Action.File.Bytes.AsSpan(target.Action.Offset + 14, 2), unchecked((ushort)point.Y));
        }
        RefreshNativeGatePayloadStamps();
    }
}
