using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private const uint DefaultResizeFillPreset = 0x02020101;
    private NativeFile? nativeResizeFill;
    private uint originalNativeResizeFill = DefaultResizeFillPreset;
    // Like target version, the original editor's FillID is working metadata
    // absent from GAMEINFO. Native loading uses MAP_DEFAULTID; New/compiled
    // working documents retain the project's selected background until reload.
    private NativeFile NativeResizeFill
    {
        get
        {
            if (nativeResizeFill is null)
            {
                var bytes = new byte[4]; WriteUInt32(bytes, 0, DefaultResizeFillPreset);
                nativeResizeFill = new(MainFile.SourcePath + "#ResizeFill", MainFile.ExportPath + "#ResizeFill", bytes);
            }
            return nativeResizeFill;
        }
    }
    private bool NativeResizeFillChanged => !IsLegacyProject && NativeResizeFill.UInt32(0) != originalNativeResizeFill;
    private void WriteNativeResizeFill(uint value) => WriteUInt32(NativeResizeFill.Bytes, 0, value);
    public uint? ResizeFillPreset
    {
        get
        {
            if (!IsLegacyProject) return TargetGameVersion is null ? null : NativeResizeFill.UInt32(0);
            var info = projectSections.SingleOrDefault(item => item.Signature == LegacyInfo);
            if (info is null) return null;
            var structure = info.UInt32(4) switch { 1 => "MAPPROPERTIESV1", 2 or 3 => "MAPPROPERTIESV2", 4 => "MAPPROPERTIESV4", 5 => "MAPPROPERTIESV5", 6 => "MAPPROPERTIESV6", 7 => "MAPPROPERTIES", _ => null };
            if (structure is null || info.Length != 12 + LegacyProjectSchemas.Size(structure)) return null;
            var fillField = LegacyProjectSchemas.Fields(structure).Single(item => item.Name == "FillID");
            return unchecked((uint)int.Parse(fillField.Read(info.Data.Span[12..]), CultureInfo.InvariantCulture));
        }
    }
}
