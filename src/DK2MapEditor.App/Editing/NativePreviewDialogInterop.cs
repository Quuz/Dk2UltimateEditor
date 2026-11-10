using System.Runtime.InteropServices;
using System.Text;

namespace DK2MapEditor.App.Editing;

internal static class NativePreviewDialogInterop
{
    internal delegate UIntPtr Hook(IntPtr window,uint message,UIntPtr wParam,IntPtr lParam);
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]internal struct OpenFileName
    {
        public uint Size;public IntPtr Owner,Instance;[MarshalAs(UnmanagedType.LPWStr)]public string? Filter;public IntPtr CustomFilter;public uint CustomFilterSize,FilterIndex;public IntPtr File;public uint MaxFile;public IntPtr FileTitle;public uint MaxFileTitle;
        [MarshalAs(UnmanagedType.LPWStr)]public string? InitialDirectory;[MarshalAs(UnmanagedType.LPWStr)]public string? Title;public uint Flags;public ushort FileOffset,ExtensionOffset;
        [MarshalAs(UnmanagedType.LPWStr)]public string? DefaultExtension;public IntPtr Data;[MarshalAs(UnmanagedType.FunctionPtr)]public Hook? Callback;public IntPtr Template,Reserved;public uint ReservedValue,FlagsEx;
    }
    [StructLayout(LayoutKind.Sequential)]internal struct Rect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]internal struct Header{public IntPtr From;public UIntPtr ID;public uint Code;}
    [StructLayout(LayoutKind.Sequential)]internal struct Notification{public Header Header;public IntPtr FileName,Path;}
    [StructLayout(LayoutKind.Sequential)]internal struct DrawItem{public uint Type,ID,ItemID,Action,State;public IntPtr Window,DC;public Rect Rect;public UIntPtr Data;}
    [StructLayout(LayoutKind.Sequential)]internal struct BitmapInfo{public uint Size;public int Width,Height;public ushort Planes,Bits;public uint Compression,ImageSize;public int XPels,YPels;public uint Used,Important;}
    [DllImport("comdlg32.dll",CharSet=CharSet.Unicode)][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool GetOpenFileNameW(ref OpenFileName value);
    [DllImport("comdlg32.dll",CharSet=CharSet.Unicode)][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool GetSaveFileNameW(ref OpenFileName value);
    [DllImport("comdlg32.dll")]internal static extern uint CommDlgExtendedError();
    [DllImport("kernel32.dll")]internal static extern IntPtr GlobalAlloc(uint flags,UIntPtr bytes);
    [DllImport("kernel32.dll")]internal static extern IntPtr GlobalFree(IntPtr handle);
    [DllImport("user32.dll")]internal static extern IntPtr GetParent(IntPtr window);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern IntPtr CreateWindowExW(uint extended,string type,string text,uint style,int x,int y,int width,int height,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern IntPtr SendMessageW(IntPtr window,uint message,UIntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern IntPtr SendMessageW(IntPtr window,uint message,UIntPtr wParam,StringBuilder text);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool PostMessageW(IntPtr window,uint message,UIntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool SetWindowTextW(IntPtr window,string text);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool GetClientRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool MapDialogRect(IntPtr window,ref Rect rect);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool MoveWindow(IntPtr window,int x,int y,int width,int height,[MarshalAs(UnmanagedType.Bool)]bool repaint);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool InvalidateRect(IntPtr window,IntPtr rect,[MarshalAs(UnmanagedType.Bool)]bool erase);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool DrawFrameControl(IntPtr dc,ref Rect rect,uint type,uint state);
    [DllImport("user32.dll")]internal static extern int FillRect(IntPtr dc,ref Rect rect,IntPtr brush);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern IntPtr LoadImageW(IntPtr instance,string name,uint type,int width,int height,uint flags);
    [DllImport("gdi32.dll")]internal static extern IntPtr GetStockObject(int index);
    [DllImport("gdi32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool DeleteObject(IntPtr handle);
    [DllImport("gdi32.dll")]internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")]internal static extern IntPtr SelectObject(IntPtr dc,IntPtr value);
    [DllImport("gdi32.dll")]internal static extern int SetStretchBltMode(IntPtr dc,int mode);
    [DllImport("gdi32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool StretchBlt(IntPtr dc,int x,int y,int width,int height,IntPtr source,int sx,int sy,int sw,int sh,uint mode);
    [DllImport("gdi32.dll")]internal static extern int StretchDIBits(IntPtr dc,int x,int y,int width,int height,int sx,int sy,int sw,int sh,byte[] bytes,ref BitmapInfo info,uint usage,uint mode);
}
