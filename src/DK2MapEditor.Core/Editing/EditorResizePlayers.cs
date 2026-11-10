using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    internal static bool VerifiedResizePlayer(NativeRecord record) => record.Category switch
    {
        "Project.MAPOPTIONS" or "Project.MAPOPTIONSV1" or "Project.MAPOPTIONSV2" => record.Length == LegacyProjectSchemas.Size(record.Category[8..]),
        "GAMEPLAYER" => record.Length == 205 && record.File.UInt32(4) == 4,
        _ => false
    };
    internal static bool ResizePlayerCameraSupported(NativeRecord record) => record.Category is "Project.MAPOPTIONS" or "GAMEPLAYER";

    /// <summary>Properties_UpdateProperties: only computer keepers3–7,
    /// source X sentinel controls the complete pair/rectangle, no clipping or
    /// additional counter/Undo. Native WORDs load into nonnegative source LONGs;
    /// 65535 is not the project's -1 sentinel.</summary>
    private void RelocateEditorResizePlayers(int shiftX, int shiftY)
    {
        var rows = PlayerSettingsRows();
        foreach (var row in rows.Where(row => row.ID is >= 3 and <= 7 && row.Computer))
        {
            if (!VerifiedResizePlayer(row.Record) || rows.Count(other => other.ID == row.ID) != 1)
                throw new InvalidOperationException("A verified unambiguous computer keeper is required for source player relocation.");
            var record = row.Record;
            NativeField Field(string name) => record.Fields.Single(field => field.Name == PlayerCoordinateField(record, IsLegacyProject, name));
            int Read(string name) => int.Parse(record.Read(Field(name)), CultureInfo.InvariantCulture);
            void Shift(string name, int shift)
            {
                var value = unchecked(Read(name) + shift);
                Field(name).Encode((IsLegacyProject ? value : unchecked((ushort)value)).ToString(CultureInfo.InvariantCulture))
                    .CopyTo(record.File.Bytes, record.Offset + Field(name).Offset);
            }
            if (!IsLegacyProject || Read("CPUAreaX1") != -1)
            {
                Shift("CPUAreaX1", shiftX); Shift("CPUAreaY1", shiftY);
                Shift("CPUAreaX2", shiftX); Shift("CPUAreaY2", shiftY);
            }
            // V1/V2 readers reset camera to -1; ignored serialized V2 bytes
            // must remain opaque instead of becoming active coordinates.
            if (ResizePlayerCameraSupported(record) && (!IsLegacyProject || Read("CameraX") != -1))
            { Shift("CameraX", shiftX); Shift("CameraY", shiftY); }
        }
    }
}
