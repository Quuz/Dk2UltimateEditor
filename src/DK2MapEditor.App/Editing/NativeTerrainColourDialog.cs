using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DK2MapEditor.App.Editing;

internal static class NativeTerrainColourDialog
{
    // Original RectEx_ChooseColor keeps this palette across both light buttons.
    private static readonly int[] customColours = new int[16];
    [StructLayout(LayoutKind.Sequential)]
    private struct ChooseColourData
    {
        public uint Size;
        public nint Owner, Instance;
        public uint Result;
        public nint CustomColours;
        public uint Flags;
        public nint CustomData, Hook, Template;
    }
    [DllImport("comdlg32.dll", EntryPoint = "ChooseColorW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ChooseColor(ref ChooseColourData data);
    public static uint? Choose(Window window, uint initial)
    {
        var memory = Marshal.AllocHGlobal(16 * sizeof(int));
        try
        {
            customColours[0] = customColours[1] = (int)(initial & 0xffffff);
            Marshal.Copy(customColours, 0, memory, 16);
            var data = new ChooseColourData { Size = (uint)Marshal.SizeOf<ChooseColourData>(), Owner = new WindowInteropHelper(window).Handle,
                Result = initial & 0xffffff, CustomColours = memory, Flags = 0x1 | 0x2 };
            var accepted = ChooseColor(ref data);
            Marshal.Copy(memory, customColours, 0, 16);
            return accepted ? data.Result : null;
        }
        finally { Marshal.FreeHGlobal(memory); }
    }
}
